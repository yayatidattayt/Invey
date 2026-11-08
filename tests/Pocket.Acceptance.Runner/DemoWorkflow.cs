using System.Diagnostics;
using System.Text.Json;

namespace Pocket.Acceptance.Runner;

internal static partial class Program
{
    private static JsonElement DemoTracking(JsonElement s) => Product(s).GetProperty("Tracking");
    private static async Task RunDemoWorkflow(string app, string output, Process? initialHost = null, Client? initialClient = null)
    {
        await Measure("D17_COMPLETE_DEMO_ROUTE", "NATIVE_CONTROLS + REAL_CAPTURE + RECORDED_MODEL + EXPLICIT_SIMULATION",
            "Main navigation, actual approved capture, pinned card, two real Workspace references, recorded tracking, practice, settings and process restart", async () =>
        {
            var (host, client) = initialHost is not null && initialClient is not null
                ? (initialHost, initialClient) : StartAcceptanceHost(app, output);
            var clock = Stopwatch.StartNew();
            double startupMs = 0;
            int workerPid = 0;
            using (host)
            using (client)
            try
            {
                if (initialClient is null) await client.Connect();
                var initial = await client.Command("demo-start");
                startupMs = clock.Elapsed.TotalMilliseconds;
                Require(Product(initial).GetProperty("DemoProfile").GetBoolean() && CameraOff(initial), "Demo opened wrong profile or camera.");
                long main = ProductHandle(initial);
                var ready = await Until(client, s => Product(s).GetProperty("PocketButtonPoint").ValueKind == JsonValueKind.Object);
                await FocusTrackingAsync(client, main); // Wait for Loaded before requesting native foreground ownership.
                var pocketButton = Product(ready).GetProperty("PocketButtonPoint");
                Input.Click(pocketButton.GetProperty("X").GetDouble(), pocketButton.GetProperty("Y").GetDouble()); // Actual main-window Pocket button.
                var shelf = await Until(client, s => ShelfVisible(s) && ProductPocket(s).GetProperty("Count").GetInt32() == 2);
                long shelfHandle = ProductPocket(shelf).GetProperty("Handle").GetInt64();
                await FocusTrackingAsync(client, shelfHandle);
                AccessKey(0x43); // Capture region.
                var capture = await Until(client, s => ProductPocket(s).GetProperty("CaptureHandle").GetInt64() != 0, 6000);
                var frozen = ProductPocket(capture).GetProperty("Capture");
                Require(frozen.GetProperty("FrozenPattern").GetProperty("Left").GetBoolean(), "Actual capture did not freeze synthetic fixture pixels.");
                await FocusTrackingAsync(client, ProductPocket(capture).GetProperty("CaptureHandle").GetInt64());
                var monitor = capture.GetProperty("Monitor");
                int x = monitor.GetProperty("Left").GetInt32(), y = monitor.GetProperty("Top").GetInt32();
                int w = monitor.GetProperty("Width").GetInt32(), h = monitor.GetProperty("Height").GetInt32();
                double offset = (TestProfile.Seed % 7) * .002;
                Input.Move(x + w * (.10 + offset), y + h * .22);
                var start = Input.Cursor();
                Input.Down();
                try { Input.Move(x + w * (.22 + offset), y + h * .34); }
                finally { Input.Up(); }
                var end = Input.Cursor();
                AccessKey(0x50); // Preview actual crop.
                var preview = await Until(client, s => ProductPocket(s).GetProperty("Capture").GetProperty("Preview").GetBoolean());
                var region = ProductPocket(preview).GetProperty("Capture").GetProperty("Region");
                Require(region.GetProperty("Width").GetInt32() == end.X - start.X &&
                    region.GetProperty("Height").GetInt32() == end.Y - start.Y &&
                    ProductPocket(preview).GetProperty("Capture").GetProperty("SampleMatchesFixture").GetBoolean(),
                    "Approved demo preview did not match its actual fixture region.");
                AccessKey(0x53); // Save.
                var saved = await Until(client, s => !ProductPocket(s).GetProperty("CaptureInProgress").GetBoolean() &&
                    ProductPocket(s).GetProperty("Count").GetInt32() == 3, 6000);
                await FocusTrackingAsync(client, shelfHandle);
                AccessKey(0x46); Press(0x24); // Saved references, Home.
                AccessKey(0x4F); // Open reference.
                var card = await Until(client, s => ProductPocket(s).GetProperty("ViewerHandle").GetInt64() != 0);
                long cardHandle = ProductPocket(card).GetProperty("ViewerHandle").GetInt64();
                await FocusTrackingAsync(client, cardHandle);
                AccessKey(0x50); // Pin actual card.
                await Until(client, s => ProductPocket(s).GetProperty("Pinned").GetBoolean());
                await FocusTrackingAsync(client, shelfHandle);
                AccessKey(0x54); // Tuck shelf.
                await Until(client, s => !ShelfVisible(s));
                Require(IsWindowVisible(new nint(cardHandle)), "Tuck hid the pinned reference.");

                await FocusTrackingAsync(client, main);
                AccessKey(0x57); // Workspace.
                var board = await Until(client, s => Board(s).ValueKind == JsonValueKind.Object && Board(s).GetProperty("References").GetInt32() == 3);
                long boardHandle = Board(board).GetProperty("Handle").GetInt64();
                await FocusTrackingAsync(client, boardHandle);
                AccessKey(0x41);
                await Until(client, s => Board(s).GetProperty("Cards").GetArrayLength() == 1);
                AccessKey(0x52); Press(0x23); // Select last real saved image.
                AccessKey(0x42);
                var loaded = await Until(client, s => Board(s).GetProperty("Cards").GetArrayLength() == 2);
                Require(Slot(loaded, "A").GetProperty("ImageLoaded").GetBoolean() &&
                    Slot(loaded, "B").GetProperty("ImageLoaded").GetBoolean(), "Workspace did not load both PNGs.");
                double widthBefore = Slot(loaded, "B").GetProperty("Width").GetDouble();
                var larger = Board(loaded).GetProperty("Larger");
                Input.Click(larger.GetProperty("X").GetDouble(), larger.GetProperty("Y").GetDouble());
                await Until(client, s => Slot(s, "B").GetProperty("Width").GetDouble() > widthBefore);
                var beforeDemo = await client.Command("status");
                double beforeX = Slot(beforeDemo, "B").GetProperty("X").GetDouble();
                AccessKey(0x47); // Labeled simulated Ghost button.
                await Until(client, s => Claim(s, "Ghost") == "B");
                await Until(client, s => Slot(s, "B").GetProperty("X").GetDouble() > beforeX);
                await Until(client, s => Claim(s, "Ghost") is null, 4000);

                await FocusTrackingAsync(client, main);
                AccessKey(0x54); // Ghost Tracking.
                var lab = await Until(client, s => Product(s).GetProperty("TrackingHandle").GetInt64() != 0);
                long labHandle = Product(lab).GetProperty("TrackingHandle").GetInt64();
                await FocusTrackingAsync(client, labHandle);
                Require(DemoTracking(lab).GetProperty("State").GetString() == "Off", "Opening Ghost started capture.");
                AccessKey(0x53); // Actual Show image checkbox.
                AccessKey(0x45); // Recorded model sequence.
                var observed = await Until(client, s => DemoTracking(s).GetProperty("State").GetString() == "Observing" &&
                    DemoTracking(s).GetProperty("VideoPreview").GetProperty("FrameVisible").GetBoolean(), 12000);
                Require(DemoTracking(observed).GetProperty("Source").GetString() == "Recorded", "Demo misidentified the source.");
                workerPid = DemoTracking(observed).GetProperty("LastWorkerPid").GetInt32();
                AccessKey(0x54);
                await Until(client, s => DemoTracking(s).GetProperty("State").GetString() == "Stopped");
                Require(!ProcessExists(workerPid), "Recorded Stop leaked worker.");
                AccessKey(0x51); Press(0x23); // Explicit simulated-practice choice.
                await Until(client, s => DemoTracking(s).GetProperty("SourceStatus").GetString()!.Contains("SIMULATED"));
                AccessKey(0x45);
                var simulated = await Until(client, s => DemoTracking(s).GetProperty("State").GetString() == "Observing", 8000);
                workerPid = DemoTracking(simulated).GetProperty("LastWorkerPid").GetInt32();
                AccessKey(0x43); // Calibrate.
                await Until(client, s => DemoTracking(s).GetProperty("Calibration").GetProperty("State").GetString() == "Ready", 5000);
                await Until(client, s => DemoTracking(s).GetProperty("Calibration").GetProperty("Practice").GetString() == "Held", 6000);
                AccessKey(0x54); // Stop while calibration is open.
                await Until(client, s => DemoTracking(s).GetProperty("State").GetString() == "Stopped");
                Require(!ProcessExists(workerPid), "Simulated practice Stop leaked worker.");
                await FocusTrackingAsync(client, main);
                AccessKey(0x53);
                var settings = await Until(client, s => Product(s).GetProperty("SettingsVisible").GetBoolean());
                await FocusTrackingAsync(client, Product(settings).GetProperty("SettingsHandle").GetInt64());
                Input.Key(0x12, true); Press(0x1B); // Settings has a Close button; use window close below.
                Input.Key(0x12, false); try { Press(0x73); } finally { Input.Key(0x12, true); }
                await FocusTrackingAsync(client, main);
                AccessKey(0x50);
                await Until(client, s => ShelfVisible(s) && ProductPocket(s).GetProperty("Count").GetInt32() == 3);
                await FocusTrackingAsync(client, main);
                AccessKey(0x58); // Actual Exit.
                await host.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Require(host.ExitCode == 0 && !ProcessExists(workerPid) && !Input.Exists(new nint(cardHandle)), "First demo process did not cleanly exit.");
            }
            finally { FinishProductHost(host); }

            var (restarted, second) = StartAcceptanceHost(app, output);
            using (restarted)
            using (second)
            try
            {
                await second.Connect();
                var opened = await second.Command("demo-start");
                long main = ProductHandle(opened);
                await FocusTrackingAsync(second, main);
                AccessKey(0x50);
                var restored = await Until(second, s => ShelfVisible(s) && ProductPocket(s).GetProperty("Count").GetInt32() == 3 &&
                    ProductPocket(s).GetProperty("Cards").GetInt32() > 0 && ProductPocket(s).GetProperty("Pinned").GetBoolean(), 6000);
                Require(CameraOff(restored), "Restart activated tracking.");
                await FocusTrackingAsync(second, main);
                AccessKey(0x58);
                await restarted.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Require(restarted.ExitCode == 0, "Restarted demo did not exit.");
            }
            finally { FinishProductHost(restarted); }
            return new { TestProfile.Seed, StartupThroughFixtureMs = startupMs, ActualCaptureApproved = true,
                TwoSyntheticSamples = true, PinnedCardVisibleWhileTucked = true, WorkspaceImagesLoaded = true,
                SimulatedGhostMovedB = true, RecordedModelObserved = true, SimulatedPracticeHeld = true,
                SettingsOpened = true, PersistedAcrossProcessRestart = true, CameraOpened = false, WorkerExited = true };
        });
    }
}
