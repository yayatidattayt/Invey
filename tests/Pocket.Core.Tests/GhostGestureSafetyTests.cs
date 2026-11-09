using System.Collections.Immutable;
using Pocket.Core.Tracking;
using Xunit;

namespace Pocket.Core.Tests;
public class GhostGestureSafetyTests
{
    private static GhostGestureCalibration Calibration()=>GhostGestureCalibration.FromPracticeSamples(HandSide.Right,[.2,.21,.22],[.5,.51,.52])!;
    private static HandObservation Hand(Guid id,long seq,long time,bool pinch)
    {
        var p=Enumerable.Repeat(new ImageLandmark(.5,.5,0),21).ToArray();
        p[4]=new(pinch?.46:.4,.5,0);p[5]=new(.4,.5,0);p[17]=new(.6,.5,0);
        return new(id,seq,time,time,p.ToImmutableArray(),HandSide.Right,.98,false,new(640,480));
    }
    [Fact] public void PinchedEntryGapPauseAndOverlappingCalibrationFailClosed()
    {
        var a=new GhostGestureIntentAdapter();var id=Guid.NewGuid();a.Begin(id,Calibration());
        for(int i=0;i<10;i++)Assert.Equal(GhostGestureCommand.None,a.Observe(Hand(id,i,1000+i*40,true),1000+i*40).ProposedCommand);
        a.Observe(Hand(id,10,1400,false),1400);a.Observe(Hand(id,11,1440,true),1440);
        Assert.Equal(GhostGestureTransition.TrackingLost,a.Observe(Hand(id,12,1800,true),1800).Transition);
        a.Pause();Assert.Equal(GhostGestureTransition.Paused,a.Observe(Hand(id,13,1840,false),1840).Transition);
        Assert.Null(GhostGestureCalibration.FromPracticeSamples(HandSide.Right,[.2,.21,.4],[.45,.5,.6]));
    }
    [Theory] [InlineData("normal")] [InlineData("late-target")] [InlineData("moved-target")] [InlineData("covered")] [InlineData("wrong-hand")] [InlineData("context")]
    public void BridgeBindsTargetAtOnsetAndNeverGrantsCameraAuthority(string scenario)
    {
        long now=1000;int presses=0,ends=0;double moved=0;var box=new DipRect(0,0,100,100);
        bool visible=scenario!="late-target";var desktop=new GhostDesktop(()=>now);
        desktop.Register(new("card",Guid.NewGuid(),()=>visible?new(box,0,0,1):null,true,_=>{presses++;return true;},p=>moved=p.X,_=>ends++,_=>false,[]));
        var engine=desktop.Begin(GhostSourceKind.Controlled);var source=Guid.NewGuid();
        var context=new LearningContext("Recorded",LearningContext.HashDevice("fixture"),640,480,new(1,0,0,1920,1080,96),2,false,12,100);
        desktop.Observe(new(engine,0,now,new(50,50),GhostProvenance.Controlled));
        var bridge=GhostGestureCoordinator.ControlledReplay(desktop);bridge.Begin(source,context,Calibration());Assert.True(bridge.ArmReplay());
        void Sample(int seq,bool pinch){now=1000+seq*40;desktop.Observe(new(engine,seq+1,now,new(50+seq,50),GhostProvenance.Controlled));var h=Hand(source,seq,now,pinch);if(scenario=="wrong-hand"&&seq>0)h=h with{Handedness=HandSide.Left};bridge.Observe(h,scenario=="context"&&seq>0?context with{Mirror=true}:context,now);}
        Sample(0,false);Sample(1,true);
        if(scenario=="late-target")visible=true;
        if(scenario=="moved-target")box=new(1,0,100,100);
        if(scenario=="covered")desktop.WindowReachable=(_,_)=>false;
        for(int i=2;i<8;i++)Sample(i,true);
        Assert.Equal(scenario=="normal"?1:0,presses);
        if(scenario=="normal")Assert.True(moved>50);
        for(int i=8;i<15;i++)Sample(i,false);
        Assert.Null(desktop.ActiveTarget);Assert.False(bridge.LiveQualified);
        var production=new GhostGestureCoordinator(desktop);production.Begin(source,context,Calibration());Assert.False(production.ArmReplay());
    }
}
