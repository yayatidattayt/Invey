using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using Pocket.Core.Tracking;

namespace Pocket.App.Tracking;

internal sealed class RecordedTrackingWindow : Window
{
    private readonly RecordedTrackingController _controller = new();
    private readonly DispatcherTimer _refresh = new() { Interval = TimeSpan.FromMilliseconds(80) };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 16, 0, 12) };
    private readonly TextBlock _sourceStatus = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) };
    private readonly TextBlock _inventoryStatus = new()
    {
        TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0)
    };
    private readonly RadioButton _recorded = new() { Content = "Rec_orded tracking", IsChecked = true, Margin = new Thickness(0, 0, 12, 0) };
    private readonly RadioButton _live = new() { Content = "_Live camera" };
    private readonly Button _find = new() { Content = "_Find cameras", MinHeight = 30, Margin = new Thickness(0, 6, 0, 6) };
    private readonly ComboBox _devices = new() { DisplayMemberPath = nameof(CameraDevice.Name), MinHeight = 28 };
    private readonly StackPanel _deviceOptions = new();
    private readonly Button _start = new() { MinHeight = 34, Padding = new Thickness(12, 4, 12, 4), Margin = new Thickness(0, 0, 8, 0) };
    private readonly Button _stop = new() { Content = "S_top", MinHeight = 34, Padding = new Thickness(12, 4, 12, 4) };
    private readonly Button _calibrate = new() { Content = "_Calibrate", MinHeight = 34, Margin = new Thickness(8, 0, 0, 0) };
    private readonly CalibrationPanel _calibration;
    private TrackingSnapshot _visible;
    private bool _stopping;
    private bool _closing;
    private bool _allowClose;
    private Task<bool>? _stopTask;
    private Task<bool>? _closeTask;
    private CancellationTokenSource? _scan;
    private Task? _scanTask;
    private readonly string? _fixtureFile;
    private readonly string? _fixtureCameraFault;
    private bool _startedInMode;
    private string _cameraStatus = "Camera off";
    public event Action<string>? CameraStatusChanged;

    public RecordedTrackingWindow(string? fixtureFile = null, string? fixtureCameraFault = null,
        string? testSettingsPath = null, bool acceptanceHost = false)
    {
        _fixtureFile = fixtureFile;
        _fixtureCameraFault = fixtureCameraFault;
        if (fixtureFile is not null) _live.Content = "_Local video fixture";
        Title = "Invey · Tracking lab";
        Width = 432;
        MinWidth = 360;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        AutomationProperties.SetLiveSetting(_status, AutomationLiveSetting.Polite);
        AutomationProperties.SetLiveSetting(_inventoryStatus, AutomationLiveSetting.Polite);
        AutomationProperties.SetName(_devices, "Camera device");
        _recorded.Checked += (_, _) => SourceChanged();
        _live.Checked += (_, _) => SourceChanged();
        _find.Click += async (_, _) => await (_scanTask = FindAsync());
        _devices.SelectionChanged += (_, _) => Render();
        _start.Click += (_, _) => Start();
        _stop.Click += async (_, _) => await StopAsync();
        string? settings = acceptanceHost ? testSettingsPath : Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PocketDualGhost", "calibration.json");
        _calibration = new CalibrationPanel(async () =>
        {
            await StopAsync();
            _start.Focus();
        }, settings is null ? null : new CalibrationPreferencesStore(settings));
        _calibrate.Click += (_, _) =>
        {
            _calibration.Begin();
            _refresh.Interval = TimeSpan.FromMilliseconds(25);
            Render();
        };
        DpiChanged += (_, _) => RefreshCalibrationDisplays();
        SystemEvents.DisplaySettingsChanged += DisplaySettingsChanged;
        SystemEvents.UserPreferenceChanged += UserPreferenceChanged;
        Closed += (_, _) =>
        {
            SystemEvents.DisplaySettingsChanged -= DisplaySettingsChanged;
            SystemEvents.UserPreferenceChanged -= UserPreferenceChanged;
        };
        var actions = new WrapPanel { Children = { _start, _stop, _calibrate } };
        var sources = new WrapPanel { Margin = new Thickness(0, 12, 0, 0), Children = { _recorded, _live } };
        _deviceOptions.Children.Add(_find);
        _deviceOptions.Children.Add(new Label { Content = "_Device", Target = _devices, Padding = new Thickness(0, 0, 0, 4) });
        _deviceOptions.Children.Add(_devices);
        _deviceOptions.Children.Add(_inventoryStatus);
        Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = new StackPanel
        {
            Margin = new Thickness(24),
            Children =
            {
                new TextBlock { Text = "Hand observation", FontSize = 20, FontWeight = FontWeights.SemiBold },
                sources,
                _sourceStatus,
                new TextBlock
                {
                    Text = $"Local camera diagnostic · Stops automatically after {RecordedTrackingController.DiagnosticSeconds} seconds.",
                    TextWrapping = TextWrapping.Wrap,
                    Visibility = _controller.HasDiagnostics ? Visibility.Visible : Visibility.Collapsed
                },
                _deviceOptions,
                _status,
                actions,
                _calibration
            }
        } };
        MaxHeight = Math.Max(380, SystemParameters.WorkArea.Height - 32);
        _visible = _controller.Read();
        _inventoryStatus.Text = "Find connected cameras, then choose one to enable.";
        Render();
        _refresh.Tick += (_, _) => Render();
        _refresh.Start();
    }

    private void DisplaySettingsChanged(object? sender, EventArgs args) =>
        _ = Dispatcher.InvokeAsync(RefreshCalibrationDisplays);

    private void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs args)
    {
        if (args.Category is UserPreferenceCategory.Window or UserPreferenceCategory.General)
            _ = Dispatcher.InvokeAsync(RefreshCalibrationDisplays);
    }

    private void RefreshCalibrationDisplays()
    {
        if (_calibration.Visibility != Visibility.Visible) return;
        _calibration.RefreshDisplays();
        Render();
    }

    public bool Start(string? fixtureFault = null)
    {
        bool live = _live.IsChecked == true;
        var device = _devices.SelectedItem as CameraDevice;
        bool started = !_stopping && !_closing && _scan is null && (!live || device is not null) &&
            _controller.Start(live ? device : null, live ? _fixtureFile : null,
                live ? _fixtureCameraFault : fixtureFault);
        _startedInMode |= started;
        Render();
        return started;
    }

    private void SourceChanged()
    {
        _startedInMode = false;
        Render();
    }

    private async Task FindAsync()
    {
        if (_scan is not null || _closing || !_controller.CanStart) return;
        using var cancel = new CancellationTokenSource();
        _scan = cancel;
        _devices.ItemsSource = null;
        ShowInventoryStatus("Finding cameras…");
        Render();
        try
        {
            var inventory = _fixtureFile is not null || _fixtureCameraFault is not null
                ? new CameraInventory([new CameraDevice(new string('0', 64), "Controlled test source")])
                : await _controller.FindCamerasAsync(cancel.Token);
            if (cancel.IsCancellationRequested) return;
            _devices.ItemsSource = inventory.Devices;
            ShowInventoryStatus(inventory.Error switch
            {
                "PermissionDenied" => "Windows denied camera access. Check Settings → Privacy & security → Camera.",
                "Timeout" => "Finding cameras timed out. Try again.",
                not null => "Camera discovery is unavailable. Recorded tracking is still available.",
                _ => inventory.Devices.Count == 0
                    ? "No usable camera found. Connect a camera and try again."
                    : "Select a camera. It stays off until you enable it."
            });
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested) { }
        finally
        {
            _scan = null;
            Render();
        }
    }

    private void ShowInventoryStatus(string text)
    {
        _inventoryStatus.Text = text;
        UIElementAutomationPeer.CreatePeerForElement(_inventoryStatus)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    public Task<bool> StopAsync() => _stopTask is { IsCompleted: false } ? _stopTask : _stopTask = StopCoreAsync();

    private async Task<bool> StopCoreAsync()
    {
        _stopping = true;
        try
        {
            var completion = _controller.StopAsync();
            Render();
            return await completion;
        }
        finally
        {
            _stopping = false;
            Render();
        }
    }

    public Task<bool> CloseAsync() => _closeTask is { IsCompleted: false } ? _closeTask : _closeTask = CloseCoreAsync();

    private async Task<bool> CloseCoreAsync()
    {
        _closing = true;
        try
        {
            _scan?.Cancel();
            if (_scanTask is not null) await _scanTask;
            if (!await StopAsync()) return false;
            _allowClose = true;
            _refresh.Stop();
            Close();
            return true;
        }
        finally { _closing = false; }
    }

    protected override async void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (_allowClose) return;
        e.Cancel = true;
        // Let WPF complete the canceled close event before closing again.
        await System.Windows.Threading.Dispatcher.Yield(DispatcherPriority.Background);
        await CloseAsync();
    }

    private void Render()
    {
        _visible = _controller.Read();
        bool live = _live.IsChecked == true;
        bool available = !_closing && !_stopping && _scan is null && _controller.CanStart;
        _live.IsEnabled = _recorded.IsEnabled = _find.IsEnabled = available;
        _devices.IsEnabled = available;
        _deviceOptions.Visibility = live ? Visibility.Visible : Visibility.Collapsed;
        _start.IsEnabled = available && (!live || _devices.SelectedItem is CameraDevice);
        _stop.IsEnabled = !_stopping && _visible.SessionId.HasValue;
        bool canCalibrate = _visible.State is TrackingState.Ready or TrackingState.Observing
            or TrackingState.Lost or TrackingState.Stale or TrackingState.Partial or TrackingState.Ambiguous;
        _calibrate.IsEnabled = canCalibrate && _calibration.Visibility != Visibility.Visible;
        if (!canCalibrate && _calibration.Visibility == Visibility.Visible)
        {
            _calibration.End();
            _refresh.Interval = TimeSpan.FromMilliseconds(80);
        }
        _calibration.Update(_visible);
        string source = !live ? "recorded tracking" : _fixtureFile is not null ? "test video" : "live camera";
        _start.Content = (_startedInMode ? "_Restart " : "_Enable ") + source;
        string cameraStatus = _visible.Source == TrackingSource.Camera ? _visible.State switch
        {
            TrackingState.Starting => "Camera starting",
            TrackingState.Stopping => "Camera releasing",
            _ => "Camera active"
        } : "Camera off";
        if (cameraStatus != _cameraStatus)
        {
            _cameraStatus = cameraStatus;
            CameraStatusChanged?.Invoke(cameraStatus);
        }
        _sourceStatus.Text = (!live ? "Recorded source" : _fixtureFile is not null ? "Native video fixture" : "Live camera")
            + " · " + cameraStatus;
        string next = _visible.State switch
        {
            TrackingState.Off => live ? "Choose a source and enable it when ready." : "Ready for an explicit start with controlled frames.",
            TrackingState.Starting => "Starting the local tracking runtime…",
            TrackingState.Ready => "Waiting for a hand observation.",
            TrackingState.Observing => "Hand detected · 21 landmarks",
            TrackingState.Lost => "No hand detected. The current observation is clear.",
            TrackingState.Partial => "Hand partly outside the frame. Move your hand into view.",
            TrackingState.Ambiguous => "Multiple hands detected. Show one hand to continue.",
            TrackingState.Stale => "No recent tracking result. The current observation is clear.",
            TrackingState.Stopping when _visible.CleanupUnconfirmed => "Worker exit is unconfirmed. Restart is held until cleanup completes.",
            TrackingState.Stopping => "Stopping. The current observation is clear.",
            TrackingState.Stopped => "Stopped. The worker has exited.",
            TrackingState.Unavailable => _visible.Failure switch
            {
                TrackingFailure.InvalidModel => "The tracking model or recorded sample is missing or invalid.",
                TrackingFailure.Timeout => "The tracking worker did not respond in time.",
                TrackingFailure.ProtocolFailure => "The tracking stream contained an invalid message.",
                TrackingFailure.WorkerExited => "The tracking worker closed unexpectedly.",
                TrackingFailure.NoDevice => "The selected camera is unavailable. Find cameras again or use recorded tracking.",
                TrackingFailure.PermissionDenied => "Windows denied camera access. Check Settings → Privacy & security → Camera.",
                TrackingFailure.DeviceBusy => "The camera is busy. Close the other camera app and restart tracking.",
                TrackingFailure.DeviceLost => "The camera was disconnected. Reconnect it and find cameras again.",
                TrackingFailure.UnsupportedFormat => "The camera could not supply a supported video format.",
                TrackingFailure.DeviceActivationFailed => "Windows could not open the selected camera.",
                TrackingFailure.SourceReaderFailed => "Windows opened the camera but could not start reading video.",
                TrackingFailure.NoFrames => "The camera did not deliver usable video. Check its privacy shutter.",
                TrackingFailure.StreamInterrupted => "The camera stopped delivering usable video. Restart to try again.",
                TrackingFailure.FrameConversionFailed => "Invey could not safely convert the camera frame.",
                _ => "Tracking is unavailable. Check the local runtime or try a different source."
            },
            _ => "Tracking is unavailable."
        };
        if (_status.Text == next) return;
        _status.Text = next;
        UIElementAutomationPeer.CreatePeerForElement(_status)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    public object TestSnapshot()
    {
        var sample = _visible.Observation;
        return new
        {
            State = _visible.State.ToString(), _visible.SessionId, Source = _visible.Source?.ToString(),
            Failure = _visible.Failure?.ToString(), _visible.AcceptedObservations, _visible.SupersededObservations,
            CanStart = _start.IsEnabled, CanStop = _stop.IsEnabled, Status = _status.Text,
            SourceStatus = _sourceStatus.Text, SourceChoicesEnabled = _live.IsEnabled && _recorded.IsEnabled,
            InventoryStatus = _inventoryStatus.Text, CameraCount = _devices.Items.Count, DeviceSelected = _devices.SelectedIndex >= 0,
            _controller.LastWorkerPid, _controller.ForcedStop, _controller.WorkerPhase, _controller.WorkerNativeCode,
            Sequence = sample?.Sequence, WristX = sample?.Landmarks[0].X,
            SourceMs = sample is null ? (long?)null : sample.ResultMonoMs - sample.CaptureMonoMs,
            HostAgeMs = _visible.HostObservationAge?.TotalMilliseconds,
            Calibration = _calibration.TestSnapshot(), IndexX = sample?.Landmarks[8].X, Title
        };
    }
}
