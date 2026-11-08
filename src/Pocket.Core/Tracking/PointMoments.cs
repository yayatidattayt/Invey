namespace Pocket.Core.Tracking;

/// <summary>Bounded aggregate position statistics, without a coordinate history.</summary>
public sealed class PointMoments
{
    private double _x, _y, _squares;
    public int Samples { get; private set; }
    public double? RmsRadius => Samples < 2 ? null :
        Math.Sqrt(Math.Max(0, _squares / Samples - Math.Pow(_x / Samples, 2) - Math.Pow(_y / Samples, 2)));

    public void Add(DipPoint point)
    {
        if (!point.IsFinite || Samples >= 4096) return;
        Samples++;
        _x += point.X;
        _y += point.Y;
        _squares += point.X * point.X + point.Y * point.Y;
    }
}
