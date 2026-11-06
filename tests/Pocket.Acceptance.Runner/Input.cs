using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Pocket.Acceptance.Runner;

/// <summary>TEST ONLY. Never referenced by GHOST product projects. Every injection requires the isolated input desktop.</summary>
internal static class Input
{
    public static void InitializeDpi(){SetProcessDpiAwarenessContext(new nint(-4));if(GetAwarenessFromDpiAwarenessContext(GetThreadDpiAwarenessContext())!=2)throw new InvalidOperationException("Runner must be per-monitor DPI aware.");}
    public static int Awareness=>GetAwarenessFromDpiAwarenessContext(GetThreadDpiAwarenessContext());
    [StructLayout(LayoutKind.Sequential)]private struct Event{public uint Type;public Union Data;}
    [StructLayout(LayoutKind.Explicit)]private struct Union{[FieldOffset(0)]public Mouse Mouse;[FieldOffset(0)]public Keyboard Keyboard;}
    [StructLayout(LayoutKind.Sequential)]private struct Mouse{public int X,Y;public uint Data,Flags,Time;public nuint Extra;}
    [StructLayout(LayoutKind.Sequential)]private struct Keyboard{public ushort Key,Scan;public uint Flags,Time;public nuint Extra;}
    [StructLayout(LayoutKind.Sequential)]internal struct Point{public int X,Y;}
    public static int ExpectedPid;
    public static void CheckTarget()
    {
        Desktop.Guard();
        GetWindowThreadProcessId(GetForegroundWindow(),out uint pid);
        if(pid!=ExpectedPid)throw new InvalidOperationException("Input target is not the controlled fixture process.");
    }
    private static void Send(Event item)
    {
        CheckTarget();
        if(SendInput(1,new[]{item},Marshal.SizeOf<Event>())!=1)throw new Win32Exception(Marshal.GetLastPInvokeError(),"Injected input rejected.");
    }
    public static void Move(double x,double y)
    {
        int left=GetSystemMetrics(76),top=GetSystemMetrics(77),width=GetSystemMetrics(78),height=GetSystemMetrics(79);
        Send(new(){Type=0,Data=new(){Mouse=new(){X=(int)Math.Round((x-left)*65535/(width-1)),Y=(int)Math.Round((y-top)*65535/(height-1)),Flags=0x8000|0x4000|1}}});
    }
    public static void Down()=>Send(new(){Type=0,Data=new(){Mouse=new(){Flags=2}}});
    public static void Up()=>Send(new(){Type=0,Data=new(){Mouse=new(){Flags=4}}});
    public static void Click(double x,double y){Move(x,y);Down();Up();}
    public static void Text(string text)
    {
        if (text.Length == 0) return;
        CheckTarget();
        uint thread = GetWindowThreadProcessId(GetForegroundWindow(), out uint pid);
        if (pid != ExpectedPid || thread == 0)
            throw new InvalidOperationException("Cannot resolve the typing fixture keyboard layout.");
        nint layout = GetKeyboardLayout(thread);
        bool caps = (GetKeyState(0x14) & 1) != 0;
        var events = new List<Event>(text.Length * 4);
        foreach (char character in text)
        {
            short mapped = VkKeyScanEx(character, layout);
            if (mapped == -1 || (mapped >> 8) is not (0 or 1))
                throw new InvalidOperationException("The typing fixture layout cannot type a seeded character with an ordinary key.");
            bool shift = ((mapped >> 8) == 1) ^ (caps && character is >= 'A' and <= 'Z');
            ushort key = (ushort)(mapped & 0xff);
            if (shift) events.Add(KeyboardEvent(0x10, false));
            events.Add(KeyboardEvent(key, false));
            events.Add(KeyboardEvent(key, true));
            if (shift) events.Add(KeyboardEvent(0x10, true));
        }
        CheckTarget();
        uint inserted = SendInput((uint)events.Count, events.ToArray(), Marshal.SizeOf<Event>());
        if (inserted != events.Count)
            throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Injected text was incomplete ({inserted}/{events.Count} events).");
    }
    private static Event KeyboardEvent(ushort key, bool up) =>
        new() { Type = 1, Data = new() { Keyboard = new() { Key = key, Flags = up ? 2u : 0u } } };
    public static void Key(ushort key,bool up)=>Send(KeyboardEvent(key,up));

    [DllImport("user32.dll")] private static extern nint GetKeyboardLayout(uint thread);
    [DllImport("user32.dll")] private static extern short GetKeyState(int key);
    [DllImport("user32.dll", EntryPoint = "VkKeyScanExW", CharSet = CharSet.Unicode)]
    private static extern short VkKeyScanEx(char character, nint layout);
    public static void ReleaseAll()
    {
        Desktop.Guard();
        var events=new[]{new Event{Data=new(){Mouse=new(){Flags=4}}},
            new Event{Type=1,Data=new(){Keyboard=new(){Key=0x12,Flags=2}}},
            new Event{Type=1,Data=new(){Keyboard=new(){Key=0x10,Flags=2}}},
            new Event{Type=1,Data=new(){Keyboard=new(){Key=0x11,Flags=2}}}};
        SendInput((uint)events.Length,events,Marshal.SizeOf<Event>());
    }
    public static Point Cursor(){if(!GetPhysicalCursorPos(out var point))throw new Win32Exception();return point;}
    public static nint Foreground=>GetForegroundWindow();
    public static bool Focus(nint hwnd)=>SetForegroundWindow(hwnd);
    public static bool Exists(nint hwnd)=>IsWindow(hwnd);
    [DllImport("user32.dll",SetLastError=true)]private static extern bool SetProcessDpiAwarenessContext(nint context);
    [DllImport("user32.dll")]private static extern nint GetThreadDpiAwarenessContext();
    [DllImport("user32.dll")]private static extern int GetAwarenessFromDpiAwarenessContext(nint context);
    [DllImport("user32.dll",SetLastError=true)]private static extern uint SendInput(uint count,Event[] items,int size);
    [DllImport("user32.dll",SetLastError=true)]private static extern bool GetPhysicalCursorPos(out Point point);
    [DllImport("user32.dll")]private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint hwnd,out uint pid);
    [DllImport("user32.dll")]private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")]private static extern bool IsWindow(nint hwnd);
}
