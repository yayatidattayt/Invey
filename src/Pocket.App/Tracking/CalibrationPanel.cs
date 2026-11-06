using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Shapes;
using Pocket.Core.Tracking;

namespace Pocket.App.Tracking;

internal sealed class CalibrationPanel : StackPanel
{
    private CalibrationReadiness _readiness = new();
    private readonly Canvas _preview = new() { Height = 126, ClipToBounds = true };
    private readonly Rectangle _frame = new() { Stroke = SystemColors.ControlDarkBrush, StrokeThickness = 1 };
    private readonly Rectangle _region = new() { Stroke = SystemColors.HighlightBrush, StrokeThickness = 1 };
    private readonly Ellipse _tip = new() { Width = 9, Height = 9, Fill = SystemColors.HighlightBrush };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly CheckBox _mirror = new() { Content = "_Mirror preview", Margin = new Thickness(0, 0, 12, 0) };
    private readonly ComboBox _hand = new() { ItemsSource = new[] { "Any hand", "Left hand", "Right hand" }, SelectedIndex = 0 };
    private readonly Slider _margin = new() { Minimum = 5, Maximum = 30, Value = 12, TickFrequency = 1, IsSnapToTickEnabled = true };
    private CalibrationSample _sample;

    public CalibrationPanel(Func<Task> cancel)
    {
        Margin = new Thickness(0, 10, 0, 0);
        Visibility = Visibility.Collapsed;
        var cancelButton = new Button { Content = "Cancel cali_bration", HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 6, 0, 0) };
        cancelButton.Click += async (_, _) => await cancel();
        AutomationProperties.SetName(_preview, "Camera geometry and fingertip marker. No video preview.");
        AutomationProperties.SetName(_hand, "Hand used for calibration");
        AutomationProperties.SetName(_margin, "Working region inset percent");
        AutomationProperties.SetLiveSetting(_status, AutomationLiveSetting.Polite);
        _preview.Children.Add(_frame);
        _preview.Children.Add(_region);
        _preview.Children.Add(_tip);
        _preview.SizeChanged += (_, _) => Draw();
        _mirror.Click += (_, _) => Reset();
        _hand.SelectionChanged += (_, _) => Reset();
        _margin.ValueChanged += (_, _) => Reset();
        Children.Add(new TextBlock { Text = "Calibration preview", FontWeight = FontWeights.SemiBold });
        Children.Add(new WrapPanel { Children = { _mirror, _hand } });
        Children.Add(new Label { Content = "Working region _inset", Target = _margin, Padding = new Thickness(0, 4, 0, 0) });
        Children.Add(_margin);
        Children.Add(_preview);
        Children.Add(_status);
        Children.Add(cancelButton);
    }

    public void Begin()
    {
        Reset();
        Visibility = Visibility.Visible;
        _mirror.Focus();
    }

    public void End()
    {
        Visibility = Visibility.Collapsed;
        Reset();
    }

    private void Reset()
    {
        _readiness = new();
        _sample = default;
        Draw();
    }

    public void Update(TrackingSnapshot snapshot)
    {
        if (Visibility != Visibility.Visible) return;
        HandSide? side = _hand.SelectedIndex switch { 1 => HandSide.Left, 2 => HandSide.Right, _ => null };
        _sample = _readiness.Observe(snapshot, side, _mirror.IsChecked == true, _margin.Value / 100);
        Draw();
        string next = _sample.State switch
        {
            CalibrationState.Ready => "Ready. The fingertip is visible and steady in this working region.",
            CalibrationState.Stabilizing => "Hold your fingertip steadily inside the working region.",
            CalibrationState.OutsideRegion => "Move one hand fully into the working region.",
            CalibrationState.WrongHand => "Show the selected hand.",
            CalibrationState.Stale => "Waiting for fresh tracking.",
            _ => "Waiting for a visible hand."
        };
        if (_status.Text == next) return;
        _status.Text = next;
        UIElementAutomationPeer.CreatePeerForElement(_status)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    private void Draw()
    {
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

    private static void Place(FrameworkElement item, double left, double top, double width, double height)
    {
        Canvas.SetLeft(item, left);
        Canvas.SetTop(item, top);
        item.Width = width;
        item.Height = height;
    }

    public object TestSnapshot() => new { Open = Visibility == Visibility.Visible, State = _sample.State.ToString(),
        Mirrored = _mirror.IsChecked == true, TipX = _sample.Point?.X, Frame = _sample.Frame };
}
