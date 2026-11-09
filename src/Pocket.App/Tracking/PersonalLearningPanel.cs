using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using Pocket.Core;
using Pocket.Core.Tracking;

namespace Pocket.App.Tracking;

internal sealed class PersonalLearningPanel : StackPanel
{
    private readonly PersonalLearningStore? _store;
    private LearningProfile _profile = LearningProfile.Empty;
    private bool _writable, _loading, _busy, _held;
    private int _epoch;
    private readonly LearningLesson _lesson = new();
    private readonly LearningQuality _quality = new();
    private LearningContext? _context;
    private TrackingSnapshot? _snapshot;
    private DipPoint? _baseline, _prediction;
    private readonly CheckBox _enabled = new() { Content = "Learn from my tracking sessions" };
    private readonly CheckBox _personal = new() { Content = "Use accepted personal correction (preview only)" };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _modelState = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _results = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Canvas _canvas = new() { Height = 180, ClipToBounds = true, Background = SystemColors.ControlLightBrush };
    private readonly Ellipse _target = new() { Width = 16, Height = 16, Stroke = SystemColors.ControlTextBrush, StrokeThickness = 2 };
    private readonly Ellipse _baseDot = new() { Width = 7, Height = 7, Fill = SystemColors.ControlDarkBrush };
    private readonly Ellipse _personalDot = new() { Width = 7, Height = 7, Fill = SystemColors.HighlightBrush };
    private readonly Button _primary = new() { Content = "Start lesson", MinHeight = 36, Padding = new(14, 6, 14, 6) };
    private readonly Button _cancel = new() { Content = "Cancel lesson", Margin = new(8, 0, 0, 0) };
    private readonly Expander _details = new() { Header = "Learning details", Margin = new(0, 8, 0, 0) };
    private bool _pendingBegin;
    private string? _setupIssue;
    internal event Action? RequestCalibration;
    internal event Action? LessonStarted;
    private readonly ComboBox _feedback = new() { ItemsSource = Enum.GetValues<LearningFeedback>(), SelectedIndex = 0 };
    public PersonalLearningPanel(string? profileRoot, bool writable)
    {
        _store = profileRoot is null ? null : new(System.IO.Path.Combine(profileRoot, "Learning"), writable);
        var loaded = _store?.Load();
        _profile = loaded?.Profile ?? LearningProfile.Empty;
        _writable = loaded?.Writable == true;
        Children.Add(new TextBlock { Text = "Personal Learning", FontWeight = FontWeights.SemiBold, Margin = new(0, 12, 0, 4) });
        Children.Add(new TextBlock { Text = "Teach a small pointer correction with a guided target lesson. Experimental preview only.",
            TextWrapping = TextWrapping.Wrap });
        Children.Add(_enabled);
        _loading = true; _enabled.IsChecked = _profile.Enabled; _personal.IsChecked = _profile.UsePersonal; _loading = false;
        _enabled.IsEnabled = _personal.IsEnabled = _writable;
        _enabled.Checked += Preference; _enabled.Unchecked += Preference;
        _personal.Checked += Preference; _personal.Unchecked += Preference;
        Children.Add(_status); Children.Add(_modelState);
        _canvas.Children.Add(_target); _canvas.Children.Add(_baseDot); _canvas.Children.Add(_personalDot);
        _canvas.SizeChanged += (_, _) => Draw();
        Children.Add(_canvas);
        _primary.Click += async (_, _) =>
        {
            if (_lesson.Phase == LessonPhase.Idle) Begin();
            else if (_lesson.Phase == LessonPhase.Complete) await EvaluateAsync();
            else { _lesson.Confirm(); Refresh(); }
        };
        _cancel.Click += (_, _) => Cancel();
        Children.Add(new WrapPanel { Children = { _primary, _cancel } });

        var details = new StackPanel();
        details.Children.Add(_personal);
        Add(details, "View learning results", ShowResults);
        details.Children.Add(_results);
        var recovery = new WrapPanel();
        Add(recovery, "Restore previous model", () => Change(p => p with { Current = p.Previous, Previous = p.Current },
            "Previous model restored; press again to reapply."));
        Add(recovery, "Reset learning", ResetLearning);
        details.Children.Add(recovery);
        var options = new WrapPanel();
        Add(options, "Retry target", () => { _lesson.Retry(); Refresh(); });
        Add(options, "Skip target", () => { _lesson.Skip(); Refresh(); });
        details.Children.Add(options);
        details.Children.Add(new TextBlock { Text = "Practice feedback only — this does not confirm a lesson target.",
            TextWrapping = TextWrapping.Wrap, Margin = new(0, 8, 0, 0) });
        var feedback = new WrapPanel { Children = { _feedback } };
        Add(feedback, "Give feedback", () =>
        {
            if (!_profile.Enabled || _snapshot?.SessionId is null)
            { _status.Text = "Enable tracking and learning before giving practice feedback."; return; }
            _quality.Feedback((LearningFeedback)_feedback.SelectedItem);
            _status.Text = "Practice feedback saved as a count. No gesture classifier trained; more labeled evidence is needed.";
        });
        details.Children.Add(feedback);
        details.Children.Add(new TextBlock { Text = "Outline = intended target; gray = baseline; blue = personal preview. " +
            "Confirm your intended position, not a dot you chased. Samples stay in memory only.",
            TextWrapping = TextWrapping.Wrap });
        _details.Content = details; Children.Add(_details);
        _status.Text = loaded?.Writable == false ? loaded.Notice : "Enable learning, choose your hand in Calibrate, then start tracking.";
        Refresh(false);
    }
    private static void Add(Panel panel, string title, Action action) => Add(panel.Children, title, action);
    private static void Add(UIElementCollection children, string title, Action action)
    {
        var b = new Button { Content = title, Margin = new(0, 4, 6, 0) };
        b.Click += (_, _) => action(); children.Add(b);
    }
    private void Preference(object sender, RoutedEventArgs args)
    {
        if (_loading) return;
        Cancel();
        Change(p => p with { Enabled = _enabled.IsChecked == true, UsePersonal = _personal.IsChecked == true },
            _enabled.IsChecked == true ? "Learning enabled. Camera activation is separate." : "Learning disabled. Ordinary tracking continues.");
    }
    private void Change(Func<LearningProfile, LearningProfile> change, string success)
    {
        if (_store is null || !_writable || _busy) { _status.Text = "Learning profile unavailable or candidate evaluation in progress."; return; }
        try { _profile = _store.Save(change(_profile), _profile.Revision); _status.Text = success; }
        catch (Exception ex) when (PersonalLearningStore.Expected(ex)) { _status.Text = "Learning save failed. Previous disk model preserved. " + ex.Message; }
        finally
        {
            _loading = true; _enabled.IsChecked = _profile.Enabled; _personal.IsChecked = _profile.UsePersonal; _loading = false;
            RecomputePrediction();
            Refresh(false);
        }
    }
    private void ResetLearning()
    {
        Cancel();
        if (_store is null) return;
        try
        {
            _profile = _store.Reset(); _writable = true; _enabled.IsEnabled = _personal.IsEnabled = true;
            _loading = true; _enabled.IsChecked = false; _personal.IsChecked = true; _loading = false;
            _status.Text = "Learning deleted and disabled. Pocket and calibration settings unchanged.";
        }
        catch (Exception ex) when (PersonalLearningStore.Expected(ex)) { _status.Text = "Learning reset failed: " + ex.Message; }
        RecomputePrediction(); Refresh(false);
    }
    private void Begin()
    {
        if (_busy) { _status.Text = "Finishing the current evaluation."; return; }
        if (!_writable) { _status.Text = "Learning profile is read-only or needs recovery. Open Learning details."; return; }
        if (!_profile.Enabled) { _status.Text = "Turn on Learn from my tracking sessions above."; return; }
        if (_snapshot?.SessionId is null || _snapshot.State is TrackingState.Off or TrackingState.Stopped or TrackingState.Stopping or TrackingState.Unavailable)
        { _status.Text = "Choose a source and Enable tracking above. The camera remains off until you do."; return; }
        _pendingBegin = true;
        RequestCalibration?.Invoke();
        TryBegin();
        Refresh(false);
    }
    private void TryBegin()
    {
        if (!_pendingBegin) return;
        if (_setupIssue is not null) { _status.Text = _setupIssue; _pendingBegin = false; return; }
        if (_context is not { IsValid: true } || _baseline is null || _snapshot?.SessionId is not { } id)
        { _status.Text = "Keep your selected hand fully visible inside the working region. The lesson starts when a fresh full-hand observation arrives."; return; }
        if (_held) { _status.Text = "Open your hand to release practice before starting the lesson."; return; }
        _epoch++; _pendingBegin = false; _lesson.Begin(_context, id); LessonStarted?.Invoke(); Refresh();
    }
    private void Cancel() { _epoch++; _pendingBegin = false; _lesson.Cancel(); _status.Text = "Lesson canceled. Saved correction unchanged."; Refresh(false); }
    internal void Observe(TrackingSnapshot snapshot, LearningContext? context, DipPoint? baseline, bool held = false, string? setupIssue = null)
    {
        _setupIssue = setupIssue;
        if (held != _held) _epoch++;
        _held = held;
        if (_profile.Enabled) _quality.Observe(snapshot);
        bool ended = _snapshot?.SessionId is not null && (snapshot.SessionId != _snapshot.SessionId ||
            snapshot.State != _snapshot.State && snapshot.State is TrackingState.Stopping or TrackingState.Stopped or TrackingState.Off or TrackingState.Unavailable);
        if (ended)
        {
            Cancel();
            if (_profile.Enabled && _writable && !_busy)
                Change(p => p with { Summary = _quality.Summary }, "Session ended. Quality counts saved; no unverified correction was learned.");
        }
        if (context != _context) _epoch++;
        _snapshot = snapshot; _context = context; _baseline = baseline;
        RecomputePrediction();
        _lesson.Observe(snapshot, context, baseline);
        TryBegin();
        Refresh(_lesson.Phase != LessonPhase.Idle);
    }
    private void RecomputePrediction() => _prediction = _baseline is { } b && _profile.UsePersonal &&
        _profile.Current is { } m && _context is { } context && m.Context == context ? m.Predict(b, context) : _baseline;
    private async Task EvaluateAsync()
    {
        if (_busy || _held || !_profile.Enabled || _lesson.Phase != LessonPhase.Complete || _context != _lesson.Context) return;
        int epoch = _epoch; var context = _context; var revision = _profile.Revision;
        var training = _lesson.Training; var validation = _lesson.Validation; var before = _lesson.Baseline; var current = _profile.Current;
        _busy = true; _status.Text = "Candidate being evaluated on independent targets."; Refresh(false);
        try
        {
            var trial = await Task.Run(() => PersonalTrainer.Train(training, validation, current));
            if (_held || epoch != _epoch || context != _context || revision != _profile.Revision || !_profile.Enabled)
            { _status.Text = "Evaluation canceled after session/settings change. Model unchanged."; return; }
            _busy = false;
            var evaluation = new LearningEvaluation(context!.Source, trial.Accepted, trial.Reason,
                before.Length >= 9 ? PersonalTrainer.Measure(before, null) : null,
                trial.Baseline, trial.Current, trial.CandidateMetrics);
            _lesson.Cancel(); // A held-out set is evaluated once; new candidates need a new lesson.
            if (trial.Candidate is { } candidate)
                Change(p => p with { Previous = p.Current, Current = candidate, Summary = _quality.Summary, Evaluation = evaluation },
                    $"Personal preview accepted: held-out mean {candidate.Baseline.MeanPixels:F1} → {candidate.Validated.MeanPixels:F1} physical px ({candidate.Validated.Count} targets). Not physical detection accuracy.");
            else Change(p => p with { Evaluation = evaluation, Summary = _quality.Summary }, trial.Reason);
            _results.Text = trial.Baseline is { } b && trial.CandidateMetrics is { } c
                ? $"Held-out normalized mean {b.Mean:F4} → {c.Mean:F4}; p95 {b.P95:F4} → {c.P95:F4}; worst region {b.WorstRegion:F4} → {c.WorstRegion:F4}."
                : trial.Reason;
        }
        catch (Exception ex) when (PersonalLearningStore.Expected(ex) || ex is InvalidOperationException)
        { _status.Text = "Candidate evaluation failed; previous model retained. " + ex.Message; }
        finally { _busy = false; Refresh(false); }
    }
    private void ShowResults()
    {
        var m = _profile.Current; var s = _profile.Summary;
        _results.Text = m is null ? "No accepted personal model. No verified correction; mapping unchanged." :
            $"Model {m.Id:N}; revision {_profile.Revision}. Held-out targets {m.Validated.Count}; normalized mean/median/p95/worst/region " +
            $"{m.Validated.Mean:F4}/{m.Validated.Median:F4}/{m.Validated.P95:F4}/{m.Validated.Worst:F4}/{m.Validated.WorstRegion:F4}. " +
            $"Physical px mean/p95/worst {m.Validated.MeanPixels:F1}/{m.Validated.P95Pixels:F1}/{m.Validated.WorstPixels:F1}. " +
            (_context == m.Context ? "Context matches." : "Context differs or source off: baseline used.");
        if (_profile.Evaluation is { } e) _results.Text += $" Last evaluation ({e.Source}): {e.Reason}" +
            (e.BeforeTraining is { } b ? $" Pre-training mean {b.MeanPixels:F1} physical px." : "");
        if (s is not null) _results.Text += $" Last saved UI-observed quality: {s.Observations} events, {s.Partial} partial, {s.Ambiguous} ambiguous, {s.Lost} loss. Not worker frame counts.";
    }
    private void Refresh(bool lessonStatus = true)
    {
        _modelState.Text = _profile.Current is null ? "" : !_profile.UsePersonal ? "Original preview selected." :
            _context != _profile.Current.Context ? "Original preview: saved correction does not match this setup." : "Personal preview active.";
        bool collecting = _lesson.Phase is LessonPhase.Baseline or LessonPhase.Teaching or LessonPhase.Validation;
        bool running = _snapshot?.SessionId is not null &&
            _snapshot.State is not (TrackingState.Off or TrackingState.Stopped or TrackingState.Stopping or TrackingState.Unavailable);
        _primary.Content = collecting ? "Confirm target" : _lesson.Phase == LessonPhase.Complete ? "Evaluate lesson" :
            _profile.Current is null ? "Start lesson" : "Improve again";
        _primary.IsEnabled = !_busy && !_pendingBegin && _profile.Enabled && _writable &&
            (collecting ? _lesson.CanConfirm : _lesson.Phase == LessonPhase.Complete ? !_held : running);
        _cancel.Visibility = _pendingBegin || _busy || _lesson.Phase != LessonPhase.Idle ? Visibility.Visible : Visibility.Collapsed;
        _canvas.Visibility = collecting ? Visibility.Visible : Visibility.Collapsed;
        if (lessonStatus && _lesson.Phase != LessonPhase.Idle)
            _status.Text = _lesson.Phase == LessonPhase.Complete ? "Targets complete. Evaluate this lesson." :
                $"{_lesson.Phase} · target {_lesson.Step} of9. {_lesson.Reason}";
        if (_lesson.Phase == LessonPhase.Idle && !_pendingBegin && !running &&
            (_status.Text.StartsWith("Enable learning") || _status.Text.StartsWith("Personal settings loaded")))
            _status.Text = "Camera off. Prepare your hand/display in Calibrate, then Enable a source.";
        if (_lesson.Phase == LessonPhase.Idle && !_pendingBegin && running &&
            (_status.Text.StartsWith("Camera off.") || _status.Text.StartsWith("Enable learning, choose") ||
             _status.Text.StartsWith("Session ended.") || _status.Text.StartsWith("Personal settings loaded")))
            _status.Text = _profile.Enabled ? "Tracking is on. Start the lesson and keep your selected hand fully visible." :
                "Turn on learning above to begin a target lesson.";
        Draw();
    }
    private void Draw()
    {
        double w = _canvas.ActualWidth, h = _canvas.ActualHeight;
        if (_context is { } c)
        {
            double scale = Math.Min(w / c.Display.Width, h / c.Display.Height);
            w = c.Display.Width * scale; h = c.Display.Height * scale;
        }
        void Place(Ellipse dot, DipPoint? p)
        {
            dot.Visibility = p is null ? Visibility.Collapsed : Visibility.Visible;
            if (p is { } point) { Canvas.SetLeft(dot, point.X * w - dot.Width / 2); Canvas.SetTop(dot, point.Y * h - dot.Height / 2); }
        }
        Place(_target, _lesson.Target); Place(_baseDot, _baseline); Place(_personalDot, _prediction);
    }
    internal DipPoint? FixtureTarget => _lesson.Target;
    internal object TestSnapshot() => new { Enabled = _profile.Enabled, _profile.Revision, _profile.UsePersonal,
        PrimaryLabel = _primary.Content, PrimaryEnabled = _primary.IsEnabled, DetailsExpanded = _details.IsExpanded,
        FeedbackVisible = _feedback.IsVisible, PendingStart = _pendingBegin,
        Model = _profile.Current?.Id, Previous = _profile.Previous?.Id, ContextMatches = _context == _profile.Current?.Context && _profile.Current is not null,
        Phase = _lesson.Phase.ToString(), _lesson.Step, _lesson.StableCount, _lesson.CanConfirm, Target = _lesson.Target,
        Baseline = _baseline, Prediction = _prediction, Status = _status.Text, Results = _results.Text, Busy = _busy };
}
