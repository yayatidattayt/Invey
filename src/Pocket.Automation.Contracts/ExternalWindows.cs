using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Pocket.Automation.Contracts;

/// <summary>Native metadata only. No desktop-wide UIA inspection, focus or input injection.</summary>
public static class ExternalWindows
{
    public sealed record Choice(long Window,int ProcessId,long Lifetime,string Label);
    public static IReadOnlyList<Choice> List()
    {
        var choices=new List<Choice>();
        EnumWindows((w,_)=> {
            if(choices.Count>=128)return false;
            try {
                if(!Eligible(w,out var p))return true;
                using(p) {var title=new StringBuilder(160);GetWindowText(w,title,title.Capacity);
                    if(title.Length>0)choices.Add(new(w.ToInt64(),p.Id,p.StartTime.ToUniversalTime().Ticks,$"{p.ProcessName} · {title}"));}
            } catch(Exception e) when(e is System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException) { }
            return true;
        },0);
        return choices;
    }
    public static ExternalGrant Consent(Choice choice,Guid generation,string[] actions)
    {
        using var host=Process.GetCurrentProcess();
        var grant=new ExternalGrant(Guid.NewGuid().ToString(),generation,choice.Window,choice.ProcessId,choice.Lifetime,
            host.Id,host.StartTime.ToUniversalTime().Ticks,actions.ToArray());
        if(!grant.Valid || !Current(grant))throw new InvalidOperationException("Selected window is unavailable, protected, elevated or replaced.");
        return grant;
    }
    public static bool Current(ExternalGrant grant)
    {
        try {
            if(!grant.Valid || !Eligible(new(grant.Window),out var p))return false;
            using(p) using(var host=Process.GetProcessById(grant.HostProcessId))
                return p.Id==grant.ProcessId && p.StartTime.ToUniversalTime().Ticks==grant.Lifetime &&
                    !host.HasExited && host.StartTime.ToUniversalTime().Ticks==grant.HostLifetime &&
                    host.SessionId==p.SessionId && Integrity(host.Handle) is int hostLevel && Integrity(p.Handle) is int targetLevel && hostLevel>=targetLevel;
        } catch(Exception e) when(e is System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException) {return false;}
    }
    public static bool Reachable(ExternalGrant grant,double x,double y)
    {
        if(!Current(grant) || !double.IsFinite(x)||!double.IsFinite(y))return false;
        int count=0;
        for(nint w=GetTopWindow(0);w!=0 && count++<2048;w=GetWindow(w,2))
        {
            if(!IsWindowVisible(w)||IsIconic(w)||!GetWindowRect(w,out var b)||x<b.Left||x>=b.Right||y<b.Top||y>=b.Bottom)continue;
            if(DwmGetWindowAttribute(w,14,out int cloaked,4)!=0 || cloaked!=0)continue;
            GetWindowThreadProcessId(w,out uint pid);
            // Exclude only this consenting host's verified passive overlay, never arbitrary transparent apps.
            long style=GetWindowLongPtr(w,-20).ToInt64();
            if(pid==grant.HostProcessId && (style&0x08000020)==0x08000020)continue;
            return w.ToInt64()==grant.Window;
        }
        return false;
    }
    public static string Diagnostic(long hwnd)
    {
        var w=new nint(hwnd);GetWindowThreadProcessId(w,out uint pid);DwmGetWindowAttribute(w,14,out int cloaked,4);
        using var p=Process.GetProcessById((int)pid);using var self=Process.GetCurrentProcess();
        return $"Window={IsWindow(w)},Visible={IsWindowVisible(w)},Minimized={IsIconic(w)},TopLevel={GetAncestor(w,2)==w},Cloaked={cloaked},Integrity={Integrity(p.Handle)},HostIntegrity={Integrity(self.Handle)},Session={p.SessionId}/{self.SessionId}";
    }
    private static bool Eligible(nint w,out Process process)
    {
        process=null!;
        if(!IsWindow(w)||!IsWindowVisible(w)||IsIconic(w)||GetAncestor(w,2)!=w)return false;
        if(DwmGetWindowAttribute(w,14,out int cloaked,4)!=0||cloaked!=0)return false;
        GetWindowThreadProcessId(w,out uint pid);
        if(pid==0||pid==Environment.ProcessId)return false;
        var candidate=Process.GetProcessById((int)pid);
        using var self=Process.GetCurrentProcess();
        if(candidate.HasExited||candidate.SessionId!=self.SessionId||Integrity(candidate.Handle) is not int level||(level!=0x2000 && !BenignDemo(candidate))||Integrity(self.Handle) is not int ownLevel||ownLevel<level)
        {candidate.Dispose();return false;}
        process=candidate;return true;
    }
    private static bool BenignDemo(Process process)
    {
        // The elevated automation host may inherit High IL. Only our exact synthetic-content
        // executable is eligible there; general applications remain Medium IL only.
        string expected=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","..","src","Invey.ExternalDemo","bin","Release","net10.0-windows","Invey.ExternalDemo.exe"));
        return process.ProcessName=="Invey.ExternalDemo" && string.Equals(process.MainModule?.FileName,expected,StringComparison.OrdinalIgnoreCase);
    }
    private static int? Integrity(nint process)
    {
        if(!OpenProcessToken(process,8,out var token))return null;
        nint buffer=0;
        try {
            GetTokenInformation(token,25,0,0,out int size);if(size<=0||size>4096)return null;
            buffer=Marshal.AllocHGlobal(size);
            if(!GetTokenInformation(token,25,buffer,size,out _))return null;
            nint sid=Marshal.ReadIntPtr(buffer);int count=Marshal.ReadByte(GetSidSubAuthorityCount(sid));
            return count>0?Marshal.ReadInt32(GetSidSubAuthority(sid,(uint)(count-1))):null;
        } finally {if(buffer!=0)Marshal.FreeHGlobal(buffer);CloseHandle(token);}
    }
    [StructLayout(LayoutKind.Sequential)] private struct Rect {public int Left,Top,Right,Bottom;}
    private delegate bool EnumProc(nint w,nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumProc callback,nint parameter);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern int GetWindowText(nint w,StringBuilder text,int size);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint w,out uint pid);
    [DllImport("user32.dll")] private static extern bool IsWindow(nint w);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint w);
    [DllImport("user32.dll")] private static extern bool IsIconic(nint w);
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint w,uint flags);
    [DllImport("user32.dll")] private static extern nint GetTopWindow(nint w);
    [DllImport("user32.dll")] private static extern nint GetWindow(nint w,uint command);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint w,out Rect rect);
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] private static extern nint GetWindowLongPtr(nint w,int index);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(nint w,int attribute,out int value,int size);
    [DllImport("advapi32.dll")] private static extern bool OpenProcessToken(nint process,uint access,out nint token);
    [DllImport("advapi32.dll")] private static extern bool GetTokenInformation(nint token,int information,nint buffer,int size,out int needed);
    [DllImport("advapi32.dll")] private static extern nint GetSidSubAuthorityCount(nint sid);
    [DllImport("advapi32.dll")] private static extern nint GetSidSubAuthority(nint sid,uint index);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(nint handle);
}
