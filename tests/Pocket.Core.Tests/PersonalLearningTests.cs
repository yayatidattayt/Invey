using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using Pocket.Core;
using Pocket.Core.Tracking;
using Xunit;

namespace Pocket.Core.Tests;

public sealed class PersonalLearningTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private static LearningContext Context => new("Simulation", LearningContext.HashDevice("controlled"), 640, 480,
        new(1, -1920, 0, 1920, 1080, 120), 2, false, 12, 100);
    private static TargetExample[] Data(double bias, bool train, double noise = 0)
    {
        double[] axis = train ? [.12, .5, .88] : [.27, .51, .73];
        return axis.SelectMany((y, j) => axis.Select((x, i) => new TargetExample(Guid.NewGuid(),
            new(x + bias + noise * (i - 1), y - bias / 2 + noise * (j - 1)), new(x, y), Context))).ToArray();
    }
    private static PersonalModel Accepted(double bias = .025) =>
        Assert.IsType<PersonalModel>(PersonalTrainer.Train(Data(bias, true), Data(bias, false), null).Candidate);
    [Theory]
    [InlineData(.015, .0005)]
    [InlineData(.035, .001)]
    [InlineData(-.02, .0008)]
    public void IndependentTargetLearningChangesParametersAndImproves(double bias, double noise)
    {
        var training = Data(bias, true, noise); var validation = Data(bias, false, noise / 2);
        var trial = PersonalTrainer.Train(training, validation, null);
        var model = Assert.IsType<PersonalModel>(trial.Candidate);
        Assert.NotEqual(0, model.X[0]);
        Assert.True(model.Validated.Mean < model.Baseline.Mean * .15);
        Assert.True(model.Validated.P95 < model.Baseline.P95);
        Assert.Equal(9, model.Validated.Count);
        Assert.Equal(18, training.Concat(validation).Select(x => x.Id).Distinct().Count());
        output.WriteLine($"bias={bias}; independent mean {model.Baseline.MeanPixels:F4} -> {model.Validated.MeanPixels:F4}px; p95={model.Validated.P95Pixels:F4}");
    }
    [Fact] public void WorseCandidateAndDistributionShiftAreRejected()
    {
        Assert.False(PersonalTrainer.Train(Data(.03, true), Data(0, false), null).Accepted);
        Assert.False(PersonalTrainer.Train(Data(.03, true), Data(-.03, false), null).Accepted);
        var current = Accepted();
        Assert.False(PersonalTrainer.Train(Data(.025, true), Data(.025, false), current).Accepted);
    }
    [Fact] public void DuplicateOrOverlappingSamplesCannotValidate()
    {
        var train = Data(.025, true); var validation = Data(.025, false);
        Assert.False(PersonalTrainer.Train(train, train, null).Accepted);
        train[8] = train[0];
        Assert.False(PersonalTrainer.Train(train, validation, null).Accepted);
    }
    [Fact] public void InsufficientCoverageInvalidLabelsAndOutliersCannotTrain()
    {
        Assert.False(PersonalTrainer.Train(Data(.02, true)[..8], Data(.02, false), null).Accepted);
        var bad = Data(.02, true); bad[0] = bad[0] with { Target = new(double.NaN, .2) };
        Assert.False(PersonalTrainer.Train(bad, Data(.02, false), null).Accepted);
        bad = Data(.02, true); bad[0] = bad[0] with { Target = new(.9, .9) };
        Assert.False(PersonalTrainer.Train(bad, Data(.02, false), null).Accepted);
        bad = Data(.02, true).Select(x => x with { Observed = new(.5, .5), Target = new(.5, .5) }).ToArray();
        Assert.False(PersonalTrainer.Train(bad, Data(.02, false), null).Accepted);
    }
    [Fact] public void ContextAndDomainMismatchFallBackExactly()
    {
        var model = Accepted(); var p = new DipPoint(.5, .5);
        Assert.NotEqual(p, model.Predict(p, Context));
        foreach (var context in new[] { Context with { Mirror = true }, Context with { Hand = 1 },
            Context with { Source = "Camera" }, Context with { FrameWidth = 1280 }, Context with { Inset = 15 },
            Context with { Display = Context.Display with { X = 0 } }, Context with { DeviceHash = LearningContext.HashDevice("other") } })
            Assert.Equal(p, model.Predict(p, context));
        foreach (var outside in new[] { new DipPoint(.19, .5), new(.5, .81), new(-1, 0) })
            Assert.Equal(outside, model.Predict(outside, Context));
        var invalid = Data(.02, false); invalid[0] = invalid[0] with { Context = Context with { Mirror = true } };
        Assert.False(PersonalTrainer.Train(Data(.02, true), invalid, null).Accepted);
    }
    private sealed class Fixture : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "invey-learning-" + Guid.NewGuid().ToString("N"));
        public string Directory => Path.Combine(Root, "Learning");
        public PersonalLearningStore Store => new(Directory);
        public Fixture() => System.IO.Directory.CreateDirectory(Root);
        public void Dispose() => System.IO.Directory.Delete(Root, true);
    }
    [Fact] public void AcceptedParametersSurviveReloadAndReversibleSwap()
    {
        using var f = new Fixture(); var model = Accepted();
        var p = f.Store.Save(LearningProfile.Empty with { Enabled = true, Current = model }, 0);
        var loaded = f.Store.Load();
        Assert.True(loaded.Writable); Assert.True(loaded.Profile.Enabled);
        var point = new DipPoint(.4, .6);
        Assert.Equal(model.Predict(point, Context), loaded.Profile.Current!.Predict(point, Context));
        var rollback = f.Store.Save(p with { Current = p.Previous, Previous = p.Current }, p.Revision);
        Assert.Null(f.Store.Load().Profile.Current);
        f.Store.Save(rollback with { Current = rollback.Previous, Previous = rollback.Current }, rollback.Revision);
        Assert.Equal(model.Predict(point, Context), f.Store.Load().Profile.Current!.Predict(point, Context));
    }
    [Fact] public void ResetDoesNotTouchPocketAndDisablesLearning()
    {
        using var f = new Fixture(); var pocket = Path.Combine(f.Root, "pocket.json"); File.WriteAllText(pocket, "preserved");
        f.Store.Save(LearningProfile.Empty with { Current = Accepted(), Enabled = true }, 0);
        var p = f.Store.Reset();
        Assert.False(p.Enabled); Assert.Null(p.Current); Assert.Equal("preserved", File.ReadAllText(pocket));
        Assert.False(File.Exists(Path.Combine(f.Directory, "personal.json.bak")));
    }
    [Fact] public void CorruptionAndFutureSchemaPreservedWithoutStartupFailure()
    {
        using var f = new Fixture(); f.Store.Save(LearningProfile.Empty, 0);
        var path = Path.Combine(f.Directory, "personal.json");
        File.WriteAllText(path, "{broken"); Assert.False(f.Store.Load().Writable);
        Assert.False(f.Store.Load().Profile.Enabled);
        Assert.ThrowsAny<Exception>(() => f.Store.Save(LearningProfile.Empty, 0));
        f.Store.Reset();
        File.WriteAllText(path, "{\"Schema\":2,\"Payload\":\"future\",\"Checksum\":\"unknown\"}");
        Assert.False(f.Store.Load().Writable);
        Assert.Throws<NotSupportedException>(() => f.Store.Reset());
        Assert.Contains("future", File.ReadAllText(path));
    }
    [Fact] public void ReadOnlyLockedConcurrentAndInterruptedStagePreserveModel()
    {
        using var f = new Fixture(); var p = f.Store.Save(LearningProfile.Empty with { Current = Accepted() }, 0);
        var path = Path.Combine(f.Directory, "personal.json");
        byte[] before = File.ReadAllBytes(path);
        Assert.Throws<UnauthorizedAccessException>(() => new PersonalLearningStore(f.Directory, false).Save(p, p.Revision));
        Assert.Throws<IOException>(() => f.Store.Save(p, 0));
        using (var gate = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            Assert.Throws<IOException>(() => f.Store.Save(p, p.Revision));
        File.WriteAllText(path + ".abandoned.tmp", "unfinished stage");
        Assert.Equal(before, File.ReadAllBytes(path)); Assert.NotNull(f.Store.Load().Profile.Current);
        f.Store.Save(p with { Enabled = true }, p.Revision);
        Assert.NotNull(f.Store.RestoreBackup(p.Revision + 1).Current);
    }
    private static readonly Guid Session = Guid.NewGuid();
    private static TrackingSnapshot Snapshot(long seq, TrackingState state = TrackingState.Observing, double age = 0, long continuity = 0)
    {
        var points = Enumerable.Repeat(new ImageLandmark(.5, .5, 0), 21).ToImmutableArray();
        var hand = new HandObservation(Session, seq, seq * 20, seq * 20 + 2, points, HandSide.Right, FrameSize: new(640, 480));
        return new(state, Session, TrackingSource.Simulation, hand, TimeSpan.FromMilliseconds(age), null, false,
            seq + 1, 0, continuity, LastSequence: seq);
    }
    private static LearningLesson Lesson()
    {
        var lesson = new LearningLesson(); lesson.Begin(Context, Session); return lesson;
    }
    [Theory]
    [InlineData(TrackingState.Partial)]
    [InlineData(TrackingState.Ambiguous)]
    [InlineData(TrackingState.Lost)]
    [InlineData(TrackingState.Stale)]
    [InlineData(TrackingState.Stopped)]
    [InlineData(TrackingState.Unavailable)]
    public void IneligibleObservationClearsTeachingStability(TrackingState state)
    {
        var lesson = Lesson();
        for (int i = 0; i < 8; i++) lesson.Observe(Snapshot(i), Context, new(.25, .25));
        Assert.True(lesson.CanConfirm);
        lesson.Observe(Snapshot(8, state), Context, new(.25, .25));
        Assert.False(lesson.Confirm()); Assert.Empty(lesson.Baseline);
    }
    [Fact] public void TemporaryLossClearsStabilityButKeepsAlreadyConfirmedLabelsInTheSameContext()
    {
        var lesson = Lesson();
        for (int i = 0; i < 8; i++) lesson.Observe(Snapshot(i), Context, new(.25, .25));
        Assert.True(lesson.Confirm()); Assert.Single(lesson.Baseline);
        lesson.Observe(Snapshot(8, TrackingState.Lost) with { Observation = null }, Context, null);
        Assert.Equal(LessonPhase.Baseline, lesson.Phase);
        Assert.Single(lesson.Baseline); Assert.False(lesson.CanConfirm);
        lesson.Observe(Snapshot(9), Context with { Mirror = true }, new(.5, .25));
        Assert.Equal(LessonPhase.Idle, lesson.Phase); Assert.Empty(lesson.Baseline);
    }

    [Fact] public void RepeatsStaleReorderWrongHandAndReplacementCannotLabel()
    {
        var l = Lesson();
        for (int i = 0; i < 20; i++) l.Observe(Snapshot(1), Context, new(.25, .25));
        Assert.False(l.CanConfirm);
        l.Observe(Snapshot(2, age: 149), Context, new(.25, .25)); Assert.False(l.CanConfirm);
        for (int i = 3; i < 12; i++) l.Observe(Snapshot(i), Context, new(.25, .25));
        Assert.True(l.CanConfirm);
        l.Observe(Snapshot(12) with { Observation = Snapshot(12).Observation! with { Handedness = HandSide.Left } }, Context, new(.25, .25));
        Assert.False(l.CanConfirm);
        l.Observe(Snapshot(13) with { SessionId = Guid.NewGuid() }, Context, new(.25, .25));
        Assert.Equal(LessonPhase.Idle, l.Phase);
        l = Lesson();
        for (int i = 1; i < 9; i++) l.Observe(Snapshot(i), Context, new(.25, .25));
        l.Observe(Snapshot(0), Context, new(.25, .25)); Assert.False(l.CanConfirm);
    }
    [Fact] public void QualityEventsNeverTrainOrChangeGestureAuthority()
    {
        var q = new LearningQuality(); var snapshot = Snapshot(0);
        for (int i = 0; i < 10; i++) q.Observe(snapshot);
        Assert.Equal(1, q.Summary.Observations);
        foreach (var f in Enum.GetValues<LearningFeedback>()) q.Feedback(f);
        Assert.Equal(1, q.Summary.FalsePinch); Assert.Equal(1, q.Summary.MissedRelease);
        Assert.Equal(1, q.Summary.WrongHand); Assert.Equal(1, q.Summary.WrongDirection);
        var practice = new CalibrationPractice();
        Assert.NotEqual(PracticeState.Held, practice.Update(snapshot with { State = TrackingState.Ambiguous }, true, true));
    }
    [Fact] public void BoundedModelPerformanceIsMeasured()
    {
        var watch = Stopwatch.StartNew(); PersonalModel? model = null;
        for (int i = 0; i < 100; i++) model = Accepted();
        double trainMs = watch.Elapsed.TotalMilliseconds / 100;
        var context = Context; var p = new DipPoint(.5, .5); var sum = 0.0;
        long memory = GC.GetAllocatedBytesForCurrentThread(); watch.Restart();
        for (int i = 0; i < 100000; i++) sum += model!.Predict(p, context).X;
        output.WriteLine($"train mean={trainMs:F4}ms; evaluation={watch.Elapsed.TotalMilliseconds * 1000 / 100000:F4}us; allocation={GC.GetAllocatedBytesForCurrentThread() - memory} bytes/100000; checksum={sum:F2}");
        Assert.True(double.IsFinite(sum));
    }
}
