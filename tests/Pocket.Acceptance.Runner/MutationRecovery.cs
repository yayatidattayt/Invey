using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Pocket.Automation.Client;
using Pocket.Automation.Contracts;

namespace Pocket.Acceptance.Runner;

internal static partial class Program
{
    private static async Task RunMutationRecovery(Client hostClient, Process host, string output)
    {
        string worker = Path.GetFullPath("src/Pocket.Automation.Worker/bin/Release/net10.0-windows/Pocket.Automation.Worker.dll");
        await Measure("A2_MUTATION_LATE_EFFECT", "NATIVE_SOFTWARE_TEST", "A real provider can commit after the worker has exited", async () =>
        {
            var outcomes = new List<object>();
            foreach (var (pattern, mode) in new[] { ("Range", "before"), ("Range", "after"), ("Toggle", "before"), ("Scroll", "before") })
            {
                string directory = Path.Combine(output, "mutation-" + pattern + "-" + mode);
                Directory.CreateDirectory(directory);
                var info = new ProcessStartInfo(Path.GetFullPath("src/Pocket.App/bin/Release/net10.0-windows/Pocket.App.exe"))
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                info.Environment["DOTNET_ROOT"] = Path.GetDirectoryName(Environment.ProcessPath)!;
                info.ArgumentList.Add("--adverse-provider");
                info.ArgumentList.Add(directory);
                using var provider = Process.Start(info) ?? throw new IOException("Could not launch mutation fixture.");
                string Marker(string name) => Path.Combine(directory, name);
                try
                {
                    await WaitMarkerAsync(Marker("ready"));
                    long window = long.Parse(await File.ReadAllTextAsync(Marker("ready")));
                    await using var automation = new WorkerClient(Environment.ProcessPath!, worker, provider.Id, 1400);
                    var probe = new Request(1, Guid.NewGuid().ToString(), "Probe", window, provider.Id, "GhostFixture." + pattern);
                    var capability = await automation.RequestAsync(probe);
                    Require(capability.Binding?.Before == (pattern == "Range" ? 10 : 0), "The original state was not captured.");
                    if (pattern == "Range" && mode == "before")
                    {
                        File.WriteAllText(Marker("mutation-slow-validation"), "");
                        var expired = await automation.RequestAsync(probe with { Id = Guid.NewGuid().ToString(), Operation = "Execute", Token = capability.Token, Value = 42 });
                        Require(expired.Outcome == MutationOutcome.Conflict && !File.Exists(Marker("mutation-calls")),
                            "Slow validation allowed an expired mutation to dispatch.");
                        File.Delete(Marker("mutation-slow-validation"));
                        probe = probe with { Id = Guid.NewGuid().ToString() };
                        capability = await automation.RequestAsync(probe);
                        Require(capability.Binding?.Before == 10, "Fresh probe after expiry was unavailable.");
                    }
                    var action = probe with { Id = Guid.NewGuid().ToString(), Operation = "Execute", Token = capability.Token, Value = pattern == "Range" ? 42 : null };
                    using var canceled = new CancellationTokenSource();
                    canceled.Cancel();
                    Require((await automation.RequestAsync(action, canceled.Token)).Outcome == MutationOutcome.Rejected, "Pre-dispatch cancellation was ambiguous.");
                    File.WriteAllText(Marker("mutation-arm"), mode);
                    if (mode == "after") File.WriteAllText(Marker("mutation-concurrent"), "");
                    int workerPid = automation.ActiveProcessId ?? throw new IOException("Missing mutation worker.");
                    var watch = Stopwatch.StartNew();
                    var pending = automation.RequestAsync(action);
                    await WaitMarkerAsync(Marker("mutation-entered"));
                    var heartbeat = Stopwatch.StartNew();
                    var status = await hostClient.Command("status");
                    double hostReplyMs = heartbeat.Elapsed.TotalMilliseconds;
                    Require(status.GetProperty("GhostClaim").ValueKind == System.Text.Json.JsonValueKind.Null, "External action leaked a Ghost claim.");
                    var uncertain = await pending;
                    Require(uncertain.Outcome == MutationOutcome.Unknown && uncertain.Status == "Timeout", "Mutation timeout was misclassified.");
                    Require(watch.ElapsedMilliseconds < 3500 && !ProcessExists(workerPid), "Mutation worker teardown was unbounded.");
                    bool appliedWhenWorkerExited = File.Exists(Marker("mutation-applied"));
                    Require(appliedWhenWorkerExited == (mode == "after"), "Fixture did not enter the expected commit phase.");
                    Require((await automation.RequestAsync(action)).Status == "Denied", "Duplicate mutation ID was accepted.");
                    Require((await automation.RequestAsync(action with { Id = Guid.NewGuid().ToString() })).Status == "Busy", "Pending uncertainty allowed another mutation.");

                    using var observationCancellation = new CancellationTokenSource(300);
                    var interrupted = await automation.ReconcileAsync(action.Id, observationCancellation.Token);
                    Require(interrupted.Status == "Canceled" && interrupted.Observation == Observation.Inconclusive, "Canceled observation was not contained.");
                    if (mode == "before" && pattern == "Range")
                    {
                        var timed = await automation.ReconcileAsync(action.Id);
                        Require(timed.Status == "Timeout" && timed.Observation == Observation.Inconclusive, "Observation deadline was not enforced.");
                    }
                    File.WriteAllText(Marker("mutation-release"), "");
                    await WaitMarkerAsync(Marker("mutation-applied"));
                    var observed = await automation.ReconcileAsync(action.Id);
                    var expected = mode == "before" ? Observation.ExpectedObserved : Observation.StateConflict;
                    Require(observed.Id == action.Id && observed.Observation == expected && observed.Outcome == MutationOutcome.Unknown,
                        "A read-only observation incorrectly resolved the original mutation: " + observed);
                    Require(File.ReadAllLines(Marker("mutation-calls")).Length == 1, "The provider received a duplicate mutation.");

                    if (mode != "before" || pattern != "Range")
                    {
                        provider.Kill();
                        Require(provider.WaitForExit(3000), "Mutation provider did not exit.");
                        Require((await automation.ReconcileAsync(action.Id)).Observation == Observation.TargetUnavailable, "Exited target remained observable.");
                    }
                    Require((await automation.ReconcileAsync(action.Id)).Status == "Denied", "Observation budget was exceeded.");
                    Require(await automation.DismissAsync(action.Id), "Dismiss failed.");
                    outcomes.Add(new { Pattern = pattern, Mode = mode, AppliedWhenWorkerExited = appliedWhenWorkerExited, uncertain.Outcome, Readback = observed.Observation, observed.Value, Calls = 1, HostReplyMs = hostReplyMs, automation.Restarts });
                }
                finally
                {
                    if (!provider.HasExited) provider.Kill();
                    Require(provider.WaitForExit(3000), "Mutation provider leaked.");
                }
            }
            return outcomes;
        });

        await Measure("A2_WORKER_QUARANTINE", "DETERMINISTIC_TEST", "Termination failure retains the old worker and blocks replacement", async () =>
        {
            var fixture = await hostClient.Command("status");
            string? previous = Environment.GetEnvironmentVariable("GHOST_ACCEPTANCE_WORKER_FAULT");
            bool allowTermination = false;
            int pid;
            string lateMarker = Path.Combine(output, "late-acknowledgement");
            string? previousMarker = Environment.GetEnvironmentVariable("GHOST_ACCEPTANCE_LATE_ACK");
            Environment.SetEnvironmentVariable("GHOST_ACCEPTANCE_WORKER_FAULT", "execute-late");
            Environment.SetEnvironmentVariable("GHOST_ACCEPTANCE_LATE_ACK", lateMarker);
            await using var automation = new WorkerClient(Environment.ProcessPath!, typeof(Program).Assembly.Location, host.Id, 600, process =>
            {
                if (!allowTermination) throw new Win32Exception(5);
                process.Kill();
            });
            try
            {
                var probe = new Request(1, Guid.NewGuid().ToString(), "Probe", fixture.GetProperty("AutomationHandle").GetInt64(), host.Id, "GhostFixture.Invoke");
                var capability = await automation.RequestAsync(probe);
                pid = automation.ActiveProcessId ?? throw new IOException("Missing fault worker.");
                var action = probe with { Id = Guid.NewGuid().ToString(), Operation = "Execute", Token = capability.Token };
                var result = await automation.RequestAsync(action);
                Require(result.Outcome == MutationOutcome.Unknown && automation.ActiveProcessId == pid, "Failed kill lost the unresolved result or process ownership.");
                await WaitMarkerAsync(lateMarker);
                var check = await automation.ReconcileAsync(action.Id);
                Require(check.Status == "Unavailable" && automation.Restarts == 1 && automation.ActiveProcessId == pid, "A replacement worker started prematurely.");
                allowTermination = true;
                // The next double exits on Observe, exercising reconciliation transport failure in a fresh generation.
                var lost = await automation.ReconcileAsync(action.Id);
                Require(lost.Status == "Unavailable" && lost.Outcome == MutationOutcome.Unknown && automation.Restarts == 2 && !ProcessExists(pid),
                    "Recovery after confirmed worker exit was not contained.");
                return new { RetainedAfterKillFailure = true, LateAcknowledgementIgnored = true, ReplacementStartedAfterExit = true, ObservationExit = lost.Status };
            }
            finally
            {
                allowTermination = true;
                Environment.SetEnvironmentVariable("GHOST_ACCEPTANCE_WORKER_FAULT", previous);
                Environment.SetEnvironmentVariable("GHOST_ACCEPTANCE_LATE_ACK", previousMarker);
            }
        });

        await Measure("A2_RECOVERY_UI", "NATIVE_SOFTWARE_TEST", "Fixture recovery commands present uncertainty and require deliberate dismissal", async () =>
        {
            await hostClient.Command("automation-ignore-range");
            await hostClient.Command("recovery-open");
            var unresolved = (await hostClient.Command("recovery-act")).GetProperty("Recovery");
            Require(!unresolved.GetProperty("CanApply").GetBoolean() && unresolved.GetProperty("CanCheck").GetBoolean() &&
                unresolved.GetProperty("Text").GetString()!.Contains("could not be confirmed"), "Unknown outcome was not presented.");
            var checkedState = (await hostClient.Command("recovery-check")).GetProperty("Recovery");
            Require(checkedState.GetProperty("Observation").GetString() == "PriorStateObserved", "Recovery view failed to read the original target.");
            var dismissed = (await hostClient.Command("recovery-dismiss")).GetProperty("Recovery");
            Require(dismissed.GetProperty("CanApply").GetBoolean() && !dismissed.GetProperty("CanCheck").GetBoolean(), "Dismiss did not restore deliberate action.");
            return new { Unresolved = unresolved, Checked = checkedState, Dismissed = dismissed };
        });
    }

    private static async Task WaitMarkerAsync(string path)
    {
        var watch = Stopwatch.StartNew();
        while (!File.Exists(path) && watch.Elapsed < TimeSpan.FromSeconds(4)) await Task.Delay(10);
        Require(File.Exists(path), "Missing fixture marker: " + Path.GetFileName(path));
    }
}
