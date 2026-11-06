using Pocket.Automation.Contracts;
using Xunit;
namespace Pocket.Core.Tests;
public class AutomationProtocolTests
{
    private static Request Valid()=>new(1,Guid.NewGuid().ToString(),"Probe",123,456,"GhostFixture.Invoke");
    [Fact]public void OnlyExplicitFixtureOperationsAndTargetsAreValid()
    {
        Assert.True(Protocol.Valid(Valid()));
        Assert.False(Protocol.Valid(Valid() with{Operation="Type"}));
        Assert.False(Protocol.Valid(Valid() with{Target="Delete"}));
        Assert.False(Protocol.Valid(Valid() with{Version=2}));
        Assert.False(Protocol.Valid(Valid() with{ProcessId=0}));
    }
    [Fact]public void ExecutionRequiresTokenAndFiniteValue()
    {
        Assert.False(Protocol.Valid(Valid() with{Operation="Execute"}));
        Assert.False(Protocol.Valid(Valid() with{Value=double.NaN}));
        Assert.True(Protocol.Valid(Valid() with{Operation="Execute",Token=Guid.NewGuid().ToString()}));
    }
    [Fact]public void PointProbeRequiresBothFiniteCoordinates()
    {
        Assert.False(Protocol.Valid(Valid() with{Operation="ProbePoint"}));
        Assert.False(Protocol.Valid(Valid() with{Operation="ProbePoint",X=double.NaN,Y=2}));
        Assert.True(Protocol.Valid(Valid() with{Operation="ProbePoint",X=-1920,Y=200}));
    }
}
