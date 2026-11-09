using System;
using System.Linq;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Pocket.Core;
using Pocket.Core.Tracking;

namespace Pocket.App.Tracking;
internal sealed partial class RecordedTrackingWindow
{
    internal async Task<object> ExerciseGesturePracticeAsync(GhostDesktopHost host)
    {
        AcceptanceHost.ValidateDesktop();
        double previousHeight=Height;Height=540;_live.IsChecked=true;UpdateLayout();
        _trackingScroll.ScrollToEnd();UpdateLayout();
        var devicePoint=_devices.TranslatePoint(new System.Windows.Point(0,0),this);
        var stopPoint=_stop.TranslatePoint(new System.Windows.Point(0,0),this);
        if(_trackingScroll.VerticalScrollBarVisibility!=System.Windows.Controls.ScrollBarVisibility.Visible||
            _trackingScroll.ViewportHeight<=0||_trackingScroll.ScrollableHeight<=0||
            devicePoint.Y<0||devicePoint.Y+_devices.ActualHeight>ActualHeight||
            stopPoint.Y<0||stopPoint.Y+_stop.ActualHeight>ActualHeight)
            throw new InvalidOperationException("Tracking source controls, scrollbar or fixed Stop are unreachable at compact height.");
        _recorded.IsChecked=true;Height=previousHeight;_trackingScroll.ScrollToTop();
        EnableDesktopPreviewFixture();
        Guid source=Guid.NewGuid();long sequence=0;
        async Task Sample(bool pinch,TrackingSource kind=TrackingSource.Camera)
        {
            var sample=_calibration.LearningFixtureObservation(source,sequence,new(.5,.5));
            var points=sample.Observation!.Landmarks.ToArray();
            points[4]=new(pinch?.532:.58,.5,0);points[5]=new(.42,.5,0);points[17]=new(.58,.5,0);
            long now=Environment.TickCount64;
            _learningFixture=sample with { Source=kind,Observation=sample.Observation with{Sequence=sequence++,CaptureMonoMs=now,ResultMonoMs=now,HandednessScore=.98,Landmarks=points.ToImmutableArray()} };
            Render();await Task.Delay(40);
        }
        try
        {
            for(int i=0;i<8;i++)await Sample(false);
            if(host.Engine.Source!=GhostSourceKind.Camera||host.Engine.Position is null)throw new InvalidOperationException("Camera-protocol fixture did not move shared desktop pointer.");
            _gesturePractice.AdvanceFixture();for(int i=0;i<14;i++)await Sample(false);
            _gesturePractice.AdvanceFixture();for(int i=0;i<14;i++)await Sample(true);
            _gesturePractice.AdvanceFixture();for(int i=0;i<14;i++)await Sample(false);
            _gesturePractice.AdvanceFixture();
            if(_gesturePractice.Stage!=4)throw new InvalidOperationException("Actual guided practice controls did not accept eligible separate samples.");
            await Sample(false);for(int i=0;i<7;i++)await Sample(true);
            if(host.Gestures.Last?.Transition is not (GhostGestureTransition.Hold or GhostGestureTransition.PinchConfirmed))throw new InvalidOperationException("Gesture proposal did not reach production coordinator.");
            if(host.Engine.Arm()||host.Engine.ActiveTarget is not null||host.Gestures.LiveQualified)throw new InvalidOperationException("Unqualified camera acquired action authority.");
            for(int i=0;i<7;i++)await Sample(false);
            var transition=host.Gestures.Last?.Transition;
            if(transition is not (GhostGestureTransition.Neutral or GhostGestureTransition.ReleaseConfirmed))throw new InvalidOperationException("Release practice did not finish.");
            await Sample(false,TrackingSource.Recorded);
            if(host.Gestures.Last?.Transition!=GhostGestureTransition.Stopped)throw new InvalidOperationException("Source change did not immediately invalidate gesture context.");
            for(int i=0;i<7;i++)await Sample(false,TrackingSource.Recorded);
            if(host.Engine.Source!=GhostSourceKind.Recorded)throw new InvalidOperationException("Source identity was not replaced.");
            if(host.Gestures.Last?.Transition!=GhostGestureTransition.Stopped)throw new InvalidOperationException("Source change did not invalidate gesture context.");
            return new{ActualTrackingUi=true,GuidedLesson=true,CameraProtocolFixture=true,CameraOpened=false,SharedPointer=true,PinchProposal=true,ReleaseProposal=true,ProductionActionsDenied=true,SourceReplacementCanceled=true};
        }
        finally {_learningFixture=null;host.Stop();Render();}
    }
}
