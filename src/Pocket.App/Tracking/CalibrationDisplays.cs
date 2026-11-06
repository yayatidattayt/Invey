using System.Runtime.InteropServices;
using System.Windows.Interop;
using Pocket.Core.Tracking;
using Screen = System.Windows.Forms.Screen;

namespace Pocket.App.Tracking;

internal sealed record CalibrationDisplayChoice(CalibrationDisplay Geometry, string Label, bool Primary);

internal static class CalibrationDisplays
{
    public static CalibrationDisplayChoice[] Find()
    {
        var choices = new List<CalibrationDisplayChoice>();
        foreach (var screen in Screen.AllScreens)
        {
            if (!screen.DeviceName.StartsWith(@"\\.\DISPLAY", StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(screen.DeviceName.AsSpan(11), out int number))
                continue;
            var bounds = screen.Bounds;
            // GetDpiForWindow reads effective DPI for a PerMonitorV2 window placed on this monitor.
            // A hidden, non-activating probe avoids moving the lab or assuming its DPI applies.
            using var probe = new HwndSource(new HwndSourceParameters("Invey display measurement")
            {
                WindowStyle = unchecked((int)0x80000000), // WS_POPUP, without WS_VISIBLE
                ExtendedWindowStyle = 0x08000080, // WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW
                PositionX = bounds.X + bounds.Width / 2,
                PositionY = bounds.Y + bounds.Height / 2,
                Width = 1,
                Height = 1
            });
            int dpi = (int)GetDpiForWindow(probe.Handle);
            var geometry = new CalibrationDisplay(number, bounds.X, bounds.Y, bounds.Width, bounds.Height, dpi);
            if (geometry.IsValid)
                choices.Add(new(geometry, $"Display {number} · {bounds.Width} × {bounds.Height} · {dpi * 100 / 96}%"
                    + (screen.Primary ? " · Primary" : ""), screen.Primary));
        }
        return choices.OrderBy(choice => choice.Geometry.Number).ToArray();
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint window);
}
