using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Pocket.Core.References;

namespace Pocket.App.References;

internal sealed class PocketWorkspace : Window
{
    private readonly PocketStore _store;
    private readonly Func<Task<bool>> _prepareCapture;
    private readonly Window? _syntheticBackground;
    private readonly ComboBox _displays = new() { DisplayMemberPath = nameof(SnapshotDisplay.Label), MinWidth = 230 };
    private readonly ListBox _references = new() { MinHeight = 165 };
    private readonly Button _capture = new() { Content = "_Capture region" };
    private readonly Button _open = new() { Content = "_Open reference" };
    private readonly Button _reload = new() { Content = "_Refresh library" };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private CaptureWindow? _selector;
    private Task? _captureTask;
    private Window? _viewer;
    private BitmapSource? _viewedImage;
    private bool _busy, _canCapture, _closed, _exitRequested, _restoreFailed;
    public bool CaptureInProgress => _captureTask is { IsCompleted: false };

    public PocketWorkspace(PocketStore store, Func<Task<bool>> prepareCapture, Window? syntheticBackground = null)
    {
        _store = store;
        _prepareCapture = prepareCapture;
        _syntheticBackground = syntheticBackground;
        Title = "Invey · Pocket";
        Width = 520;
        Height = 455;
        MinWidth = 390;
        MinHeight = 340;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        AutomationProperties.SetName(_displays, "Capture display");
        AutomationProperties.SetName(_references, "Saved references");
        AutomationProperties.SetLiveSetting(_status, AutomationLiveSetting.Polite);
        var displays = new Button { Content = "Refresh _displays" };
        displays.Click += (_, _) => RefreshDisplays();
        _capture.Click += async (_, _) => await (_captureTask = CaptureAsync());
        _reload.Click += async (_, _) => await ReloadAsync();
        _open.Click += async (_, _) => await OpenAsync();
        _references.SelectionChanged += (_, _) => UpdateActions();
        _references.MouseDoubleClick += async (_, _) => await OpenAsync();
        _displays.SelectionChanged += (_, _) => UpdateActions();
        Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = new StackPanel
        {
            Margin = new Thickness(24), Children =
            {
                new TextBlock { Text = "Pocket", FontSize = 24, FontWeight = FontWeights.SemiBold },
                new TextBlock { Text = "Save a region of your screen as a local reference. Selection takes focus.",
                    TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 14) },
                new Label { Content = "Capture displa_y", Target = _displays, Padding = new Thickness(0, 0, 0, 4) },
                _displays,
                new WrapPanel { Margin = new Thickness(0, 8, 0, 8), Children = { _capture, displays } },
                _references,
                new WrapPanel { Margin = new Thickness(0, 8, 0, 2), Children = { _open, _reload } },
                _status
            }
        } };
        RefreshDisplays();
        UpdateActions();
        Loaded += async (_, _) => await ReloadAsync();
        Closed += (_, _) => { _closed = true; _viewer?.Close(); };
    }

    private sealed record Row(PocketReference Reference)
    {
        public override string ToString() =>
            $"Snapshot · {Reference.CreatedUtc.ToLocalTime():g} · {Reference.Width} × {Reference.Height}";
    }

    public async Task PrepareExitAsync()
    {
        _exitRequested = true;
        UpdateActions();
        if (_selector is { Committing: false }) _selector.Close();
        if (_captureTask is not null) await _captureTask;
    }

    public void ResumeAfterAbortedExit()
    {
        _exitRequested = false;
        UpdateActions();
    }

    private void RefreshDisplays()
    {
        var previous = (_displays.SelectedItem as SnapshotDisplay)?.Device;
        var items = SnapshotDisplay.Available();
        _displays.ItemsSource = items;
        _displays.SelectedItem = items.FirstOrDefault(item => item.Device == previous) ??
            items.FirstOrDefault(item => item.Label.EndsWith("Primary", StringComparison.Ordinal)) ?? items.FirstOrDefault();
    }

    private void UpdateActions()
    {
        _capture.IsEnabled = !_busy && !_exitRequested && !_restoreFailed && _canCapture &&
            _displays.SelectedItem is SnapshotDisplay;
        _open.IsEnabled = !_busy && _references.SelectedItem is Row;
        _reload.IsEnabled = !_busy;
    }

    private async Task ReloadAsync(bool saved = false)
    {
        if (_busy || _closed) return;
        _busy = true;
        UpdateActions();
        _status.Text = "Reading local library…";
        try
        {
            var library = await Task.Run(_store.Load);
            if (_closed) return;
            _references.ItemsSource = library.Items.OrderByDescending(item => item.CreatedUtc).Select(item => new Row(item)).ToArray();
            _references.SelectedIndex = library.Items.Count == 0 ? -1 : 0;
            _canCapture = library.RecoveryNotice is null;
            _status.Text = library.RecoveryNotice ?? (saved ? "Reference saved locally." :
                library.Items.Count == 0 ? "Your library is empty. Capture a region to get started." :
                $"{library.Items.Count} saved reference(s).");
        }
        catch (Exception ex) when (IsStorageError(ex))
        {
            _canCapture = false;
            if (!_closed) _status.Text = "Could not read the local library. Check storage access and refresh.";
        }
        finally { _busy = false; UpdateActions(); }
    }

    private async Task CaptureAsync()
    {
        if (!_capture.IsEnabled || _displays.SelectedItem is not SnapshotDisplay display) return;
        _busy = true;
        UpdateActions();
        Window[] visible = [];
        var cloaked = new HashSet<Window>();
        bool saved = false;
        try
        {
            if (!await _prepareCapture()) { _status.Text = "Tracking cleanup is pending. Try again after Stop completes."; return; }
            if (_exitRequested) return;
            visible = Application.Current.Windows.OfType<Window>()
                .Where(window => window.IsVisible && window != _syntheticBackground).ToArray();
            foreach (var window in visible)
            {
                if (!ScreenSnapshot.SetCloaked(window, true))
                    throw new IOException("Windows could not exclude an Invey window from capture.");
                cloaked.Add(window);
                window.Hide();
            }
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            if (_exitRequested) return;
            ScreenSnapshot.Synchronize();
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            if (_exitRequested) return;
            if (visible.Any(ScreenSnapshot.NativeVisible))
                throw new IOException("An Invey window was still visible.");
            BitmapSource? image = await Task.Run(() => ScreenSnapshot.Capture(display));
            if (_exitRequested) return;
            _selector = new CaptureWindow(display, image, _store);
            image = null;
            _selector.Position();
            _selector.ShowDialog();
            saved = _selector.Saved;
            _status.Text = _selector.DisplayChanged ? "The display changed. Choose a display and retry." :
                saved ? "Reference saved locally." : "Capture closed.";
        }
        catch (Exception ex) when (IsStorageError(ex) || ex is Win32Exception or ArgumentException or InvalidOperationException)
        {
            _status.Text = ex is InvalidDataException ? ex.Message : "Capture unavailable. Check the selected display and retry.";
        }
        finally
        {
            if (_selector is { IsClosed: false }) _selector.Close();
            _selector = null;
            foreach (var window in visible)
            {
                if (!Application.Current.Windows.OfType<Window>().Contains(window)) continue;
                bool activate = window.ShowActivated;
                try
                {
                    window.ShowActivated = false;
                    bool needsUncloak = cloaked.Contains(window) && !ScreenSnapshot.SetCloaked(window, false);
                    window.Show();
                    if (needsUncloak && !ScreenSnapshot.SetCloaked(window, false)) _restoreFailed = true;
                }
                catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
                {
                    _restoreFailed = true;
                    System.Diagnostics.Trace.TraceError("Pocket window restoration failed: 0x{0:X8}", ex.HResult);
                }
                finally
                {
                    window.ShowActivated = activate;
                }
            }
            if (_restoreFailed)
            {
                const string message = "Windows could not restore an Invey window. Capture is paused. Reopen Invey from the tray.";
                _status.Text = message;
                System.Diagnostics.Trace.TraceError(message);
                if (_syntheticBackground is null) MessageBox.Show(message, "Invey · Window recovery");
            }
            if (IsVisible) { Activate(); _capture.Focus(); }
            _busy = false;
            UpdateActions();
        }
        if (saved && !_restoreFailed && !_exitRequested) await ReloadAsync(saved: true);
    }

    private async Task OpenAsync()
    {
        if (_busy || _references.SelectedItem is not Row row) return;
        _busy = true;
        UpdateActions();
        _status.Text = "Opening reference…";
        try
        {
            var image = await Task.Run(() => ScreenSnapshot.Decode(_store.Open(row.Reference)));
            if (_closed) return;
            _viewer?.Close();
            var hide = new Button { Content = "_Hide reference", HorizontalAlignment = HorizontalAlignment.Right };
            var content = new DockPanel { Margin = new Thickness(12) };
            DockPanel.SetDock(hide, Dock.Bottom);
            content.Children.Add(hide);
            var display = new Image { Source = image, Stretch = Stretch.Uniform };
            AutomationProperties.SetName(display, "Saved snapshot");
            content.Children.Add(display);
            var viewer = new Window { Owner = this, Title = "Invey · Snapshot", Content = content,
                Width = Math.Min(640, SystemParameters.WorkArea.Width), Height = Math.Min(500, SystemParameters.WorkArea.Height),
                WindowStartupLocation = WindowStartupLocation.CenterOwner };
            _viewer = viewer;
            _viewedImage = image;
            hide.Click += (_, _) => viewer.Close();
            viewer.Closed += (_, _) => { _viewer = null; _viewedImage = null; display.Source = null; };
            viewer.Show();
            hide.Focus();
            _status.Text = "Reference open.";
        }
        catch (Exception ex) when (IsStorageError(ex) || ex is FileFormatException or ArgumentException)
        { _status.Text = "This reference is missing or damaged. Other references remain available."; }
        finally { _busy = false; UpdateActions(); }
    }

    private static bool IsStorageError(Exception error) =>
        error is IOException or UnauthorizedAccessException or InvalidDataException or NotSupportedException;

    internal object TestState()
    {
        var sample = new byte[4];
        if (_viewedImage is { } viewed)
        {
            var rgb = new FormatConvertedBitmap(viewed, PixelFormats.Bgr32, null, 0);
            rgb.CopyPixels(new Int32Rect(viewed.PixelWidth / 2, viewed.PixelHeight / 2, 1, 1), sample, 4, 0);
        }
        return new
        {
            Handle = new WindowInteropHelper(this).Handle.ToInt64(),
            CaptureHandle = _selector is { } selector ? new WindowInteropHelper(selector).Handle.ToInt64() : 0,
            Capture = _selector?.TestState(), _status.Text, Count = _references.Items.Count, Busy = _busy,
            ViewerHandle = _viewer is { } viewer ? new WindowInteropHelper(viewer).Handle.ToInt64() : 0,
            Width = _viewedImage?.PixelWidth, Height = _viewedImage?.PixelHeight,
            SampleMatchesFixture = _viewedImage is not null && sample[2] == 192 && sample[1] == 32 && sample[0] == 64,
            FixtureGeometry = _syntheticBackground is { IsVisible: true } fixture
                ? new { fixture.ActualWidth, fixture.ActualHeight, Origin = fixture.PointToScreen(new Point(0, 0)) } : null
        };
    }
}
