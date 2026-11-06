using Pocket.Automation.Contracts;
using Xunit;
namespace Pocket.Core.Tests;
public class RequestLedgerTests
{
    [Fact]public void DuplicateGuidsCannotBypassDedupWithAlternateFormatting()
    {
        var ledger=new RequestLedger();var id=Guid.NewGuid();
        Assert.True(ledger.TryAdd(id.ToString("D")));Assert.False(ledger.TryAdd(id.ToString("B").ToUpperInvariant()));
    }
    [Fact]public void FullLedgerFailsClosedWithoutEvictingOldAuthority()
    {
        var ledger=new RequestLedger(2);string first=Guid.NewGuid().ToString();
        Assert.True(ledger.TryAdd(first));Assert.True(ledger.TryAdd(Guid.NewGuid().ToString()));
        Assert.False(ledger.TryAdd(Guid.NewGuid().ToString()));Assert.False(ledger.TryAdd(first));
    }
}
