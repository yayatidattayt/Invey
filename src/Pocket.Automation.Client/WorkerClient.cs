using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Pocket.Automation.Contracts;

namespace Pocket.Automation.Client;

/// <summary>One supervised worker and one unresolved mutation per fixture session.</summary>
public sealed class WorkerClient : IAsyncDisposable
{
    private sealed record PendingMutation(Request Request, TargetBinding Binding)
    {
        public int Checks { get; set; }
    }

    private readonly string _dotnet, _worker;
    private readonly int _fixturePid, _deadline;
    private readonly long _fixtureLifetime;
    private readonly Action<Process> _terminate;
    private readonly SemaphoreSlim _serial = new(1, 1);
    private readonly RequestLedger _requests = new();
    private readonly Dictionary<string, (Request Request, TargetBinding Binding)> _capabilities = new();
    private Process? _process;
    private bool _disposed;
    private bool _quarantined;
    private PendingMutation? _pending;

    public int? ActiveProcessId
    {
        get
        {
            var process = _process;
            try { return process is { HasExited: false } ? process.Id : null; }
            catch (InvalidOperationException) { return null; }
        }
    }
    public int Restarts { get; private set; }

    public WorkerClient(string dotnet, string worker, int fixturePid, int deadlineMs = 2000)
        : this(dotnet, worker, fixturePid, deadlineMs, static process => process.Kill()) { }

    internal WorkerClient(string dotnet, string worker, int fixturePid, int deadlineMs, Action<Process> terminate)
    {
        if (fixturePid <= 0 || deadlineMs < 50 || deadlineMs > 5000)
            throw new ArgumentOutOfRangeException(nameof(fixturePid));

        _dotnet = dotnet;
        _worker = worker;
        _fixturePid = fixturePid;
        _deadline = deadlineMs;
        _terminate = terminate;
        using var owner = Process.GetProcessById(fixturePid);
        _fixtureLifetime = owner.StartTime.ToUniversalTime().Ticks;
    }

    private void Start()
    {
        var info = new ProcessStartInfo(_dotnet)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        info.ArgumentList.Add(_worker);
        info.ArgumentList.Add("--fixture-pid");
        info.ArgumentList.Add(_fixturePid.ToString());
        info.ArgumentList.Add("--lifetime");
        info.ArgumentList.Add(_fixtureLifetime.ToString());
        _process = Process.Start(info) ?? throw new IOException("Worker did not start.");
        _ = DrainErrors(_process.StandardError);
        Restarts++;
    }

    private static async Task DrainErrors(StreamReader reader)
    {
        var buffer = new char[1024];
        try { while (await reader.ReadAsync(buffer.AsMemory()).ConfigureAwait(false) > 0) { } }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
    }

    public async Task<Reply> RequestAsync(Request request, CancellationToken cancellationToken = default)
    {
        bool execute = request.Operation == "Execute";
        Reply Reject(string status, string? reason = null, MutationOutcome outcome = MutationOutcome.Rejected)
        {
            var reply = new Reply(request.Id, status, Error: reason);
            return execute ? reply.WithOutcome(outcome) : reply;
        }

        if (!Protocol.Valid(request) || request.Operation == "Observe") return Reject("Denied", "Invalid request.");
        if (cancellationToken.IsCancellationRequested) return Reject("Canceled");
        if (!await _serial.WaitAsync(0).ConfigureAwait(false)) return Reject("Busy", "One request is already active.");
        try
        {
            if (_disposed) return Reject("Unavailable");
            if (!_requests.TryAdd(request.Id)) return Reject("Denied", "Duplicate ID or session request limit reached.");
            if (execute && _pending is not null) return Reject("Busy", "Review or dismiss the unresolved action first.");

            TargetBinding? binding = null;
            if (execute)
            {
                if (!_capabilities.Remove(request.Token!, out var capability) ||
                    request.Window != capability.Request.Window || request.ProcessId != capability.Request.ProcessId ||
                    request.Target != capability.Request.Target || request.X != capability.Request.X || request.Y != capability.Request.Y)
                    return Reject("Stale", "A fresh target probe is required.", MutationOutcome.Conflict);
                binding = capability.Binding;
            }

            var reply = await ExchangeAsync(request, cancellationToken).ConfigureAwait(false);
            if (execute && reply.Outcome == MutationOutcome.Unknown)
            {
                _capabilities.Clear();
                _pending = new(request, binding!);
            }
            else if (!execute && reply.Status == "Confirmed" && reply.Token is not null && reply.Binding is not null)
            {
                if (_capabilities.Count >= 32) _capabilities.Clear();
                _capabilities[reply.Token] = (request, reply.Binding);
            }
            return reply;
        }
        finally { _serial.Release(); }
    }

    public async Task<Reply> ReconcileAsync(string operationId, CancellationToken cancellationToken = default)
    {
        Reply Unobserved(string status) =>
            new Reply(operationId, status, Observation: Observation.Inconclusive).WithOutcome(MutationOutcome.Unknown);

        if (cancellationToken.IsCancellationRequested) return Unobserved("Canceled");
        if (!await _serial.WaitAsync(0).ConfigureAwait(false)) return Unobserved("Busy");
        try
        {
            if (_disposed || _pending is not { } pending || pending.Request.Id != operationId || pending.Checks >= 3)
                return Unobserved("Denied");

            pending.Checks++;
            var request = pending.Request with
            {
                Id = Guid.NewGuid().ToString(),
                Operation = "Observe",
                Token = null,
                Binding = pending.Binding
            };
            if (!_requests.TryAdd(request.Id)) return Unobserved("Denied");
            var reply = await ExchangeAsync(request, cancellationToken).ConfigureAwait(false);
            return (reply with { Id = operationId, Observation = reply.Observation ?? Observation.Inconclusive })
                .WithOutcome(MutationOutcome.Unknown);
        }
        finally { _serial.Release(); }
    }

    public async Task<bool> DismissAsync(string operationId)
    {
        if (!await _serial.WaitAsync(0).ConfigureAwait(false)) return false;
        try
        {
            if (_pending?.Request.Id != operationId) return false;
            _pending = null;
            _capabilities.Clear();
            return true;
        }
        finally { _serial.Release(); }
    }

    private async Task<Reply> ExchangeAsync(Request request, CancellationToken cancellationToken)
    {
        bool mayHaveDispatched = false;
        bool execute = request.Operation == "Execute";
        Reply Failure(string status, string? error = null)
        {
            var reply = new Reply(request.Id, status, Error: error);
            return execute ? reply.WithOutcome(mayHaveDispatched ? MutationOutcome.Unknown : MutationOutcome.Rejected) : reply;
        }

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(_deadline);
        try
        {
            deadline.Token.ThrowIfCancellationRequested();
            if (_quarantined || _process is { HasExited: true })
            {
                if (!await StopAsync().ConfigureAwait(false))
                    return Failure("Unavailable", "Previous worker exit could not be confirmed.");
                if (execute)
                    return new Reply(request.Id, "Stale", Error: "Worker generation changed.").WithOutcome(MutationOutcome.Conflict);
            }
            deadline.Token.ThrowIfCancellationRequested();
            if (_process is null)
            {
                // Process startup and failure cleanup can perform synchronous OS work.
                await Task.Run(Start, deadline.Token).ConfigureAwait(false);
            }
            string json = JsonSerializer.Serialize(request);
            if (json.Length > Protocol.MaxLine) return Failure("Denied");
            deadline.Token.ThrowIfCancellationRequested();
            mayHaveDispatched = true;
            await _process!.StandardInput.WriteLineAsync(json.AsMemory(), deadline.Token).ConfigureAwait(false);
            string response = await ReadBounded(_process.StandardOutput, deadline.Token).ConfigureAwait(false);
            var reply = JsonSerializer.Deserialize<Reply>(response) ?? throw new IOException("Missing reply.");
            if (reply.Id != request.Id || string.IsNullOrWhiteSpace(reply.Status) ||
                (execute && (reply.Outcome is not { } outcome || !Enum.IsDefined(outcome) ||
                    reply.OutcomeUnknown != (outcome == MutationOutcome.Unknown) ||
                    (reply.Status == "Confirmed") != (outcome == MutationOutcome.Applied))) ||
                (request.Operation == "Observe" && reply.Status == "Observed" &&
                    (reply.Observation is not { } observation || !Enum.IsDefined(observation))))
                throw new IOException("Invalid reply or correlation.");
            return reply;
        }
        catch (OperationCanceledException)
        {
            bool stopped = await StopAsync().ConfigureAwait(false);
            return Failure(cancellationToken.IsCancellationRequested ? "Canceled" : "Timeout",
                stopped ? "Worker stopped." : "Worker exit could not be confirmed.");
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidOperationException or Win32Exception)
        {
            bool stopped = await StopAsync().ConfigureAwait(false);
            return Failure("Unavailable", stopped ? ex.GetType().Name : "Worker exit could not be confirmed.");
        }
    }

    private static async Task<string> ReadBounded(StreamReader reader, CancellationToken token)
    {
        var result = new StringBuilder();
        var one = new char[1];
        while (true)
        {
            if (await reader.ReadAsync(one.AsMemory(), token).ConfigureAwait(false) == 0) throw new IOException("Worker EOF.");
            if (one[0] == '\n') return result.ToString().TrimEnd('\r');
            if (result.Length >= Protocol.MaxLine) throw new IOException("Oversized reply.");
            result.Append(one[0]);
        }
    }

    private async Task<bool> StopAsync()
    {
        _capabilities.Clear();
        if (_process is not { } process) return true;
        _quarantined = true;
        bool exited = await Task.Run(() =>
        {
            try
            {
                if (process.HasExited) return true;
                _terminate(process);
                return process.WaitForExit(1000);
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
            {
                // Termination may have raced a normal exit. Keep ownership until exit is observed.
                try { return process.HasExited; }
                catch (Exception check) when (check is InvalidOperationException or Win32Exception) { return false; }
            }
        }).ConfigureAwait(false);
        if (!exited) return false;

        _process = null;
        // The redirected streams were used directly. Close them after observed exit.
        try { process.StandardInput.Dispose(); }
        catch (IOException) { }
        process.StandardOutput.Dispose();
        process.StandardError.Dispose();
        process.Dispose();
        _quarantined = false;
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        await _serial.WaitAsync().ConfigureAwait(false);
        try
        {
            _disposed = true;
            if (!await StopAsync().ConfigureAwait(false))
                throw new IOException("Worker exit could not be confirmed. Disposal may be retried.");
        }
        finally { _serial.Release(); }
    }
}
