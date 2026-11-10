using System.Collections.Immutable;
using Pocket.Core.Tracking;
using Xunit;
namespace Pocket.Core.Tests;
public class GhostMappingPipelineTests
{
    [Theory]
    [InlineData(96,false)] [InlineData(120,false)] [InlineData(96,true)] [InlineData(120,true)]
    public void WholeMapperCoversEdgesCenterNegativeOriginMirrorAndInvalidation(int dpi,bool mirror)
    {
        var display=new CalibrationDisplay(2,-1920,-120,1920,1200,dpi);
        var context=new LearningContext("Recorded",LearningContext.HashDevice("mapping-fixture"),640,480,display,2,mirror,12,100);
        foreach(var uv in new[]{new DipPoint(0,0),new(1,0),new(0,1),new(1,1),new(.5,.5)})
        {
            var mapper=new GhostTrackingMapper();var source=Guid.NewGuid();DipPoint? result=null;TrackingSnapshot? last=null;
            for(int i=0;i<8;i++)
            {
                var hand=new HandObservation(source,i,1000+i*40,1000+i*40,
                    Enumerable.Repeat(new ImageLandmark(.12+.76*uv.X,.12+.76*uv.Y,0),21).ToImmutableArray(),HandSide.Right,.98,false,new(640,480));
                last=new(TrackingState.Observing,source,TrackingSource.Recorded,hand,TimeSpan.Zero,null,false,i+1,0);
                result=mapper.Map(last,context,null);
            }
            Assert.NotNull(result);Assert.Equal(display.X+(mirror?1-uv.X:uv.X)*1919,result.Value.X,5);
            Assert.Equal(display.Y+uv.Y*1199,result.Value.Y,5);
            Assert.Null(mapper.Map(last!,context with{FrameWidth=1280},null));
            Assert.Null(mapper.Map(last! with{State=TrackingState.Stopped},context,null));
            Assert.Null(mapper.Map(last!,null,null));
        }
    }
}
