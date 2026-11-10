using System.Collections.Immutable;
using Pocket.Core.Tracking;
using Xunit;

namespace Pocket.Core.Tests;

public sealed class GhostGestureIntentAdapterTests
{
    [Theory]
    [InlineData(250,false)]
    [InlineData(251,true)]
    public void Capture_gap_boundary_requires_a_new_open_hand_after_reset(int gap,bool resets)
    {
        var adapter=new GhostGestureIntentAdapter();var session=Guid.NewGuid();adapter.Begin(session,Calibration());
        Observe(adapter,session,0,1000,false);
        var next=Observe(adapter,session,1,1000+gap,true);
        Assert.Equal(resets?GhostGestureTransition.TrackingLost:GhostGestureTransition.PinchCandidate,next.Transition);
        if(resets){var blocked=Observe(adapter,session,2,1040+gap,true);Assert.Equal(GhostGestureTransition.Neutral,blocked.Transition);Assert.Equal("Open your hand before starting a new pinch.",blocked.Reason);Assert.False(adapter.IsHolding);}
        Assert.False(next.MayExecute);
    }

    [Fact]
    public void FullFreshSelectedHandProducesDwelledTransitionsButNeverDispatches()
    {
        var adapter=new GhostGestureIntentAdapter();var session=Guid.NewGuid();var calibration=Calibration();
        Assert.Equal(GhostGestureTransition.Neutral,adapter.Begin(session,calibration).Transition);
        Arm(adapter,"card:sample");
        Assert.Equal(GhostGestureTransition.Neutral,Observe(adapter,session,0,1000,false).Transition);
        Assert.Equal(GhostGestureTransition.PinchCandidate,Observe(adapter,session,1,1100,true).Transition);
        Assert.Equal(GhostGestureTransition.PinchCandidate,Observe(adapter,session,2,1250,true).Transition);
        var confirmed=Observe(adapter,session,3,1290,true);
        Assert.Equal(GhostGestureTransition.PinchConfirmed,confirmed.Transition);
        Assert.Equal(GhostGestureCommand.Press,confirmed.ProposedCommand);
        Assert.False(confirmed.MayExecute);Assert.Equal("card:sample",confirmed.Target?.Id);
        Assert.Equal(GhostGestureTransition.Hold,Observe(adapter,session,4,1330,true).Transition);
        Assert.Equal(GhostGestureTransition.ReleaseCandidate,Observe(adapter,session,5,1350,false).Transition);
        var released=Observe(adapter,session,6,1540,false);
        Assert.Equal(GhostGestureTransition.ReleaseConfirmed,released.Transition);
        Assert.Equal(GhostGestureCommand.Release,released.ProposedCommand);
        Assert.False(released.MayExecute);Assert.False(adapter.LiveActionsEnabled);Assert.False(adapter.IsHolding);
    }

    [Fact]
    public void AmbiguityStaleDataWrongHandAndReplacementReleaseAndDisarm()
    {
        var adapter=new GhostGestureIntentAdapter();var session=Guid.NewGuid();adapter.Begin(session,Calibration());
        Arm(adapter,"card");
        Assert.Equal(GhostGestureTransition.Neutral,Observe(adapter,session,0,1000,true).Transition);
        var ambiguous=adapter.Observe(Hand(session,1,1030,true,ambiguous:true),1030);
        Assert.Equal(GhostGestureTransition.Ambiguous,ambiguous.Transition);Assert.False(ambiguous.PocketArmed);Assert.False(adapter.IsHolding);
        Assert.False(Arm(adapter,"card",assert:false));
        var stale=adapter.Observe(Hand(session,2,1000,true),1200);
        Assert.Equal(GhostGestureTransition.TrackingLost,stale.Transition);
        var other=adapter.Observe(Hand(Guid.NewGuid(),3,1210,true),1210);
        Assert.Equal(GhostGestureTransition.TrackingLost,other.Transition);
    }

    [Fact]
    public void PartialVisualAndWrongHandObservationsCannotBecomePinchIntent()
    {
        var adapter=new GhostGestureIntentAdapter();var session=Guid.NewGuid();adapter.Begin(session,Calibration());Arm(adapter,"card");
        var partial=Hand(session,0,1000,true) with { ProtocolVersion=2,Partial=true,Landmarks=ImmutableArray<ImageLandmark>.Empty,
            PartialLandmarks=Enumerable.Repeat<ImageLandmark?>(null,21).ToImmutableArray() };
        Assert.Equal(GhostGestureTransition.TrackingLost,adapter.Observe(partial,1000).Transition);
        adapter.Begin(session,Calibration());Arm(adapter,"card");
        var wrong=Hand(session,1,1010,true) with { Handedness=HandSide.Left };
        var rejected=adapter.Observe(wrong,1010);
        Assert.Equal(GhostGestureTransition.TrackingLost,rejected.Transition);
        Assert.Equal(GhostGestureCommand.Release,rejected.ProposedCommand);Assert.False(rejected.MayExecute);
    }

    [Fact]
    public void PauseAndStopClearTargetAndHeldIntent()
    {
        var adapter=new GhostGestureIntentAdapter();var session=Guid.NewGuid();adapter.Begin(session,Calibration());
        Arm(adapter,"card");
        Observe(adapter,session,0,950,false);Observe(adapter,session,1,1000,true);
        Assert.Equal(GhostGestureTransition.PinchConfirmed,Observe(adapter,session,2,1200,true).Transition);
        Assert.Equal(GhostGestureTransition.Hold,Observe(adapter,session,3,1240,true).Transition);
        Assert.True(adapter.IsHolding);
        var paused=adapter.Pause();Assert.Equal(GhostGestureTransition.Paused,paused.Transition);Assert.False(adapter.IsHolding);Assert.False(paused.MayExecute);
        var stopped=adapter.Stop();Assert.Equal(GhostGestureTransition.Stopped,stopped.Transition);Assert.Equal(GhostGestureCommand.None,stopped.ProposedCommand);
    }

    [Fact]
    public void CalibrationRequiresSeparatedPracticeSamplesAndMinimumExamples()
    {
        Assert.Null(GhostGestureCalibration.FromPracticeSamples(HandSide.Left,[.2,.21],[.4,.45]));
        Assert.Null(GhostGestureCalibration.FromPracticeSamples(HandSide.Left,[.2,.21,.22],[.35,.36,.37]));
        Assert.NotNull(GhostGestureCalibration.FromPracticeSamples(HandSide.Left,[.2,.21,.22],[.5,.51,.52]));
    }

    [Fact]
    public void TargetLifetimeMustRemainRegisteredAndCannotBeReplacedDuringHold()
    {
        var adapter=new GhostGestureIntentAdapter();var session=Guid.NewGuid();adapter.Begin(session,Calibration());
        bool registered=true;var target=new GhostGestureTarget("card",Guid.NewGuid());
        Assert.True(adapter.ArmPocket(target,(id,lifetime)=>registered&&id==target.Id&&lifetime==target.Lifetime?target:null));
        Observe(adapter,session,0,950,false);Observe(adapter,session,1,1000,true);Observe(adapter,session,2,1050,true);
        Assert.Equal(GhostGestureTransition.PinchConfirmed,Observe(adapter,session,3,1200,true).Transition);
        Assert.False(adapter.ArmPocket(new("other",Guid.NewGuid()),(_,_)=>null));
        registered=false;
        var lost=Observe(adapter,session,4,1280,true);
        Assert.Equal(GhostGestureTransition.TrackingLost,lost.Transition);Assert.False(lost.PocketArmed);Assert.False(lost.MayExecute);
    }

    private static GhostGestureCalibration Calibration()=>GhostGestureCalibration.FromPracticeSamples(HandSide.Right,[.2,.21,.22],[.5,.51,.52])!;
    private static bool Arm(GhostGestureIntentAdapter adapter,string id,bool assert=true)
    {
        var target=new GhostGestureTarget(id,Guid.NewGuid());
        bool armed=adapter.ArmPocket(target,(targetId,lifetime)=>target.Id==targetId&&target.Lifetime==lifetime?target:null);
        if(assert)Assert.True(armed);
        return armed;
    }
    private static GhostGestureResult Observe(GhostGestureIntentAdapter adapter,Guid session,long sequence,long time,bool pinched)=>adapter.Observe(Hand(session,sequence,time,pinched),time);
    private static HandObservation Hand(Guid session,long sequence,long time,bool pinched,bool ambiguous=false)
    {
        var points=Enumerable.Repeat(new ImageLandmark(.5,.5,0),21).ToArray();
        points[4]=new(.5,.5,0);points[8]=new(pinched?.54:.60,.5,0);points[5]=new(.4,.5,0);points[17]=new(.6,.5,0);
        return new(session,sequence,time,time,ImmutableArray.Create(points),HandSide.Right,.98,false,new(640,480),ambiguous);
    }
}
