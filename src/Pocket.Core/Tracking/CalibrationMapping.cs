using System.Text.Json.Serialization;

namespace Pocket.Core.Tracking;

/// <summary>Physical desktop bounds and effective DPI for one monitor. DIPs are monitor-local.</summary>
public readonly record struct CalibrationDisplay([property: JsonRequired] int Number,
    [property: JsonRequired] int X, [property: JsonRequired] int Y,
    [property: JsonRequired] int Width, [property: JsonRequired] int Height, [property: JsonRequired] int Dpi)
{
    [JsonIgnore]
    public bool IsValid => Number > 0 && Width > 1 && Height > 1 && Dpi is >= 48 and <= 960;
}

public sealed class CalibrationMapping
{
    private readonly DisplayTransform _display;
    private readonly int _width, _height;
    private readonly double _inset, _sensitivity;
    private readonly bool _mirror;

    public CalibrationMapping(CalibrationDisplay display, double inset, double sensitivity, bool mirror)
    {
        if (!display.IsValid || !IsValidRegion(inset) || !(sensitivity is >= .5 and <= 2))
            throw new ArgumentException("Invalid calibration geometry.");
        _display = new(new(display.X, display.Y), display.Dpi);
        _width = display.Width - 1;
        _height = display.Height - 1;
        _inset = inset;
        _sensitivity = sensitivity;
        _mirror = mirror;
    }

    public static bool IsValidRegion(double inset) => inset is >= .05 and <= .30;
    public ScreenPoint ToPhysical(DipPoint logical) => _display.ToScreen(logical);

    public DipPoint? Map(DipPoint camera)
    {
        if (!camera.IsFinite || !InRegion(camera.X) || !InRegion(camera.Y)) return null;
        double u = Shape((camera.X - _inset) / (1 - 2 * _inset), _sensitivity);
        double v = Shape((camera.Y - _inset) / (1 - 2 * _inset), _sensitivity);
        return new((_mirror ? 1 - u : u) * _width * 96 / _display.Dpi, v * _height * 96 / _display.Dpi);
    }

    public DipPoint? Inverse(DipPoint logical)
    {
        double u = logical.X * _display.Dpi / (96 * _width);
        double v = logical.Y * _display.Dpi / (96 * _height);
        if (!logical.IsFinite || !(u is >= -1e-12 and <= 1.000000000001 && v is >= -1e-12 and <= 1.000000000001))
            return null;
        u = Math.Clamp(u, 0, 1);
        v = Math.Clamp(v, 0, 1);
        return new(_inset + (1 - 2 * _inset) * Shape(_mirror ? 1 - u : u, 1 / _sensitivity),
            _inset + (1 - 2 * _inset) * Shape(v, 1 / _sensitivity));
    }

    private bool InRegion(double value) => value >= _inset && value <= 1 - _inset;
    private static double Shape(double value, double sensitivity)
    {
        double centered = 2 * Math.Clamp(value, 0, 1) - 1;
        return (1 + Math.CopySign(Math.Pow(Math.Abs(centered), sensitivity), centered)) / 2;
    }
}
