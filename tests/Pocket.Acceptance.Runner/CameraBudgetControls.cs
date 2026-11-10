using System.Text.Json;

namespace Pocket.Acceptance.Runner;

internal static partial class Program
{
    private static async Task RunCameraBudgetControls(Client client)
    {
        await Measure("CAMERA_DURABLE_SUPERVISOR", "INDEPENDENT_PROCESS + SYNTHETIC_NON_CAMERA_WORKER",
            "Six durable permits across guardian processes, rejected seventh, hard timeout, interrupted supervisor and invalid configuration fail closed", async () =>
        {
            var outcomes = new List<JsonElement>();
            for (int i = 0; i < 7; i++)
            {
                var state = await client.Command("budget-series");
                var result = state.GetProperty("BudgetResult");
                outcomes.Add(result);
                bool permit = result.GetProperty("Permit").GetBoolean();
                Require(permit == (i < 6), $"Reservation {i + 1} had unexpected permission: " + result.GetRawText());
                Require(result.GetProperty("WorkerExited").GetBoolean(), "A budget double remained active.");
                Require(result.GetProperty("Started").GetBoolean() == permit, "A denied synthetic worker received a start signal.");
                if (permit) Require(result.GetProperty("ReservationReleased").GetBoolean(), "Early stop did not complete a reservation.");
            }
            for (int i = 0; i < 13; i++)
            {
                var state = await client.Command("budget-amended-series");
                var result = state.GetProperty("BudgetResult");
                outcomes.Add(result);
                Require(result.GetProperty("Permit").GetBoolean() == (i < 12), "Amended session-count enforcement failed.");
                Require(result.GetProperty("WorkerExited").GetBoolean(), "Amended worker remained active.");
            }
            for (int i = 0; i < 3; i++)
            {
                var state = await client.Command("budget-targets-series");
                var result = state.GetProperty("BudgetResult");
                outcomes.Add(result);
                Require(result.GetProperty("Permit").GetBoolean() == (i < 2),
                    "Target-lesson extension must grant exactly two further starts across supervisor processes.");
                Require(result.GetProperty("WorkerExited").GetBoolean(), "Target-lesson worker remained active.");
                Require(result.GetProperty("Started").GetBoolean() == (i < 2), "Denied target-lesson worker started.");
                Require(result.GetProperty("ReservationReleased").GetBoolean(), "Target-lesson release not confirmed.");
            }
            foreach (string mode in new[] { "performance-series", "performance-followup" })
            for (int i=0;i<2;i++)
            {
                var state=await client.Command("budget-"+mode);
                var result=state.GetProperty("BudgetResult");outcomes.Add(result);
                Require(result.GetProperty("Permit").GetBoolean()==(i==0),"Performance stage renewed or bypassed its separate permission gate.");
                Require(result.GetProperty("Started").GetBoolean()==(i==0)&&result.GetProperty("WorkerExited").GetBoolean(),"Performance guardian worker lifecycle failed.");
                Require(result.GetProperty("ReservationReleased").GetBoolean(),"Performance synthetic reservation release failed.");
            }
            foreach (string mode in new[] { "missing", "corrupt", "directory", "canceled", "deadline", "interrupted" })
            {
                var state = await client.Command("budget-" + mode);
                var result = state.GetProperty("BudgetResult");
                outcomes.Add(result);
                Require(result.GetProperty("WorkerExited").GetBoolean(), "The independent guard leaked its worker: " + mode);
                if (mode is "deadline" or "interrupted")
                    Require(result.GetProperty("Permit").GetBoolean() &&
                        result.GetProperty("ReservationReleased").ValueKind == JsonValueKind.False,
                        "An unclean worker received a budget refund: " + result.GetRawText());
                else
                    Require(!result.GetProperty("Permit").GetBoolean() && !result.GetProperty("Started").GetBoolean(),
                        "Invalid configuration or startup cancellation permitted activation: " + result.GetRawText());
            }
            foreach (string mode in new[] { "pending", "exhausted", "performance" })
            {
                var state = await client.Command("budget-ui-" + mode);
                var lab = state.GetProperty("Tracking");
                Require(lab.GetProperty("DeviceSelected").GetBoolean(), "Budget fixture must have a selected dummy device.");
                Require(!lab.GetProperty("CanStart").GetBoolean(), "Blocked budget offered an enabled Start button.");
                Require(lab.GetProperty("LastWorkerPid").ValueKind == JsonValueKind.Null, "Budget preflight launched a worker.");
                string message = lab.GetProperty("BudgetStatus").GetString() ?? "";
                Require(message.Contains(mode == "pending" ? "verification" : mode == "performance" ? "Choose raw" : "allowance used"), "Budget reason was not specific: " + message);
                if(mode=="performance")
                {
                    var selected=await client.Command("budget-ui-choose-capture");
                    var configured=selected.GetProperty("Tracking");
                    Require(configured.GetProperty("CanStart").GetBoolean(),"Approved raw mode did not enable the explicit user start.");
                    Require(configured.GetProperty("LastWorkerPid").ValueKind==JsonValueKind.Null,"Choosing diagnostic mode activated a worker.");
                }
            }
            return new { Outcomes = outcomes, RealCameraActivations = 0, BudgetUiPreflight = true };
        });
    }
}
