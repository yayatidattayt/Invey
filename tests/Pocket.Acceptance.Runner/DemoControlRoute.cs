using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Pocket.Acceptance.Runner;

internal static partial class Program
{
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint ReadDemoWindowStyle(nint window, int index);
    private static bool ActualPinned(long window) => IsWindowVisible(new nint(window)) &&
        (ReadDemoWindowStyle(new nint(window), -20).ToInt64() & 8) != 0;

    private static async Task RunDemoControlRoute(string app, string output, Process host, Client client)
    {
        await Measure("D17_WPF_CONTROL_ROUTE", "ACTUAL_WPF_CONTROLS + GDI_CAPTURE; NO_NATIVE_INPUT",
            "Supplemental control-route verification; native remote rehearsal remains a separate requirement", async () =>
        {
            var timer = Stopwatch.StartNew();
            double startup;
            int trackingPid = 0;
            using (host)
            using (client)
            try
            {
                await client.Command("demo-start");
                await Until(client, s => Product(s).GetProperty("WindowLoaded").GetBoolean());
                startup = timer.Elapsed.TotalMilliseconds;
                await client.Command("demo-ui|main|Pocket");
                await Until(client, s => ShelfVisible(s) && ProductPocket(s).GetProperty("Count").GetInt32() == 2);
                await client.Command("demo-ui|pocket|Capture region");
                var capture = await Until(client, s => ProductPocket(s).GetProperty("CaptureHandle").GetInt64() != 0, 6000);
                Require(ProductPocket(capture).GetProperty("Capture").GetProperty("FrozenPattern").GetProperty("Left").GetBoolean(), "GDI fixture capture did not match.");
                await client.Command("demo-ui|capture|Keyboard region");
                await Until(client, s => ProductPocket(s).GetProperty("Capture").GetProperty("Region").ValueKind == JsonValueKind.Object &&
                    ProductPocket(s).GetProperty("Capture").GetProperty("Region").GetProperty("Width").GetInt32() > 0);
                await client.Command("demo-ui|capture|Preview");
                var preview = await Until(client, s => ProductPocket(s).GetProperty("Capture").GetProperty("Preview").GetBoolean());
                Require(ProductPocket(preview).GetProperty("Capture").GetProperty("SaveEnabled").GetBoolean(), "Actual keyboard crop cannot be saved.");
                await client.Command("demo-ui|capture|Save to Pocket");
                await Until(client, s => !ProductPocket(s).GetProperty("CaptureInProgress").GetBoolean() && ProductPocket(s).GetProperty("Count").GetInt32() == 3, 6000);
                await client.Command("demo-ui|pocket|Open reference");
                await Until(client, s => ProductPocket(s).GetProperty("ViewerHandle").GetInt64() != 0);
                await client.Command("demo-toggle|card|Pin above other windows");
                var pinned = await Until(client, s => ProductPocket(s).GetProperty("Pinned").GetBoolean());
                long card = ProductPocket(pinned).GetProperty("ViewerHandle").GetInt64();
                await client.Command("demo-toggle|card|Pin above other windows");
                await Until(client, s => !ProductPocket(s).GetProperty("Pinned").GetBoolean());
                await client.Command("demo-toggle|card|Pin above other windows");
                await Until(client, s => ProductPocket(s).GetProperty("Pinned").GetBoolean());
                await client.Command("demo-ui|pocket|Tuck shelf");
                await Until(client, s => !ShelfVisible(s));
                Require(ActualPinned(card), "Tuck did not preserve the actual visible WS_EX_TOPMOST card window.");

                await client.Command("demo-ui|main|Workspace");
                await Until(client, s => Board(s).ValueKind == JsonValueKind.Object && Board(s).GetProperty("References").GetInt32() == 3);
                await client.Command("demo-ui|workspace|Load A");
                await Until(client, s => Board(s).GetProperty("Cards").GetArrayLength() == 1);
                await client.Command("demo-select|workspace|" + (1 + Math.Abs(TestProfile.Seed % 2)));
                await client.Command("demo-ui|workspace|Load B");
                var two = await Until(client, s => Board(s).GetProperty("Cards").GetArrayLength() == 2);
                Require(Slot(two, "A").GetProperty("ImageLoaded").GetBoolean() && Slot(two, "B").GetProperty("ImageLoaded").GetBoolean(), "Workspace did not decode actual references.");
                double width = Slot(two, "B").GetProperty("Width").GetDouble();
                await client.Command("demo-ui|workspace|Larger");
                await Until(client, s => Slot(s, "B").GetProperty("Width").GetDouble() > width);
                var positions = await client.Command("status");
                double startA = Slot(positions, "A").GetProperty("X").GetDouble();
                double startB = Slot(positions, "B").GetProperty("X").GetDouble();
                await client.Command("demo-ui|workspace|Move right");
                await Until(client, s => Slot(s, "B").GetProperty("X").GetDouble() > startB &&
                    Slot(s, "A").GetProperty("X").GetDouble() == startA);
                await client.Command("demo-ui|workspace|Move left");
                await Until(client, s => Slot(s, "B").GetProperty("X").GetDouble() == startB);
                await client.Command("demo-ui|workspace|100% zoom");
                await client.Command("demo-ui|workspace|Fit image");
                await client.Command("demo-ui|workspace|Simulated Ghost demo");
                await Until(client, s => Claim(s, "Ghost") == "B" && Slot(s, "B").GetProperty("X").GetDouble() > startB);
                await Until(client, s => Claim(s, "Ghost") is null, 4000);
                await client.Command("demo-ui|workspace|Simulated Ghost demo");
                await Until(client, s => Claim(s, "Ghost") == "B");
                await client.Command("demo-ui|main|Pause Ghost");
                await Until(client, s => Claim(s, "Ghost") is null);
                await Task.Delay(300);
                Require(Claim(await client.Command("status"), "Ghost") is null, "Pause left a late simulation callback armed.");

                await client.Command("demo-ui|main|Ghost Tracking");
                var lab = await Until(client, s => Product(s).GetProperty("Tracking").ValueKind == JsonValueKind.Object);
                Require(DemoTracking(lab).GetProperty("State").GetString() == "Off", "Tracking opened a source automatically.");
                await client.Command("demo-toggle|tracking|Show local camera view and finger landmarks");
                await client.Command("demo-ui|tracking|Enable recorded tracking");
                var hand = await Until(client, s => DemoTracking(s).GetProperty("State").GetString() == "Observing" &&
                    DemoTracking(s).GetProperty("VideoPreview").GetProperty("FrameVisible").GetBoolean(), 12000);
                trackingPid = DemoTracking(hand).GetProperty("LastWorkerPid").GetInt32();
                await client.Command("demo-ui|tracking|Stop");
                await Until(client, s => DemoTracking(s).GetProperty("State").GetString() == "Stopped");
                Require(!ProcessExists(trackingPid), "Recorded worker did not exit.");
                await client.Command("demo-select|tracking|1");
                await Until(client, s => DemoTracking(s).GetProperty("SourceStatus").GetString()!.Contains("SIMULATED"));
                await client.Command("demo-ui|tracking|Enable simulated practice");
                var practice = await Until(client, s => DemoTracking(s).GetProperty("State").GetString() == "Observing", 8000);
                trackingPid = DemoTracking(practice).GetProperty("LastWorkerPid").GetInt32();
                await client.Command("demo-ui|tracking|Calibrate");
                await Until(client, s => DemoTracking(s).GetProperty("Calibration").GetProperty("State").GetString() == "Ready", 6000);
                await Until(client, s => DemoTracking(s).GetProperty("Calibration").GetProperty("Practice").GetString() == "Held", 6000);
                await client.Command("demo-ui|tracking|Stop");
                await Until(client, s => DemoTracking(s).GetProperty("State").GetString() == "Stopped");
                Require(!ProcessExists(trackingPid), "Practice worker did not exit.");
                await client.Command("demo-ui|main|Settings");
                await Until(client, s => Product(s).GetProperty("SettingsVisible").GetBoolean());
                await client.Command("demo-ui|settings|Close");
                await Until(client, s => !Product(s).GetProperty("SettingsVisible").GetBoolean());
                await client.Command("demo-ui|main|Pocket");
                await Until(client, s => ShelfVisible(s));
                await client.Command("demo-ui|main|Exit");
                await host.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Require(host.ExitCode == 0 && !ProcessExists(trackingPid) && !Input.Exists(new nint(card)), "Demo exit leaked resources.");
            }
            catch (Exception error)
            {
                await File.WriteAllTextAsync(Path.Combine(output, "demo-route-error.log"),
                    $"Exited={host.HasExited}; ExitCode={(host.HasExited ? host.ExitCode : -999)}; " + error);
                throw;
            }
            finally { FinishProductHost(host); }
            var (restart, second) = StartAcceptanceHost(app, output);
            using (restart)
            using (second)
            try
            {
                await second.Connect();
                await second.Command("demo-start");
                await Until(second, s => Product(s).GetProperty("WindowLoaded").GetBoolean());
                await second.Command("demo-ui|main|Pocket");
                var restored = await Until(second, s => ShelfVisible(s) && ProductPocket(s).GetProperty("Count").GetInt32() == 3 &&
                    ProductPocket(s).GetProperty("Cards").GetInt32() > 0 && ProductPocket(s).GetProperty("Pinned").GetBoolean(), 6000);
                Require(CameraOff(restored), "Restart opened camera.");
                Require(ActualPinned(ProductPocket(restored).GetProperty("ViewerHandle").GetInt64()), "Pinned native card style was not restored.");
                await second.Command("demo-ui|main|Exit");
                await restart.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Require(restart.ExitCode == 0, "Second process did not exit cleanly.");
            }
            finally { FinishProductHost(restart); }
            return new { TestProfile.Seed, StartupThroughFixtureMs = startup, ActualControls = true,
                NativeInput = false, ActualGdiCapture = true, SamplesAndCapturePersisted = true,
                PinnedCardSurvivedTuck = true, RealWorkspaceImages = true, SimulationMovedB = true,
                RecordedModelObserved = true, SimulatedPracticeHeld = true, ProcessRestart = true,
                WorkerExitConfirmed = true, CameraOpened = false, NativeRehearsal = "MANUAL VERIFICATION REQUIRED" };
        });
    }
}
