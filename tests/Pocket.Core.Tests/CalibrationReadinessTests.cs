using System.Collections.Immutable;
using Pocket.Core;
using Pocket.Core.Tracking;
using Xunit;

namespace Pocket.Core.Tests;

public sealed class CalibrationReadinessTests
{
    private static readonly TrackingFrameSize Wide = new(1920, 1080);

    [Fact]
    public void FitsKnownFrameAndRejectsInvalidDimensions()
    {
        Assert.Equal(new DipRect(0, 35, 160, 90), CalibrationReadiness.Fit(Wide, 160, 160));
        Assert.False(CalibrationReadiness.Fit(new(0, 480), 160, 160).IsValid);
        Assert.False(CalibrationReadiness.Fit(Wide, double.NaN, 160).IsValid);
    }

    [Fact]
    public void RequiresDistinctContinuousFramesAndClearsOnLossOrStale()
    {
        var probe = new CalibrationReadiness();
        var id = Guid.NewGuid();
        for (int i = 0; i < 8; i++)
            Assert.Equal(CalibrationState.Stabilizing, probe.Observe(Sample(id, 0), null, false, .12).State);
        for (int seq = 1; seq <= 5; seq++)
            Assert.Equal(seq == 5 ? CalibrationState.Ready : CalibrationState.Stabilizing,
                probe.Observe(Sample(id, seq), null, false, .12).State);
        Assert.Equal(CalibrationState.Stale,
            probe.Observe(Sample(id, 5) with { HostObservationAge = TimeSpan.FromMilliseconds(250) }, null, false, .12).State);
        Assert.Equal(CalibrationState.Stabilizing, probe.Observe(Sample(id, 6), null, false, .12).State);
        Assert.Null(probe.Observe(Sample(id, 7) with { Observation = null, State = TrackingState.Lost }, null, false, .12).Point);
        Assert.Equal(CalibrationState.Stabilizing, probe.Observe(Sample(Guid.NewGuid(), 0), null, false, .12).State);
    }

    [Fact]
    public void MirrorsXOnlyAndRejectsWrongHandEdgesAndMalformedCoordinates()
    {
        var id = Guid.NewGuid();
        var hand = Sample(id, 0);
        Assert.Equal(new DipPoint(.25, .4), new CalibrationReadiness().Observe(hand, HandSide.Right, false, .12).Point);
        Assert.Equal(new DipPoint(.75, .4), new CalibrationReadiness().Observe(hand, HandSide.Right, true, .12).Point);
        Assert.Equal(CalibrationState.WrongHand, new CalibrationReadiness().Observe(hand, HandSide.Left, false, .12).State);
        Assert.Equal(CalibrationState.OutsideRegion, new CalibrationReadiness().Observe(hand, null, true, .3).State);
        var invalid = hand with { Observation = hand.Observation! with
            { Landmarks = ImmutableArray.CreateRange(Enumerable.Repeat(new ImageLandmark(double.NaN, .4, 0), 21)) } };
        Assert.Equal(CalibrationState.Waiting, new CalibrationReadiness().Observe(invalid, null, true, .12).State);
    }

    private static TrackingSnapshot Sample(Guid id, long sequence)
    {
        long capture = 100 + sequence * 100;
        var landmarks = ImmutableArray.CreateRange(Enumerable.Repeat(new ImageLandmark(.25, .4, 0), 21));
        return new(TrackingState.Observing, id, TrackingSource.Camera,
            new HandObservation(id, sequence, capture, capture + 20, landmarks, HandSide.Right, FrameSize: Wide),
            TimeSpan.FromMilliseconds(20), null, false, sequence + 1, 0);
    }
}
