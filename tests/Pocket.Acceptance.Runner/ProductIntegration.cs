using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Pocket.Acceptance.Runner;

internal static partial class Program
{
    private static JsonElement Product(JsonElement state) => state.GetProperty("Product");
    private static JsonElement ProductPocket(JsonElement state) => Product(state).GetProperty("Pocket");
    private static JsonElement Board(JsonElement state) => Product(state).GetProperty("Workspace");
    private static JsonElement Slot(JsonElement state, string slot) =>
        Board(state).GetProperty("Cards").EnumerateArray().Single(card => card.GetProperty("Slot").GetString() == slot);
    private static string? Claim(JsonElement state, string owner) => Board(state).GetProperty(owner).GetString();
    private static JsonElement Binding(JsonElement state, string action) => Product(state).GetProperty("Bindings")
        .EnumerateArray().Single(row => row.GetProperty("Action").GetString() == action);
    private static bool ShelfVisible(JsonElement state) =>
        ProductPocket(state).ValueKind == JsonValueKind.Object && ProductPocket(state).GetProperty("LibraryVisible").GetBoolean();
    private static bool CameraOff(JsonElement state) => Product(state).GetProperty("Camera").GetString() == "Ghost · Camera off" &&
        state.GetProperty("Tracking").ValueKind == JsonValueKind.Null;
    private static long ProductHandle(JsonElement state) => Product(state).GetProperty("Handle").GetInt64();

    private static void Press(ushort key) { Input.Key(key, false); Input.Key(key, true); }

    private static void HotKey(ushort key, int repeats = 1)
    {
        Input.Key(0x11, false);
        try
        {
            Input.Key(0x12, false);
            try
            {
                for (int i = 0; i < repeats; i++) Input.Key(key, false);
                Input.Key(key, true);
            }
            finally { Input.Key(0x12, true); }
        }
        finally { Input.Key(0x11, true); }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint hwnd);

    [DllImport("user32.dll")]
    private static extern uint GetGuiResources(nint process, uint flags);

    private static async Task RunProductIntegration(string app, string output)
    {
        if (!Cases.Any(c => c.Id == "C05_C11_POCKET_RESTART" && c.Status == "PASS")) return;
        await Measure("F12_F15_SHELF_SHORTCUTS", "NATIVE_HOTKEY + WPF_INTEGRATION",
            "Conflicted chord stays owned by the fixture; remap dispatches once, tucks the real shelf, and restores after exit", async () =>
        {
            var (host, client) = StartAcceptanceHost(app, output);
            using (host)
            using (client)
            try
            {
                await client.Connect();
                var budget = await client.Command("product-diagnostic-budget");
                Require(budget.GetProperty("DiagnosticReservations").EnumerateArray().Select(value => value.GetBoolean())
                    .SequenceEqual(new[] { true, true, true, false }), "The diagnostic camera budget allowed a fourth start.");
                await client.Command("product-conflict");
                var start = await client.Command("product-start");
                long main = ProductHandle(start);
                await FocusTrackingAsync(client, main);
                Require(CameraOff(start) && Binding(start, "Pocket").GetProperty("Status").GetString()!.StartsWith("Unavailable"),
                    "Startup did not leave the camera off or disclose the fixture conflict.");
                Require(Binding(start, "Capture").GetProperty("Status").GetString() == "Registered" &&
                    Binding(start, "Workspace").GetProperty("Status").GetString() == "Registered",
                    "Available default shortcuts did not register.");
                HotKey(0x50);
                var conflict = await Until(client, s => s.GetProperty("FixtureHotKeyHits").GetInt32() == 1);
                Require(!ShelfVisible(conflict), "The conflicting fixture shortcut triggered Pocket.");
                var fallback = await client.Command("product-tray-fallback");
                Require(ShelfVisible(fallback), "The shared tray command failed to reveal the shelf.");
                long shelf = ProductPocket(fallback).GetProperty("Handle").GetInt64();
                await FocusTrackingAsync(client, shelf);
                AccessKey(0x54); // Tuck shelf
                await Until(client, s => !ShelfVisible(s));
                Require(!IsWindowVisible(new nint(shelf)), "The tucked shelf retained a visible HWND.");
                var fixture = await client.Command("focus");
                long fixtureHandle = fixture.GetProperty("FixtureHandle").GetInt64();
                await FocusTrackingAsync(client, fixtureHandle);
                int clicks = Number(fixture, "Clicks");
                Input.Click(Coordinate(fixture, "Button", "X"), Coordinate(fixture, "Button", "Y"));
                await Until(client, s => Number(s, "Clicks") == clicks + 1 && !ShelfVisible(s));

                var invalid = await client.Command("product-invalid");
                Require(invalid.GetProperty("ShortcutResult").ValueKind == JsonValueKind.String &&
                    Binding(invalid, "Pocket").GetProperty("Chord").GetString() == "Ctrl+Alt+P",
                    "Invalid remap changed the old mapping.");
                var remap = await client.Command("product-remap");
                Require(remap.GetProperty("ShortcutResult").ValueKind == JsonValueKind.Null &&
                    Binding(remap, "Pocket").GetProperty("Status").GetString() == "Registered", "Remap failed.");
                string mapFile = Path.Combine(output, "pocket-migration", "Invey", "shortcuts.json");
                byte[] savedBindings = File.ReadAllBytes(mapFile);
                using (File.Open(mapFile, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    var locked = await client.Command("product-locked-remap");
                    Require(locked.GetProperty("ShortcutResult").GetString()?.StartsWith("Could not save") == true &&
                        Binding(locked, "Pocket").GetProperty("Chord").GetString() == "Ctrl+Alt+J",
                        "A failed settings write changed the registered mapping.");
                }
                Require(savedBindings.SequenceEqual(File.ReadAllBytes(mapFile)), "A failed remap changed the settings file.");
                HotKey(0x4A, repeats: 4);
                await Until(client, ShelfVisible);
                var settled = await client.Command("status");
                Require(ShelfVisible(settled) && IsWindowVisible(new nint(shelf)),
                    "One repeated press did not leave the shelf visibly revealed.");
                HotKey(0x4A);
                await Until(client, s => !ShelfVisible(s));

                HotKey(0x47);
                await Until(client, s => ProductPocket(s).GetProperty("CaptureHandle").GetInt64() != 0);
                Press(0x1B);
                var canceled = await Until(client, s => ProductPocket(s).GetProperty("CaptureHandle").GetInt64() == 0 &&
                    !ProductPocket(s).GetProperty("Busy").GetBoolean());
                Require(!ShelfVisible(canceled) && CameraOff(canceled) && ProductPocket(canceled).GetProperty("Count").GetInt32() == 2,
                    "Capture cancel changed the shelf, saved a reference or opened the camera.");

                await RunTwoTargetWorkspace(client);
                await client.Command("exit");
                await host.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Require(host.ExitCode == 0 && !Input.Exists(new nint(main)) && !Input.Exists(new nint(shelf)),
                    "Product windows survived process exit.");
            }
            finally { FinishProductHost(host); }

            var (restarted, newClient) = StartAcceptanceHost(app, output);
            using (restarted)
            using (newClient)
            try
            {
                await newClient.Connect();
                var reopened = await newClient.Command("product-start");
                await FocusTrackingAsync(newClient, ProductHandle(reopened));
                Require(Binding(reopened, "Pocket").GetProperty("Chord").GetString() == "Ctrl+Alt+J" &&
                    Binding(reopened, "Pocket").GetProperty("Status").GetString() == "Registered",
                    "A fresh process could not register the persisted binding.");
                HotKey(0x4A);
                var visible = await Until(newClient, ShelfVisible);
                Require(CameraOff(visible), "Revealing the shelf after restart opened the camera.");
                if (Environment.GetEnvironmentVariable("INVEY_ACCEPTANCE_CARD_PROFILE") == "1")
                    await RunCardProfile(newClient, restarted, output);
                await newClient.Command("exit");
                await restarted.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Require(restarted.ExitCode == 0, "The restarted product did not close cleanly.");
            }
            finally { FinishProductHost(restarted); }
            return new { ConflictOwned = true, InvalidRejected = true, SharedTrayCommand = true, Remapped = true,
                NativeShelfHidden = true, FixtureClickDelivered = true, NoRepeat = true, CaptureCanceled = true,
                CameraOff = true, ReRegisteredAfterExit = true, DiagnosticFourthStartRejected = true };
        });
    }

    private static async Task RunCardProfile(Client client, Process host, string output)
    {
        await Measure("POCKET_CARD_RESOURCE_PROFILE", "NATIVE_WPF_SYNTHETIC_RESOURCE_PROFILE",
            "Exercise four real floating cards for 45 seconds, refuse a fifth, recover from a locked placement write, and sample resources", async () =>
        {
            var opening = Stopwatch.StartNew();
            var opened = await client.Command("product-profile-start");
            double openingMs = opening.Elapsed.TotalMilliseconds;
            var samples = new List<object>();
            var times = new List<double>();
            void Record(JsonElement state, int cycle)
            {
                host.Refresh();
                var resources = state.GetProperty("Resources");
                Require(resources.GetProperty("Cards").GetInt32() == 4 && CameraOff(state), "The card or camera bound changed.");
                samples.Add(new { Cycle = cycle, host.WorkingSet64, GdiObjects = GetGuiResources(host.Handle, 0),
                    UserObjects = GetGuiResources(host.Handle, 1), Ui = resources });
            }
            Record(opened, 0);
            string manifest = Path.Combine(output, "pocket-migration", "Invey", "manifest.json");
            using (File.Open(manifest, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                await client.Command("product-profile-cycle");
                await Until(client, state => state.GetProperty("Resources").GetProperty("PlacementFailures").GetInt32() > 0);
            }

            var duration = Stopwatch.StartNew();
            int cycle = 0;
            while (duration.Elapsed < TimeSpan.FromSeconds(45))
            {
                var action = Stopwatch.StartNew();
                var state = await client.Command("product-profile-cycle");
                times.Add(action.Elapsed.TotalMilliseconds);
                if (++cycle % 15 == 0) Record(state, cycle);
                await Task.Delay(360);
            }
            var final = await client.Command("status");
            Record(final, cycle);
            using var saved = JsonDocument.Parse(File.ReadAllBytes(manifest));
            int expectedZoom = cycle % 2 == 0 ? 125 : 0;
            Require(saved.RootElement.GetProperty("Items").EnumerateArray().Count(item =>
                item.TryGetProperty("Placement", out var placement) && placement.ValueKind == JsonValueKind.Object &&
                placement.GetProperty("Zoom").GetInt32() == expectedZoom) >= 4,
                "The writer did not commit the latest zoom after the lock was released.");

            long shelf = ProductPocket(final).GetProperty("Handle").GetInt64();
            await FocusTrackingAsync(client, ShelfVisible(final) ? shelf : ProductHandle(final));
            HotKey(0x47);
            var capture = await Until(client, state => ProductPocket(state).GetProperty("CaptureHandle").GetInt64() != 0);
            var probe = ProductPocket(capture).GetProperty("Capture").GetProperty("BlackFixtureBands");
            Require(probe.GetProperty("Historical").GetProperty("Nonblack").GetInt32() == 0 &&
                probe.GetProperty("Bottom").GetProperty("Nonblack").GetInt32() == 0, "A moved card left content in the frozen black stripe: " + probe.GetRawText());
            Press(0x1B);
            await Until(client, s => !ProductPocket(s).GetProperty("CaptureInProgress").GetBoolean());
            times.Sort();
            return new { Cycles = cycle, Seconds = duration.Elapsed.TotalSeconds, OpeningMs = openingMs,
                MedianDispatchRoundTripMs = times[times.Count / 2], P95DispatchRoundTripMs = times[(int)(times.Count * .95)],
                Samples = samples, PlacementRecovered = true, FourCardCapture = true };
        });
    }

    private static void FinishProductHost(Process host)
    {
        Input.ReleaseAll();
        if (host.HasExited) return;
        host.Kill(entireProcessTree: true);
        Require(host.WaitForExit(3000), "Owned product host remained active.");
    }

    private static async Task RunTwoTargetWorkspace(Client client)
    {
        await Measure("C09_WORKSPACE_CONTROLLED", "NATIVE_WPF_MOUSE + SIMULATED_LOGICAL_GHOST",
            "Two actual snapshot views move independently for 30 sessions with separate claims and native cursor control", async () =>
        {
            HotKey(0x57);
            var opened = await Until(client, s => Board(s).ValueKind == JsonValueKind.Object &&
                Board(s).GetProperty("References").GetInt32() >= 2);
            long board = Board(opened).GetProperty("Handle").GetInt64();
            await FocusTrackingAsync(client, board);
            AccessKey(0x41); // Load first selection.
            await Until(client, s => Board(s).GetProperty("Cards").GetArrayLength() == 1);
            AccessKey(0x52); // Focus saved reference picker.
            Press(0x23); // End selects the other snapshot.
            AccessKey(0x42);
            var two = await Until(client, s => Board(s).GetProperty("Cards").GetArrayLength() == 2);
            Require(Slot(two, "A").GetProperty("ImageLoaded").GetBoolean() && Slot(two, "B").GetProperty("ImageLoaded").GetBoolean(),
                "Both verified snapshot views did not load.");
            double originalWidth = Slot(two, "B").GetProperty("Width").GetDouble();
            var larger = Board(two).GetProperty("Larger");
            Input.Click(larger.GetProperty("X").GetDouble(), larger.GetProperty("Y").GetDouble());
            await Until(client, s => Slot(s, "B").GetProperty("Width").GetDouble() > originalWidth);

            for (int i = 0; i < 30; i++)
            {
                var before = await client.Command("status");
                var a = Slot(before, "A");
                var b = Slot(before, "B");
                var grip = a.GetProperty("Grip");
                double x = grip.GetProperty("X").GetDouble(), y = grip.GetProperty("Y").GetDouble();
                double startA = a.GetProperty("X").GetDouble(), startB = b.GetProperty("X").GetDouble();
                Input.Move(x, y);
                Input.Down();
                try
                {
                    await Until(client, s => Claim(s, "Mouse") == "A");
                    var held = await client.Command("product-ghost-press");
                    Require(Claim(held, "Mouse") == "A" && Claim(held, "Ghost") == "B", $"Trial {i + 1} failed to acquire both claims.");
                    var cursor = Input.Cursor();
                    var movedB = await client.Command(i % 2 == 0 ? "product-ghost-move" : "product-ghost-back");
                    var afterGhost = Input.Cursor();
                    Require(Slot(movedB, "B").GetProperty("X").GetDouble() == startB + (i % 2 == 0 ? 8 : -8) &&
                        Slot(movedB, "A").GetProperty("X").GetDouble() == startA &&
                        cursor.X == afterGhost.X && cursor.Y == afterGhost.Y, $"Trial {i + 1} Ghost affected mouse or failed to move B.");
                    var heldB = Slot(movedB, "B").GetProperty("X").GetDouble();
                    Input.Move(x + (i % 2 == 0 ? 24 : -24), y);
                    await Until(client, s => Slot(s, "A").GetProperty("X").GetDouble() != startA &&
                        Slot(s, "B").GetProperty("X").GetDouble() == heldB);
                    if (i % 2 == 0)
                    {
                        var canceledGhost = await client.Command("product-ghost-cancel");
                        Require(Claim(canceledGhost, "Mouse") == "A" && Claim(canceledGhost, "Ghost") is null,
                            $"Trial {i + 1} canceled the mouse claim with Ghost.");
                    }
                }
                finally { Input.Up(); }
                var releasedMouse = await Until(client, s => Claim(s, "Mouse") is null);
                if (i % 2 != 0)
                {
                    Require(Claim(releasedMouse, "Ghost") == "B", $"Trial {i + 1} released Ghost with the mouse.");
                    await client.Command("product-ghost-cancel");
                }
            }
            var last = await client.Command("status");
            var gripA = Slot(last, "A").GetProperty("Grip");
            Input.Move(gripA.GetProperty("X").GetDouble(), gripA.GetProperty("Y").GetDouble());
            Input.Down();
            try
            {
                await Until(client, s => Claim(s, "Mouse") == "A");
                await client.Command("product-ghost-press");
                var closed = await client.Command("product-close-board");
                Require(Board(closed).ValueKind == JsonValueKind.Null && !Input.Exists(new nint(board)),
                    "Closing the board with both claims left a live window.");
            }
            finally { Input.ReleaseAll(); }
            Require(CameraOff(await client.Command("status")), "Workspace opened the camera.");
            return new { Trials = 30, MouseReleasedIndependently = 15, GhostCanceledIndependently = 15,
                BothViewsDecoded = true, Resize = true, NativeCursorStableDuringGhost = true, CameraOff = true };
        });
    }
}
