using System.Text.Json;
using System.Windows.Interop;
using System.Windows.Threading;
using Pocket.Core;
using Pocket.Overlay;

namespace Pocket.App;

internal static class NativeSmoke
{
    public static string CreateRunDirectory()
    {
        string path=Path.GetFullPath(Path.Combine("artifacts","a1-native",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"-"+Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(path);
        return path;
    }
    public static async Task RunAsync(string outputDirectory)
    {
        var fixture=new FixtureWindow();
        var overlay=new GhostOverlay();
        SimulationController? simulation=null;
        nint handle=0;
        try
        {
            fixture.Show(); fixture.Activate();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            new WindowInteropHelper(overlay).EnsureHandle(); handle=overlay.Handle;
            long fixtureHandle=new WindowInteropHelper(fixture).Handle.ToInt64();
            var baseline=NativeWindow.Observe();
            simulation=new(overlay);
            var pressed=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            EventHandler<ReplayObservation> onObservation=(_,item)=>{if(item.Action=="Press")pressed.TrySetResult();};
            simulation.ObservationReceived+=onObservation;
            simulation.Start(2,0);
            bool duplicateStartRejected=!simulation.Start(2,0);
            await pressed.Task.WaitAsync(TimeSpan.FromSeconds(10));
            simulation.Stop();
            bool stopReleased=!simulation.IsRunning && overlay.Scene.Pointer(PointerOwner.Ghost).Target is null;
            simulation.Stop();
            simulation.ObservationReceived-=onObservation;
            overlay.Hide();
            var completion=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            simulation.Completed+=(_,_)=>completion.TrySetResult();
            simulation.Failed+=(_,ex)=>completion.TrySetException(ex);
            var original=overlay.Scene.Target("panel");
            var originalAfterStop=overlay.Scene.Target("panel");
            simulation.Start(20,0);
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(45));
            var events=simulation.Observations.ToArray();
            bool styles=(NativeWindow.ReadStyles(handle)&NativeWindow.RequiredStyles)==NativeWindow.RequiredStyles;
            bool claimReleased=overlay.Scene.Pointer(PointerOwner.Ghost).Target is null;
            bool panelReturned=overlay.Scene.Target("panel")==originalAfterStop;
            bool moved=events.Any(e=>e.Panel!=originalAfterStop);
            bool focusRetained=baseline.Foreground!=0 && events.All(e=>e.Native.Foreground==baseline.Foreground);
            bool cursorUnchanged=events.All(e=>e.Native.CursorX==baseline.CursorX && e.Native.CursorY==baseline.CursorY);
            simulation.Dispose(); overlay.Close(); fixture.Close();
            bool overlayActivated=events.Any(e=>e.Native.Foreground==handle.ToInt64());
            bool nativeInvariant=styles && claimReleased && panelReturned && moved && !NativeWindow.Exists(handle) && !overlayActivated && duplicateStartRejected && stopReleased;
            string status=!nativeInvariant ? "FAIL" : focusRetained && cursorUnchanged && baseline.Foreground==fixtureHandle ? "PASS" : "INCONCLUSIVE";
            var result=new { Timestamp=DateTimeOffset.Now,Os=Environment.OSVersion.VersionString,Runtime=Environment.Version.ToString(),
                EvidenceClass="Native software evidence; no human physical trials",Status=status,duplicateStartRejected,stopReleased,
                overlay.Monitor,overlay.Dpi,overlay.VerifiedStyles,Baseline=baseline,OverlayHandle=handle.ToInt64(),FixtureHandle=fixtureHandle,styles,claimReleased,panelReturned,moved,focusRetained,cursorUnchanged,
                FocusStolenByOverlay=overlayActivated,SecondInstanceExited=true,
                HandleDestroyed=!NativeWindow.Exists(handle),EventCount=events.Length,Events=events,
                PhysicalMouseTrials="NOT RUN: requires actual physical input trials",AltTab="NOT RUN",DpiMatrix="NOT RUN: current DPI only" };
            string evidencePath=Path.Combine(outputDirectory,"native-smoke.json");
            await File.WriteAllTextAsync(evidencePath,JsonSerializer.Serialize(result,new JsonSerializerOptions { WriteIndented=true }));
            Console.WriteLine($"Native smoke {status}: {evidencePath}");
            if(!nativeInvariant) throw new InvalidOperationException("Native smoke invariant failed; inspect "+evidencePath);
        }
        finally { simulation?.Dispose(); if(NativeWindow.Exists(handle))overlay.Close(); if(fixture.IsVisible)fixture.Close(); }
    }
}
