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
    private readonly Button _confirm = new() { Content = "Correct target — confirm" };
    private readonly Button _evaluate = new() { Content = "Evaluate candidate" };
    private readonly ComboBox _feedback = new() { ItemsSource = Enum.GetValues<LearningFeedback>(), SelectedIndex = 0 };
    public PersonalLearningPanel(string? profileRoot, bool writable)
    {
        _store = profileRoot is null ? null : new(System.IO.Path.Combine(profileRoot, "Learning"), writable);
        var loaded = _store?.Load();
        _profile = loaded?.Profile ?? LearningProfile.Empty;
        _writable = loaded?.Writable == true;
        Children.Add(new TextBlock { Text = "Personal Learning · experimental preview", FontWeight = FontWeights.SemiBold, Margin = new(0, 12, 0, 0) });
        Children.Add(new TextBlock { Text = "Learns a small local mapping correction, not missing fingers or OS gestures. Samples stay in memory only.",
            TextWrapping = TextWrapping.Wrap });
        Children.Add(_enabled); Children.Add(_personal); Children.Add(_modelState);
        _loading = true; _enabled.IsChecked = _profile.Enabled; _personal.IsChecked = _profile.UsePersonal; _loading = false;
        _enabled.IsEnabled = _personal.IsEnabled = _writable;
        _enabled.Checked += Preference; _enabled.Unchecked += Preference;
        _personal.Checked += Preference; _personal.Unchecked += Preference;
        var actions = new WrapPanel();
        Add(actions, "Improve my tracking", Begin);
        Add(actions, "Cancel learning", Cancel);
        Add(actions, "Restore previous model", () => Change(p => p with { Current = p.Previous, Previous = p.Current }, "Previous model restored; click again to reapply."));
        Add(actions, "Reset learning", ResetLearning);
        Children.Add(actions);
        Children.Add(new TextBlock { Text = "Selected-display diagram: outline = intended target; gray = baseline; blue = accepted personal preview. Keep the selected hand steady. Confirm only your intended target; do not chase the dots.",
            TextWrapping = TextWrapping.Wrap });
        _canvas.Children.Add(_target); _canvas.Children.Add(_baseDot); _canvas.Children.Add(_personalDot);
        _canvas.SizeChanged += (_, _) => Draw();
        Children.Add(_canvas);
        var lessonActions = new WrapPanel();
        _confirm.Click += (_, _) => { _lesson.Confirm(); Refresh(); };
        lessonActions.Children.Add(_confirm);
        Add(lessonActions, "Retry target", () => { _lesson.Retry(); Refresh(); });
        Add(lessonActions, "Skip target", () => { _lesson.Skip(); Refresh(); });
        _evaluate.Click += async (_, _) => await EvaluateAsync();
        lessonActions.Children.Add(_evaluate);
        Children.Add(lessonActions);
        var feedback = new WrapPanel { Children = { _feedback } };
        Add(feedback, "Give feedback", () =>
        {
            if (!_profile.Enabled || _snapshot?.SessionId is null) { _status.Text = "Start tracking with learning enabled before giving session feedback."; return; }
            _quality.Feedback((LearningFeedback)_feedback.SelectedItem);
            _status.Text = "Feedback counted locally. No gesture classifier trained: target/time-labeled gesture examples are insufficient. Gesture authority unchanged.";
        });
        Children.Add(feedback);
        Add(Children, "View learning results", ShowResults);
        Children.Add(_status); Children.Add(_results);
        _status.Text = loaded?.Notice ?? "Learning unavailable: no writable profile.";
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
        if (_busy || !_profile.Enabled || !_writable || _context is not { IsValid: true } || _snapshot?.SessionId is not { } id)
        { _status.Text = "Enable learning, start a source, open Calibration and select Left or Right hand and a connected display."; return; }
        _epoch++; _lesson.Begin(_context, id); Refresh();
    }
    private void Cancel() { _epoch++; _lesson.Cancel(); Refresh(); }
    internal void Observe(TrackingSnapshot snapshot, LearningContext? context, DipPoint? baseline, bool held = false)
    {
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
        _modelState.Text = _profile.Current is null ? "Default mapping · no accepted personal model." :
            !_profile.UsePersonal ? "Default mapping · personal preview switched off." :
            _context != _profile.Current.Context ? "Default mapping · personal model context differs or source is off." :
            "Personal preview active in the central 20–80% region. Gesture practice uses its original mapping.";
        _confirm.IsEnabled = !_busy && _profile.Enabled && _lesson.CanConfirm;
        _evaluate.IsEnabled = !_busy && !_held && _profile.Enabled && _lesson.Phase == LessonPhase.Complete;
        if (lessonStatus) _status.Text = $"{_lesson.Phase} {_lesson.Step}/9 · {_lesson.Reason}";
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
        Model = _profile.Current?.Id, Previous = _profile.Previous?.Id, ContextMatches = _context == _profile.Current?.Context && _profile.Current is not null,
        Phase = _lesson.Phase.ToString(), _lesson.Step, _lesson.StableCount, _lesson.CanConfirm, Target = _lesson.Target,
        Baseline = _baseline, Prediction = _prediction, Status = _status.Text, Results = _results.Text, Busy = _busy };
}
