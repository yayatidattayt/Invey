using System.Collections.Immutable;
using System.Text.Json;
using Pocket.Core.Tracking;
using Xunit;

namespace Pocket.Core.Tests;

public sealed class VisualPointerTests
{
    private static HandObservation Full(Guid id,long seq) => new(id,seq,1000+seq*20,1001+seq*20,
        Enumerable.Repeat(new ImageLandmark(.5,.5,0),21).ToImmutableArray(),HandSide.Right,FrameSize:new(640,480));
    private static HandObservation Visual(Guid id,long seq) => new(id,seq,1000+seq*20,1001+seq*20,[],HandSide.Right,
        FrameSize:new(640,480),ProtocolVersion:3,Visual:new(.51,.5,1080,2));

    [Fact]
    public void VisualHasProvenanceButNoModelPointsLearningOrGestureAuthority()
    {
        var s=new TrackingSession(); var id=s.Enable(TrackingSource.Recorded)!.Value; s.Accept(new TrackerReady(id));
        var pointer=new FingerPointer(); var practice=new CalibrationPractice(); var ready=new CalibrationReadiness();
        for(int i=0;i<5;i++) { s.Accept(Full(id,i)); pointer.Observe(s.Read(),HandSide.Right); }
        Assert.True(s.Accept(Visual(id,5))); var snapshot=s.Read(); var p=pointer.Observe(snapshot,HandSide.Right);
        Assert.Equal(TrackingState.Partial,snapshot.State); Assert.Equal(FingerPointerState.FingerVisual,p.State);
        Assert.NotNull(p.Position); Assert.False(p.ActionEligible); Assert.False(snapshot.Observation!.HasModelPoints);
        Assert.Null(snapshot.Observation.Point(8)); Assert.False(snapshot.Observation.Tracked);
        Assert.NotEqual(CalibrationState.Ready,ready.Observe(snapshot,HandSide.Right,false,.12).State);
        Assert.NotEqual(PracticeState.Held,practice.Update(snapshot,true,true));
        Assert.True(s.Accept(Full(id,6))); Assert.Equal(FingerPointerState.Stable,pointer.Observe(s.Read(),HandSide.Right).State);
        s.Stop(); Assert.Null(pointer.Observe(s.Read(),HandSide.Right).Position);
    }

    [Fact]
    public void RepeatedUiReadsCannotExtendVisualAnchorLifetime()
    {
        var id=Guid.NewGuid(); var p=new FingerPointer();
        TrackingSnapshot Snap(HandObservation h, int age=0) => new(TrackingState.Partial,id,TrackingSource.Recorded,h,
            TimeSpan.FromMilliseconds(age),null,false,1,0);
        for(int i=20;i<25;i++) p.Observe(Snap(Full(id,i)),null);
        var visual=Visual(id,25) with { Visual=new(.51,.5,1000,2) };
        Assert.NotNull(p.Observe(Snap(visual),null).Position);
        Assert.Null(p.Observe(Snap(visual,100),null).Position);
    }

    [Fact]
    public void WorkerMovementEpochDoesNotBypassThePointerSpikeGate()
    {
        var session=new TrackingSession(); var id=session.Enable(TrackingSource.Recorded)!.Value;
        session.Accept(new TrackerReady(id)); var p=new FingerPointer();
        for(int i=0;i<8;i++) { session.Accept(Full(id,i)); p.Observe(session.Read(),null); }
        session.Accept(Full(id,8) with { Landmarks=Enumerable.Repeat(new ImageLandmark(.9,.5,0),21).ToImmutableArray() });
        var snap=session.Read();
        Assert.Equal(TrackingBreak.Movement,snap.LastBreak);
        Assert.Equal(.5,p.Observe(snap,null).Position!.Value.X);
        session.Accept(Full(id,9)); Assert.Equal(.5,p.Observe(session.Read(),null).Position!.Value.X);
    }

    [Fact]
    public void ColdStaleChangedHandAndChangedGeometryVisualsAreHidden()
    {
        var id=Guid.NewGuid();
        TrackingSnapshot Snap(HandObservation h) => new(TrackingState.Partial,id,TrackingSource.Recorded,h,TimeSpan.Zero,null,false,1,0);
        Assert.Null(new FingerPointer().Observe(Snap(Visual(id,5)),null).Position);
        foreach(int variant in Enumerable.Range(0,5)) {
            var p=new FingerPointer(); for(int i=0;i<5;i++) p.Observe(Snap(Full(id,i)),null);
            var v=Visual(id,5); var snapshot=Snap(v);
            snapshot=variant switch {
                0=>snapshot with { HostObservationAge=TimeSpan.FromMilliseconds(151) },
                1=>Snap(v with { Handedness=HandSide.Left }),
                2=>Snap(v with { FrameSize=new(1280,720) }),
                3=>snapshot with { PointerContinuity=1 },
                _=>snapshot with { SessionId=Guid.NewGuid() }
            };
            Assert.Null(p.Observe(snapshot,null).Position);
        }
    }

    [Fact]
    public void V3StrictParsingRejectsForgedFullGeometryAndExpiredEvidence()
    {
        var id=Guid.NewGuid();
        byte[] Wire(int version=3,long anchor=1000,double residual=2,bool tracked=false) => JsonSerializer.SerializeToUtf8Bytes(new {
            v=version,kind="observation",session=id,seq=5,captureMonoMs=1100,resultMonoMs=1101,tracked,
            landmarks=tracked ? Enumerable.Repeat(new[]{.5,.5,0},21).ToArray() : [],handedness="right",
            frame=new{width=640,height=480},visual=new{x=.5,y=.5,anchorMonoMs=anchor,residual}
        });
        Assert.NotNull(Assert.IsType<HandObservation>(TrackingProtocol.Parse(Wire())).Visual);
        Assert.Throws<InvalidDataException>(()=>TrackingProtocol.Parse(Wire(version:2)));
        Assert.Throws<InvalidDataException>(()=>TrackingProtocol.Parse(Wire(anchor:499)));
        Assert.Throws<InvalidDataException>(()=>TrackingProtocol.Parse(Wire(residual:13)));
        Assert.Throws<InvalidDataException>(()=>TrackingProtocol.Parse(Wire(tracked:true)));
    }
}
