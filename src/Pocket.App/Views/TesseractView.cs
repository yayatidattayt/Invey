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
    private Window? _owner;
    private Vector _velocity;
    private readonly System.Windows.Threading.DispatcherTimer _inertia=new(){Interval=TimeSpan.FromMilliseconds(16)};
    private long _lastTick;
    private long _lastMove;
    private void StopMotion(){_inertia.Stop();_velocity=new();}
    private void Deactivated(object? sender,EventArgs e){StopMotion();Cancel();}

    public TesseractView()
    {
        Focusable=true; ClipToBounds=true;
        LostMouseCapture+=(_,_)=>_drag=null;
        Loaded+=(_,_)=>{_owner=Window.GetWindow(this);if(_owner is not null)_owner.Deactivated+=Deactivated;};
        _inertia.Tick+=(_,_)=>{
            if(!IsVisible||_owner?.IsActive!=true||!SystemParameters.ClientAreaAnimation){StopMotion();return;}
            long now=Environment.TickCount64;double dt=Math.Clamp((now-_lastTick)/1000d,0,.05);_lastTick=now;
            _yaw+=_velocity.X*dt;_pitch+=_velocity.Y*dt;_velocity*=Math.Exp(-7*dt);
            if(_velocity.Length<.015)StopMotion();InvalidateVisual();
        };
        IsVisibleChanged+=(_,_)=>{if(!IsVisible){StopMotion();Cancel();}};
        Unloaded+=(_,_)=>{StopMotion();Cancel();if(_owner is not null)_owner.Deactivated-=Deactivated;_owner=null;};
    }
    private void Cancel(){_drag=null;if(IsMouseCaptured)ReleaseMouseCapture();}
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {base.OnMouseLeftButtonDown(e);StopMotion();_lastMove=Environment.TickCount64;Focus();if(CaptureMouse())_drag=e.GetPosition(this);e.Handled=true;}
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);if(_drag is not {} previous)return;
        var now=e.GetPosition(this);var stamp=Environment.TickCount64;
        double dt=Math.Clamp((stamp-_lastMove)/1000d,.008,.1);_lastMove=stamp;
        var delta=new Vector(Math.Clamp(now.X-previous.X,-40,40)*.01,Math.Clamp(now.Y-previous.Y,-40,40)*.01);
        _velocity=new Vector(Math.Clamp(delta.X/dt,-3,3),Math.Clamp(delta.Y/dt,-3,3));_yaw+=delta.X;_pitch+=delta.Y;
        _yaw%=Math.Tau;_pitch%=Math.Tau;_drag=now;InvalidateVisual();
    }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e){base.OnMouseLeftButtonUp(e);Cancel();if(SystemParameters.ClientAreaAnimation&&Environment.TickCount64-_lastMove<100&&_velocity.Length>.015){_lastTick=Environment.TickCount64;_inertia.Start();}e.Handled=true;}
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);StopMotion();
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
