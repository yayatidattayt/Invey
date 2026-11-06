using Pocket.Core;
using Xunit;
namespace Pocket.Core.Tests;
public class InteractionTests
{
    [Fact] public void MouseReleaseCannotCancelGhostClaim()
    {
        var s=Scene();s.Move(PointerOwner.Mouse,new(110,110));s.Move(PointerOwner.Ghost,new(510,110));
        Assert.True(s.Press(PointerOwner.Mouse,"a"));Assert.True(s.Press(PointerOwner.Ghost,"b"));
        s.Release(PointerOwner.Mouse);s.Cancel(PointerOwner.Mouse);
        Assert.Equal("b",s.Pointer(PointerOwner.Ghost).Target);
        s.Move(PointerOwner.Ghost,new(550,150));Assert.Equal(540,s.Target("b").X);
    }
    [Fact] public void CanceledChannelCannotMoveReleasedTargetOrStealOtherClaim()
    {
        var s=Scene();s.Move(PointerOwner.Mouse,new(110,110));s.Move(PointerOwner.Ghost,new(510,110));
        s.Press(PointerOwner.Mouse,"a");s.Press(PointerOwner.Ghost,"b");s.Cancel(PointerOwner.Ghost);
        var before=s.Target("b");s.Move(PointerOwner.Ghost,new(900,700));
        Assert.Equal(before,s.Target("b"));Assert.Equal("a",s.Pointer(PointerOwner.Mouse).Target);
        s.Move(PointerOwner.Ghost,new(120,120));Assert.False(s.Press(PointerOwner.Ghost,"a"));
    }
    [Fact]
    public void RemovingTargetCancelsOnlyItsOwnerAndResizeRespectsClaims()
    {
        var scene = Scene();
        scene.Move(PointerOwner.Mouse, new(110, 110));
        scene.Move(PointerOwner.Ghost, new(510, 110));
        Assert.True(scene.Press(PointerOwner.Mouse, "a"));
        Assert.True(scene.Press(PointerOwner.Ghost, "b"));
        Assert.False(scene.Resize(PointerOwner.Mouse, "b", new(510, 120, 300, 200)));
        scene.RemoveTarget("b");
        Assert.Null(scene.Pointer(PointerOwner.Ghost).Target);
        Assert.Equal("a", scene.Pointer(PointerOwner.Mouse).Target);
        scene.SetTarget("b", new(990, 770, 320, 200));
        Assert.Equal(new DipRect(680, 600, 320, 200), scene.Target("b"));
    }

    private static InteractionState Scene()=>new(new(0,0,1000,800),new Dictionary<string,DipRect>{{"a",new(100,100,200,100)},{"b",new(500,100,200,100)}});
    [Fact] public void DistinctTargetsCanBeClaimedAndReleasedIndependently()
    {
        var s=Scene();s.Move(PointerOwner.Mouse,new(110,110));s.Move(PointerOwner.Ghost,new(510,110));
        Assert.True(s.Press(PointerOwner.Mouse,"a"));Assert.True(s.Press(PointerOwner.Ghost,"b"));
        s.Move(PointerOwner.Mouse,new(130,150));s.Move(PointerOwner.Ghost,new(540,160));
        Assert.Equal(120,s.Target("a").X);Assert.Equal(530,s.Target("b").X);
        s.Release(PointerOwner.Ghost);Assert.Equal("a",s.Pointer(PointerOwner.Mouse).Target);
        s.Cancel(PointerOwner.Ghost);Assert.Equal("a",s.Pointer(PointerOwner.Mouse).Target);
    }
    [Fact] public void OccupiedTargetAndMissAreRejected()
    {
        var s=Scene();Assert.False(s.Press(PointerOwner.Ghost,"a"));
        s.Move(PointerOwner.Mouse,new(110,110));s.Move(PointerOwner.Ghost,new(120,120));
        Assert.True(s.Press(PointerOwner.Mouse,"a"));Assert.False(s.Press(PointerOwner.Ghost,"a"));
    }
    [Fact] public void DragKeepsOffsetAndClamps()
    {
        var s=Scene();s.Move(PointerOwner.Ghost,new(120,120));s.Press(PointerOwner.Ghost,"a");
        s.Move(PointerOwner.Ghost,new(999,799));Assert.Equal(new DipRect(800,700,200,100),s.Target("a"));
        s.Cancel(PointerOwner.Ghost);s.Move(PointerOwner.Ghost,new(1,1));Assert.Equal(800,s.Target("a").X);
    }
    [Theory][InlineData(double.NaN)][InlineData(double.PositiveInfinity)][InlineData(double.NegativeInfinity)]
    public void NonFiniteInputIsRejected(double value){var s=Scene();Assert.False(s.Move(PointerOwner.Ghost,new(value,0)));}
    [Theory][InlineData(96)][InlineData(120)][InlineData(144)][InlineData(192)]
    public void CoordinateRoundTripSupportsNegativeOrigin(double dpi)
    {
        var t=new DisplayTransform(new(-1920,-300),dpi);var p=new ScreenPoint(-1400,100);var result=t.ToScreen(t.ToDip(p));
        Assert.Equal(p.X,result.X,8);Assert.Equal(p.Y,result.Y,8);
    }
    [Fact] public void RoutePreservesAllTransitionsEvenWhenTicksAreLate()
    {
        var r=ReplaySession.PanelRoute(new(100,100,300,160),new(0,0,1000,800),20,5000);
        Assert.True(r.Start());Assert.False(r.Start());Assert.Empty(r.Advance(r.Epoch,TimeSpan.FromSeconds(4)));
        var due=r.Advance(r.Epoch,r.Duration);
        Assert.Equal(20,due.Count(x=>x.Action==ReplayAction.Press));Assert.Equal(20,due.Count(x=>x.Action==ReplayAction.Release));
        Assert.Equal(800,due.Count(x=>x.Action==ReplayAction.Move)-20);Assert.False(r.IsRunning);
        Assert.Empty(r.Advance(r.Epoch,r.Duration));
    }
    [Fact] public void StopAndRestartRejectStaleEpoch()
    {
        var r=ReplaySession.PanelRoute(new(100,100,300,160),new(0,0,1000,800),1,0);
        r.Start();long old=r.Epoch;r.Stop();Assert.Empty(r.Advance(old,r.Duration));
        r.Start();Assert.Empty(r.Advance(old,r.Duration));Assert.NotEmpty(r.Advance(r.Epoch,TimeSpan.Zero));
    }
    [Fact] public void BackwardTimeCannotDuplicateEvents()
    {
        var r=ReplaySession.PanelRoute(new(100,100,300,160),new(0,0,1000,800),1,0);r.Start();
        r.Advance(r.Epoch,TimeSpan.FromMilliseconds(500));Assert.Empty(r.Advance(r.Epoch,TimeSpan.Zero));
    }
    [Fact] public void InvalidReplayFailsBeforeStart()
    {
        Assert.Throws<ArgumentException>(()=>new ReplaySession(Array.Empty<ReplayEvent>()));
        Assert.Throws<ArgumentException>(()=>new ReplaySession(new[]{new ReplayEvent(TimeSpan.FromSeconds(-1),ReplayAction.Move,new(0,0))}));
    }
    [Fact] public void ReplayMovesAndReleasesActualScene()
    {
        var s=Scene();var original=s.Target("a");var r=ReplaySession.PanelRoute(original,s.Bounds,1,0);r.Start();
        foreach(var e in r.Advance(r.Epoch,r.Duration))
            switch(e.Action){case ReplayAction.Move:s.Move(PointerOwner.Ghost,e.Position);break;case ReplayAction.Press:Assert.True(s.Press(PointerOwner.Ghost,"a"));break;case ReplayAction.Release:s.Release(PointerOwner.Ghost);break;}
        Assert.Equal(original,s.Target("a"));Assert.Null(s.Pointer(PointerOwner.Ghost).Target);
    }
}
