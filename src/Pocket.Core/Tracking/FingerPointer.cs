using System.Collections.Immutable;

namespace Pocket.Core.Tracking;

public enum FingerKind { Thumb, Index, Middle, Ring, Little }
public enum FingerEvidence { Observed, Partial, Predicted, Unavailable, Ambiguous, Visual }
public sealed record FingerSample(FingerKind Finger, FingerEvidence Evidence, ImageLandmark? Tip,
    bool ChainAvailable, double? BendRadians, bool? Stable, bool ActionEligible);
public enum FingerPointerState { Off, Searching, WrongHand, Ambiguous, Stabilizing, Stable, Partial, Lost, Stale, Paused, FingerVisual }
public sealed record FingerPointerSample(FingerPointerState State, DipPoint? Position,
    ImmutableArray<FingerSample> Fingers, bool ActionEligible = false);

/// <summary>Timestamp-aware One Euro filter in normalized image space.</summary>
public sealed class AdaptivePointFilter(double minCutoff = 1.5, double beta = 3, double derivativeCutoff = 1)
{
    private DipPoint? _raw, _filtered;
    private DipPoint _velocity;
    private long _time;

    public void Reset() { _raw = _filtered = null; _velocity = default; _time = 0; }

    public DipPoint Apply(DipPoint point, long milliseconds, double? minimumOverride = null)
    {
        if (!point.IsFinite || minCutoff <= 0 || beta < 0 || derivativeCutoff <= 0)
            throw new ArgumentException("Invalid smoothing input.");
        if (_raw is not { } previous || milliseconds - _time is <= 0 or > 250)
        {
            Reset();
            _time = milliseconds;
            _raw = _filtered = point;
            return point;
        }
        double dt = (milliseconds - _time) / 1000d;
        _time = milliseconds;
        var velocity = new DipPoint((point.X - previous.X) / dt, (point.Y - previous.Y) / dt);
        _velocity = Blend(_velocity, velocity, Alpha(derivativeCutoff, dt));
        double speed = Math.Sqrt(_velocity.X * _velocity.X + _velocity.Y * _velocity.Y);
        var result = Blend(_filtered!.Value, point, Alpha((minimumOverride ?? minCutoff) + beta * speed, dt));
        _raw = point;
        _filtered = result;
        return result;
    }

    private static double Alpha(double cutoff, double dt) => 1 / (1 + 1 / (2 * Math.PI * cutoff * dt));
    private static DipPoint Blend(DipPoint a, DipPoint b, double alpha) =>
        new(a.X + alpha * (b.X - a.X), a.Y + alpha * (b.Y - a.Y));
}

/// <summary>Preview eligibility is independent from calibration and action authority. No prediction or input injection.</summary>
public sealed class FingerPointer
{
    private readonly PrecisionPointFilter _filter = new();
    private readonly AdaptivePointFilter _legacyFilter = new();
    private readonly bool _legacy = Environment.GetEnvironmentVariable("INVEY_LEGACY_POINTER") == "1";
    private Guid? _session;
    private HandSide? _owner, _selection;
    private TrackingFrameSize? _frame;
    private long _sequence = -1, _capture, _continuity;
    private int _stable;
    private bool _fullSeen;
    private ImmutableArray<ImageLandmark?> _previous = [];
    private FingerPointerSample _sample = new(FingerPointerState.Off, null, []);

    public void Reset()
    {
        _filter.Reset();
        _legacyFilter.Reset();
        _sequence = -1;
        _capture = 0;
        _stable = 0;
        _fullSeen = false;
        _owner = null;
        _previous = [];
        _sample = new(FingerPointerState.Searching, null, [], false);
    }

    public FingerPointerSample Observe(TrackingSnapshot snapshot, HandSide? selected, bool paused = false)
    {
        if (_session != snapshot.SessionId || _selection != selected || _continuity != snapshot.PointerContinuity)
        {
            Reset();
            _session = snapshot.SessionId;
            _selection = selected;
            _continuity = snapshot.PointerContinuity;
        }
        if (paused) return Cancel(FingerPointerState.Paused);
        if (snapshot.State is TrackingState.Off or TrackingState.Stopped or TrackingState.Stopping or TrackingState.Unavailable)
            return Cancel(FingerPointerState.Off);
        if (snapshot.State is TrackingState.Starting or TrackingState.Ready && snapshot.Observation is null)
            return Cancel(FingerPointerState.Searching);
        if (snapshot.State == TrackingState.Ambiguous)
            return Cancel(FingerPointerState.Ambiguous);
        if (snapshot.State == TrackingState.Stale)
            return Cancel(FingerPointerState.Stale);
        if (snapshot.Observation is not { IsValid: true, FrameSize: { IsValid: true } frame } hand ||
            (!hand.HasModelPoints && hand.Visual is null) ||
            hand.SessionId != snapshot.SessionId || snapshot.HostObservationAge is not { } age || age < TimeSpan.Zero ||
            age.TotalMilliseconds + hand.ResultMonoMs - hand.CaptureMonoMs >= 150)
            return Cancel(FingerPointerState.Lost);
        if (hand.Handedness is null) return Cancel(FingerPointerState.Ambiguous);
        if (selected.HasValue && selected != hand.Handedness) return Cancel(FingerPointerState.WrongHand);
        // A repeated UI read must not extend the image evidence's anchor lifetime.
        if (hand.Visual is { } timedVisual && hand.ResultMonoMs - timedVisual.AnchorMonoMs + age.TotalMilliseconds > 600)
            return Cancel(FingerPointerState.Lost);
        if (hand.Sequence < _sequence) return Cancel(FingerPointerState.Lost);
        if (hand.Sequence == _sequence) return _sample;
        if (hand.Visual is { } visualEvidence && (!_fullSeen || _owner != hand.Handedness || _frame != frame ||
            hand.CaptureMonoMs - _capture is <= 0 or > 220 ||
            hand.ResultMonoMs - visualEvidence.AnchorMonoMs + age.TotalMilliseconds > 600))
            return Cancel(FingerPointerState.Lost);
        if ((_owner.HasValue && _owner != hand.Handedness) || (_frame.HasValue && _frame != frame) ||
            (_sequence >= 0 && hand.CaptureMonoMs - _capture is <= 0 or > 220))
            Reset();
        _owner = hand.Handedness;
        _frame = frame;
        if (hand.Tracked) _fullSeen = true;
        if (hand.Visual is { } visual)
        {
            // Preview only: no invented model chain, bend, pinch geometry or supervised sample.
            _sequence = hand.Sequence; _capture = hand.CaptureMonoMs;
            _previous = []; _stable++;
            var observed = new DipPoint(visual.X, visual.Y);
            var filtered = _legacy ? _legacyFilter.Apply(observed, hand.CaptureMonoMs) : _filter.Apply(observed, hand.CaptureMonoMs);
            var samples = Enum.GetValues<FingerKind>().Select(kind => new FingerSample(kind,
                kind == FingerKind.Index ? FingerEvidence.Visual : FingerEvidence.Unavailable,
                kind == FingerKind.Index ? new ImageLandmark(visual.X, visual.Y, 0) : null, false, null, null, false)).ToImmutableArray();
            return _sample = new(FingerPointerState.FingerVisual, _stable >= 4 && !_filter.Uncertain ? filtered : null, samples, false);
        }
        double dt = _capture == 0 ? 0 : (hand.CaptureMonoMs - _capture) / 1000d;
        var fingers = ImmutableArray.CreateBuilder<FingerSample>(5);
        foreach (var kind in Enum.GetValues<FingerKind>())
        {
            int first = 1 + (int)kind * 4;
            var chain = Enumerable.Range(first, 4).Select(hand.Point).ToArray();
            var tip = chain[3];
            bool complete = chain.All(p => p.HasValue);
            double? bend = complete ? Bend(chain[1]!.Value, chain[2]!.Value, chain[3]!.Value,
                (double)frame.Width / frame.Height) : null;
            bool? stable = tip.HasValue && dt > 0 && !_previous.IsDefaultOrEmpty && _previous[first + 3] is { } previous
                ? Distance(tip.Value, previous, (double)frame.Width / frame.Height) / dt < .5 : null;
            var evidence = tip is null ? FingerEvidence.Unavailable : hand.Partial || !complete ? FingerEvidence.Partial : FingerEvidence.Observed;
            fingers.Add(new(kind, evidence, tip, complete && bend.HasValue, bend, stable,
                hand.Tracked && !hand.Partial && complete && bend.HasValue));
        }
        _previous = Enumerable.Range(0, 21).Select(hand.Point).ToImmutableArray();
        _sequence = hand.Sequence;
        _capture = hand.CaptureMonoMs;
        if (hand.Point(8) is not { } index) return Cancel(FingerPointerState.Lost, fingers.ToImmutable());
        if (_legacy && _sample.Position is { } old && (Math.Abs(old.X - index.X) > .20 || Math.Abs(old.Y - index.Y) > .20))
        {
            _filter.Reset();
            _legacyFilter.Reset();
            _stable = 0;
        }
        var point = _legacy ? _legacyFilter.Apply(new(index.X, index.Y), hand.CaptureMonoMs) :
            _filter.Apply(new(index.X, index.Y), hand.CaptureMonoMs);
        _stable++;
        bool eligible = _stable >= 4 && (_legacy || !_filter.SpikePending) && hand.Tracked && !hand.Partial && fingers.All(f => f.ActionEligible);
        return _sample = new(_stable < 4 || !_legacy && _filter.Uncertain ? FingerPointerState.Stabilizing : hand.Partial ? FingerPointerState.Partial :
            FingerPointerState.Stable, _stable < 4 || !_legacy && _filter.Uncertain ? null : point, fingers.MoveToImmutable(),
            eligible);
    }

    private FingerPointerSample Cancel(FingerPointerState state, ImmutableArray<FingerSample> fingers = default)
    {
        Reset();
        var missing = fingers.IsDefault ? Enum.GetValues<FingerKind>().Select(kind => new FingerSample(kind,
            state == FingerPointerState.Ambiguous ? FingerEvidence.Ambiguous : FingerEvidence.Unavailable,
            null, false, null, null, false)).ToImmutableArray() : fingers;
        return _sample = new(state, null, missing, false);
    }

    private static double Distance(ImageLandmark a, ImageLandmark b, double aspect)
    {
        double x = (a.X - b.X) * aspect, y = a.Y - b.Y;
        return Math.Sqrt(x * x + y * y);
    }

    private static double? Bend(ImageLandmark a, ImageLandmark b, ImageLandmark c, double aspect)
    {
        double ax = (b.X - a.X) * aspect, ay = b.Y - a.Y;
        double bx = (c.X - b.X) * aspect, by = c.Y - b.Y;
        double lengths = Math.Sqrt((ax * ax + ay * ay) * (bx * bx + by * by));
        return lengths < .000004 ? null : Math.Acos(Math.Clamp((ax * bx + ay * by) / lengths, -1, 1));
    }
}
