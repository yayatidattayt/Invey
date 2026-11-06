using System.ComponentModel;
using System.Runtime.InteropServices;
using Pocket.Core;

namespace Pocket.Overlay;

public sealed record MonitorGeometry(int Left, int Top, int Width, int Height);
public sealed record NativeObservation(int CursorX, int CursorY, long Foreground);
public static class NativeWindow
{
    public const long RequiredStyles = 0x80000 | 0x20 | 0x08000000 | 0x80;
    public static long ConfigurePassive(nint hwnd)
    {
        Marshal.SetLastPInvokeError(0);
        var current=GetWindowLongPtr(hwnd,-20);
        if(current==0 && Marshal.GetLastPInvokeError()!=0) throw new Win32Exception(Marshal.GetLastPInvokeError());
        Marshal.SetLastPInvokeError(0);
        var previous=SetWindowLongPtr(hwnd,-20,new(current.ToInt64() | RequiredStyles));
        if(previous==0 && Marshal.GetLastPInvokeError()!=0) throw new Win32Exception(Marshal.GetLastPInvokeError());
        var actual=ReadStyles(hwnd);
        if((actual & RequiredStyles)!=RequiredStyles) throw new InvalidOperationException("Passive HWND style assertion failed.");
        return actual;
    }
    public static long ReadStyles(nint hwnd) => GetWindowLongPtr(hwnd,-20).ToInt64();
    public static MonitorGeometry PrimaryMonitor()
    {
        var monitor=MonitorFromPoint(new Point(),1);
        var info=new MonitorInfo { Size=Marshal.SizeOf<MonitorInfo>() };
        if(!GetMonitorInfo(monitor,ref info)) throw new Win32Exception(Marshal.GetLastPInvokeError());
        return new(info.Monitor.Left,info.Monitor.Top,info.Monitor.Right-info.Monitor.Left,info.Monitor.Bottom-info.Monitor.Top);
    }
    public static void Position(nint hwnd, MonitorGeometry monitor)
    {
        if(!SetWindowPos(hwnd,new nint(-1),monitor.Left,monitor.Top,monitor.Width,monitor.Height,0x10|0x20))
            throw new Win32Exception(Marshal.GetLastPInvokeError());
    }
    public static uint Dpi(nint hwnd)
    {
        var dpi=GetDpiForWindow(hwnd);
        return dpi==0 ? throw new InvalidOperationException("Unable to determine HWND DPI.") : dpi;
    }
    public static NativeObservation Observe()
    {
        if(!GetCursorPos(out var p)) throw new Win32Exception(Marshal.GetLastPInvokeError());
        return new(p.X,p.Y,GetForegroundWindow().ToInt64());
    }
    public static nint Foreground => GetForegroundWindow();
    public static bool Exists(nint hwnd) => IsWindow(hwnd);
    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X,Y; }
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left,Top,Right,Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public Rect Monitor,Work; public uint Flags; }
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW",SetLastError=true)] private static extern nint GetWindowLongPtr(nint hwnd,int index);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW",SetLastError=true)] private static extern nint SetWindowLongPtr(nint hwnd,int index,nint value);
    [DllImport("user32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(nint hwnd,nint after,int x,int y,int width,int height,uint flags);
    [DllImport("user32.dll")] private static extern nint MonitorFromPoint(Point point,uint flags);
    [DllImport("user32.dll",EntryPoint="GetMonitorInfoW",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(nint monitor,ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(nint hwnd);
    [DllImport("user32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindow(nint hwnd);
}
