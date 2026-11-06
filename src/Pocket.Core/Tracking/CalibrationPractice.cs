namespace Pocket.Core.Tracking;

public enum PracticeState { OpenHand, Move, PinchCandidate, Held, Released, Canceled }

/// <summary>Timed, calibration-only pinch feedback. This reducer cannot dispatch input.</summary>
public sealed class CalibrationPractice
{
    private Guid? _session;
    private long _sequence = -1, _time, _continuity;
    private long? _since;
    private bool _armed;
    public PracticeState State { get; private set; }

    public PracticeState Update(TrackingSnapshot snapshot, bool ready, bool overTarget)
    {
        if (!ready || snapshot.State != TrackingState.Observing ||
            snapshot.Observation is not { Tracked: true, IsValid: true } hand || hand.SessionId != snapshot.SessionId ||
            snapshot.HostObservationAge is not { } age || age < TimeSpan.Zero ||
            age.TotalMilliseconds + (hand.ResultMonoMs - hand.CaptureMonoMs) >= 150)
            return Cancel();

        if (_session != hand.SessionId || snapshot.Continuity != _continuity)
        {
            Cancel();
            _session = hand.SessionId;
            _continuity = snapshot.Continuity;
            _sequence = -1;
        }
        if (hand.Sequence == _sequence) return State;
        if (hand.Sequence < _sequence) return Cancel();
        if (_sequence >= 0 && (hand.Sequence != _sequence + 1 || hand.CaptureMonoMs - _time is <= 0 or > 150))
            Cancel();
        _sequence = hand.Sequence;
        _time = hand.CaptureMonoMs;

        var width = Distance(hand, 5, 17);
        if (width < .001) return Cancel();
        var ratio = Distance(hand, 4, 8) / width;
        if (State == PracticeState.Held)
        {
            if (!Dwell(ratio >= .42, 80)) return State;
            _since = null;
            return State = PracticeState.Released;
        }
        if (!_armed)
        {
            _armed = Dwell(ratio >= .42, 80);
            if (_armed) _since = null;
            return State = _armed ? PracticeState.Move : PracticeState.OpenHand;
        }
        if (!overTarget || ratio > .28)
        {
            _since = null;
            return State = PracticeState.Move;
        }
        if (!Dwell(true, 100)) return State = PracticeState.PinchCandidate;
        _since = null;
        return State = PracticeState.Held;
    }

    private PracticeState Cancel()
    {
        _since = null;
        _armed = false;
        return State = State is PracticeState.Held or PracticeState.Canceled ? PracticeState.Canceled : PracticeState.OpenHand;
    }

    private bool Dwell(bool condition, int milliseconds)
    {
        _since = condition ? _since ?? _time : null;
        return _since is { } start && _time - start >= milliseconds;
    }

    private static double Distance(HandObservation hand, int a, int b)
    {
        var first = hand.Landmarks[a];
        var second = hand.Landmarks[b];
        double aspect = hand.FrameSize is { } size ? (double)size.Width / size.Height : 1;
        double x = (first.X - second.X) * aspect;
        double y = first.Y - second.Y;
        return Math.Sqrt(x * x + y * y);
    }
}
