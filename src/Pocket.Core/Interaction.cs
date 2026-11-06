namespace Pocket.Core;

public readonly record struct DipPoint(double X, double Y)
{
    public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y);
}
public readonly record struct ScreenPoint(double X, double Y);
public readonly record struct DipRect(double X, double Y, double Width, double Height)
{
    public bool IsValid => double.IsFinite(X) && double.IsFinite(Y) && double.IsFinite(Width) && double.IsFinite(Height) && Width > 0 && Height > 0;
    public bool Contains(DipPoint p) => p.IsFinite && p.X >= X && p.X < X + Width && p.Y >= Y && p.Y < Y + Height;
    public DipRect Clamp(DipRect bounds)
    {
        if (!IsValid || !bounds.IsValid) throw new ArgumentOutOfRangeException(nameof(bounds));
        var width = Math.Min(Width, bounds.Width);
        var height = Math.Min(Height, bounds.Height);
        return new(Math.Clamp(X, bounds.X, bounds.X + bounds.Width - width), Math.Clamp(Y, bounds.Y, bounds.Y + bounds.Height - height), width, height);
    }
}
public sealed record DisplayTransform
{
    public ScreenPoint Origin { get; }
    public double Dpi { get; }
    public DisplayTransform(ScreenPoint origin, double dpi)
    {
        if (!double.IsFinite(origin.X) || !double.IsFinite(origin.Y) || !double.IsFinite(dpi) || dpi <= 0) throw new ArgumentOutOfRangeException(nameof(dpi));
        Origin = origin; Dpi = dpi;
    }
    public DipPoint ToDip(ScreenPoint p) => new((p.X-Origin.X)*96/Dpi, (p.Y-Origin.Y)*96/Dpi);
    public ScreenPoint ToScreen(DipPoint p) => new(Origin.X+p.X*Dpi/96, Origin.Y+p.Y*Dpi/96);
}
public enum PointerOwner { Mouse, Ghost }
public enum PointerPhase { Idle, Pressed, Dragging }
public sealed record PointerState(DipPoint Position, string? Target, PointerPhase Phase, DipPoint Offset);

/// <summary>Dispatcher-owned pure state; independent pointers and per-target claims.</summary>
public sealed class InteractionState
{
    private readonly Dictionary<string, DipRect> _targets;
    private readonly Dictionary<PointerOwner, PointerState> _pointers = new()
    {
        [PointerOwner.Mouse] = new(new(0,0), null, PointerPhase.Idle, new(0,0)),
        [PointerOwner.Ghost] = new(new(0,0), null, PointerPhase.Idle, new(0,0))
    };
    public DipRect Bounds { get; }
    public InteractionState(DipRect bounds, IEnumerable<KeyValuePair<string, DipRect>> targets)
    {
        if (!bounds.IsValid) throw new ArgumentOutOfRangeException(nameof(bounds));
        Bounds = bounds; _targets = targets.ToDictionary(x => x.Key, x => x.Value.Clamp(bounds));
    }
    public PointerState Pointer(PointerOwner owner) => _pointers[owner];
    public DipRect Target(string id) => _targets[id];
    public bool Move(PointerOwner owner, DipPoint position)
    {
        if (!position.IsFinite) return false;
        position = new(Math.Clamp(position.X, Bounds.X, Bounds.X+Bounds.Width), Math.Clamp(position.Y, Bounds.Y, Bounds.Y+Bounds.Height));
        var state = _pointers[owner];
        if (state.Target is { } id)
        {
            var box = _targets[id];
            _targets[id] = (box with { X=position.X-state.Offset.X, Y=position.Y-state.Offset.Y }).Clamp(Bounds);
            state = state with { Phase=PointerPhase.Dragging };
        }
        _pointers[owner] = state with { Position=position }; return true;
    }
    public bool Press(PointerOwner owner, string id)
    {
        var state = _pointers[owner];
        if (state.Target != null || !_targets.TryGetValue(id,out var box) || !box.Contains(state.Position) || _pointers.Values.Any(p=>p.Target==id)) return false;
        _pointers[owner] = state with { Target=id, Phase=PointerPhase.Pressed, Offset=new(state.Position.X-box.X,state.Position.Y-box.Y) }; return true;
    }
    public void Release(PointerOwner owner) => _pointers[owner] = _pointers[owner] with { Target=null, Phase=PointerPhase.Idle };
    public void Cancel(PointerOwner owner) => Release(owner);

    public void SetTarget(string id, DipRect bounds)
    {
        if (_pointers.Values.Any(state => state.Target == id))
            throw new InvalidOperationException("Release the target before replacing it.");
        _targets[id] = bounds.Clamp(Bounds);
    }

    public bool Resize(PointerOwner owner, string id, DipRect bounds)
    {
        if (!bounds.IsValid || !_targets.ContainsKey(id) ||
            _pointers.Any(pair => pair.Key != owner && pair.Value.Target == id)) return false;
        _targets[id] = bounds.Clamp(Bounds);
        return true;
    }

    public void RemoveTarget(string id)
    {
        foreach (var owner in Enum.GetValues<PointerOwner>())
            if (_pointers[owner].Target == id) Cancel(owner);
        _targets.Remove(id);
    }
}
