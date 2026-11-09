using System.Windows;
using Pocket.Core;
using Pocket.Core.Tracking;

namespace Pocket.App;
public partial class MainWindow
{
    private readonly GhostTrackingMapper _desktopMapper=new();
    private long _desktopSequence=-1;
    private Guid? _desktopSource;
    private async void OpenControlledGhost(object sender,RoutedEventArgs args)
    {
        _simulation?.Stop();_overlay?.Hide();
        if(_tracking is not null&&!await _tracking.StopAsync()){Status.Text="Tracking did not stop. Close it from the Tracking lab before starting the controlled beta.";return;}
        DesktopGhost.Stop();DesktopGhost.ShowControls();
    }
    private void DesktopTracking(TrackingSnapshot snapshot,LearningContext? context,DipPoint? ignored,PersonalModel? model)
    {
        if(!DesktopGhost.TrackingEnabled)return;
        if(snapshot.State is TrackingState.Off or TrackingState.Stopped or TrackingState.Stopping or TrackingState.Unavailable)
        { _desktopMapper.Reset(); DesktopGhost.TrackingStopped();return; }
        if(snapshot.SessionId is not { } id)return;
        if(_desktopSource!=id){_desktopSequence=-1;_desktopSource=id;_desktopMapper.Reset();}
        var watch=System.Diagnostics.Stopwatch.StartNew();
        var point=_desktopMapper.Map(snapshot,context,model);
        DesktopGhost.LastMappingMs=watch.Elapsed.TotalMilliseconds;
        if(snapshot.Observation is not { } hand || point is null)
        { DesktopGhost.Gestures.Stop();DesktopGhost.Engine.Lose("Tracking lost or mapping unavailable — released");return; }
        if(hand.Sequence==_desktopSequence)return;
        _desktopSequence=hand.Sequence;
        long age=(long)(snapshot.HostObservationAge?.TotalMilliseconds??150)+hand.ResultMonoMs-hand.CaptureMonoMs;
        DesktopGhost.TrackingPosition(id,hand.Sequence,point,_desktopMapper.Provenance,age,
            snapshot.Source==TrackingSource.Camera?GhostSourceKind.Camera:GhostSourceKind.Recorded);
        DesktopGhost.Gestures.Observe(hand,context,hand.ResultMonoMs+(long)(snapshot.HostObservationAge?.TotalMilliseconds??150));
    }
    internal void EnableDesktopPreviewFixture()=>_tracking?.EnableDesktopPreviewFixture();
}
