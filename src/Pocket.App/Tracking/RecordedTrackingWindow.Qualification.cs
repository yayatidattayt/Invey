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
        Guid source=Guid.NewGuid();long sequence=0;long fixtureTime=1000;
        var trace=new System.Collections.Generic.List<object>();
        host.Gestures.Diagnostic=row=>{if(trace.Count<160)trace.Add(row);};
        async Task Sample(bool pinch,TrackingSource kind=TrackingSource.Camera)
        {
            var sample=_calibration.LearningFixtureObservation(source,sequence,new(.5,.5));
            var points=sample.Observation!.Landmarks.ToArray();
            points[4]=new(pinch?.532:.58,.5,0);points[5]=new(.42,.5,0);points[17]=new(.58,.5,0);
            long now=fixtureTime;fixtureTime+=40;
            _learningFixture=sample with { Source=kind,Observation=sample.Observation with{Sequence=sequence++,CaptureMonoMs=now,ResultMonoMs=now,HandednessScore=.98,Landmarks=points.ToImmutableArray()} };
            Render();
            // This lesson checks a complete synthetic sequence, not latest-only queue delivery.
            // Queue cadence/coalescing remains covered by ExerciseObservationDeliveryAsync.
            DeliverDesktopObservation();
            await Task.Delay(40);

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
            await Sample(false);
            if(int.TryParse(Environment.GetEnvironmentVariable("INVEY_GESTURE_FIXTURE_GAP_MS"),out int injectedGap)&&injectedGap is >=250 and <=400)await Task.Delay(injectedGap);
            for(int i=0;i<7;i++)await Sample(true);
            if(host.Gestures.Last?.Transition is not (GhostGestureTransition.Hold or GhostGestureTransition.PinchConfirmed))throw new InvalidOperationException("Gesture proposal did not reach production coordinator: "+System.Text.Json.JsonSerializer.Serialize(new{host.Gestures.Last,host.Gestures.Status,host.Engine.Mode,host.Engine.Source,host.Engine.Session,trace}));
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
        finally {
            host.Gestures.Diagnostic=null;
            var folder=System.IO.Path.GetDirectoryName(Environment.GetEnvironmentVariable("GHOST_TEST_POCKET_STORE"))!;
            System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"gesture-observation-trace.json"),System.Text.Json.JsonSerializer.Serialize(trace));
            _learningFixture=null;host.Stop();_refresh.Start();Render();}
    }

    internal async Task<object> ExerciseObservationDeliveryAsync(GhostDesktopHost host)
    {
        AcceptanceHost.ValidateDesktop();
        EnableDesktopPreviewFixture();
        Guid source = Guid.NewGuid(); long sequence = 0;
        try
        {
            var delivery = new System.Collections.Generic.List<object>();
            _refresh.Stop(); // Prove that label/image polling is not driving the shared pointer.
            foreach (int hz in new[] { 30, 60 })
            {
                long before = host.PointerUpdates;
                var elapsed = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < hz; i++)
                {
                    long now = Environment.TickCount64;
                    var sample = _calibration.LearningFixtureObservation(source, sequence, new(.5 + .03 * Math.Sin(i * .1), .5));
                    _learningFixture = sample with { Source = TrackingSource.Recorded, Observation = sample.Observation! with {
                        Sequence = sequence++, CaptureMonoMs = now, ResultMonoMs = now } };
                    _pointerNotification.Signal();
                    await Task.Delay(TimeSpan.FromMilliseconds(1000d / hz));
                }
                long updates = host.PointerUpdates - before;
                if (updates < hz * .8) throw new InvalidOperationException($"{hz}Hz fixture delivered only {updates} positions with label polling disabled.");
                delivery.Add(new { NominalInputHz = hz, Updates = updates, ElapsedMs = elapsed.Elapsed.TotalMilliseconds,
                    ActualPointerUpdatesPerSecond = updates / elapsed.Elapsed.TotalSeconds, LabelPolling = false, CameraOpened = false });
            }
            return new { Evidence = "SYNTHETIC_OBSERVATIONS_ACTUAL_WPF_SHARED_POINTER", ObservationDelivery = delivery };
        }
        finally { _learningFixture = null; host.Stop(); _refresh.Start(); Render(); }
    }
    internal async Task<object> ExerciseHybridSourceAsync(GhostDesktopHost host)
    {
        AcceptanceHost.ValidateDesktop();
        bool previousStarted=_startedInMode;bool previousImage=_showPreview.IsChecked==true;
        int previousSequence=_recordedDemo.SelectedIndex;
        await StopAsync();_recorded.IsChecked=true;_showPreview.IsChecked=true;EnableDesktopPreviewFixture();
        long before=host.PointerUpdates;
        _recordedDemo.SelectedIndex=2;
        _start.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        if(_controller.Read().SessionId is null)throw new InvalidOperationException("Hybrid recorded source control did not start.");
        bool visual=false,image=false;
        try
        {
            var timeout=System.Diagnostics.Stopwatch.StartNew();
            while(timeout.Elapsed<TimeSpan.FromSeconds(4))
            {
                var snapshot=_controller.Read();
                if(snapshot.Observation?.Visual is not null&&host.Engine.Position is not null)visual=true;
                image|=_liveView.Sequence.HasValue;
                if(visual&&image)break;
                await Task.Delay(20);
            }
            if(!visual||!image||host.PointerUpdates-before<2)throw new InvalidOperationException("Actual optical packet/image did not reach the shared WPF pointer.");
            if(host.Engine.Source!=GhostSourceKind.Recorded||host.Engine.Arm()||host.Gestures.LiveQualified)
                throw new InvalidOperationException("Recorded optical preview gained action authority.");
            return new { ActualImageProcessing=true,ActualWorkerPipe=true,ActualWpfImage=image,SharedPointer=visual,
                PointerUpdates=host.PointerUpdates-before,CameraOpened=false,ActionsDenied=true };
        }
        finally { await StopAsync();host.Stop();_recordedDemo.SelectedIndex=previousSequence;_startedInMode=previousStarted;_showPreview.IsChecked=previousImage;Render(); }
    }
}

