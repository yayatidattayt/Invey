using System.Collections.Immutable;
using Pocket.Core.Tracking;
using Xunit;

namespace Pocket.Core.Tests;

public sealed class CalibrationFlowTests
{
    private static readonly CalibrationDisplay Display = new(2, -1920, -1200, 1920, 1200, 120);

    [Theory]
    [InlineData(false, 0.5)]
    [InlineData(true, 1.0)]
    [InlineData(true, 2.0)]
    public void RectangleRouteMapsToSelectedDisplayAndBack(bool mirror, double sensitivity)
    {
        var map = new CalibrationMapping(Display, .125, sensitivity, mirror);
        var camera = new[] { new DipPoint(.125, .125), new(.875, .125), new(.875, .875), new(.125, .875), new(.125, .125) };
        for (int i = 0; i < camera.Length; i++)
        {
            var dip = map.Map(camera[i])!.Value;
            bool right = (camera[i].X == .875) != mirror;
            bool bottom = camera[i].Y == .875;
            Assert.Equal(new ScreenPoint(right ? -1 : -1920, bottom ? -1 : -1200), map.ToPhysical(dip));
            Assert.Equal((right ? 1919 : 0) * .8, dip.X, 10);
            Assert.Equal((bottom ? 1199 : 0) * .8, dip.Y, 10);
            Assert.Equal(camera[i], map.Inverse(dip));
        }
        var interior = new DipPoint(.29, .72);
        var roundtrip = map.Inverse(map.Map(interior)!.Value)!.Value;
        Assert.Equal(interior.X, roundtrip.X, 10);
        Assert.Equal(interior.Y, roundtrip.Y, 10);
        Assert.Null(map.Map(new(.1, .4)));
        Assert.Null(map.Map(new(double.NaN, .4)));
        Assert.Null(map.Inverse(new(-10, 0)));
        Assert.Throws<ArgumentException>(() => new CalibrationMapping(Display, .5, 1, false));
        Assert.Throws<ArgumentException>(() => new CalibrationMapping(Display with { Dpi = 0 }, .125, 1, false));
        Assert.Equal(new DipRect(0, 0, 1920, 1080), CalibrationReadiness.FitAspect(7680, 4320, 1920, 1080));
    }

    [Fact]
    public void SettingsReloadAndIgnoreIncompleteSiblingOrInvalidDocument()
    {
        var folder = Directory.CreateTempSubdirectory("invey-calibration-");
        string path = Path.Combine(folder.FullName, "calibration.json");
        var prefs = new CalibrationPreferences(1, Display, 1, 2, 15, 125);
        try
        {
            new CalibrationPreferencesStore(path).Save(prefs);
            File.WriteAllText(path + ".tmp", "{");
            Assert.Equal((prefs, false), new CalibrationPreferencesStore(path).Load());
            Assert.Throws<ArgumentException>(() => new CalibrationPreferencesStore(path).Save(prefs with { Version = 2 }));
            Assert.Equal((prefs, false), new CalibrationPreferencesStore(path).Load());
            new CalibrationPreferencesStore(path).Save(prefs with { Mirror = 0 });
            Assert.Equal(0, new CalibrationPreferencesStore(path).Load().Preferences!.Mirror);
            File.WriteAllText(path, """{"Version":1,"CameraEnabled":1}""");
            Assert.Equal((null, true), new CalibrationPreferencesStore(path).Load());
            File.WriteAllText(path, "{");
            Assert.Equal((null, true), new CalibrationPreferencesStore(path).Load());
        }
        finally
        {
            File.Delete(path);
            File.Delete(path + ".tmp");
            folder.Delete();
        }
    }

    [Fact]
    public void CoalescedAmbiguityCannotCarryReadyIntoNextHand()
    {
        var tracker = new TrackingSession();
        var id = tracker.Enable(TrackingSource.Recorded)!.Value;
        Assert.True(tracker.Accept(new TrackerReady(id)));
        var readiness = new CalibrationReadiness();
        for (int i = 0; i < 6; i++)
        {
            Assert.True(tracker.Accept(Hand(id, i, false)));
            readiness.Observe(tracker.Read(), HandSide.Left, false, .12);
        }
        Assert.Equal(1, readiness.ReadyEntries);
        Assert.True(tracker.Accept(new HandObservation(id, 6, 300, 310, [], Ambiguous: true)));
        Assert.True(tracker.Accept(Hand(id, 7, false)));
        Assert.Equal(CalibrationState.Stabilizing, readiness.Observe(tracker.Read(), HandSide.Left, false, .12).State);
        Assert.Equal(1, readiness.Consecutive);
        Assert.Contains("Ambiguous", readiness.Resets.Keys);
        readiness.Invalidate();
        Assert.Equal(CalibrationState.Waiting, readiness.Observe(tracker.Read(), HandSide.Left, false, .12).State);
    }

    [Fact]
    public void PracticeUsesDistinctFramesAndCancelsOnLossStopOrReorderedFrame()
    {
        var id = Guid.NewGuid();
        var practice = new CalibrationPractice();
        PracticeState At(int seq, bool closed, bool target = true) =>
            practice.Update(Snapshot(Hand(id, seq, closed)), true, target);
        Assert.Equal(PracticeState.OpenHand, At(0, false));
        At(1, false);
        Assert.Equal(PracticeState.Move, At(2, false));
        Assert.Equal(PracticeState.Move, At(3, true, false));
        Assert.Equal(PracticeState.PinchCandidate, At(4, true));
        Assert.Equal(PracticeState.PinchCandidate, At(4, true));
        At(5, true);
        Assert.Equal(PracticeState.Held, At(6, true));
        At(7, false);
        At(8, false);
        Assert.Equal(PracticeState.Released, At(9, false));
        At(10, true);
        At(11, true);
        Assert.Equal(PracticeState.Held, At(12, true));
        Assert.Equal(PracticeState.Canceled, At(11, true));
        Assert.Equal(PracticeState.OpenHand, At(13, true));
        At(14, false);
        At(15, false);
        At(16, false);
        At(17, true);
        At(18, true);
        Assert.Equal(PracticeState.Held, At(19, true));
        foreach (var state in new[] { TrackingState.Stale, TrackingState.Stopping, TrackingState.Unavailable })
            Assert.Equal(PracticeState.Canceled, practice.Update(Snapshot(Hand(id, 20, false)) with
                { State = state, Observation = null }, false, false));
        Assert.Equal(PracticeState.OpenHand, At(21, true));
    }

    private static HandObservation Hand(Guid id, int sequence, bool closed)
    {
        var points = Enumerable.Repeat(new ImageLandmark(.5, .5, 0), 21).ToArray();
        points[4] = new(closed ? .51 : .62, .5, 0);
        points[5] = new(.4, .5, 0);
        points[17] = new(.6, .5, 0);
        return new(id, sequence, sequence * 50, sequence * 50 + 10, points.ToImmutableArray(), HandSide.Left,
            FrameSize: new(640, 480));
    }

    private static TrackingSnapshot Snapshot(HandObservation hand) =>
        new(TrackingState.Observing, hand.SessionId, TrackingSource.Recorded, hand, TimeSpan.Zero, null, false, 0, 0);
}
