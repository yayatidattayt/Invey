using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Pocket.Core.Tracking;

namespace Pocket.App.Tracking;

/// <summary>Local, nonpersistent open/pinch/release lesson. Never grants action authority.</summary>
internal sealed class GesturePracticePanel : StackPanel
{
    private readonly Button _next=new(){Content="Begin gesture practice",HorizontalAlignment=HorizontalAlignment.Left};
    private readonly TextBlock _hint=new(){Text="Pocket interaction disabled · practice only",TextWrapping=TextWrapping.Wrap};
    private readonly List<double> _open=[],_pinch=[],_release=[];
    private LearningContext? _context;
    private Guid? _session;
    private long _sequence=-1,_capture=-1;
    private int _stage;
    public event Action<Guid,LearningContext,GhostGestureCalibration>? Ready;
    public GesturePracticePanel()
    {
        Children.Add(_next);Children.Add(_hint);
        _next.Click+=(_,_)=>
        {
            if(_stage==0||_stage==4){Reset();_stage=1;}
            else if(_stage==1&&_open.Count>=12)_stage=2;
            else if(_stage==2&&_pinch.Count>=12)_stage=3;
            else if(_stage==3&&_release.Count>=12&&_context is {} c&&_session is {} s)
            {
                var calibration=GhostGestureCalibration.FromPracticeSamples(c.Hand==1?HandSide.Left:HandSide.Right,_pinch,_open.Concat(_release));
                if(calibration is null){Reset();_hint.Text="Samples overlap or are unstable. Begin again with a comfortable open hand and deliberate pinch.";return;}
                _stage=4;Ready?.Invoke(s,c,calibration);
            }
            Render();
        };
    }
    private void Reset(){_stage=0;_open.Clear();_pinch.Clear();_release.Clear();_context=null;_session=null;_sequence=_capture=-1;}
    internal int Stage => _stage;
    internal void AdvanceFixture(){Pocket.App.AcceptanceHost.ValidateDesktop();_next.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));}
    public void Update(TrackingSnapshot snapshot,LearningContext? context)
    {
        if(_stage==0)return;
        if(_context is not null&&(_context!=context||_session!=snapshot.SessionId))
        {Reset();_hint.Text="Source or calibration changed. Begin gesture practice again.";return;}
        if(context is not {IsValid:true} c||snapshot.Observation is not {} h)return;
        _context??=c;_session??=snapshot.SessionId;
        if(h.Sequence==_sequence)return;
        _sequence=h.Sequence;
        bool eligible=h.SessionId==_session&&h.IsValid&&h.Tracked&&!h.Ambiguous&&!h.Partial&&h.Visual is null&&
            h.Handedness==(c.Hand==1?HandSide.Left:HandSide.Right)&&h.HandednessScore>=.7&&
            snapshot.HostObservationAge is {} age&&age.TotalMilliseconds<150&&
            h.ResultMonoMs-h.CaptureMonoMs<=250&&(_capture<0||h.CaptureMonoMs>_capture);
        if(!eligible){if(_stage<4){_open.Clear();_pinch.Clear();_release.Clear();_stage=1;Render();}return;}
        if(_capture>=0&&h.CaptureMonoMs-_capture<25)return;
        if(_capture>=0&&h.CaptureMonoMs-_capture>250){_open.Clear();_pinch.Clear();_release.Clear();_stage=1;}
        _capture=h.CaptureMonoMs;
        if(_stage<4&&GhostGestureIntentAdapter.TryPinchRatio(h,out var ratio))
        {var samples=_stage==1?_open:_stage==2?_pinch:_release;if(samples.Count<60)samples.Add(ratio);}
        Render();
    }
    private void Render()
    {
        _next.Content=_stage switch{1=>"Continue to pinch",2=>"Continue to release",3=>"Check samples and practice",_=>"Redo gesture practice"};
        _hint.Text=_stage switch{1=>$"Hold fingers comfortably apart · {_open.Count}/12 samples",2=>$"Bring thumb and index together · {_pinch.Count}/12 samples",3=>$"Open fingers again · {_release.Count}/12 samples",_=>"Gesture practice active · Pocket interaction disabled"};
    }
}
