namespace Pocket.Core;

public enum GhostSourceKind { Controlled, Recorded, Camera }
public enum GhostProvenance { Controlled, FullHand, PartialHand, FingerVisual }
public enum GhostCommand { Activate, Pin, Unpin, Hide }
public sealed record GhostPosition(Guid Session, long Sequence, long Timestamp, DipPoint Physical, GhostProvenance Evidence);
public sealed record GhostTargetGeometry(DipRect Bounds, int ZOrder, int Priority, long Window);
public sealed record GhostDesktopTarget(string Id, Guid Lifetime, Func<GhostTargetGeometry?> Geometry,
    bool Draggable, Func<DipPoint,bool> Begin, Action<DipPoint> Move, Action<bool> End,
    Func<GhostCommand,bool> Command, IReadOnlyCollection<GhostCommand> SupportedCommands);

/// <summary>One dispatcher-owned desktop pointer. Coordinates are physical virtual-desktop pixels.
/// Only an explicit controlled source can issue intent; camera/recorded sources are preview-only.</summary>
public sealed class GhostDesktop(Func<long> clock)
{
    public const int PreviewLifetimeMilliseconds=250;
    private readonly Dictionary<string,GhostDesktopTarget> _targets = [];
    private GhostDesktopTarget? _held;
    private Guid? _session;
    private long _sequence=-1, _timestamp=-1, _received;
    private GhostSourceKind _kind;
    private GhostProvenance _evidence;
    public GhostControlMode Mode { get; private set; }
    public DipPoint? Position { get; private set; }
    public Guid? Session => _session;
    public long Sequence => _sequence;
    public GhostSourceKind Source => _kind;
    public string? ActiveTarget => _held?.Id;
    public string? HoverTarget => Hit()?.Id;
    internal GhostDesktopTarget? ReachableTarget => Hit();
    public string Status { get; private set; } = "Pocket control off";
    public Func<long,DipPoint,bool>? WindowReachable { get; set; }
    public event Action? Changed;
    public Guid Begin(GhostSourceKind kind)
    {
        Stop(); _session=Guid.NewGuid(); _kind=kind; Mode=GhostControlMode.Preview;
        Status=kind==GhostSourceKind.Controlled ? "CONTROLLED · pointer preview only" : "Tracking · pointer preview only";
        Changed?.Invoke(); return _session.Value;
    }
    public void Register(GhostDesktopTarget target)
    {
        if(_targets.ContainsKey(target.Id)) throw new InvalidOperationException("Duplicate Ghost target.");
        _targets.Add(target.Id,target);
    }
    public void Unregister(string id, Guid lifetime)
    {
        if(!_targets.TryGetValue(id,out var target) || target.Lifetime!=lifetime) return;
        if(_held==target) Cancel("Target closed — released");
        _targets.Remove(id);
    }
    public IReadOnlyList<(string Id,GhostTargetGeometry Geometry)> Targets => _targets.Values
        .Select(t=>(t.Id,Geometry:t.Geometry())).Where(t=>t.Geometry is { Bounds.IsValid:true })
        .Select(t=>(t.Id,t.Geometry!)).ToArray();
    public bool Observe(GhostPosition observation)
    {
        if(observation.Session!=_session || Mode is GhostControlMode.Off or GhostControlMode.Paused) return false;
        long now=clock();
        if(observation.Sequence<=_sequence || observation.Timestamp<_timestamp || observation.Timestamp>now ||
            now-observation.Timestamp>=(_kind==GhostSourceKind.Controlled?150:PreviewLifetimeMilliseconds) || !observation.Physical.IsFinite || !Enum.IsDefined(observation.Evidence))
        { Lose("Invalid or stale source — released"); return false; }
        _sequence=observation.Sequence; _timestamp=observation.Timestamp; _received=now;
        _evidence=observation.Evidence; Position=observation.Physical;
        if(Mode==GhostControlMode.Lost) Mode=GhostControlMode.Preview;
        if(_evidence is GhostProvenance.PartialHand or GhostProvenance.FingerVisual)
            Cancel("Pointer preview only — gesture evidence lost");
        if(_held is { } held)
        {
            if(held.Geometry() is null) Cancel("Target unavailable — released");
            else if(held.Draggable) held.Move(observation.Physical);
        }
        Changed?.Invoke(); return true;
    }
    public bool Arm()
    {
        Tick();
        if(_kind!=GhostSourceKind.Controlled || _evidence!=GhostProvenance.Controlled ||
            _session is null || Position is null || Mode!=GhostControlMode.Preview) return false;
        Mode=GhostControlMode.Armed; Status="CONTROLLED · Pocket control armed"; Changed?.Invoke(); return true;
    }
    public bool Press(Guid session,long sequence)
    {
        if(!CanAct(session,sequence) || _held is not null) return false;
        var target=Hit();
        if(target is null) { Status="No reachable Pocket target under Ghost"; Changed?.Invoke(); return false; }
        if(!target.Begin(Position!.Value)) { Status="Target is busy or unavailable"; Changed?.Invoke(); return false; }
        _held=target; Mode=GhostControlMode.Holding; Status=target.Draggable?"Moving reference":"Reference selected"; Changed?.Invoke(); return true;
    }
    public bool Release(Guid session,long sequence)
    {
        if(!CanAct(session,sequence) || _held is not { } held) return false;
        bool activate=!held.Draggable && Hit()==held;
        _held=null; held.End(true); Mode=GhostControlMode.Armed; Status="Released";
        if(activate && held.SupportedCommands.Contains(GhostCommand.Activate))
            Status=held.Command(GhostCommand.Activate)?"Reference opened":"Could not open that reference";
        Changed?.Invoke(); return true;
    }
    public bool Execute(Guid session,long sequence,GhostCommand command)
    {
        if(!CanAct(session,sequence) || _held is not null || !Enum.IsDefined(command)) return false;
        var target=Hit();
        bool supported=target is not null&&target.SupportedCommands.Contains(command);
        bool result=supported&&target!.Command(command);
        Status=result?command+" complete":target is null?"No reachable Pocket target under Ghost":supported?"Target action failed":"That action is unavailable on this target";
        Changed?.Invoke(); return result;
    }
    private bool CanAct(Guid session,long sequence)
    {
        Tick(); return session==_session && sequence==_sequence && _kind==GhostSourceKind.Controlled &&
            _evidence==GhostProvenance.Controlled && Position.HasValue && Mode is GhostControlMode.Armed or GhostControlMode.Holding;
    }
    private GhostDesktopTarget? Hit() => Position is not { } p ? null : _targets.Values
        .Select(t=>(Target:t,Box:t.Geometry())).Where(t=>t.Box is { Bounds.IsValid:true } g && g.Bounds.Contains(p) &&
            (WindowReachable?.Invoke(g.Window,p) ?? true))
        .OrderBy(t=>t.Box!.ZOrder).ThenByDescending(t=>t.Box!.Priority).Select(t=>t.Target).FirstOrDefault();
    public IReadOnlyCollection<GhostCommand> AvailableCommands => Hit()?.SupportedCommands ?? Array.Empty<GhostCommand>();
    public void Tick()
    {
        if(_session.HasValue && Position.HasValue && (_kind==GhostSourceKind.Controlled?clock()-_received>=150:clock()-_timestamp>=PreviewLifetimeMilliseconds)) Lose("Tracking lost — released");
        if(_held is { } target && target.Geometry() is null) Cancel("Target unavailable — released");
    }
    public void Disarm() { Cancel("Pointer preview only"); Changed?.Invoke(); }
    public void Lose(string reason) { Cancel(reason); Mode=GhostControlMode.Lost; Position=null; Changed?.Invoke(); }
    public void Stop() { Cancel("Pocket control off"); Mode=GhostControlMode.Off; Position=null; _session=null; _sequence=_timestamp=-1; Changed?.Invoke(); }
    public void Pause() { Stop(); Mode=GhostControlMode.Paused; Status="Paused"; Changed?.Invoke(); }
    private void Cancel(string reason)
    {
        var target=_held; _held=null; target?.End(false);
        if(_session.HasValue) Mode=GhostControlMode.Preview;
        Status=reason;
    }
}
