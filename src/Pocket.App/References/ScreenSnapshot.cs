using System.ComponentModel;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Pocket.Core.References;
using Drawing = System.Drawing;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Screen = System.Windows.Forms.Screen;

namespace Pocket.App.References;

internal sealed record SnapshotDisplay(string Device, Drawing.Rectangle Bounds, string Label)
{
    public static SnapshotDisplay[] Available() => Screen.AllScreens.Select((screen, i) => new SnapshotDisplay(
        screen.DeviceName, screen.Bounds, $"Display {i + 1} · {screen.Bounds.Width} × {screen.Bounds.Height}" +
        (screen.Primary ? " · Primary" : ""))).ToArray();

    public bool StillConnected() => Available().Any(display => display.Device == Device && display.Bounds == Bounds);
}

internal static class ScreenSnapshot
{
    public static BitmapSource Capture(SnapshotDisplay display)
    {
        var bounds = display.Bounds;
        if (!PocketStore.SupportsSize(bounds.Width, bounds.Height))
            throw new InvalidDataException("This display exceeds the capture size limit.");
        if (!display.StillConnected()) throw new IOException("The selected display changed. Try again.");
        Synchronize();
        VerifyInputDesktop();
        using var bitmap = new Drawing.Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppRgb);
        using (var graphics = Drawing.Graphics.FromImage(bitmap))
            graphics.CopyFromScreen(bounds.Location, Drawing.Point.Empty, bounds.Size, Drawing.CopyPixelOperation.SourceCopy);
        VerifyInputDesktop();
        if (!display.StillConnected()) throw new IOException("The selected display changed. Try again.");
        var bits = bitmap.LockBits(new Drawing.Rectangle(Drawing.Point.Empty, bounds.Size), ImageLockMode.ReadOnly,
            PixelFormat.Format32bppRgb);
        try
        {
            var source = BitmapSource.Create(bounds.Width, bounds.Height, 96, 96, PixelFormats.Bgr32, null,
                bits.Scan0, checked(bits.Stride * bounds.Height), bits.Stride);
            source.Freeze();
            return source;
        }
        finally { bitmap.UnlockBits(bits); }
    }

    public static (BitmapSource Image, bool IsBlack) Crop(BitmapSource source, PixelRegion region)
    {
        int stride = checked(region.Width * 4);
        var bytes = new byte[checked(stride * region.Height)];
        source.CopyPixels(new Int32Rect(region.X, region.Y, region.Width, region.Height), bytes, stride, 0);
        bool visible = false;
        for (int i = 0; i < bytes.Length && !visible; i += 4)
            visible = (bytes[i] | bytes[i + 1] | bytes[i + 2]) != 0;
        var crop = BitmapSource.Create(region.Width, region.Height, 96, 96, PixelFormats.Bgr32, null, bytes, stride);
        crop.Freeze();
        return (crop, !visible);
    }

    public static void Place(Window window, Drawing.Rectangle bounds)
    {
        var handle = new WindowInteropHelper(window).EnsureHandle();
        if (!SetWindowPos(handle, 0, bounds.X, bounds.Y, bounds.Width, bounds.Height, 0x0014))
            throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    public static uint WindowDpi(Window window) => GetDpiForWindow(new WindowInteropHelper(window).Handle);
    public static Drawing.Rectangle? Bounds(nint window) => GetWindowRect(window, out var rect)
        ? Drawing.Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom) : null;
    public static bool NativeVisible(Window window) => IsWindowVisible(new WindowInteropHelper(window).Handle);
    public static bool SetCloaked(Window window, bool cloaked)
    {
        int value = cloaked ? 1 : 0;
        return DwmSetWindowAttribute(new WindowInteropHelper(window).Handle, 13, ref value, sizeof(int)) >= 0;
    }
    public static void Synchronize()
    {
        if (DwmFlush() < 0) throw new IOException("Windows could not synchronize the display. Try again.");
    }

    public static BitmapSource Decode(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var image = decoder.Frames[0];
        image.Freeze();
        return image;
    }

    private static void VerifyInputDesktop()
    {
        var input = OpenInputDesktop(0, false, 1);
        if (input == 0) throw new IOException("Capture is unavailable on the current Windows desktop.");
        try
        {
            if (DesktopName(input) != DesktopName(GetThreadDesktop(GetCurrentThreadId())))
                throw new IOException("The active Windows desktop changed. Try again.");
        }
        finally { CloseDesktop(input); }
    }

    private static string DesktopName(nint handle)
    {
        var name = new StringBuilder(256);
        if (!GetUserObjectInformation(handle, 2, name, 512, out _))
            throw new IOException("Could not verify the active Windows desktop.");
        return name.ToString();
    }

    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint window, uint attribute, ref int value, int size);
    [DllImport("user32.dll")] private static extern nint OpenInputDesktop(uint flags, bool inherit, uint access);
    [DllImport("user32.dll")] private static extern bool CloseDesktop(nint desktop);
    [DllImport("user32.dll")] private static extern nint GetThreadDesktop(uint thread);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(nint window);
    [StructLayout(LayoutKind.Sequential)] private struct WindowRect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint window, out WindowRect rect);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint window);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetUserObjectInformation(nint handle, int index, StringBuilder name, int bytes, out int needed);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
}
