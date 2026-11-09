namespace Pocket.Core.Tracking;

/// <summary>One Euro tuning plus a one-observation large-jump gate. No extrapolation or dead zone.</summary>
public sealed class PrecisionPointFilter
{
    private readonly AdaptivePointFilter _filter = new(.7, 10, 1);
    private DipPoint? _accepted, _pending, _output;
    private long _time;
    private long? _pendingSince;
    public bool Uncertain => _pendingSince is { } since && _time-since>=80;
    private readonly Queue<(long Time, DipPoint Point)> _window = new();
    private int _moving;
    public string MotionState => _moving > 0 ? "Moving" : "Resting";
    public bool SpikePending => _pending.HasValue;
    public void Reset() { _filter.Reset(); _accepted = _pending = _output = null; _time = 0; _window.Clear(); _moving = 0; _pendingSince=null; }
    public DipPoint Apply(DipPoint point, long milliseconds)
    {
        if (!point.IsFinite || point.X is < 0 or > 1 || point.Y is < 0 or > 1)
            throw new ArgumentException("Expected a finite normalized point.");
        long interval=milliseconds-_time;
        if (_accepted is not { } previous || interval is <= 0 or > 250)
        { Reset(); _time = milliseconds; _accepted = point; return (_output = _filter.Apply(point, milliseconds)).Value; }
        _time = milliseconds;
        // One large discontinuity needs a second nearby measurement. A real sustained step is accepted
        // on that second measurement; an isolated spike never poisons the derivative estimate.
        double jump=Math.Clamp(.08*interval/40d,.08,.20);
        if (Distance(point, previous) > jump && (_pending is not { } pending || Distance(point, pending) > .04))
        { _pending = point; _pendingSince ??= milliseconds; return _output!.Value; }
        _pending = null; _pendingSince=null; _accepted = point;
        _window.Enqueue((milliseconds,point));
        while(_window.Count>10) _window.Dequeue();
        if(_window.Count>=6)
        {
            double t0=_window.Peek().Time, meanT=_window.Average(p=>(p.Time-t0)/1000d);
            double mx=_window.Average(p=>p.Point.X), my=_window.Average(p=>p.Point.Y);
            double denominator=_window.Sum(p=>Math.Pow((p.Time-t0)/1000d-meanT,2));
            double vx=_window.Sum(p=>((p.Time-t0)/1000d-meanT)*(p.Point.X-mx))/denominator;
            double vy=_window.Sum(p=>((p.Time-t0)/1000d-meanT)*(p.Point.Y-my))/denominator;
            double residual=Math.Sqrt(_window.Average(p=>Math.Pow(p.Point.X-mx-vx*((p.Time-t0)/1000d-meanT),2)+
                Math.Pow(p.Point.Y-my-vy*((p.Time-t0)/1000d-meanT),2)));
            double displacement=Math.Sqrt(vx*vx+vy*vy)*(milliseconds-t0)/1000d;
            if(displacement>2*residual+.001) _moving=4;
            else if(_moving>0) _moving--;
        }
        return (_output = _filter.Apply(point, milliseconds, _moving>0 ? 2 : .7)).Value;
    }
    private static double Distance(DipPoint a, DipPoint b) => Math.Sqrt(Math.Pow(a.X-b.X, 2) + Math.Pow(a.Y-b.Y, 2));
}
