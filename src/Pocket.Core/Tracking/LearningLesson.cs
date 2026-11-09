namespace Pocket.Core.Tracking;

public enum LessonPhase { Idle, Baseline, Teaching, Validation, Complete }
public enum LearningFeedback { Correct, MissedPinch, FalsePinch, MissedRelease, TrackingLoss, WrongHand, WrongDirection }
public sealed class LearningLesson
{
    private readonly List<TargetExample> _baseline = [], _training = [], _validation = [];
    private readonly List<DipPoint> _stable = [];
    private Guid? _session;
    private long _sequence = -1, _capture = -1, _continuity;
    private int _index;
    public LearningContext? Context { get; private set; }
    public LessonPhase Phase { get; private set; }
    public int StableCount => _stable.Count;
    public bool CanConfirm => Phase is LessonPhase.Baseline or LessonPhase.Teaching or LessonPhase.Validation && _stable.Count >= 8;
    public int Step => _index + 1;
    public string Reason { get; private set; } = "Enable learning and select one hand in Calibration.";
    public TargetExample[] Training => _training.ToArray();
    public TargetExample[] Validation => _validation.ToArray();
    public TargetExample[] Baseline => _baseline.ToArray();
    public DipPoint? Target => Phase switch
    {
        LessonPhase.Baseline => Grid(.25, .5, .75),
        LessonPhase.Teaching => Grid(.12, .5, .88),
        LessonPhase.Validation => Grid(.27, .51, .73),
        _ => null
    };
    private DipPoint Grid(double a, double b, double c)
    {
        double[] p = [a, b, c]; return new(p[_index % 3], p[_index / 3]);
    }
    public void Begin(LearningContext context, Guid session)
    {
        if (!context.IsValid || session == Guid.Empty) throw new ArgumentException("Select one hand and a valid display.");
        Cancel(); Context = context; _session = session; Phase = LessonPhase.Baseline; Reason = "Match the target and hold steady.";
    }
    public void Cancel()
    {
        _baseline.Clear(); _training.Clear(); _validation.Clear(); _stable.Clear();
        Context = null; _session = null; _sequence = _capture = -1; _index = 0; Phase = LessonPhase.Idle;
        Reason = "Lesson canceled. Accepted model unchanged.";
    }
    public void Observe(TrackingSnapshot snapshot, LearningContext? context, DipPoint? mapped)
    {
        if (Phase == LessonPhase.Idle) return;
        if (snapshot.SessionId != _session || context != Context)
        { Cancel(); Reason = "Session or geometry changed. Start a fresh lesson."; return; }
        if (Phase == LessonPhase.Complete) return;
        var h = snapshot.Observation;
        bool valid = snapshot.State == TrackingState.Observing && h is { Tracked: true, IsValid: true, Partial: false, Ambiguous: false } &&
            h.SessionId == _session && h.FrameSize is { } frame && frame.Width == Context!.FrameWidth && frame.Height == Context.FrameHeight &&
            snapshot.Source?.ToString() == Context.Source && h.Handedness == (Context!.Hand == 1 ? HandSide.Left : HandSide.Right) &&
            snapshot.HostObservationAge is { } age && age >= TimeSpan.Zero && age.TotalMilliseconds + h.ResultMonoMs - h.CaptureMonoMs < 150 &&
            mapped is { } p && PersonalTrainer.Unit(p);
        if (!valid) { _stable.Clear(); Reason = "Need fresh full selected-hand observations; loss/partial/ambiguity cannot teach."; return; }
        if (h!.Sequence == _sequence) return;
        if (h.Sequence < _sequence) { _stable.Clear(); Reason = "Out-of-order observation rejected."; return; }
        bool gap = _sequence >= 0 && (snapshot.Continuity != _continuity || h.CaptureMonoMs - _capture is <= 0 or > 150);
        _sequence = h.Sequence; _capture = h.CaptureMonoMs; _continuity = snapshot.Continuity;
        if (gap || _stable.Any(x => PersonalTrainer.Distance(x, mapped!.Value) > .012)) _stable.Clear();
        _stable.Add(mapped!.Value);
        if (_stable.Count > 8) _stable.RemoveAt(0);
        Reason = CanConfirm ? "Steady. Confirm only if your finger is intentionally at the shown target." : "Hold steady for eight fresh observations.";
    }
    public bool Confirm()
    {
        if (!CanConfirm || Target is not { } target || Context is null) return false;
        var observed = new DipPoint(_stable.Average(x => x.X), _stable.Average(x => x.Y));
        if (PersonalTrainer.Distance(observed, target) > .12)
        { _stable.Clear(); Reason = "Target mismatch exceeds the small correction range. Check mirror/direction; retry."; return false; }
        var row = new TargetExample(Guid.NewGuid(), observed, target, Context);
        (Phase == LessonPhase.Baseline ? _baseline : Phase == LessonPhase.Teaching ? _training : _validation).Add(row);
        Advance(); return true;
    }
    public void Retry() { _stable.Clear(); Reason = "Target retried. No label added."; }
    public void Skip() { if (Phase is LessonPhase.Baseline or LessonPhase.Teaching or LessonPhase.Validation) Advance(); }
    private void Advance()
    {
        _stable.Clear(); _index++;
        if (_index == 9)
        {
            _index = 0; Phase = Phase == LessonPhase.Baseline ? LessonPhase.Teaching :
                Phase == LessonPhase.Teaching ? LessonPhase.Validation : LessonPhase.Complete;
        }
        Reason = Phase == LessonPhase.Complete ? "Independent targets collected. Evaluate the candidate." : "Match the next target. Skipped targets do not count as labels.";
    }
}
