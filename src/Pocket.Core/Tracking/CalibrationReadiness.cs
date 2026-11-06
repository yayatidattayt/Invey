namespace Pocket.Core.Tracking;

public enum CalibrationState { Waiting, Stale, OutsideRegion, WrongHand, Stabilizing, Ready }
public readonly record struct CalibrationSample(CalibrationState State, DipPoint? Point, TrackingFrameSize? Frame);

/// <summary>Session-local visual readiness. Never creates input commands or stores calibration data.</summary>
public sealed class CalibrationReadiness
{
    private Guid? _session;
    private TrackingFrameSize? _frame;
    private HandSide? _side;
    private long _sequence = -1, _captureMs;
    private int _consecutive;
    private DipPoint? _previous;

    public CalibrationSample Observe(TrackingSnapshot snapshot, HandSide? selected, bool mirror, double inset)
    {
        if (snapshot.SessionId != _session)
        {
            Reset();
            _frame = null;
            _session = snapshot.SessionId;
        }
        if (snapshot.State == TrackingState.Stale || snapshot.HostObservationAge >= TrackingSession.ObservationLifetime)
            return Clear(CalibrationState.Stale);
        if (snapshot.State == TrackingState.Partial)
            return Clear(CalibrationState.OutsideRegion);
        if (snapshot.Observation is not { Tracked: true, FrameSize: { IsValid: true } frame } hand ||
            !hand.IsValid || hand.SessionId != snapshot.SessionId || snapshot.HostObservationAge is null)
            return Clear(CalibrationState.Waiting);

        if (_frame != frame || _side != hand.Handedness)
        {
            Reset();
            _frame = frame;
            _side = hand.Handedness;
        }
        if (selected is not null && hand.Handedness != selected)
            return Clear(CalibrationState.WrongHand);

        var tip = hand.Landmarks[8];
        var point = new DipPoint(mirror ? 1 - tip.X : tip.X, tip.Y);
        if (!double.IsFinite(inset) || inset is < 0 or >= .5 ||
            point.X < inset || point.X > 1 - inset || point.Y < inset || point.Y > 1 - inset)
            return Clear(CalibrationState.OutsideRegion) with { Point = point };

        if (hand.Sequence > _sequence)
        {
            // Six distinct frames, with no gap over 220 ms or large fingertip jump.
            bool continuous = _previous is { } p && hand.CaptureMonoMs - _captureMs is > 0 and <= 220 &&
                Math.Abs(point.X - p.X) <= .14 && Math.Abs(point.Y - p.Y) <= .14;
            _consecutive = continuous ? Math.Min(6, _consecutive + 1) : 1;
            _sequence = hand.Sequence;
            _captureMs = hand.CaptureMonoMs;
            _previous = point;
        }
        return new(_consecutive >= 6 ? CalibrationState.Ready : CalibrationState.Stabilizing, point, _frame);
    }

    public static DipRect Fit(TrackingFrameSize frame, double width, double height)
    {
        if (!frame.IsValid || !double.IsFinite(width) || !double.IsFinite(height) || width <= 0 || height <= 0)
            return default;
        double scale = Math.Min(width / frame.Width, height / frame.Height);
        double w = frame.Width * scale, h = frame.Height * scale;
        return new((width - w) / 2, (height - h) / 2, w, h);
    }

    private CalibrationSample Clear(CalibrationState state)
    {
        Reset();
        return new(state, null, _frame);
    }

    private void Reset()
    {
        _sequence = -1;
        _consecutive = 0;
        _previous = null;
    }
}
