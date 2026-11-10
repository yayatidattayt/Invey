using System.Text.Json;
using Pocket.Core.Tracking;
using Xunit;
namespace Pocket.Core.Tests;

public class PerformanceBudgetTests
{
    private static string Ledger(CameraBudgetEntry[] rows)
    {
        var path = Path.Combine(Path.GetTempPath(), "performance-budget-" + Guid.NewGuid() + ".json");
        File.WriteAllText(path, JsonSerializer.Serialize(new CameraBudgetState(6,"execution-16", rows,
            CameraBudget.PointerRetestAmendment,5,10)));
        return path;
    }
    [Fact]
    public void Two_starts_preserve_history_and_followup_requires_first_release_and_separate_amendment()
    {
        var rows=Enumerable.Range(0,12).Select(i=>new CameraBudgetEntry(Guid.NewGuid(),i==11?174077:80000,true)).ToArray();
        string path=Ledger(rows);
        try
        {
            Guid first=Guid.NewGuid(),second=Guid.NewGuid();
            using(var budget=new CameraBudget(path))
            {
                Assert.False(budget.Availability.Available);
                Assert.Throws<InvalidDataException>(budget.AmendPerformanceFollowup);
                budget.AmendPerformanceCheck();budget.AmendPerformanceCheck();
                Assert.Equal(rows,budget.Read().Sessions);Assert.Equal(1054077,budget.Read().Sessions.Sum(r=>r.ChargedMs));
                Assert.Equal(14,budget.SessionLimitCount);Assert.Equal(1800000,budget.TotalLimitMilliseconds);
                Assert.Equal("capture",budget.RequiredPerformanceMode);
                Assert.Equal(180000,budget.SessionLimitMilliseconds);Assert.Equal(1,budget.Availability.Remaining);
                budget.Reserve(first);Assert.Throws<InvalidDataException>(budget.AmendPerformanceFollowup);
                Assert.False(budget.Availability.Available);budget.Complete(first,180000);
            }
            using(var budget=new CameraBudget(path))
            {
                budget.AmendPerformanceCheck();Assert.False(budget.Availability.Available);
                Assert.Throws<InvalidDataException>(()=>budget.Reserve(second));
                budget.AmendPerformanceFollowup();Assert.Equal(1,budget.Availability.Remaining);
                Assert.Equal("full",budget.RequiredPerformanceMode);
                budget.Reserve(second);budget.Complete(second,180000);
            }
            using(var budget=new CameraBudget(path))
            {
                budget.AmendPerformanceCheck();budget.AmendPerformanceFollowup();budget.AmendPointerRetest();
                Assert.False(budget.Availability.Available);Assert.Equal(14,budget.Read().Sessions.Length);
                Assert.Equal(rows,budget.Read().Sessions.Take(12));
                Assert.Equal(1414077,budget.Read().Sessions.Sum(r=>r.ChargedMs));
                Assert.Throws<InvalidDataException>(()=>budget.Reserve(Guid.NewGuid()));
            }
        }
        finally {File.Delete(path);File.Delete(path+".lock");}
    }
    [Theory]
    [InlineData(false,130000)]
    [InlineData(true,80000)]
    public void Inadequate_time_or_unconfirmed_history_cannot_be_amended(bool pending,int charge)
    {
        var rows=Enumerable.Range(0,12).Select(i=>new CameraBudgetEntry(Guid.NewGuid(),charge,!pending||i!=11)).ToArray();
        string path=Ledger(rows);
        try
        {
            string original=File.ReadAllText(path);
            using var budget=new CameraBudget(path);
            Assert.Throws<InvalidDataException>(budget.AmendPerformanceCheck);
            Assert.Equal(original,File.ReadAllText(path));
        }
        finally {File.Delete(path);File.Delete(path+".lock");}
    }
    [Fact]
    public void Temporary_delete_sharing_conflict_can_recover_without_losing_rows()
    {
        var rows=Enumerable.Range(0,12).Select(_=>new CameraBudgetEntry(Guid.NewGuid(),80000,true)).ToArray();
        string path=Ledger(rows);
        try
        {
            using var budget=new CameraBudget(path);
            using var blocker=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
            using var started=new ManualResetEventSlim();
            var release=new Thread(()=>{started.Set();Thread.Sleep(70);blocker.Dispose();}) { IsBackground=true };
            release.Start();Assert.True(started.Wait(TimeSpan.FromSeconds(2)));
            try { budget.AmendPerformanceCheck(); }
            finally { Assert.True(release.Join(TimeSpan.FromSeconds(2))); }
            Assert.Equal(rows,budget.Read().Sessions);Assert.Equal(7,budget.Read().Version);
        }
        finally {File.Delete(path);File.Delete(path+".lock");}
    }
    [Fact]
    public void Persistent_conflict_fails_closed_with_original_bytes_and_allowance()
    {
        string path=Ledger(Enumerable.Range(0,12).Select(_=>new CameraBudgetEntry(Guid.NewGuid(),80000,true)).ToArray());
        try
        {
            string original=File.ReadAllText(path);
            using var budget=new CameraBudget(path);
            using var blocker=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
            var elapsed=System.Diagnostics.Stopwatch.StartNew();
            Assert.ThrowsAny<IOException>(budget.AmendPerformanceCheck);
            Assert.True(elapsed.Elapsed<TimeSpan.FromSeconds(2));
            Assert.Equal(original,File.ReadAllText(path));Assert.Equal(6,budget.Read().Version);
            Assert.False(budget.Availability.Available);
        }
        finally {File.Delete(path);File.Delete(path+".lock");}
    }
}
