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
            string response=await (await Application.Current.Dispatcher.InvokeAsync(()=>DispatchAsync(command)));
            await writer.WriteLineAsync(response);
            if(command=="exit")break;
        }
    }
    private async Task<string> DispatchAsync(string command)
    {
        switch(command)
        {
            case "status":break;
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
            case "tracking-start-calibration-practice": _tracking!.Start("calibration-practice"); break;
            case "tracking-display-disconnect": _tracking!.SimulateDisplayDisconnect(); break;
            case "tracking-stop": await _tracking!.StopAsync(); break;
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
                if (_pocket is { } workspace) await workspace.PrepareExitAsync();
                _simulation.Stop();if(NativeWindow.Exists(_overlay.Handle))_overlay.Hide();break;
            case "close-overlay":_overlay.Close();break;
            default:throw new InvalidOperationException("Unsupported acceptance operation.");
        }
        return JsonSerializer.Serialize(new { Fixture=_fixture.AcceptanceSnapshot(),FixtureHandle=new WindowInteropHelper(_fixture).Handle.ToInt64(),
            OverlayHandle=_overlay.Handle.ToInt64(),_overlay.Dpi,_overlay.Monitor,Styles=NativeWindow.ReadStyles(_overlay.Handle),
            AutomationHandle=_automation is null?0:new WindowInteropHelper(_automation).Handle.ToInt64(),Automation=_automation?.Snapshot(),Recovery=_recovery?.Snapshot(),
            TrackingHandle = _tracking is null ? 0 : new WindowInteropHelper(_tracking).Handle.ToInt64(), Tracking = _tracking?.TestSnapshot(),
            Pocket = _pocket?.TestState(),
            TimerScheduled=_simulation.IsScheduled,OverlayVisible=_overlay.IsVisible,Running=_simulation.IsRunning,Events=_simulation.Observations.Count,
            GhostClaim=_overlay.Scene.Pointer(Pocket.Core.PointerOwner.Ghost).Target,Panel=_overlay.Scene.Target("panel"),
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
            var display = SnapshotDisplay.Available().FirstOrDefault(item => item.Label.EndsWith("Primary", StringComparison.Ordinal))
                ?? throw new InvalidOperationException("No primary display for the capture fixture.");
            var grid = new Grid();
            foreach (var color in new[] { Color.FromRgb(192, 32, 64), Colors.Black, Colors.RoyalBlue })
            {
                var stripe = new Border { Background = new SolidColorBrush(color) };
                Grid.SetColumn(stripe, grid.ColumnDefinitions.Count);
                grid.ColumnDefinitions.Add(new());
                grid.Children.Add(stripe);
            }
            _capturePattern = new Window { WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.Manual, Content = grid, ShowInTaskbar = false };
            ScreenSnapshot.Place(_capturePattern, display.Bounds);
            _capturePattern.Loaded += (_, _) => ScreenSnapshot.Place(_capturePattern, display.Bounds);
            _capturePattern.Show();
            _pocket = new PocketWorkspace(new PocketStore(path), async () =>
            {
                _simulation.Stop();
                return _tracking is null || await _tracking.StopAsync();
            }, _capturePattern);
            var bounds = display.Bounds;
            _pocket.Loaded += (_, _) => ScreenSnapshot.Place(_pocket, new System.Drawing.Rectangle(
                bounds.Left + 24, bounds.Top + 64, Math.Min(630, bounds.Width / 2 - 48), Math.Min(670, bounds.Height - 96)));
            _pocket.Closed += (_, _) => _pocket = null;
        }
        _pocket.Show();
        _pocket.Activate();
    }
    public async Task CloseFixturesAsync()
    {
        try { if (_recovery is not null) await _recovery.ShutdownAsync(); }
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
    public void Dispose(){_simulation.Dispose();if(NativeWindow.Exists(_overlay.Handle))_overlay.Close();_pocket?.Close();_capturePattern?.Close();_fixture.Close();_automation?.Close();}
    [DllImport("kernel32.dll")]private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")]private static extern nint GetThreadDesktop(uint thread);
    [DllImport("user32.dll",EntryPoint="GetUserObjectInformationW",CharSet=CharSet.Unicode,SetLastError=true)]private static extern bool GetUserObjectInformation(nint handle,int index,StringBuilder text,int size,out int needed);
}
