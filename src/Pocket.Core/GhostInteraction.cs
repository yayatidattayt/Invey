namespace Pocket.Core;

public enum GhostControlMode { Off, Preview, Armed, Holding, Paused, Lost }
public enum GhostIntent { Move, Press, Release }

/// <summary>Dispatcher-owned adapter to the existing ownership reducer. Controlled sources only
/// until a separately qualified live gesture adapter is provided. No native input dependencies.</summary>
public sealed class GhostInteraction(InteractionState scene)
{
    private Guid? _source;
    private long _sequence = -1, _timestamp = -1;
    private readonly HashSet<string> _targets = [];
    public GhostControlMode Mode { get; private set; }
    public DipPoint? Position { get; private set; }
    public Guid BeginControlledSource()
    {
        Stop(); _source = Guid.NewGuid(); Mode = GhostControlMode.Preview;
        return _source.Value;
    }
    public bool Arm(Guid source)
    {
        if (_source != source || Mode != GhostControlMode.Preview) return false;
        Mode = GhostControlMode.Armed; return true;
    }
    public void Register(string id) { _ = scene.Target(id); _targets.Add(id); }
    public void Unregister(string id)
    {
        if (scene.Pointer(PointerOwner.Ghost).Target == id) Cancel(GhostControlMode.Preview);
        _targets.Remove(id);
    }
    public bool Submit(Guid source, long sequence, long timestamp, DipPoint position, GhostIntent intent,
        bool freshFullHandEvidence, long ageMilliseconds = 0)
    {
        if (source != _source || Mode is GhostControlMode.Off or GhostControlMode.Paused or GhostControlMode.Lost)
            return false;
        if (sequence <= _sequence || timestamp <= _timestamp || ageMilliseconds is < 0 or >= 150 ||
            !freshFullHandEvidence || !position.IsFinite || !scene.Bounds.Contains(position))
        { Cancel(GhostControlMode.Lost); return false; }
        _sequence = sequence; _timestamp = timestamp;
        Position = position;
        if (Mode == GhostControlMode.Preview) return intent == GhostIntent.Move;
        scene.Move(PointerOwner.Ghost, position);
        if (intent == GhostIntent.Press)
        {
            if (Mode == GhostControlMode.Holding) return false;
            string? target = _targets.OrderBy(id => id, StringComparer.Ordinal)
                .LastOrDefault(id => scene.Target(id).Contains(position));
            if (target is null || !scene.Press(PointerOwner.Ghost, target)) return false;
            Mode = GhostControlMode.Holding;
        }
        else if (intent == GhostIntent.Release)
        { scene.Release(PointerOwner.Ghost); Mode = GhostControlMode.Armed; }
        return true;
    }
    public void Pause() { Cancel(GhostControlMode.Paused); _source = null; }
    public void Stop() { Cancel(GhostControlMode.Off); _source = null; _sequence = _timestamp = -1; }
    private void Cancel(GhostControlMode mode)
    { scene.Cancel(PointerOwner.Ghost); Position = null; Mode = mode; }
}
