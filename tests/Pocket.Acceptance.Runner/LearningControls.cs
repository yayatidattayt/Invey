using System.Diagnostics;
using System.IO;
using System.Text.Json;
namespace Pocket.Acceptance.Runner;
internal static partial class Program
{
    private static JsonElement Learning(JsonElement s) => DemoTracking(s).GetProperty("Learning");
    private static async Task RunLearningControls(string app, string output, Process host, Client client)
    {
        await Measure("L18_WPF_LEARNING_LOOP", "ACTUAL_WPF_CONTROLS; SYNTHETIC_LABELED_OBSERVATIONS; NO_NATIVE_INPUT; NO_CAMERA",
            "Observe, confirm targets, fit, holdout validate, save, new process reload, reuse, rollback, reapply, reset", async () =>
        {
            string? model = null; double learnedX = 0; long revision = 0;
            try
            {
                await client.Command("demo-start");
                await client.Command("demo-ui|main|Ghost Tracking");
                var opened = await Until(client, s => DemoTracking(s).ValueKind == JsonValueKind.Object && DemoTracking(s).GetProperty("WindowLoaded").GetBoolean());
                Require(DemoTracking(opened).GetProperty("State").GetString() == "Off", "Unexpected camera/source startup.");
                Require(!Learning(opened).GetProperty("Enabled").GetBoolean(), "New profile learning should default off.");
                Require(!Learning(opened).GetProperty("DetailsExpanded").GetBoolean() && !Learning(opened).GetProperty("FeedbackVisible").GetBoolean(), "Advanced learning controls should start tucked away.");
                await client.Command("demo-ui|tracking|Calibrate");
                await Until(client, s => DemoTracking(s).GetProperty("Calibration").GetProperty("Open").GetBoolean());
                await client.Command("demo-hand|tracking|2");
                await client.Command("demo-ui|tracking|Save settings");
                var prepared = await Until(client, s => DemoTracking(s).GetProperty("Calibration").GetProperty("SettingsStatus").GetString() == "Calibration settings saved.");
                Require(DemoTracking(prepared).GetProperty("State").GetString() == "Off", "Preparing calibration activated a source.");
                Require(!Learning(prepared).GetProperty("CanConfirm").GetBoolean(), "Camera-off setup granted teaching authority.");
                await client.Command("demo-toggle|tracking|Learn from my tracking sessions");
                await Until(client, s => Learning(s).GetProperty("Enabled").GetBoolean());
                await client.Command("learning-fixture|begin");
                await client.Command("learning-fixture|loss");
                await client.Command("demo-ui|tracking|Start lesson");
                await Until(client, s => Learning(s).GetProperty("PendingStart").GetBoolean());
                Require(Learning(await client.Command("status")).GetProperty("Phase").GetString() == "Idle", "Missing observations invented a lesson label.");
                await client.Command("learning-fixture|observe");
                await Until(client, s => Learning(s).GetProperty("Phase").GetString() == "Baseline");
                await client.Command("demo-ui|tracking|Cancel lesson");
                await Until(client, s => Learning(s).GetProperty("Phase").GetString() == "Idle");
                Require(Learning(await client.Command("status")).GetProperty("Phase").GetString() == "Idle", "Cancel did not clear lesson.");
                await client.Command("demo-ui|tracking|Start lesson");
                for (int target = 0; target < 27; target++)
                {
                    await client.Command("learning-fixture|observe");
                    var ready = await Until(client, s => Learning(s).GetProperty("CanConfirm").GetBoolean());
                    Require(Learning(ready).GetProperty("Target").ValueKind == JsonValueKind.Object, "Known target missing.");
                    await client.Command("demo-ui|tracking|Confirm target");
                    // Invoke is queued by the actual WPF automation peer.
                    string phase = target < 8 ? "Baseline" : target < 17 ? "Teaching" : target < 26 ? "Validation" : "Complete";
                    int step = target < 8 ? target + 2 : target < 17 ? target - 7 : target < 26 ? target - 16 : 1;
                    await Until(client, s => Learning(s).GetProperty("Phase").GetString() == phase &&
                        Learning(s).GetProperty("Step").GetInt32() == step);
                    if (target == 0)
                    {
                        await client.Command("learning-fixture|loss");
                        var paused = Learning(await client.Command("status"));
                        Require(paused.GetProperty("Phase").GetString() == "Baseline" && paused.GetProperty("Step").GetInt32() == 2,
                            "A temporary tracking loss discarded confirmed labels.");
                        Require(!paused.GetProperty("CanConfirm").GetBoolean(), "Loss allowed a target label.");
                    }
                }
                await client.Command("demo-ui|tracking|Evaluate lesson");
                var accepted = await Until(client, s => Learning(s).GetProperty("Model").ValueKind == JsonValueKind.String, 8000);
                model = Learning(accepted).GetProperty("Model").GetString();
                revision = Learning(accepted).GetProperty("Revision").GetInt64();
                await client.Command("learning-fixture|observe");
                learnedX = Learning(await client.Command("status")).GetProperty("Prediction").GetProperty("X").GetDouble();
                await File.WriteAllTextAsync(System.IO.Path.Combine(output, "learning-accepted.json"), Learning(accepted).GetRawText());
                await client.Command("learning-fixture|end");
                await client.Command("demo-ui|main|Exit");
                await host.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Require(host.ExitCode == 0, "Learning host failed exit.");
            }
            finally { FinishProductHost(host); client.Dispose(); host.Dispose(); }
            var (restart, next) = StartAcceptanceHost(app, output);
            try
            {
                await next.Connect(); await next.Command("demo-start"); await next.Command("demo-ui|main|Ghost Tracking");
                var loaded = await Until(next, s => DemoTracking(s).ValueKind == JsonValueKind.Object && DemoTracking(s).GetProperty("WindowLoaded").GetBoolean());
                Require(DemoTracking(loaded).GetProperty("State").GetString() == "Off", "Restart activated source.");
                Require(Learning(loaded).GetProperty("Model").GetString() == model && Learning(loaded).GetProperty("Enabled").GetBoolean(),
                    "Fresh application process did not reload accepted model and preference.");
                await next.Command("learning-fixture|begin");
                var reused = Learning(await next.Command("status"));
                Require(Math.Abs(reused.GetProperty("Prediction").GetProperty("X").GetDouble() - learnedX) < .00001, "Reloaded numerical behavior changed.");
                double baseX = reused.GetProperty("Baseline").GetProperty("X").GetDouble();
                Require(Math.Abs(baseX - learnedX) > .01, "Correction did not affect preview.");
                await next.Command("demo-expand|tracking|Learning details");
                await next.Command("demo-ui|tracking|Restore previous model");
                var rolled = await Until(next, s => Learning(s).GetProperty("Model").ValueKind == JsonValueKind.Null);
                Require(Math.Abs(Learning(rolled).GetProperty("Prediction").GetProperty("X").GetDouble() - baseX) < .00001, "Rollback did not restore baseline.");
                await next.Command("demo-expand|tracking|Learning details");
                await next.Command("demo-ui|tracking|Restore previous model");
                await Until(next, s => Learning(s).GetProperty("Model").ValueKind == JsonValueKind.String &&
                    Math.Abs(Learning(s).GetProperty("Prediction").GetProperty("X").GetDouble() - learnedX) < .00001);
                await next.Command("demo-ui|tracking|Reset learning");
                await Until(next, s => !Learning(s).GetProperty("Enabled").GetBoolean() && Learning(s).GetProperty("Model").ValueKind == JsonValueKind.Null);
                await next.Command("demo-ui|main|Pocket");
                var pocket = await Until(next, s => ProductPocket(s).ValueKind == JsonValueKind.Object && ProductPocket(s).GetProperty("Count").GetInt32() == 2);
                Require(ProductPocket(pocket).GetProperty("Count").GetInt32() == 2, "Learning reset changed Pocket references.");
                await next.Command("learning-fixture|end"); await next.Command("demo-ui|main|Exit");
                await restart.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Require(restart.ExitCode == 0, "Restart cleanup failed.");
                return new { AcceptedModel = model, AcceptedRevision = revision, LearnedX = learnedX, FreshProcessReload = true,
                    Reversal = true, Reapply = true, Reset = true, PocketPreserved = true, CameraStarted = false };
            }
            finally { FinishProductHost(restart); next.Dispose(); restart.Dispose(); }
        });
    }
}
