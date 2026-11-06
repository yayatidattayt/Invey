using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Pocket.Core;

namespace Pocket.Overlay;

public sealed class GhostOverlay : Window
{
    private readonly Canvas _canvas=new();
    private readonly Border _panel;
    private readonly Ellipse _ghost;
    private HwndSource? _source;
    public InteractionState Scene { get; private set; } = null!;
    public nint Handle { get; private set; }
    public long VerifiedStyles { get; private set; }
    public MonitorGeometry Monitor { get; private set; } = null!;
    public uint Dpi { get; private set; }
    public event EventHandler? SurfaceInvalidated;

    public GhostOverlay()
    {
        Title="GHOST passive overlay";
        WindowStyle=WindowStyle.None; ResizeMode=ResizeMode.NoResize; AllowsTransparency=true;
        Background=Brushes.Transparent; ShowInTaskbar=false; ShowActivated=false; Topmost=true;
        IsHitTestVisible=false; Focusable=false;
        _panel=new Border { Width=300,Height=160,CornerRadius=new(11),BorderThickness=new(1),
            BorderBrush=SystemColors.WindowTextBrush, Background=SystemColors.WindowBrush,
            Child=new StackPanel { Margin=new(20), Children={
                new TextBlock { Text="POCKET",FontSize=15,FontWeight=FontWeights.SemiBold,Foreground=SystemColors.WindowTextBrush },
                new TextBlock { Text="Simulated reference · Gate A1",Margin=new(0,12,0,8),Foreground=SystemColors.WindowTextBrush },
                new TextBlock { Text="Your mouse passes through this panel.",TextWrapping=TextWrapping.Wrap,Foreground=SystemColors.WindowTextBrush }
            }} };
        _ghost=new Ellipse { Width=18,Height=18,Stroke=SystemColors.HighlightBrush,StrokeThickness=3,Fill=SystemColors.WindowBrush };
        Content=_canvas; _canvas.Children.Add(_panel); _canvas.Children.Add(_ghost);
    }
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Handle=new WindowInteropHelper(this).Handle;
        _source=HwndSource.FromHwnd(Handle); _source?.AddHook(WindowMessage);
        VerifiedStyles=NativeWindow.ConfigurePassive(Handle);
        Monitor=NativeWindow.PrimaryMonitor(); Dpi=NativeWindow.Dpi(Handle);
        NativeWindow.Position(Handle,Monitor);
        var bounds=new DipRect(0,0,Monitor.Width*96d/Dpi,Monitor.Height*96d/Dpi);
        Scene=new(bounds,new Dictionary<string,DipRect>{{"panel",new DipRect(120,180,300,160).Clamp(bounds)}});
        Scene.Move(PointerOwner.Ghost,new(260,240)); RenderScene();
    }
    private nint WindowMessage(nint hwnd,int msg,nint wParam,nint lParam,ref bool handled)
    {
        if(msg==0x21) { handled=true; return new nint(3); } // WM_MOUSEACTIVATE -> MA_NOACTIVATE
        if(msg is 0x02E0 or 0x007E) Dispatcher.BeginInvoke(new Action(()=>SurfaceInvalidated?.Invoke(this,EventArgs.Empty)));
        return 0;
    }
    public void RenderScene()
    {
        if(Scene is null) return;
        var panel=Scene.Target("panel"); var pointer=Scene.Pointer(PointerOwner.Ghost);
        _panel.Width=panel.Width; _panel.Height=panel.Height;
        Canvas.SetLeft(_panel,panel.X); Canvas.SetTop(_panel,panel.Y);
        Canvas.SetLeft(_ghost,pointer.Position.X-9); Canvas.SetTop(_ghost,pointer.Position.Y-9);
        _ghost.Fill=pointer.Target is null ? SystemColors.WindowBrush : SystemColors.HighlightBrush;
    }
    protected override void OnClosed(EventArgs e)
    {
        Scene?.Cancel(PointerOwner.Ghost);
        _source?.RemoveHook(WindowMessage); _source=null;
        base.OnClosed(e);
    }
}
