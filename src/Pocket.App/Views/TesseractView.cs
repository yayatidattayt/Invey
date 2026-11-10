using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Pocket.Core;
namespace Pocket.App.Views;
public sealed class TesseractView : FrameworkElement
{
    private static readonly Pen NearPen=CreatePen(Brushes.DeepSkyBlue);
    private static readonly Pen FarPen=CreatePen(Brushes.DodgerBlue);
    private static Pen CreatePen(Brush brush){var pen=new Pen(brush,1.35);pen.Freeze();return pen;}
    private double _yaw=.45, _pitch=.3;
    private Point? _drag;
    public TesseractView()
    {
        Focusable=true; ClipToBounds=true;
        LostMouseCapture+=(_,_)=>_drag=null;
        IsVisibleChanged+=(_,_)=>{if(!IsVisible)Cancel();};
        Unloaded+=(_,_)=>Cancel();
    }
    private void Cancel(){_drag=null;if(IsMouseCaptured)ReleaseMouseCapture();}
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {base.OnMouseLeftButtonDown(e);Focus();if(CaptureMouse())_drag=e.GetPosition(this);e.Handled=true;}
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);if(_drag is not {} previous)return;
        var now=e.GetPosition(this);_yaw+=Math.Clamp(now.X-previous.X,-40,40)*.01;_pitch+=Math.Clamp(now.Y-previous.Y,-40,40)*.01;
        _yaw%=Math.Tau;_pitch%=Math.Tau;_drag=now;InvalidateVisual();
    }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e){base.OnMouseLeftButtonUp(e);Cancel();e.Handled=true;}
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch(e.Key){case Key.Home:_yaw=.45;_pitch=.3;break;case Key.Left:_yaw-=.1;break;case Key.Right:_yaw+=.1;break;case Key.Up:_pitch-=.1;break;case Key.Down:_pitch+=.1;break;case Key.Escape:Cancel();break;default:return;}
        InvalidateVisual();e.Handled=true;
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);dc.DrawRectangle(Brushes.Transparent,null,new Rect(RenderSize));
        var points=TesseractGeometry.Project(_yaw,_pitch);
        var scale=Math.Min(ActualWidth,ActualHeight)*.17;
        foreach(var (a,b) in TesseractGeometry.Edges)
        {
            var p=points[a];var q=points[b];
            var pen=a<8&&b<8?NearPen:FarPen;
            dc.DrawLine(pen,new Point(ActualWidth/2+p.X*scale,ActualHeight/2+p.Y*scale),new Point(ActualWidth/2+q.X*scale,ActualHeight/2+q.Y*scale));
        }
        if(IsKeyboardFocused)dc.DrawRectangle(null,new Pen(Brushes.DeepSkyBlue,1),new Rect(1,1,Math.Max(0,ActualWidth-2),Math.Max(0,ActualHeight-2)));
    }
}
