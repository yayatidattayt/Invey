using Pocket.Core;
using Pocket.Core.Tracking;
using Xunit;
namespace Pocket.Core.Tests;
public class PointerStillnessMeasurementTests
{
    [Fact]public void Designated_interval_deduplicates_and_reports_known_aggregate_without_point_series()
    {
        var m=new PointerStillnessMeasurement();var id=Guid.NewGuid();m.Begin(id,1000);
        m.Observe(id,0,1001,new(-1,0));m.Observe(id,0,1002,new(100,0));m.Observe(id,1,1003,new(1,0));
        m.Tick(id,11000);var json=System.Text.Json.JsonSerializer.SerializeToElement(m.Read());
        Assert.False(m.Running);Assert.Equal(2,json.GetProperty("ValidFinalPointerSamples").GetInt64());
        Assert.Equal(1,json.GetProperty("RmsPixels").GetDouble(),6);Assert.False(json.GetProperty("ContainsRawPoints").GetBoolean());
    }
    [Fact]public void Loss_is_not_a_fabricated_stationary_point_and_session_change_cancels()
    {
        var m=new PointerStillnessMeasurement();var id=Guid.NewGuid();m.Begin(id,1000);m.Observe(id,0,1001,null);
        m.Observe(Guid.NewGuid(),1,1002,new(0,0));Assert.False(m.Running);Assert.Contains("Incomplete",m.Status);
        var json=System.Text.Json.JsonSerializer.SerializeToElement(m.Read());Assert.Equal(0,json.GetProperty("ValidFinalPointerSamples").GetInt64());Assert.Equal(System.Text.Json.JsonValueKind.Null,json.GetProperty("RmsPixels").ValueKind);
    }
}
