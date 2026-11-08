using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;
using Pocket.Core.Tracking;

namespace Pocket.App.Tracking;

internal sealed record CameraDevice(string Id, string Name)
{
    public override string ToString() => Name;
}
internal sealed record CameraInventory(IReadOnlyList<CameraDevice> Devices, string? Error = null);

/// <summary>Owns one explicitly started tracking worker. Start and Stop are called by the WPF dispatcher.</summary>
internal sealed class RecordedTrackingController
{
    private readonly TrackingSession _session = new();
    private readonly string _root;
    private CancellationTokenSource? _stop;
    private Task _run = Task.CompletedTask;
    private volatile int _lastPid;
    private volatile bool _forcedStop;
    private volatile string? _workerPhase;
    private volatile string? _workerNativeCode;
    private Process? _unconfirmedWorker;
    private Guid _unconfirmedSession;
    private volatile bool _explicitStop;
    private CameraPreviewChannel? _previewChannel;
    public CameraPreviewFrame? ReadPreview(long? matchSequence = null)
    {
        var channel = Volatile.Read(ref _previewChannel);
        var frame = matchSequence is { } sequence ? channel?.Matching(sequence) ?? channel?.Latest : channel?.Latest;
        return frame is not null && Environment.TickCount64 - frame.ReceivedMs < 500 ? frame : null;
    }
    public bool PreviewFailed => Volatile.Read(ref _previewChannel)?.Failed == true;
    internal static string? DiagnosticDirectory => Environment.GetEnvironmentVariable("INVEY_CAMERA_DIAGNOSTICS") ??
        Environment.GetEnvironmentVariable("GHOST_CAMERA_DIAGNOSTICS");
    private readonly string? _diagnostics = DiagnosticDirectory;
    public const int DiagnosticSeconds = 178;

    public bool HasDiagnostics => !string.IsNullOrWhiteSpace(_diagnostics) ||
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("INVEY_CAMERA_EXECUTION")) ||
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("INVEY_CAMERA_BUDGET"));
    public bool DiagnosticBudgetAvailable => !HasDiagnostics ||
        (Directory.Exists(_diagnostics) && File.Exists(Environment.GetEnvironmentVariable("INVEY_CAMERA_BUDGET")));

    public RecordedTrackingController(string? root = null) =>
        _root = root ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    public TrackingSnapshot Read() => _session.Read();
    public int? LastWorkerPid => _lastPid > 0 ? _lastPid : null;
    public bool ForcedStop => _forcedStop;
    public string? WorkerPhase => _workerPhase;
    public string? WorkerNativeCode => _workerNativeCode;
    public bool CanStart => _run.IsCompleted && _session.Tick() is TrackingState.Off or TrackingState.Stopped or TrackingState.Unavailable;

    public bool Start(CameraDevice? device, string? fixtureFile = null, string? testFault = null, bool preview = false)
    {
        var source = device is null || fixtureFile is not null ? TrackingSource.Recorded : TrackingSource.Camera;
        bool boundedCamera = source == TrackingSource.Camera && testFault is null && HasDiagnostics;
        if (!CanStart || (boundedCamera && !DiagnosticBudgetAvailable) || _session.Enable(source) is not { } id) return false;
        _stop?.Dispose();
        _stop = new CancellationTokenSource();
        _explicitStop = false;
        string? diagnostics = boundedCamera ? _diagnostics : null;
        if (boundedCamera) _stop.CancelAfter(TimeSpan.FromSeconds(DiagnosticSeconds));
        _lastPid = 0;
        _forcedStop = false;
        _workerPhase = null;
        _workerNativeCode = null;
        var token = _stop.Token;
        _run = Task.Run(() => RunAsync(id, device, fixtureFile, testFault, diagnostics, preview, token));
        return true;
    }

    public async Task<bool> StopAsync()
    {
        if (_stop?.IsCancellationRequested == false) _explicitStop = true;
        _session.Stop(); // Invalidate the observation before awaiting worker cleanup.
        _stop?.Cancel();
        await _run;
        if (_unconfirmedWorker is { } worker && await WaitForExitAsync(worker, TimeSpan.FromMilliseconds(500)))
        {
            _session.ConfirmWorkerExit(_unconfirmedSession);
            worker.Dispose();
            _unconfirmedWorker = null;
        }
        return _session.Tick() != TrackingState.Stopping;
    }

    private async Task RunAsync(Guid id, CameraDevice? device, string? fixtureFile, string? testFault,
        string? diagnostics, bool preview, CancellationToken stop)
    {
        Process? worker = null;
        CameraBudgetGuard? budgetGuard = null;
        CameraPreviewChannel? previewChannel = null;
        SafeFileHandle? job = null;
        using var readCancellation = CancellationTokenSource.CreateLinkedTokenSource(stop);
        NamedPipeServerStream? pipe = null;
        Task<TrackerMessage?>? pendingRead = null;
        Task? stderrDrain = null;
        var clock = Stopwatch.StartNew();
        var startedUtc = DateTimeOffset.UtcNow;
        double? firstResultMs = null;
        long lastSequence = -1;
        long rejectedObservations = 0;
        try
        {
            stop.ThrowIfCancellationRequested();
            var start = TrackingStartInfo(id, device, fixtureFile, testFault);
            if (diagnostics is not null)
            {
                start.ArgumentList.Add("--diagnostics");
                start.ArgumentList.Add(Path.Combine(diagnostics, id + ".worker.json"));
            }
            if (preview)
            {
                previewChannel = new CameraPreviewChannel();
                Volatile.Write(ref _previewChannel, previewChannel);
                start.ArgumentList.Add("--preview-pipe");
                start.ArgumentList.Add(previewChannel.Name);
            }
            pipe = new NamedPipeServerStream("GhostTracking-" + id.ToString("N"), PipeDirection.In, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly, 4096, 0);
            job = RecordedWorkerJob.Create();
            worker = Process.Start(start) ?? throw new IOException("Tracker did not start.");
            _lastPid = worker.Id;
            RecordedWorkerJob.Assign(job, worker);
            previewChannel?.Start(worker.Id);
            stderrDrain = DrainAsync(worker.StandardError);
            if (device is not null && fixtureFile is null && testFault is null &&
                (HasDiagnostics || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("INVEY_CAMERA_BUDGET"))))
                budgetGuard = await CameraBudgetGuard.StartAsync(worker, id, diagnostics ??
                    throw new InvalidDataException("Camera diagnostics are required for the execution budget."), stop);
            stop.ThrowIfCancellationRequested();
            await worker.StandardInput.WriteLineAsync("start".AsMemory(), stop).ConfigureAwait(false);
            await worker.StandardInput.FlushAsync(stop).ConfigureAwait(false);
            using (var connection = CancellationTokenSource.CreateLinkedTokenSource(stop))
            {
                connection.CancelAfter(TrackingSession.StartupDeadline);
                await pipe.WaitForConnectionAsync(connection.Token).ConfigureAwait(false);
            }
            if (!RecordedWorkerJob.IsPeer(pipe, worker.Id))
                throw new InvalidDataException("Unexpected tracker pipe peer.");

            var reader = new TrackingLineReader(pipe);
            while (!stop.IsCancellationRequested && _session.Tick() != TrackingState.Stopping)
            {
                pendingRead ??= reader.ReadAsync(readCancellation.Token).AsTask();
                TrackerMessage? message;
                try { message = await pendingRead.WaitAsync(TimeSpan.FromMilliseconds(100), stop).ConfigureAwait(false); }
                catch (TimeoutException) { continue; }
                pendingRead = null;
                if (stop.IsCancellationRequested) break;
                if (message is null or TrackerStopped)
                {
                    _session.ReportFailure(id, TrackingFailure.WorkerExited);
                    break;
                }
                bool accepted = _session.Accept(message);
                if (message is HandObservation observation)
                {
                    if (accepted)
                    {
                        firstResultMs ??= clock.Elapsed.TotalMilliseconds;
                        lastSequence = observation.Sequence;
                    }
                    else rejectedObservations++;
                }
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        catch (OperationCanceledException) { _session.ReportFailure(id, TrackingFailure.Timeout); }
        catch (InvalidDataException) { _session.ReportProtocolFailure(id); }
        catch (TimeoutException) { _session.ReportFailure(id, TrackingFailure.Timeout); }
        catch (Exception ex) when (ex is IOException or Win32Exception or UnauthorizedAccessException or InvalidOperationException)
        {
            _session.ReportFailure(id, TrackingFailure.RuntimeFailure);
        }
        finally
        {
            double cleanupStartMs = clock.Elapsed.TotalMilliseconds;
            long? workingSet = null;
            if (diagnostics is not null && worker is not null)
            {
                try { workingSet = worker.WorkingSet64; }
                catch (Exception ex) when (ex is InvalidOperationException or Win32Exception) { }
            }
            _session.Stop();
            if (previewChannel is not null)
            {
                await previewChannel.DisposeAsync();
                Volatile.Write(ref _previewChannel, null);
            }
            readCancellation.Cancel();
            try { pipe?.Dispose(); }
            catch (IOException ex) { Trace.TraceWarning("Tracking pipe close failed: 0x{0:X8}", ex.HResult); }
            if (pendingRead is not null)
            {
                // Observe a canceled pipe read without blocking cleanup.
                _ = pendingRead.ContinueWith(task => _ = task.Exception, TaskScheduler.Default);
            }

            if (worker is null)
            {
                job?.Dispose();
                _session.ConfirmWorkerExit(id);
            }
            else
            {
                try { worker.StandardInput.Close(); }
                catch (Exception ex) when (ex is IOException or InvalidOperationException)
                {
                    Trace.TraceWarning("Tracking stop signal close failed: 0x{0:X8}", ex.HResult);
                }
                bool exited = await WaitForExitAsync(worker, TrackingSession.StopDeadline).ConfigureAwait(false);
                if (!exited)
                {
                    _forcedStop = true;
                    // Closing the job also covers a worker blocked inside native inference.
                    job?.Dispose();
                    try { if (!worker.HasExited) worker.Kill(); }
                    catch (Exception ex) when (ex is Win32Exception or InvalidOperationException) { }
                    exited = await WaitForExitAsync(worker, TrackingSession.StopDeadline).ConfigureAwait(false);
                }
                job?.Dispose();
                if (stderrDrain is not null)
                {
                    worker.StandardError.Dispose();
                    await stderrDrain.ConfigureAwait(false);
                }
                if (exited)
                {
                    worker.Dispose();
                    _session.ConfirmWorkerExit(id);
                }
                else
                {
                    _unconfirmedWorker = worker;
                    _unconfirmedSession = id;
                }
            }
            if (budgetGuard is not null) await budgetGuard.DisposeAsync();
            if (diagnostics is not null)
            {
                var final = _session.Read();
                try
                {
                    File.WriteAllText(Path.Combine(diagnostics, id + ".host.json"), JsonSerializer.Serialize(new
                    {
                        Session = id, StartedUtc = startedUtc, WorkerPid = LastWorkerPid,
                        FirstAcceptedResultMs = firstResultMs, LastSequence = lastSequence,
                        ReceivedObservations = final.AcceptedObservations + rejectedObservations,
                        final.AcceptedObservations, RejectedObservations = rejectedObservations,
                        final.SupersededObservations, final.Continuity, HostCounters = _session.ReadCounters(),
                        CurrentObservationCleared = final.Observation is null,
                        State = final.State.ToString(), Failure = final.Failure?.ToString(),
                        ExitConfirmed = !final.SessionId.HasValue, ForcedStop, WorkerPhase, WorkerNativeCode,
                        Stop = _explicitStop ? "Explicit" : stop.IsCancellationRequested ? "DiagnosticLimit" : "Failure",
                        CleanupMs = clock.Elapsed.TotalMilliseconds - cleanupStartMs, WorkerWorkingSetBytes = workingSet
                    }, new JsonSerializerOptions { WriteIndented = true }));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                { Trace.TraceWarning("Tracking diagnostic write failed: 0x{0:X8}", ex.HResult); }
            }
        }
    }

    private ProcessStartInfo WorkerStartInfo()
    {
        string environment = Path.Combine(_root, ".tools", "exec05-inference");
        string? home = File.ReadLines(Path.Combine(environment, "pyvenv.cfg"))
            .FirstOrDefault(line => line.StartsWith("home = ", StringComparison.Ordinal))?[7..];
        if (string.IsNullOrWhiteSpace(home)) throw new IOException("Tracking runtime is unavailable.");
        string worker = Path.Combine(_root, "tests", "tracking", "recorded_worker.py");
        if (!File.Exists(worker)) throw new IOException("Tracking worker is unavailable.");
        // Launch the base interpreter directly. The Windows venv launcher may create a second process.
        var info = new ProcessStartInfo(Path.Combine(home, "python.exe"))
        {
            WorkingDirectory = _root, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardError = true
        };
        foreach (var arg in new[] { "-I", "-B", "-S", worker })
            info.ArgumentList.Add(arg);
        return info;
    }

    private ProcessStartInfo TrackingStartInfo(Guid id, CameraDevice? device, string? fixtureFile, string? testFault)
    {
        var info = WorkerStartInfo();
        string environment = Path.Combine(_root, ".tools", "exec05-inference");
        string assets = Path.Combine(_root, ".tools", "exec05-model");
        foreach (var arg in new[] { "--pipe", "GhostTracking-" + id.ToString("N"),
                     "--session", id.ToString(), "--site-packages", Path.Combine(environment, "Lib", "site-packages"),
                     "--model", Path.Combine(assets, "hand_landmarker.task"), "--palm", Path.Combine(assets, "public-palm.jpg") })
            info.ArgumentList.Add(arg);
        if (fixtureFile is not null)
        {
            info.ArgumentList.Add("--fixture-file");
            info.ArgumentList.Add(fixtureFile);
        }
        else if (device is not null)
        {
            info.ArgumentList.Add("--camera");
            info.ArgumentList.Add(device.Id);
        }
        if (testFault is not null)
        {
            info.ArgumentList.Add("--fault");
            info.ArgumentList.Add(testFault);
        }
        return info;
    }

    public Task<CameraInventory> FindCamerasAsync(CancellationToken cancel) =>
        Task.Run(() => ScanAsync(cancel), cancel);

    private async Task<CameraInventory> ScanAsync(CancellationToken cancel)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        deadline.CancelAfter(TrackingSession.StartupDeadline);
        var token = deadline.Token;
        try
        {
            token.ThrowIfCancellationRequested();
            var start = WorkerStartInfo();
            start.ArgumentList.Add("--list-cameras");
            start.RedirectStandardOutput = true;
            using var job = RecordedWorkerJob.Create();
            using var worker = Process.Start(start) ?? throw new IOException("Camera discovery could not start.");
            try
            {
                RecordedWorkerJob.Assign(job, worker);
                await worker.StandardInput.WriteLineAsync("start".AsMemory(), token).ConfigureAwait(false);
                worker.StandardInput.Close();
                var output = new StringBuilder();
                var buffer = new char[1024];
                int read;
                while ((read = await worker.StandardOutput.ReadAsync(buffer.AsMemory(), token).ConfigureAwait(false)) != 0)
                {
                    if (output.Length + read > 128 * 1024) throw new InvalidDataException("Camera inventory too large.");
                    output.Append(buffer, 0, read);
                }
                await worker.WaitForExitAsync(token).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(output.ToString(), new JsonDocumentOptions { MaxDepth = 4 });
                if (doc.RootElement.TryGetProperty("error", out var error))
                    return new([], error.GetString() == "permission-denied" ? "PermissionDenied" : "Unavailable");
                var entries = doc.RootElement.GetProperty("devices");
                if (entries.GetArrayLength() > 64) throw new InvalidDataException("Too many camera devices.");
                var devices = new List<CameraDevice>();
                foreach (var entry in entries.EnumerateArray())
                {
                    string? id = entry.GetProperty("id").GetString();
                    string? name = entry.GetProperty("name").GetString();
                    if (id?.Length != 64 || !id.All(Uri.IsHexDigit) || string.IsNullOrWhiteSpace(name) || name.Length > 256)
                        throw new InvalidDataException("Invalid camera inventory.");
                    devices.Add(new(id, name));
                }
                return new(devices);
            }
            finally
            {
                try { if (!worker.HasExited) worker.Kill(); }
                catch (Exception ex) when (ex is Win32Exception or InvalidOperationException) { }
            }
        }
        catch (OperationCanceledException) when (!cancel.IsCancellationRequested) { return new([], "Timeout"); }
        catch (Exception ex) when (ex is IOException or Win32Exception or UnauthorizedAccessException or
                                   JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return new([], "Unavailable");
        }
    }

    private async Task DrainAsync(StreamReader reader)
    {
        var buffer = new char[1024];
        var line = new System.Text.StringBuilder(96);
        try
        {
            int count;
            while ((count = await reader.ReadAsync(buffer.AsMemory()).ConfigureAwait(false)) != 0)
            {
                for (int i = 0; i < count; i++)
                {
                    if (buffer[i] == '\n')
                    {
                        string entry = line.ToString().TrimEnd('\r');
                        if (entry == "GHOST_TRACKING_DIAGNOSTICS_WRITE_FAILED")
                            Trace.TraceWarning("Tracking worker could not write diagnostic evidence.");
                        const string NativeCodePrefix = "GHOST_TRACKING_NATIVE_CODE=";
                        if (entry.StartsWith(NativeCodePrefix, StringComparison.Ordinal) &&
                            entry.Length == NativeCodePrefix.Length + 8 &&
                            entry[NativeCodePrefix.Length..].All(Uri.IsHexDigit))
                            _workerNativeCode = entry[NativeCodePrefix.Length..];
                        _workerPhase = entry switch
                        {
                            "GHOST_TRACKING_PHASE=waiting-for-start" => "WaitingForStart",
                            "GHOST_TRACKING_PHASE=connecting" => "Connecting",
                            "GHOST_TRACKING_PHASE=connected" => "Connected",
                            "GHOST_TRACKING_PHASE=importing" => "Importing",
                            "GHOST_TRACKING_PHASE=opening-model" => "OpeningModel",
                            "GHOST_TRACKING_PHASE=opening-camera" => "OpeningCamera",
                            "GHOST_TRACKING_PHASE=ready" => "Ready",
                            _ => _workerPhase
                        };
                        line.Clear();
                    }
                    else if (line.Length < 96) line.Append(buffer[i]);
                }
            }
        }
        catch (IOException ex) { Trace.TraceWarning("Tracking diagnostic stream closed: 0x{0:X8}", ex.HResult); }
        catch (ObjectDisposedException) { }
    }

    private static async Task<bool> WaitForExitAsync(Process worker, TimeSpan limit)
    {
        using var deadline = new CancellationTokenSource(limit);
        try
        {
            await worker.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is OperationCanceledException or Win32Exception or InvalidOperationException) { return false; }
    }
}
