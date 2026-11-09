using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using Pocket.Overlay;
using Pocket.App.Tracking;
using Pocket.App.References;
using Pocket.Core.References;
using Pocket.Core.Tracking;
using System.Windows.Controls;
using System.Windows.Media;

namespace Pocket.App;

/// <summary>Explicit development-test interface. No OS input injection; available only on a named acceptance desktop.</summary>
internal sealed class AcceptanceHost : IDisposable
{
    internal static void PublishReady(string directory, nint window)
    {
        string ready = Path.Combine(directory, "ready");
        string staged = ready + ".pending";
        File.WriteAllText(staged, window.ToInt64().ToString());
        File.Move(staged, ready, overwrite: true);
    }

    private readonly FixtureWindow _fixture=new();
    private readonly GhostOverlay _overlay=new();
    private readonly SimulationController _simulation;
    private readonly string _pipeName,_token;
    private AutomationFixtureWindow? _automation;
    private MutationRecoveryWindow? _recovery;
    private RecordedTrackingWindow? _tracking;
    private PocketWorkspace? _pocket;
    private Window? _capturePattern;
    private MainWindow? _product;
    private int _fixtureHotKeyHits;
    private bool _fixtureHotKey;
    private string? _shortcutResult;
    private int _profileCycle;
    private bool _profileStarted;
    private bool[]? _diagnosticReservations;
    private object? _budgetResult;
    private object? _previewResult;
    public AcceptanceHost(string pipeName,string token)
    {
        ValidateDesktop();
        _pipeName=pipeName;_token=token;
        _fixture.Show();_fixture.Activate();
        new WindowInteropHelper(_overlay).EnsureHandle();
        _simulation=new(_overlay);
    }
    internal static void ValidateDesktop()
    {
        string expected=Environment.GetEnvironmentVariable("GHOST_ACCEPTANCE_DESKTOP")??"";
        if(!expected.StartsWith("GhostAcceptance_",StringComparison.Ordinal) || DesktopName()!=expected)
            throw new InvalidOperationException("Acceptance host requires its designated private test desktop.");
    }
    private static string DesktopName()
    {
        var text=new StringBuilder(256);
        if(!GetUserObjectInformation(GetThreadDesktop(GetCurrentThreadId()),2,text,512,out _))throw new InvalidOperationException("Cannot identify desktop.");
        return text.ToString();
    }
    public async Task RunAsync()
    {
        using var pipe=new NamedPipeServerStream(_pipeName,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
        await pipe.WaitForConnectionAsync().WaitAsync(TimeSpan.FromSeconds(20));
        using var reader=new StreamReader(pipe,Encoding.UTF8,false,4096,true);
        using var writer=new StreamWriter(pipe,new UTF8Encoding(false),4096,true){AutoFlush=true};
        while(pipe.IsConnected)
        {
            string? line=await ReadBoundedAsync(reader);
            if(line is null)break;
            using var json=JsonDocument.Parse(line);
            if(json.RootElement.GetProperty("token").GetString()!=_token)throw new InvalidOperationException("Invalid test token.");
            string command=json.RootElement.GetProperty("command").GetString()??"";
            string response;
            try { response=await (await Application.Current.Dispatcher.InvokeAsync(()=>DispatchAsync(command))); }
            catch (Exception error)
            {
                string? fixture = Environment.GetEnvironmentVariable("GHOST_TEST_POCKET_STORE");
                if (fixture is not null) await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(fixture)!, "dispatch-error.log"), error.ToString());
                throw;
            }
            await writer.WriteLineAsync(response);
            if(command=="exit")break;
        }
    }
    private async Task<string> DispatchAsync(string command)
    {
        if(command.StartsWith("ghost-aim|",StringComparison.Ordinal)) { _product!.DesktopGhost.Aim(command[10..]);command="status"; }
        if(command.StartsWith("ghost-control|",StringComparison.Ordinal)){_product!.DesktopGhost.ClickControlFixture(command[14..]);command="status";}
        if(command.StartsWith("ghost-move|",StringComparison.Ordinal))
        {
            var values=command.Split('|');var host=_product!.DesktopGhost;
            if(host.Engine.Position is { } p)host.MoveControlled(new(p.X+int.Parse(values[1]),p.Y+int.Parse(values[2])));
            command="status";
        }
        switch(command)
        {
            case "ghost-start": _product!.DesktopGhost.StartControlled();break;
            case "ghost-controls": _product!.DesktopGhost.ShowControls();break;
            case "ghost-controls-place": _product!.DesktopGhost.PositionControlsForAcceptance(900,50);break;
            case "ghost-pattern-hide": _capturePattern?.Hide();break;
            case "ghost-arm": _product!.DesktopGhost.Engine.Arm();break;
            case "ghost-press": _product!.DesktopGhost.Intent(Pocket.Core.GhostIntent.Press);break;
            case "ghost-release": _product!.DesktopGhost.Intent(Pocket.Core.GhostIntent.Release);break;
            case "ghost-activate": _product!.DesktopGhost.Command(Pocket.Core.GhostCommand.Activate);break;
            case "ghost-pin": _product!.DesktopGhost.Command(Pocket.Core.GhostCommand.Pin);break;
            case "ghost-unpin": _product!.DesktopGhost.Command(Pocket.Core.GhostCommand.Unpin);break;
            case "ghost-hide": _product!.DesktopGhost.Command(Pocket.Core.GhostCommand.Hide);break;
            case "ghost-stop": _product!.DesktopGhost.Stop();break;
            case "ghost-loss": _product!.DesktopGhost.Engine.Lose("Injected tracking loss — released");break;
            case "ghost-recorded-preview": _product!.EnableDesktopPreviewFixture();break;
            case "status":break;
            case "product-diagnostic-budget":
                _diagnosticReservations = ReserveFixtureBudget();
                break;
            case "preview-protocol-controls":
                _previewResult = await CameraPreviewFixture.ExerciseAsync();
                break;
            case "budget-ui-pending":
            case "budget-ui-exhausted":
                if (_tracking is not null) await _tracking.CloseAsync();
                _tracking = CameraBudgetFixture.CreateBudgetPreview(command[10..]);
                _tracking.Show();
                break;
            case "budget-targets-series":
            case "budget-amended-series":
            case "budget-series":
            case "budget-corrupt":
            case "budget-missing":
            case "budget-directory":
            case "budget-deadline":
            case "budget-interrupted":
            case "budget-canceled":
                _budgetResult = await CameraBudgetFixture.ExerciseAsync(command[7..]);
                break;
            case "product-conflict":
                var source = (HwndSource)PresentationSource.FromVisual(_fixture)!;
                if (!RegisterHotKey(source.Handle, 0x6111, 0x4003, 0x50))
                    throw new InvalidOperationException("Could not reserve the test shortcut.");
                _fixtureHotKey = true;
                source.AddHook(ObserveFixtureShortcut);
                break;
            case "demo-start":
            case "dual-demo-start":
                string demoRoot = Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("GHOST_TEST_POCKET_STORE"))!, command=="dual-demo-start"?"dual-demo-profile":"demo-profile");
                var demoProfile = DemoProfile.Prepare(demoRoot);
                _capturePattern ??= CreatePocketPattern();
                _product = new MainWindow(demoProfile, _capturePattern);
                _product.StartShortcuts();
                _product.Show();
                _product.Activate();
                break;
            case "demo-close":
                if (_product is not null) await _product.CloseAcceptanceAsync();
                _product = null;
                break;
            case "product-start":
                string fixtureRoot = Environment.GetEnvironmentVariable("GHOST_TEST_POCKET_STORE")!;
                string profile = Path.Combine(Path.GetDirectoryName(fixtureRoot)!, "pocket-migration", "Invey");
                if (!Directory.Exists(profile)) throw new InvalidOperationException("No migrated fixture profile.");
                _capturePattern ??= CreatePocketPattern();
                _product = new MainWindow(new InveyProfile(profile), _capturePattern);
                _product.StartShortcuts();
                _product.Show();
                _product.Activate();
                break;
            case "product-remap":
                _shortcutResult = _product!.ApplyTestShortcuts(["Ctrl+Alt+G", "Ctrl+Alt+J", "Ctrl+Alt+Space", "Ctrl+Alt+W"]);
                break;
            case "product-locked-remap":
                _shortcutResult = _product!.ApplyTestShortcuts(["Ctrl+Alt+G", "Ctrl+Alt+K", "Ctrl+Alt+Space", "Ctrl+Alt+W"]);
                break;
            case "product-profile-start":
                await SeedProfileImagesAsync();
                await _product!.OpenFixtureCardsAsync();
                _profileStarted = true;
                break;
            case "product-profile-cycle":
                _product!.ExerciseFixtureCards(_profileCycle++);
                if (_profileCycle % 2 == 0) _product.ShowPocket(toggle: true);
                break;
            case "product-invalid":
                _shortcutResult = _product!.ApplyTestShortcuts(["Win+G", "Ctrl+Alt+J", "", ""]);
                break;
            case "product-tray-fallback":
                _product!.ShowPocket(toggle: true);
                break;
            case "product-ghost-press":
                if (!_product!.PressTestGhost()) throw new InvalidOperationException("Ghost could not claim Reference B.");
                break;
            case "product-ghost-move": _product!.MoveTestGhost(); break;
            case "product-ghost-back": _product!.MoveTestGhost(-8); break;
            case "product-close-board": _product!.CloseTestWorkspace(); break;
            case "product-ghost-cancel": _product!.CancelTestGhost(); break;
            case "pocket-open":
                OpenPocketFixture();
                break;
            case "pocket-open-migrated":
                OpenPocketFixture(migrate: true);
                break;
            case "pocket-hide-library":
                _pocket!.Hide();
                break;
            case "pocket-close":
                if (_pocket is { } pocket) { await pocket.PrepareExitAsync(); pocket.Close(); }
                _capturePattern?.Close();
                _capturePattern = null;
                break;
            case "tracking-open":
            case "tracking-open-native-video":
            case "tracking-open-camera-denied":
            case "tracking-open-camera-busy":
            case "tracking-open-camera-missing":
                if (_tracking is null)
                {
                    _tracking = new RecordedTrackingWindow(
                        command == "tracking-open-native-video" ? Path.GetFullPath("artifacts/execution-08/controlled.mp4") : null,
                        command.StartsWith("tracking-open-camera-", StringComparison.Ordinal) ? command[14..] : null,
                        testSettingsPath: Environment.GetEnvironmentVariable("GHOST_TEST_CALIBRATION_SETTINGS"),
                        acceptanceHost: true);
                    _tracking.Closed += (_, _) => _tracking = null;
                }
                _tracking.Show();
                _tracking.Activate();
                break;
            case "tracking-start-crash": _tracking!.Start("crash"); break;
            case "tracking-start-malformed": _tracking!.Start("malformed"); break;
            case "tracking-start-hang": _tracking!.Start("startup-hang"); break;
            case "tracking-start-bad-model": _tracking!.Start("bad-model"); break;
            case "tracking-start-gap": _tracking!.Start("result-gap"); break;
            case "tracking-start-ignore-stop": _tracking!.Start("ignore-stop"); break;
            case "tracking-finger-preview-open": _tracking!.OpenFingerFixture(); break;
            case "tracking-enable-video-fixture": _tracking!.EnablePreviewFixture(); break;
            case "tracking-start-finger-preview": _tracking!.Start("finger-preview"); break;
            case "tracking-start-finger-visual": _tracking!.Start("finger-visual"); break;
            case "tracking-start-calibration-practice": _tracking!.Start("calibration-practice"); break;
            case "tracking-display-disconnect": _tracking!.SimulateDisplayDisconnect(); break;
            case "tracking-stop": if (_tracking is not null) await _tracking.StopAsync(); break;
            case "tracking-close": if (_tracking is not null) await _tracking.CloseAsync(); break;
            case "automation-open":_automation??=new AutomationFixtureWindow();_automation.Show();break;
            case "recovery-open":
                _recovery ??= new MutationRecoveryWindow(() => new WindowInteropHelper(_automation!).Handle.ToInt64());
                _recovery.Show();
                break;
            case "recovery-act": await _recovery!.ApplyAsync(); break;
            case "recovery-check": await _recovery!.CheckAsync(); break;
            case "recovery-dismiss": await _recovery!.DismissAsync(); break;
            case "automation-normal":case "automation-disabled":case "automation-duplicate":case "automation-missing":case "automation-replace":case "automation-no-pattern":case "automation-no-scroll":case "automation-scroll-end":case "automation-read-only":case "automation-ignore-range":case "automation-reject-range":
                _automation!.Configure(command["automation-".Length..]);break;
            case "automation-recreate":_automation!.Close();_automation=new AutomationFixtureWindow();_automation.Show();break;
            case "reset":_fixture.ResetTest();break;
            case "show":_overlay.Show();break;
            case "hide":_simulation.Stop();_overlay.Hide();break;
            case "start":_simulation.Start(20,0);break;
            case "stop":_simulation.Stop();break;
            case "focus":_fixture.Activate();break;
            case "exit":
                if (_product is { } product) { await product.CloseAcceptanceAsync(); _product = null; }
                if (_pocket is { } workspace) await workspace.PrepareExitAsync();
                _simulation.Stop();if(NativeWindow.Exists(_overlay.Handle))_overlay.Hide();break;
            case "close-overlay":_overlay.Close();break;
            default:
                if (command.StartsWith("learning-fixture|", StringComparison.Ordinal))
                {
                    ValidateDesktop();
                    Application.Current.Windows.OfType<RecordedTrackingWindow>().Single().LearningFixture(command.Split('|')[1]);
                    break;
                }
                if (command.StartsWith("demo-ui|", StringComparison.Ordinal) ||
                    command.StartsWith("demo-toggle|", StringComparison.Ordinal) ||
                    command.StartsWith("demo-select|", StringComparison.Ordinal) ||
                    command.StartsWith("demo-hand|", StringComparison.Ordinal) ||
                    command.StartsWith("demo-expand|", StringComparison.Ordinal))
                { DemoControlFixture.Apply(_product ?? throw new InvalidOperationException("No demo product."), command); break; }
                throw new InvalidOperationException("Unsupported acceptance operation.");
        }
        return JsonSerializer.Serialize(new { Fixture=_fixture.AcceptanceSnapshot(),FixtureHandle=new WindowInteropHelper(_fixture).Handle.ToInt64(),
            OverlayHandle=_overlay.Handle.ToInt64(),_overlay.Dpi,_overlay.Monitor,Styles=NativeWindow.ReadStyles(_overlay.Handle),
            AutomationHandle=_automation is null?0:new WindowInteropHelper(_automation).Handle.ToInt64(),Automation=_automation?.Snapshot(),Recovery=_recovery?.Snapshot(),
            TrackingHandle = _tracking is null ? 0 : new WindowInteropHelper(_tracking).Handle.ToInt64(), Tracking = _tracking?.TestSnapshot(),
            Pocket = _pocket?.TestState(), Product = _product?.TestState(),
            FixtureHotKeyHits = _fixtureHotKeyHits, ShortcutResult = _shortcutResult,
            Resources = _profileStarted ? _product?.FixtureResources() : null,
            DiagnosticReservations = _diagnosticReservations, BudgetResult = _budgetResult, PreviewResult = _previewResult,
            TimerScheduled=_simulation.IsScheduled,OverlayVisible=_overlay.IsVisible,Running=_simulation.IsRunning,Events=_simulation.Observations.Count,
            GhostClaim=_overlay.Scene.Pointer(Pocket.Core.PointerOwner.Ghost).Target,Panel=_overlay.Scene.Target("panel"),
            Windows=Application.Current.Windows.Cast<Window>().Select(w=>new { Type=w.GetType().FullName, w.Title, w.IsVisible,
                w.WindowState,w.Left,w.Top,w.Width,w.Height,Handle=new WindowInteropHelper(w).Handle.ToInt64() }).ToArray(),
            Native=NativeWindow.Observe() });
    }

    private void OpenPocketFixture(bool migrate = false)
    {
        ValidateDesktop();
        if (_pocket is null)
        {
            string path = Environment.GetEnvironmentVariable("GHOST_TEST_POCKET_STORE") ??
                throw new InvalidOperationException("No isolated Pocket store.");
            if (migrate)
            {
                var migrated = InveyProfile.Resolve(Path.Combine(Path.GetDirectoryName(path)!, "pocket-migration"));
                if (!migrated.Available || !migrated.Writable || Path.GetFileName(migrated.Root) != "Invey")
                    throw new InvalidOperationException("The isolated profile did not migrate.");
                path = migrated.Root;
            }
            _capturePattern = CreatePocketPattern();
            _pocket = new PocketWorkspace(new PocketStore(path), async () =>
            {
                _simulation.Stop();
                return _tracking is null || await _tracking.StopAsync();
            }, _capturePattern);
            var bounds = PrimaryFixtureDisplay().Bounds;
            _pocket.Loaded += (_, _) => ScreenSnapshot.Place(_pocket, new System.Drawing.Rectangle(
                bounds.Left + 24, bounds.Top + 64, Math.Min(630, bounds.Width / 2 - 48), Math.Min(670, bounds.Height - 96)));
            _pocket.Closed += (_, _) => _pocket = null;
        }
        _pocket.Show();
        _pocket.Activate();
    }
    private static bool[] ReserveFixtureBudget()
    {
        string store = Environment.GetEnvironmentVariable("GHOST_TEST_POCKET_STORE")!;
        string directory = Path.Combine(Path.GetDirectoryName(store)!, "budget-fixture-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "budget.json");
        CameraBudget.Initialize(path);
        var result = new List<bool>();
        try
        {
            for (int i = 0; i < 7; i++)
            {
                using var budget = new CameraBudget(path);
                Guid id = Guid.NewGuid();
                try { budget.Reserve(id); budget.Complete(id, 0); result.Add(true); }
                catch (InvalidDataException) { result.Add(false); }
            }
            return result.ToArray();
        }
        finally
        {
            File.Delete(path);
            File.Delete(path + ".lock");
            Directory.Delete(directory);
        }
    }

    private static async Task SeedProfileImagesAsync()
    {
        string fixture = Environment.GetEnvironmentVariable("GHOST_TEST_POCKET_STORE")!;
        var store = new PocketStore(Path.Combine(Path.GetDirectoryName(fixture)!, "pocket-migration", "Invey"));
        await Task.Run(() =>
        {
            const int width = 1600, height = 900;
            var pixels = new byte[width * height * 4];
            Array.Fill(pixels, (byte)72);
            var bitmap = System.Windows.Media.Imaging.BitmapSource.Create(width, height, 96, 96,
                PixelFormats.Bgr32, null, pixels, width * 4);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            using var output = new MemoryStream();
            encoder.Save(output);
            byte[] bytes = output.ToArray();
            for (int i = 0; i < 3; i++) store.Save(Guid.NewGuid(), bytes);
        });
    }

    private static SnapshotDisplay PrimaryFixtureDisplay() =>
        SnapshotDisplay.Available().FirstOrDefault(item => item.Label.EndsWith("Primary", StringComparison.Ordinal))
        ?? throw new InvalidOperationException("No primary display for the capture fixture.");

    private static Window CreatePocketPattern()
    {
        var bounds = PrimaryFixtureDisplay().Bounds;
        var grid = new Grid();
        foreach (var color in new[] { Color.FromRgb(192, 32, 64), Colors.Black, Colors.RoyalBlue })
        {
            var stripe = new Border { Background = new SolidColorBrush(color) };
            Grid.SetColumn(stripe, grid.ColumnDefinitions.Count);
            grid.ColumnDefinitions.Add(new());
            grid.Children.Add(stripe);
        }
        var pattern = new Window { WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.Manual, Content = grid, ShowInTaskbar = false, ShowActivated = false };
        ScreenSnapshot.Place(pattern, bounds);
        pattern.Loaded += (_, _) => ScreenSnapshot.Place(pattern, bounds);
        pattern.Show();
        return pattern;
    }

    private nint ObserveFixtureShortcut(nint window, int message, nint id, nint data, ref bool handled)
    {
        if (message == 0x0312 && id == 0x6111) { _fixtureHotKeyHits++; handled = true; }
        return 0;
    }

    public async Task CloseFixturesAsync()
    {
        try
        {
            CameraBudgetFixture.Cleanup();
            if (_product is { } product) await product.CloseAcceptanceAsync();
            if (_pocket is { } shelf) await shelf.PrepareExitAsync();
            if (_recovery is not null) await _recovery.ShutdownAsync();
        }
        finally
        {
            if (_tracking is not null && !await _tracking.CloseAsync())
                throw new InvalidOperationException("Tracking worker exit could not be confirmed.");
        }
    }
    private static async Task<string?> ReadBoundedAsync(StreamReader reader)
    {
        var text=new StringBuilder();var c=new char[1];
        while(true)
        {
            int count=await reader.ReadAsync(c.AsMemory()).AsTask().WaitAsync(TimeSpan.FromSeconds(90));
            if(count==0)return text.Length==0?null:throw new IOException("Partial command.");
            if(c[0]=='\n')return text.ToString().TrimEnd('\r');
            text.Append(c[0]);if(text.Length>16384)throw new IOException("Oversized command.");
        }
    }
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(nint hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(nint hwnd, int id);
    public void Dispose(){if (_fixtureHotKey) _ = UnregisterHotKey(new WindowInteropHelper(_fixture).Handle, 0x6111); _simulation.Dispose();if(NativeWindow.Exists(_overlay.Handle))_overlay.Close();_pocket?.Close();_capturePattern?.Close();_fixture.Close();_automation?.Close();}
    [DllImport("kernel32.dll")]private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")]private static extern nint GetThreadDesktop(uint thread);
    [DllImport("user32.dll",EntryPoint="GetUserObjectInformationW",CharSet=CharSet.Unicode,SetLastError=true)]private static extern bool GetUserObjectInformation(nint handle,int index,StringBuilder text,int size,out int needed);
}
