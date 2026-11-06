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
    private readonly PocketCards _cards;
    private readonly bool _ownsCards;
    private readonly bool _writable;
    private readonly Func<Task<bool>> _prepareCapture;
    private readonly Window? _syntheticBackground;
    private readonly ComboBox _displays = new() { DisplayMemberPath = nameof(SnapshotDisplay.Label), MinWidth = 230 };
    private readonly ListBox _references = new() { MinHeight = 165 };
    private readonly Button _capture = new() { Content = "_Capture region" };
    private readonly Button _open = new() { Content = "_Open reference" };
    private readonly Button _reload = new() { Content = "_Refresh library" };
    private readonly Button _rename = new() { Content = "Re_name" };
    private readonly Button _up = new() { Content = "Move _up" };
    private readonly Button _down = new() { Content = "Move do_wn" };
    private readonly Button _delete = new() { Content = "De_lete" };
    private readonly Button _undo = new() { Content = "Undo delet_e" };
    private readonly Button _purge = new() { Content = "Purge e_xpired" };
    private readonly Button _repair = new() { Content = "Repair inde_x" };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private CaptureWindow? _selector;
    private Window? _renameWindow;
    private Task? _captureTask;
    private IReadOnlyList<DeletedReference> _deleted = [];
    private bool _needsRepair;
    private bool _busy, _canCapture, _closed, _exitRequested, _restoreFailed;
    public bool CaptureInProgress => _captureTask is { IsCompleted: false };

    public PocketWorkspace(PocketStore store, Func<Task<bool>> prepareCapture, Window? syntheticBackground = null,
        PocketCards? cards = null, bool writable = true)
    {
        _store = store;
        _cards = cards ?? new PocketCards(store, writable);
        _ownsCards = cards is null;
        _writable = writable;
        _prepareCapture = prepareCapture;
        _syntheticBackground = syntheticBackground;
        Title = "Invey · Pocket";
        Width = 520;
        Height = 530;
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
        _rename.Click += async (_, _) => await RenameAsync();
        _up.Click += async (_, _) => await ChangeSelectedAsync(item => _store.Move(item.Id, -1));
        _down.Click += async (_, _) => await ChangeSelectedAsync(item => _store.Move(item.Id, 1));
        _delete.Click += async (_, _) => await DeleteAsync();
        _undo.Click += async (_, _) => await UndoAsync();
        _purge.Click += async (_, _) =>
        {
            if (MessageBox.Show(this, "Permanently remove expired deleted references?", "Invey · Purge expired",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                await ChangeAsync(() => _ = _store.PurgeExpired());
        };
        _repair.Click += async (_, _) =>
        {
            if (MessageBox.Show(this, "Rebuild the index from verified references and preserve the old index?",
                    "Invey · Repair library", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                await ChangeAsync(() =>
                {
                    foreach (var item in _store.Load().Items) _ = ScreenSnapshot.Decode(_store.Open(item));
                    _store.Repair();
                });
        };
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
                new Label { Content = "Saved re_ferences", Target = _references, Padding = new Thickness(0, 0, 0, 4) },
                _references,
                new WrapPanel { Margin = new Thickness(0, 8, 0, 2), Children = { _open, _reload } },
                new WrapPanel { Children = { _rename, _up, _down, _delete, _undo, _purge, _repair } },
                _status
            }
        } };
        RefreshDisplays();
        UpdateActions();
        Loaded += async (_, _) =>
        {
            await ReloadAsync();
            if (_ownsCards && !_closed)
                try { await _cards.RestoreAsync(); }
                catch (Exception error) when (IsStorageError(error)) { _status.Text = "Could not restore saved cards."; }
        };
        Closed += (_, _) => _closed = true;
    }

    private sealed record Row(PocketReference Reference)
    {
        public override string ToString() =>
            $"{Reference.Title ?? "Snapshot"} · {Reference.CreatedUtc.ToLocalTime():g} · {Reference.Width} × {Reference.Height}";
    }

    public async Task PrepareExitAsync()
    {
        _exitRequested = true;
        UpdateActions();
        if (_selector is { Committing: false }) _selector.Close();
        if (_captureTask is not null) await _captureTask;
        if (_ownsCards) await _cards.CloseAsync();
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
        bool edit = !_busy && !_exitRequested && _canCapture;
        _rename.IsEnabled = _delete.IsEnabled = edit && _references.SelectedIndex >= 0;
        _up.IsEnabled = edit && _references.SelectedIndex > 0;
        _down.IsEnabled = edit && _references.SelectedIndex >= 0 && _references.SelectedIndex + 1 < _references.Items.Count;
        _undo.IsEnabled = edit && _deleted.Any(_store.CanUndo);
        _purge.IsEnabled = edit && _deleted.Any(row => !_store.CanUndo(row));
        _repair.IsEnabled = !_busy && _needsRepair;
    }

    private async Task ReloadAsync(bool saved = false, Guid? selected = null)
    {
        if (_busy || _closed) return;
        _busy = true;
        UpdateActions();
        _status.Text = "Reading local library…";
        try
        {
            var library = await Task.Run(_store.Load);
            if (_closed) return;
            var rows = library.Items.Select(item => new Row(item)).ToArray();
            _references.ItemsSource = rows;
            _references.SelectedItem = rows.FirstOrDefault(row => row.Reference.Id == selected) ?? rows.FirstOrDefault();
            _deleted = library.Deleted ?? [];
            _needsRepair = library.RecoveryNotice is not null &&
                !library.RecoveryNotice.Contains("newer format", StringComparison.Ordinal);
            _canCapture = _writable && library.RecoveryNotice is null;
            _needsRepair &= _writable;
            _status.Text = library.RecoveryNotice ?? (!_writable ? "The original profile is open read-only." : saved ? "Reference saved locally." :
                library.Items.Count == 0 ? "Your library is empty. Capture a region to get started." :
                $"{library.Items.Count} saved reference(s).");
        }
        catch (Exception ex) when (IsStorageError(ex))
        {
            _canCapture = false;
            _needsRepair = false;
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
            await _cards.OpenAsync(row.Reference);
            _status.Text = "Reference open.";
        }
        catch (Exception ex) when (IsStorageError(ex) || ex is FileFormatException or ArgumentException)
        { _status.Text = ex is InvalidDataException && ex.Message.StartsWith("Hide a card", StringComparison.Ordinal)
            ? ex.Message : "This reference is missing or damaged. Other references remain available."; }
        finally { _busy = false; UpdateActions(); }
    }

    private async Task RenameAsync()
    {
        if (_references.SelectedItem is not Row row) return;
        var input = new TextBox { Text = row.Reference.Title ?? "Snapshot", MaxLength = 120, MinWidth = 260,
            Margin = new Thickness(0, 0, 0, 10) };
        AutomationProperties.SetName(input, "Reference title");
        var dialog = new Window { Title = "Invey · Rename reference", Owner = this, SizeToContent = SizeToContent.WidthAndHeight,
            ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var save = new Button { Content = "_Save title", IsDefault = true };
        var cancel = new Button { Content = "_Cancel", IsCancel = true };
        var validation = new TextBlock { TextWrapping = TextWrapping.Wrap };
        save.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(input.Text)) validation.Text = "Enter a title.";
            else dialog.DialogResult = true;
        };
        dialog.Content = new StackPanel { Margin = new Thickness(16), Children =
            { input, validation, new WrapPanel { Children = { save, cancel } } } };
        dialog.Loaded += (_, _) => { input.Focus(); input.SelectAll(); };
        _renameWindow = dialog;
        bool confirmed;
        try { confirmed = dialog.ShowDialog() == true; }
        finally { _renameWindow = null; }
        if (!confirmed) return;
        string title = input.Text.Trim();
        if (await ChangeAsync(() => _store.Rename(row.Reference.Id, title), row.Reference.Id))
            _cards.UpdateTitle(row.Reference.Id, title);
    }

    private async Task DeleteAsync()
    {
        if (_references.SelectedItem is not Row row) return;
        await _cards.SaveBeforeDeleteAsync(row.Reference.Id);
        if (await ChangeAsync(() => _store.Delete(row.Reference.Id)))
        {
            _cards.CloseDeleted(row.Reference.Id);
            _status.Text = "Reference deleted. Undo is available for 10 minutes.";
        }
    }

    private async Task UndoAsync()
    {
        var row = _deleted.LastOrDefault(_store.CanUndo);
        if (row is null) { _status.Text = "No deleted reference is eligible for Undo."; UpdateActions(); return; }
        if (await ChangeAsync(() => _store.Undo(row.Item.Id), row.Item.Id) &&
            row.Item.Placement is { Pinned: true, Visible: true })
        {
            try { await _cards.OpenAsync(row.Item, activate: false); }
            catch (Exception error) when (IsStorageError(error) || error is FileFormatException)
            { _status.Text = "Reference restored. Its card could not be opened."; }
        }
    }

    private Task<bool> ChangeSelectedAsync(Action<PocketReference> operation) =>
        _references.SelectedItem is Row row ? ChangeAsync(() => operation(row.Reference), row.Reference.Id) : Task.FromResult(false);

    private async Task<bool> ChangeAsync(Action operation, Guid? selected = null)
    {
        if (_busy || _exitRequested) return false;
        _busy = true;
        UpdateActions();
        try { await Task.Run(operation); }
        catch (Exception error) when (IsStorageError(error) || error is FileFormatException)
        {
            _status.Text = error is InvalidDataException or NotSupportedException ? error.Message :
                "Could not update the library. Check local storage and try again.";
            return false;
        }
        finally { _busy = false; UpdateActions(); }
        await ReloadAsync(selected: selected);
        return true;
    }

    private static bool IsStorageError(Exception error) =>
        error is IOException or UnauthorizedAccessException or InvalidDataException or NotSupportedException;

    internal object TestState()
    {
        var sample = new byte[4];
        var viewed = _cards.LastImage;
        if (viewed is not null)
        {
            var rgb = new FormatConvertedBitmap(viewed, PixelFormats.Bgr32, null, 0);
            rgb.CopyPixels(new Int32Rect(viewed.PixelWidth / 2, viewed.PixelHeight / 2, 1, 1), sample, 4, 0);
        }
        var lastRow = _references.Items.Count > 0
            ? _references.ItemContainerGenerator.ContainerFromIndex(_references.Items.Count - 1) as ListBoxItem : null;
        var lastRowPoint = IsVisible && lastRow is { IsVisible: true }
            ? lastRow.PointToScreen(new Point(24, lastRow.ActualHeight / 2)) : (Point?)null;
        return new
        {
            LastRowPoint = lastRowPoint,
            Handle = new WindowInteropHelper(this).Handle.ToInt64(),
            CaptureHandle = _selector is { } selector ? new WindowInteropHelper(selector).Handle.ToInt64() : 0,
            Capture = _selector?.TestState(), _status.Text, Count = _references.Items.Count, Busy = _busy,
            ViewerHandle = _cards.LastWindow is { } viewer ? new WindowInteropHelper(viewer).Handle.ToInt64() : 0,
            Cards = _cards.Count, Pinned = _cards.LastWindow?.Topmost == true,
            Zoom = _cards.LastZoom,
            RenameHandle = _renameWindow is { } rename ? new WindowInteropHelper(rename).Handle.ToInt64() : 0,
            LibraryVisible = IsVisible, SelectedTitle = (_references.SelectedItem as Row)?.Reference.Title,
            SelectedIndex = _references.SelectedIndex,
            ViewerBounds = _cards.LastWindow is { } visibleCard
                ? ScreenSnapshot.Bounds(new WindowInteropHelper(visibleCard).Handle) : null,
            Width = viewed?.PixelWidth, Height = viewed?.PixelHeight,
            SampleMatchesFixture = viewed is not null && sample[2] == 192 && sample[1] == 32 && sample[0] == 64,
            SampleBlack = viewed is not null && (sample[2] | sample[1] | sample[0]) == 0,
            FixtureGeometry = _syntheticBackground is { IsVisible: true } fixture
                ? new { fixture.ActualWidth, fixture.ActualHeight, Origin = fixture.PointToScreen(new Point(0, 0)) } : null
        };
    }
}
