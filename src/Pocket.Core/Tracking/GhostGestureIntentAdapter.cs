using System;
using System.Collections.Generic;
using System.Linq;

namespace Pocket.Core.Tracking;

public enum GhostGestureTransition
{
    Neutral, PinchCandidate, PinchConfirmed, Hold, ReleaseCandidate, ReleaseConfirmed,
    TrackingLost, Ambiguous, Paused, Stopped
}

public enum GhostGestureCommand { None, Press, Release }

/// <summary>A user-calibrated pinch band. It is valid only with independent open/pinch samples.</summary>
public sealed class GhostGestureCalibration
{
    private GhostGestureCalibration(Guid id,HandSide hand,double enter,double release,int dwell)
    {Id=id;SelectedHand=hand;PinchEnterRatio=enter;ReleaseRatio=release;DwellMilliseconds=dwell;}
    public Guid Id {get;}
    public HandSide SelectedHand {get;}
    public double PinchEnterRatio {get;}
    public double ReleaseRatio {get;}
    public int DwellMilliseconds {get;}
    public bool IsValid => Id!=Guid.Empty&&Enum.IsDefined(SelectedHand)&&PinchEnterRatio==.28&&ReleaseRatio==.42&&DwellMilliseconds is >=180 and <=1000;

    public static GhostGestureCalibration? FromPracticeSamples(HandSide selectedHand,IEnumerable<double> pinchRatios,IEnumerable<double> openRatios,int dwellMilliseconds=180)
    {
        var pinches=pinchRatios.ToArray();var opens=openRatios.ToArray();
        if(!Enum.IsDefined(selectedHand)||dwellMilliseconds is <180 or >1000||pinches.Length<3||opens.Length<3||
            pinches.Concat(opens).Any(x=>!double.IsFinite(x)||x is <0 or >1))return null;
        if(pinches.Max()>.28||opens.Min()<.42||opens.Min()-pinches.Max()<.14)return null;
        return new(Guid.NewGuid(),selectedHand,.28,.42,dwellMilliseconds);
    }
    private static double Median(double[] values){var sorted=values.Order().ToArray();return sorted[sorted.Length/2];}
}

public sealed record GhostGestureTarget(string Id, Guid Lifetime);
public sealed record GhostGestureResult(GhostGestureTransition Transition, GhostGestureCommand ProposedCommand,
    bool PocketArmed, GhostGestureTarget? Target, bool MayExecute, string Reason);

/// <summary>
/// Camera/recording gesture-intent prototype. It consumes validated full observations and emits
/// typed proposals only. No production handler currently consumes these proposals; physical
/// qualification and a separately reviewed action bridge are still required.
/// </summary>
public sealed class GhostGestureIntentAdapter
{
    private Guid? _session;
    private GhostGestureCalibration? _calibration;
    private GhostGestureTarget? _target;
    private Func<string,Guid,GhostGestureTarget?>? _resolveTarget;
    private bool _armed, _held, _neutralSeen;
    private long _sequence=-1, _lastCapture=-1, _candidateSince=-1, _releaseSince=-1;
    private GhostGestureTransition _transition=GhostGestureTransition.Stopped;
    public GhostGestureTransition Transition => _transition;
    public bool LiveActionsEnabled => false;
    public bool IsHolding => _held;
    internal object DiagnosticState => new { Sequence=_sequence,CaptureMs=_lastCapture,NeutralSeen=_neutralSeen,CandidateSince=_candidateSince,Held=_held };

    public GhostGestureResult Begin(Guid session, GhostGestureCalibration calibration)
    {
        Stop();
        if(session==Guid.Empty || !calibration.IsValid)
            return Set(GhostGestureTransition.Stopped,GhostGestureCommand.None,null,"Gesture calibration is missing or invalid.");
        _session=session;_calibration=calibration;
        return Set(GhostGestureTransition.Neutral,GhostGestureCommand.None,null,"Waiting for a fresh full selected hand.");
    }

    public bool ArmPocket(GhostGestureTarget target,Func<string,Guid,GhostGestureTarget?> resolveTarget)
    {
        if(_session is null || _calibration is null || target is null || string.IsNullOrWhiteSpace(target.Id) || target.Lifetime==Guid.Empty ||
            _held || _transition is GhostGestureTransition.Paused or GhostGestureTransition.Stopped or GhostGestureTransition.Ambiguous or GhostGestureTransition.TrackingLost)
            return false;
        if(resolveTarget(target.Id,target.Lifetime)!=target)return false;
        _target=target;_resolveTarget=resolveTarget;_armed=true;return true;
    }

    public GhostGestureResult Observe(HandObservation observation,long nowMonoMs)
    {
        if(_session is null || _calibration is null)
            return Set(GhostGestureTransition.Stopped,GhostGestureCommand.None,null,"Adapter is stopped.");
        if(_transition==GhostGestureTransition.Paused)
            return Set(GhostGestureTransition.Paused,GhostGestureCommand.None,null,"Paused; restart practice to resume.");
        if(_armed&&(_target is null||_resolveTarget?.Invoke(_target.Id,_target.Lifetime)!=_target))
        {
            _held=false;_armed=false;_neutralSeen=false;_target=null;_resolveTarget=null;_candidateSince=_releaseSince=-1;
            return Set(GhostGestureTransition.TrackingLost,GhostGestureCommand.Release,null,"Target closed or replaced; gesture released.");
        }
        if(observation.Ambiguous)
        {
            _held=false;_neutralSeen=false;_candidateSince=_releaseSince=-1;_armed=false;_target=null;_resolveTarget=null;
            return Set(GhostGestureTransition.Ambiguous,GhostGestureCommand.Release,null,"Ambiguous hand observation released control; re-arm after tracking is clear.");
        }
        if(observation.SessionId!=_session || !observation.IsValid || !observation.Tracked || observation.Partial || observation.Visual is not null ||
            observation.Handedness!=_calibration.SelectedHand || observation.HandednessScore is not >=0.7 ||
            observation.CaptureMonoMs>nowMonoMs || observation.ResultMonoMs>nowMonoMs ||
            nowMonoMs-observation.ResultMonoMs>=150 || nowMonoMs-observation.CaptureMonoMs>250 ||
            observation.Sequence<=_sequence || observation.CaptureMonoMs<_lastCapture || !TryPinchRatio(observation,out var ratio))
        {
            _held=false;_armed=false;_target=null;_resolveTarget=null;_candidateSince=_releaseSince=-1;
            return Set(GhostGestureTransition.TrackingLost,GhostGestureCommand.Release,null,"Hand mismatch, stale data, source replacement or incomplete landmarks released control.");
        }

        if(_lastCapture>=0&&observation.CaptureMonoMs-_lastCapture>250)
        {
            _neutralSeen=false;_held=false;_candidateSince=_releaseSince=-1;_armed=false;_target=null;_resolveTarget=null;
            _sequence=observation.Sequence;_lastCapture=observation.CaptureMonoMs;
            return Set(GhostGestureTransition.TrackingLost,GhostGestureCommand.Release,null,"Observation gap; open hand and re-arm.");
        }
        _sequence=observation.Sequence;_lastCapture=observation.CaptureMonoMs;
        if(!_neutralSeen)
        {
            if(ratio>=_calibration.ReleaseRatio)_neutralSeen=true;
            else return Set(GhostGestureTransition.Neutral,GhostGestureCommand.None,null,"Open your hand before starting a new pinch.");
        }
        if(!_held)
        {
            if(ratio<=_calibration.PinchEnterRatio)
            {
                if(_candidateSince<0)_candidateSince=observation.CaptureMonoMs;
                if(observation.CaptureMonoMs-_candidateSince>=_calibration.DwellMilliseconds)
                {
                    _held=true;_releaseSince=-1;
                    return Set(GhostGestureTransition.PinchConfirmed,GhostGestureCommand.Press,null,"Pinch sustained; preview proposal only.");
                }
                return Set(GhostGestureTransition.PinchCandidate,GhostGestureCommand.None,null,"Keep the pinch steady.");
            }
            _candidateSince=-1;
            return Set(GhostGestureTransition.Neutral,GhostGestureCommand.None,null,"No pinch intent.");
        }

        if(ratio>=_calibration.ReleaseRatio)
        {
            if(_releaseSince<0)_releaseSince=observation.CaptureMonoMs;
            if(observation.CaptureMonoMs-_releaseSince>=_calibration.DwellMilliseconds)
            {
                _held=false;_candidateSince=_releaseSince=-1;
                return Set(GhostGestureTransition.ReleaseConfirmed,GhostGestureCommand.Release,null,"Open hand sustained; release proposal only.");
            }
            return Set(GhostGestureTransition.ReleaseCandidate,GhostGestureCommand.None,null,"Open fingers steadily to release.");
        }
        _releaseSince=-1;
        return Set(GhostGestureTransition.Hold,GhostGestureCommand.None,null,"Pinch held; waiting for release.");
    }

    public GhostGestureResult Pause()
    {
        _held=false;_armed=false;_target=null;_resolveTarget=null;_candidateSince=_releaseSince=-1;
        return Set(GhostGestureTransition.Paused,GhostGestureCommand.Release,null,"Paused; any held intent released.");
    }

    public GhostGestureResult Stop()
    {
        bool wasHeld=_held;_held=false;_armed=false;_neutralSeen=false;_target=null;_resolveTarget=null;_session=null;_calibration=null;
        _candidateSince=_releaseSince=-1;_sequence=_lastCapture=-1;
        return Set(GhostGestureTransition.Stopped,wasHeld?GhostGestureCommand.Release:GhostGestureCommand.None,null,"Stopped; no action dispatched.");
    }

    private GhostGestureResult Set(GhostGestureTransition state,GhostGestureCommand command,GhostGestureTarget? target,string reason)
    {
        _transition=state;
        // Deliberately hard-disabled: this adapter has no production Pocket action bridge.
        return new(state,command,_armed,target??_target,false,reason);
    }

    internal static bool TryPinchRatio(HandObservation hand,out double ratio)
    {
        ratio=double.NaN;
        if(hand.FrameSize is not {IsValid:true} frame)return false;
        var thumb=hand.Point(4);var index=hand.Point(8);var palmA=hand.Point(5);var palmB=hand.Point(17);
        if(thumb is not{} t||index is not{} i||palmA is not{} a||palmB is not{} b)return false;
        double aspect=(double)frame.Width/frame.Height;
        double pinch=Distance(t,i,aspect),width=Distance(a,b,aspect);
        if(!double.IsFinite(pinch)||!double.IsFinite(width)||width<=1e-6)return false;
        ratio=pinch/width;return double.IsFinite(ratio)&&ratio<=2;
    }

    private static double Distance(ImageLandmark a,ImageLandmark b,double aspect)
    {double x=(a.X-b.X)*aspect,y=a.Y-b.Y;return Math.Sqrt(x*x+y*y);}
}
