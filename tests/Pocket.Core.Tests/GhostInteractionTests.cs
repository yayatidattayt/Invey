using Pocket.Core;
using Xunit;
namespace Pocket.Core.Tests;
public sealed class GhostInteractionTests
{
    [Fact]
    public void IndependentClaimsAndStopRejectLateEvents()
    {
        var scene = new InteractionState(new(0,0,900,600),new Dictionary<string,DipRect> {
            ["A"]=new(0,0,200,200),["B"]=new(400,100,200,200)});
        var bridge=new GhostInteraction(scene); bridge.Register("A"); bridge.Register("B");
        scene.Move(PointerOwner.Mouse,new(10,10)); Assert.True(scene.Press(PointerOwner.Mouse,"A"));
        var id=bridge.BeginControlledSource(); Assert.True(bridge.Arm(id));
        Assert.False(bridge.Submit(id,0,1,new(10,10),GhostIntent.Press,true));
        Assert.True(bridge.Submit(id,1,2,new(410,110),GhostIntent.Press,true));
        Assert.True(bridge.Submit(id,2,3,new(450,150),GhostIntent.Move,true));
        Assert.Equal(440,scene.Target("B").X); Assert.Equal("A",scene.Pointer(PointerOwner.Mouse).Target);
        bridge.Stop(); bridge.Stop();
        Assert.Null(scene.Pointer(PointerOwner.Ghost).Target);
        Assert.False(bridge.Submit(id,3,4,new(460,160),GhostIntent.Press,true));
        Assert.Equal("A",scene.Pointer(PointerOwner.Mouse).Target);
    }
    [Theory]
    [InlineData(false,0)] [InlineData(true,150)] [InlineData(true,-1)]
    public void InvalidGestureEvidenceCancelsAndCannotRearm(bool full,long age)
    {
        var scene=new InteractionState(new(0,0,500,500),new Dictionary<string,DipRect>{{"B",new(0,0,200,200)}});
        var b=new GhostInteraction(scene); b.Register("B"); var id=b.BeginControlledSource(); b.Arm(id);
        Assert.True(b.Submit(id,0,1,new(20,20),GhostIntent.Press,true));
        Assert.False(b.Submit(id,1,2,new(30,30),GhostIntent.Move,full,age));
        Assert.Null(scene.Pointer(PointerOwner.Ghost).Target); Assert.False(b.Arm(id));
    }
    [Fact]
    public void ReplacementAndRemovalInvalidateClaims()
    {
        var scene=new InteractionState(new(0,0,500,500),new Dictionary<string,DipRect>{{"B",new(0,0,200,200)}});
        var b=new GhostInteraction(scene); b.Register("B"); var old=b.BeginControlledSource(); b.Arm(old);
        b.Submit(old,0,1,new(20,20),GhostIntent.Press,true); b.Unregister("B");
        Assert.Null(scene.Pointer(PointerOwner.Ghost).Target);
        var current=b.BeginControlledSource(); b.Arm(current);
        Assert.False(b.Submit(old,1,2,new(20,20),GhostIntent.Press,true));
        Assert.Null(scene.Pointer(PointerOwner.Ghost).Target); b.Pause(); Assert.False(b.Arm(current));
    }
}
