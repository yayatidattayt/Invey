using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Pocket.Acceptance.Runner;

internal static partial class Program
{
    private static JsonElement Pocket(JsonElement state) => state.GetProperty("Pocket");
    private static string PocketText(JsonElement state) => Pocket(state).GetProperty("Text").GetString() ?? "";
    private static long Selector(JsonElement state) => Pocket(state).GetProperty("CaptureHandle").GetInt64();
    private static long Viewer(JsonElement state) => Pocket(state).GetProperty("ViewerHandle").GetInt64();

    private static async Task<JsonElement> BeginPocketCapture(Client client)
    {
        AccessKey(0x43);
        var active = await Until(client, state => Selector(state) != 0 ||
            PocketText(state).StartsWith("Capture unavailable.", StringComparison.Ordinal), 5000);
        Require(Selector(active) != 0, "Capture did not open: " + PocketText(active));
        await Until(client, state => state.GetProperty("Native").GetProperty("Foreground").GetInt64() == Selector(active));
        return active;
    }

    private static async Task RunPocketCapture(Client client, string output)
    {
        await Measure("C01_C04_POCKET_CAPTURE", "NATIVE_GDI_FIXTURE + WPF_INTEGRATION",
            "Frozen selection captures known desktop pixels, cancels without files, rejects black and saves only the crop", async () =>
        {
            await client.Command("show"); // The capture must hide the passive overlay before taking pixels.
            var opened = await client.Command("pocket-open");
            long workspace = Pocket(opened).GetProperty("Handle").GetInt64();
            await FocusTrackingAsync(client, workspace);
            await Until(client, s => PocketText(s).StartsWith("Your library is empty", StringComparison.Ordinal));
            string store = Path.Combine(output, "pocket-test");
            var active = await BeginPocketCapture(client);
            var pattern = Pocket(active).GetProperty("Capture").GetProperty("FrozenPattern");
            Require(pattern.GetProperty("Left").GetBoolean() && pattern.GetProperty("Center").GetBoolean() &&
                pattern.GetProperty("Right").GetBoolean(), "Frozen screen did not match fixture stripes: " + pattern.GetRawText());
            Require(!active.GetProperty("OverlayVisible").GetBoolean(), "Overlay was still visible during capture.");
            var monitor = active.GetProperty("Monitor");
            int left = monitor.GetProperty("Left").GetInt32(), top = monitor.GetProperty("Top").GetInt32();
            int width = monitor.GetProperty("Width").GetInt32(), height = monitor.GetProperty("Height").GetInt32();
            void Draw(double x, double y, double right, double bottom)
            {
                Input.Move(left + width * x, top + height * y);
                Input.Down();
                try { Input.Move(left + width * right, top + height * bottom); }
                finally { Input.Up(); }
            }
            Input.Click(left + width * .5, top + height * .25);
            AccessKey(0x50); // Preview is disabled for a zero-area selection.
            Require(!Pocket(await client.Command("status")).GetProperty("Capture").GetProperty("Preview").GetBoolean(),
                "Zero-area selection showed a preview.");
            Draw(.42, .22, .53, .34);
            AccessKey(0x50);
            await Until(client, s =>
            {
                Require(Selector(s) != 0, "Selector closed while checking black content: " + PocketText(s));
                return Pocket(s).GetProperty("Capture").GetProperty("Status").GetString()!.Contains("blank or unavailable");
            });
            AccessKey(0x4B); // Keyboard region
            AccessKey(0x50); // Preview
            await Until(client, s => Pocket(s).GetProperty("Capture").GetProperty("Preview").GetBoolean());
            Input.Key(0x1B, false); Input.Key(0x1B, true);
            await Until(client, s => Selector(s) == 0 && PocketText(s) == "Capture closed.");
            Require(!Directory.Exists(store), "Cancel wrote Pocket data.");
            await Until(client, s => s.GetProperty("Native").GetProperty("Foreground").GetInt64() == workspace);

            active = await BeginPocketCapture(client);
            Input.Move(left + width * .10, top + height * .22);
            var start = Input.Cursor();
            Input.Down();
            try { Input.Move(left + width * .22, top + height * .34); }
            finally { Input.Up(); }
            var end = Input.Cursor();
            AccessKey(0x50);
            var preview = await Until(client, s => Pocket(s).GetProperty("Capture").GetProperty("Preview").GetBoolean());
            var region = Pocket(preview).GetProperty("Capture").GetProperty("Region");
            int cropWidth = end.X - start.X, cropHeight = end.Y - start.Y;
            Require(region.GetProperty("X").GetInt32() == start.X - left &&
                region.GetProperty("Y").GetInt32() == start.Y - top &&
                region.GetProperty("Width").GetInt32() == cropWidth && region.GetProperty("Height").GetInt32() == cropHeight,
                "Crop did not match the independently sampled cursor endpoints: " + region.GetRawText());
            Require(Pocket(preview).GetProperty("Capture").GetProperty("SampleMatchesFixture").GetBoolean(),
                "The preview did not match the owned fixture. Refusing to save it.");
            AccessKey(0x53); // Save
            await Until(client, s => Selector(s) == 0 && Pocket(s).GetProperty("Count").GetInt32() == 1 && PocketText(s) == "Reference saved locally.", 6000);
            Require(Directory.GetFiles(Path.Combine(store, "assets"), "*.png").Length == 1,
                "Save did not produce exactly one cropped asset.");
            AccessKey(0x4F); // Open reference
            var viewed = Pocket(await Until(client, s => Viewer(s) != 0));
            Require(viewed.GetProperty("Width").GetInt32() == cropWidth && viewed.GetProperty("Height").GetInt32() == cropHeight &&
                viewed.GetProperty("SampleMatchesFixture").GetBoolean(), "Saved reference has incorrect dimensions or fixture pixels.");
            AccessKey(0x48); // Hide reference
            await Until(client, s => Viewer(s) == 0);
            await FocusTrackingAsync(client, workspace);
            await BeginPocketCapture(client);
            Draw(.10, .22, .22, .34);
            AccessKey(0x50);
            var retry = await Until(client, s => Pocket(s).GetProperty("Capture").GetProperty("Preview").GetBoolean());
            Require(Pocket(retry).GetProperty("Capture").GetProperty("SampleMatchesFixture").GetBoolean(),
                "The storage failure fixture did not match known pixels.");
            string manifest = Path.Combine(store, "manifest.json");
            var originalManifest = File.ReadAllBytes(manifest);
            using (File.Open(manifest, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                AccessKey(0x53);
                await Until(client, s => Pocket(s).GetProperty("Capture").GetProperty("Status").GetString()!
                    .StartsWith("Could not save the reference.", StringComparison.Ordinal), 6000);
            }
            Require(originalManifest.SequenceEqual(File.ReadAllBytes(manifest)) &&
                Directory.GetFiles(Path.Combine(store, "assets"), "*.png").Length == 1,
                "The failed save changed the existing library or wrote a second asset.");
            Input.Key(0x1B, false); Input.Key(0x1B, true);
            var restored = await Until(client, s => Selector(s) == 0 &&
                s.GetProperty("Native").GetProperty("Foreground").GetInt64() == workspace);
            Require(restored.GetProperty("OverlayVisible").GetBoolean(), "Cancel after a save failure did not restore the overlay.");
            var closed = await client.Command("pocket-close");
            Require(Pocket(closed).ValueKind == JsonValueKind.Null &&
                closed.GetProperty("Tracking").ValueKind == JsonValueKind.Null, "Pocket was not closed or started tracking.");
            return new { Dpi = active.GetProperty("Dpi").GetInt32(), CropWidth = cropWidth, CropHeight = cropHeight,
                ExpectedPixels = true, CancelWroteNothing = true, BlackRejected = true, CameraOff = true,
                LockedLibraryPreserved = true, RestoredAfterFailure = true };
        });
    }

    private static async Task RunPocketRestart(string app, string output)
    {
        if (!Cases.Any(c => c.Id == "C01_C04_POCKET_CAPTURE" && c.Status == "PASS")) return;
        await Measure("C05_C11_POCKET_RESTART", "PROCESS_RESTART + WPF_INTEGRATION",
            "A fresh process reopens the saved image and exits safely during a new unsaved capture", async () =>
        {
            var (host, client) = StartAcceptanceHost(app, output);
            using (host)
            using (client)
            try
            {
                await client.Connect();
                var opened = await client.Command("pocket-open");
                long workspace = Pocket(opened).GetProperty("Handle").GetInt64();
                await FocusTrackingAsync(client, workspace);
                await Until(client, s => Pocket(s).GetProperty("Count").GetInt32() == 1);
                AccessKey(0x4F);
                var viewer = await Until(client, s => Viewer(s) != 0);
                Require(Pocket(viewer).GetProperty("SampleMatchesFixture").GetBoolean(), "Saved pixels were lost across restart.");
                AccessKey(0x48);
                await Until(client, s => Viewer(s) == 0);
                await FocusTrackingAsync(client, workspace);
                var capture = await BeginPocketCapture(client);
                long selector = Selector(capture);
                Require(capture.GetProperty("Tracking").ValueKind == JsonValueKind.Null, "Pocket unexpectedly opened tracking.");
                await client.Command("exit");
                await host.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Require(host.ExitCode == 0 && !Input.Exists(new nint(workspace)) && !Input.Exists(new nint(selector)),
                    "Pocket windows survived application exit.");
                Require(Directory.GetFiles(Path.Combine(output, "pocket-test", "assets"), "*.png").Length == 1,
                    "Exit during capture wrote a second image.");
                return new { Reloaded = true, CameraOff = true, ClosedDuringCapture = true, NoUnconfirmedSave = true };
            }
            finally
            {
                Input.ReleaseAll();
                if (!host.HasExited)
                {
                    host.Kill(entireProcessTree: true);
                    Require(host.WaitForExit(3000), "Pocket restart host remained active.");
                }
            }
        });
    }

    private static void CleanPocketFixture(string output)
    {
        string root = Path.Combine(output, "pocket-test");
        if (!Directory.Exists(root)) return;
        try
        {
            string assets = Path.Combine(root, "assets");
            Require((File.GetAttributes(root) & FileAttributes.ReparsePoint) == 0, "Refusing linked fixture root.");
            if (Directory.Exists(assets))
            {
                Require((File.GetAttributes(assets) & FileAttributes.ReparsePoint) == 0, "Refusing linked fixture assets.");
                foreach (var asset in Directory.GetFiles(assets, "*.png"))
                    if (Guid.TryParseExact(Path.GetFileNameWithoutExtension(asset), "N", out _)) File.Delete(asset);
                Directory.Delete(assets);
            }
            File.Delete(Path.Combine(root, "manifest.json"));
            File.Delete(Path.Combine(root, "manifest.backup.json"));
            Directory.Delete(root);
            Cases.Add(new("POCKET_FIXTURE_CLEANUP", "TEST_HARNESS_CLEANUP", "Remove synthetic capture files", new { Removed = true }, "PASS", 0));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        { Cases.Add(new("POCKET_FIXTURE_CLEANUP", "TEST_HARNESS_CLEANUP", "Remove synthetic capture files", ex.Message, "FAIL", 0)); }
    }
}
