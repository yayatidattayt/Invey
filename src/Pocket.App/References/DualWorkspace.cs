using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Pocket.Core;
using Pocket.Core.References;

namespace Pocket.App.References;

internal sealed class DualWorkspace : Window
{
    private const string First = "A", Second = "B";
    private readonly PocketStore _store;
    private readonly InteractionState _scene = new(new(0, 0, 960, 620), []);
    private readonly Canvas _board = new() { Width = 960, Height = 620, ClipToBounds = true };
    private readonly ComboBox _library = new() { MinWidth = 260, DisplayMemberPath = nameof(Choice.Label) };
    private readonly Button _larger;
    private readonly WrapPanel _selectionTools;
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Dictionary<string, Tile> _tiles = [];
    private readonly HashSet<Guid> _deleted = [];
    private string? _selection;
    private FrameworkElement? _mouseHandle;
    private bool _closed, _busy;
    private readonly System.Windows.Threading.DispatcherTimer _demo = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private int _demoTick;

    private sealed record Choice(PocketReference Item)
    {
        public string Label => $"{Item.Title ?? "Snapshot"} · {Item.CreatedUtc.ToLocalTime():g} · {Item.Width} × {Item.Height}";
    }

    private sealed record Tile(Guid Id, Border Frame, Image Image, ScrollViewer Scroll, Button Grip);

    public DualWorkspace(PocketStore store)
    {
        _store = store;
        Title = "Invey · Workspace";
        SourceInitialized += (_, _) => ScreenSnapshot.DisableNativeTransitions(this);
        Width = 1050;
        Height = 790;
        MinWidth = 430;
        MinHeight = 380;
        AutomationProperties.SetName(_library, "Saved references");
        AutomationProperties.SetLiveSetting(_status, AutomationLiveSetting.Polite);
        var tools = new WrapPanel { Margin = new(0, 0, 0, 8) };
        tools.Children.Add(new Label { Content = "Saved _reference", Target = _library });
        tools.Children.Add(_library);
        tools.Children.Add(Command("Load _A", async () => await LoadAsync(First)));
        tools.Children.Add(Command("Load _B", async () => await LoadAsync(Second)));
        tools.Children.Add(Command("Re_fresh library", async () => await RefreshAsync()));
        tools.Children.Add(Command("Simulated _Ghost demo", () =>
        {
            if (_demo.IsEnabled) { _demo.Stop(); CancelGhost(); return; }
            if (!_tiles.ContainsKey(First) || !PressGhost()) { _status.Text = "Load A and B first; release B before starting the simulation."; return; }
            _demoTick = 0;
            _demo.Start();
        }));
        _demo.Tick += (_, _) =>
        {
            if (_closed || !_tiles.ContainsKey(Second) || _scene.Pointer(PointerOwner.Ghost).Target is null || ++_demoTick > 20)
            { _demo.Stop(); CancelGhost(); return; }
            MoveGhost(_demoTick <= 10 ? 8 : -8, 0);
        };
        var selectionTools = _selectionTools = new WrapPanel { Margin = new(0, 0, 0, 8), IsEnabled = false };
        selectionTools.Children.Add(Command("Move _left", () => Adjust(-16, 0)));
        selectionTools.Children.Add(Command("Move _right", () => Adjust(16, 0)));
        selectionTools.Children.Add(Command("Move _up", () => Adjust(0, -16)));
        selectionTools.Children.Add(Command("Move _down", () => Adjust(0, 16)));
        selectionTools.Children.Add(Command("_Smaller", () => Resize(-24)));
        _larger = Command("_Larger", () => Resize(24));
        selectionTools.Children.Add(_larger);
        selectionTools.Children.Add(Command("100% _zoom", () => Zoom(true)));
        selectionTools.Children.Add(Command("F_it image", () => Zoom(false)));
        selectionTools.Children.Add(Command("Remo_ve from board", () => { if (_selection is { } slot) Remove(slot); }));
        _board.Background = SystemColors.ControlBrush;
        var surface = new ScrollViewer { Content = _board, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var header = new StackPanel { Children = { tools, selectionTools,
            new TextBlock { Text = "Simulated Ghost moves B only; this action never starts a camera. Live hand actuation is not connected.", TextWrapping = TextWrapping.Wrap }, _status } };
        var layout = new DockPanel { Margin = new(16) };
        DockPanel.SetDock(header, Dock.Top);
        layout.Children.Add(header);
        layout.Children.Add(surface);
        Content = layout;
        Loaded += async (_, _) => await RefreshAsync();
        PreviewKeyDown += (_, args) =>
        {
            if (args.Key != Key.Escape || _scene.Pointer(PointerOwner.Mouse).Target is null) return;
            EndMouse();
            args.Handled = true;
        };
        Closed += (_, _) =>
        {
            _closed = true;
            _demo.Stop();
            EndMouse();
            CancelGhost();
            foreach (string slot in _tiles.Keys.ToArray()) Remove(slot);
        };
    }

    private static Button Command(string label, Action run)
    {
        var button = new Button { Content = label, Padding = new(6, 4, 6, 4), Margin = new(2) };
        button.Click += (_, _) => run();
        return button;
    }

    private async Task RefreshAsync()
    {
        if (_busy || _closed) return;
        _busy = true;
        try
        {
            var library = await Task.Run(_store.Load);
            if (_closed) return;
            Guid? current = (_library.SelectedItem as Choice)?.Item.Id;
            var choices = library.Items.Where(item => !_deleted.Contains(item.Id)).Select(item => new Choice(item)).ToArray();
            _library.ItemsSource = choices;
            _library.SelectedItem = choices.FirstOrDefault(item => item.Item.Id == current) ?? choices.FirstOrDefault();
            _status.Text = library.RecoveryNotice ?? (_selection is null ?
                "Choose two snapshots and load them into A and B." : $"Reference {_selection} selected.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or NotSupportedException)
        { if (!_closed) _status.Text = "Could not read the Pocket library."; }
        finally { _busy = false; }
    }

    private async Task LoadAsync(string slot)
    {
        if (_busy || _closed || _library.SelectedItem is not Choice choice) return;
        var item = choice.Item;
        string other = slot == First ? Second : First;
        if (_tiles.TryGetValue(other, out var existing) && existing.Id == item.Id)
        {
            _status.Text = "Choose a different snapshot for each position.";
            return;
        }
        if ((long)item.Width * item.Height + (existing is null ? 0 : (long)((BitmapSource)existing.Image.Source).PixelWidth *
            ((BitmapSource)existing.Image.Source).PixelHeight) > 24_000_000)
        {
            _status.Text = "These snapshots exceed the Workspace image limit.";
            return;
        }
        _busy = true;
        try
        {
            var image = await Task.Run(() => ScreenSnapshot.Decode(_store.Open(item)));
            if (!_closed && !_deleted.Contains(item.Id)) Mount(slot, item, image);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or
                                      NotSupportedException or FileFormatException)
        { if (!_closed) _status.Text = "Could not open that snapshot. Refresh the library or choose another."; }
        finally { _busy = false; }
    }

    private void Mount(string slot, PocketReference item, BitmapSource source)
    {
        if (_tiles.TryGetValue(slot, out var old) && old.Id == item.Id) { Select(slot); return; }
        Remove(slot);
        _scene.SetTarget(slot, slot == First ? new(32, 32, 370, 260) : new(480, 130, 370, 260));
        var image = new Image { Source = source, Stretch = Stretch.Uniform };
        AutomationProperties.SetName(image, $"Reference {slot} snapshot");
        var scroll = new ScrollViewer { Content = image, Focusable = true, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled };
        AutomationProperties.SetName(scroll, $"Reference {slot} viewer");
        var label = new TextBlock { Text = $"{slot} · {item.Title ?? "Snapshot"}", TextTrimming = TextTrimming.CharacterEllipsis };
        var grip = new Button { Content = label, HorizontalContentAlignment = HorizontalAlignment.Left, Cursor = Cursors.SizeAll };
        AutomationProperties.SetName(grip, $"Select or move reference {slot}");
        grip.Click += (_, _) => Select(slot);
        grip.PreviewMouseLeftButtonDown += (_, args) =>
        {
            Select(slot);
            if (BeginMouse(slot, grip, args.GetPosition(_board))) args.Handled = true;
        };
        var dock = new DockPanel();
        DockPanel.SetDock(grip, Dock.Top);
        dock.Children.Add(grip);
        dock.Children.Add(scroll);
        var frame = new Border { Child = dock, Background = SystemColors.WindowBrush, BorderThickness = new(2) };
        frame.GotKeyboardFocus += (_, _) => Select(slot);
        _tiles.Add(slot, new(item.Id, frame, image, scroll, grip));
        _board.Children.Add(frame);
        Render(slot);
        Select(slot);
    }

    private bool BeginMouse(string slot, FrameworkElement handle, Point point)
    {
        _scene.Move(PointerOwner.Mouse, new(point.X, point.Y));
        if (!_scene.Press(PointerOwner.Mouse, slot)) return false;
        if (!handle.CaptureMouse()) { _scene.Cancel(PointerOwner.Mouse); return false; }
        _mouseHandle = handle;
        AddHandler(Mouse.PreviewMouseMoveEvent, new MouseEventHandler(MouseMoved), handledEventsToo: true);
        handle.PreviewMouseLeftButtonUp += MouseReleased;
        handle.LostMouseCapture += MouseCaptureLost;
        return true;
    }

    private void MouseMoved(object sender, MouseEventArgs args)
    {
        if (args.LeftButton != MouseButtonState.Pressed) { EndMouse(); return; }
        if (_scene.Pointer(PointerOwner.Mouse).Target is not { } slot) return;
        Point p = args.GetPosition(_board);
        _scene.Move(PointerOwner.Mouse, new(p.X, p.Y));
        Render(slot);
    }

    private void MouseReleased(object sender, MouseButtonEventArgs args)
    {
        EndMouse();
        args.Handled = true;
    }

    private void MouseCaptureLost(object sender, MouseEventArgs args) => EndMouse();

    private void EndMouse()
    {
        _scene.Cancel(PointerOwner.Mouse);
        if (_mouseHandle is not { } handle) return;
        _mouseHandle = null;
        RemoveHandler(Mouse.PreviewMouseMoveEvent, new MouseEventHandler(MouseMoved));
        handle.PreviewMouseLeftButtonUp -= MouseReleased;
        handle.LostMouseCapture -= MouseCaptureLost;
        if (handle.IsMouseCaptured) handle.ReleaseMouseCapture();
    }

    private void Select(string slot)
    {
        _selection = slot;
        _selectionTools.IsEnabled = true;
        foreach (var pair in _tiles)
        {
            bool selected = pair.Key == slot;
            pair.Value.Frame.BorderBrush = selected ? SystemColors.HighlightBrush : SystemColors.ControlDarkBrush;
            Panel.SetZIndex(pair.Value.Frame, selected ? 1 : 0);
        }
        _status.Text = $"Reference {slot} selected.";
    }

    private void Adjust(int dx, int dy)
    {
        if (_selection is not { } slot || _scene.Pointer(PointerOwner.Mouse).Target is not null) return;
        var box = _scene.Target(slot);
        if (_scene.Resize(PointerOwner.Mouse, slot, box with { X = box.X + dx, Y = box.Y + dy })) Render(slot);
    }

    private void Resize(int change)
    {
        if (_selection is not { } slot || _scene.Pointer(PointerOwner.Mouse).Target is not null) return;
        var box = _scene.Target(slot);
        if (_scene.Resize(PointerOwner.Mouse, slot, box with { Width = Math.Max(240, box.Width + change),
                Height = Math.Max(160, box.Height + change) })) Render(slot);
    }

    private void Zoom(bool actualSize)
    {
        if (_selection is not { } slot || !_tiles.TryGetValue(slot, out var tile) || tile.Image.Source is not BitmapSource image) return;
        tile.Image.Width = actualSize ? image.PixelWidth : double.NaN;
        tile.Image.Height = actualSize ? image.PixelHeight : double.NaN;
        tile.Scroll.HorizontalScrollBarVisibility = tile.Scroll.VerticalScrollBarVisibility =
            actualSize ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
    }

    private void Render(string slot)
    {
        var box = _scene.Target(slot);
        var frame = _tiles[slot].Frame;
        Canvas.SetLeft(frame, box.X);
        Canvas.SetTop(frame, box.Y);
        frame.Width = box.Width;
        frame.Height = box.Height;
    }

    private void Remove(string slot)
    {
        if (!_tiles.Remove(slot, out var tile)) return;
        if (_scene.Pointer(PointerOwner.Mouse).Target == slot) EndMouse();
        _scene.RemoveTarget(slot);
        tile.Image.Source = null;
        _board.Children.Remove(tile.Frame);
        if (_selection == slot)
        {
            _selection = null;
            _selectionTools.IsEnabled = false;
            _status.Text = $"Reference {slot} removed from Workspace.";
        }
    }

    public void ReferenceDeleted(Guid id)
    {
        _deleted.Add(id);
        foreach (var pair in _tiles.Where(pair => pair.Value.Id == id).ToArray()) Remove(pair.Key);
        if (_library.ItemsSource is Choice[] choices)
            _library.ItemsSource = choices.Where(choice => choice.Item.Id != id).ToArray();
    }

    public void ReferenceRestored(Guid id) => _deleted.Remove(id);
    public bool InteractionActive => _scene.Pointer(PointerOwner.Mouse).Target is not null;

    // Used by the explicitly labeled simulation and private-desktop fixture; live tracking is not routed here.
    internal bool PressGhost()
    {
        if (!_tiles.ContainsKey(Second) || _scene.Pointer(PointerOwner.Ghost).Target is not null) return false;
        var box = _scene.Target(Second);
        _scene.Move(PointerOwner.Ghost, new(box.X + 12, box.Y + 12));
        return _scene.Press(PointerOwner.Ghost, Second);
    }

    internal void MoveGhost(int dx, int dy)
    {
        if (_scene.Pointer(PointerOwner.Ghost) is not { Target: { } slot, Position: var p }) return;
        _scene.Move(PointerOwner.Ghost, new(p.X + dx, p.Y + dy));
        Render(slot);
    }

    internal void CancelGhost() => _scene.Cancel(PointerOwner.Ghost);

    internal object TestState() => new
    {
        Handle = new WindowInteropHelper(this).Handle.ToInt64(),
        References = _library.Items.Count,
        Larger = _larger.IsVisible ? _larger.PointToScreen(new Point(_larger.ActualWidth / 2, _larger.ActualHeight / 2)) : default,
        Mouse = _scene.Pointer(PointerOwner.Mouse).Target,
        Ghost = _scene.Pointer(PointerOwner.Ghost).Target,
        Cards = _tiles.Select(pair =>
        {
            var frame = pair.Value.Frame;
            var grip = pair.Value.Grip;
            return new { Slot = pair.Key, X = Canvas.GetLeft(frame), Y = Canvas.GetTop(frame),
                Width = frame.ActualWidth, Height = frame.ActualHeight,
                Grip = grip.IsVisible ? grip.PointToScreen(new Point(24, grip.ActualHeight / 2)) : default,
                ImageLoaded = pair.Value.Image.Source is BitmapSource };
        }).ToArray()
    };
}
