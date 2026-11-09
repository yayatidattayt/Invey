using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Pocket.Core.Tracking;

namespace Pocket.App.Tracking;

internal sealed class CameraPreviewView : Grid
{
    private readonly Image _image = new() { Stretch = Stretch.Uniform };
    private readonly Canvas _overlay = new() { IsHitTestVisible = false, ClipToBounds = true };
    private readonly Ellipse[] _points;
    private readonly Line[] _bones;
    private static readonly (int A, int B)[] Edges =
    [
        (0,1),(1,2),(2,3),(3,4), (0,5),(5,6),(6,7),(7,8),
        (5,9),(9,10),(10,11),(11,12), (9,13),(13,14),(14,15),(15,16),
        (13,17),(0,17),(17,18),(18,19),(19,20)
    ];
    private CameraPreviewFrame? _frame;
    private HandObservation? _observation;
    private bool _mirror;
    private int _drawnMode=-1;
    private long _updates,_unchanged;
    public int OverlayMode { get; set; } = 2;
    private long _lastRendered = -1, _renderStarted, _uniqueRendered;
    public double UniquePresentedFps => _renderStarted == 0 ? 0 :
        _uniqueRendered * 1000.0 / Math.Max(1, Environment.TickCount64 - _renderStarted);
    private void Presented(object? sender, EventArgs args)
    {
        if (!IsVisible || _frame is not { } frame || frame.Sequence == _lastRendered) return;
        if (_renderStarted == 0) _renderStarted = Environment.TickCount64;
        _lastRendered = frame.Sequence;
        _uniqueRendered++;
    }
    public long? Sequence => _frame?.Sequence;
    internal long UpdateCount=>_updates;

    public CameraPreviewView()
    {
        Height = 230;
        Loaded += (_, _) => CompositionTarget.Rendering += Presented;
        Unloaded += (_, _) => CompositionTarget.Rendering -= Presented;
        Background = SystemColors.ControlBrush;
        Visibility = Visibility.Collapsed;
        Children.Add(_image);
        Children.Add(_overlay);
        _image.RenderTransformOrigin=new(.5,.5);
        _bones = Edges.Select(_ => new Line { Stroke = SystemColors.HighlightBrush, StrokeThickness = 1.5 }).ToArray();
        _points = Enumerable.Range(0, 21).Select(i => new Ellipse
        {
            Width = i is 4 or 8 or 12 or 16 or 20 ? 8 : 4,
            Height = i is 4 or 8 or 12 or 16 or 20 ? 8 : 4,
            Fill = SystemColors.HighlightBrush
        }).ToArray();
        foreach (var bone in _bones) _overlay.Children.Add(bone);
        foreach (var point in _points) _overlay.Children.Add(point);
        SizeChanged += (_, _) => Draw();
        System.Windows.Automation.AutomationProperties.SetName(this, "Local live camera view with model landmarks");
    }

    public void Update(CameraPreviewFrame? frame, HandObservation? hand, bool mirror)
    {
        var matched=hand is not null&&hand.Sequence==frame?.Sequence?hand:null;
        if(ReferenceEquals(_frame,frame)&&ReferenceEquals(_observation,matched)&&_mirror==mirror&&_drawnMode==OverlayMode)
        {_unchanged++;return;}
        _updates++;
        if (frame is null) { _renderStarted = 0; _uniqueRendered = 0; _lastRendered = -1; }
        _frame = frame;
        _observation = matched;
        if(_mirror!=mirror)_image.RenderTransform=mirror?new ScaleTransform(-1,1):Transform.Identity;
        _mirror = mirror;
        _image.Source = frame?.Image;
        Visibility = frame is null ? Visibility.Collapsed : Visibility.Visible;
        Draw();
    }

    private Point? Position(int index)
    {
        var observed = index == 8 && _observation?.Visual is { } visual ?
            new ImageLandmark(visual.X, visual.Y, 0) : _observation?.Point(index);
        if (_frame is not { } frame || observed is not { } point) return null;
        var box = CalibrationReadiness.FitAspect(frame.Image.PixelWidth, frame.Image.PixelHeight, ActualWidth, ActualHeight);
        return new(box.X + (_mirror ? 1 - point.X : point.X) * box.Width, box.Y + point.Y * box.Height);
    }

    private void Draw()
    {
        _drawnMode=OverlayMode;
        for (int i = 0; i < _points.Length; i++)
        {
            var visual = _points[i];
            visual.Fill = i == 8 && _observation?.Visual is not null ? Brushes.DarkOrange : SystemColors.HighlightBrush;
            var point = Position(i);
            visual.Visibility = point is null || OverlayMode == 0 ||
                (OverlayMode == 1 && i is not (4 or 8 or 12 or 16 or 20)) ? Visibility.Collapsed : Visibility.Visible;
            if (point is not { } p) continue;
            Canvas.SetLeft(visual, p.X - visual.Width / 2);
            Canvas.SetTop(visual, p.Y - visual.Height / 2);
        }
        for (int i = 0; i < Edges.Length; i++)
        {
            var (a, b) = Edges[i];
            var first = Position(a);
            var last = Position(b);
            var line = _bones[i];
            line.Visibility = OverlayMode == 2 && first.HasValue && last.HasValue ? Visibility.Visible : Visibility.Collapsed;
            if (first is not { } p || last is not { } q) continue;
            line.X1 = p.X;
            line.Y1 = p.Y;
            line.X2 = q.X;
            line.Y2 = q.Y;
        }
    }

    internal object TestState() => new { FrameVisible = _image.IsVisible && _image.Source is not null,
        Updates=_updates,UnchangedPolls=_unchanged,
        Sequence, OverlaySequence = _observation?.Sequence, VisiblePoints = _points.Count(point => point.IsVisible) };
}
