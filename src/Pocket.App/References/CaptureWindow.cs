using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using Pocket.Core;
using Pocket.Core.References;

namespace Pocket.App.References;

internal sealed class CaptureWindow : Window
{
    private readonly SnapshotDisplay _display;
    private readonly PocketStore _store;
    private readonly Guid _id = Guid.NewGuid();
    private BitmapSource? _source, _crop;
    private readonly Image _desktop = new() { Stretch = Stretch.Fill, IsHitTestVisible = false };
    private readonly Image _preview = new() { Stretch = Stretch.Uniform, MaxWidth = 720, MaxHeight = 480 };
    private readonly Canvas _surface = new() { Background = Brushes.Transparent, Focusable = true };
    private readonly Rectangle _outline = new() { Stroke = SystemColors.HighlightBrush, StrokeThickness = 2, IsHitTestVisible = false };
    private readonly TextBlock _status = new() { Margin = new Thickness(8), TextWrapping = TextWrapping.Wrap };
    private readonly Button _review = new() { Content = "_Preview" };
    private readonly Button _back = new() { Content = "_Back", Visibility = Visibility.Collapsed };
    private readonly Button _save = new() { Content = "_Save to Pocket", Visibility = Visibility.Collapsed };
    private readonly CheckBox _acceptBlack = new() { Content = "Save this blac_k snapshot", Visibility = Visibility.Collapsed,
        VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8) };
    private readonly Button _cancel = new() { Content = "_Cancel" };
    private readonly Button _keyboard = new() { Content = "_Keyboard region" };
    private readonly Border _previewBox;
    private Point _start, _end;
    private PixelRegion? _previewRegion;
    private bool _drawing, _busy, _committing, _closed;
    private uint _selectionDpi;
    public bool Saved { get; private set; }
    public bool DisplayChanged { get; private set; }
    public bool Committing => _committing;
    public bool IsClosed => _closed;

    public CaptureWindow(SnapshotDisplay display, BitmapSource source, PocketStore store)
    {
        _display = display;
        _store = store;
        _source = source;
        _desktop.Source = source;
        Title = "Invey · Select a Pocket region";
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.Manual;
        ShowInTaskbar = false;
        Topmost = true;
        Width = 320;
        Height = 240;
        AutomationProperties.SetName(_surface, "Frozen screen selection. Arrow keys move the region. Shift and arrow keys resize it.");
        AutomationProperties.SetLiveSetting(_status, AutomationLiveSetting.Polite);
        _surface.Children.Add(_outline);
        _surface.MouseLeftButtonDown += (_, e) =>
        {
            if (_busy || _crop is not null) return;
            _surface.Focus();
            _start = _end = Clamp(e.GetPosition(_surface));
            _drawing = _surface.CaptureMouse();
            Draw();
        };
        _surface.MouseMove += (_, e) => { if (_drawing) { _end = Clamp(e.GetPosition(_surface)); Draw(); } };
        _surface.MouseLeftButtonUp += (_, e) =>
        {
            if (!_drawing) return;
            _end = Clamp(e.GetPosition(_surface));
            _drawing = false;
            _surface.ReleaseMouseCapture();
            Draw();
        };
        _surface.LostMouseCapture += (_, _) => { _drawing = false; Draw(); };
        _review.Click += async (_, _) => await PreviewAsync();
        _back.Click += (_, _) => ReturnToSelection();
        _save.Click += async (_, _) => await SaveAsync();
        _acceptBlack.Checked += (_, _) => _save.IsEnabled = !_busy;
        _acceptBlack.Unchecked += (_, _) => _save.IsEnabled = false;
        _cancel.Click += (_, _) => Close();
        _keyboard.Click += (_, _) =>
        {
            _start = new(_surface.ActualWidth * .25, _surface.ActualHeight * .25);
            _end = new(_surface.ActualWidth * .75, _surface.ActualHeight * .75);
            _surface.Focus();
            Draw();
        };
        var bar = new DockPanel { Background = SystemColors.WindowBrush, HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(12) };
        var buttons = new WrapPanel();
        buttons.Children.Add(_acceptBlack);
        foreach (var button in new[] { _keyboard, _review, _back, _save, _cancel })
        {
            button.Margin = new Thickness(4);
            button.Padding = new Thickness(10, 5, 10, 5);
            buttons.Children.Add(button);
        }
        DockPanel.SetDock(buttons, Dock.Right);
        bar.Children.Add(buttons);
        bar.Children.Add(_status);
        _previewBox = new Border { Child = _preview, Background = SystemColors.WindowBrush, Padding = new Thickness(12),
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed, IsHitTestVisible = false };
        Content = new Grid { Background = Brushes.Black, Children = { _desktop, _surface, _previewBox, bar } };
        Loaded += (_, _) =>
        {
            Position();
            _selectionDpi = ScreenSnapshot.WindowDpi(this);
            _surface.Focus();
            Draw();
            SystemEvents.DisplaySettingsChanged += OnDisplayChange;
        };
        SourceInitialized += (_, _) => ScreenSnapshot.DisableNativeTransitions(this);
        PreviewKeyDown += OnCaptureKey;
    }

    public void Position() => ScreenSnapshot.Place(this, _display.Bounds);

    private void OnDisplayChange(object? sender, EventArgs args) => _ = Dispatcher.InvokeAsync(AbortDisplayChange);

    private void AbortDisplayChange()
    {
        if (_closed || _committing) return;
        DisplayChanged = true;
        Close();
    }

    private void OnCaptureKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && !_committing) { e.Handled = true; Close(); return; }
        if (_busy || _crop is not null || !_surface.IsKeyboardFocused) return;
        Vector step = e.Key switch
        {
            Key.Left => new(-4, 0), Key.Right => new(4, 0), Key.Up => new(0, -4), Key.Down => new(0, 4), _ => default
        };
        if (step == default) return;
        e.Handled = true;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) _end = Clamp(_end + step);
        else
        {
            step.X = Math.Clamp(step.X, -Math.Min(_start.X, _end.X), _surface.ActualWidth - Math.Max(_start.X, _end.X));
            step.Y = Math.Clamp(step.Y, -Math.Min(_start.Y, _end.Y), _surface.ActualHeight - Math.Max(_start.Y, _end.Y));
            _start += step;
            _end += step;
        }
        Draw();
    }

    private Point Clamp(Point point) => new(Math.Clamp(point.X, 0, _surface.ActualWidth), Math.Clamp(point.Y, 0, _surface.ActualHeight));

    private PixelRegion? Region() => _source is { } source
        ? CaptureGeometry.Select(new DipPoint(_start.X, _start.Y), new DipPoint(_end.X, _end.Y),
            _surface.ActualWidth, _surface.ActualHeight, source.PixelWidth, source.PixelHeight)
        : null;

    private void Draw()
    {
        if (_closed || _crop is not null || _busy) return;
        _outline.Width = Math.Abs(_start.X - _end.X);
        _outline.Height = Math.Abs(_start.Y - _end.Y);
        Canvas.SetLeft(_outline, Math.Min(_start.X, _end.X));
        Canvas.SetTop(_outline, Math.Min(_start.Y, _end.Y));
        var region = Region();
        _review.IsEnabled = !_drawing && region.HasValue;
        _status.Text = region is { } area ? $"{area.Width} × {area.Height} pixels" : "Draw a rectangle or choose Keyboard region.";
    }

    private async Task PreviewAsync()
    {
        if (_busy || _source is not { } source || Region() is not { } region) return;
        if (!DisplayValid()) { AbortDisplayChange(); return; }
        _busy = true;
        _review.IsEnabled = _keyboard.IsEnabled = false;
        _status.Text = "Preparing preview…";
        try
        {
            var (crop, isBlack) = await Task.Run(() => ScreenSnapshot.Crop(source, region));
            if (_closed) return;
            _preview.Source = _crop = crop;
            _previewRegion = region;
            _previewBox.Visibility = _back.Visibility = _save.Visibility = Visibility.Visible;
            _review.Visibility = _keyboard.Visibility = _surface.Visibility = Visibility.Collapsed;
            _acceptBlack.IsChecked = false;
            _acceptBlack.Visibility = isBlack ? Visibility.Visible : Visibility.Collapsed;
            _save.IsEnabled = !isBlack;
            _status.Text = isBlack ? "This preview is black. Some content may be unavailable." :
                $"Preview · {crop.PixelWidth} × {crop.PixelHeight} pixels";
            if (isBlack) _acceptBlack.Focus(); else _save.Focus();
        }
        catch (Exception ex) when (ex is InvalidDataException or ArgumentException or InvalidOperationException)
        {
            if (!_closed)
            {
                _status.Text = ex is InvalidDataException ? ex.Message : "Could not preview this region. Try again.";
                _review.IsEnabled = true;
            }
        }
        finally { _busy = false; _keyboard.IsEnabled = true; }
    }

    private void ReturnToSelection()
    {
        _preview.Source = _crop = null;
        _previewRegion = null;
        _acceptBlack.Visibility = Visibility.Collapsed;
        _acceptBlack.IsChecked = false;
        _previewBox.Visibility = _back.Visibility = _save.Visibility = Visibility.Collapsed;
        _review.Visibility = _keyboard.Visibility = _surface.Visibility = Visibility.Visible;
        _surface.Focus();
        Draw();
    }

    private async Task SaveAsync()
    {
        if (_busy || !_save.IsEnabled || _crop is not { } crop) return;
        if (!DisplayValid()) { AbortDisplayChange(); return; }
        _busy = _committing = true;
        _save.IsEnabled = _cancel.IsEnabled = false;
        _desktop.Source = _source = null;
        _back.Visibility = Visibility.Collapsed;
        _status.Text = "Saving locally…";
        try
        {
            await Task.Run(() =>
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(crop));
                using var output = new MemoryStream();
                encoder.Save(output);
                var bytes = output.ToArray();
                var verified = ScreenSnapshot.Decode(bytes);
                if (verified.PixelWidth != crop.PixelWidth || verified.PixelHeight != crop.PixelHeight)
                    throw new InvalidDataException("PNG verification failed.");
                _store.Save(_id, bytes);
            });
            Saved = true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or
                                   NotSupportedException or InvalidOperationException)
        {
            _status.Text = ex is InvalidDataException or NotSupportedException ? ex.Message :
                "Could not save the reference. Check local storage and try again, or cancel.";
        }
        finally
        {
            _busy = _committing = false;
            _cancel.IsEnabled = true;
            _save.IsEnabled = _acceptBlack.Visibility != Visibility.Visible || _acceptBlack.IsChecked == true;
        }
        if (Saved) Close();
    }

    private bool DisplayValid() => _display.StillConnected() &&
        _selectionDpi > 0 && _selectionDpi == ScreenSnapshot.WindowDpi(this);

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_committing) e.Cancel = true;
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _closed = true;
        SystemEvents.DisplaySettingsChanged -= OnDisplayChange;
        if (_surface.IsMouseCaptured) _surface.ReleaseMouseCapture();
        _preview.Source = _desktop.Source = _crop = _source = null;
        base.OnClosed(e);
    }

    internal object TestState()
    {
        var sample = new byte[4];
        if (_crop is { } crop)
            crop.CopyPixels(new Int32Rect(crop.PixelWidth / 2, crop.PixelHeight / 2, 1, 1), sample, 4, 0);
        return new
        {
            Preview = _crop is not null, Region = _previewRegion ?? Region(), Status = _status.Text,
            SaveEnabled = _save.IsEnabled && _save.IsVisible,
            Width = ActualWidth, Height = ActualHeight, Origin = IsVisible ? PointToScreen(new Point(0, 0)) : default,
            SampleMatchesFixture = _crop is not null && sample[2] == 192 && sample[1] == 32 && sample[0] == 64,
            BlackFixtureBands = _source is { } fixture ? new
            {
                Historical = CountFixturePixels(fixture, .42, .22, .53, .34),
                Bottom = CountFixturePixels(fixture, .48, .87, .54, .94)
            } : null,
            FrozenPattern = _source is { } frozen ? new
            {
                Left = PixelMatches(frozen, .16, .25, 192, 32, 64),
                Center = PixelMatches(frozen, .5, .25, 0, 0, 0),
                Right = PixelMatches(frozen, .84, .25, 65, 105, 225),
                LeftTop = PixelMatches(frozen, .16, .075, 192, 32, 64),
                CenterTop = PixelMatches(frozen, .5, .075, 0, 0, 0),
                LeftBottom = PixelMatches(frozen, .16, .75, 192, 32, 64),
                CenterBottom = PixelMatches(frozen, .5, .75, 0, 0, 0)
            } : null
        };
    }

    private static object CountFixturePixels(BitmapSource source, double left, double top, double right, double bottom)
    {
        int x0 = (int)(left * source.PixelWidth), y0 = (int)(top * source.PixelHeight);
        int width = (int)Math.Ceiling(right * source.PixelWidth) - x0;
        int height = (int)Math.Ceiling(bottom * source.PixelHeight) - y0;
        var line = new byte[width * 4];
        int nonblack = 0, minX = width, minY = height, maxX = -1, maxY = -1;
        for (int y = 0; y < height; y++)
        {
            source.CopyPixels(new Int32Rect(x0, y0 + y, width, 1), line, line.Length, 0);
            for (int x = 0; x < width; x++)
            {
                if ((line[x * 4] | line[x * 4 + 1] | line[x * 4 + 2]) == 0) continue;
                nonblack++;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }
        return new { Nonblack = nonblack, X = x0, Y = y0, Width = width, Height = height,
            Bounds = nonblack == 0 ? null : new { Left = minX + x0, Top = minY + y0, Right = maxX + x0, Bottom = maxY + y0 } };
    }

    private static bool PixelMatches(BitmapSource source, double x, double y, byte red, byte green, byte blue)
    {
        var bytes = new byte[4];
        source.CopyPixels(new Int32Rect((int)(source.PixelWidth * x), (int)(source.PixelHeight * y), 1, 1), bytes, 4, 0);
        return bytes[2] == red && bytes[1] == green && bytes[0] == blue;
    }
}
