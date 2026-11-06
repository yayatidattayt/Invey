using System.Windows;
using System.ComponentModel;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Pocket.Core.References;
using Drawing = System.Drawing;
using Screen = System.Windows.Forms.Screen;

namespace Pocket.App.References;

internal sealed class PocketCards(PocketStore store, bool writable = true)
{
    private const int MaxCards = 4;
    private const long MaxOpenPixels = 24_000_000;
    private readonly Dictionary<Guid, Card> _cards = new();
    private readonly SemaphoreSlim _opening = new(1);
    private Task _writes = Task.CompletedTask;
    private readonly Dictionary<Guid, (Card Card, CardPlacement Placement)> _pending = [];
    private bool _exiting;
    private Guid? _last;
    private Card? Last => _last is { } id && _cards.TryGetValue(id, out var card) ? card : null;
    public Window? LastWindow => Last?.Window;
    public BitmapSource? LastImage => Last?.Image;
    public int? LastZoom => Last?.ZoomPercent;
    public int Count => _cards.Count;
    internal int PlacementFailures { get; private set; }

    public async Task RestoreAsync()
    {
        var library = await Task.Run(store.Load);
        if (library.RecoveryNotice is not null) return;
        foreach (var item in library.Items.Where(item => item.Placement is { Pinned: true, Visible: true }).Take(MaxCards))
        {
            try { await OpenAsync(item, activate: false); }
            catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
                                          NotSupportedException or FileFormatException)
            { System.Diagnostics.Trace.TraceWarning("Pocket card restore failed: 0x{0:X8}", error.HResult); }
        }
    }

    public async Task OpenAsync(PocketReference item, bool activate = true)
    {
        await _opening.WaitAsync();
        try
        {
            if (_exiting) return;
            if (_cards.TryGetValue(item.Id, out var existing))
            {
                _last = item.Id;
                if (activate) existing.Window.Activate();
                return;
            }
            if (_cards.Count >= MaxCards ||
                _cards.Values.Sum(card => (long)card.Image.PixelWidth * card.Image.PixelHeight) +
                    (long)item.Width * item.Height > MaxOpenPixels)
                throw new InvalidDataException("Hide a card before opening another reference.");
            var image = await Task.Run(() => ScreenSnapshot.Decode(store.Open(item)));
            if (_exiting) return;
            var card = new Card(this, item, image, activate);
            _cards.Add(item.Id, card);
            _last = item.Id;
            try { card.Window.Show(); }
            catch { card.Window.Close(); throw; }
        }
        finally { _opening.Release(); }
    }

    internal Task BlockOpeningAsync() => _opening.WaitAsync();
    internal void ResumeOpening() => _opening.Release();

    internal void ExerciseFixture(int cycle)
    {
        foreach (var card in _cards.Values)
        {
            double size = cycle % 2 == 0 ? 0 : 24;
            card.Window.Left = 48 + size;
            card.Window.Top = 48 + size;
            card.Window.Width = 460 + size;
            card.Window.Height = 360 + size;
            card.SetFixtureControls(cycle % 2 == 0);
        }
    }

    public void UpdateTitle(Guid id, string title)
    {
        if (_cards.TryGetValue(id, out var card)) card.Window.Title = $"Invey · {title}";
    }

    public void CloseDeleted(Guid id)
    {
        if (!_cards.TryGetValue(id, out var card)) return;
        card.SuppressSave = true;
        card.Window.Close();
    }

    public Task SaveBeforeDeleteAsync(Guid id)
    {
        if (_cards.TryGetValue(id, out var card)) card.Save(visible: true);
        return _writes;
    }

    public async Task CloseAsync()
    {
        _exiting = true;
        foreach (var card in _cards.Values.ToArray())
        {
            card.Save(visible: true);
            card.SuppressSave = true;
            card.Window.Close();
        }
        await _writes;
    }

    private void Persist(Card card, CardPlacement placement)
    {
        if (!writable) return;
        _pending[card.Id] = (card, placement);
        if (_writes.IsCompleted) _writes = DrainAsync();
    }

    private async Task DrainAsync()
    {
        while (_pending.Count > 0)
        {
            var item = _pending.First();
            _pending.Remove(item.Key);
            var (card, placement) = item.Value;
            try
            {
                await Task.Run(() => store.Place(card.Id, placement));
                card.ClearPlacementError();
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or NotSupportedException)
            {
                PlacementFailures++;
                card.Status.Text = "Could not save card placement.";
                System.Diagnostics.Trace.TraceWarning("Pocket card placement write failed: 0x{0:X8}", error.HResult);
            }
        }
    }

    private sealed class Card
    {
        private readonly PocketCards _owner;
        private readonly DispatcherTimer _placementTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
        private readonly Image _display;
        private readonly ScrollViewer _scroll;
        private readonly CheckBox _pin;
        private CardPlacement? _lastPlacement;
        private int _zoom;
        private bool _loaded;
        public Guid Id { get; }
        public Window Window { get; }
        public BitmapSource Image { get; }
        public int ZoomPercent => _zoom;
        public TextBlock Status { get; } = new() { Margin = new Thickness(8), TextWrapping = TextWrapping.Wrap };
        public bool SuppressSave { get; set; }

        public Card(PocketCards owner, PocketReference item, BitmapSource image, bool activate)
        {
            _owner = owner;
            Id = item.Id;
            Image = image;
            _zoom = item.Placement?.Zoom ?? 0;
            _lastPlacement = item.Placement;
            _pin = new CheckBox { Content = "_Pin above other windows", IsChecked = item.Placement?.Pinned == true,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 10, 0) };
            _display = new Image { Source = image, Stretch = Stretch.Uniform };
            AutomationProperties.SetName(_display, "Saved snapshot");
            _scroll = new ScrollViewer { Content = _display };
            var hide = Button("_Hide reference", Hide);
            var controls = new WrapPanel { Children = { _pin, Button("Zoom _in", () => Zoom(25)),
                Button("Zoom _out", () => Zoom(-25)), Button("_Fit", () => SetZoom(0)),
                Button("_Reset zoom", () => SetZoom(100)), hide } };
            var layout = new DockPanel { Margin = new Thickness(12) };
            DockPanel.SetDock(controls, Dock.Top);
            DockPanel.SetDock(Status, Dock.Bottom);
            layout.Children.Add(controls);
            layout.Children.Add(Status);
            layout.Children.Add(_scroll);
            Window = new Window
            {
                Title = $"Invey · {item.Title ?? "Snapshot"}", Content = layout, Width = 560, Height = 410,
                MinWidth = 260, MinHeight = 190, ResizeMode = ResizeMode.CanResize,
                WindowStartupLocation = WindowStartupLocation.Manual, Topmost = _pin.IsChecked == true,
                ShowActivated = activate
            };
            Window.SourceInitialized += (_, _) => ScreenSnapshot.DisableNativeTransitions(Window);
            Window.Loaded += (_, _) =>
            {
                if (_loaded) return;
                try { ApplyPlacement(item.Placement); }
                catch (Win32Exception error)
                { System.Diagnostics.Trace.TraceWarning("Pocket card positioning failed: 0x{0:X8}", error.HResult); }
                ApplyZoom();
                _loaded = true;
                SystemEvents.DisplaySettingsChanged += DisplaysChanged;
                if (activate) hide.Focus();
            };
            Window.LocationChanged += (_, _) => Schedule();
            Window.SizeChanged += (_, _) => Schedule();
            _pin.Click += (_, _) => ApplyPin();
            _placementTimer.Tick += (_, _) => { _placementTimer.Stop(); Save(visible: true); };
            Window.Closed += (_, _) =>
            {
                _loaded = false;
                SystemEvents.DisplaySettingsChanged -= DisplaysChanged;
                _placementTimer.Stop();
                _display.Source = null;
                _owner._cards.Remove(Id);
                if (_owner._last == Id) _owner._last = null;
            };
            Window.Closing += (_, _) => { if (!SuppressSave) Save(visible: false); };
        }

        private static Button Button(string label, Action action)
        {
            var button = new Button { Content = label, Padding = new Thickness(6, 4, 6, 4) };
            button.Click += (_, _) => action();
            return button;
        }

        private void Hide() => Window.Close();

        private void ApplyPin()
        {
            Window.Topmost = _pin.IsChecked == true;
            Save(visible: true);
        }

        public void SetFixtureControls(bool enabled)
        {
            _pin.IsChecked = enabled;
            ApplyPin();
            SetZoom(enabled ? 125 : 0);
        }

        private void DisplaysChanged(object? sender, EventArgs args) => _ = Window.Dispatcher.InvokeAsync(() =>
        {
            if (!_loaded || Window.WindowState != WindowState.Normal) return;
            try { ApplyPlacement(_lastPlacement); }
            catch (Win32Exception error)
            { System.Diagnostics.Trace.TraceWarning("Pocket display change positioning failed: 0x{0:X8}", error.HResult); }
        });

        private void Zoom(int delta) => SetZoom(Math.Clamp((_zoom == 0 ? 100 : _zoom) + delta, 25, 400));
        private void SetZoom(int zoom) { _zoom = zoom; ApplyZoom(); Save(visible: true); }
        public void ClearPlacementError()
        {
            if (Status.Text == "Could not save card placement.") ShowZoomStatus();
        }

        private void ShowZoomStatus() => Status.Text = _zoom == 0 ? "Fit to window" : $"{_zoom}%";

        private void ApplyZoom()
        {
            _display.Width = _zoom == 0 ? double.NaN : Image.PixelWidth * _zoom / 100d;
            _display.Height = _zoom == 0 ? double.NaN : Image.PixelHeight * _zoom / 100d;
            _scroll.HorizontalScrollBarVisibility = _scroll.VerticalScrollBarVisibility =
                _zoom == 0 ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
            ShowZoomStatus();
        }

        private void ApplyPlacement(CardPlacement? placement)
        {
            var screen = Screen.AllScreens.FirstOrDefault(row => row.DeviceName == placement?.Display) ?? Screen.PrimaryScreen;
            if (screen is null) return;
            var work = screen.WorkingArea;
            if (work.Width <= 0 || work.Height <= 0) return;
            var bounds = CardPlacement.Fit(placement, screen.DeviceName, new(work.X, work.Y, work.Width, work.Height));
            ScreenSnapshot.Place(Window, new Drawing.Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height));
        }

        private void Schedule()
        {
            if (!_loaded || SuppressSave) return;
            _placementTimer.Stop();
            _placementTimer.Start();
        }

        public void Save(bool visible)
        {
            if (!_loaded || SuppressSave) return;
            _placementTimer.Stop();
            var handle = new WindowInteropHelper(Window).Handle;
            if (Window.WindowState == WindowState.Normal && ScreenSnapshot.Bounds(handle) is { } bounds)
                _lastPlacement = new(Screen.FromHandle(handle).DeviceName, bounds.X, bounds.Y, bounds.Width, bounds.Height,
                    _pin.IsChecked == true, visible, _zoom);
            if (_lastPlacement is { } placement)
                _owner.Persist(this, placement with { Visible = visible, Pinned = _pin.IsChecked == true, Zoom = _zoom });
        }
    }
}
