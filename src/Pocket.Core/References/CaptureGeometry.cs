namespace Pocket.Core.References;

public readonly record struct PixelRegion(int X, int Y, int Width, int Height);

public static class CaptureGeometry
{
    public static PixelRegion? Select(DipPoint start, DipPoint end, double viewportWidth, double viewportHeight,
        int pixelWidth, int pixelHeight)
    {
        if (!start.IsFinite || !end.IsFinite || !double.IsFinite(viewportWidth) || !double.IsFinite(viewportHeight) ||
            viewportWidth <= 0 || viewportHeight <= 0 || !PocketStore.SupportsSize(pixelWidth, pixelHeight)) return null;
        double left = Math.Clamp(Math.Min(start.X, end.X), 0, viewportWidth);
        double right = Math.Clamp(Math.Max(start.X, end.X), 0, viewportWidth);
        double top = Math.Clamp(Math.Min(start.Y, end.Y), 0, viewportHeight);
        double bottom = Math.Clamp(Math.Max(start.Y, end.Y), 0, viewportHeight);
        if (right <= left || bottom <= top) return null;
        int x = (int)Math.Floor(ToPixel(left, viewportWidth, pixelWidth));
        int y = (int)Math.Floor(ToPixel(top, viewportHeight, pixelHeight));
        int width = (int)Math.Ceiling(ToPixel(right, viewportWidth, pixelWidth)) - x;
        int height = (int)Math.Ceiling(ToPixel(bottom, viewportHeight, pixelHeight)) - y;
        return width > 0 && height > 0 ? new(x, y, width, height) : null;
    }

    private static double ToPixel(double dip, double viewport, int size)
    {
        double value = dip * size / viewport;
        double nearest = Math.Round(value);
        return Math.Abs(value - nearest) < 1e-9 ? nearest : value;
    }
}
