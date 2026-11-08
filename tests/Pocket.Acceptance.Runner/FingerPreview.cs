using System.Text.Json;

namespace Pocket.Acceptance.Runner;

internal static partial class Program
{
    private static async Task RunFingerPreview(Client client)
    {
        await Measure("F16_PARTIAL_FINGER_PREVIEW", "WPF + V2_SYNTHETIC_WORKER",
            "A real preview marker moves with the index despite a missing wrist, hides on missing index and ambiguity, and Stop exits the worker", async () =>
        {
            try
            {
            var protocol = await client.Command("preview-protocol-controls");
            Require(protocol.GetProperty("PreviewResult").GetProperty("RejectedHeaders").GetInt32() == 7 &&
                protocol.GetProperty("PreviewResult").GetProperty("UnconnectedStop").GetBoolean() &&
                protocol.GetProperty("PreviewResult").GetProperty("BoundedFrameMatch").GetBoolean(),
                "Preview protocol controls failed.");
            await client.Command("tracking-close");
            var opened = await client.Command("tracking-open");
            await Until(client, s => Tracking(s).GetProperty("WindowLoaded").GetBoolean());
            await client.Command("tracking-enable-video-fixture");
            await client.Command("tracking-start-finger-preview");
            await Until(client, s => State(s) == "Observing", 10000);
            await client.Command("tracking-finger-preview-open");
            var nativeCursor = Input.Cursor();
            var video = await Until(client, s => Tracking(s).GetProperty("VideoPreview").GetProperty("FrameVisible").GetBoolean() &&
                Tracking(s).GetProperty("VideoPreview").GetProperty("VisiblePoints").GetInt32() > 0, 3000);
            Require(Tracking(video).GetProperty("VideoPreview").GetProperty("Sequence").GetInt64() ==
                Tracking(video).GetProperty("VideoPreview").GetProperty("OverlaySequence").GetInt64(), "Overlay used a different frame.");
            var partial = await Until(client, s =>
            {
                var panel = Tracking(s).GetProperty("Calibration");
                return State(s) == "Partial" && panel.GetProperty("PointerState").GetString() == "Partial" &&
                    panel.GetProperty("PointerMarkerVisible").GetBoolean();
            }, 4000);
            var first = Tracking(partial).GetProperty("Calibration").GetProperty("PointerMarkerX").GetDouble();
            var moved = await Until(client, s =>
            {
                var panel = Tracking(s).GetProperty("Calibration");
                return State(s) == "Partial" && panel.GetProperty("PointerPosition").ValueKind == JsonValueKind.Object &&
                    panel.GetProperty("PointerMarkerX").GetDouble() > first + .2;
            }, 2000);
            var partialPanel = Tracking(moved).GetProperty("Calibration");
            Require(!partialPanel.GetProperty("PointerActionEligible").GetBoolean() &&
                partialPanel.GetProperty("Practice").GetString() != "Held", "Partial preview gained action authority.");
            await Until(client, s => State(s) == "Partial" && Tracking(s).GetProperty("IndexX").ValueKind == JsonValueKind.Null &&
                Tracking(s).GetProperty("Calibration").GetProperty("PointerPosition").ValueKind == JsonValueKind.Null, 3000);
            await Until(client, s => State(s) == "Ambiguous" &&
                Tracking(s).GetProperty("Calibration").GetProperty("PointerState").GetString() == "Ambiguous", 3000);
            await Until(client, s => Tracking(s).GetProperty("Calibration").GetProperty("PointerState").GetString() == "Stable", 3000);
            int pid = Tracking(moved).GetProperty("LastWorkerPid").GetInt32();
            var finalCursor = Input.Cursor();
            Require(finalCursor.X == nativeCursor.X && finalCursor.Y == nativeCursor.Y,
                $"Cursor changed during the preview check: before={nativeCursor.X},{nativeCursor.Y}; after={finalCursor.X},{finalCursor.Y}. Attribution requires independent evidence.");
            await client.Command("tracking-stop");
            var stopped = await Until(client, s => State(s) == "Stopped");
            Require(!ProcessExists(pid) &&
                !Tracking(stopped).GetProperty("VideoPreview").GetProperty("FrameVisible").GetBoolean() &&
                !Tracking(stopped).GetProperty("Calibration").GetProperty("PointerMarkerVisible").GetBoolean(),
                "Stop left a pointer marker or worker.");
            await client.Command("tracking-close");
            return new { LiveRgbAssigned = true, MatchingSkeleton = true, RejectedPreviewHeaders = 7, PartialMarkerMoved = true, NoPartialActions = true, MissingIndexHidden = true,
                AmbiguityCanceled = true, ReacquisitionStabilized = true, WorkerExited = true, CameraOpened = false };
            }
            finally
            {
                await client.Command("tracking-stop");
                await client.Command("tracking-close");
            }
        });
    }
}
