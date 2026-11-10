using Pocket.Automation.Contracts;
using Pocket.Automation.Client;
using Xunit;
namespace Pocket.Core.Tests;
public class ExternalProtocolTests
{
    private static ExternalGrant Grant()=>new(Guid.NewGuid().ToString(),Guid.NewGuid(),123,456,999,789,888,["Invoke","Toggle"]);
    private static Request Probe(ExternalGrant g)=>new(2,Guid.NewGuid().ToString(),"ProbePoint",g.Window,g.ProcessId,"Invoke",X:-123,Y:456,Grant:g.Id,Generation:g.Generation);
    [Fact] public void ExternalConsentIsSeparateFromFixtureTrust()
    {
        var g=Grant();var r=Probe(g);Assert.True(g.Valid);Assert.True(Protocol.Valid(r));Assert.True(g.Permits(r));
        Assert.False(Protocol.Valid(r with{Version=1,Target="GhostFixture.Invoke"}));
        Assert.False(Protocol.Valid(r with{Target="GhostFixture.Invoke"}));
        Assert.False(Protocol.Valid(r with{Grant=null}));Assert.False(Protocol.Valid(r with{Generation=Guid.Empty}));
    }
    [Fact] public void ExternalGrantBindsWindowLifetimeSessionAndCapabilityScope()
    {
        var g=Grant();var r=Probe(g);
        Assert.False(g.Permits(r with{Window=124}));Assert.False(g.Permits(r with{ProcessId=457}));
        Assert.False(g.Permits(r with{Grant=Guid.NewGuid().ToString()}));Assert.False(g.Permits(r with{Generation=Guid.NewGuid()}));
        Assert.False(g.Permits(r with{Target="Scroll"}));
        Assert.False((g with{Lifetime=0}).Valid);Assert.False((g with{HostProcessId=g.ProcessId}).Valid);
    }
    [Fact] public void ExternalDiscoveryCannotExpandToArbitraryCommandsOrUnboundedScope()
    {
        var g=Grant();var r=Probe(g);
        Assert.False((g with{Actions=["Type"]}).Valid);Assert.False((g with{Actions=[]}).Valid);
        Assert.False((g with{Actions=["Invoke","Invoke"]}).Valid);
        Assert.False(Protocol.Valid(r with{X=null}));Assert.False(Protocol.Valid(r with{X=double.PositiveInfinity}));
        Assert.False(Protocol.Valid(r with{Operation="Probe"}));Assert.False(Protocol.Valid(r with{Operation="Click"}));
    }
    [Fact] public void ExternalMutationsStillRequireOneUseCapabilityAndObservationBinding()
    {
        var r=Probe(Grant());Assert.False(Protocol.Valid(r with{Operation="Execute"}));
        Assert.True(Protocol.Valid(r with{Operation="Execute",Token=Guid.NewGuid().ToString()}));
        Assert.False(Protocol.Valid(r with{Operation="Observe"}));
        Assert.True(Protocol.Valid(r with{Operation="Observe",Binding=new("root","element",1,2,3,4,0)}));
    }
    [Fact] public async Task FixtureClientCannotAcceptExternalAuthorityOrStartWorkerForIt()
    {
        await using var client=new WorkerClient("does-not-exist","does-not-exist",Environment.ProcessId);
        var denied=await client.RequestAsync(Probe(Grant()));Assert.Equal("Denied",denied.Status);Assert.Null(client.ActiveProcessId);
    }
}

