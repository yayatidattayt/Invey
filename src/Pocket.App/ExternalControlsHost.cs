using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Pocket.Automation.Client;
using Pocket.Automation.Contracts;
using Pocket.Core;
using Pocket.Overlay;

namespace Pocket.App;

/// <summary>Session-only semantic route from the one Ghost position; no camera or native input API.</summary>
internal sealed class ExternalControlsHost
{
    private readonly GhostDesktopHost _ghost;
    private readonly DispatcherTimer _timer=new(){Interval=TimeSpan.FromMilliseconds(250)};
    private readonly string _root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
    private System.Diagnostics.Process? _demo;
    private WorkerClient? _worker,_unreleased;
    private ExternalGrant? _grant;
    private CancellationTokenSource _cancel=new();
    private Task _work=Task.CompletedTask,_cleanup=Task.CompletedTask,_release=Task.CompletedTask;
    private long _consentEpoch;
    private bool _authorizing;
    private Window? _window,_highlight;
    private CheckBox? _enabled;
    private ComboBox? _windows,_patterns;
    private TextBlock? _status;
    private bool _armed,_unknown;
    private Reply? _hover;
    private DipPoint? _hoverPoint;
    private Guid? _hoverSession;
    private long _probes,_executions;
    private double _lastProbeMs,_lastExecuteMs;
    public string Status {get;private set;}="External controls off";
    public ExternalControlsHost(GhostDesktopHost ghost)
    {
        _ghost=ghost;
        _timer.Tick+=(_,_)=>{
            if(!_work.IsCompleted||!_cleanup.IsCompleted)return;
            if(_grant is not null)_work=ProbeAsync();
            else if(_worker is not null)_cleanup=StopAsync();
        };
        _timer.Start();
        _ghost.Stopped+=OnStopped;
    }
    private void OnStopped()=>_cleanup=StopFromUiAsync();
    private async Task StopFromUiAsync()
    {
        try{await StopAsync();}
        catch(Exception error)when(error is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {Feedback("Cleanup unconfirmed · new actions blocked; retry Stop");System.Diagnostics.Trace.TraceWarning("External cleanup failed: {0}",error.GetType().Name);}
    }
    private bool Controlled => _ghost.Engine.Source==GhostSourceKind.Controlled && _ghost.Engine.Session==_grant?.Generation &&
        _ghost.Engine.Mode is GhostControlMode.Preview or GhostControlMode.Armed && _ghost.Engine.Position.HasValue;
    private void Feedback(string text){Status=text;if(_status is not null)_status.Text=text;}
    public void Show()
    {
        if(_window is not null){_window.Show();_window.Activate();return;}
        var panel=new StackPanel{Margin=new(18)};
        panel.Children.Add(new TextBlock{Text="Experimental External Controls",FontSize=20,FontWeight=FontWeights.SemiBold});
        panel.Children.Add(new TextBlock{Text="Controlled Ghost only. Select a benign window and a semantic action. Camera/recorded input cannot activate it. Some apps change keyboard focus. No universal mouse clicks or external dragging.",TextWrapping=TextWrapping.Wrap,Margin=new(0,8,0,10)});
        _enabled=new(){Content="Enable external controls for this session"};panel.Children.Add(_enabled);
        _windows=new(){DisplayMemberPath="Label",MinHeight=28};panel.Children.Add(_windows);
        void Button(string name,Func<Task> action){var button=new Button{Content=name,Margin=new(0,5,0,0),Padding=new(8,5,8,5)};button.Click+=async(_,_)=>{try{await action();}catch(Exception e)when(e is InvalidOperationException or IOException or System.ComponentModel.Win32Exception or ArgumentException){Feedback("Unavailable · "+e.Message);}};panel.Children.Add(button);}
        Button("Open synthetic external demo",()=>{OpenDemo();return Task.CompletedTask;});
        Button("Find eligible windows",()=>{_windows.ItemsSource=ExternalWindows.List();return Task.CompletedTask;});
        _patterns=new(){ItemsSource=ExternalGrant.Patterns.ToArray(),SelectedItem="Invoke",MinHeight=28,Margin=new(0,8,0,0)};panel.Children.Add(_patterns);
        Button("Authorize selected window",async()=>{
            if(_enabled.IsChecked!=true||_windows.SelectedItem is not ExternalWindows.Choice choice){Feedback("Enable external controls and select one window first.");return;}
            await AuthorizeAsync(choice,(string)_patterns.SelectedItem);
        });
        Button("Arm external action",()=>{Arm();return Task.CompletedTask;});
        Button("Activate current action",async()=>{await ActivateAsync();});
        Button("Review uncertain outcome",async()=>{if(_worker is not null&&_unknownId is not null){var r=await _worker.ReconcileAsync(_unknownId);Feedback("Outcome review: "+r.Observation+" · no automatic retry");}else Feedback(_unknown?"Outcome unknown · inspect the selected app before dismissing.":"No uncertain action.");});
        Button("Dismiss reviewed outcome",DismissAsync);
        Button("Stop external controls",StopAsync);
        _status=new(){Text=Status,TextWrapping=TextWrapping.Wrap,Margin=new(0,12,0,0)};panel.Children.Add(_status);
        _patterns.SelectionChanged+=async(_,_)=>await StopFromUiAsync();
        _enabled.Unchecked+=async(_,_)=>await StopFromUiAsync();
        _window=new(){Title="Invey · External Controls",Width=430,Height=610,MinWidth=360,Content=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto}};
        _window.Closed+=async(_,_)=>{_window=null;await StopFromUiAsync();};_window.Show();
    }
    internal void OpenDemoThroughUiFixture()
    {
        AcceptanceHost.ValidateDesktop();Show();
        if(_enabled!.IsChecked==true)throw new InvalidOperationException("External controls did not start off by default.");
        var panel=(StackPanel)((ScrollViewer)_window!.Content).Content;
        panel.Children.OfType<Button>().Single(b=>Equals(b.Content,"Open synthetic external demo")).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
    }
    public void OpenDemo()
    {
        if(_demo is {HasExited:false}){Feedback("Synthetic demo is already open. Find windows to select it.");return;}
        _demo?.Dispose();
        string path=Path.Combine(_root,"src","Invey.ExternalDemo","bin","Release","net10.0-windows","Invey.ExternalDemo.exe");
        if(!File.Exists(path))throw new IOException("Build Invey.sln Release to include the separate demo application.");
        var info=new System.Diagnostics.ProcessStartInfo(path){UseShellExecute=false,CreateNoWindow=true};
        info.Environment["DOTNET_ROOT"]=Path.Combine(_root,".tools","dotnet");
        _demo=System.Diagnostics.Process.Start(info)??throw new IOException("Demo did not start.");
        Feedback("Synthetic demo opened · find windows, select it and authorize a pattern.");
    }
    public async Task ActivateFromUiAsync()
    {
        try{await ActivateAsync();}
        catch(Exception error)when(error is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {Feedback("External action unavailable · "+error.GetType().Name);System.Diagnostics.Trace.TraceWarning("External action failed: {0}",error.GetType().Name);}
    }
    public async Task AuthorizeAsync(ExternalWindows.Choice choice,string pattern)
    {
        if(_authorizing){Feedback("Busy · window authorization is already pending");return;}
        _authorizing=true;
        try
        {
            await StopAsync();
            if(_unknown){Feedback("Review/dismiss the previous uncertain outcome before new actions.");return;}
            if(_ghost.Engine.Source!=GhostSourceKind.Controlled || _ghost.Engine.Session is not {} session || _ghost.Engine.Position is null)
            {Feedback("Start controlled Ghost preview first. Camera and recorded input are preview-only.");return;}
            long epoch=_consentEpoch;
            var grant=await Task.Run(()=>ExternalWindows.Consent(choice,session,[pattern]));
            if(epoch!=_consentEpoch||_ghost.Engine.Source!=GhostSourceKind.Controlled||_ghost.Engine.Session!=session)return;
            string dotnet=Path.Combine(_root,".tools","dotnet","dotnet.exe"),worker=Path.Combine(_root,"src","Pocket.Automation.Worker","bin","Release","net10.0-windows","Pocket.Automation.Worker.dll");
            _worker=new(dotnet,worker,grant);_grant=grant;_cancel.Dispose();_cancel=new();
            Feedback("Authorized selected window · disarmed · "+pattern);
        }
        finally{_authorizing=false;}
    }
    public bool Arm()
    {
        if(!Controlled||_worker is null||_unknown){Feedback("Unavailable · controlled source, current window grant and resolved prior action required.");return false;}
        _armed=true;Feedback("External action armed · move Ghost to a supported control");return true;
    }
    private Request Request(string operation,DipPoint p,string? token=null,double? value=null) =>new(2,Guid.NewGuid().ToString(),operation,
        _grant!.Window,_grant.ProcessId,_grant.Actions[0],token,value,p.X,p.Y,Grant:_grant.Id,Generation:_grant.Generation);
    public async Task<Reply?> ProbeAsync()
    {
        _hover=null;_highlight?.Hide();
        if(!Controlled||_worker is null||_ghost.Engine.HoverTarget is not null||_unknown)
        {
            if(_grant is not null&&!Controlled){_armed=false;_grant=null;_cancel.Cancel();Feedback("Preview only · source/session changed; reauthorize controlled Ghost.");}
            return null;
        }
        var point=_ghost.Engine.Position!.Value;var session=_ghost.Engine.Session;var worker=_worker;
        var measure=System.Diagnostics.Stopwatch.StartNew();
        var reply=await worker.RequestAsync(Request("ProbePoint",point),_cancel.Token);
        _lastProbeMs=measure.Elapsed.TotalMilliseconds;_probes++;
        if(worker!=_worker||!Controlled||_ghost.Engine.Position!=point||session!=_ghost.Engine.Session)return null;
        if(reply.Status=="Confirmed")
        {
            _hover=reply;_hoverPoint=point;_hoverSession=session;
            Feedback(($"{(_armed?"Armed":"Disarmed")} · {reply.Pattern}{(reply.Pattern=="RangeValue"?" · set midpoint":"")} · {reply.Label}"));
            if(reply.Binding is {} b)Highlight(b);
        }
        else {
            Feedback(($"{(_armed?"Armed":"Disarmed")} · {reply.Status} · no action"));
            if(reply.Status is "Denied" or "Unavailable"){_armed=false;_grant=null;_cancel.Cancel();}
        }
        return reply;
    }
    public async Task<Reply?> ReviewAsync()
    {
        if(_worker is null||_unknownId is null)return null;
        var reply=await _worker.ReconcileAsync(_unknownId,_cancel.Token);Feedback("Outcome review: "+reply.Observation+" · no automatic retry");return reply;
    }
    public async Task DismissAsync()
    {
        if(!_work.IsCompleted){Feedback("Action is still pending · wait or Stop before reviewing");return;}
        if(_worker is not null&&_unknownId is not null && !await _worker.DismissAsync(_unknownId)){Feedback("Outcome review is busy");return;}
        _unknown=false;_unknownId=null;Feedback("Reviewed outcome dismissed. Reauthorize and arm deliberately.");
    }
    internal void PauseProbingForFixture(){AcceptanceHost.ValidateDesktop();_timer.Stop();}
    private string? _unknownId;
    public async Task<Reply?> ActivateAsync(double? value=null)
    {
        if(!_work.IsCompleted || !_cleanup.IsCompleted){Feedback("Busy · wait for current probe/action");return null;}
        if(!_armed||!Controlled||_unknown||_ghost.Engine.HoverTarget is not null){Feedback("Disarmed or unsupported source/target · no action");return null;}
        // Fresh probe at the shared point, then one token, one dispatch. Never execute a cached hover.
        var action=ExecuteCore(value);_work=action;return await action;
    }
    private async Task<Reply?> ExecuteCore(double? value)
    {
        var reply=await ProbeAsync();var worker=_worker;
        if(reply?.Token is null||worker is null||!_armed||!Controlled||_hoverSession!=_ghost.Engine.Session||_hoverPoint!=_ghost.Engine.Position)return null;
        if(reply.Pattern=="RangeValue"&&value is null)
        {
            if(reply.Binding?.Minimum is not double min||reply.Binding.Maximum is not double max){Feedback("Unsupported range");return null;}
            value=min+(max-min)/2; // Explicit bounded midpoint action, never a guessed slider drag.
        }
        var request=Request("Execute",_hoverPoint!.Value,reply.Token,value);
        var measure=System.Diagnostics.Stopwatch.StartNew();
        var result=await worker.RequestAsync(request,_cancel.Token);
        _lastExecuteMs=measure.Elapsed.TotalMilliseconds;_executions++;
        if(result.Outcome==MutationOutcome.Unknown){_unknown=true;_unknownId=request.Id;_armed=false;}
        _hover=null;_highlight?.Hide();
        Feedback(result.Outcome==MutationOutcome.Applied?"State change observed · "+result.Pattern:
            result.Outcome==MutationOutcome.Unknown?"Outcome unknown · inspect/review; do not retry automatically":result.Status+" · no confirmed change");
        return result;
    }
    public Task StopAsync()
    {
        _consentEpoch++;_armed=false;_grant=null;_hover=null;_highlight?.Hide();_cancel.Cancel();
        // Detach immediately; an older cleanup continuation can never erase a newer worker.
        var worker=_worker??_unreleased;_worker=null;_unreleased=null;var prior=_release;var work=_work;
        if(work.IsFaulted)
        {
            System.Diagnostics.Trace.TraceWarning("External operation failed before cleanup: {0}",work.Exception?.GetBaseException().GetType().Name);
            work=Task.CompletedTask;_work=Task.CompletedTask;
        }
        if(prior.IsFaulted)
        {
            System.Diagnostics.Trace.TraceWarning("Retrying external cleanup after observed failure: {0}",prior.Exception?.GetBaseException().GetType().Name);
            prior=Task.CompletedTask;
        }
        return _release=ReleaseAsync(prior,work,worker);
    }
    private async Task ReleaseAsync(Task prior,Task work,WorkerClient? worker)
    {
        try { await prior;await work; }
        finally
        {
            if(worker is not null)
            {
                try{await worker.DisposeAsync();}
                catch(IOException){_unreleased=worker;Feedback("Worker exit unconfirmed · new actions blocked");throw;}
            }
        }
        if(_grant is null)Feedback(_unknown?"Stopped · prior outcome remains unknown":"External controls stopped · grants revoked");
    }
    private void Highlight(TargetBinding b)
    {
        if(_highlight is null)
        {
            _highlight=new(){Title="Invey · External target",WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,AllowsTransparency=true,Background=Brushes.Transparent,
                ShowInTaskbar=false,ShowActivated=false,Focusable=false,IsHitTestVisible=false,Topmost=true,
                Content=new Border{BorderBrush=SystemColors.HighlightBrush,BorderThickness=new(2),Background=Brushes.Transparent}};
            _highlight.SourceInitialized+=(_,_)=>NativeWindow.ConfigurePassive(new WindowInteropHelper(_highlight).Handle);
        }
        _highlight.Show();NativeWindow.Position(new WindowInteropHelper(_highlight).Handle,new((int)b.Left,(int)b.Top,(int)b.Width,(int)b.Height));
    }
    public async Task CloseAsync()
    {
        _timer.Stop();await StopAsync();_ghost.Stopped-=OnStopped;
        _window?.Close();_window=null;_highlight?.Close();_highlight=null;
        if(_demo is {} demo)
        {
            await Task.Run(()=>{if(!demo.HasExited){demo.CloseMainWindow();if(!demo.WaitForExit(2000)){demo.Kill();if(!demo.WaitForExit(1000))throw new IOException("Owned demo cleanup unconfirmed.");}}});
            demo.Dispose();_demo=null;
        }
    }
    public object State=>new{Status,OwnedDemoPid=_demo is {HasExited:false}?_demo.Id:(int?)null,Enabled=_enabled?.IsChecked==true,Busy=_authorizing||!_work.IsCompleted||!_cleanup.IsCompleted||!_release.IsCompleted,ProbeCount=_probes,ExecutionCount=_executions,LastProbeMs=_lastProbeMs,LastExecuteMs=_lastExecuteMs,Armed=_armed,Unknown=_unknown,Granted=_grant is not null,Session=_grant?.Generation,
        Hover=_hover?.Pattern,HoverBounds=_hover?.Binding,HighlightVisible=_highlight?.IsVisible==true,Worker=(_worker??_unreleased)?.ActiveProcessId};
}
