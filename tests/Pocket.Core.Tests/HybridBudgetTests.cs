using System.Text.Json;
using Pocket.Core.Tracking;
using Xunit;
namespace Pocket.Core.Tests;
public class HybridBudgetTests
{
    private static string Ledger(bool released=true,int charge=90000)
    {
        var rows=Enumerable.Range(0,14).Select(i=>new CameraBudgetEntry(Guid.NewGuid(),i==13&&charge==90000?92999:charge,released)).ToArray();
        string path=Path.Combine(Path.GetTempPath(),"hybrid-budget-"+Guid.NewGuid()+".json");
        File.WriteAllText(path,JsonSerializer.Serialize(new CameraBudgetState(8,"execution-16",rows,CameraBudget.PerformanceFollowupAmendment,5,10)));return path;
    }
    [Fact] public void Approved_AB_preserves_fourteen_rows_and_each_start_requires_its_own_stage()
    {
        string path=Ledger();
        try {
            CameraBudgetEntry[] history;
            using(var budget=new CameraBudget(path))
            {
                history=budget.Read().Sessions;Assert.Equal(1262999,history.Sum(r=>r.ChargedMs));Assert.False(budget.Availability.Available);
                Assert.Throws<InvalidDataException>(budget.AmendHybridFollowup);
                budget.AmendHybridQualification();budget.AmendHybridQualification();Assert.Equal(history,budget.Read().Sessions);
                Assert.Equal(16,budget.SessionLimitCount);Assert.Equal(1800000,budget.TotalLimitMilliseconds);Assert.Equal("full",budget.RequiredPerformanceMode);
                Assert.True(budget.AllowsVisualBackend(true));Assert.False(budget.AllowsVisualBackend(false));Assert.Equal(1,budget.Availability.Remaining);
                var first=Guid.NewGuid();budget.Reserve(first);Assert.Throws<InvalidDataException>(budget.AmendHybridFollowup);budget.Complete(first,180000);
                Assert.False(budget.Availability.Available);Assert.Throws<InvalidDataException>(()=>budget.Reserve(Guid.NewGuid()));
            }
            using(var budget=new CameraBudget(path))
            {
                budget.AmendHybridQualification();Assert.False(budget.Availability.Available);
                budget.AmendHybridFollowup();Assert.False(budget.AllowsVisualBackend(true));Assert.True(budget.AllowsVisualBackend(false));
                var second=Guid.NewGuid();budget.Reserve(second);budget.Complete(second,180000);
            }
            using(var budget=new CameraBudget(path))
            {
                budget.AmendHybridFollowup();budget.AmendHybridQualification();budget.AmendPerformanceFollowup();
                Assert.Equal(16,budget.Read().Sessions.Length);Assert.Equal(history,budget.Read().Sessions.Take(14));
                Assert.Equal(1622999,budget.Read().Sessions.Sum(r=>r.ChargedMs));Assert.False(budget.Availability.Available);
                Assert.Throws<InvalidDataException>(()=>budget.Reserve(Guid.NewGuid()));
            }
        } finally {File.Delete(path);File.Delete(path+".lock");}
    }
    [Theory][InlineData(false,90000)][InlineData(true,125000)]
    public void Amendment_cannot_erase_pending_release_or_exceed_unchanged_time_ceiling(bool released,int charge)
    {
        string path=Ledger(released,charge);try{using var b=new CameraBudget(path);var before=b.Read();Assert.Throws<InvalidDataException>(b.AmendHybridQualification);Assert.Equal(before.Sessions,b.Read().Sessions);Assert.Equal(8,b.Read().Version);}
        finally{File.Delete(path);File.Delete(path+".lock");}
    }
}
