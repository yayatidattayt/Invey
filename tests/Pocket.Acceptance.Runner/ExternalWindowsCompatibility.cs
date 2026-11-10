using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Pocket.Automation.Contracts;
namespace Pocket.Acceptance.Runner;
internal static partial class Program
{
    private sealed class CompatibilityGap(string message):Exception(message);
    private static async Task RunWindowsCompatibility(Client client,string output)
    {
        var clock=Stopwatch.StartNew();Process? owned=null;
        try
        {
            Desktop.Guard();
            // No shell activation: a linked, less-privileged token and explicit private desktop.
            using var current=Process.GetCurrentProcess();
            if(!OpenProcessTokenE27(current.Handle,0xA,out var token))throw new CompatibilityGap("Current token unavailable");
            nint linked=0;
            try
            {
                if(!GetTokenInformationE27(token,19,out linked,IntPtr.Size,out _))throw new CompatibilityGap("No linked standard-user token; no privilege workaround attempted");
                var start=new StartupE27{Size=Marshal.SizeOf<StartupE27>(),Desktop="WinSta0\\"+Environment.GetEnvironmentVariable("GHOST_ACCEPTANCE_DESKTOP")};
                string exe=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"winver.exe");
                if(!File.Exists(exe))throw new CompatibilityGap("Windows About application unavailable");
                if(!CreateProcessWithTokenW(linked,0,exe,null,0,0,Environment.CurrentDirectory,ref start,out var created))throw new CompatibilityGap("Standard-token private-desktop launch refused: "+Marshal.GetLastPInvokeError());
                CloseHandleE27(created.Thread);CloseHandleE27(created.Process);owned=Process.GetProcessById(created.Pid);
            }
            finally {if(linked!=0)CloseHandleE27(linked);CloseHandleE27(token);}
            long window=0;var wait=Stopwatch.StartNew();
            while(wait.ElapsedMilliseconds<5000 && !owned.HasExited)
            {
                var choice=ExternalWindows.List().FirstOrDefault(c=>c.ProcessId==owned.Id);
                if(choice is not null){window=choice.Window;break;}await Task.Delay(50);
            }
            if(window==0)throw new CompatibilityGap("No eligible medium-integrity About window; scoped UIA was not attempted");
            nint button=GetDlgItemE27(new(window),1);
            if(button==0||!GetWindowRectE27(button,out var bounds))throw new CompatibilityGap("Version-specific OK control not available");
            await client.Command("ghost-start");await client.Command($"external|select|{window}|Invoke");
            await client.Command($"external|move|{(bounds.Left+bounds.Right)/2}|{(bounds.Top+bounds.Bottom)/2}");await client.Command("external|arm");
            var hover=await client.Command("external|probe");Require(DesktopGhost(hover).GetProperty("External").GetProperty("Hover").GetString()=="Invoke","Windows About OK did not expose Invoke");
            string? version=owned.MainModule?.FileVersionInfo.FileVersion;
            var cursor=Input.Cursor();var before=Input.Foreground;var result=await client.Command("external|activate");
            var closed=Stopwatch.StartNew();while(Input.Exists(new(window))&&closed.ElapsedMilliseconds<3000)await Task.Delay(30);
            var end=Input.Cursor();Require(!Input.Exists(new(window)),"Windows About did not actually close");Require(cursor.X==end.X&&cursor.Y==end.Y,"Genuine action moved native cursor");
            var evidence=new{Application="Windows winver.exe",Version=version,Pattern="Invoke",Target="OK / dialog item1",WindowClosed=true,CursorUnchanged=true,ForegroundBefore=before.ToInt64(),ForegroundAfter=Input.Foreground.ToInt64(),Feedback=DesktopGhost(result).GetProperty("External").GetProperty("Status").GetString()};
            File.WriteAllText(Path.Combine(output,"windows-compatibility.json"),JsonSerializer.Serialize(evidence));
            Cases.Add(new("E27_WINDOWS_ABOUT_INVOKE","REAL_INSTALLED_APP + SCOPED_UIA","Close owned About window without mouse movement",evidence,"PASS",clock.Elapsed.TotalMilliseconds));
            await client.Command("external|dismiss");
        }
        catch(CompatibilityGap error)
        {
            Cases.Add(new("E27_WINDOWS_ABOUT_INVOKE","ENVIRONMENT_OR_COMPATIBILITY_GAP","Medium-integrity owned About window only",new{Reason=error.Message},"BLOCKED",clock.Elapsed.TotalMilliseconds));
        }
        catch(Exception error)
        {
            Cases.Add(new("E27_WINDOWS_ABOUT_INVOKE","REAL_APP_TEST_FAILURE","Observe actual supported action",new{Error=error.ToString()},"FAIL",clock.Elapsed.TotalMilliseconds));
        }
        finally
        {
            await client.Command("external|stop");
            if(owned is not null){if(!owned.HasExited){owned.Kill();Require(owned.WaitForExit(3000),"Owned About app cleanup unconfirmed");}owned.Dispose();}
        }
    }
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] private struct StartupE27 {public int Size;public string? Reserved;public string? Desktop;public string? Title;public int X,Y,Width,Height,XCount,YCount,Fill,Flags;public short Show,Reserved2;public nint ReservedPtr,Input,Output,Error;}
    [StructLayout(LayoutKind.Sequential)] private struct CreatedE27 {public nint Process,Thread;public int Pid,Tid;}
    [StructLayout(LayoutKind.Sequential)] private struct RectE27 {public int Left,Top,Right,Bottom;}
    [DllImport("advapi32.dll",EntryPoint="OpenProcessToken")]private static extern bool OpenProcessTokenE27(nint p,uint access,out nint token);
    [DllImport("advapi32.dll",EntryPoint="GetTokenInformation")]private static extern bool GetTokenInformationE27(nint token,int kind,out nint linked,int size,out int needed);
    [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)]private static extern bool CreateProcessWithTokenW(nint token,uint flags,string app,string? command,uint creation,nint environment,string directory,ref StartupE27 startup,out CreatedE27 process);
    [DllImport("kernel32.dll",EntryPoint="CloseHandle")]private static extern bool CloseHandleE27(nint handle);
    [DllImport("user32.dll",EntryPoint="GetDlgItem")]private static extern nint GetDlgItemE27(nint window,int id);
    [DllImport("user32.dll",EntryPoint="GetWindowRect")]private static extern bool GetWindowRectE27(nint window,out RectE27 rect);
}
