using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Pocket.Core.Tracking;

// No images, raw landmarks or device identifiers are persisted by this subsystem.
public sealed record LearningContext(string Source, string DeviceHash, int FrameWidth, int FrameHeight,
    CalibrationDisplay Display, int Hand, bool Mirror, int Inset, int Sensitivity)
{
    [JsonIgnore] public bool IsValid => Source is "Camera" or "Recorded" or "Simulation" &&
        DeviceHash is { Length: 64 } && DeviceHash.All(Uri.IsHexDigit) && FrameWidth > 0 && FrameHeight > 0 &&
        Display.IsValid && Hand is 1 or 2 && Inset is >= 5 and <= 30 && Sensitivity is >= 50 and <= 200;
    public static string HashDevice(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
public sealed record TargetExample(Guid Id, DipPoint Observed, DipPoint Target, LearningContext Context);
public sealed record ErrorMetrics(int Count, double Mean, double Median, double P95, double Worst,
    double WorstRegion, double MeanPixels, double P95Pixels, double WorstPixels)
{
    [JsonIgnore] public bool IsValid => Count is >= 9 and <= 90 &&
        new[] { Mean, Median, P95, Worst, WorstRegion, MeanPixels, P95Pixels, WorstPixels }.All(x => double.IsFinite(x) && x >= 0);
}
public sealed record PersonalModel(Guid Id, LearningContext Context, double[] X, double[] Y,
    ErrorMetrics Baseline, ErrorMetrics Validated, int TrainingCount)
{
    // The fixed supported domain is deliberately smaller than the teaching grid. No extrapolation.
    [JsonIgnore] public bool IsValid => Id != Guid.Empty && Context is { IsValid: true } &&
        X is { Length: 3 } && Y is { Length: 3 } && X.Concat(Y).All(v => double.IsFinite(v) && Math.Abs(v) <= .12) &&
        TrainingCount is >= 9 and <= 90 && Baseline is { IsValid: true } && Validated is { IsValid: true } && Bounded();
    private bool Bounded()
    {
        foreach (double x in new[] { .2, .8 })
        foreach (double y in new[] { .2, .8 })
        {
            var d = Delta(new(x, y));
            if (Math.Abs(d.X) > .08 || Math.Abs(d.Y) > .08) return false;
        }
        // Avoid folding the local affine map.
        return (1 + X[1]) * (1 + Y[2]) - X[2] * Y[1] is > .7 and < 1.4;
    }
    private DipPoint Delta(DipPoint p) => new(X[0] + X[1] * (p.X - .5) + X[2] * (p.Y - .5),
        Y[0] + Y[1] * (p.X - .5) + Y[2] * (p.Y - .5));
    public DipPoint Predict(DipPoint p, LearningContext context)
    {
        if (context != Context || !p.IsFinite || p.X is < .2 or > .8 || p.Y is < .2 or > .8) return p;
        var d = Delta(p);
        return new(p.X + d.X, p.Y + d.Y);
    }
}
public sealed record LearningEvaluation(string Source, bool Accepted, string Reason, ErrorMetrics? BeforeTraining,
    ErrorMetrics? Baseline, ErrorMetrics? Current, ErrorMetrics? Candidate)
{
    [JsonIgnore] public bool IsValid => Source is "Camera" or "Simulation" or "Recorded" && Reason.Length <= 256 &&
        new[] { BeforeTraining, Baseline, Current, Candidate }.All(m => m is null || m.IsValid);
}
public sealed record LearningTrial(PersonalModel? Candidate, string Reason,
    ErrorMetrics? Baseline = null, ErrorMetrics? Current = null, ErrorMetrics? CandidateMetrics = null)
{
    public bool Accepted => Candidate is not null;
}
public static class PersonalTrainer
{
    public const double Ridge = .01;
    public static LearningTrial Train(TargetExample[] training, TargetExample[] validation, PersonalModel? current)
    {
        if (!Dataset(training, true) || !Dataset(validation, false) ||
            training.Concat(validation).Select(x => x.Id).Distinct().Count() != training.Length + validation.Length ||
            training.Concat(validation).Any(x => x.Context != training[0].Context))
            return new(null, "Insufficient, duplicate, incompatible or invalid labeled evidence.");
        var context = training[0].Context;
        var a = new double[3, 3]; var bx = new double[3]; var by = new double[3];
        foreach (var row in training)
        {
            double[] f = [1, row.Observed.X - .5, row.Observed.Y - .5];
            for (int i = 0; i < 3; i++)
            {
                bx[i] += f[i] * (row.Target.X - row.Observed.X);
                by[i] += f[i] * (row.Target.Y - row.Observed.Y);
                for (int j = 0; j < 3; j++) a[i, j] += f[i] * f[j];
            }
        }
        for (int i = 0; i < 3; i++) a[i, i] += Ridge;
        var baseline = Measure(validation, null);
        var existing = Measure(validation, current?.Context == context ? current : null);
        var provisional = new PersonalModel(Guid.NewGuid(), context, Solve(a, bx), Solve(a, by), baseline, baseline, training.Length);
        if (!provisional.IsValid) return new(null, "Candidate exceeds correction or numerical bounds.", baseline, existing);
        var measured = Measure(validation, provisional);
        bool Better(ErrorMetrics reference) => measured.Mean < reference.Mean * .95 &&
            reference.Mean - measured.Mean >= .001 &&
            measured.P95 <= reference.P95 + .001 && measured.Worst <= reference.Worst + .001 &&
            measured.WorstRegion <= reference.WorstRegion + .001;
        // A localized regression must not be hidden by the overall mean.
        var regions = validation.GroupBy(x => Region(x.Target));
        bool regionSafe = regions.All(rows => Measure(rows.ToArray(), provisional).Mean <=
            Math.Min(Measure(rows.ToArray(), null).Mean, Measure(rows.ToArray(), current?.Context == context ? current : null).Mean) + .001);
        if (!Better(baseline) || !Better(existing) || !regionSafe)
            return new(null, "Independent targets did not show a safe improvement. Model unchanged.", baseline, existing, measured);
        return new(provisional with { Validated = measured }, "Accepted on independent targets; preview only.", baseline, existing, measured);
    }
    private static bool Dataset(TargetExample[] rows, bool training)
    {
        if (rows.Length is < 9 or > 90 || rows.Any(x => x is null || x.Id == Guid.Empty || x.Context is not { IsValid: true } ||
            !Unit(x.Observed) || !Unit(x.Target) || Distance(x.Observed, x.Target) > .12)) return false;
        if (rows.Select(x => Region(x.Target)).Distinct().Count() != 9) return false;
        // Teaching must surround the entire supported .2..8 domain.
        if (training && (rows.Min(x => x.Observed.X) > .2 || rows.Max(x => x.Observed.X) < .8 ||
                        rows.Min(x => x.Observed.Y) > .2 || rows.Max(x => x.Observed.Y) < .8 || !SurroundsDomain(rows))) return false;
        return true;
    }
    private static bool SurroundsDomain(TargetExample[] rows)
    {
        static double Cross(DipPoint a, DipPoint b, DipPoint c) =>
            (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        var points = rows.Select(r => r.Observed).Distinct().OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();
        var hull = new List<DipPoint>();
        foreach (var p in points)
        {
            while (hull.Count >= 2 && Cross(hull[^2], hull[^1], p) <= 0) hull.RemoveAt(hull.Count - 1);
            hull.Add(p);
        }
        int lower = hull.Count;
        foreach (var p in points.Reverse().Skip(1))
        {
            while (hull.Count > lower && Cross(hull[^2], hull[^1], p) <= 0) hull.RemoveAt(hull.Count - 1);
            hull.Add(p);
        }
        if (hull.Count < 4) return false;
        foreach (double x in new[] { .2, .8 })
        foreach (double y in new[] { .2, .8 })
            for (int i = 0; i < hull.Count - 1; i++)
                if (Cross(hull[i], hull[i + 1], new(x, y)) < -1e-10) return false;
        return true;
    }
    public static bool Unit(DipPoint p) => p.IsFinite && p.X is >= 0 and <= 1 && p.Y is >= 0 and <= 1;
    public static double Distance(DipPoint a, DipPoint b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
    private static int Region(DipPoint p) => Math.Min(2, (int)(p.X * 3)) + 3 * Math.Min(2, (int)(p.Y * 3));
    public static ErrorMetrics Measure(TargetExample[] rows, PersonalModel? model)
    {
        if (rows.Length == 0) throw new ArgumentException("No observations.");
        double Error(TargetExample x) => Distance(model?.Predict(x.Observed, x.Context) ?? x.Observed, x.Target);
        double Pixel(TargetExample x)
        {
            var p = model?.Predict(x.Observed, x.Context) ?? x.Observed;
            return Math.Sqrt(Math.Pow((p.X - x.Target.X) * (x.Context.Display.Width - 1), 2) +
                Math.Pow((p.Y - x.Target.Y) * (x.Context.Display.Height - 1), 2));
        }
        var e = rows.Select(Error).Order().ToArray(); var px = rows.Select(Pixel).Order().ToArray();
        int p95 = (int)Math.Ceiling(e.Length * .95) - 1;
        return new(e.Length, e.Average(), (e[(e.Length - 1) / 2] + e[e.Length / 2]) / 2,
            e[p95], e[^1], rows.GroupBy(x => Region(x.Target)).Max(g => g.Average(Error)),
            px.Average(), px[p95], px[^1]);
    }
    private static double[] Solve(double[,] matrix, double[] rhs)
    {
        var a = (double[,])matrix.Clone(); var b = (double[])rhs.Clone();
        for (int k = 0; k < 3; k++)
        {
            int pivot = k;
            for (int i = k + 1; i < 3; i++) if (Math.Abs(a[i, k]) > Math.Abs(a[pivot, k])) pivot = i;
            for (int j = 0; j < 3; j++) (a[k, j], a[pivot, j]) = (a[pivot, j], a[k, j]);
            (b[k], b[pivot]) = (b[pivot], b[k]);
            double d = a[k, k];
            for (int j = k; j < 3; j++) a[k, j] /= d;
            b[k] /= d;
            for (int i = 0; i < 3; i++)
            {
                if (i == k) continue;
                double f = a[i, k];
                for (int j = k; j < 3; j++) a[i, j] -= f * a[k, j];
                b[i] -= f * b[k];
            }
        }
        return b;
    }
}
