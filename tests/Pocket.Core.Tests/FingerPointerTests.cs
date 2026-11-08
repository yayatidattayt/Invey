using System.Collections.Immutable;
using System.Text.Json;
using Pocket.Core;
using Pocket.Core.Tracking;
using Xunit;

namespace Pocket.Core.Tests;

public sealed class FingerPointerTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private static readonly Guid Session = Guid.NewGuid();

    private static HandObservation Hand(long sequence, bool partial = false, double x = .5)
    {
        var points = ImmutableArray.CreateBuilder<ImageLandmark>(21);
        points.Add(new(.5, .8, 0));
        for (int finger = 0; finger < 5; finger++)
            for (int joint = 0; joint < 4; joint++)
                points.Add(new(x + (finger - 1) * .04, .65 - joint * .04, 0));
        var full = points.MoveToImmutable();
        return partial
            ? new(Session, sequence, 1000 + sequence * 20, 1005 + sequence * 20, [], HandSide.Right, .9, true,
                new(640, 480), PartialLandmarks: full.Select((p, i) => i == 0 ? (ImageLandmark?)null : p).ToImmutableArray(),
                ProtocolVersion: 2)
            : new(Session, sequence, 1000 + sequence * 20, 1005 + sequence * 20, full, HandSide.Right, .9,
                FrameSize: new(640, 480));
    }

    private static TrackingSnapshot Snapshot(HandObservation hand) => new(hand.Partial ? TrackingState.Partial : TrackingState.Observing,
        Session, TrackingSource.Recorded, hand, TimeSpan.Zero, null, false, hand.Sequence + 1, 0, LastSequence: hand.Sequence);

    [Fact]
    public void PartialWristPreservesFiveFingerTipsWithoutActionAuthority()
    {
        var pointer = new FingerPointer();
        FingerPointerSample sample = new(FingerPointerState.Off, null, []);
        for (int i = 0; i < 8; i++) sample = pointer.Observe(Snapshot(Hand(i, partial: true)), HandSide.Right);
        Assert.Equal(FingerPointerState.Partial, sample.State);
        Assert.NotNull(sample.Position);
        Assert.Equal(5, sample.Fingers.Length);
        Assert.All(sample.Fingers, finger =>
        {
            Assert.Equal(FingerEvidence.Partial, finger.Evidence);
            Assert.NotNull(finger.Tip);
            Assert.True(finger.ChainAvailable);
            Assert.False(finger.ActionEligible);
        });
        Assert.False(sample.ActionEligible);
        var readiness = new CalibrationReadiness();
        var practice = new CalibrationPractice();
        Assert.NotEqual(CalibrationState.Ready, readiness.Observe(Snapshot(Hand(9, true)), null, false, .12).State);
        Assert.NotEqual(PracticeState.Held, practice.Update(Snapshot(Hand(9, true)), true, true));
    }

    [Fact]
    public void V2ProtocolRetainsIndexWhenWristIsAbsentAndRejectsInvalidVersions()
    {
        var hand = Hand(0, true);
        object?[] points = hand.PartialLandmarks.Select(p => p is { } value ? (object)new[] { value.X, value.Y, value.Z } : null).ToArray();
        byte[] Wire(int version) => JsonSerializer.SerializeToUtf8Bytes(new
        {
            v = version, kind = "observation", session = Session, seq = 0, captureMonoMs = 1000, resultMonoMs = 1005,
            tracked = false, landmarks = Array.Empty<object>(), partial = true, points, handedness = "right",
            frame = new { width = 640, height = 480 }
        });
        var parsed = Assert.IsType<HandObservation>(TrackingProtocol.Parse(Wire(2)));
        Assert.Null(parsed.Point(0));
        Assert.NotNull(parsed.Point(8));
        Assert.False(parsed.Tracked);
        Assert.True(parsed.IsValid);
        Assert.Throws<InvalidDataException>(() => TrackingProtocol.Parse(Wire(1)));
        Assert.Throws<InvalidDataException>(() => TrackingProtocol.Parse(Wire(3)));
        points[8] = new[] { 1.1, .5, 0 };
        Assert.Throws<InvalidDataException>(() => TrackingProtocol.Parse(Wire(2)));
    }

    [Fact]
    public void SessionKeepsPartialPreviewButLossClearsIt()
    {
        var state = new TrackingSession();
        Guid id = state.Enable(TrackingSource.Recorded)!.Value;
        Assert.True(state.Accept(new TrackerReady(id)));
        var hand = Hand(0, true) with { SessionId = id };
        Assert.True(state.Accept(hand));
        Assert.Equal(TrackingState.Partial, state.Read().State);
        Assert.NotNull(state.Read().Observation?.Point(8));
        Assert.True(state.Accept(new HandObservation(id, 1, 1020, 1025, [])));
        Assert.Null(state.Read().Observation);
    }

    [Fact]
    public void CoalescedValidPreviewFramesStayStableWhileHostSafetyEpochDoesNotChange()
    {
        var pointer = new FingerPointer();
        for (int i = 0; i < 6; i++) pointer.Observe(Snapshot(Hand(i)), null);
        var next = pointer.Observe(Snapshot(Hand(7)), null);
        Assert.Equal(FingerPointerState.Stable, next.State);
        Assert.NotNull(next.Position);
    }

    [Fact]
    public void CoalescedAmbiguityCannotRetainStablePointerOwnership()
    {
        var session = new TrackingSession();
        Guid id = session.Enable(TrackingSource.Recorded)!.Value;
        session.Accept(new TrackerReady(id));
        var pointer = new FingerPointer();
        for (int i = 0; i < 6; i++)
        {
            session.Accept(Hand(i) with { SessionId = id });
            pointer.Observe(session.Read(), null);
        }
        session.Accept(new HandObservation(id, 6, 1120, 1121, [], Ambiguous: true));
        session.Accept(Hand(7, true) with { SessionId = id });
        var latest = pointer.Observe(session.Read(), null);
        Assert.Equal(FingerPointerState.Stabilizing, latest.State);
        Assert.Null(latest.Position);
        Assert.False(latest.ActionEligible);
    }

    [Fact]
    public void LossAndHandChangeRequireFreshStabilization()
    {
        var pointer = new FingerPointer();
        for (int i = 0; i < 8; i++) pointer.Observe(Snapshot(Hand(i)), HandSide.Right);
        var lost = Snapshot(Hand(8)) with { State = TrackingState.Lost, Observation = null };
        Assert.Null(pointer.Observe(lost, HandSide.Right).Position);
        Assert.Null(pointer.Observe(Snapshot(Hand(9)), HandSide.Right).Position);
        Assert.Equal(FingerPointerState.WrongHand, pointer.Observe(Snapshot(Hand(10)), HandSide.Left).State);
        Assert.Null(pointer.Observe(Snapshot(Hand(11)), HandSide.Right).Position);
    }

    [Fact]
    public void TwoHundredNegativeOpportunitiesNeverCreateAHold()
    {
        var pointer = new FingerPointer();
        var practice = new CalibrationPractice();
        for (int i = 0; i < 200; i++)
        {
            var hand = Hand(i, partial: i % 5 == 0);
            var snapshot = Snapshot(hand);
            snapshot = (i % 5) switch
            {
                1 => snapshot with { State = TrackingState.Ambiguous, Observation = null },
                2 => snapshot with { HostObservationAge = TimeSpan.FromMilliseconds(200) },
                3 => snapshot with { State = TrackingState.Lost, Observation = null },
                4 => snapshot with { Observation = hand with { Handedness = null, HandednessScore = null } },
                _ => snapshot
            };
            var sample = pointer.Observe(snapshot, HandSide.Right);
            Assert.False(sample.ActionEligible);
            Assert.NotEqual(PracticeState.Held, practice.Update(snapshot, sample.ActionEligible, true));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    public void StraightFingerBendIsRotationInvariant(double scale)
    {
        var pointer = new FingerPointer();
        var hand = Hand(0);
        var points = hand.Landmarks.Select(p => new ImageLandmark(.5 - (p.Y - .5) / scale,
            .5 + (p.X - .5) * (640d / 480) / scale, p.Z)).ToImmutableArray();
        var sample = pointer.Observe(Snapshot(hand with { Landmarks = points }), null);
        Assert.All(sample.Fingers, finger => Assert.InRange(finger.BendRadians!.Value, 0, .00001));
    }

    [Fact]
    public void FilteringExperimentReportsSyntheticJitterLagAndProcessingCost()
    {
        var rows = new List<object>();
        foreach (string mode in new[] { "none", "fixed", "one-euro" })
        {
            var filter = new AdaptivePointFilter(beta: mode == "fixed" ? 0 : 3);
            var errors = new List<double>();
            double position = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
            {
                double x = .5 + (i % 2 == 0 ? .008 : -.008);
                position = mode == "none" ? x : filter.Apply(new(x, .5), i * 20).X;
                if (i > 30) errors.Add((position - .5) * (position - .5));
            }
            int crossing = -1;
            for (int i = 200; i < 250; i++)
            {
                position = mode == "none" ? .8 : filter.Apply(new(.8, .5), i * 20).X;
                if (crossing < 0 && position >= .77) crossing = (i - 200) * 20;
            }
            watch.Stop();
            Assert.True(crossing >= 0);
            rows.Add(new { Mode = mode, NormalizedRmsJitter = Math.Sqrt(errors.Average()),
                Step90PercentMs = crossing, ProcessingMsFor250 = watch.Elapsed.TotalMilliseconds });
        }
        output.WriteLine(JsonSerializer.Serialize(new { Source = "synthetic-50Hz", Rows = rows }));
    }

    [Fact]
    public void AdaptiveFilterReducesDeterministicStationaryJitterWithoutIgnoringMovement()
    {
        var filter = new AdaptivePointFilter();
        var raw = new List<double>();
        var smoothed = new List<double>();
        for (int i = 0; i < 120; i++)
        {
            double noise = i % 2 == 0 ? .008 : -.008;
            var point = filter.Apply(new(.5 + noise, .5), i * 20);
            if (i > 20) { raw.Add(Math.Abs(noise)); smoothed.Add(Math.Abs(point.X - .5)); }
        }
        Assert.True(smoothed.Average() < raw.Average() * .5);
        double moved = .5;
        for (int i = 120; i < 140; i++) moved = filter.Apply(new(.8, .5), i * 20).X;
        Assert.InRange(moved, .79, .8);
        filter.Reset();
        Assert.Equal(new DipPoint(.2, .4), filter.Apply(new(.2, .4), 5000));
    }
}
