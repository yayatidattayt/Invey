using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Pocket.Core.Tests")]
[assembly: InternalsVisibleTo("Invey")]

namespace Pocket.Core.Tracking;

/// <summary>Dispatcher-owned proposal gate. Production instances have no action capability.
/// The internal controlled replay route uses the existing Controlled-only desktop authority.
/// Camera qualification has deliberately no issuer in this release.</summary>
public sealed class GhostGestureCoordinator
{
    private readonly GhostDesktop _desktop;
    private readonly bool _controlledReplay;
    private readonly GhostGestureIntentAdapter _adapter=new();
    private Guid? _source, _desktopSession;
    private LearningContext? _context;
    private GhostGestureCalibration? _calibration;
    private GhostDesktopTarget? _candidate;
    private GhostTargetGeometry? _candidateGeometry;
    private bool _armed, _neutral;
    internal Action<object>? Diagnostic {get;set;}
    public GhostGestureResult? Last {get;private set;}
    public bool LiveQualified => false;
    public string Status => Last?.Reason??"Gesture practice off · Pocket interaction disabled";
    public GhostGestureCoordinator(GhostDesktop desktop) { _desktop=desktop; }
    private GhostGestureCoordinator(GhostDesktop desktop,bool replay) { _desktop=desktop;_controlledReplay=replay; }
    internal static GhostGestureCoordinator ControlledReplay(GhostDesktop desktop)=>new(desktop,true);
    public void Begin(Guid source,LearningContext context,GhostGestureCalibration calibration)
    {
        Stop();if(source==Guid.Empty||!context.IsValid||!calibration.IsValid||
            calibration.SelectedHand!=(context.Hand==1?HandSide.Left:HandSide.Right))return;
        _source=source;_context=context;_calibration=calibration;_desktopSession=_desktop.Session;
        Last=_adapter.Begin(source,calibration);
    }
    internal bool ArmReplay()
    {
        _armed=_controlledReplay&&_source.HasValue&&_desktop.Source==GhostSourceKind.Controlled&&
            _desktop.Session==_desktopSession&&_desktop.Arm();
        _neutral=false;_candidate=null;return _armed;
    }
    public GhostGestureResult? Observe(HandObservation hand,LearningContext? context,long now)
    {
        if(_source is null)return Last;
        if(context!=_context||_desktop.Session!=_desktopSession||hand.SessionId!=_source||
            hand.FrameSize is not {} frame||frame.Width!=context?.FrameWidth||frame.Height!=context?.FrameHeight||
            _desktop.Mode is GhostControlMode.Off or GhostControlMode.Paused or GhostControlMode.Lost)
        {Stop();return Last;}
        var before=Diagnostic is null?null:_adapter.DiagnosticState;
        Last=_adapter.Observe(hand,now);
        if(Diagnostic is {} trace)trace(new {hand.Sequence,hand.CaptureMonoMs,hand.ResultMonoMs,NowMs=now,Ratio=GhostGestureIntentAdapter.TryPinchRatio(hand,out var ratio)?ratio:(double?)null,Before=before,After=_adapter.DiagnosticState,Result=Last});
        if(Last.Transition is GhostGestureTransition.TrackingLost or GhostGestureTransition.Ambiguous or GhostGestureTransition.Paused or GhostGestureTransition.Stopped)
        {Cancel();return Last;}
        if(!_armed||!_controlledReplay||_desktop.Source!=GhostSourceKind.Controlled)return Last;
        if(Last.Transition==GhostGestureTransition.Neutral){_neutral=true;_candidate=null;}
        if(Last.Transition==GhostGestureTransition.PinchCandidate&&_neutral&&_candidate is null)
        {
            _neutral=false;_candidate=_desktop.ReachableTarget;
            _candidateGeometry=_candidate?.Geometry();
        }
        if(Last.ProposedCommand==GhostGestureCommand.Press)
        {
            // Bind acquisition to the target and geometry at pinch onset. No retroactive press.
            if(_candidate is null||_desktop.ReachableTarget!=_candidate||_candidate.Geometry()!=_candidateGeometry||
                !_desktop.Press(_desktopSession!.Value,_desktop.Sequence)) Cancel();
        }
        if(Last.Transition==GhostGestureTransition.ReleaseConfirmed)
        {
            if(_desktop.ActiveTarget is not null)_desktop.Release(_desktopSession!.Value,_desktop.Sequence);
            _candidate=null;_neutral=true;
        }
        if(_adapter.IsHolding&&_candidate is not null&&_desktop.ActiveTarget is null)Cancel();
        return Last;
    }
    private void Cancel(){if(_armed)_desktop.Disarm();_armed=false;_neutral=false;_candidate=null;}
    public void Stop(){Cancel();Last=_adapter.Stop();_source=null;_context=null;_calibration=null;_desktopSession=null;}
}
