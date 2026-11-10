using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using Pocket.Core;
using Pocket.Core.Tracking;

namespace Pocket.App;

internal static class GhostGestureReplayFixture
{
    internal static async Task<object> RunAsync(GhostDesktopHost host)
    {
        AcceptanceHost.ValidateDesktop();
        host.StartControlled();
        var target=host.Engine.Targets.First(t=>t.Id.StartsWith("card:",StringComparison.Ordinal));
        var screen=System.Windows.Forms.Screen.FromPoint(new((int)target.Geometry.Bounds.X,(int)target.Geometry.Bounds.Y));
        var b=screen.Bounds;var display=new CalibrationDisplay(1,b.X,b.Y,b.Width,b.Height,96);
        var context=new LearningContext("Recorded",LearningContext.HashDevice("explicit-controlled-gesture-fixture"),640,480,display,2,false,12,100);
        var source=Guid.NewGuid();long sequence=0;
        var mapper=new GhostTrackingMapper();
        var bridge=GhostGestureCoordinator.ControlledReplay(host.Engine);
        var timings=new List<double>();double mapMs=0,gestureMs=0,pointerMs=0;
        var box=target.Geometry.Bounds;
        double x=.12+.76*(box.X+box.Width/2-b.X)/(b.Width-1),y=.12+.76*(box.Y+box.Height/2-b.Y)/(b.Height-1);
        async Task Sample(bool pinch,double offset=0,bool ambiguous=false)
        {
            long now=Environment.TickCount64;
            var watch=Stopwatch.StartNew();
            var points=Enumerable.Repeat(new ImageLandmark(x+offset,y,0),21).ToArray();
            points[4]=new(x+offset-(pinch?.04:.10),y,0);
            points[5]=new(x+offset-.10,y,0);points[17]=new(x+offset+.10,y,0);
            var hand=new HandObservation(source,sequence++,now,now,points.ToImmutableArray(),HandSide.Right,.98,false,new(640,480),ambiguous);
            var snapshot=new TrackingSnapshot(TrackingState.Observing,source,TrackingSource.Recorded,hand,TimeSpan.Zero,null,false,sequence,0);
            var p=mapper.Map(snapshot,context,null);
            mapMs+=watch.Elapsed.TotalMilliseconds;watch.Restart();
            if(p is {} position)host.MoveControlled(position);
            pointerMs+=watch.Elapsed.TotalMilliseconds;watch.Restart();
            bridge.Observe(hand,context,now);
            gestureMs+=watch.Elapsed.TotalMilliseconds;
            timings.Add(Environment.TickCount64-now);
            await Task.Delay(40);
        }
        try
        {
            for(int i=0;i<8;i++)await Sample(false);
            var calibration=GhostGestureCalibration.FromPracticeSamples(HandSide.Right,[.2,.21,.22],[.5,.51,.52])!;
            bridge.Begin(source,context,calibration);
            if(!bridge.ArmReplay())throw new InvalidOperationException("Controlled gesture capability failed to arm.");
            await Sample(false);
            for(int i=0;i<7;i++)await Sample(true);
            if(host.Engine.ActiveTarget!=target.Id)throw new InvalidOperationException("Replay pinch did not acquire actual Pocket card.");
            for(int i=0;i<10;i++)await Sample(true,.025);
            for(int i=0;i<7;i++)await Sample(false,.025);
            if(host.Engine.ActiveTarget is not null)throw new InvalidOperationException("Replay release retained Pocket ownership.");
            if(host.Engine.Targets.First(t=>t.Id==target.Id).Geometry.Bounds.X<=box.X+5)
                throw new InvalidOperationException("Mapped replay did not move actual WPF card.");
            await Sample(false,.025);
            for(int i=0;i<7;i++)await Sample(true,.025);
            await Sample(true,.025,ambiguous:true);
            if(host.Engine.ActiveTarget is not null||host.Engine.Mode==GhostControlMode.Armed)
                throw new InvalidOperationException("Ambiguity did not cancel and disarm.");
            var sorted=timings.Order().ToArray();
            return new {MappedControlledReplay=true,ActualCardMoved=true,Release=true,AmbiguityCanceled=true,
                Samples=timings.Count,MappingTotalMs=mapMs,PointerAndWpfMoveTotalMs=pointerMs,GestureAndDispatchTotalMs=gestureMs,
                MedianProcessingMs=sorted[sorted.Length/2],P95ProcessingMs=sorted[(int)(sorted.Length*.95)],CameraOpened=false};
        }
        finally {bridge.Stop();host.Stop();}
    }
}
