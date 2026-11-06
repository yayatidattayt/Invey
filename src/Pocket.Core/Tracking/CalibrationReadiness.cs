namespace Pocket.Core.Tracking;

public enum CalibrationState { Waiting, Stale, OutsideRegion, WrongHand, Ambiguous, Stabilizing, Ready }
public readonly record struct CalibrationSample(CalibrationState State, DipPoint? Point, TrackingFrameSize? Frame);

/// <summary>Session-local visual readiness. Never creates input commands or stores calibration data.</summary>
public sealed class CalibrationReadiness
{
    private Guid? _session;
    private TrackingFrameSize? _frame;
    private HandSide? _side;
    private long _captureMs;
    private long _continuity, _seen = -1;
    private int _consecutive;
    private DipPoint? _previous;
    private readonly Dictionary<string, int> _resets = new();
    private CalibrationSample _sample;
    public int Consecutive => _consecutive;
    public int MaxConsecutive { get; private set; }
    public int Fresh { get; private set; }
    public int ReadyEntries { get; private set; }
    public IReadOnlyDictionary<string, int> Resets => _resets;
    public void Invalidate() => Clear("CalibrationChanged", CalibrationState.Waiting);

    public CalibrationSample Observe(TrackingSnapshot snapshot, HandSide? selected, bool mirror, double inset)
    {
        if (snapshot.SessionId != _session)
        {
            Reset();
            _frame = null;
            _session = snapshot.SessionId;
            _continuity = snapshot.Continuity;
            _seen = -1;
        }
        if (snapshot.Continuity != _continuity)
        {
            Clear(snapshot.LastBreak.ToString(), CalibrationState.Waiting);
            _continuity = snapshot.Continuity;
        }
        if (snapshot.State == TrackingState.Stale || snapshot.HostObservationAge >= TrackingSession.ObservationLifetime)
            return Clear("Stale", CalibrationState.Stale);
        if (snapshot.State == TrackingState.Ambiguous)
            return Clear("MultipleHands", CalibrationState.Ambiguous);
        if (snapshot.State == TrackingState.Partial)
            return Clear("Partial", CalibrationState.OutsideRegion);
        if (snapshot.Observation is not { Tracked: true, FrameSize: { IsValid: true } frame } hand ||
            !hand.IsValid || hand.SessionId != snapshot.SessionId || snapshot.HostObservationAge is null ||
            snapshot.State != TrackingState.Observing)
            return Clear(snapshot.State.ToString(), CalibrationState.Waiting);
        if (snapshot.HostObservationAge.Value.TotalMilliseconds + (hand.ResultMonoMs - hand.CaptureMonoMs) >=
            TrackingSession.ObservationLifetime.TotalMilliseconds)
            return Clear("Stale", CalibrationState.Stale);
        if (hand.Sequence < _seen) return Clear("Reordered", CalibrationState.Waiting);
        if (hand.Sequence == _seen) return _sample;
        _seen = hand.Sequence;
        Fresh++;

        if (_frame != frame || _side != hand.Handedness)
        {
            Clear("HandOrFrameChanged", CalibrationState.Waiting);
            _frame = frame;
            _side = hand.Handedness;
        }
        if (hand.Handedness is null)
            return Clear("UnknownHand", CalibrationState.Ambiguous);
        if (selected is not null && hand.Handedness != selected)
            return Clear("WrongHand", CalibrationState.WrongHand);

        var tip = hand.Landmarks[8];
        var point = new DipPoint(mirror ? 1 - tip.X : tip.X, tip.Y);
        if (!double.IsFinite(inset) || inset is < 0 or >= .5 ||
            point.X < inset || point.X > 1 - inset || point.Y < inset || point.Y > 1 - inset)
            return _sample = Clear("OutsideRegion", CalibrationState.OutsideRegion) with { Point = point };

        if (_previous is { } p)
        {
            if (hand.CaptureMonoMs - _captureMs is <= 0 or > 220)
                Clear("FrameGap", CalibrationState.Waiting);
            else if (Math.Abs(point.X - p.X) > .14 || Math.Abs(point.Y - p.Y) > .14)
                Clear("Movement", CalibrationState.Waiting);
        }
        // Six distinct frames, with no gap over 220 ms or large fingertip jump.
        _consecutive++;
        MaxConsecutive = Math.Max(MaxConsecutive, _consecutive);
        if (_consecutive == 6) ReadyEntries++;
        _captureMs = hand.CaptureMonoMs;
        _previous = point;
        return _sample = new(_consecutive >= 6 ? CalibrationState.Ready : CalibrationState.Stabilizing, point, _frame);
    }

    public static DipRect Fit(TrackingFrameSize frame, double width, double height) =>
        frame.IsValid ? FitAspect(frame.Width, frame.Height, width, height) : default;

    public static DipRect FitAspect(int sourceWidth, int sourceHeight, double width, double height)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0 || !double.IsFinite(width) || !double.IsFinite(height) || width <= 0 || height <= 0)
            return default;
        double scale = Math.Min(width / sourceWidth, height / sourceHeight);
        double w = sourceWidth * scale, h = sourceHeight * scale;
        return new((width - w) / 2, (height - h) / 2, w, h);
    }

    private CalibrationSample Clear(string reason, CalibrationState state)
    {
        if (_consecutive > 0 || _sample.State != state || _sample.Point is not null)
            _resets[reason] = _resets.GetValueOrDefault(reason) + 1;
        Reset();
        return _sample = new(state, null, _frame);
    }

    private void Reset()
    {
        _consecutive = 0;
        _previous = null;
    }
}
