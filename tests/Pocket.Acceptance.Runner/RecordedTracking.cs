using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Pocket.Acceptance.Runner;

internal static partial class Program
{
    private static JsonElement Tracking(JsonElement state) => state.GetProperty("Tracking");
    private static string? State(JsonElement state) => Tracking(state).GetProperty("State").GetString();

    private static async Task RunRecordedTracking(Client client)
    {
        await Measure("F06_RECORDED_WPF_LIFECYCLE", "RECORDED_SOURCE_REAL_INFERENCE + UI_INTEGRATION",
            "Keyboard start, actual model hand/motion/loss/return, stop, new session and close", async () =>
        {
            var opened = await client.Command("tracking-open");
            long handle = opened.GetProperty("TrackingHandle").GetInt64();
            Require(State(opened) == "Off" && Tracking(opened).GetProperty("LastWorkerPid").ValueKind == JsonValueKind.Null,
                "Opening the lab started a worker.");
            Require(Tracking(opened).GetProperty("SourceStatus").GetString() == "Recorded source · Camera off",
                "The lab did not open in the camera-off recorded mode.");
            await FocusTrackingAsync(client, handle);
            var start = Stopwatch.StartNew();
            AccessKey(0x45); // Alt+E, actual WPF Enable button.

            var blank = Tracking(await Until(client, s => State(s) == "Lost" &&
                Tracking(s).GetProperty("AcceptedObservations").GetInt64() >= 2, 12000));
            double firstBlankMs = start.Elapsed.TotalMilliseconds;
            string id = blank.GetProperty("SessionId").GetString()!;
            Require(blank.GetProperty("Source").GetString() == "Recorded", "Recorded data was not identified as a recorded source.");
            var hand = Tracking(await Until(client, s => State(s) == "Observing", 5000));
            double firstHandMs = start.Elapsed.TotalMilliseconds;
            Require(hand.GetProperty("Status").GetString()!.Contains("21 landmarks"), "Rendered observation status is missing.");
            Require(!hand.GetProperty("CanStart").GetBoolean(), "Start stayed enabled with an active worker.");
            int pid = hand.GetProperty("LastWorkerPid").GetInt32();
            double wrist = hand.GetProperty("WristX").GetDouble();
            var moved = Tracking(await Until(client, s => State(s) == "Observing" &&
                Tracking(s).GetProperty("WristX").GetDouble() > wrist + .015, 5000));
            double wristDelta = moved.GetProperty("WristX").GetDouble() - wrist;

            var observed = new SortedDictionary<long, (double SourceMs, double AgeMs)>();
            void Capture(JsonElement sample)
            {
                if (sample.GetProperty("Sequence").ValueKind != JsonValueKind.Number) return;
                observed.TryAdd(sample.GetProperty("Sequence").GetInt64(),
                    (sample.GetProperty("SourceMs").GetDouble(), sample.GetProperty("HostAgeMs").GetDouble()));
            }
            Capture(hand);
            Capture(moved);
            var lost = Tracking(await Until(client, s =>
            {
                var sample = Tracking(s);
                Capture(sample);
                return State(s) == "Lost" && sample.GetProperty("AcceptedObservations").GetInt64() >= 23;
            }, 5000));
            Require(lost.GetProperty("Sequence").ValueKind == JsonValueKind.Null, "Loss left a displayed hand.");
            var returned = Tracking(await Until(client, s =>
            {
                var sample = Tracking(s);
                Capture(sample);
                return State(s) == "Observing" && sample.GetProperty("Sequence").GetInt64() >= 33;
            }, 5000));
            Require(returned.GetProperty("SessionId").GetString() == id, "Tracking return changed the session.");
            double sequenceDurationMs = start.Elapsed.TotalMilliseconds;
            using var process = Process.GetProcessById(pid);
            process.Refresh();
            long initialWorkingSet = process.WorkingSet64;
            double initialCpuMs = process.TotalProcessorTime.TotalMilliseconds;
            var sustained = Stopwatch.StartNew();
            long firstAccepted = returned.GetProperty("AcceptedObservations").GetInt64();
            var later = Tracking(await Until(client, s =>
            {
                var sample = Tracking(s);
                Capture(sample);
                return State(s) == "Observing" && sample.GetProperty("AcceptedObservations").GetInt64() >= firstAccepted + 92;
            }, 16000));
            Require(later.GetProperty("SessionId").GetString() == id, "Sustained run changed the session.");
            double sustainedMs = sustained.Elapsed.TotalMilliseconds;
            long additionalObservations = later.GetProperty("AcceptedObservations").GetInt64() - firstAccepted;
            process.Refresh();
            long finalWorkingSet = process.WorkingSet64;
            double sustainedCpuMs = process.TotalProcessorTime.TotalMilliseconds - initialCpuMs;

            await FocusTrackingAsync(client, handle);
            start.Restart();
            AccessKey(0x54); // Alt+T, actual WPF Stop button.
            var stopped = Tracking(await Until(client, s => State(s) == "Stopped" &&
                Tracking(s).GetProperty("CanStart").GetBoolean(), 5000));
            double stopMs = start.Elapsed.TotalMilliseconds;
            Require(stopped.GetProperty("SessionId").ValueKind == JsonValueKind.Null &&
                stopped.GetProperty("Sequence").ValueKind == JsonValueKind.Null && !ProcessExists(pid),
                "Stop was displayed without independently observed exit and cleared data.");

            start.Restart();
            AccessKey(0x52); // Alt+R, actual WPF Restart button.
            var restarted = Tracking(await Until(client, s => State(s) == "Observing", 12000));
            double restartMs = start.Elapsed.TotalMilliseconds;
            int newPid = restarted.GetProperty("LastWorkerPid").GetInt32();
            Require(restarted.GetProperty("SessionId").GetString() != id && newPid != pid,
                "Restart reused the previous generation.");
            await FocusTrackingAsync(client, handle);
            AccessKey(0x73); // Alt+F4, actual window close.
            await Until(client, s => s.GetProperty("TrackingHandle").GetInt64() == 0, 5000);
            Require(!ProcessExists(newPid) && !Input.Exists(new nint(handle)), "Window close leaked worker or HWND.");
            return new
            {
                Source = "public still translated in memory, real CPU VIDEO inference",
                CameraFrames = 0, FirstBlankMs = firstBlankMs, FirstHandMs = firstHandMs, WristDelta = wristDelta,
                SequenceDurationMs = sequenceDurationMs, DisplayedSamples = observed.Count,
                AcceptedAtReturn = returned.GetProperty("AcceptedObservations").GetInt64(),
                SourceToResultMedianMs = Median(observed.Values.Select(sample => sample.SourceMs)),
                HostDisplayedAgeMaxMs = observed.Values.Max(sample => sample.AgeMs),
                SustainedMs = sustainedMs, AdditionalObservations = additionalObservations,
                SustainedObservationsPerSecond = additionalObservations * 1000 / sustainedMs,
                InitialWorkingSetBytes = initialWorkingSet, FinalWorkingSetBytes = finalWorkingSet,
                SustainedWorkerCpuMs = sustainedCpuMs, StopMs = stopMs, RestartMs = restartMs,
                OldWorkerExited = true, NewWorkerExitedOnWindowClose = true
            };
        });

        await Measure("F06_RECORDED_STALE_RESULT", "UI_INTEGRATION + CONTROLLED_FAILURE_INJECTION",
            "A delayed result clears a real observation without claiming no hand, then resumes in the same session", async () =>
        {
            await client.Command("tracking-close");
            await client.Command("tracking-open");
            await client.Command("tracking-start-gap");
            var active = Tracking(await Until(client, s => State(s) == "Observing", 12000));
            string id = active.GetProperty("SessionId").GetString()!;
            int pid = active.GetProperty("LastWorkerPid").GetInt32();
            long initial = active.GetProperty("AcceptedObservations").GetInt64();
            var stale = Tracking(await Until(client, s => State(s) == "Stale" &&
                Tracking(s).GetProperty("AcceptedObservations").GetInt64() >= initial, 4000));
            long accepted = stale.GetProperty("AcceptedObservations").GetInt64();
            Require(stale.GetProperty("Sequence").ValueKind == JsonValueKind.Null && ProcessExists(pid) &&
                stale.GetProperty("Status").GetString()!.Contains("No recent tracking result"),
                "The stale result was retained or misreported as a no-hand inference.");
            var resumed = Tracking(await Until(client, s => State(s) == "Observing" &&
                Tracking(s).GetProperty("AcceptedObservations").GetInt64() > accepted, 5000));
            Require(resumed.GetProperty("SessionId").GetString() == id &&
                resumed.GetProperty("LastWorkerPid").GetInt32() == pid, "A result gap replaced the session or worker.");
            await client.Command("tracking-close");
            Require(!ProcessExists(pid), "Closing the recovered session leaked its worker.");
            return new { InitialAccepted = initial, AcceptedAtGap = accepted,
                AcceptedAfterGap = resumed.GetProperty("AcceptedObservations").GetInt64(), WorkerExited = true };
        });

        await Measure("F06_RECORDED_WORKER_FAULTS", "UI_INTEGRATION + CONTROLLED_FAILURE_INJECTION",
            "Actual worker failure, invalid messages, model failure, canceled startup and forced teardown", async () =>
        {
            await client.Command("tracking-close");
            var opened = await client.Command("tracking-open");
            long handle = opened.GetProperty("TrackingHandle").GetInt64();
            var outcomes = new Dictionary<string, string>();
            foreach (var (mode, expected) in new[] { ("crash", "WorkerExited"), ("malformed", "ProtocolFailure"), ("bad-model", "InvalidModel"), ("hang", "Timeout") })
            {
                await client.Command("tracking-start-" + mode);
                var failed = Tracking(await Until(client, s => State(s) == "Unavailable" &&
                    Tracking(s).GetProperty("CanStart").GetBoolean(), 12000));
                Require(failed.GetProperty("Failure").GetString() == expected &&
                    failed.GetProperty("Sequence").ValueKind == JsonValueKind.Null &&
                    !ProcessExists(failed.GetProperty("LastWorkerPid").GetInt32()), "Incorrect or uncontained worker fault: " + failed);
                outcomes.Add(mode, expected);
            }

            await client.Command("tracking-start-hang");
            await Until(client, s => State(s) == "Starting" &&
                Tracking(s).GetProperty("LastWorkerPid").ValueKind == JsonValueKind.Number, 5000);
            await FocusTrackingAsync(client, handle);
            AccessKey(0x54);
            var canceled = Tracking(await Until(client, s => State(s) == "Stopped" &&
                Tracking(s).GetProperty("CanStart").GetBoolean(), 5000));
            Require(!ProcessExists(canceled.GetProperty("LastWorkerPid").GetInt32()), "Stop during startup leaked the worker.");

            await client.Command("tracking-start-ignore-stop");
            var ready = Tracking(await Until(client, s => State(s) == "Ready", 12000));
            int pid = ready.GetProperty("LastWorkerPid").GetInt32();
            await FocusTrackingAsync(client, handle);
            AccessKey(0x54);
            var stopping = Tracking(await Until(client, s => State(s) == "Stopping", 1500));
            Require(!stopping.GetProperty("CanStart").GetBoolean() && ProcessExists(pid),
                "Restart became available while the old worker was still alive.");
            AccessKey(0x52);
            var forced = Tracking(await Until(client, s => State(s) == "Stopped" &&
                Tracking(s).GetProperty("CanStart").GetBoolean(), 6000));
            Require(forced.GetProperty("ForcedStop").GetBoolean() && !ProcessExists(pid) &&
                forced.GetProperty("LastWorkerPid").GetInt32() == pid, "Hung worker was not contained or Restart escaped the hold.");
            await client.Command("tracking-close");
            return new { Outcomes = outcomes, CanceledStartup = true, ForcedTermination = true };
        });
    }

    private static async Task RunCalibration(Client client)
    {
        await Measure("F07_CALIBRATION_PREVIEW", "RECORDED_INFERENCE + UI_INTEGRATION",
            "Invey opens a calibrated region, mirrors a real landmark and stops on Cancel", async () =>
        {
            await client.Command("tracking-close");
            var opened = await client.Command("tracking-open");
            long handle = opened.GetProperty("TrackingHandle").GetInt64();
            Require(Tracking(opened).GetProperty("Title").GetString() == "Invey · Tracking lab", "Incorrect tracking window title.");
            await FocusTrackingAsync(client, handle);
            AccessKey(0x45);
            var active = await Until(client, s => State(s) == "Observing", 12000);
            int pid = Tracking(active).GetProperty("LastWorkerPid").GetInt32();
            AccessKey(0x43); // Calibrate
            await Until(client, s => Tracking(s).GetProperty("Calibration").GetProperty("State").GetString() == "Ready", 9000);
            AccessKey(0x4D); // Mirror preview
            var mirrored = Tracking(await Until(client, s =>
            {
                var cal = Tracking(s).GetProperty("Calibration");
                return cal.GetProperty("Mirrored").GetBoolean() && cal.GetProperty("TipX").ValueKind == JsonValueKind.Number;
            }, 6000));
            double x = mirrored.GetProperty("IndexX").GetDouble();
            double mapped = mirrored.GetProperty("Calibration").GetProperty("TipX").GetDouble();
            Require(Math.Abs(mapped - (1 - x)) < 1e-8, "Mirror did not reflect the fresh normalized landmark.");
            AccessKey(0x42); // Cancel calibration
            var stopped = Tracking(await Until(client, s => State(s) == "Stopped" &&
                !Tracking(s).GetProperty("Calibration").GetProperty("Open").GetBoolean(), 5000));
            Require(!ProcessExists(pid) && stopped.GetProperty("IndexX").ValueKind == JsonValueKind.Null,
                "Calibration Cancel left the worker or hand active.");
            await client.Command("tracking-close");
            return new { Title = "Invey · Tracking lab", PreviewReady = true, Mirrored = true, WorkerExited = true };
        });
    }

    private static async Task RunNativeTrackingFixture(Client client)
    {
        const string nativeCase = "F06_NATIVE_READER_VIDEO_FIXTURE";
        const string nativeEvidence = "NATIVE_MEDIA_FOUNDATION + PRERECORDED_INFERENCE + UI_INTEGRATION";
        if (!File.Exists("artifacts/execution-08/controlled.mp4"))
            Cases.Add(new(nativeCase, nativeEvidence, "Prepared local MP4 fixture", new
            {
                Reason = "Generate the controlled MP4 using tests/tracking/prepare_native_fixture.py."
            }, "BLOCKED", 0));
        else await Measure(nativeCase, nativeEvidence,
            "An explicitly selected local video uses the real Windows reader, inference, Stop and new worker", async () =>
        {
            await client.Command("tracking-close");
            var opened = await client.Command("tracking-open-native-video");
            long handle = opened.GetProperty("TrackingHandle").GetInt64();
            await SelectCameraFixture(client, handle);
            var clock = Stopwatch.StartNew();
            AccessKey(0x45);
            var hand = Tracking(await Until(client, s => State(s) == "Observing", 12000));
            double firstHandMs = clock.Elapsed.TotalMilliseconds;
            int pid = hand.GetProperty("LastWorkerPid").GetInt32();
            string? session = hand.GetProperty("SessionId").GetString();
            Require(hand.GetProperty("Source").GetString() == "Recorded" &&
                !hand.GetProperty("SourceChoicesEnabled").GetBoolean(), "Native video fixture was labeled as a live device or source switching escaped ownership.");
            var lost = Tracking(await Until(client, s => State(s) == "Lost" &&
                Tracking(s).GetProperty("AcceptedObservations").GetInt64() >= 23, 5000));
            Require(lost.GetProperty("Sequence").ValueKind == JsonValueKind.Null, "File loss retained a hand.");
            var returned = Tracking(await Until(client, s => State(s) == "Observing" &&
                Tracking(s).GetProperty("AcceptedObservations").GetInt64() >= 34, 5000));
            Require(returned.GetProperty("SessionId").GetString() == session, "Source loss replaced the session.");

            clock.Restart();
            AccessKey(0x54);
            var stopped = Tracking(await Until(client, s => State(s) == "Stopped", 6000));
            double stopMs = clock.Elapsed.TotalMilliseconds;
            Require(stopped.GetProperty("Sequence").ValueKind == JsonValueKind.Null && !ProcessExists(pid),
                "Stopped native reader still owns its old process or hand.");
            clock.Restart();
            AccessKey(0x52);
            var next = Tracking(await Until(client, s => State(s) == "Observing", 12000));
            double restartMs = clock.Elapsed.TotalMilliseconds;
            int freshPid = next.GetProperty("LastWorkerPid").GetInt32();
            Require(freshPid != pid && next.GetProperty("SessionId").GetString() != session, "Reader restart reused its generation.");
            AccessKey(0x73);
            await Until(client, s => s.GetProperty("TrackingHandle").GetInt64() == 0, 6000);
            Require(!ProcessExists(freshPid) && !Input.Exists(new nint(handle)), "Closing the lab leaked its native reader process or HWND.");
            return new { CameraFrames = 0, FirstHandMs = firstHandMs, StopMs = stopMs, RestartMs = restartMs,
                OldWorkerExited = true, FreshWorkerExited = true };
        });

        await Measure("F06_CAMERA_PERMISSION_FEEDBACK", "UI_INTEGRATION + CONTROLLED_FAILURE_INJECTION",
            "Live selection remains off until Enable, then permission failure is visible and the worker exits", async () =>
        {
            await client.Command("tracking-close");
            var opened = await client.Command("tracking-open-camera-denied");
            await SelectCameraFixture(client, opened.GetProperty("TrackingHandle").GetInt64());
            AccessKey(0x45);
            var failed = Tracking(await Until(client, s => State(s) == "Unavailable" &&
                Tracking(s).GetProperty("CanStart").GetBoolean(), 12000));
            Require(failed.GetProperty("Failure").GetString() == "PermissionDenied" &&
                failed.GetProperty("Status").GetString()!.Contains("Privacy & security") &&
                failed.GetProperty("SourceStatus").GetString() == "Live camera · Camera off" &&
                !ProcessExists(failed.GetProperty("LastWorkerPid").GetInt32()), "Permission failure left incorrect state or live worker.");
            await client.Command("tracking-close");
            return new { PermissionFailureVisible = true, WorkerExited = true, CameraFrames = 0 };
        });
    }

    private static async Task SelectCameraFixture(Client client, long handle)
    {
        await FocusTrackingAsync(client, handle);
        AccessKey(0x4C); // Alt+L, Live camera or explicitly labeled local video fixture.
        AccessKey(0x46);
        await Until(client, s => Tracking(s).GetProperty("CameraCount").GetInt32() == 1);
        AccessKey(0x44);
        Input.Key(0x28, false);
        Input.Key(0x28, true);
        var selected = Tracking(await Until(client, s => Tracking(s).GetProperty("DeviceSelected").GetBoolean()));
        Require(selected.GetProperty("State").GetString() == "Off" &&
            selected.GetProperty("LastWorkerPid").ValueKind == JsonValueKind.Null &&
            selected.GetProperty("SourceStatus").GetString()!.EndsWith("Camera off"),
            "Choosing a source did not leave tracking off.");
    }

    private static double Median(IEnumerable<double> values)
    {
        var sorted = values.Order().ToArray();
        return sorted[sorted.Length / 2];
    }

    private static async Task FocusTrackingAsync(Client client, long handle)
    {
        Input.Focus(new nint(handle));
        await Until(client, s => s.GetProperty("Native").GetProperty("Foreground").GetInt64() == handle);
    }

    private static void AccessKey(ushort key)
    {
        Input.Key(0x12, false);
        try { Input.Key(key, false); Input.Key(key, true); }
        finally { Input.Key(0x12, true); }
    }

    private static async Task ExitAndVerifyAsync(Client client, Process host, long overlay, long fixture)
    {
        int? trackingPid = null;
        long trackingWindow = 0;
        if (Cases.Any(c => c.Id == "F06_RECORDED_WPF_LIFECYCLE" && c.Status == "PASS"))
        {
            var opened = await client.Command("tracking-open");
            trackingWindow = opened.GetProperty("TrackingHandle").GetInt64();
            await FocusTrackingAsync(client, trackingWindow);
            AccessKey(0x45);
            var active = Tracking(await Until(client, s => State(s) == "Observing", 12000));
            trackingPid = active.GetProperty("LastWorkerPid").GetInt32();
        }
        await client.Command("exit");
        await host.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        Require(host.ExitCode == 0 && !Input.Exists(new nint(overlay)) && !Input.Exists(new nint(fixture)), "Host cleanup failed.");
        if (trackingPid is { } pid)
            Require(!ProcessExists(pid) && !Input.Exists(new nint(trackingWindow)), "Host exit leaked tracking worker or HWND.");
        Cases.Add(new("EXIT_CLEANUP", "NATIVE_SOFTWARE_TEST", "Host exits and owned windows and active workers vanish",
            new { host.ExitCode, OverlayGone = true, FixtureGone = true, TrackingWorkerExited = trackingPid.HasValue }, "PASS", 0));
    }
}
