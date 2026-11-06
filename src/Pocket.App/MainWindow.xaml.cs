using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Text.Json;
using Microsoft.Win32;
using Pocket.Overlay;
using Pocket.App.Tracking;

namespace Pocket.App;

public partial class MainWindow : Window
{
    private GhostOverlay? _overlay;
    private SimulationController? _simulation;
    private Window? _settings;
    private FixtureWindow? _fixture;
    private RecordedTrackingWindow? _tracking;
    private bool _exiting;
    internal event Action<string>? CameraStatusChanged;
    public MainWindow() { InitializeComponent(); }
    private void EnsureOverlay()
    {
        if(_overlay!=null) return;
        var overlay=new GhostOverlay();
        try { new System.Windows.Interop.WindowInteropHelper(overlay).EnsureHandle(); }
        catch { overlay.Close(); throw; }
        _overlay=overlay; _simulation=new(overlay);
        overlay.SurfaceInvalidated+=(_,_)=>{ DisposeOverlay(); Status.Text="Display changed. Replay canceled; show overlay to recreate on primary monitor."; UpdateButtons(); };
        _simulation.Completed+=(_,_)=>{ Status.Text="Replay completed. Export observations; physical mouse/focus acceptance still requires review."; UpdateButtons(); };
        _simulation.Failed+=(_,ex)=>{ Status.Text="Replay stopped: "+ex.Message; UpdateButtons(); };
    }
    private void Safely(Action action)
    {
        try { action(); }
        catch(Exception ex) { DisposeOverlay(); Status.Text="Operation failed: "+ex.Message; }
        UpdateButtons();
    }
    private void ShowOverlay(object sender,RoutedEventArgs e) => Safely(()=>{EnsureOverlay(); _overlay!.Show(); Status.Text=$"Passive styles verified: 0x{_overlay.VerifiedStyles:X}; DPI {_overlay.Dpi}.";});
    private void HideOverlay(object sender,RoutedEventArgs e) => Safely(()=>{_simulation?.Stop(); _overlay?.Hide(); Status.Text="Overlay hidden; GHOST claim canceled.";});
    private void StartReplay(object sender,RoutedEventArgs e) => Safely(()=>{EnsureOverlay(); _simulation!.Start(); Status.Text="Replay starts in 5 seconds, runs 20 cycles. Focus the test fixture or editor now.";});
    private void StopReplay(object sender,RoutedEventArgs e) => Safely(()=>{_simulation?.Stop(); Status.Text="Replay stopped; GHOST claim canceled.";});
    private void UpdateButtons() { StartButton.IsEnabled=_simulation?.IsRunning!=true; StopButton.IsEnabled=_simulation?.IsRunning==true; }
    private void OpenFixture(object sender,RoutedEventArgs e)
    {
        if(_fixture==null) { _fixture=new(); _fixture.Closed+=(_,_)=>_fixture=null; }
        _fixture.Show(); _fixture.Activate();
    }
    public void ShowSettings()
    {
        if(_settings==null)
        {
            var content=new StackPanel { Margin=new(24) };
            content.Children.Add(new TextBlock { Text="Invey settings",FontSize=20,FontWeight=FontWeights.SemiBold });
            content.Children.Add(new TextBlock { Text="Invey (in-vay) brings together Pocket, Dual, and Ghost.", TextWrapping=TextWrapping.Wrap });
            content.Children.Add(new TextBlock { Margin=new(0,16,0,16),TextWrapping=TextWrapping.Wrap,Text="Primary display only. Camera use is optional and starts from the Tracking lab. No settings are persisted yet." });
            var checkbox=new CheckBox { Content="Show passive overlay",IsChecked=_overlay?.IsVisible==true,Margin=new(0,0,0,16) };
            checkbox.Checked+=(_,_)=>Safely(()=>{EnsureOverlay();_overlay!.Show();});
            checkbox.Unchecked+=(_,_)=>Safely(()=>{_simulation?.Stop();_overlay?.Hide();});
            content.Children.Add(checkbox);
            var close=new Button { Content="Close",HorizontalAlignment=HorizontalAlignment.Left };
            close.Click+=(_,_)=>_settings?.Close(); content.Children.Add(close);
            _settings=new Window { Title="Invey settings",Width=460,Height=300,Content=content,Owner=this };
            _settings.Closed+=(_,_)=>_settings=null;
        }
        _settings.Show(); _settings.Activate();
    }
    private void OpenSettings(object sender,RoutedEventArgs e)=>ShowSettings();
    private async void ExportEvidence(object sender,RoutedEventArgs e)
    {
        if(_simulation==null || _overlay==null) { Status.Text="Run a replay first."; return; }
        var dialog=new SaveFileDialog { Filter="JSON evidence|*.json",FileName="ghost-a1-observations.json" };
        if(dialog.ShowDialog(this)!=true) return;
        var payload=new { Os=Environment.OSVersion.VersionString, Architecture=System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            _overlay.Monitor,_overlay.Dpi,_overlay.VerifiedStyles, _simulation.TimerTicks, Events=_simulation.Observations.ToArray(),
            Fixture=_fixture?.Counters(), PhysicalAcceptance="NOT RUN unless independently observed and recorded" };
        try { await File.WriteAllTextAsync(dialog.FileName,JsonSerializer.Serialize(payload,new JsonSerializerOptions { WriteIndented=true })); Status.Text="Evidence exported. No screen pixels or typed text included."; }
        catch(Exception ex) { Status.Text="Export failed: "+ex.Message; }
    }
    private void OpenTrackingLab(object sender, RoutedEventArgs e)
    {
        if (_tracking is null)
        {
            _tracking = new RecordedTrackingWindow();
            _tracking.CameraStatusChanged += status => CameraStatusChanged?.Invoke(status);
            _tracking.Closed += (_, _) => _tracking = null;
        }
        _tracking.Show();
        _tracking.Activate();
    }
    private async void ExitApp(object sender, RoutedEventArgs e) => await ExitAsync();
    public async Task ExitAsync()
    {
        if (_tracking is null || await _tracking.CloseAsync()) Application.Current.Shutdown();
    }
    public async void Pause()
    {
        _simulation?.Stop();
        _overlay?.Hide();
        Status.Text = "Stopping from tray.";
        UpdateButtons();
        if (_tracking is null || await _tracking.StopAsync()) Status.Text = "Stopped from tray.";
    }
    private void DisposeOverlay()
    {
        _simulation?.Dispose(); _simulation=null; _overlay?.Close(); _overlay=null;
    }
    public void Cleanup()
    {
        _exiting=true; DisposeOverlay(); _settings?.Close(); _fixture?.Close(); Close();
    }
    protected override void OnClosing(CancelEventArgs e)
    {
        if(!_exiting) { e.Cancel=true; Hide(); }
        base.OnClosing(e);
    }
}
