using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Pocket.Core.Tracking;

namespace Pocket.App.Tracking;

/// <summary>An independent process owns the camera job and its durable reservation before start is sent.</summary>
internal sealed class CameraBudgetGuard : IAsyncDisposable
{
    private readonly NamedPipeServerStream _pipe;
    private readonly Process _guardian;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private CameraBudgetGuard(NamedPipeServerStream pipe, Process guardian, StreamReader reader, StreamWriter writer)
        => (_pipe, _guardian, _reader, _writer) = (pipe, guardian, reader, writer);

    internal int GuardianId => _guardian.Id;

    public static async Task<CameraBudgetGuard> StartAsync(Process worker, Guid session, string diagnostics, CancellationToken stop,
        int limitMs = 178_000)
    {
        if (limitMs is < 100 or > 178_000) throw new ArgumentOutOfRangeException(nameof(limitMs));
        stop.ThrowIfCancellationRequested();
        string? ledger = Environment.GetEnvironmentVariable("INVEY_CAMERA_BUDGET");
        if (string.IsNullOrWhiteSpace(ledger) || !Directory.Exists(diagnostics))
            throw new InvalidDataException("Diagnostic camera configuration is incomplete.");
        string pipeName = "InveyCameraBudget-" + Guid.NewGuid().ToString("N");
        var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        Process? guardian = null;
        try
        {
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true };
            foreach (string arg in new[] { "--camera-budget-supervisor", pipeName,
                Process.GetCurrentProcess().Id.ToString(), worker.Id.ToString(), worker.StartTime.ToUniversalTime().Ticks.ToString(),
                session.ToString(), Path.GetFullPath(ledger), Path.GetFullPath(diagnostics), limitMs.ToString() }) start.ArgumentList.Add(arg);
            guardian = Process.Start(start) ?? throw new IOException("Camera supervisor could not start.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stop);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            await pipe.WaitForConnectionAsync(timeout.Token);
            if (!RecordedWorkerJob.IsPeer(pipe, guardian.Id)) throw new InvalidDataException("Invalid camera supervisor.");
            var reader = new StreamReader(pipe, Encoding.UTF8, false, 256, true);
            var writer = new StreamWriter(pipe, new UTF8Encoding(false), 256, true) { AutoFlush = true };
            string? reply = await reader.ReadLineAsync(timeout.Token);
            if (reply != "permit") throw new InvalidDataException("The independent camera budget denied this start.");
            return new(pipe, guardian, reader, writer);
        }
        catch
        {
            pipe.Dispose();
            if (guardian is not null)
            {
                // Closing the channel stops the guardian. Its owned job terminates an attached worker.
                if (!guardian.HasExited)
                {
                    try { await guardian.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2)); }
                    catch (TimeoutException)
                    {
                        guardian.Kill();
                        await guardian.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2));
                    }
                }
                guardian.Dispose();
            }
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            await _writer.WriteLineAsync("release".AsMemory(), timeout.Token);
            if (await _reader.ReadLineAsync(timeout.Token) != "released")
                Trace.TraceWarning("Camera budget release remains unconfirmed.");
            await _guardian.WaitForExitAsync(timeout.Token);
        }
        catch (Exception error) when (error is IOException or OperationCanceledException)
        { Trace.TraceWarning("Camera supervisor cleanup did not confirm release."); }
        finally { _pipe.Dispose(); _guardian.Dispose(); }
    }

    // Runs in its own process, independently of the main app's dispatcher and timers.
    internal static async Task<int> RunSupervisorAsync(string[] args)
    {
        if (args.Length != 9 || args[0] != "--camera-budget-supervisor") return 2;
        try
        {
            using var parent = Process.GetProcessById(int.Parse(args[2]));
            using var worker = Process.GetProcessById(int.Parse(args[3]));
            if (worker.StartTime.ToUniversalTime().Ticks != long.Parse(args[4]) || !Guid.TryParse(args[5], out var session))
                return 2;
            if (parent.MainModule?.FileName != Environment.ProcessPath) return 2;
            int limitMs = int.Parse(args[8]);
            if (limitMs is < 100 or > 178_000) return 2;
            string ledgerPath = Path.GetFullPath(args[6]), diagnostics = Path.GetFullPath(args[7]);
            if (!Directory.Exists(diagnostics)) return 2;
            using var connection = new NamedPipeClientStream(".", args[1], PipeDirection.InOut,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            using var connectDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await connection.ConnectAsync(connectDeadline.Token);
            if (!RecordedWorkerJob.IsServer(connection, parent.Id)) return 2;
            using var reader = new StreamReader(connection, Encoding.UTF8, false, 256, true);
            using var writer = new StreamWriter(connection, new UTF8Encoding(false), 256, true) { AutoFlush = true };
            using var budget = new CameraBudget(ledgerPath);
            if (limitMs > budget.SessionLimitMilliseconds - 2000) return 2;
            using var job = RecordedWorkerJob.Create();
            RecordedWorkerJob.Assign(job, worker);
            long started = Environment.TickCount64;
            budget.Reserve(session);
            await writer.WriteLineAsync("permit");
            var release = reader.ReadLineAsync();
            using var deadline = new CancellationTokenSource(TimeSpan.FromMilliseconds(limitMs));
            var ended = worker.WaitForExitAsync(deadline.Token);
            var parentEnded = parent.WaitForExitAsync(deadline.Token);
            var deadlineTask = Task.Delay(Timeout.Infinite, deadline.Token);
            var first = await Task.WhenAny(release, ended, parentEnded, deadlineTask);
            if (deadline.IsCancellationRequested) return 3;
            if (first == ended)
                await release.WaitAsync(TimeSpan.FromSeconds(2));
            else if (first != release)
                return 3; // Job disposal kills the worker. The charged unfinished reservation remains durable.
            if (!worker.HasExited || await release != "release" || !NativeReleaseConfirmed(diagnostics, session))
                return 3;
            long elapsed = Environment.TickCount64 - started;
            budget.Complete(session, elapsed);
            await writer.WriteLineAsync("released");
            return 0;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or
            System.ComponentModel.Win32Exception or FormatException or OverflowException or JsonException or TimeoutException or
            OperationCanceledException or ArgumentException)
        {
            Trace.TraceWarning("Independent camera supervisor denied or terminated a diagnostic session.");
            return 2;
        }
    }

    private static bool NativeReleaseConfirmed(string directory, Guid session)
    {
        string file = Path.Combine(directory, session + ".worker.json");
        using var stream = File.OpenRead(file);
        if (stream.Length is <= 0 or > 128 * 1024) return false;
        using var doc = JsonDocument.Parse(stream);
        var root = doc.RootElement;
        if (root.GetProperty("session").GetString() != session.ToString()) return false;
        if (root.TryGetProperty("nativeReleaseConfirmed", out var release)) return release.ValueKind == JsonValueKind.True;
        return root.TryGetProperty("stages", out var stages) && !stages.TryGetProperty("MFCreateDeviceSource", out _);
    }
}
