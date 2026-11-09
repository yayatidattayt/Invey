using System.Collections.Immutable;
using Pocket.Core.Tracking;
using Xunit;
namespace Pocket.Core.Tests;
public class SlowCameraPreviewTests
{
    [Fact] public void MeasuredTenHertzCadenceKeepsPreviewWhileGestureFreshnessStaysStrict()
    {
        var mapper=new GhostTrackingMapper();var id=Guid.NewGuid();var context=new LearningContext("Camera",LearningContext.HashDevice("cadence"),640,480,new(1,0,0,1920,1080,96),2,false,12,100);
        TrackingSnapshot Snapshot(int i,int age)
        {
            var h=new HandObservation(id,i,1000+i*100,1050+i*100,Enumerable.Repeat(new ImageLandmark(.5,.5,0),21).ToImmutableArray(),HandSide.Right,.98,false,new(640,480));
            return new(TrackingState.Observing,id,TrackingSource.Camera,h,TimeSpan.FromMilliseconds(age),null,false,i+1,0);
        }
        for(int i=0;i<6;i++)mapper.Map(Snapshot(i,0),context,null);
        Assert.NotNull(mapper.Map(Snapshot(6,0),context,null));
        // With50ms processing plus110ms UI age, the previous150ms preview gate reset the filter.
        Assert.NotNull(mapper.Map(Snapshot(6,110),context,null));
        var strict=new FingerPointer();for(int i=0;i<6;i++)strict.Observe(Snapshot(i,0),HandSide.Right);
        Assert.Null(strict.Observe(Snapshot(6,110),HandSide.Right).Position);
        Assert.Null(mapper.Map(Snapshot(6,200),context,null));
        Assert.Null(mapper.Map(Snapshot(7,0) with{State=TrackingState.Lost,Observation=null},context,null));
    }
    [Fact] public void PreviewInterpolationNeverExtrapolatesAndResetDoesNotBridgeSources()
    {
        var motion=new GhostPreviewMotion();var id=Guid.NewGuid();
        Assert.Equal(new DipPoint(0,0),motion.Present(id,0,new(0,0),0));
        Assert.Equal(new DipPoint(0,0),motion.Present(id,1,new(100,50),100));
        Assert.Equal(new DipPoint(50,25),motion.Present(id,1,new(100,50),125));
        Assert.Equal(new DipPoint(100,50),motion.Present(id,1,new(100,50),300));
        motion.Reset();Assert.Equal(new DipPoint(900,700),motion.Present(Guid.NewGuid(),0,new(900,700),301));
    }
    [Fact] public void PreviewExpiresFromCaptureAndCannotArmOrExtendItself()
    {
        long now=1000;var desktop=new GhostDesktop(()=>now);var id=desktop.Begin(GhostSourceKind.Camera);
        Assert.True(desktop.Observe(new(id,0,950,new(40,50),GhostProvenance.FullHand)));Assert.False(desktop.Arm());
        now=1199;desktop.Tick();Assert.NotNull(desktop.Position);
        now=1200;desktop.Tick();Assert.Null(desktop.Position);Assert.Equal(GhostControlMode.Lost,desktop.Mode);
    }
    [Fact] public void NormalMotionAtTenHertzIsNotMistakenForRepeatedFastSpikes()
    {
        var filter=new PrecisionPointFilter();filter.Apply(new(.2,.5),1000);
        var p=filter.Apply(new(.3,.5),1100);Assert.True(p.X>.2);Assert.False(filter.SpikePending);
        filter.Apply(new(.95,.5),1200);Assert.True(filter.SpikePending);
    }
}
