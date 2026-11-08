using System.Text.Json.Serialization;
namespace Pocket.Core.Tracking;

// Aggregate, UI-observed quality; never a claim about every worker/camera frame.
public sealed record LearningSummary(long Observations, long Fresh, long Partial, long Ambiguous, long Lost,
    long Stale, int Correct, int MissedPinch, int FalsePinch, int MissedRelease, int LossFeedback, int WrongHand = 0, int WrongDirection = 0)
{
    [JsonIgnore] public bool IsValid => new[] { Observations, Fresh, Partial, Ambiguous, Lost, Stale,
        Correct, MissedPinch, FalsePinch, MissedRelease, LossFeedback, WrongHand, WrongDirection }.All(x => x is >= 0 and <= 10000000);
}
public sealed class LearningQuality
{
    private Guid? _session; private long _seen = -1; private TrackingState? _state;
    public LearningSummary Summary { get; private set; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    public void Observe(TrackingSnapshot snapshot)
    {
        if (_session != snapshot.SessionId) { _session = snapshot.SessionId; _seen = -1; _state = null; }
        bool distinct = snapshot.LastSequence > _seen || snapshot.State != _state;
        if (!distinct) return;
        _seen = Math.Max(_seen, snapshot.LastSequence); _state = snapshot.State;
        var s = Summary;
        Summary = s with { Observations = Math.Min(10000000, s.Observations + 1),
            Fresh = s.Fresh + (snapshot.State == TrackingState.Observing ? 1 : 0),
            Partial = s.Partial + (snapshot.State == TrackingState.Partial ? 1 : 0),
            Ambiguous = s.Ambiguous + (snapshot.State == TrackingState.Ambiguous ? 1 : 0),
            Lost = s.Lost + (snapshot.State == TrackingState.Lost ? 1 : 0),
            Stale = s.Stale + (snapshot.State == TrackingState.Stale ? 1 : 0) };
    }
    public void Feedback(LearningFeedback feedback)
    {
        var s = Summary;
        Summary = s with { Correct = s.Correct + (feedback == LearningFeedback.Correct ? 1 : 0),
            MissedPinch = s.MissedPinch + (feedback == LearningFeedback.MissedPinch ? 1 : 0),
            FalsePinch = s.FalsePinch + (feedback == LearningFeedback.FalsePinch ? 1 : 0),
            MissedRelease = s.MissedRelease + (feedback == LearningFeedback.MissedRelease ? 1 : 0),
            LossFeedback = s.LossFeedback + (feedback == LearningFeedback.TrackingLoss ? 1 : 0),
            WrongHand = s.WrongHand + (feedback == LearningFeedback.WrongHand ? 1 : 0),
            WrongDirection = s.WrongDirection + (feedback == LearningFeedback.WrongDirection ? 1 : 0) };
    }
}
