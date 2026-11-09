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
    public Pocket.Core.Tracking.GhostGestureCoordinator Gestures {get;}
    internal object? GestureReplayEvidence {get;set;}
    private readonly DispatcherTimer _timer = new() { Interval=TimeSpan.FromMilliseconds(40) };
    private PointerWindow? _overlay;
    private Window? _controls;
    private CheckBox? _keyboardSteering;
    private long _controlledSequence;
    private bool _controlled;
    private System.Drawing.Rectangle? _controlledBounds;
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
        Gestures=new(Engine);
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
    public void StartControlled(string? displayName=null)
    {
        TrackingEnabled=false;
        _controlled=true; Engine.Begin(GhostSourceKind.Controlled); _controlledSequence=0;
        var screen=System.Windows.Forms.Screen.AllScreens.FirstOrDefault(s=>s.DeviceName==displayName)??System.Windows.Forms.Screen.PrimaryScreen!;
        var b=screen.Bounds;_controlledBounds=b;
        MoveControlled(new(b.X+b.Width/2d,b.Y+b.Height/2d));
    }
    public void MoveControlled(DipPoint position)
    {
        if(!_controlled || Engine.Session is not { } session) return;
        var watch=Stopwatch.StartNew();
        var screen=_controlledBounds??System.Windows.Forms.Screen.FromPoint(new((int)position.X,(int)position.Y)).Bounds;
        position=new(Math.Clamp(position.X,screen.Left,screen.Right-1),Math.Clamp(position.Y,screen.Top,screen.Bottom-1));
        Engine.Observe(new(session,_controlledSequence++,Environment.TickCount64,position,GhostProvenance.Controlled));
        LastMovementMs=watch.Elapsed.TotalMilliseconds;MaxMovementMs=Math.Max(MaxMovementMs,LastMovementMs);PointerUpdates++;
    }
    public void TrackingPosition(Guid source, long sequence, DipPoint? point, GhostProvenance evidence, long age, GhostSourceKind kind)
    {
        if(_controlled || !TrackingEnabled) return;
        _trackingKind=kind;
        if(_trackingSource!=source || Engine.Session is null || Engine.Source!=kind)
        { Engine.Begin(_trackingKind); _trackingSource=source; }
        if(point is null) { Engine.Lose("Tracking lost — released"); return; }
        Engine.Observe(new(Engine.Session!.Value,sequence,Environment.TickCount64-age,point.Value,evidence));
    }
    private Guid? _trackingSource;
    private GhostSourceKind _trackingKind;
    public void EnableTracking(GhostSourceKind kind) { Stop(); _trackingKind=kind; TrackingEnabled=true; }
    public void TrackingStopped(){Gestures.Stop();_trackingSource=null;Engine.Stop();}
    public void Stop() { Gestures.Stop();_controlled=false;_controlledBounds=null;TrackingEnabled=false;_trackingSource=null;Engine.Stop();Stopped?.Invoke(); }
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
        _overlay.Show(); _overlay.Draw(p,Engine.Mode,Engine.Source==GhostSourceKind.Controlled&&Engine.HoverTarget is not null);
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
        var panel=new StackPanel { Margin=new(18) };
        panel.Children.Add(new TextBlock { Text="Try Ghost · controlled pointer", FontSize=20,FontWeight=FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text="Move this separate pointer over Pocket. It never moves the Windows cursor.",TextWrapping=TextWrapping.Wrap,Margin=new(0,4,0,10) });
        var display=new ComboBox { MinWidth=230,DisplayMemberPath=nameof(DisplayChoice.Label),ItemsSource=System.Windows.Forms.Screen.AllScreens.Select(s=>new DisplayChoice(s.DeviceName,$"{s.DeviceName} · {s.Bounds.Width} × {s.Bounds.Height}")).ToArray() };
        display.SelectedItem=display.Items.OfType<DisplayChoice>().FirstOrDefault(x=>x.Name==System.Windows.Forms.Screen.PrimaryScreen?.DeviceName);
        panel.Children.Add(new TextBlock { Text="Display" });panel.Children.Add(display);
        var buttons=new List<Button>();
        Button AddButton(Panel parent,string label,Action action){var b=new Button{Content=label,Margin=new(2),Padding=new(8,5,8,5),MinWidth=78};b.Click+=(_,_)=>action();parent.Children.Add(b);buttons.Add(b);return b;}
        var primary=new WrapPanel{Margin=new(0,8,0,4)};panel.Children.Add(primary);
        AddButton(primary,"Start preview",()=>StartControlled((display.SelectedItem as DisplayChoice)?.Name));
        AddButton(primary,"Stop Ghost",Stop);
        var armRow=new WrapPanel{Margin=new(0,0,0,8)};panel.Children.Add(armRow);
        AddButton(armRow,"Arm Pocket",()=>Engine.Arm());AddButton(armRow,"Disarm",Engine.Disarm);
        var keyboard=new CheckBox{Content="Enable keyboard steering in this window",Margin=new(2,5,2,5),IsThreeState=false};_keyboardSteering=keyboard;panel.Children.Add(keyboard);
        panel.Children.Add(new TextBlock{Text="With this panel focused: WASD or arrows move Ghost. Shift makes small adjustments; Ctrl moves farther. Space grabs/releases. Escape stops immediately. Steering handles keys only in this panel.",TextWrapping=TextWrapping.Wrap,Margin=new(0,2,0,8)});
        var direction=new Grid{HorizontalAlignment=HorizontalAlignment.Left};
        for(int i=0;i<3;i++){direction.ColumnDefinitions.Add(new(){Width=GridLength.Auto});direction.RowDefinitions.Add(new(){Height=GridLength.Auto});}
        void AddMove(string label,int x,int y,int row,int column){var b=AddButton(direction,label,()=>Step(x,y));Grid.SetRow(b,row);Grid.SetColumn(b,column);}
        AddMove("↑",0,-16,0,1);AddMove("←",-16,0,1,0);AddMove("↓",0,16,1,1);AddMove("→",16,0,1,2);panel.Children.Add(direction);
        var status=new TextBlock { TextWrapping=TextWrapping.Wrap, Margin=new(0,8,0,2),FontWeight=FontWeights.SemiBold }; panel.Children.Add(status);
        var targetHint=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(0,0,0,8)};panel.Children.Add(targetHint);
        var grabRow=new WrapPanel();panel.Children.Add(grabRow);
        AddButton(grabRow,"Grab / press",()=>Intent(GhostIntent.Press));
        AddButton(grabRow,"Release",()=>Intent(GhostIntent.Release));
        var pinRow=new WrapPanel();panel.Children.Add(pinRow);
        AddButton(pinRow,"Pin",()=>Command(GhostCommand.Pin));AddButton(pinRow,"Unpin",()=>Command(GhostCommand.Unpin));AddButton(pinRow,"Hide card",()=>Command(GhostCommand.Hide));
        var targets=new ComboBox { MinWidth=240,DisplayMemberPath=nameof(TargetChoice.Label) };
        var advanced=new Expander{Header="Accessibility and advanced controls",Margin=new(0,8,0,0)};
        var advancedPanel=new StackPanel();advanced.Content=advancedPanel;
        var targetRow=new WrapPanel();advancedPanel.Children.Add(targetRow);targetRow.Children.Add(targets);
        AddButton(targetRow,"Refresh targets",()=>{targets.ItemsSource=Engine.Targets.Select(t=>new TargetChoice(t.Id,_labels.GetValueOrDefault(t.Id,t.Id))).ToArray();targets.SelectedIndex=0;});
        AddButton(targetRow,"Aim at target",()=>{if(targets.SelectedItem is TargetChoice choice) Aim(choice.Id);});
        var commandRow=new WrapPanel();advancedPanel.Children.Add(commandRow);AddButton(commandRow,"Activate",()=>Command(GhostCommand.Activate));
        panel.Children.Add(advanced);
        _controls=new Window {Title="Invey · Try Ghost",Width=460,SizeToContent=SizeToContent.Height,MinWidth=380,Content=panel,WindowStartupLocation=WindowStartupLocation.CenterScreen};
        void Update()
        {
            status.Text=Engine.Status;
            string target=Engine.HoverTarget is { } id?_labels.GetValueOrDefault(id,id):"No supported Pocket target under Ghost.";
            targetHint.Text=Engine.Mode==GhostControlMode.Holding?"Holding: "+_labels.GetValueOrDefault(Engine.ActiveTarget??"",Engine.ActiveTarget??"target"):
                Engine.Mode==GhostControlMode.Armed?"Armed · Target under Ghost: "+target:"Aim: "+target;
            var available=Engine.AvailableCommands;
            foreach(var button in buttons)
            {
                string label=(string)button.Content;
                button.IsEnabled=label switch {
                    "Arm Pocket"=>_controlled&&Engine.Mode==GhostControlMode.Preview&&Engine.Position.HasValue,
                    "Grab / press"=>Engine.Mode==GhostControlMode.Armed,
                    "Release"=>Engine.Mode==GhostControlMode.Holding,
                    "Activate" or "Pin" or "Unpin" or "Hide card"=>Engine.Mode==GhostControlMode.Armed&&available.Contains(label switch{"Pin"=>GhostCommand.Pin,"Unpin"=>GhostCommand.Unpin,"Hide card"=>GhostCommand.Hide,_=>GhostCommand.Activate}),
                    "↑" or "←" or "↓" or "→"=>_controlled&&Engine.Position.HasValue,
                    _=>true
                };
            }
        }
        Engine.Changed+=Update; Update();
        _controls.Closed+=(_,_)=>{Engine.Changed-=Update;_keyboardSteering=null; _controls=null; Stop();}; _controls.Show();_controls.Activate();
        _controls.PreviewKeyDown+=(_,e)=>
        {
            if(!_controls.IsActive)return;
            if(e.Key==System.Windows.Input.Key.Escape){Stop();keyboard.IsChecked=false;e.Handled=true;return;}
            if(keyboard.IsChecked!=true||!_controlled)return;
            var delta=e.Key switch{System.Windows.Input.Key.W or System.Windows.Input.Key.Up=>(0,-16),System.Windows.Input.Key.A or System.Windows.Input.Key.Left=>(-16,0),System.Windows.Input.Key.S or System.Windows.Input.Key.Down=>(0,16),System.Windows.Input.Key.D or System.Windows.Input.Key.Right=>(16,0),_=>default};
            int scale=(System.Windows.Input.Keyboard.Modifiers&System.Windows.Input.ModifierKeys.Control)!=0?8:(System.Windows.Input.Keyboard.Modifiers&System.Windows.Input.ModifierKeys.Shift)!=0?1:4;
            if(delta!=(0,0)){Step(delta.Item1*scale,delta.Item2*scale);e.Handled=true;}
            else if(e.Key==System.Windows.Input.Key.Space){if(!e.IsRepeat&&Engine.Mode==GhostControlMode.Armed)Intent(GhostIntent.Press);e.Handled=true;}
        };
        _controls.PreviewKeyUp+=(_,e)=>{if(_controls.IsActive&&keyboard.IsChecked==true&&e.Key==System.Windows.Input.Key.Space&&Engine.Mode==GhostControlMode.Holding){Intent(GhostIntent.Release);e.Handled=true;}};
        _controls.Deactivated+=(_,_)=>{if(Engine.Mode==GhostControlMode.Holding)Engine.Disarm();};
    }
    internal void PositionControlsForAcceptance(double left,double top)
    {
        AcceptanceHost.ValidateDesktop();
        if(_controls is null || !double.IsFinite(left) || !double.IsFinite(top))
            throw new InvalidOperationException("Controlled Ghost window is not available for isolated acceptance positioning.");
        _controls.Left=left;_controls.Top=top;
    }
    private sealed record TargetChoice(string Id,string Label);
    private sealed record DisplayChoice(string Name,string Label);
    internal void ClickControlFixture(string label)
    {
        AcceptanceHost.ValidateDesktop();
        if(_controls?.Content is not StackPanel panel)throw new InvalidOperationException("Controlled Ghost window is not open.");
        if(label=="Arm Pocket control")label="Arm Pocket";
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
        KeyboardSteeringPoint=_keyboardSteering is {IsVisible:true} key?_keyboardSteering.PointToScreen(new(key.ActualWidth/2,key.ActualHeight/2)):(Point?)null,
        GestureReplay=GestureReplayEvidence,
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
        private System.Drawing.Rectangle? _screenBounds;
        public Point? RenderedPoint => IsVisible && _dot.IsVisible ? _dot.PointToScreen(new(9,9)) : null;
        public PointerWindow(Action invalidated)
        {
            Title="Invey · Ghost pointer";WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;
            Background=Brushes.Transparent;ShowInTaskbar=false;ShowActivated=false;Topmost=true;IsHitTestVisible=false;Focusable=false;
            Content=_canvas;_canvas.Children.Add(_reveal);_canvas.Children.Add(_dot);
            SourceInitialized+=(_,_)=>{Styles=NativeWindow.ConfigurePassive(new WindowInteropHelper(this).Handle);ScreenSnapshot.DisableNativeTransitions(this);};
            DpiChanged+=(_,_)=>{_screenBounds=null;invalidated();};
        }
        public void Draw(DipPoint physical,GhostControlMode mode,bool hovering)
        {
            var screen=System.Windows.Forms.Screen.FromPoint(new((int)physical.X,(int)physical.Y)).Bounds;
            if(_screenBounds!=screen)
            {NativeWindow.Position(new WindowInteropHelper(this).Handle,new(screen.X,screen.Y,screen.Width,screen.Height));_screenBounds=screen;}
            var p=PointFromScreen(new(physical.X,physical.Y));Canvas.SetLeft(_dot,p.X-9);Canvas.SetTop(_dot,p.Y-9);
            var reveal=RevealBounds(physical);var a=PointFromScreen(new(reveal.X,reveal.Y));var b=PointFromScreen(new(reveal.X+reveal.Width,reveal.Y+reveal.Height));
            Canvas.SetLeft(_reveal,a.X);Canvas.SetTop(_reveal,a.Y);_reveal.Width=b.X-a.X;_reveal.Height=b.Y-a.Y;
            _dot.Stroke=mode switch{GhostControlMode.Holding=>Brushes.LimeGreen,GhostControlMode.Armed=>Brushes.DarkOrange,_=>SystemColors.HighlightBrush};
            _dot.Fill=mode==GhostControlMode.Holding?Brushes.LimeGreen:mode==GhostControlMode.Armed?Brushes.DarkOrange:Brushes.White;
            _dot.StrokeThickness=hovering?4:2;
        }
    }
}
