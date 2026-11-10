using System.Collections.Immutable;
using Pocket.Core.Tracking;
using Xunit;

namespace Pocket.Core.Tests;
public class PreviewRecoveryTests
{
    private sealed class Clock : TimeProvider
    {
        public long Now;
        public override long TimestampFrequency => 1000;
        public override long GetTimestamp() => Now;
    }
    private static readonly LearningContext Context = new("Camera", LearningContext.HashDevice("replay"), 640, 480,
        new(1, 0, 0, 1920, 1080, 96), 2, false, 12, 100);
    private static TrackingSnapshot Frame(Guid id, int seq, long capture, long continuity = 0, HandSide side = HandSide.Right) =>
        new(TrackingState.Observing, id, TrackingSource.Camera,
            new(id, seq, capture, capture + 20, Enumerable.Repeat(new ImageLandmark(.5,.5,0),21).ToImmutableArray(), side, .98, false, new(640,480)),
            TimeSpan.Zero, null, false, seq+1, 0, PointerContinuity: continuity);

    [Fact]
    public void One_missing_ten_hertz_frame_preserves_claim_but_real_loss_requires_reacquisition()
    {
        var mapper = new GhostTrackingMapper(); var id = Guid.NewGuid();
        for (int i=0;i<4;i++) mapper.Map(Frame(id,i,1000+i*100),Context,null);
        Assert.NotNull(mapper.Map(Frame(id,5,1500),Context,null)); // One omitted frame; gap200ms.
        Assert.Null(mapper.Map(Frame(id,6,1600) with { State=TrackingState.Lost, Observation=null },Context,null));
        for(int i=7;i<10;i++) Assert.Null(mapper.Map(Frame(id,i,1700+(i-7)*100,1),Context,null));
        Assert.NotNull(mapper.Map(Frame(id,10,2000,1),Context,null));
        Assert.Null(mapper.Map(Frame(id,11,2100,2,HandSide.Left),Context,null));
        Assert.Equal("Selected hand not detected",mapper.Status);
        Assert.Null(mapper.Map(Frame(id,12,2400,3),Context,null)); // Beyond preview freshness continuity.
    }
    [Fact]
    public void Reason_entries_and_durations_are_distinct_from_repeated_polls()
    {
        var clock=new Clock(); var mapper=new GhostTrackingMapper(clock); var id=Guid.NewGuid();
        var lost=Frame(id,0,1000) with { State=TrackingState.Lost,Observation=null };
        mapper.Map(lost,Context,null);clock.Now=40;mapper.Map(lost,Context,null);clock.Now=100;
        Assert.Equal(2,mapper.UiRejectionPolls["No hand detected"]);
        Assert.Equal(1,mapper.ReasonEntries["No hand detected"]);
        Assert.Equal(100,mapper.ReasonMilliseconds["No hand detected"]);
        mapper.Map(lost with { State=TrackingState.Stale },Context,null);
        clock.Now=130; Assert.Equal(30,mapper.ReasonMilliseconds["Tracking observation expired"]);
    }
    [Theory]
    [InlineData(TrackingState.Lost,"No hand detected")]
    [InlineData(TrackingState.Stale,"Tracking observation expired")]
    [InlineData(TrackingState.Ambiguous,"Multiple hands detected")]
    public void Missing_observation_is_not_misreported_as_missing_user_calibration(TrackingState state,string expected)
    {
        var mapper=new GhostTrackingMapper();
        Assert.Null(mapper.Map(Frame(Guid.NewGuid(),0,1000) with { State=state, Observation=null },null,null));
        Assert.Equal(expected,mapper.Status);
    }
}
