using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Pocket.Core;
using Pocket.Overlay;
using Pocket.App.References;

namespace Pocket.App;

/// <summary>Application-owned dispatcher boundary. Never injects Windows input.</summary>
internal sealed class GhostDesktopHost : IDisposable
{
    public GhostDesktop Engine { get; } = new(()=>Environment.TickCount64);
    private readonly DispatcherTimer _timer = new() { Interval=TimeSpan.FromMilliseconds(40) };
    private PointerWindow? _overlay;
    private Window? _controls;
    private long _controlledSequence;
    private bool _controlled;
    public bool TrackingEnabled { get; private set; }
    private string _displaySignature="";
    private bool _disposed;
    private bool _revealRegistered;
    private readonly Dictionary<string,string> _labels=[];
    public event Action? Stopped;
    public double LastDispatchMs { get; private set; }
    public double LastMovementMs { get; private set; }
    public double MaxMovementMs { get; private set; }
    public double LastMappingMs { get; set; }
    public long PointerUpdates { get; private set; }
    public GhostDesktopHost()
    {
        Engine.WindowReachable=Reachable;
        Engine.Changed+=Render;
        _timer.Tick+=(_,_)=>
        {
            var signature=string.Join(";",System.Windows.Forms.Screen.AllScreens.Select(s=>s.Bounds.ToString()));
            if(_displaySignature!="" && signature!=_displaySignature) { Stop(); _displaySignature=signature; return; }
            _displaySignature=signature;
            if(_controlled && Engine.Position is { } p) MoveControlled(p);
            Engine.Tick();
        };
        _timer.Start();
    }
    public void StartControlled()
    {
        TrackingEnabled=false;
        _controlled=true; Engine.Begin(GhostSourceKind.Controlled); _controlledSequence=0;
        var b=System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
        MoveControlled(new(b.X+b.Width/2d,b.Y+b.Height/2d));
    }
    public void MoveControlled(DipPoint position)
    {
        if(!_controlled || Engine.Session is not { } session) return;
        var watch=Stopwatch.StartNew();
        var screen=System.Windows.Forms.Screen.FromPoint(new((int)position.X,(int)position.Y)).Bounds;
        position=new(Math.Clamp(position.X,screen.Left,screen.Right-1),Math.Clamp(position.Y,screen.Top,screen.Bottom-1));
        Engine.Observe(new(session,_controlledSequence++,Environment.TickCount64,position,GhostProvenance.Controlled));
        LastMovementMs=watch.Elapsed.TotalMilliseconds;MaxMovementMs=Math.Max(MaxMovementMs,LastMovementMs);PointerUpdates++;
    }
    public void TrackingPosition(Guid source, long sequence, DipPoint? point, GhostProvenance evidence, long age, GhostSourceKind kind)
    {
        if(_controlled || !TrackingEnabled) return;
        _trackingKind=kind;
        if(_trackingSource!=source || Engine.Session is null)
        { Engine.Begin(_trackingKind); _trackingSource=source; }
        if(point is null) { Engine.Lose("Tracking lost — released"); return; }
        Engine.Observe(new(Engine.Session!.Value,sequence,Environment.TickCount64-age,point.Value,evidence));
    }
    private Guid? _trackingSource;
    private GhostSourceKind _trackingKind;
    public void EnableTracking(GhostSourceKind kind) { Stop(); _trackingKind=kind; TrackingEnabled=true; }
    public void TrackingStopped(){_trackingSource=null;Engine.Stop();}
    public void Stop() { _controlled=false; TrackingEnabled=false; _trackingSource=null; Engine.Stop(); Stopped?.Invoke(); }
    public bool Intent(GhostIntent intent)
    {
        var watch=Stopwatch.StartNew();
        bool result=Engine.Session is { } s && (intent==GhostIntent.Press ? Engine.Press(s,Engine.Sequence) : Engine.Release(s,Engine.Sequence));
        LastDispatchMs=watch.Elapsed.TotalMilliseconds; return result;
    }
    public bool Command(GhostCommand command)
    {var watch=Stopwatch.StartNew();bool result=Engine.Session is { } s && Engine.Execute(s,Engine.Sequence,command);LastDispatchMs=watch.Elapsed.TotalMilliseconds;return result;}
    public bool Aim(string id)
    {
        var target=Engine.Targets.FirstOrDefault(t=>t.Id==id);
        if(target.Geometry is null) return false;
        var b=target.Geometry.Bounds; MoveControlled(new(b.X+b.Width/2,b.Y+b.Height/2)); return true;
    }
    public IDisposable Register(string id, Window window, FrameworkElement element, bool drag,
        Func<DipPoint,bool>? begin=null, Action<DipPoint>? move=null, Action<bool>? end=null,
        Func<GhostCommand,bool>? command=null, int priority=0, Func<int>? priorityGetter=null, string? label=null,
        GhostCommand[]? supportedCommands=null)
    {
        Guid lifetime=Guid.NewGuid();
        _labels[id]=label??id;
        Engine.Register(new(id,lifetime,()=>Geometry(window,element,priorityGetter?.Invoke()??priority),drag,begin??(_=>true),
            move??(_=>{}),end??(_=>{}),command??(_=>false),supportedCommands??(drag?[]:[GhostCommand.Activate])));
        Registration? registration=null;
        EventHandler closed=(_,_)=>registration?.Dispose();
        registration=new Registration(()=>{window.Closed-=closed;Engine.Unregister(id,lifetime);_labels.Remove(id);});
        window.Closed+=closed;
        return registration;
    }
    public void RegisterReveal(Action reveal)
    {
        if(_revealRegistered)return;_revealRegistered=true;
        _labels["shelf:reveal"]="Reveal Pocket shelf";
        var lifetime=Guid.NewGuid();
        Engine.Register(new("shelf:reveal",lifetime,()=>Engine.Position is { } p
            ? new(RevealBounds(p),-1,100,0):null,false,_=>true,_=>{},_=>{},c=>
            {if(c!=GhostCommand.Activate)return false;reveal();return true;},[GhostCommand.Activate]));
    }
    private static DipRect RevealBounds(DipPoint p)
    {
        var b=System.Windows.Forms.Screen.FromPoint(new((int)p.X,(int)p.Y)).Bounds;
        return new(b.Right-60,b.Top+b.Height/2d-20,60,40);
    }
    private sealed class Registration(Action dispose) : IDisposable { private Action? _dispose=dispose; public void Dispose(){ var action=_dispose; _dispose=null; action?.Invoke(); } }
    internal static GhostTargetGeometry? Geometry(Window window,FrameworkElement element,int priority)
    {
        if(!window.IsVisible || window.WindowState==WindowState.Minimized || !element.IsVisible || !element.IsEnabled ||
            PresentationSource.FromVisual(element) is null || element.ActualWidth<=0 || element.ActualHeight<=0) return null;
        var a=element.PointToScreen(new(0,0)); var b=element.PointToScreen(new(element.ActualWidth,element.ActualHeight));
        var hwnd=new WindowInteropHelper(window).Handle;
        return new(new(a.X,a.Y,b.X-a.X,b.Y-a.Y),Z(hwnd),priority,hwnd.ToInt64());
    }
    private static int Z(nint hwnd) { int rank=0; for(var w=GetTopWindow(0); w!=0 && rank<2000; w=GetWindow(w,2),rank++) if(w==hwnd) return rank; return int.MaxValue; }
    private bool Reachable(long window,DipPoint p)
    {
        if(window==0)return true; // Only the explicitly registered passive Pocket affordance.
        // Observe real top-level stacking, skipping only transparent/passive windows. Never dispatch externally.
        int bounded=0;
        for(var w=GetTopWindow(0); w!=0 && bounded++<2000; w=GetWindow(w,2))
        {
            if(!IsWindowVisible(w) || (GetWindowLongPtr(w,-20).ToInt64()&0x20)!=0) continue;
            if(ScreenSnapshot.Bounds(w) is { } b && b.Contains((int)p.X,(int)p.Y)) return w.ToInt64()==window;
        }
        return false;
    }
    private void Render()
    {
        if(_disposed)return;
        try
        {
        if(Engine.Position is not { } p || Engine.Mode is GhostControlMode.Off or GhostControlMode.Paused or GhostControlMode.Lost)
        { _overlay?.Hide(); return; }
        _overlay??=new PointerWindow(()=>Engine.Lose("Display changed — released"));
        _overlay.Show(); _overlay.Draw(p,Engine.Mode==GhostControlMode.Holding);
        }
        catch(Exception error) when(error is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Trace.TraceWarning("Ghost pointer presentation failed: 0x{0:X8}",error.HResult);
            if(Engine.Position.HasValue)Engine.Lose("Pointer presentation unavailable — released");
        }
    }
    public void ShowControls()
    {
        if(_controls is not null) { _controls.Show(); _controls.Activate(); return; }
        var panel=new StackPanel { Margin=new(16) };
        panel.Children.Add(new TextBlock { Text="CONTROLLED Ghost · camera off", FontWeight=FontWeights.SemiBold });
        var status=new TextBlock { TextWrapping=TextWrapping.Wrap, Margin=new(0,8,0,8) }; panel.Children.Add(status);
        var buttons=new List<Button>();
        void Buttons(params (string Label,Action Action)[] actions) { var row=new WrapPanel(); foreach(var (label,action) in actions){var b=new Button{Content=label,Margin=new(2)}; b.Click+=(_,_)=>action();row.Children.Add(b);buttons.Add(b);} panel.Children.Add(row); }
        Buttons(("Start preview",StartControlled),("Arm Pocket control",()=>Engine.Arm()),("Disarm",Engine.Disarm),("Stop",Stop));
        var targets=new ComboBox { MinWidth=260,DisplayMemberPath=nameof(TargetChoice.Label) }; panel.Children.Add(targets);
        Buttons(("Refresh targets",()=>{targets.ItemsSource=Engine.Targets.Select(t=>new TargetChoice(t.Id,_labels.GetValueOrDefault(t.Id,t.Id))).ToArray();targets.SelectedIndex=0;}),
            ("Move Ghost to target",()=>{if(targets.SelectedItem is TargetChoice choice) Aim(choice.Id);}));
        Buttons(("←",()=>Step(-16,0)),("↑",()=>Step(0,-16)),("↓",()=>Step(0,16)),("→",()=>Step(16,0)));
        Buttons(("Press",()=>Intent(GhostIntent.Press)),("Release",()=>Intent(GhostIntent.Release)),
            ("Activate",()=>Command(GhostCommand.Activate)),("Pin",()=>Command(GhostCommand.Pin)),
            ("Unpin",()=>Command(GhostCommand.Unpin)),("Hide",()=>Command(GhostCommand.Hide)));
        panel.Children.Add(new TextBlock { Text="Start → choose a target → move Ghost → Arm → Press, move, Release. Only Invey targets respond. Tracking preview has no action authority.",TextWrapping=TextWrapping.Wrap });
        _controls=new Window {Title="Invey · Controlled Ghost",Width=430,SizeToContent=SizeToContent.Height,Content=panel};
        void Update()
        {
            status.Text=Engine.Status;
            var available=Engine.AvailableCommands;
            foreach(var button in buttons)
            {
                string label=(string)button.Content;
                button.IsEnabled=label switch {
                    "Arm Pocket control"=>_controlled&&Engine.Mode==GhostControlMode.Preview&&Engine.Position.HasValue,
                    "Press"=>Engine.Mode==GhostControlMode.Armed,
                    "Release"=>Engine.Mode==GhostControlMode.Holding,
                    "Activate" or "Pin" or "Unpin" or "Hide"=>Engine.Mode==GhostControlMode.Armed&&available.Contains(Enum.Parse<GhostCommand>(label)),
                    _=>true
                };
            }
        }
        Engine.Changed+=Update; Update();
        _controls.Closed+=(_,_)=>{Engine.Changed-=Update; _controls=null; Stop();}; _controls.Show();
        _controls.PreviewKeyDown+=(_,e)=>{if(e.Key==System.Windows.Input.Key.Escape){Stop();e.Handled=true;}};
    }
    internal void PositionControlsForAcceptance(double left,double top)
    {
        AcceptanceHost.ValidateDesktop();
        if(_controls is null || !double.IsFinite(left) || !double.IsFinite(top))
            throw new InvalidOperationException("Controlled Ghost window is not available for isolated acceptance positioning.");
        _controls.Left=left;_controls.Top=top;
    }
    private sealed record TargetChoice(string Id,string Label);
    internal void ClickControlFixture(string label)
    {
        AcceptanceHost.ValidateDesktop();
        if(_controls?.Content is not StackPanel panel)throw new InvalidOperationException("Controlled Ghost window is not open.");
        var button=panel.Children.OfType<WrapPanel>().SelectMany(row=>row.Children.OfType<Button>()).Single(b=>Equals(b.Content,label));
        if(!button.IsEnabled)throw new InvalidOperationException("Requested controlled UI action is disabled.");
        button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
    }
    private void Step(int x,int y){if(Engine.Position is { } p) MoveControlled(new(p.X+x,p.Y+y));}
    public object State => new { Mode=Engine.Mode.ToString(), Engine.Position, Engine.Status, Engine.ActiveTarget, Engine.Sequence,
        Generation=Engine.Session,
        WorkArea=System.Windows.Forms.Screen.FromPoint(new((int)(Engine.Position?.X??0),(int)(Engine.Position?.Y??0))).WorkingArea,
        Source=Engine.Source.ToString(), OverlayHandle=_overlay is null?0:new WindowInteropHelper(_overlay).Handle.ToInt64(),
        ControlsHandle=_controls is null?0:new WindowInteropHelper(_controls).Handle.ToInt64(),
        OverlayVisible=_overlay?.IsVisible==true, OverlayStyles=_overlay?.Styles??0,
        RenderedPoint=_overlay?.RenderedPoint,
        Targets=Engine.Targets.Select(t=>new{t.Id,t.Geometry.Bounds,t.Geometry.Window,t.Geometry.ZOrder}).ToArray(),LastDispatchMs,LastMovementMs,MaxMovementMs,LastMappingMs,PointerUpdates };
    public void Dispose(){if(_disposed)return;_disposed=true;Stop();_timer.Stop();_controls?.Close();_overlay?.Close();Engine.Changed-=Render;}
    [DllImport("user32.dll")] private static extern nint GetTopWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern nint GetWindow(nint hwnd,uint command);
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] private static extern nint GetWindowLongPtr(nint hwnd,int index);

    private sealed class PointerWindow : Window
    {
        private readonly Canvas _canvas=new(); private readonly Ellipse _dot=new(){Width=18,Height=18,Stroke=SystemColors.HighlightBrush,StrokeThickness=3,Fill=SystemColors.WindowBrush};
        private readonly Border _reveal=new(){Background=SystemColors.WindowBrush,BorderBrush=SystemColors.HighlightBrush,BorderThickness=new(1),Child=new TextBlock{Text="Pocket",Margin=new(4)}};
        public long Styles { get; private set; }
        public Point? RenderedPoint => IsVisible && _dot.IsVisible ? _dot.PointToScreen(new(9,9)) : null;
        public PointerWindow(Action invalidated)
        {
            Title="Invey · Ghost pointer";WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;
            Background=Brushes.Transparent;ShowInTaskbar=false;ShowActivated=false;Topmost=true;IsHitTestVisible=false;Focusable=false;
            Content=_canvas;_canvas.Children.Add(_reveal);_canvas.Children.Add(_dot);
            SourceInitialized+=(_,_)=>{Styles=NativeWindow.ConfigurePassive(new WindowInteropHelper(this).Handle);ScreenSnapshot.DisableNativeTransitions(this);};
            DpiChanged+=(_,_)=>invalidated();
        }
        public void Draw(DipPoint physical,bool held)
        {
            var screen=System.Windows.Forms.Screen.FromPoint(new((int)physical.X,(int)physical.Y)).Bounds;
            NativeWindow.Position(new WindowInteropHelper(this).Handle,new(screen.X,screen.Y,screen.Width,screen.Height));
            var p=PointFromScreen(new(physical.X,physical.Y));Canvas.SetLeft(_dot,p.X-9);Canvas.SetTop(_dot,p.Y-9);
            var reveal=RevealBounds(physical);var a=PointFromScreen(new(reveal.X,reveal.Y));var b=PointFromScreen(new(reveal.X+reveal.Width,reveal.Y+reveal.Height));
            Canvas.SetLeft(_reveal,a.X);Canvas.SetTop(_reveal,a.Y);_reveal.Width=b.X-a.X;_reveal.Height=b.Y-a.Y;
            _dot.Fill=held?SystemColors.HighlightBrush:SystemColors.WindowBrush;
        }
    }
}
