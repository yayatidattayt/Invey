using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Text.Json;
using Pocket.Core;
using Pocket.Core.Tracking;

namespace Pocket.App.Tracking;

internal sealed class CalibrationPanel : StackPanel
{
    private CalibrationReadiness _readiness = new();
    private CalibrationPractice _practice = new();
    private readonly CalibrationPreferencesStore? _store;
    private CalibrationPreferences? _saved;
    private CalibrationDisplay? _desiredDisplay;
    private CalibrationMapping? _mapping;
    private bool _refreshingDisplays;
    private int _readyDisplayed;
    private Guid? _diagnosticSession;
    private string _endReason = "SourceEnded";
    private readonly Canvas _preview = new() { Height = 126, ClipToBounds = true };
    private readonly Rectangle _frame = new() { Stroke = SystemColors.ControlDarkBrush, StrokeThickness = 1 };
    private readonly Rectangle _region = new() { Stroke = SystemColors.HighlightBrush, StrokeThickness = 1 };
    private readonly Ellipse _tip = new() { Width = 9, Height = 9, Fill = SystemColors.HighlightBrush };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _practiceStatus = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _settingsStatus = new() { TextWrapping = TextWrapping.Wrap };
    private readonly ComboBox _display = new() { DisplayMemberPath = nameof(CalibrationDisplayChoice.Label) };
    private readonly CheckBox _mirror = new() { Content = "_Mirror horizontal pointer mapping", Margin = new Thickness(0, 0, 12, 0) };
    private readonly ComboBox _hand = new() { ItemsSource = new[] { "Any hand", "Left hand", "Right hand" }, SelectedIndex = 0 };
    private readonly Slider _margin = new() { Minimum = 5, Maximum = 30, Value = 12, TickFrequency = 1, IsSnapToTickEnabled = true };
    private readonly Slider _sensitivity = new() { Minimum = 50, Maximum = 200, Value = 100, TickFrequency = 5, IsSnapToTickEnabled = true };
    private readonly Canvas _workspace = new() { Height = 108, ClipToBounds = true };
    private readonly Rectangle _screen = new() { Stroke = SystemColors.ControlDarkBrush, StrokeThickness = 1 };
    private readonly Rectangle _target = new() { Stroke = SystemColors.HighlightBrush, StrokeThickness = 2 };
    private readonly Ellipse _ghost = new() { Width = 9, Height = 9, Fill = SystemColors.HighlightBrush };
    private readonly Button _save = new() { Content = "Sa_ve settings" };
    private CalibrationSample _sample;
    private DipPoint? _mapped;
    private bool _completed;
    private bool _focusRequested;
    private readonly CheckBox _pointerPreview = new() { Content = "Experimental _finger pointer preview" };
    private readonly TextBlock _fingerStatus = new() { TextWrapping = TextWrapping.Wrap };
    private readonly FingerPointer _fingerEngine = new();
    private FingerPointerSample _fingerSample = new(FingerPointerState.Off, null, []);
    private readonly Dictionary<string, long> _pointerCounts = [];
    private long _pointerSequence = -1;
    private PointMoments _rawMoments = new(), _filteredMoments = new();

    public CalibrationPanel(Func<Task> stop, CalibrationPreferencesStore? store)
    {
        _store = store;
        Margin = new Thickness(0, 10, 0, 0);
        Visibility = Visibility.Collapsed;
        var cancel = new Button { Content = "Cancel cali_bration" };
        var skip = new Button { Content = "S_kip practice" };
        var repeat = new Button { Content = "Re_peat practice" };
        cancel.Click += async (_, _) => { _endReason = "Cancel"; RestoreSaved(); await stop(); };
        skip.Click += async (_, _) => { _endReason = "Skip"; RestoreSaved(); await stop(); };
        repeat.Click += (_, _) => Reset();
        _save.Click += (_, _) => Save();
        AutomationProperties.SetName(_preview, "Camera geometry and fingertip marker. No video preview.");
        AutomationProperties.SetName(_hand, "Hand used for calibration");
        AutomationProperties.SetName(_margin, "Working region inset percent");
        AutomationProperties.SetLiveSetting(_status, AutomationLiveSetting.Polite);
        AutomationProperties.SetLiveSetting(_practiceStatus, AutomationLiveSetting.Polite);
        AutomationProperties.SetLiveSetting(_settingsStatus, AutomationLiveSetting.Polite);
        AutomationProperties.SetName(_display, "Selected calibration display");
        AutomationProperties.SetName(_sensitivity, "Sensitivity percent");
        AutomationProperties.SetName(_workspace, "Selected display practice surface. The target is centered.");
        _preview.Children.Add(_frame);
        _preview.Children.Add(_region);
        _preview.Children.Add(_tip);
        _preview.SizeChanged += (_, _) => Draw();
        _workspace.Children.Add(_screen);
        _workspace.Children.Add(_target);
        _workspace.Children.Add(_ghost);
        _workspace.SizeChanged += (_, _) => Draw();
        _display.SelectionChanged += (_, _) =>
        {
            if (_refreshingDisplays) return;
            _desiredDisplay = (_display.SelectedItem as CalibrationDisplayChoice)?.Geometry;
            Reset();
        };
        _mirror.IsVisibleChanged += (_, _) => FocusWhenVisible();
        _mirror.Click += (_, _) => Reset();
        _hand.SelectionChanged += (_, _) => Reset();
        _margin.ValueChanged += (_, _) => Reset();
        _sensitivity.ValueChanged += (_, _) => Reset();
        Children.Add(new TextBlock { Text = "Calibration preview", FontWeight = FontWeights.SemiBold });
        Children.Add(new Label { Content = "Active _display", Target = _display, Padding = new Thickness(0, 4, 0, 0) });
        Children.Add(_display);
        Children.Add(new WrapPanel { Children = { _mirror, _hand } });
        Children.Add(new Label { Content = "Working region _inset", Target = _margin, Padding = new Thickness(0, 4, 0, 0) });
        Children.Add(_margin);
        Children.Add(new Label { Content = "_Sensitivity", Target = _sensitivity, Padding = new Thickness(0, 4, 0, 0) });
        Children.Add(_sensitivity);
        _pointerPreview.Click += (_, _) => Reset();
        AutomationProperties.SetLiveSetting(_fingerStatus, AutomationLiveSetting.Polite);
        Children.Add(_pointerPreview);
        Children.Add(_fingerStatus);
        Children.Add(_preview);
        Children.Add(_status);
        Children.Add(new TextBlock { Text = "Display practice", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 0) });
        Children.Add(_workspace);
        Children.Add(_practiceStatus);
        Children.Add(_settingsStatus);
        foreach (var button in new[] { cancel, skip, repeat, _save })
            button.Margin = new Thickness(0, 6, 6, 0);
        Children.Add(new WrapPanel { Children = { cancel, skip, repeat, _save } });

        if (_store is not null)
        {
            var loaded = _store.Load();
            _saved = loaded.Preferences;
            if (loaded.Unreadable) Announce(_settingsStatus, "Saved calibration could not be read. Defaults are available.");
        }
        RestoreSaved();
    }

    private Guid? _learningFrameSession;
    internal LearningContext? DesktopContext(TrackingSnapshot snapshot,string device)
    {
        if(_hand.SelectedIndex==0 || _display.SelectedItem is not CalibrationDisplayChoice choice ||
            snapshot.Observation?.FrameSize is not { IsValid:true } frame)return null;
        return new(snapshot.Source?.ToString()??"",LearningContext.HashDevice(device),frame.Width,frame.Height,
            choice.Geometry,_hand.SelectedIndex,_mirror.IsChecked==true,(int)_margin.Value,(int)_sensitivity.Value);
    }
    private TrackingFrameSize? _learningFrame;
    internal string? LearningSetupIssue => _display.SelectedItem is not CalibrationDisplayChoice || _mapping is null
        ? "Choose a connected display in Calibration settings." : _hand.SelectedIndex == 0
        ? "Choose Left hand or Right hand in Calibration settings." : null;

    internal (LearningContext? Context, DipPoint? Point) LearningInput(TrackingSnapshot snapshot, string device)
    {
        if (snapshot.SessionId != _learningFrameSession)
        { _learningFrameSession = snapshot.SessionId; _learningFrame = null; }
        if (snapshot.Observation?.FrameSize is { IsValid: true } observedFrame) _learningFrame = observedFrame;
        if (Visibility != Visibility.Visible || _mapping is null || _hand.SelectedIndex == 0 ||
            _display.SelectedItem is not CalibrationDisplayChoice choice || _learningFrame is not { } frame ||
            snapshot.SessionId is null) return (null, null);
        var hand = snapshot.Observation;
        var context = new LearningContext(snapshot.Source?.ToString() ?? "", LearningContext.HashDevice(device),
            frame.Width, frame.Height, choice.Geometry, _hand.SelectedIndex, _mirror.IsChecked == true,
            (int)_margin.Value, (int)_sensitivity.Value);
        DipPoint? point = null;
        if (snapshot.State == TrackingState.Observing && hand is { Tracked: true, IsValid: true } &&
            hand.Handedness == (_hand.SelectedIndex == 1 ? HandSide.Left : HandSide.Right) &&
            snapshot.HostObservationAge is { } age && age >= TimeSpan.Zero &&
            age.TotalMilliseconds + hand.ResultMonoMs - hand.CaptureMonoMs < 150 &&
            hand.Point(8) is { } tip && _mapping.Map(new(tip.X, tip.Y)) is { } logical)
        {
            var physical = _mapping.ToPhysical(logical);
            point = new((physical.X - choice.Geometry.X) / (choice.Geometry.Width - 1),
                (physical.Y - choice.Geometry.Y) / (choice.Geometry.Height - 1));
        }
        return (context, point);
    }

    internal TrackingSnapshot LearningFixtureObservation(Guid session, long sequence, DipPoint normalized)
    {
        AcceptanceHost.ValidateDesktop();
        _hand.SelectedIndex = 2;
        _mirror.IsChecked = false;
        _margin.Value = 12; _sensitivity.Value = 100;
        if (_mapping is null || _display.SelectedItem is not CalibrationDisplayChoice choice)
            throw new InvalidOperationException("No fixture display.");
        var camera = _mapping.Inverse(new(normalized.X * (choice.Geometry.Width - 1) * 96 / choice.Geometry.Dpi,
            normalized.Y * (choice.Geometry.Height - 1) * 96 / choice.Geometry.Dpi)) ?? throw new InvalidOperationException("Invalid fixture point.");
        var points = Enumerable.Repeat(new ImageLandmark(camera.X, camera.Y, 0), 21).ToArray();
        points[4] = new(Math.Clamp(camera.X + .1, 0, 1), camera.Y, 0);
        points[5] = new(Math.Clamp(camera.X - .08, 0, 1), camera.Y, 0);
        points[17] = new(Math.Clamp(camera.X + .08, 0, 1), camera.Y, 0);
        var hand = new HandObservation(session, sequence, sequence * 20, sequence * 20,
            System.Collections.Immutable.ImmutableArray.Create(points), HandSide.Right, FrameSize: new(640, 480));
        return new(TrackingState.Observing, session, TrackingSource.Simulation, hand, TimeSpan.Zero, null, false,
            sequence + 1, 0, LastSequence: sequence);
    }

    internal bool PracticeHeld => _practice.State == PracticeState.Held;
    internal bool MirrorPreview => _mirror.IsChecked == true;

    public void Begin(Guid? diagnosticSession, bool focus = true)
    {
        _focusRequested = focus;
        _diagnosticSession = diagnosticSession;
        _pointerCounts.Clear();
        _pointerSequence = -1;
        _rawMoments = new();
        _filteredMoments = new();
        _endReason = "SourceEnded";
        _readiness = new();
        RestoreSaved();
        RefreshDisplays();
        _readyDisplayed = 0;
        Visibility = Visibility.Visible;
        FocusWhenVisible();
    }

    private void FocusWhenVisible()
    {
        if (!_focusRequested || !_mirror.IsVisible) return;
        _focusRequested = !_mirror.Focus();
    }

    internal void EnableFixturePreview()
    {
        _mirror.IsChecked = false; // Explicit synthetic fixture mapping, independent of earlier saved test preferences.
        _pointerPreview.IsChecked = true;
        Reset();
    }

    public void End()
    {
        _focusRequested = false;
        WriteDiagnostics();
        Visibility = Visibility.Collapsed;
        Reset();
    }

    private void Reset()
    {
        _readiness.Invalidate();
        _fingerEngine.Reset();
        _fingerSample = new(FingerPointerState.Searching, null, []);
        _pointerSequence = -1;
        _practice = new();
        _sample = default;
        _mapped = null;
        _completed = false;
        var display = (_display.SelectedItem as CalibrationDisplayChoice)?.Geometry;
        _mapping = display is { } geometry ? new(geometry, _margin.Value / 100, _sensitivity.Value / 100, _mirror.IsChecked == true) : null;
        _save.IsEnabled = _mapping is not null && _store?.CanSave == true;
        Draw();
    }

    public void RefreshDisplays()
    {
        CalibrationDisplayChoice[] choices;
        try { choices = CalibrationDisplays.Find(); }
        catch (System.ComponentModel.Win32Exception)
        {
            choices = [];
            Announce(_settingsStatus, "Windows could not read display geometry. Try calibration again.");
        }
        ApplyDisplays(choices);
    }

    internal void ApplyDisplays(CalibrationDisplayChoice[] choices)
    {
        var chosen = _desiredDisplay is { } desired
            ? choices.FirstOrDefault(choice => choice.Geometry == desired)
            : choices.FirstOrDefault(choice => choice.Primary) ?? choices.FirstOrDefault();
        _refreshingDisplays = true;
        try
        {
            _display.ItemsSource = choices;
            _display.SelectedItem = chosen;
        }
        finally { _refreshingDisplays = false; }
        _desiredDisplay ??= chosen?.Geometry;
        Reset();
    }

    private void RestoreSaved()
    {
        _desiredDisplay = _saved?.Display;
        _hand.SelectedIndex = _saved?.Hand ?? 0;
        _mirror.IsChecked = _saved?.Mirror == 1;
        _margin.Value = _saved?.InsetPercent ?? 12;
        _sensitivity.Value = _saved?.SensitivityPercent ?? 100;
    }

    private void Save()
    {
        if (_store?.CanSave != true || _display.SelectedItem is not CalibrationDisplayChoice choice) return;
        var value = new CalibrationPreferences(1, choice.Geometry, _mirror.IsChecked == true ? 1 : 0,
            _hand.SelectedIndex, (int)_margin.Value, (int)_sensitivity.Value);
        try
        {
            _store.Save(value);
            _saved = value;
            Announce(_settingsStatus, "Calibration settings saved.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { Announce(_settingsStatus, "Could not save calibration. Previous settings remain available."); }
    }

    public void Update(TrackingSnapshot snapshot)
    {
        if (Visibility != Visibility.Visible) return;
        HandSide? side = _hand.SelectedIndex switch { 1 => HandSide.Left, 2 => HandSide.Right, _ => null };
        _sample = _readiness.Observe(snapshot, side, _mirror.IsChecked == true, _margin.Value / 100);
        var hand = snapshot.Observation;
        if (_pointerPreview.IsChecked == true)
        {
            _fingerSample = _fingerEngine.Observe(snapshot, side);
            if (snapshot.LastSequence != _pointerSequence)
            {
                _pointerSequence = snapshot.LastSequence;
                string state = _fingerSample.State.ToString();
                _pointerCounts[state] = _pointerCounts.GetValueOrDefault(state) + 1;
                if (_fingerSample.Position is { } filtered && snapshot.Observation?.Point(8) is { } rawTip)
                {
                    _rawMoments.Add(new(rawTip.X, rawTip.Y));
                    _filteredMoments.Add(filtered);
                }
            }
            Announce(_fingerStatus, _fingerSample.State switch {
                FingerPointerState.FingerVisual => "Tracking visible finger · visual continuation, preview only",
                FingerPointerState.Stable => "Tracking hand",
                FingerPointerState.Partial => "Tracking partial hand · preview only",
                FingerPointerState.Stabilizing => "Reacquiring",
                FingerPointerState.Ambiguous => "Show one hand",
                _ => "Finger tracking lost · show your full selected hand"
            });
        }
        else _fingerStatus.Text = "";
        DipPoint? anchor = _pointerPreview.IsChecked == true ? _fingerSample.Position :
            hand?.Point(8) is { } raw ? new(raw.X, raw.Y) : null;
        _mapped = (_sample.State is CalibrationState.Ready or CalibrationState.Stabilizing) &&
            hand is { Tracked: true } && anchor is { } point
            ? _mapping?.Map(point) : null;
        var logical = WorkspaceFraction();
        bool overTarget = logical is { X: >= .38 and <= .62, Y: >= .32 and <= .68 };
        var practice = _practice.Update(snapshot, _sample.State == CalibrationState.Ready && logical.HasValue, overTarget);
        _completed |= practice == PracticeState.Released;
        Draw();
        string next = _mapping is null ? "Choose a connected display to calibrate." : _sample.State switch
        {
            CalibrationState.Ready => "Ready. The fingertip is visible and steady in this working region.",
            CalibrationState.Stabilizing => "Hold your fingertip steadily inside the working region.",
            CalibrationState.OutsideRegion => "Move one hand fully into the working region.",
            CalibrationState.WrongHand => "Show the selected hand.",
            CalibrationState.Ambiguous => "Show one clearly visible hand.",
            CalibrationState.Stale => "Waiting for fresh tracking.",
            _ => "Waiting for a visible hand."
        };
        if (_sample.State == CalibrationState.Ready && _status.Text != next && _mapping is not null) _readyDisplayed++;
        Announce(_status, next);
        Announce(_practiceStatus, practice switch
        {
            PracticeState.Held => "Pinch held. Open your fingers to release.",
            PracticeState.Canceled => "Practice hold canceled. Open your hand to try again.",
            PracticeState.PinchCandidate => "Keep your pinch steady.",
            _ when _completed => "Practice complete. Repeat or save your settings.",
            PracticeState.Move => "Move into the outlined target and hold a pinch.",
            _ when _sample.State == CalibrationState.Ready && _mapped.HasValue => "Open your hand to start practice.",
            _ => "Practice becomes available when tracking is ready."
        });
    }

    private void Draw()
    {
        DrawWorkspace();
        var box = _sample.Frame is { } size
            ? CalibrationReadiness.Fit(size, _preview.ActualWidth, _preview.ActualHeight) : default;
        _frame.Visibility = _region.Visibility = box.IsValid ? Visibility.Visible : Visibility.Collapsed;
        _tip.Visibility = Visibility.Collapsed;
        if (!box.IsValid) return;
        Place(_frame, box.X, box.Y, box.Width, box.Height);
        double inset = _margin.Value / 100;
        Place(_region, box.X + box.Width * inset, box.Y + box.Height * inset,
            box.Width * (1 - 2 * inset), box.Height * (1 - 2 * inset));
        if (_sample.Point is { } tip)
        {
            _tip.Visibility = Visibility.Visible;
            Place(_tip, box.X + tip.X * box.Width - 4.5, box.Y + tip.Y * box.Height - 4.5, 9, 9);
        }
    }

    private DipPoint? WorkspaceFraction(bool fingerPreview = false)
    {
        var mapped = fingerPreview ? _fingerSample.Position is { } raw ? _mapping?.Map(raw) : null : _mapped;
        if (mapped is not { } point || _mapping is null ||
            _display.SelectedItem is not CalibrationDisplayChoice { Geometry: var display }) return null;
        var physical = _mapping.ToPhysical(point);
        return new((physical.X - display.X) / (display.Width - 1), (physical.Y - display.Y) / (display.Height - 1));
    }

    private void DrawWorkspace()
    {
        var box = _display.SelectedItem is CalibrationDisplayChoice { Geometry: var display }
            ? CalibrationReadiness.FitAspect(display.Width, display.Height, _workspace.ActualWidth, _workspace.ActualHeight)
            : default;
        _screen.Visibility = _target.Visibility = box.IsValid ? Visibility.Visible : Visibility.Collapsed;
        _ghost.Visibility = Visibility.Collapsed;
        if (!box.IsValid) return;
        Place(_screen, box.X, box.Y, box.Width, box.Height);
        Place(_target, box.X + .38 * box.Width, box.Y + .32 * box.Height, .24 * box.Width, .36 * box.Height);
        _target.Fill = _practice.State == PracticeState.Held ? SystemColors.HighlightBrush : null;
        if (WorkspaceFraction(_pointerPreview.IsChecked == true) is not { } point) return;
        _ghost.Visibility = Visibility.Visible;
        Place(_ghost, box.X + point.X * box.Width - 4.5, box.Y + point.Y * box.Height - 4.5, 9, 9);
    }

    private static void Announce(TextBlock label, string text)
    {
        if (label.Text == text) return;
        label.Text = text;
        UIElementAutomationPeer.CreatePeerForElement(label)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    private object Counts() => new
    {
        _readiness.Fresh, _readiness.WrongHandObservations, _readiness.Consecutive, _readiness.MaxConsecutive, _readiness.Resets,
        _readiness.ReadyEntries, ReadyDisplayed = _readyDisplayed
    };

    private void WriteDiagnostics()
    {
        string? directory = RecordedTrackingController.DiagnosticDirectory;
        if (_diagnosticSession is not { } session || !Directory.Exists(directory)) return;
        try { File.WriteAllText(System.IO.Path.Combine(directory!, session + ".calibration.json"),
            JsonSerializer.Serialize(new { Session = session, EndReason = _endReason,
                Mirror = _mirror.IsChecked == true, HandSelection = _hand.SelectedIndex, UiObserved = Counts(),
                PointerPreview = _pointerPreview.IsChecked == true, PointerUiCounts = _pointerCounts,
                PointerMoments = new { Samples = _rawMoments.Samples, RawNormalizedRms = _rawMoments.RmsRadius,
                    FilteredNormalizedRms = _filteredMoments.RmsRadius } })); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { System.Diagnostics.Trace.TraceWarning("Calibration diagnostic write failed: 0x{0:X8}", ex.HResult); }
    }

    private static void Place(FrameworkElement item, double left, double top, double width, double height)
    {
        Canvas.SetLeft(item, left);
        Canvas.SetTop(item, top);
        item.Width = width;
        item.Height = height;
    }

    public object TestSnapshot() => new { Open = Visibility == Visibility.Visible, State = _sample.State.ToString(),
        Mirrored = _mirror.IsChecked == true, TipX = _sample.Point?.X, Frame = _sample.Frame,
        Display = (_display.SelectedItem as CalibrationDisplayChoice)?.Geometry,
        Mapped = _mapped, Physical = _mapped is { } point ? _mapping?.ToPhysical(point) : null,
        Status = _status.Text, Practice = _practice.State.ToString(), PracticeStatus = _practiceStatus.Text,
        SettingsStatus = _settingsStatus.Text, Counts = Counts(),
        PointerPreview = _pointerPreview.IsChecked == true,
        PointerToggle = _pointerPreview.IsVisible ? _pointerPreview.PointToScreen(new Point(24, _pointerPreview.ActualHeight / 2)) : default,
        PointerState = _fingerSample.State.ToString(),
        PointerPosition = _fingerSample.Position, PointerMarkerVisible = _ghost.IsVisible,
        PointerMarkerX = _ghost.IsVisible && double.IsFinite(Canvas.GetLeft(_ghost)) ? (double?)Canvas.GetLeft(_ghost) : null,
        PointerActionEligible = _fingerSample.ActionEligible, FingerStatus = _fingerStatus.Text,
        Focus = _mirror.IsKeyboardFocused ? "Mirror" : _hand.IsKeyboardFocused ? "Hand" : null };
}
