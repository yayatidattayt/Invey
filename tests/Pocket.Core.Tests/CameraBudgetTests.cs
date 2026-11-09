using System.Text.Json;
using Pocket.Core.Tracking;
using Xunit;

namespace Pocket.Core.Tests;

public sealed class CameraBudgetTests
{
    [Fact]
    public void SixthReservationSurvivesNewControllerAndSeventhIsRejected()
    {
        using var fixture = new Fixture();
        for (int i = 0; i < 6; i++)
        {
            using var controller = new CameraBudget(fixture.Path);
            Guid id = Guid.NewGuid();
            controller.Reserve(id);
            controller.Complete(id, 120_000);
        }
        using var fresh = new CameraBudget(fixture.Path);
        Assert.Equal(720_000, fresh.Read().Sessions.Sum(row => row.ChargedMs));
        Assert.Throws<InvalidDataException>(() => fresh.Reserve(Guid.NewGuid()));
    }

    [Fact]
    public void InterruptedReservationDoesNotRefundOrCreateAnotherBudget()
    {
        using var fixture = new Fixture();
        using (var controller = new CameraBudget(fixture.Path)) controller.Reserve(Guid.NewGuid());
        using var restarted = new CameraBudget(fixture.Path);
        Assert.False(Assert.Single(restarted.Read().Sessions).Released);
        Assert.Throws<InvalidDataException>(() => restarted.Reserve(Guid.NewGuid()));
        Assert.Throws<IOException>(() => new CameraBudget(fixture.Path));
    }

    [Fact]
    public void MissingAndCorruptBudgetFailClosed()
    {
        using var fixture = new Fixture();
        File.Delete(fixture.Path);
        Assert.Throws<InvalidDataException>(() => new CameraBudget(fixture.Path));
        File.WriteAllText(fixture.Path, "{broken");
        Assert.Throws<JsonException>(() => new CameraBudget(fixture.Path));
    }

    [Fact]
    public void EarlyStopChargesElapsedButOverrunCannotRefund()
    {
        using var fixture = new Fixture();
        using var controller = new CameraBudget(fixture.Path);
        Guid id = Guid.NewGuid();
        controller.Reserve(id);
        Assert.Throws<InvalidDataException>(() => controller.Complete(id, 120_001));
        Assert.Equal(120_000, Assert.Single(controller.Read().Sessions).ChargedMs);
        controller.Complete(id, 573);
        Assert.Equal(573, Assert.Single(controller.Read().Sessions).ChargedMs);
    }

    [Fact]
    public void ContinuationAmendmentPreservesHistoryAndEnforcesBothNewLimits()
    {
        using var fixture = new Fixture();
        using (var original = new CameraBudget(fixture.Path))
        {
            var id = Guid.NewGuid();
            original.Reserve(id);
            original.Complete(id, 81_609);
            var before = original.Read().Sessions;
            original.AmendContinuation();
            original.AmendContinuation();
            Assert.Equal(before, original.Read().Sessions);
        }
        using var amended = new CameraBudget(fixture.Path);
        Assert.Equal(2, amended.Read().Version);
        Assert.Equal(180_000, amended.SessionLimitMilliseconds);
        for (int i = 1; i < 10; i++)
        {
            var id = Guid.NewGuid();
            amended.Reserve(id);
            amended.Complete(id, 180_000);
        }
        Assert.Throws<InvalidDataException>(() => amended.Reserve(Guid.NewGuid()));
        Assert.Equal(10, amended.Read().Sessions.Length);
    }

    [Fact]
    public void ContinuationCannotEraseUnconfirmedRelease()
    {
        using var fixture = new Fixture();
        using var budget = new CameraBudget(fixture.Path);
        budget.Reserve(Guid.NewGuid());
        Assert.Throws<InvalidDataException>(() => budget.AmendContinuation());
        Assert.Equal(1, budget.Read().Version);
    }

    [Fact]
    public void LearningAmendmentCarriesAllChargesAndAllowsOnlyThreeNewSessions()
    {
        using var f = new Fixture();
        using var budget = new CameraBudget(f.Path);
        var first = Guid.NewGuid(); budget.Reserve(first); budget.Complete(first, 81609);
        budget.AmendContinuation();
        var history = budget.Read().Sessions;
        budget.AmendLearning(); budget.AmendLearning();
        Assert.Equal(history, budget.Read().Sessions);
        for (int i = 0; i < 3; i++)
        {
            var session = Guid.NewGuid(); budget.Reserve(session); budget.Complete(session, 17000);
        }
        Assert.Throws<InvalidDataException>(() => budget.Reserve(Guid.NewGuid()));
        Assert.Equal(81609 + 51000, budget.Read().Sessions.Sum(x => x.ChargedMs));
    }

    [Fact]
    public void TargetsAmendmentPreservesEightRowsAndAllowsOnlyTwoFurtherStartsAcrossRestart()
    {
        using var f = new Fixture();
        using (var budget = new CameraBudget(f.Path))
        {
            for (int i = 0; i < 5; i++) { var id = Guid.NewGuid(); budget.Reserve(id); budget.Complete(id, 1000); }
            budget.AmendContinuation(); budget.AmendLearning();
            for (int i = 0; i < 3; i++) { var id = Guid.NewGuid(); budget.Reserve(id); budget.Complete(id, 12000); }
            Assert.Throws<InvalidDataException>(() => budget.Reserve(Guid.NewGuid()));
            var history = budget.Read().Sessions;
            budget.AmendLearningTargets(); budget.AmendLearningTargets();
            Assert.Equal(history, budget.Read().Sessions);
            Assert.Equal(5, budget.Read().Execution18StartCount);
            Assert.Equal(12, budget.SessionLimitCount);
            Assert.Equal(1800000, budget.TotalLimitMilliseconds);
            for (int i = 0; i < 2; i++) { var id = Guid.NewGuid(); budget.Reserve(id); budget.Complete(id, 180000); }
        }
        using var restarted = new CameraBudget(f.Path);
        restarted.AmendLearningTargets(); restarted.AmendLearning();
        Assert.Equal(10, restarted.Read().Sessions.Length);
        Assert.Equal(401000, restarted.Read().Sessions.Sum(s => s.ChargedMs));
        Assert.Throws<InvalidDataException>(() => restarted.Reserve(Guid.NewGuid()));
    }

    [Fact]
    public void TargetsAmendmentCannotRefundOrBypassUnconfirmedReservation()
    {
        using var f = new Fixture(); using var budget = new CameraBudget(f.Path);
        Assert.Throws<InvalidDataException>(() => budget.AmendLearningTargets());
        budget.AmendContinuation(); budget.AmendLearning(); budget.Reserve(Guid.NewGuid());
        var before = budget.Read();
        Assert.Throws<InvalidDataException>(() => budget.AmendLearningTargets());
        Assert.Equal(before.Sessions, budget.Read().Sessions);
        Assert.Equal(3, budget.Read().Version);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string _root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "InveyBudget-" + Guid.NewGuid().ToString("N"));
        public string Path => System.IO.Path.Combine(_root, "budget.json");
        public Fixture() { Directory.CreateDirectory(_root); CameraBudget.Initialize(Path); }
        public void Dispose()
        {
            File.Delete(Path);
            File.Delete(Path + ".lock");
            Directory.Delete(_root);
        }
    }
}
