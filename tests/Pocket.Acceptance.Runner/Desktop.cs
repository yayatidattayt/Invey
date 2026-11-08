using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Pocket.Acceptance.Runner;

internal static class Desktop
{
    public static string Name(nint handle)
    {
        var name=new StringBuilder(256);
        if(!GetUserObjectInformation(handle,2,name,512,out _))throw new Win32Exception(Marshal.GetLastPInvokeError());
        return name.ToString();
    }
    internal static string InputDesktopName()
    {
        nint input = OpenInputDesktop(0, false, 1);
        if (input == 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
        try { return Name(input); }
        finally { CloseDesktop(input); }
    }
    public static void Guard()
    {
        nint input=OpenInputDesktop(0,false,0x100);
        if(input==0)throw new InvalidOperationException("Input desktop unavailable or locked.");
        try {if(Name(input)!=Environment.GetEnvironmentVariable("GHOST_ACCEPTANCE_DESKTOP"))throw new InvalidOperationException("Refusing input outside designated test desktop.");}
        finally{CloseDesktop(input);}
    }
    public static int LaunchIsolated(string executable,string assembly,string output)
    {
        nint original=OpenInputDesktop(0,false,0x100|1);
        if(original==0)throw new Win32Exception(Marshal.GetLastPInvokeError());
        nint test=0,job=0;ProcessInformation process=default;
        try
        {
            if(Name(original)!="Default")throw new InvalidOperationException("Refusing to switch a secured or unexpected desktop: "+Name(original));
            string name="GhostAcceptance_"+Guid.NewGuid().ToString("N");
            test=CreateDesktop(name,0,0,0,0x01FF,0);
            if(test==0)throw new Win32Exception(Marshal.GetLastPInvokeError());
            Environment.SetEnvironmentVariable("GHOST_ACCEPTANCE_DESKTOP",name);
            job=CreateJobObject(0,null);
            var limits=new JobLimits();limits.Basic.LimitFlags=0x2000;
            if(job==0 || !SetInformationJobObject(job,9,ref limits,Marshal.SizeOf<JobLimits>()))throw new Win32Exception(Marshal.GetLastPInvokeError());
            var start=new StartupInfo { Size=Marshal.SizeOf<StartupInfo>(),Desktop=name };
            var command=new StringBuilder("\""+executable+"\" \""+assembly+"\" --session \""+output+"\"");
            if(!CreateProcess(executable,command,0,0,false,0x08000004,0,null,ref start,out process))throw new Win32Exception(Marshal.GetLastPInvokeError());
            if(!AssignProcessToJobObject(job,process.Process))throw new Win32Exception(Marshal.GetLastPInvokeError());
            if(!SwitchDesktop(test))throw new Win32Exception(Marshal.GetLastPInvokeError());
            ResumeThread(process.Thread);
            // Independent watchdog returns the original desktop even if the child hangs.
            using var done=new ManualResetEventSlim();
            var watchdog=Task.Run(()=>{if(!done.Wait(TimeSpan.FromSeconds(TestProfile.BudgetSeconds+5)))SwitchDesktop(original);});
            uint wait=WaitForSingleObject(process.Process,(uint)(TestProfile.BudgetSeconds*1000));
            done.Set();watchdog.GetAwaiter().GetResult();
            if(wait!=0)throw new TimeoutException("Acceptance session exceeded its profile budget.");
            GetExitCodeProcess(process.Process,out uint code);
            return (int)code;
        }
        finally
        {
            try { if(test!=0){try{Guard();Input.ReleaseAll();}catch(InvalidOperationException){}} }
            finally
            {
                SwitchDesktop(original);
                if(job!=0)CloseHandle(job); // kill only this acceptance process tree
                if(process.Thread!=0)CloseHandle(process.Thread);
                if(process.Process!=0)CloseHandle(process.Process);
                if(test!=0)CloseDesktop(test);CloseDesktop(original);
            }
        }
    }
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]private struct StartupInfo
    {
        public int Size;public string? Reserved,Desktop,Title;public int X,Y,XSize,YSize,XChars,YChars,Fill,Flags;public short Show,Reserved2;public nint ReservedPtr,Input,Output,Error;
    }
    [StructLayout(LayoutKind.Sequential)]private struct ProcessInformation{public nint Process,Thread;public uint ProcessId,ThreadId;}
    [StructLayout(LayoutKind.Sequential)]private struct BasicLimits{public long PerProcess,PerJob;public uint LimitFlags;public nuint Min,Max;public uint Active;public nuint Affinity;public uint Priority,Scheduling;}
    [StructLayout(LayoutKind.Sequential)]private struct IoCounters{public ulong Read,Write,Other,ReadBytes,WriteBytes,OtherBytes;}
    [StructLayout(LayoutKind.Sequential)]private struct JobLimits{public BasicLimits Basic;public IoCounters Io;public nuint ProcessMemory,JobMemory,PeakProcess,PeakJob;}
    [DllImport("user32.dll",SetLastError=true)]internal static extern nint OpenInputDesktop(uint flags,bool inherit,uint access);
    [DllImport("user32.dll",SetLastError=true)]internal static extern bool CloseDesktop(nint handle);
    [DllImport("user32.dll",EntryPoint="GetUserObjectInformationW",CharSet=CharSet.Unicode,SetLastError=true)]private static extern bool GetUserObjectInformation(nint handle,int index,StringBuilder name,int size,out int needed);
    [DllImport("user32.dll",EntryPoint="CreateDesktopW",CharSet=CharSet.Unicode,SetLastError=true)]private static extern nint CreateDesktop(string name,nint device,nint mode,uint flags,uint access,nint security);
    [DllImport("user32.dll",SetLastError=true)]private static extern bool SwitchDesktop(nint handle);
    [DllImport("kernel32.dll",EntryPoint="CreateProcessW",CharSet=CharSet.Unicode,SetLastError=true)]private static extern bool CreateProcess(string app,StringBuilder command,nint a,nint b,bool inherit,uint flags,nint env,string? cwd,ref StartupInfo start,out ProcessInformation process);
    [DllImport("kernel32.dll",SetLastError=true)]private static extern bool CloseHandle(nint handle);
    [DllImport("kernel32.dll")]private static extern uint ResumeThread(nint thread);
    [DllImport("kernel32.dll")]private static extern uint WaitForSingleObject(nint handle,uint milliseconds);
    [DllImport("kernel32.dll")]private static extern bool GetExitCodeProcess(nint handle,out uint code);
    [DllImport("kernel32.dll",EntryPoint="CreateJobObjectW",CharSet=CharSet.Unicode)]private static extern nint CreateJobObject(nint attributes,string? name);
    [DllImport("kernel32.dll",SetLastError=true)]private static extern bool SetInformationJobObject(nint job,int type,ref JobLimits info,int size);
    [DllImport("kernel32.dll",SetLastError=true)]private static extern bool AssignProcessToJobObject(nint job,nint process);
}
