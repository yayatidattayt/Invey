using System.Diagnostics;
using System.Windows.Threading;
using Pocket.Core;
using Pocket.Overlay;

namespace Pocket.App;

public sealed record ReplayObservation(double ElapsedMs,string Action,bool Visible,NativeObservation Native,DipRect Panel,string? Claim);
public sealed class SimulationController : IDisposable
{
    private readonly GhostOverlay _overlay;
    private readonly DispatcherTimer _timer=new(DispatcherPriority.Input);
    private readonly Stopwatch _clock=new();
    private ReplaySession? _replay;
    private long _epoch;
    private bool _disposed;
    public bool IsRunning => _replay?.IsRunning==true;
    public bool IsScheduled=>_timer.IsEnabled;
    public int TimerTicks { get; private set; }
    public List<ReplayObservation> Observations { get; }=new();
    public event EventHandler? Completed;
    public event EventHandler<ReplayObservation>? ObservationReceived;
    public event EventHandler<Exception>? Failed;
    public SimulationController(GhostOverlay overlay)
    {
        _overlay=overlay; _timer.Interval=TimeSpan.FromMilliseconds(16); _timer.Tick+=Tick;
        _overlay.Closed+=OverlayClosed;
    }
    private void OverlayClosed(object? sender,EventArgs args)=>Stop();
    public bool Start(int cycles=20,int countdownMs=5000)
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        if(IsRunning) return false;
        _overlay.Scene.Cancel(PointerOwner.Ghost);
        _replay=ReplaySession.PanelRoute(_overlay.Scene.Target("panel"),_overlay.Scene.Bounds,cycles,countdownMs);
        _replay.Start(); _epoch=_replay.Epoch; Observations.Clear(); TimerTicks=0;
        _clock.Restart(); _timer.Start(); return true;
    }
    private void Tick(object? sender,EventArgs args)
    {
        try
        {
            TimerTicks++;
            foreach(var item in _replay!.Advance(_epoch,_clock.Elapsed))
            {
                switch(item.Action)
                {
                    case ReplayAction.Show: _overlay.Show(); break;
                    case ReplayAction.Hide: _overlay.Hide(); break;
                    case ReplayAction.Move: _overlay.Scene.Move(PointerOwner.Ghost,item.Position); break;
                    case ReplayAction.Press:
                        if(!_overlay.Scene.Press(PointerOwner.Ghost,"panel")) throw new InvalidOperationException("Replay missed its Pocket target.");
                        break;
                    case ReplayAction.Release: _overlay.Scene.Release(PointerOwner.Ghost); break;
                }
                _overlay.RenderScene();
                var observation=new ReplayObservation(_clock.Elapsed.TotalMilliseconds,item.Action.ToString(),_overlay.IsVisible,
                    NativeWindow.Observe(),_overlay.Scene.Target("panel"),_overlay.Scene.Pointer(PointerOwner.Ghost).Target);
                if(Observations.Count<2000) Observations.Add(observation);
                ObservationReceived?.Invoke(this,observation);
            }
            if(!_replay.IsRunning) { Stop(); Completed?.Invoke(this,EventArgs.Empty); }
        }
        catch(Exception ex) { Stop(); _overlay.Hide(); Failed?.Invoke(this,ex); }
    }
    public void Stop()
    {
        _timer.Stop(); _clock.Stop(); _replay?.Stop(); _overlay.Scene.Cancel(PointerOwner.Ghost); _overlay.RenderScene();
    }
    public void Dispose()
    {
        if(_disposed) return;
        Stop(); _timer.Tick-=Tick; _overlay.Closed-=OverlayClosed; _disposed=true;
    }
}
