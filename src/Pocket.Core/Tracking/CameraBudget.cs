using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pocket.Core.Tracking;

public sealed record CameraReleaseEvidence(Guid Session, Guid WorkerSession, int HostWorkerPid, int WorkerPid,
    bool ExitConfirmed, bool ObservationCleared, string HostState, DateTimeOffset HostStarted,
    DateTimeOffset WorkerStarted, bool WorkerStillExists, bool IndicatorOffConfirmed)
{
    public bool IsSufficient => Session != Guid.Empty && Session == WorkerSession &&
        HostWorkerPid > 0 && HostWorkerPid == WorkerPid && ExitConfirmed && ObservationCleared &&
        HostState == "Stopped" && Math.Abs((WorkerStarted - HostStarted).TotalSeconds) < 15 &&
        !WorkerStillExists && IndicatorOffConfirmed;
}

public sealed record CameraBudgetAvailability(bool Available, int Used, int Allowed, int Remaining, string Message);

public sealed record CameraBudgetEntry([property: JsonRequired] Guid Session,
    [property: JsonRequired] long ChargedMs, [property: JsonRequired] bool Released);
public sealed record CameraBudgetState([property: JsonRequired] int Version,
    [property: JsonRequired] string Execution, [property: JsonRequired] CameraBudgetEntry[] Sessions, string? Amendment = null,
    int? Execution18StartCount = null,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] int? PointerStartCount = null);

/// <summary>One durable execution budget. Unfinished reservations fail closed across restarts.</summary>
public sealed class CameraBudget : IDisposable
{
    public const int MaxSessions = 6;
    public const int SessionMilliseconds = 120_000;
    public const int TotalMilliseconds = 720_000;
    public const string ContinuationAmendment = "execution-16-continuation-2026-11-08";
    public const string LearningAmendment = "execution-18-user-enabled-2026-11-09";
    public const string LearningTargetsAmendment = "execution-18-two-more-user-enabled-2026-11-09";
    public const string PointerAmendment = "execution-23-one-user-enabled-pointer-only-2026-11-10";
    public int SessionLimitMilliseconds => _state.Version >= 2 ? 180_000 : SessionMilliseconds;
    public int SessionLimitCount => _state.Version >= 2 ? 12 : MaxSessions;
    public int TotalLimitMilliseconds => _state.Version >= 2 ? 1_800_000 : TotalMilliseconds;
    private readonly string _path;
    private readonly FileStream _lock;
    private CameraBudgetState _state;
    private static readonly JsonSerializerOptions Schema = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, AllowDuplicateProperties = false
    };

    public static void Initialize(string path)
    {
        ValidatePath(path);
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(stream, new CameraBudgetState(1, "execution-16", []), Schema);
        stream.Flush(true);
    }

    public CameraBudget(string path)
    {
        _path = Path.GetFullPath(path);
        ValidatePath(_path);
        if (!File.Exists(_path)) throw new InvalidDataException("The diagnostic camera budget is missing.");
        _lock = new FileStream(_path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        try
        {
            using var stream = File.OpenRead(_path);
            if (stream.Length is <= 0 or > 8192) throw new InvalidDataException("Invalid camera budget size.");
            _state = JsonSerializer.Deserialize<CameraBudgetState>(stream, Schema)
                ?? throw new InvalidDataException("Invalid camera budget.");
            if (_state.Sessions is null || (_state.Version is < 1 or > 5) ||
                (_state.Version == 1 ? _state.Amendment is not null :
                    _state.Version == 2 ? _state.Amendment != ContinuationAmendment :
                    _state.Version == 3 ? _state.Amendment != LearningAmendment :
                    _state.Version == 4 ? _state.Amendment != LearningTargetsAmendment : _state.Amendment != PointerAmendment) ||
                (_state.Version >= 3 ? _state.Execution18StartCount is not { } start || start < 0 ||
                    start > _state.Sessions.Length || (_state.Version==5?_state.PointerStartCount??-1:_state.Sessions.Length) - start > (_state.Version >= 4 ? 5 : 3) : _state.Execution18StartCount is not null) ||
                (_state.Version==5 ? _state.PointerStartCount!=10||_state.Execution18StartCount!=5||_state.Sessions.Length is <10 or >11 : _state.PointerStartCount is not null) ||
                _state.Execution != "execution-16" || _state.Sessions is null ||
                _state.Sessions.Length > SessionLimitCount || _state.Sessions.Any(row => row is null || row.Session == Guid.Empty ||
                    row.ChargedMs < 0 || row.ChargedMs > SessionLimitMilliseconds) ||
                _state.Sessions.Select(row => row.Session).Distinct().Count() != _state.Sessions.Length ||
                _state.Sessions.Sum(row=>row.ChargedMs)>TotalLimitMilliseconds)
                throw new InvalidDataException("Invalid camera budget values.");
        }
        catch { _lock.Dispose(); throw; }
    }

    public CameraBudgetAvailability Availability
    {
        get
        {
            int used = _state.Version==5?_state.Sessions.Length-_state.PointerStartCount!.Value:
                _state.Version >= 3 ? _state.Sessions.Length - _state.Execution18StartCount!.Value : _state.Sessions.Length;
            int allowed = _state.Version==5?1:_state.Version == 4 ? 5 : _state.Version == 3 ? 3 : SessionLimitCount;
            int remaining = Math.Max(0, Math.Min(allowed - used, Math.Min(SessionLimitCount - _state.Sessions.Length,
                (int)((TotalLimitMilliseconds - _state.Sessions.Sum(r => r.ChargedMs)) / SessionLimitMilliseconds))));
            if (_state.Sessions.Any(r => !r.Released))
                return new(false, used, allowed, remaining, "Previous camera release needs verification. Camera stays off.");
            return remaining == 0
                ? new(false, used, allowed, 0, $"Test allowance used ({used}/{allowed} sessions). A new bounded authorization is needed.")
                : new(true, used, allowed, remaining, $"{remaining} authorized test session(s) remaining. Enable starts the timer; automatic stop at170 seconds.");
        }
    }

    public CameraBudgetState Read() => _state with { Sessions = _state.Sessions.ToArray() };

    public void AmendContinuation()
    {
        if (_state.Version >= 2) return;
        if (_state.Sessions.Any(row => !row.Released))
            throw new InvalidDataException("Cannot amend an active or unconfirmed reservation.");
        Commit(_state with { Version = 2, Amendment = ContinuationAmendment });
    }

    public void AmendLearning()
    {
        if (_state.Version >= 3) return;
        if (_state.Version != 2 || _state.Sessions.Any(row => !row.Released))
            throw new InvalidDataException("Cannot amend an incompatible or active camera budget.");
        Commit(_state with { Version = 3, Amendment = LearningAmendment, Execution18StartCount = _state.Sessions.Length });
    }

    public void AmendLearningTargets()
    {
        if (_state.Version == 4) return;
        if (_state.Version != 3 || _state.Sessions.Any(row => !row.Released))
            throw new InvalidDataException("Cannot extend an incompatible or unconfirmed camera budget.");
        // Original start count and every charge remain immutable: this adds two, never renews five.
        Commit(_state with { Version = 4, Amendment = LearningTargetsAmendment });
    }

    public void Reserve(Guid session)
    {
        if (!Availability.Available ||
            session == Guid.Empty || _state.Sessions.Any(row => !row.Released || row.Session == session) ||
            _state.Sessions.Length >= SessionLimitCount ||
            _state.Sessions.Sum(row => row.ChargedMs) + SessionLimitMilliseconds > TotalLimitMilliseconds)
            throw new InvalidDataException("The diagnostic camera budget is exhausted or a prior release is unconfirmed.");
        Commit(_state with { Sessions = [.. _state.Sessions, new(session, SessionLimitMilliseconds, false)] });
    }

    /// <summary>Call only following a separate explicit one-session pointer-only authorization.
    /// Never called by startup, preview, or Enable. Repeating this cannot replenish a start.</summary>
    public void AmendPointerQualification()
    {
        if(_state.Version==5)return;
        if(_state.Version!=4||_state.Execution18StartCount!=5||_state.Sessions.Length!=10||
            _state.Sessions.Any(r=>!r.Released)||_state.Sessions.Sum(r=>r.ChargedMs)+180_000>TotalLimitMilliseconds)
            throw new InvalidDataException("Pointer authorization cannot amend this ledger.");
        Commit(_state with{Version=5,Amendment=PointerAmendment,PointerStartCount=10});
    }

    public void ConfirmIndependentlyVerifiedRelease(CameraReleaseEvidence evidence)
    {
        if (!evidence.IsSufficient || !_state.Sessions.Any(r => r.Session == evidence.Session && !r.Released))
            throw new InvalidDataException("Independent release verification is incomplete.");
        // Manual recovery does not refund time, remove a row or grant another start.
        Complete(evidence.Session, SessionLimitMilliseconds);
    }

    public void Complete(Guid session, long elapsedMs)
    {
        if (elapsedMs < 0 || elapsedMs > SessionLimitMilliseconds ||
            !_state.Sessions.Any(row => row.Session == session && !row.Released))
            throw new InvalidDataException("The diagnostic camera reservation cannot be completed.");
        Commit(_state with { Sessions = _state.Sessions.Select(row =>
            row.Session == session ? new CameraBudgetEntry(session, elapsedMs, true) : row).ToArray() });
    }

    private void Commit(CameraBudgetState next)
    {
        string stage = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var file = new FileStream(stage, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(file, next, Schema);
                file.Flush(true);
            }
            File.Replace(stage, _path, null);
            _state = next;
        }
        finally { if (File.Exists(stage)) File.Delete(stage); }
    }

    private static void ValidatePath(string path)
    {
        for (var directory = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(path))!);
             directory is not null; directory = directory.Parent)
            if (directory.Exists && directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
                throw new UnauthorizedAccessException("Linked camera budget directories are unsupported.");
        foreach (string file in new[] { path, path + ".lock" })
            if (File.Exists(file) && File.GetAttributes(file).HasFlag(FileAttributes.ReparsePoint))
                throw new UnauthorizedAccessException("Linked camera budget files are unsupported.");
    }

    public void Dispose() => _lock.Dispose();
}
