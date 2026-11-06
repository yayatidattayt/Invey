using System.Diagnostics;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Windows;
using Pocket.Core.References;
using Forms=System.Windows.Forms;

namespace Pocket.App;

public partial class App : Application
{
    private Mutex? _instance;
    private EventWaitHandle? _reopen;
    private RegisteredWaitHandle? _wait;
    private Forms.NotifyIcon? _tray;
    private MainWindow? _controller;
    private AcceptanceHost? _acceptance;
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            int standardIndex=Array.IndexOf(e.Args,"--standard-provider");
            if(standardIndex>=0){AcceptanceHost.ValidateDesktop();StandardProviderFixture.Show(e.Args[standardIndex+1]);return;}
            int providerIndex=Array.IndexOf(e.Args,"--adverse-provider");
            if(providerIndex>=0){AcceptanceHost.ValidateDesktop();new AdverseProviderWindow(e.Args[providerIndex+1]).Show();return;}
            int acceptanceIndex=Array.IndexOf(e.Args,"--acceptance-host");
            if(acceptanceIndex>=0)
            {
                _acceptance=new AcceptanceHost(e.Args[acceptanceIndex+1],e.Args[acceptanceIndex+2]);
                _=ServeAcceptance();return;
            }
            string identity=WindowsIdentity.GetCurrent().User!.Value+"."+Process.GetCurrentProcess().SessionId;
            _instance=new Mutex(true,@"Local\Pocket.Ghost.A1."+identity,out bool first);
            _reopen=new EventWaitHandle(false,EventResetMode.AutoReset,@"Local\Pocket.Ghost.Open."+identity);
            if(!first) { _reopen.Set(); Shutdown(); return; }
            int identityResult = SetCurrentProcessExplicitAppUserModelID("Invey.Desktop");
            if (identityResult < 0) Trace.TraceWarning("Invey app identity could not be set: 0x{0:X8}", identityResult);
            string profileParent = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var profile = await Task.Run(() => InveyProfile.Resolve(profileParent));
            _controller=new MainWindow(profile); MainWindow=_controller;
            _wait=ThreadPool.RegisterWaitForSingleObject(_reopen,(_,_)=>Dispatcher.BeginInvoke(new Action(Open)),null,Timeout.Infinite,false);
            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("Open Invey", null, (_, _) => Open());
            menu.Items.Add("Capture region", null, async (_, _) => await _controller.CaptureAsync());
            menu.Items.Add("Toggle Pocket shelf", null, (_, _) => _controller.ShowPocket(toggle: true));
            menu.Items.Add("Workspace", null, (_, _) => _controller.ShowWorkspace());
            menu.Items.Add("Settings", null, (_, _) => { if (!_controller.CaptureInProgress) { Open(); _controller.ShowSettings(); } });
            menu.Items.Add("Pause Ghost", null, (_, _) => _controller.Pause());
            menu.Items.Add("Exit Invey", null, async (_, _) => await _controller.ExitAsync());
            try
            {
                _tray = new Forms.NotifyIcon { Text = "Invey · Camera off", Icon = System.Drawing.SystemIcons.Application, ContextMenuStrip = menu };
                _tray.Visible = true;
                _tray.DoubleClick += (_, _) => Open();
                _controller.TrayAvailable = true;
            }
            catch (System.ComponentModel.Win32Exception error)
            {
                _tray?.Dispose();
                _tray = null;
                menu.Dispose();
                Trace.TraceWarning("Invey tray could not start: 0x{0:X8}", error.HResult);
            }
            _controller.CameraStatusChanged += status => { if (_tray is { } tray) tray.Text = "Invey · " + status; };
            _controller.StartShortcuts();
            _controller.Show();
            if(e.Args.Contains("--native-smoke")) _=RunSmoke();
        }
        catch(Exception ex) { MessageBox.Show(ex.Message,"Invey startup failed"); Shutdown(1); }
    }
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);

    internal static bool HasShellTray() => FindWindow("Shell_TrayWnd", null) != 0;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint FindWindow(string windowClass, string? title);
    private async Task RunSmoke()
    {
        string outputDirectory=NativeSmoke.CreateRunDirectory();
        try
        {
            var info=new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute=false,CreateNoWindow=true };
            if(string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath),"dotnet",StringComparison.OrdinalIgnoreCase)) info.ArgumentList.Add(System.Reflection.Assembly.GetExecutingAssembly().Location);
            using var second=Process.Start(info) ?? throw new InvalidOperationException("Second instance failed to start.");
            await second.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            if(second.ExitCode!=0) throw new InvalidOperationException("Second instance did not exit cleanly.");
            _controller!.Hide();
            await NativeSmoke.RunAsync(outputDirectory); Shutdown();
        }
        catch(Exception ex)
        {
            await File.WriteAllTextAsync(Path.Combine(outputDirectory,"native-smoke-error.txt"),ex.ToString());
            Shutdown(1);
        }
    }
    private async Task ServeAcceptance()
    {
        try
        {
            try { await _acceptance!.RunAsync(); }
            finally { await _acceptance!.CloseFixturesAsync(); }
            Shutdown();
        }
        catch(Exception ex){Console.Error.WriteLine(ex);Shutdown(1);}
    }
    private void Open() { if(_controller is null || _controller.CaptureInProgress) return; _controller.Show(); _controller.WindowState=WindowState.Normal; _controller.Activate(); }
    protected override void OnExit(ExitEventArgs e)
    {
        _wait?.Unregister(null); _tray?.Dispose(); _controller?.Cleanup();
        _acceptance?.Dispose();
        _reopen?.Dispose(); _instance?.Dispose();
        base.OnExit(e);
    }
}
