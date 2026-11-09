namespace Pocket.Acceptance.Runner;

internal static partial class Program
{
    private static async Task RunPrecisionPreview(Client client)
    {
        await Measure("F19_VISUAL_CONTINUATION", "ACTUAL_WPF + SYNTHETIC_IMAGES + V3_WORKER",
            "Image-based index moves without model points, returns to full hand, hides on loss/ambiguity and releases on Stop", async () =>
        {
            try
            {
                await client.Command("tracking-close"); await client.Command("tracking-open");
                await Until(client,s=>Tracking(s).GetProperty("WindowLoaded").GetBoolean());
                await client.Command("tracking-enable-video-fixture");
                await client.Command("tracking-start-finger-visual");
                await Until(client,s=>State(s)=="Observing",10000);
                await client.Command("tracking-finger-preview-open");
                await Until(client,s=>Tracking(s).GetProperty("Calibration").GetProperty("PointerState").GetString()=="Stable",10000);
                var first=await Until(client,s=>Tracking(s).GetProperty("Calibration").GetProperty("PointerState").GetString()=="FingerVisual" &&
                    Tracking(s).GetProperty("Calibration").GetProperty("PointerMarkerVisible").GetBoolean(),4000);
                double x=Tracking(first).GetProperty("Calibration").GetProperty("PointerMarkerX").GetDouble();
                var moved=await Until(client,s=>Tracking(s).GetProperty("Calibration").GetProperty("PointerState").GetString()=="FingerVisual" &&
                    Tracking(s).GetProperty("Calibration").GetProperty("PointerMarkerX").GetDouble()>x+.2,1000);
                var panel=Tracking(moved).GetProperty("Calibration");
                Require(!panel.GetProperty("PointerActionEligible").GetBoolean() && panel.GetProperty("Practice").GetString()!="Held",
                    "Visual source acquired gesture authority.");
                Require(Tracking(moved).GetProperty("IndexX").ValueKind==System.Text.Json.JsonValueKind.Null,
                    "Visual point was mislabeled as a model landmark.");
                await Until(client,s=>Tracking(s).GetProperty("Calibration").GetProperty("PointerState").GetString()=="Stable",2000);
                await Until(client,s=>State(s)=="Lost" && !Tracking(s).GetProperty("Calibration").GetProperty("PointerMarkerVisible").GetBoolean(),2000);
                await Until(client,s=>State(s)=="Ambiguous",2000);
                await Until(client,s=>Tracking(s).GetProperty("Calibration").GetProperty("PointerState").GetString()=="Stable",2000);
                int pid=Tracking(moved).GetProperty("LastWorkerPid").GetInt32();
                await client.Command("tracking-stop");
                await Until(client,s=>State(s)=="Stopped");
                Require(!ProcessExists(pid),"Visual worker survived Stop.");
                await client.Command("tracking-start-finger-visual");
                await Until(client,s=>State(s)=="Observing",10000);
                await client.Command("tracking-finger-preview-open");
                var fallback=await Until(client,s=>Tracking(s).GetProperty("Calibration").GetProperty("PointerState").GetString()=="FingerVisual",4000);
                int restartedPid=Tracking(fallback).GetProperty("LastWorkerPid").GetInt32();
                await client.Command("tracking-stop");
                var ended=await Until(client,s=>State(s)=="Stopped");
                Require(!ProcessExists(restartedPid) && !Tracking(ended).GetProperty("Calibration").GetProperty("PointerMarkerVisible").GetBoolean(),
                    "Stop during visual fallback left a marker or worker.");
                return new { ImageBasedMovement=true, NoInventedModelPoints=true, GestureAuthority=false, Reacquired=true,
                    LostHidden=true, AmbiguityCanceled=true, WorkerExited=true, CameraOpened=false };
            }
            finally { await client.Command("tracking-stop"); await client.Command("tracking-close"); }
        });
    }
}
