using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using Pocket.Core.Tracking;

namespace Pocket.App.Tracking;

internal sealed partial class RecordedTrackingWindow : Window
{
    private readonly PointerStillnessMeasurement _stillness=new();
    private readonly Button _measureStill=new(){Content="Measure 10 seconds still",MinHeight=28};
    private readonly TextBlock _stillStatus=new(){Text="Stillness not measured",TextWrapping=TextWrapping.Wrap};
    internal Func<Pocket.Core.DipPoint?>? DesktopPosition {get;set;}
    private readonly RecordedTrackingController _controller = new();
    private readonly DispatcherTimer _refresh = new() { Interval = TimeSpan.FromMilliseconds(80) };
    private readonly LatestNotification _pointerNotification;
    private long _freshPointerObservations, _lastPointerSequence = -1;
    private Guid? _pointerSession;
    private readonly System.Diagnostics.Stopwatch _pointerClock = new();
    private readonly System.Collections.Generic.List<double> _deliveryAges = [];
    private string? _runningPerformance;
    private double _lastSessionPreviewFps;
    private long _lastSessionPreviewFrames;
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 16, 0, 12) };
    private readonly TextBlock _sourceStatus = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) };
    private readonly TextBlock _budgetStatus = new() { TextWrapping = TextWrapping.Wrap, Margin = new(0, 6, 0, 6) };
    private readonly ComboBox _performanceMode = new() { ItemsSource = new[] { "Normal tracking", "Performance check · raw → conversion (30 s each)", "Performance check · full pointer (no actions)" }, SelectedIndex = 0, MinHeight = 28 };
    private readonly TextBlock _performance = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _inventoryStatus = new()
    {
        TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0)
    };
    private readonly RadioButton _recorded = new() { Content = "Rec_orded tracking", IsChecked = true, Margin = new Thickness(0, 0, 12, 0) };
    private readonly Label _recordedLabel = new() { Content = "Recorded se_quence" };
    private readonly ComboBox _recordedDemo = new() { ItemsSource = new[] { "Recorded model sequence", "SIMULATED calibration practice", "SIMULATED model loss · actual image-motion tracking" }, SelectedIndex = 0, MinHeight = 28 };
    private string RecordedIdentity => _recordedDemo.SelectedIndex switch { 1 => "simulated-practice", 2 => "synthetic-hybrid-images", _ => "recorded-model" };
    private readonly RadioButton _live = new() { Content = "_Live camera" };
    private readonly Button _find = new() { Content = "_Find cameras", MinHeight = 30, Margin = new Thickness(0, 6, 0, 6) };
    private readonly ComboBox _devices = new() { DisplayMemberPath = nameof(CameraDevice.Name), MinHeight = 28 };
    private readonly StackPanel _deviceOptions = new();
    private readonly Button _start = new() { MinHeight = 34, Padding = new Thickness(12, 4, 12, 4), Margin = new Thickness(0, 0, 8, 0) };
    private readonly Button _stop = new() { Content = "S_top", MinHeight = 34, Padding = new Thickness(12, 4, 12, 4) };
    private readonly Button _calibrate = new() { Content = "_Calibrate", MinHeight = 34, Margin = new Thickness(8, 0, 0, 0) };
    private readonly CalibrationPanel _calibration;
    private readonly PersonalLearningPanel _learning;
    private bool _preparingCalibration;
    private readonly Expander _calibrationDetails = new() { Header = "Calibration settings & practice" };
    private readonly CheckBox _showPreview = new() { Content = "_Show local camera view and finger landmarks", Margin = new(0, 8, 0, 8) };
    private readonly CheckBox _mirrorCamera = new() { Content = "Mirror camera image", Margin = new(0, 4, 0, 4) };
    private readonly ComboBox _previewTiming = new() { ItemsSource = new[] { "Match landmarks to frame", "Latest completed image" }, SelectedIndex = 0, MinHeight = 28 };
    private readonly CameraPreviewView _liveView = new();
    private readonly ComboBox _overlayMode = new() { ItemsSource = new[] { "Clean camera view", "Fingertips only", "Full hand landmarks" }, SelectedIndex = 2, MinHeight = 28 };

    private readonly TextBlock _previewStatus = new() { TextWrapping = TextWrapping.Wrap };
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
    internal event Action<TrackingSnapshot,LearningContext?,Pocket.Core.DipPoint?,PersonalModel?>? DesktopPointerChanged;
    internal event Action<bool>? DesktopPreviewChanged;
    private readonly GesturePracticePanel _gesturePractice=new();
    internal event Action<Guid,LearningContext,GhostGestureCalibration>? GesturePracticeReady;
    private readonly CheckBox _desktopPreview=new(){Content="Desktop Ghost preview · no live actions"};
    internal Func<string>? DesktopStatus { get; set; }
    internal Func<long>? DesktopUpdateCount { get; set; }
    internal Func<object>? DesktopMetrics { get; set; }
    private long _pointerUpdateStart;
    private readonly TextBlock _desktopStatus=new(){TextWrapping=TextWrapping.Wrap};
    private readonly TextBlock _qualification=new(){TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,6,0,6)};
    private readonly ScrollViewer _trackingScroll=new(){VerticalScrollBarVisibility=ScrollBarVisibility.Visible,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,CanContentScroll=false};

    public RecordedTrackingWindow(string? fixtureFile = null, string? fixtureCameraFault = null,
        string? testSettingsPath = null, bool acceptanceHost = false, string? profileSettingsPath = null,
        bool profileWritable = true)
    {
        _pointerNotification = new LatestNotification(
            action => Dispatcher.BeginInvoke(DispatcherPriority.Input, action), DeliverDesktopObservation);
        _fixtureFile = fixtureFile;
        _gesturePractice.Ready+=(source,context,calibration)=>GesturePracticeReady?.Invoke(source,context,calibration);
        _fixtureCameraFault = fixtureCameraFault;
        if (fixtureFile is not null) _live.Content = "_Local video fixture";
        Title = "Invey · Tracking lab";
        SetResourceReference(StyleProperty, typeof(Window));
        Width = 700;
        MinWidth = 360;
        SizeToContent = SizeToContent.Manual;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        SourceInitialized += (_, _) => References.ScreenSnapshot.DisableNativeTransitions(this);
        AutomationProperties.SetLiveSetting(_status, AutomationLiveSetting.Polite);
        AutomationProperties.SetLiveSetting(_inventoryStatus, AutomationLiveSetting.Polite);
        AutomationProperties.SetName(_devices, "Camera device");
        _recorded.Checked += (_, _) => SourceChanged();
        _live.Checked += (_, _) => SourceChanged();
        _find.Click += async (_, _) => await (_scanTask = FindAsync());
        _devices.SelectionChanged += (_, _) => Render();
        _mirrorCamera.Click += (_, _) => Render();
        AutomationProperties.SetName(_previewTiming, "Camera preview timing");
        _previewTiming.SelectionChanged += (_, _) => Render();
        AutomationProperties.SetName(_overlayMode, "Camera landmark overlay");
        _overlayMode.SelectionChanged += (_, _) => { _liveView.OverlayMode = _overlayMode.SelectedIndex; Render(); };
        AutomationProperties.SetName(_recordedDemo, "Recorded demonstration");
        _recordedLabel.Target = _recordedDemo;
        _recordedDemo.SelectionChanged += (_, _) => SourceChanged();
        _measureStill.Click+=(_,_)=>{var sample=_controller.Read();if(sample.Source==TrackingSource.Camera&&sample.SessionId is {} session&&DesktopPosition?.Invoke() is not null)_stillness.Begin(session,Environment.TickCount64);};
        _start.Click += (_, _) => Start(_recorded.IsChecked == true ? _recordedDemo.SelectedIndex switch { 1 => "calibration-practice", 2 => "hybrid-preview", _ => null } : null);
        _stop.Click += async (_, _) => await StopAsync();
        string? settings = acceptanceHost ? testSettingsPath : profileSettingsPath;
        _calibration = new CalibrationPanel(async () =>
        {
            await StopAsync();
            _start.Focus();
        }, settings is null ? null : new CalibrationPreferencesStore(settings, profileWritable));
        _learning = new PersonalLearningPanel(settings is null ? null : Path.GetDirectoryName(settings), profileWritable);
        _desktopPreview.Click+=(_,_)=>
        {
            if(_desktopPreview.IsChecked==true){_calibration.Begin(null,focus:false);_calibrationDetails.IsExpanded=true;}
            DesktopPreviewChanged?.Invoke(_desktopPreview.IsChecked==true);
        };
        _calibrationDetails.Content = _calibration;
        _learning.RequestCalibration += () =>
        {
            if (_calibration.Visibility != Visibility.Visible)
            {
                var current = _controller.Read();
                _calibration.Begin(_controller.HasDiagnostics && current.Source == TrackingSource.Camera ? current.SessionId : null, focus: false);
                Render();
            }
            if (_calibration.LearningSetupIssue is not null) _calibrationDetails.IsExpanded = true;
        };
        _learning.LessonStarted += () => _calibrationDetails.IsExpanded = false;
        _calibrate.Click += (_, _) =>
        {
            var snapshot = _controller.Read();
            _calibrationDetails.IsExpanded = true;
            _preparingCalibration = snapshot.State is TrackingState.Off or TrackingState.Stopped or TrackingState.Unavailable;
            _calibration.Begin(_controller.HasDiagnostics && snapshot.Source == TrackingSource.Camera &&
                _fixtureCameraFault is null ? snapshot.SessionId : null);
            _refresh.Interval = TimeSpan.FromMilliseconds(25);
            Render();
        };
        DpiChanged += (_, _) => RefreshCalibrationDisplays();
        SystemEvents.DisplaySettingsChanged += DisplaySettingsChanged;
        SystemEvents.UserPreferenceChanged += UserPreferenceChanged;
        Closed += (_, _) =>
        {
            _controller.SnapshotChanged -= _pointerNotification.Signal;
            _pointerNotification.Dispose();
            DesktopPreviewChanged?.Invoke(false);
            SystemEvents.DisplaySettingsChanged -= DisplaySettingsChanged;
            SystemEvents.UserPreferenceChanged -= UserPreferenceChanged;
        };
        var actions = new WrapPanel { Children = { _start, _stop, _calibrate } };
        var sources = new WrapPanel { Margin = new Thickness(0, 12, 0, 0), Children = { _recorded, _live } };
        _deviceOptions.Children.Add(_find);
        _deviceOptions.Children.Add(new Label { Content = "_Device", Target = _devices, Padding = new Thickness(0, 0, 0, 4) });
        _deviceOptions.Children.Add(_devices);
        _deviceOptions.Children.Add(_inventoryStatus);
        AutomationProperties.SetName(_performanceMode, "Ghost Camera Performance Check");
        _deviceOptions.Children.Add(new Expander { Header = "Ghost Camera Performance Check", Content = new StackPanel { Children = {
            new TextBlock { Text = "Choose a check, then Enable yourself. Raw and conversion share one camera start. No images are saved; Pocket actions stay disabled.", TextWrapping = TextWrapping.Wrap }, _performanceMode } } });
        var setup=new StackPanel {Margin=new Thickness(20,16,20,8),Children={
            new TextBlock{Text="Ghost",FontSize=18,FontWeight=FontWeights.SemiBold},
            new TextBlock{Text="Camera starts only with Enable.",TextWrapping=TextWrapping.Wrap},
            sources,_recordedLabel,_recordedDemo,_deviceOptions,_sourceStatus,_budgetStatus}};
        _trackingScroll.Content = new StackPanel
        {
            Margin = new Thickness(20,8,20,16),
            Children =
            {
                _status,
                _liveView,
                _previewStatus,
                _showPreview,
                _desktopPreview,
                new Expander { Header = "Performance dashboard", Content = new StackPanel{Children={_desktopStatus,_performance,_measureStill,_stillStatus}} },
                _mirrorCamera,
                _qualification,
                _calibrationDetails,
                new TextBlock{Text="Practice · Pocket actions disabled",FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,12,0,8)},
                _gesturePractice,
                _learning,
                new Expander{Header="Preview options and diagnostics",Margin=new Thickness(0,12,0,0),Content=new StackPanel{Children={_previewTiming,_overlayMode}}}
            }
        };
        // An explicit star-sized viewport avoids unbounded content measurement in SizeToContent.
        var layout=new Grid();layout.RowDefinitions.Add(new(){Height=GridLength.Auto});
        layout.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});layout.RowDefinitions.Add(new(){Height=GridLength.Auto});
        Grid.SetRow(_trackingScroll,1);Grid.SetRow(actions,2);actions.Margin=new Thickness(20,12,20,8);
        layout.Children.Add(setup);layout.Children.Add(_trackingScroll);layout.Children.Add(actions);Content=layout;
        MaxHeight = Math.Max(380, SystemParameters.WorkArea.Height - 32);
        Height=Math.Min(760,MaxHeight);
        Loaded+=(_,_)=>_trackingScroll.ScrollToTop();
        _visible = _learningFixture ?? _controller.Read();
        _controller.SnapshotChanged += _pointerNotification.Signal;
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
        if (_allowClose || _closing || _calibration.Visibility != Visibility.Visible) return;
        _calibration.RefreshDisplays();
        Render();
    }

    internal void SimulateDisplayDisconnect()
    {
        _calibration.ApplyDisplays([]);
        Render();
    }

    internal void EnablePreviewFixture() => _showPreview.IsChecked = true;

    internal void OpenFingerFixture()
    {
        _calibrationDetails.IsExpanded = true;
        _calibration.Begin(null);
        _calibration.EnableFixturePreview();
    }

    public bool Start(string? fixtureFault = null)
    {
        bool live = _live.IsChecked == true;
        var device = _devices.SelectedItem as CameraDevice;
        string? performance = live ? _performanceMode.SelectedIndex switch { 1 => "capture", 2 => "full", _ => null } : null;
        bool started = !_stopping && !_closing && _scan is null && (!live || device is not null) &&
            _controller.Start(live ? device : null, live ? _fixtureFile : null,
                live ? _fixtureCameraFault : fixtureFault, _showPreview.IsChecked == true && fixtureFault != "calibration-practice" && performance != "capture", performance);
        _startedInMode |= started;
        if (started) _runningPerformance = performance;
        if(started&&live){_calibrationDetails.IsExpanded=false;_trackingScroll.ScrollToTop();}
        Render();
        return started;
    }

    private void SourceChanged()
    {
        _startedInMode = false;
        if(_live.IsChecked==true)_showPreview.IsChecked=true;
        if(_recorded.IsChecked==true&&_recordedDemo.SelectedIndex==2)_showPreview.IsChecked=true;
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
        Guid? diagnosticSession = _controller.Read().SessionId;
        _stillness.Cancel();
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
            if (diagnosticSession is { } id && RecordedTrackingController.DiagnosticDirectory is { } directory)
            {
                try
                {
                    System.IO.File.WriteAllText(System.IO.Path.Combine(directory, id + ".pointer.json"),
                        System.Text.Json.JsonSerializer.Serialize(new { Session = id, Evidence = "UI_OBSERVED_NOT_SENSOR", Stillness=_stillness.Read(),
                            FreshDeliveries = _freshPointerObservations, PointerUpdates = (DesktopUpdateCount?.Invoke() ?? 0) - _pointerUpdateStart,
                            ElapsedMs = _pointerClock.Elapsed.TotalMilliseconds, _pointerNotification.Notifications,
                            UniquePreviewPresentedFps = _lastSessionPreviewFps, UniquePreviewPresentedFrames = _lastSessionPreviewFrames,
                            PresentationMeaning = "Distinct local image sequences observed on WPF rendering callbacks; not optical latency",
                            MappingScope = "Mapper lifetime in this application; not worker frame counts",
                            _pointerNotification.Coalesced, _pointerNotification.Deliveries,
                            HostReceiveToDispatchMs = _deliveryAges.Count == 0 ? null : new {
                                Median = _deliveryAges.Order().ElementAt(_deliveryAges.Count / 2),
                                P95 = _deliveryAges.Order().ElementAt((int)Math.Ceiling(.95 * _deliveryAges.Count) - 1) },
                            Mapping = DesktopMetrics?.Invoke() }));
                }
                catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException)
                { System.Diagnostics.Trace.TraceWarning("Pointer diagnostic write failed: 0x{0:X8}", error.HResult); }
            }
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

    private void DeliverDesktopObservation()
    {
        if (_closing || _allowClose) return;
        var snapshot = _learningFixture ?? _controller.Read();
        if (snapshot.SessionId.HasValue && _pointerSession != snapshot.SessionId)
        { _pointerSession = snapshot.SessionId; _lastPointerSequence = -1; _freshPointerObservations = 0; _deliveryAges.Clear(); _lastSessionPreviewFps = 0; _lastSessionPreviewFrames = 0; _pointerUpdateStart = DesktopUpdateCount?.Invoke() ?? 0; _pointerClock.Restart(); }
        if (snapshot.State is TrackingState.Stopped or TrackingState.Stopping or TrackingState.Unavailable or TrackingState.Off) _pointerClock.Stop();
        if (_desktopPreview.IsChecked != true) return;
        if (snapshot.Observation is { } hand && hand.Sequence != _lastPointerSequence)
        { _lastPointerSequence = hand.Sequence; _freshPointerObservations++; if (_deliveryAges.Count < 1500 && snapshot.HostObservationAge is { } age) _deliveryAges.Add(age.TotalMilliseconds); }
        long updatesBefore=DesktopUpdateCount?.Invoke()??0;
        DesktopPointerChanged?.Invoke(snapshot, _calibration.DesktopContext(snapshot, snapshot.Source == TrackingSource.Camera
            ? (_devices.SelectedItem as CameraDevice)?.Id ?? "unknown" : RecordedIdentity),
            null, _learning.AcceptedPreviewModel);
        if(snapshot.Source==TrackingSource.Camera&&snapshot.SessionId is {} session&&snapshot.Observation is {} observation)
            _stillness.Observe(session,observation.Sequence,Environment.TickCount64,(DesktopUpdateCount?.Invoke()??0)>updatesBefore?DesktopPosition?.Invoke():null);
    }

    private void Render()
    {
        _visible = _learningFixture ?? _controller.Read();
        if (_learningFixture is not null) _pointerNotification.Signal();
        _stillness.Tick(_visible.SessionId,Environment.TickCount64);
        _measureStill.IsEnabled=_visible.Source==TrackingSource.Camera&&DesktopPosition?.Invoke() is not null&&!_stillness.Running;
        _stillStatus.Text=_stillness.Running?"Hold the index still for 10 seconds · no actions":"Stillness: "+_stillness.Status;
        bool live = _live.IsChecked == true;
        bool available = !_closing && !_stopping && _scan is null && _controller.CanStart;
        _live.IsEnabled = _recorded.IsEnabled = _find.IsEnabled = available;
        _devices.IsEnabled = available;
        _performanceMode.IsEnabled = available && _controller.HasDiagnostics;
        bool simulated = !live && _recordedDemo.SelectedIndex == 1;
        _showPreview.IsEnabled = available && !simulated;
        _showPreview.ToolTip = simulated ? "Simulated practice uses landmarks only, with no video." : null;
        _recordedDemo.IsEnabled = available;
        _recordedDemo.Visibility = _recordedLabel.Visibility = live ? Visibility.Collapsed : Visibility.Visible;
        _deviceOptions.Visibility = live ? Visibility.Visible : Visibility.Collapsed;
        var budget = live && available && _fixtureFile is null && _fixtureCameraFault is null
            ? _controller.DiagnosticBudget() : null;
        bool budgetAvailable = budget?.Available != false;
        _budgetStatus.Text = budget?.Message ?? (live && _controller.HasDiagnostics && _visible.SessionId.HasValue
            ? $"Timed camera test: automatic stop at{RecordedTrackingController.DiagnosticSeconds} seconds. Stop is always available." : "");
        _budgetStatus.Visibility = string.IsNullOrEmpty(_budgetStatus.Text) ? Visibility.Collapsed : Visibility.Visible;
        string? selectedPerformance = _performanceMode.SelectedIndex switch { 1 => "capture", 2 => "full", _ => null };
        bool permittedMode = !live || _controller.RequiredPerformanceMode is not { } required || selectedPerformance == required;
        if (live && available && budgetAvailable && !permittedMode)
        { _budgetStatus.Text += " Choose " + (_controller.RequiredPerformanceMode == "capture" ? "raw → conversion" : "full pointer") + " under Ghost Camera Performance Check before Enable."; _budgetStatus.Visibility = Visibility.Visible; }
        _start.IsEnabled = available && budgetAvailable && permittedMode && (!live || _devices.SelectedItem is CameraDevice);
        if (!budgetAvailable) _start.ToolTip = budget!.Message;
        else _start.ToolTip = null;
        _stop.IsEnabled = !_stopping && _visible.SessionId.HasValue;
        bool showing = _runningPerformance != "capture" && !simulated && _showPreview.IsChecked == true && _visible.SessionId.HasValue &&
            _visible.State is not (TrackingState.Stopping or TrackingState.Unavailable);
        var frame = showing ? _controller.ReadPreview(_previewTiming.SelectedIndex == 0 ? _visible.Observation?.Sequence : null) : null;
        _liveView.Update(frame, _visible.Observation, _mirrorCamera.IsChecked == true);
        if (frame is not null)
        { _lastSessionPreviewFps = _liveView.UniquePresentedFps; _lastSessionPreviewFrames = _liveView.UniquePresentedFrames; }
        _previewStatus.Text = !showing ? (_runningPerformance == "capture" && _visible.SessionId.HasValue ? "Performance check: metadata only · no inference or image preview. See Performance dashboard. Raw 30 s, then conversion 30 s, then automatic release." : simulated ? "SIMULATED landmarks only. No camera image is used." : live&&_showPreview.IsChecked==true?"Camera off · Enable shows the local image here.":"") : _controller.PreviewFailed ?
            "Live view is unavailable. Tracking and Stop remain available." :
            frame is null ? "Waiting for the local camera view…" :
            $"Preview: {_liveView.UniquePresentedFps:F1} unique frames/s · local frame age: {Environment.TickCount64 - frame.ReceivedMs} ms. " +
            "Model landmarks only on matching frames. Move one hand into view; keep the index visible as the wrist leaves.";
        _previewTiming.Visibility = showing ? Visibility.Visible : Visibility.Collapsed;
        _overlayMode.Visibility = showing ? Visibility.Visible : Visibility.Collapsed;
        _mirrorCamera.Visibility = showing ? Visibility.Visible : Visibility.Collapsed;
        bool canCalibrate = _visible.State is TrackingState.Ready or TrackingState.Observing
            or TrackingState.Lost or TrackingState.Stale or TrackingState.Partial or TrackingState.Ambiguous;
        if (canCalibrate) _preparingCalibration = false;
        _calibrate.IsEnabled = (canCalibrate || available) && _calibration.Visibility != Visibility.Visible;
        if (!canCalibrate && _calibration.Visibility == Visibility.Visible && (!_preparingCalibration || !available))
        {
            _calibration.End();
            _refresh.Interval = TimeSpan.FromMilliseconds(80);
        }
        _refresh.Interval = TimeSpan.FromMilliseconds(showing ? 16 :
            _calibration.Visibility == Visibility.Visible ? 25 : 80);
        _calibration.Update(_visible);
        var input = _calibration.LearningInput(_visible, _visible.Source == TrackingSource.Camera
            ? (_devices.SelectedItem as CameraDevice)?.Id ?? "unknown" : RecordedIdentity);
        _learning.Observe(_visible, input.Context, input.Point, _calibration.PracticeHeld, _calibration.LearningSetupIssue);
        _gesturePractice.Update(_visible,_calibration.DesktopContext(_visible,_visible.Source==TrackingSource.Camera
            ? (_devices.SelectedItem as CameraDevice)?.Id??"unknown" : RecordedIdentity));
        // Polling handles expiry/Stop only; fresh positions arrive independently via the bounded wakeup.
        if (_visible.Observation is null) DeliverDesktopObservation();
        _desktopStatus.Text=_desktopPreview.IsChecked==true ? "Desktop Ghost · "+(DesktopStatus?.Invoke()??"Preview unavailable") : "";
        _performance.Text = _controller.PerformanceSummary(_visible) +
            $"\nHost accepted {_visible.AcceptedObservations} · pointer {((DesktopUpdateCount?.Invoke() ?? 0) - _pointerUpdateStart) / Math.Max(.001, _pointerClock.Elapsed.TotalSeconds):F1} updates/s · preview {_liveView.UniquePresentedFps:F1} unique/s" +
            $"\nAge {_visible.HostObservationAge?.TotalMilliseconds:F0} ms · {_visible.State} · {_cameraStatus} · {(_stopping ? "Stop requested" : "Stop available during run")}" +
            $"\n{(_desktopPreview.IsChecked == true ? DesktopStatus?.Invoke() : "Desktop preview off")} · coalesced {_pointerNotification.Coalesced}";
        var selectedContext=_calibration.DesktopContext(_visible,(_devices.SelectedItem as CameraDevice)?.Id??"recorded");
        _qualification.Text=$"{(_devices.SelectedItem as CameraDevice)?.Name??"No camera selected"} · "+
            (selectedContext is {} configured?$"Display {configured.Display.Number} · {(configured.Hand==1?"Left":"Right")} hand · Mirror {(configured.Mirror?"on":"off")}":"Choose display and hand in Calibration")+
            "\n"+(_visible.State==TrackingState.Stopped?"Stop complete · worker exited":
            _visible.Observation is {Visual:not null}?"Visual fingertip continuation · preview only":
            _visible.Observation is {Partial:true}?"Partial hand · gesture evidence incomplete":
            _visible.Observation is {Tracked:true}?"Full hand observed": "Finger tracking unavailable")+
            " · Pocket actions disabled";
        string source = !live ? (_recordedDemo.SelectedIndex == 1 ? "simulated practice" : _recordedDemo.SelectedIndex==2 ? "simulated image motion" : "recorded tracking") : _fixtureFile is not null ? "test video" : "live camera";
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
        _sourceStatus.Text = (!live ? (_recordedDemo.SelectedIndex == 1 ? "SIMULATED practice" : _recordedDemo.SelectedIndex==2 ? "SIMULATED image motion · no camera or model inference" : "Recorded source") : _fixtureFile is not null ? "Native video fixture" : "Live camera")
            + " · " + cameraStatus;
        string next = _visible.State switch
        {
            TrackingState.Off => live ? "Choose a source and enable it when ready." : "Ready for an explicit start with controlled frames.",
            TrackingState.Starting => "Starting the local tracking runtime…",
            TrackingState.Ready => "Reacquiring · show your full selected hand.",
            TrackingState.Observing => "Tracking hand",
            TrackingState.Lost => "Tracking lost · show your full selected hand to reacquire.",
            TrackingState.Partial when _visible.Observation?.Visual is not null => "Tracking visible finger · short visual continuation, preview only.",
            TrackingState.Partial => "Tracking visible finger · partial model, preview only.",
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

    internal void EnableDesktopPreviewFixture()
    {
        AcceptanceHost.ValidateDesktop();
        _desktopPreview.IsChecked=true;
        _desktopPreview.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        _calibration.LearningFixtureObservation(Guid.NewGuid(),0,new(.5,.5)); // Settings only; returned observation is not submitted.
    }
    public object TestSnapshot()
    {
        var sample = _visible.Observation;
        return new
        {
            VideoPreview = _liveView.TestState(), WindowLoaded = IsLoaded, State = _visible.State.ToString(), _visible.SessionId, Source = _visible.Source?.ToString(),
            Scrollbar=_trackingScroll.VerticalScrollBarVisibility.ToString(),ScrollViewport=_trackingScroll.ViewportHeight,ScrollExtent=_trackingScroll.ExtentHeight,
            Failure = _visible.Failure?.ToString(), _visible.AcceptedObservations, _visible.SupersededObservations,
            CanStart = _start.IsEnabled, CanStop = _stop.IsEnabled, Status = _status.Text,
            SourceStatus = _sourceStatus.Text, SourceChoicesEnabled = _live.IsEnabled && _recorded.IsEnabled,
            BudgetStatus = _budgetStatus.Text, InventoryStatus = _inventoryStatus.Text, CameraCount = _devices.Items.Count, DeviceSelected = _devices.SelectedIndex >= 0,
            _controller.LastWorkerPid, _controller.ForcedStop, _controller.WorkerPhase, _controller.WorkerNativeCode,
            Sequence = sample?.Sequence, WristX = sample?.Point(0)?.X,
            SourceMs = sample is null ? (long?)null : sample.ResultMonoMs - sample.CaptureMonoMs,
            HostAgeMs = _visible.HostObservationAge?.TotalMilliseconds,
            Calibration = _calibration.TestSnapshot(), Learning = _learning.TestSnapshot(), IndexX = sample?.Point(8)?.X,
            VisualIndex = sample?.Visual, Title
        };
    }
}
