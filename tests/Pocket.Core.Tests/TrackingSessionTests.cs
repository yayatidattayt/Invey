using System.Collections.Immutable;
using System.Diagnostics;
using Pocket.Core.Tracking;
using Xunit;
using Xunit.Abstractions;

namespace Pocket.Core.Tests;

public class TrackingSessionTests(ITestOutputHelper output)
{
    private sealed class TestClock : TimeProvider
    {
        private long _now;
        public override long TimestampFrequency => 1000;
        public override long GetTimestamp() => _now;
        public void Advance(int milliseconds) => _now += milliseconds;
    }

    private static readonly ImmutableArray<ImageLandmark> Points =
        Enumerable.Repeat(new ImageLandmark(.25, .75, -.1), 21).ToImmutableArray();

    private static HandObservation Sample(Guid id, long sequence = 1, long capture = 900_000, long result = 900_010) =>
        new(id, sequence, capture, result, Points, HandSide.Right, .8);

    private static Guid Ready(TrackingSession session)
    {
        Guid id = Assert.IsType<Guid>(session.Enable(TrackingSource.Simulation));
        Assert.True(session.Accept(new TrackerReady(id)));
        return id;
    }

    [Fact]
    public void OffRejectsUnsolicitedDataAndConcurrentEnableCreatesOnlyOneSession()
    {
        var session = new TrackingSession(new TestClock());
        Assert.Equal(TrackingState.Off, session.Read().State);
        Assert.False(session.Accept(new TrackerReady(Guid.NewGuid())));
        Assert.Null(session.Read().Observation);
        var ids = new Guid?[16];
        Parallel.For(0, ids.Length, i => ids[i] = session.Enable(TrackingSource.Simulation));
        Guid id = Assert.Single(ids, value => value.HasValue)!.Value;
        Assert.False(session.Accept(Sample(id)));
        Assert.Equal(TrackingState.Starting, session.Read().State);
    }

    [Fact]
    public void StopDuringInitializationAndRestartCannotAcceptLateCallbacks()
    {
        var session = new TrackingSession(new TestClock());
        Guid old = session.Enable(TrackingSource.Simulation)!.Value;
        Assert.True(session.Stop());
        Assert.False(session.Stop());
        Assert.False(session.Accept(new TrackerReady(old)));
        Assert.False(session.Accept(Sample(old)));
        Assert.Null(session.Enable(TrackingSource.Simulation));
        Assert.False(session.Accept(new TrackerStopped(old)));
        Assert.Null(session.Enable(TrackingSource.Simulation));
        Assert.True(session.ConfirmWorkerExit(old));
        Assert.Equal(TrackingState.Stopped, session.Read().State);

        Guid fresh = Ready(session);
        Assert.NotEqual(old, fresh);
        Assert.False(session.Accept(Sample(old, sequence: 1000)));
        Assert.False(session.ConfirmWorkerExit(old));
        Assert.True(session.Accept(Sample(fresh)));
        Assert.Equal(fresh, session.Read().Observation!.SessionId);
        Assert.True(session.Stop());
        Assert.Null(session.Read().Observation);
        Assert.False(session.Accept(Sample(fresh, sequence: 2)));
    }

    [Fact]
    public void InvalidAndOutOfOrderFramesCannotReplaceLatestOrRefreshAge()
    {
        var clock = new TestClock();
        var session = new TrackingSession(clock);
        Guid id = Ready(session);
        Assert.True(session.Accept(Sample(id, sequence: 4)));
        clock.Advance(140);
        Assert.False(session.Accept(Sample(id, sequence: 4)));
        Assert.False(session.Accept(Sample(id, 5, 899_990, 900_011)));
        Assert.False(session.Accept(Sample(id, 5, 900_000, 900_009)));
        Assert.False(session.Accept(Sample(id, 5, 900_020, 900_400)));
        Assert.False(session.Accept(Sample(id, 5) with { Landmarks = Points.SetItem(0, new(double.NaN, 0, 0)) }));
        Assert.Equal(4, session.Read().Observation!.Sequence);
        Assert.Equal(TimeSpan.FromMilliseconds(140), session.Read().HostObservationAge);
        clock.Advance(110);
        Assert.Equal(TrackingState.Stale, session.Read().State);
        Assert.Null(session.Read().Observation);
        Assert.True(session.Accept(Sample(id, 6)));
    }

    [Fact]
    public void NoHandInvalidatesObservationImmediatelyWithoutWaitingForHeartbeat()
    {
        var clock = new TestClock();
        var session = new TrackingSession(clock);
        Guid id = Ready(session);
        Assert.True(session.Accept(Sample(id)));
        Assert.True(session.Accept(new HandObservation(id, 2, 900_020, 900_030, [])));
        var lost = session.Read();
        Assert.Equal(TrackingState.Lost, lost.State);
        Assert.Null(lost.Observation);
        Assert.Equal(2, lost.AcceptedObservations);
        clock.Advance(TrackingSession.ObservationLifetime.Milliseconds);
        Assert.True(session.Accept(new TrackerHealth(id)));
        Assert.Equal(TrackingState.Stale, session.Read().State);
        Assert.Equal(2, session.Read().AcceptedObservations);
        Assert.True(session.Accept(Sample(id, 3, 900_040, 900_050)));
        Assert.Equal(TrackingState.Observing, session.Read().State);
    }

    [Fact]
    public void StartupAndHealthDeadlinesRequireVerifiedCleanupBeforeEnable()
    {
        var clock = new TestClock();
        var session = new TrackingSession(clock);
        Guid id = session.Enable(TrackingSource.Simulation)!.Value;
        clock.Advance(5000);
        Assert.False(session.Accept(new TrackerReady(id)));
        Assert.Equal(TrackingFailure.Timeout, session.Read().Failure);
        clock.Advance(2000);
        Assert.True(session.Read().CleanupUnconfirmed);
        Assert.Null(session.Enable(TrackingSource.Simulation));
        Assert.True(session.ConfirmWorkerExit(id));
        Assert.Equal(TrackingState.Unavailable, session.Read().State);

        Guid next = Ready(session);
        Assert.True(session.Accept(Sample(next)));
        clock.Advance(2900);
        Assert.True(session.Accept(new TrackerHealth(next)));
        Assert.Equal(TrackingState.Stale, session.Read().State);
        clock.Advance(3000);
        Assert.Equal(TrackingState.Stopping, session.Read().State);
        Assert.False(session.Accept(new TrackerHealth(next)));
        Assert.False(session.Accept(new TrackerStopped(next)));
        Assert.True(session.ConfirmWorkerExit(next));
    }

    [Theory]
    [InlineData(false, TrackingFailure.NoFrames)]
    [InlineData(true, TrackingFailure.StreamInterrupted)]
    public void CameraTimeoutDistinguishesFirstFrameFromInterruptedDelivery(bool observed, TrackingFailure failure)
    {
        var clock = new TestClock();
        var session = new TrackingSession(clock);
        Guid id = session.Enable(TrackingSource.Camera)!.Value;
        Assert.True(session.Accept(new TrackerReady(id)));
        if (observed) Assert.True(session.Accept(Sample(id)));
        clock.Advance((int)TrackingSession.HealthDeadline.TotalMilliseconds);
        Assert.Equal(failure, session.Read().Failure);
        Assert.Null(session.Read().Observation);
        Assert.Null(session.Enable(TrackingSource.Camera));
        Assert.True(session.ConfirmWorkerExit(id));
    }

    [Theory]
    [InlineData(TrackingFailure.NoDevice)]
    [InlineData(TrackingFailure.PermissionDenied)]
    [InlineData(TrackingFailure.DeviceBusy)]
    [InlineData(TrackingFailure.DeviceLost)]
    [InlineData(TrackingFailure.UnsupportedFormat)]
    [InlineData(TrackingFailure.InvalidModel)]
    [InlineData(TrackingFailure.RuntimeFailure)]
    public void SourceFaultClearsObservationAndOnlyExplicitRestartCanResume(TrackingFailure failure)
    {
        var session = new TrackingSession(new TestClock());
        Guid id = Ready(session);
        Assert.True(session.Accept(Sample(id)));
        Assert.True(session.Accept(new TrackerError(id, failure)));
        Assert.Null(session.Read().Observation);
        Assert.Equal(failure, session.Read().Failure);
        Assert.Null(session.Enable(TrackingSource.Simulation));
        Assert.False(session.Accept(new TrackerStopped(id)));
        Assert.True(session.ConfirmWorkerExit(id));
        Assert.Equal(TrackingState.Unavailable, session.Read().State);
        Assert.Null(session.Read().SessionId);
        Assert.NotEqual(id, Ready(session));
    }

    [Fact]
    public void ProtocolFailureAndUnexpectedExitHaveExplicitDifferentEvidence()
    {
        var session = new TrackingSession(new TestClock());
        Guid first = Ready(session);
        Assert.True(session.ReportProtocolFailure(first));
        Assert.False(session.Accept(Sample(first)));
        Assert.True(session.ConfirmWorkerExit(first));
        Assert.Equal(TrackingFailure.ProtocolFailure, session.Read().Failure);

        Guid second = Ready(session);
        Assert.True(session.Accept(Sample(second)));
        Assert.True(session.ConfirmWorkerExit(second));
        Assert.Equal(TrackingFailure.WorkerExited, session.Read().Failure);
        Assert.Null(session.Read().Observation);
    }

    [Fact]
    public void SlowConsumerReceivesLatestOfTenThousandObservations()
    {
        const int count = 10_000;
        var clock = new TestClock();
        var session = new TrackingSession(clock);
        Guid id = Ready(session);
        var elapsed = Stopwatch.StartNew();
        for (int i = 0; i < count; i++)
        {
            clock.Advance(16);
            Assert.True(session.Accept(Sample(id, i, i * 16, i * 16 + 10)));
        }
        var snapshot = session.Read();
        elapsed.Stop();
        Assert.Equal(count, snapshot.AcceptedObservations);
        Assert.Equal(count - 1, snapshot.SupersededObservations);
        Assert.Equal(count - 1, snapshot.Observation!.Sequence);
        Assert.Equal(TimeSpan.Zero, snapshot.HostObservationAge);
        output.WriteLine($"Synthetic observations={count}, retained=1, superseded={snapshot.SupersededObservations}, elapsedMs={elapsed.Elapsed.TotalMilliseconds:F3}");
    }
}
