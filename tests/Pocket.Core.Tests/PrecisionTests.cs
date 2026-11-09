using System.Text.Json;
using Pocket.Core;
using Pocket.Core.Tracking;
using Xunit;

namespace Pocket.Core.Tests;

public sealed class PrecisionTests(Xunit.Abstractions.ITestOutputHelper output)
{
    [Fact]
    public void CompareIdenticalSignalsAndPreserveMetrics()
    {
        var rows = new List<object>();
        foreach (var signal in new[] { "stationary", "slow", "rapid", "small-adjustment", "step", "curve", "reversal", "spike", "irregular", "dropout" })
        foreach (var mode in new[] { "raw", "original", "candidate" })
        {
            var original = new AdaptivePointFilter(); var candidate = new PrecisionPointFilter();
            var errors = new List<double>(); var xs = new List<double>(); var truth = new List<DipPoint>(); var displayed = new List<DipPoint>();
            long time = 0; int first = -1, half = -1, ninety = -1; double overshoot = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i=0; i<300; i++)
            {
                time += signal == "irregular" ? new[] { 12, 40, 19, 29 }[i%4] : signal == "dropout" && i==180 ? 300 : 20;
                double x=.5, y=.5;
                if (i>=100) (x,y) = signal switch {
                    "slow" => (.5 + (i-100)*.0005, .5),
                    "rapid" => (.5 + Math.Min(i-100,30)*.01, .5),
                    "small-adjustment" => (.506, .5),
                    "step" => (.8,.5),
                    "curve" => (.5+.15*Math.Sin((i-100)*.05), .5+.15*(1-Math.Cos((i-100)*.05))),
                    "reversal" => (.5+.002*(i<200 ? i-100 : 300-i),.5),
                    "irregular" or "dropout" => (.5+.1*Math.Sin((i-100)*.04), .5),
                    _ => (.5,.5) };
                double noise = signal == "stationary" ? (i%2==0 ? .008 : -.008) : .0015*Math.Sin(i*2.3);
                var raw = new DipPoint(signal=="spike" && i==150 ? .9 : x+noise,y);
                var p = mode=="raw" ? raw : mode=="original" ? original.Apply(raw,time) : candidate.Apply(raw,time);
                truth.Add(new(x,y)); displayed.Add(p);
                Assert.True(p.IsFinite); Assert.InRange(p.X,0,1); Assert.InRange(p.Y,0,1);
                if(i>=100) { errors.Add(Math.Sqrt(Math.Pow(p.X-x,2)+Math.Pow(p.Y-y,2))); xs.Add(p.X); }
                if(signal=="step" && i>=100) {
                    if(first<0 && p.X>.503) first=(i-100)*20;
                    if(half<0 && p.X>=.65) half=(i-100)*20;
                    if(ninety<0 && p.X>=.77) ninety=(i-100)*20;
                    overshoot=Math.Max(overshoot,p.X-.8);
                }
            }
            watch.Stop(); errors.Sort(); double rms=Math.Sqrt(errors.Average(e=>e*e));
            int lag=Enumerable.Range(0,21).MinBy(delay=>Enumerable.Range(120,160).Average(i=>
                Math.Pow(displayed[i].X-truth[i-delay].X,2)+Math.Pow(displayed[i].Y-truth[i-delay].Y,2)));
            int reversal=Enumerable.Range(200,60).FirstOrDefault(i=>displayed[i].X<displayed[i-1].X,-1);
            rows.Add(new { signal, mode, Rms=rms, RmsAt1920Pixels=rms*1919, P95=errors[(int)(errors.Count*.95)],
                StandardDeviation=Math.Sqrt(xs.Average(x=>Math.Pow(x-xs.Average(),2))), PeakToPeak=xs.Max()-xs.Min(),
                InitialResponseMs=first, Step50Ms=half, Step90Ms=ninety, Overshoot=overshoot,
                TrajectoryLagMs=signal is "curve" or "slow" or "rapid" ? (int?)(lag*20) : null,
                ReversalDelayMs=signal=="reversal" && reversal>=0 ? (int?)((reversal-200)*20) : null,
                CpuMs=watch.Elapsed.TotalMilliseconds });
        }
        string json=JsonSerializer.Serialize(new { Evidence="SYNTHETIC_SIGNAL", rows },new JsonSerializerOptions { WriteIndented=true });
        output.WriteLine(json);
        if(Environment.GetEnvironmentVariable("INVEY_PRECISION_BENCHMARK") is { } path) File.WriteAllText(path,json);
    }

    [Fact]
    public void SingleSpikeIsRejectedButSustainedStepAndTinyMotionAreNotStuck()
    {
        var f=new PrecisionPointFilter();
        for(int i=0;i<40;i++) f.Apply(new(.5,.5),i*20);
        Assert.Equal(.5,f.Apply(new(.9,.5),800).X);
        Assert.Equal(.5,f.Apply(new(.5,.5),820).X);
        for(int i=42;i<90;i++) f.Apply(new(.503,.5),i*20);
        Assert.InRange(f.Apply(new(.503,.5),1800).X,.5028,.503);
        f.Apply(new(.8,.5),1820);
        Assert.True(f.Apply(new(.8,.5),1840).X>.6);
        Assert.Equal(new DipPoint(.2,.3),f.Apply(new(.2,.3),2200));
        Assert.Throws<ArgumentException>(()=>f.Apply(new(double.NaN,.5),2220));
    }
}
