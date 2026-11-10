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
        var ready = Pocket(await Until(client, state => !Pocket(state).GetProperty("CaptureInProgress").GetBoolean() &&
            Pocket(state).GetProperty("CapturePoint").ValueKind == JsonValueKind.Object));
        var button = ready.GetProperty("CapturePoint");
        Input.Click(button.GetProperty("X").GetDouble(), button.GetProperty("Y").GetDouble());
        var active = await Until(client, state => Selector(state) != 0 ||
            PocketText(state).StartsWith("Capture unavailable.", StringComparison.Ordinal), 5000);
        Require(Selector(active) != 0, "Capture did not open: " + PocketText(active));
        await Until(client, state => state.GetProperty("Native").GetProperty("Foreground").GetInt64() == Selector(active));
        return active;
    }

    private static async Task RunPocketCapture(Client client, string output)
    {
        await Measure("C01_C04_POCKET_CAPTURE", "NATIVE_GDI_FIXTURE + WPF_INTEGRATION",
            "Frozen selection captures known desktop pixels, cancels without files, confirms black and saves only the crop", async () =>
        {
            await client.Command("show"); // The capture must hide the passive overlay before taking pixels.
            var opened = await client.Command("pocket-open");
            long workspace = Pocket(opened).GetProperty("Handle").GetInt64();
            await FocusTrackingAsync(client, workspace);
            await Until(client, s => PocketText(s).StartsWith("Your library is empty", StringComparison.Ordinal));
            string store = Path.Combine(output, "pocket-test");
            var historical = new List<JsonElement>();
            async Task<JsonElement> Begin()
            {
                var state = await BeginPocketCapture(client);
                historical.Add(Pocket(state).GetProperty("Capture").GetProperty("BlackFixtureBands"));
                return state;
            }
            var active = await Begin();
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
            Draw(.32, .55, .35, .59);
            AccessKey(0x50);
            var boundary = await Until(client, s => Pocket(s).GetProperty("Capture").GetProperty("Preview").GetBoolean());
            Require(Pocket(boundary).GetProperty("Capture").GetProperty("SaveEnabled").GetBoolean(),
                "A black-to-color boundary was reported as all black.");
            AccessKey(0x42);
            await Until(client, s => !Pocket(s).GetProperty("Capture").GetProperty("Preview").GetBoolean());
            Draw(.50, .82, .505, .825);
            AccessKey(0x50);
            var small = await Until(client, s => Pocket(s).GetProperty("Capture").GetProperty("Preview").GetBoolean());
            Require(!Pocket(small).GetProperty("Capture").GetProperty("SaveEnabled").GetBoolean(),
                "The small black region did not require confirmation: " + Pocket(small).GetProperty("Capture").GetRawText());
            AccessKey(0x42);
            await Until(client, s => !Pocket(s).GetProperty("Capture").GetProperty("Preview").GetBoolean());
            Draw(.38, .64, .62, .80);
            AccessKey(0x50);
            var black = await Until(client, s =>
            {
                Require(Selector(s) != 0, "Selector closed while checking black content: " + PocketText(s));
                return Pocket(s).GetProperty("Capture").GetProperty("Status").GetString()!.Contains("preview is black");
            });
            Require(!Pocket(black).GetProperty("Capture").GetProperty("SaveEnabled").GetBoolean(),
                "Black content could be saved without explicit confirmation.");
            AccessKey(0x42); // Back
            AccessKey(0x4B); // Keyboard region
            AccessKey(0x50); // Preview
            await Until(client, s => Pocket(s).GetProperty("Capture").GetProperty("Preview").GetBoolean());
            Input.Key(0x1B, false); Input.Key(0x1B, true);
            await Until(client, s => Selector(s) == 0 && PocketText(s) == "Capture closed.");
            Require(!Directory.Exists(store), "Cancel wrote Pocket data.");
            await Until(client, s => s.GetProperty("Native").GetProperty("Foreground").GetInt64() == workspace);

            active = await Begin();
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
            var afterCardClose = await Begin();
            var oldBand = Pocket(afterCardClose).GetProperty("Capture").GetProperty("BlackFixtureBands").GetProperty("Historical");
            Require(oldBand.GetProperty("Nonblack").GetInt32() == 0,
                "The previous card left pixels in the historical black region: " + oldBand.GetRawText());
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

            await Begin();
            Draw(.48, .87, .54, .94);
            AccessKey(0x50);
            await Until(client, s => Pocket(s).GetProperty("Capture").GetProperty("Status").GetString()!.Contains("preview is black"));
            Input.Key(0x20, false); Input.Key(0x20, true); // Acknowledge the black-content warning.
            await Until(client, s => Pocket(s).GetProperty("Capture").GetProperty("SaveEnabled").GetBoolean());
            AccessKey(0x53);
            await Until(client, s => Selector(s) == 0 && Pocket(s).GetProperty("Count").GetInt32() == 2, 6000);
            AccessKey(0x4F);
            var blackCard = await Until(client, s => Viewer(s) != 0 && Pocket(s).GetProperty("SampleBlack").GetBoolean());
            long pinnedHandle = Viewer(blackCard);
            AccessKey(0x49); // Zoom in from Fit to 125%.
            await Until(client, s => Pocket(s).GetProperty("Zoom").GetInt32() == 125);
            AccessKey(0x50);
            var pinned = await Until(client, s => Pocket(s).GetProperty("Pinned").GetBoolean());
            var bounds = Pocket(pinned).GetProperty("ViewerBounds");
            int cardX = bounds.GetProperty("X").GetInt32(), cardY = bounds.GetProperty("Y").GetInt32();
            Input.Move(cardX + 150, cardY + 20);
            Input.Down();
            try { Input.Move(cardX + 250, cardY - 55); }
            finally { Input.Up(); }
            var moved = await Until(client, s => Pocket(s).GetProperty("ViewerBounds").GetProperty("X").GetInt32() != cardX);
            var movedBounds = Pocket(moved).GetProperty("ViewerBounds");
            int right = movedBounds.GetProperty("Right").GetInt32(), bottom = movedBounds.GetProperty("Bottom").GetInt32();
            int originalWidth = movedBounds.GetProperty("Width").GetInt32();
            Input.Move(right - 2, bottom - 2);
            Input.Down();
            try
            {
                await Task.Delay(80);
                for (int step = 1; step <= 4; step++)
                {
                    Input.Move(right - 2 + step * 20, bottom - 2 + step * 14);
                    await Task.Delay(20);
                }
            }
            finally { Input.Up(); }
            await Until(client, s => Pocket(s).GetProperty("ViewerBounds").GetProperty("Width").GetInt32() > originalWidth);
            var hidden = await client.Command("pocket-hide-library");
            Require(!Pocket(hidden).GetProperty("LibraryVisible").GetBoolean() && Input.Exists(new nint(pinnedHandle)),
                "Hiding the library removed its independent pinned card.");
            await client.Command("pocket-open");
            await FocusTrackingAsync(client, workspace);
            var rows = Pocket(await Until(client, s => !Pocket(s).GetProperty("Busy").GetBoolean() &&
                Pocket(s).GetProperty("LastRowPoint").ValueKind == JsonValueKind.Object));
            var lastRow = rows.GetProperty("LastRowPoint");
            Input.Click(lastRow.GetProperty("X").GetDouble(), lastRow.GetProperty("Y").GetDouble());
            await Until(client, s => Pocket(s).GetProperty("SelectedIndex").GetInt32() == 1 &&
                s.GetProperty("Native").GetProperty("Foreground").GetInt64() == workspace);
            AccessKey(0x4F);
            await Until(client, s => Pocket(s).GetProperty("Cards").GetInt32() == 2 &&
                Pocket(s).GetProperty("SampleMatchesFixture").GetBoolean());
            await FocusTrackingAsync(client, workspace);
            AccessKey(0x55); // Move up
            await Until(client, s => Pocket(s).GetProperty("SelectedIndex").GetInt32() == 0 && !Pocket(s).GetProperty("Busy").GetBoolean());
            AccessKey(0x4E); // Rename
            await Until(client, s => Pocket(s).GetProperty("RenameHandle").GetInt64() is var rename && rename != 0 &&
                s.GetProperty("Native").GetProperty("Foreground").GetInt64() == rename);
            Input.Text("Known crop");
            Input.Key(0x0D, false); Input.Key(0x0D, true);
            await Until(client, s => Pocket(s).GetProperty("SelectedTitle").GetString() == "Known crop");
            AccessKey(0x4C); // Delete
            await Until(client, s => Pocket(s).GetProperty("Count").GetInt32() == 1 && Pocket(s).GetProperty("Cards").GetInt32() == 1);
            AccessKey(0x45); // Undo delete
            await Until(client, s => Pocket(s).GetProperty("Count").GetInt32() == 2 &&
                Pocket(s).GetProperty("SelectedTitle").GetString() == "Known crop");
            var closed = await client.Command("pocket-close");
            Require(Pocket(closed).ValueKind == JsonValueKind.Null &&
                closed.GetProperty("Tracking").ValueKind == JsonValueKind.Null, "Pocket was not closed or started tracking.");
            return new { Dpi = active.GetProperty("Dpi").GetInt32(), CropWidth = cropWidth, CropHeight = cropHeight,
                ExpectedPixels = true, CancelWroteNothing = true, ConfirmedBlack = true, CameraOff = true,
                LockedLibraryPreserved = true, RestoredAfterFailure = true, MultipleCards = true,
                IndependentPin = true, MoveResize = true, RenameMoveDeleteUndo = true,
                HistoricalAndBottomFixtureBands = historical, BlackBoundaryAndSmallCrop = true };
        });
    }

    private static async Task RunPocketRestart(string app, string output)
    {
        if (!Cases.Any(c => c.Id == "C01_C04_POCKET_CAPTURE" && c.Status == "PASS")) return;
        await Measure("C05_C11_POCKET_RESTART", "PROCESS_RESTART + WPF_INTEGRATION",
            "A fresh process reopens the saved image and exits safely during a new unsaved capture", async () =>
        {
            string source = Path.Combine(output, "pocket-test");
            string migration = Path.Combine(output, "pocket-migration");
            string legacy = Path.Combine(migration, "PocketDualGhost");
            string originalAssets = Path.Combine(legacy, "assets");
            Directory.CreateDirectory(originalAssets);
            foreach (var file in Directory.GetFiles(Path.Combine(source, "assets"), "*.png"))
                File.Copy(file, Path.Combine(originalAssets, Path.GetFileName(file)));
            var originalIndex = File.ReadAllBytes(Path.Combine(source, "manifest.json"));
            File.WriteAllBytes(Path.Combine(legacy, "manifest.json"), originalIndex);
            using var index = JsonDocument.Parse(originalIndex);
            var placement = index.RootElement.GetProperty("Items").EnumerateArray()
                .Select(item => item.GetProperty("Placement"))
                .Single(value => value.ValueKind == JsonValueKind.Object && value.GetProperty("Pinned").GetBoolean());
            var (host, client) = StartAcceptanceHost(app, output);
            using (host)
            using (client)
            try
            {
                await client.Connect();
                var opened = await client.Command("pocket-open-migrated");
                long workspace = Pocket(opened).GetProperty("Handle").GetInt64();
                await FocusTrackingAsync(client, workspace);
                var restored = await Until(client, s => Pocket(s).GetProperty("Count").GetInt32() == 2 &&
                    Pocket(s).GetProperty("Cards").GetInt32() == 1 && Pocket(s).GetProperty("Pinned").GetBoolean());
                var restoredBounds = Pocket(restored).GetProperty("ViewerBounds");
                foreach (string property in new[] { "X", "Y", "Width", "Height" })
                    Require(restoredBounds.GetProperty(property).GetInt32() == placement.GetProperty(property).GetInt32(),
                        "The restarted card did not restore its physical placement.");
                Require(Pocket(restored).GetProperty("Zoom").GetInt32() == 125, "The restarted card lost its zoom.");
                await FocusTrackingAsync(client, Viewer(restored));
                AccessKey(0x50); // Unpin the restored card.
                await Until(client, s => !Pocket(s).GetProperty("Pinned").GetBoolean() &&
                    Pocket(s).GetProperty("Cards").GetInt32() == 1);
                await FocusTrackingAsync(client, workspace);
                AccessKey(0x4F);
                var viewer = await Until(client, s => Pocket(s).GetProperty("Cards").GetInt32() == 2);
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
                Require(Directory.GetFiles(Path.Combine(migration, "Invey", "assets"), "*.png").Length == 2,
                    "Exit during capture wrote another image.");
                Require(originalIndex.SequenceEqual(File.ReadAllBytes(Path.Combine(legacy, "manifest.json"))) &&
                    Directory.GetFiles(originalAssets, "*.png").Length == 2, "Profile migration changed the original fixture.");
                return new { Reloaded = true, MigratedOriginalPreserved = true, PinnedCardRestored = true,
                    Unpinned = true, ZoomRestored = true, CameraOff = true, ClosedDuringCapture = true, NoUnconfirmedSave = true };
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
            string migration = Path.Combine(output, "pocket-migration");
            if (Directory.Exists(migration))
            {
                Require((File.GetAttributes(migration) & FileAttributes.ReparsePoint) == 0, "Refusing linked fixture migration.");
                CleanStore(Path.Combine(migration, "Invey"));
                CleanStore(Path.Combine(migration, "PocketDualGhost"));
                Directory.Delete(migration);
            }
            CleanStore(root);
            Cases.Add(new("POCKET_FIXTURE_CLEANUP", "TEST_HARNESS_CLEANUP", "Remove synthetic capture files", new { Removed = true }, "PASS", 0));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        { Cases.Add(new("POCKET_FIXTURE_CLEANUP", "TEST_HARNESS_CLEANUP", "Remove synthetic capture files", ex.Message, "FAIL", 0)); }

        static void CleanStore(string root)
        {
            if (!Directory.Exists(root)) return;
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
            File.Delete(Path.Combine(root, "shortcuts.json"));
            Directory.Delete(root);
        }
    }
}
