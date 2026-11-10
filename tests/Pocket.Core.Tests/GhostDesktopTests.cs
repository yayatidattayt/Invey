using System.Collections.Immutable;
using Pocket.Core;
using Pocket.Core.Tracking;
using Xunit;
namespace Pocket.Core.Tests;

public sealed class GhostDesktopTests
{
    [Fact]
    public void PreviewCannotActAndOnlyControlledSourceCanArm()
    {
        long now=1000;var g=new GhostDesktop(()=>now);int commands=0;
        g.Register(Target("one",0,()=>commands++));
        foreach(var source in Enum.GetValues<GhostSourceKind>())
        {
            var id=g.Begin(source);g.Observe(new(id,0,now,new(50,50),source==GhostSourceKind.Controlled?GhostProvenance.Controlled:GhostProvenance.FullHand));
            Assert.False(g.Execute(id,0,GhostCommand.Pin));
            Assert.Equal(source==GhostSourceKind.Controlled,g.Arm());
            if(source==GhostSourceKind.Controlled){Assert.True(g.Execute(id,0,GhostCommand.Pin));Assert.Equal(1,commands);}
        }
    }
    [Theory]
    [InlineData(GhostProvenance.PartialHand)] [InlineData(GhostProvenance.FingerVisual)]
    public void VisualEvidenceMovesPreviewAndReleasesHold(GhostProvenance evidence)
    {
        long now=1000;int ended=0;var g=new GhostDesktop(()=>now);
        g.Register(Target("one",0,()=>{},end:()=>ended++));
        var id=g.Begin(GhostSourceKind.Controlled);g.Observe(new(id,0,now,new(50,50),GhostProvenance.Controlled));g.Arm();g.Press(id,0);
        now+=20;g.Observe(new(id,1,now,new(60,50),evidence));
        Assert.Equal(new DipPoint(60,50),g.Position);Assert.Null(g.ActiveTarget);Assert.Equal(1,ended);Assert.False(g.Press(id,1));
    }
    [Theory]
    [InlineData("duplicate")] [InlineData("order")] [InlineData("stale")] [InlineData("future")] [InlineData("nonfinite")]
    public void InvalidEventsReleaseAndLateSourceCannotRearm(string fault)
    {
        long now=1000;int ended=0;var g=new GhostDesktop(()=>now);g.Register(Target("one",0,()=>{},end:()=>ended++));
        var id=g.Begin(GhostSourceKind.Controlled);g.Observe(new(id,3,now,new(50,50),GhostProvenance.Controlled));g.Arm();g.Press(id,3);
        now+=20;
        Assert.False(g.Observe(new(id,fault=="duplicate"?3:4,fault=="order"?900:fault=="stale"?800:fault=="future"?2000:now,
            fault=="nonfinite"?new(double.NaN,50):new(60,50),GhostProvenance.Controlled)));
        Assert.Null(g.ActiveTarget);Assert.Equal(1,ended);g.Stop();g.Stop();Assert.False(g.Press(id,4));
        var replacement=g.Begin(GhostSourceKind.Controlled);Assert.NotEqual(id,replacement);Assert.False(g.Observe(new(id,99,now,new(50,50),GhostProvenance.Controlled)));
    }
    [Fact]
    public void TargetHitOrderFollowsStackingAndDisposalCancels()
    {
        long now=1000;string? hit=null;var g=new GhostDesktop(()=>now);
        var a=Target("alphabetically-last",10,()=>hit="back");var b=Target("first",0,()=>hit="front");
        g.Register(a);g.Register(b);Assert.Throws<InvalidOperationException>(()=>g.Register(b));
        var id=g.Begin(GhostSourceKind.Controlled);g.Observe(new(id,0,now,new(50,50),GhostProvenance.Controlled));g.Arm();
        g.Execute(id,0,GhostCommand.Pin);Assert.Equal("front",hit);
        g.Press(id,0);Assert.Equal("first",g.ActiveTarget);g.Unregister(b.Id,b.Lifetime);Assert.Null(g.ActiveTarget);
        Assert.Equal(GhostControlMode.Preview,g.Mode);
    }
    [Fact]
    public void ExpiryPauseAndTargetVisibilityInvalidateClaims()
    {
        long now=1000;bool visible=true;var g=new GhostDesktop(()=>now);
        var t=Target("one",0,()=>{});g.Register(t with { Geometry=()=>visible?new(new(0,0,100,100),0,0,1):null });
        var id=g.Begin(GhostSourceKind.Controlled);g.Observe(new(id,0,now,new(50,50),GhostProvenance.Controlled));g.Arm();g.Press(id,0);
        visible=false;g.Tick();Assert.Null(g.ActiveTarget);g.Pause();Assert.False(g.Arm());
        id=g.Begin(GhostSourceKind.Controlled);g.Observe(new(id,0,now,new(50,50),GhostProvenance.Controlled));now+=150;g.Tick();
        Assert.Null(g.Position);Assert.Equal(GhostControlMode.Lost,g.Mode);
    }
    private static GhostDesktopTarget Target(string id,int z,Action command,Action? end=null)=>new(id,Guid.NewGuid(),
        ()=>new(new(0,0,100,100),z,0,1),true,_=>true,_=>{},_=>end?.Invoke(),_=>{command();return true;},[GhostCommand.Pin]);

    [Theory]
    [InlineData(96,false)] [InlineData(120,false)] [InlineData(120,true)]
    public void TrackingMappingUsesPhysicalOriginAndCompatibleCorrection(int dpi,bool mirror)
    {
        var display=new CalibrationDisplay(2,-1920,-100,1920,1200,dpi);
        var context=new LearningContext("Recorded",LearningContext.HashDevice("test"),640,480,display,2,mirror,12,100);
        var metrics=new ErrorMetrics(9,.01,.01,.01,.01,.01,1,1,1);
        var model=new PersonalModel(Guid.NewGuid(),context,[.02,0,0],[-.01,0,0],metrics,metrics,9);
        var mapper=new GhostTrackingMapper();var baseline=new GhostTrackingMapper();var incompatible=new GhostTrackingMapper();
        var id=Guid.NewGuid();DipPoint? actual=null,original=null,mismatch=null;
        for(int i=0;i<8;i++)
        {
            var hand=new HandObservation(id,i,1000+i*20,1001+i*20,Enumerable.Repeat(new ImageLandmark(.5,.5,0),21).ToImmutableArray(),HandSide.Right,FrameSize:new(640,480));
            var s=new TrackingSnapshot(TrackingState.Observing,id,TrackingSource.Recorded,hand,TimeSpan.Zero,null,false,i+1,0);
            original=baseline.Map(s,context,null);actual=mapper.Map(s,context,model);
            mismatch=incompatible.Map(s,context,model with { Context=context with { DeviceHash=LearningContext.HashDevice("other") } });
        }
        Assert.NotNull(actual);Assert.Equal(original,mismatch);
        Assert.Equal(-1920+.52*1919,actual!.Value.X,6);Assert.Equal(-100+.49*1199,actual.Value.Y,6);
    }
}
