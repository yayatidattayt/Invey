using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Text.Json;
using Microsoft.Win32;
using Pocket.Overlay;
using Pocket.App.Tracking;
using Pocket.App.References;
using Pocket.Core.References;

namespace Pocket.App;

public partial class MainWindow : Window
{
    private GhostOverlay? _overlay;
    private SimulationController? _simulation;
    private Window? _settings;
    private FixtureWindow? _fixture;
    private RecordedTrackingWindow? _tracking;
    private PocketWorkspace? _pocket;
    private DualWorkspace? _workspace;
    private Shortcuts? _shortcuts;
    private readonly InveyProfile _profile;
    private readonly PocketStore? _store;
    private readonly Window? _captureBackground;
    private readonly PocketCards? _cards;
    private bool _exiting, _exitInProgress;
    internal bool TrayAvailable { get; set; }
    internal event Action<string>? CameraStatusChanged;
    public MainWindow() : this(new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Invey"))) { }
    internal MainWindow(InveyProfile profile, Window? captureBackground = null)
    {
        _profile = profile;
        _captureBackground = captureBackground;
        if (profile.Available)
        {
            _store = new PocketStore(profile.Root);
            _cards = new PocketCards(_store, profile.Writable);
        }
        InitializeComponent();
        if (profile.Notice?.StartsWith("DEMO PROFILE", StringComparison.Ordinal) == true)
        {
            Title = "Invey · Demo";
            ProfileNotice.Text = profile.Notice;
            ProfileNotice.Visibility = Visibility.Visible;
        }
        SourceInitialized += (_, _) => ScreenSnapshot.DisableNativeTransitions(this);
        if (profile.Notice is not null) Status.Text = profile.Notice;
        CameraStatusChanged += status => GhostState.Text = $"Ghost · {status}";
        Loaded += async (_, _) =>
        {
            if (_cards is null) return;
            try { await _cards.RestoreAsync(); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException)
            { Status.Text = "Could not restore the Pocket library."; }
        };
    }
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
    internal void StartShortcuts()
    {
        try
        {
            _shortcuts = new Shortcuts(_profile.Available ? Path.Combine(_profile.Root, "shortcuts.json") : null,
                _profile.Writable, HandleShortcut);
            if (_shortcuts.LoadNotice is { } notice) Status.Text = notice;
            else if (Enum.GetValues<ShortcutAction>().Any(action => _shortcuts.Status(action).StartsWith("Unavailable")))
                Status.Text = "A shortcut is unavailable. Use the tray or remap it in Settings.";
        }
        catch (Exception error) when (error is Win32Exception or InvalidOperationException)
        {
            System.Diagnostics.Trace.TraceWarning("Shortcut startup failed: 0x{0:X8}", error.HResult);
            Status.Text = "Windows shortcuts are unavailable. Use Invey’s buttons or tray.";
        }
    }

    private async void HandleShortcut(ShortcutAction action)
    {
        if (_exitInProgress || CaptureInProgress || _workspace?.InteractionActive == true) return;
        switch (action)
        {
            case ShortcutAction.Capture: await CaptureAsync(); break;
            case ShortcutAction.Pocket: ShowPocket(toggle: true); break;
            case ShortcutAction.PauseGhost: Pause(); break;
            case ShortcutAction.Workspace: ShowWorkspace(); break;
        }
    }

    public void ShowSettings()
    {
        if (CaptureInProgress || _exitInProgress) return;
        if(_settings==null)
        {
            var content=new StackPanel { Margin=new(24) };
            content.Children.Add(new TextBlock { Text="Invey settings",FontSize=20,FontWeight=FontWeights.SemiBold });
            content.Children.Add(new TextBlock { Text="Invey (in-vay) brings together Pocket, Dual, and Ghost.", TextWrapping=TextWrapping.Wrap });
            content.Children.Add(new TextBlock { Margin=new(0,16,0,16),TextWrapping=TextWrapping.Wrap,Text="Camera use is optional and starts from the Tracking lab. Pocket references are stored in your local Windows profile." });
            if (_shortcuts is { } shortcuts) AddShortcutSettings(content, shortcuts);
            var close=new Button { Content="Close",HorizontalAlignment=HorizontalAlignment.Left };
            close.Click+=(_,_)=>_settings?.Close(); content.Children.Add(close);
            _settings=new Window { Title="Invey settings",Width=530,Height=560,
                Content=new ScrollViewer { Content=content, VerticalScrollBarVisibility=ScrollBarVisibility.Auto }, Owner=this };
            _settings.SourceInitialized += (_, _) => ScreenSnapshot.DisableNativeTransitions(_settings);
            _settings.Closed+=(_,_)=>_settings=null;
        }
        _settings.Show(); _settings.Activate();
    }
    private static void AddShortcutSettings(StackPanel content, Shortcuts shortcuts)
    {
        ShortcutAction[] actions = Enum.GetValues<ShortcutAction>();
        string[] names = ["Capture region", "Toggle Pocket shelf", "Pause Ghost", "Open Workspace"];
        var fields = new TextBox[actions.Length];
        var statuses = new TextBlock[actions.Length];
        content.Children.Add(new TextBlock { Text = "Shortcuts", FontWeight = FontWeights.SemiBold });
        content.Children.Add(new TextBlock { Text = "Example: Ctrl+Alt+G. Clear a field to disable its shortcut.",
            TextWrapping = TextWrapping.Wrap, Margin = new(0, 4, 0, 6) });
        for (int i = 0; i < actions.Length; i++)
        {
            fields[i] = new TextBox { Text = shortcuts.Binding(actions[i]), MaxLength = 40, IsReadOnly = !shortcuts.CanEdit };
            System.Windows.Automation.AutomationProperties.SetName(fields[i], names[i]);
            statuses[i] = new TextBlock { Text = shortcuts.Status(actions[i]), Margin = new(0, 2, 0, 8), TextWrapping = TextWrapping.Wrap };
            content.Children.Add(new TextBlock { Text = names[i] });
            content.Children.Add(fields[i]);
            content.Children.Add(statuses[i]);
        }
        var feedback = new TextBlock { Text = shortcuts.LoadNotice, TextWrapping = TextWrapping.Wrap };
        System.Windows.Automation.AutomationProperties.SetLiveSetting(feedback, System.Windows.Automation.AutomationLiveSetting.Polite);
        var apply = new Button { Content = "_Apply shortcuts", IsEnabled = shortcuts.CanEdit, HorizontalAlignment = HorizontalAlignment.Left };
        apply.Click += (_, _) =>
        {
            string? failure = shortcuts.Apply(fields.Select(box => box.Text).ToArray());
            feedback.Text = failure ?? "Shortcuts saved.";
            for (int i = 0; i < actions.Length; i++) statuses[i].Text = shortcuts.Status(actions[i]);
        };
        content.Children.Add(apply);
        content.Children.Add(feedback);
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
            _tracking = new RecordedTrackingWindow(profileSettingsPath: _profile.Available
                ? Path.Combine(_profile.Root, "calibration.json") : null, profileWritable: _profile.Writable);
            _tracking.CameraStatusChanged += status => CameraStatusChanged?.Invoke(status);
            _tracking.Closed += (_, _) => _tracking = null;
        }
        _tracking.Show();
        _tracking.Activate();
    }
    private async void ExitApp(object sender, RoutedEventArgs e) => await ExitAsync();
    private void OpenPocket(object sender, RoutedEventArgs e) => ShowPocket();
    private PocketWorkspace? PocketShelf()
    {
        if (_store is null) { Status.Text = _profile.Notice; return null; }
        if (_pocket is not null) return _pocket;
        var pocket = new PocketWorkspace(_store, PrepareCaptureAsync, _captureBackground, _cards, _profile.Writable);
        pocket.ReferenceDeleted += id => _workspace?.ReferenceDeleted(id);
        pocket.ReferenceRestored += id => _workspace?.ReferenceRestored(id);
        pocket.Closed += (_, _) => _pocket = null;
        return _pocket = pocket;
    }

    public void ShowPocket(bool toggle = false)
    {
        if (_exitInProgress || CaptureInProgress || PocketShelf() is not { } shelf) return;
        if (toggle && shelf.IsVisible && shelf.WindowState != WindowState.Minimized) { shelf.Tuck(); return; }
        shelf.Show();
        if (shelf.WindowState == WindowState.Minimized) shelf.WindowState = WindowState.Normal;
        shelf.Activate();
    }

    public async Task CaptureAsync()
    {
        if (!_exitInProgress && !CaptureInProgress && PocketShelf() is { } shelf) await shelf.StartCaptureAsync();
    }

    public void ShowWorkspace()
    {
        if (_exitInProgress || CaptureInProgress || _store is null) return;
        if (_workspace is null)
        {
            _workspace = new DualWorkspace(_store);
            _workspace.Closed += (_, _) => _workspace = null;
        }
        _workspace.Show();
        if (_workspace.WindowState == WindowState.Minimized) _workspace.WindowState = WindowState.Normal;
        _workspace.Activate();
    }

    private async void CaptureClick(object sender, RoutedEventArgs e) => await CaptureAsync();
    private void WorkspaceClick(object sender, RoutedEventArgs e) => ShowWorkspace();
    private void PauseClick(object sender, RoutedEventArgs e) => Pause();
    private async Task<bool> PrepareCaptureAsync()
    {
        _simulation?.Stop();
        UpdateButtons();
        return _tracking is null || await _tracking.StopAsync();
    }
    public async Task ExitAsync()
    {
        if (_exitInProgress) return;
        _exitInProgress = true;
        try
        {
            if (_pocket is { } pocket) await pocket.PrepareExitAsync();
            if (_tracking is not null && !await _tracking.CloseAsync())
            {
                _pocket?.ResumeAfterAbortedExit();
                return;
            }
            _workspace?.Close();
            if (_cards is not null) await _cards.CloseAsync();
            Application.Current.Shutdown();
        }
        finally { _exitInProgress = false; }
    }
    internal object TestState() => new
    {
        Handle = new System.Windows.Interop.WindowInteropHelper(this).Handle.ToInt64(),
        Pocket = _pocket?.TestState(), Workspace = _workspace?.TestState(), Camera = GhostState.Text,
        Tracking = _tracking?.TestSnapshot(),
        TrackingHandle = _tracking is null ? 0 : new System.Windows.Interop.WindowInteropHelper(_tracking).Handle.ToInt64(),
        SettingsVisible = _settings?.IsVisible == true,
        SettingsHandle = _settings is null ? 0 : new System.Windows.Interop.WindowInteropHelper(_settings).Handle.ToInt64(),
        DemoProfile = Title == "Invey · Demo",
        WindowLoaded = IsLoaded,
        PocketButtonPoint = IsLoaded && PocketButton.IsVisible
            ? PocketButton.PointToScreen(new Point(PocketButton.ActualWidth / 2, PocketButton.ActualHeight / 2)) : (Point?)null,
        Bindings = Enum.GetValues<ShortcutAction>().Select(action => new
        {
            Action = action.ToString(), Chord = _shortcuts?.Binding(action), Status = _shortcuts?.Status(action)
        }).ToArray()
    };

    internal string? ApplyTestShortcuts(string[] entries) => _shortcuts?.Apply(entries);

    internal bool PressTestGhost() => _workspace?.PressGhost() == true;
    internal void MoveTestGhost(int dx = 8) => _workspace?.MoveGhost(dx, 0);
    internal void CancelTestGhost() => _workspace?.CancelGhost();
    internal void CloseTestWorkspace() => _workspace?.Close();

    internal async Task CloseAcceptanceAsync()
    {
        if (_pocket is { } pocket) await pocket.PrepareExitAsync();
        if (_tracking is not null && !await _tracking.CloseAsync()) throw new IOException("Tracking cleanup was not confirmed.");
        if (_cards is not null) await _cards.CloseAsync();
        _workspace?.Close();
        _pocket?.Close();
        Cleanup();
    }

    internal async Task OpenFixtureCardsAsync()
    {
        var library = await Task.Run(_store!.Load);
        foreach (var item in library.Items.Take(4)) await _cards!.OpenAsync(item, activate: false);
        try { await _cards!.OpenAsync(library.Items[4], activate: false); }
        catch (InvalidDataException) when (_cards!.Count == 4) { return; }
        throw new InvalidOperationException("The fifth floating card was not refused.");
    }

    internal void ExerciseFixtureCards(int cycle) => _cards!.ExerciseFixture(cycle);

    internal object FixtureResources() => new
    {
        ManagedHeap = GC.GetTotalMemory(false),
        VisibleWindows = Application.Current.Windows.OfType<Window>().Count(window => window.IsVisible),
        Cards = _cards!.Count,
        _cards.PlacementFailures
    };

    internal bool CaptureInProgress => _pocket?.CaptureInProgress == true;
    public async void Pause()
    {
        _workspace?.StopSimulation();
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
        _exiting=true; _shortcuts?.Dispose(); DisposeOverlay(); _workspace?.Close(); _settings?.Close(); _fixture?.Close(); Close();
    }
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_exiting)
        {
            e.Cancel = true;
            if (TrayAvailable && App.HasShellTray()) Hide();
            else Dispatcher.BeginInvoke(async () => await ExitAsync());
        }
        base.OnClosing(e);
    }
}
