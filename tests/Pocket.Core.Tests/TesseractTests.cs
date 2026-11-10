using Xunit;
namespace Pocket.Core.Tests;
public class TesseractTests
{
 [Fact] public void Topology_is_four_regular_with_32_unique_edges(){var e=TesseractGeometry.Edges;Assert.Equal(32,e.Count);Assert.Equal(32,e.Distinct().Count());for(int i=0;i<16;i++)Assert.Equal(4,e.Count(x=>x.A==i||x.B==i));foreach(var(a,b) in e)Assert.Equal(1,System.Numerics.BitOperations.PopCount((uint)(a^b)));}
 [Fact] public void Projection_is_finite_and_repeatable(){var reset=TesseractGeometry.Project(.45,.3);for(int i=0;i<1000;i++)Assert.All(TesseractGeometry.Project(i*.1,i*.2),p=>Assert.True(double.IsFinite(p.X)&&double.IsFinite(p.Y)));Assert.Equal(reset,TesseractGeometry.Project(.45,.3));Assert.Throws<ArgumentOutOfRangeException>(()=>TesseractGeometry.Project(double.NaN,0));}
}
