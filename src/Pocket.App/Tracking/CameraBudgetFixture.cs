using System.Diagnostics;
using System.Text.Json;
using Pocket.Core.Tracking;

namespace Pocket.App.Tracking;

/// <summary>Camera-free doubles behind the existing private-desktop guard.</summary>
internal static class CameraBudgetFixture
{
    internal static async Task<int> RunDoubleAsync(string[] args)
    {
        if (args.Length != 4) return 2;
        string session = args[1], directory = args[2], mode = args[3];
        if (await Console.In.ReadLineAsync() != "start") return 0;
        await File.WriteAllTextAsync(Path.Combine(directory, session + ".started"), "synthetic-no-camera");
        if (mode == "ignore") await Task.Delay(TimeSpan.FromSeconds(30));
        else await Console.In.ReadLineAsync(); // Closing stdin is the normal early-stop path.
        await File.WriteAllTextAsync(Path.Combine(directory, session + ".worker.json"),
            JsonSerializer.Serialize(new { session, fixture = true, stages = new { } }));
        return 0;
    }

    internal static async Task<object> ExerciseAsync(string mode)
    {
        AcceptanceHost.ValidateDesktop();
        string store = Environment.GetEnvironmentVariable("GHOST_TEST_POCKET_STORE")!;
        string root = Path.Combine(Path.GetDirectoryName(store)!, "camera-budget-fixture");
        Directory.CreateDirectory(root);
        bool performance = mode is "performance-series" or "performance-followup";
        string path = Path.Combine(root, performance ? "performance-series.json" : mode is "series" or "amended-series" or "targets-series" ? mode + ".json" : Guid.NewGuid().ToString("N") + ".json");
        bool fresh = !File.Exists(path);
        if (fresh && performance)
            File.WriteAllText(path,JsonSerializer.Serialize(new CameraBudgetState(6,"execution-16",
                Enumerable.Range(0,12).Select(_=>new CameraBudgetEntry(Guid.NewGuid(),80000,true)).ToArray(),CameraBudget.PointerRetestAmendment,5,10)));
        else if (fresh) CameraBudget.Initialize(path);
        if (performance)
        {
            using var budget = new CameraBudget(path);
            budget.AmendPerformanceCheck();
            if (mode == "performance-followup") budget.AmendPerformanceFollowup();
        }
        if (fresh && mode == "targets-series")
        {
            using var budget = new CameraBudget(path);
            for (int i = 0; i < 5; i++) { var id = Guid.NewGuid(); budget.Reserve(id); budget.Complete(id, 1); }
            budget.AmendContinuation(); budget.AmendLearning();
            for (int i = 0; i < 3; i++) { var id = Guid.NewGuid(); budget.Reserve(id); budget.Complete(id, 1); }
            budget.AmendLearningTargets();
        }
        if (mode.StartsWith("amended", StringComparison.Ordinal))
        {
            using var budget = new CameraBudget(path);
            budget.AmendContinuation();
        }
        if (mode == "corrupt") File.WriteAllText(path, "{broken");
        if (mode == "missing") File.Delete(path);
        Guid session = Guid.NewGuid();
        string? previous = Environment.GetEnvironmentVariable("INVEY_CAMERA_BUDGET");
        Environment.SetEnvironmentVariable("INVEY_CAMERA_BUDGET", path);
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardError = true };
        foreach (string arg in new[] { "--camera-budget-double", session.ToString(), root,
            mode is "deadline" or "interrupted" ? "ignore" : "stop" }) start.ArgumentList.Add(arg);
        using var worker = Process.Start(start) ?? throw new IOException("No camera budget double.");
        CameraBudgetGuard? guard = null;
        bool permit = false;
        try
        {
            using var cancel = new CancellationTokenSource();
            if (mode == "canceled") cancel.Cancel();
            try
            {
                guard = await CameraBudgetGuard.StartAsync(worker, session,
                    mode == "directory" ? Path.Combine(root, "absent") : root, cancel.Token, limitMs: 700);
                permit = true;
                await worker.StandardInput.WriteLineAsync("start");
                await worker.StandardInput.FlushAsync();
                if (mode == "interrupted")
                {
                    using var guardian = Process.GetProcessById(guard.GuardianId);
                    guardian.Kill();
                    await guardian.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2));
                }
                else if (mode != "deadline")
                    worker.StandardInput.Close();
            }
            catch (Exception error) when (error is IOException or OperationCanceledException or InvalidDataException)
            { }
            if (!permit) worker.StandardInput.Close();
            await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3));
            if (guard is not null) { await guard.DisposeAsync(); guard = null; }
            bool started = File.Exists(Path.Combine(root, session + ".started"));
            bool clean = File.Exists(Path.Combine(root, session + ".worker.json"));
            bool? released = null;
            if (File.Exists(path) && mode != "corrupt")
            {
                using var budget = new CameraBudget(path);
                released = budget.Read().Sessions.LastOrDefault()?.Released;
            }
            return new { Mode = mode, Permit = permit, Started = started, WorkerExited = worker.HasExited,
                CleanSyntheticStop = clean, ReservationReleased = released, RealDeviceOpened = false };
        }
        finally
        {
            Environment.SetEnvironmentVariable("INVEY_CAMERA_BUDGET", previous);
            if (!worker.HasExited)
            {
                worker.Kill();
                await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2));
            }
            if (guard is not null) await guard.DisposeAsync();
        }
    }

    internal static RecordedTrackingWindow CreateBudgetPreview(string mode)
    {
        AcceptanceHost.ValidateDesktop();
        string store = Environment.GetEnvironmentVariable("GHOST_TEST_POCKET_STORE")!;
        string root = Path.Combine(Path.GetDirectoryName(store)!, "camera-budget-fixture");
        Directory.CreateDirectory(root);
        string path = Path.Combine(root, Guid.NewGuid().ToString("N") + ".json");
        if (mode == "performance")
        {
            File.WriteAllText(path,JsonSerializer.Serialize(new CameraBudgetState(6,"execution-16",
                Enumerable.Range(0,12).Select(_=>new CameraBudgetEntry(Guid.NewGuid(),80000,true)).ToArray(),CameraBudget.PointerRetestAmendment,5,10)));
            using var b=new CameraBudget(path);b.AmendPerformanceCheck();
        }
        else
        {
            CameraBudget.Initialize(path);
            using var b = new CameraBudget(path);
            b.AmendContinuation(); b.AmendLearning(); b.AmendLearningTargets();
            int count = mode == "pending" ? 1 : 5;
            for (int i = 0; i < count; i++)
            {
                var id = Guid.NewGuid(); b.Reserve(id);
                if (mode != "pending") b.Complete(id, 1);
            }
        }
        string? oldBudget = Environment.GetEnvironmentVariable("INVEY_CAMERA_BUDGET");
        string? oldDiagnostics = Environment.GetEnvironmentVariable("INVEY_CAMERA_DIAGNOSTICS");
        try
        {
            Environment.SetEnvironmentVariable("INVEY_CAMERA_BUDGET", path);
            Environment.SetEnvironmentVariable("INVEY_CAMERA_DIAGNOSTICS", root);
            var window = new RecordedTrackingWindow(acceptanceHost: true, testSettingsPath: Path.Combine(root, "calibration.json"));
            window.ConfigureBudgetPreviewFixture();
            return window;
        }
        finally
        {
            Environment.SetEnvironmentVariable("INVEY_CAMERA_BUDGET", oldBudget);
            Environment.SetEnvironmentVariable("INVEY_CAMERA_DIAGNOSTICS", oldDiagnostics);
        }
    }

    internal static void Cleanup()
    {
        string store = Environment.GetEnvironmentVariable("GHOST_TEST_POCKET_STORE")!;
        string root = Path.Combine(Path.GetDirectoryName(store)!, "camera-budget-fixture");
        if (!Directory.Exists(root)) return;
        if (File.GetAttributes(root).HasFlag(FileAttributes.ReparsePoint)) throw new IOException("Linked budget fixture.");
        foreach (string file in Directory.GetFiles(root))
        {
            if (File.GetAttributes(file).HasFlag(FileAttributes.ReparsePoint)) throw new IOException("Linked budget fixture file.");
            File.Delete(file);
        }
        Directory.Delete(root);
    }
}
