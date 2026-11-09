using System.Text.Json;
using Pocket.Core.Tracking;
using Xunit;
namespace Pocket.Core.Tests;
public class PointerBudgetTests
{
    [Fact] public void OneExplicitAmendmentRetainsHistoryAndCannotRenew()
    {
        var path=Path.Combine(Path.GetTempPath(),"pointer-budget-"+Guid.NewGuid()+".json");
        var rows=Enumerable.Range(0,10).Select(_=>new CameraBudgetEntry(Guid.NewGuid(),80000,true)).ToArray();
        File.WriteAllText(path,JsonSerializer.Serialize(new CameraBudgetState(4,"execution-16",rows,CameraBudget.LearningTargetsAmendment,5)));
        try
        {
            Guid session=Guid.NewGuid();
            using(var b=new CameraBudget(path))
            {
                Assert.False(b.Availability.Available);b.AmendPointerQualification();Assert.Equal(1,b.Availability.Remaining);
                Assert.Equal(rows,b.Read().Sessions);Assert.Equal(12,b.SessionLimitCount);Assert.Equal(1800000,b.TotalLimitMilliseconds);
                b.Reserve(session);Assert.False(b.Availability.Available);b.Complete(session,1000);
            }
            using var reopened=new CameraBudget(path);reopened.AmendPointerQualification();Assert.False(reopened.Availability.Available);
            Assert.Equal(rows,reopened.Read().Sessions.Take(10));Assert.Throws<InvalidDataException>(()=>reopened.Reserve(Guid.NewGuid()));
            reopened.AmendPointerRetest();Assert.Equal(1,reopened.Availability.Remaining);
            var retest=Guid.NewGuid();reopened.Reserve(retest);
            Assert.Throws<InvalidDataException>(()=>reopened.Reserve(Guid.NewGuid()));reopened.Complete(retest,1000);
            reopened.AmendPointerRetest();reopened.AmendPointerQualification();Assert.Equal(12,reopened.Read().Sessions.Length);
            Assert.False(reopened.Availability.Available);Assert.Equal(rows,reopened.Read().Sessions.Take(10));
        }
        finally{File.Delete(path);File.Delete(path+".lock");}
    }
}
