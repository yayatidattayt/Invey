using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Pocket.Core.Tracking;

namespace Pocket.App.Tracking;

internal static class CameraReleaseRecovery
{
    // Explicit operator workflow only; never called automatically by Enable or startup.
    internal static int Run(string[] args)
    {
        if (args.Length != 5 || args[4] != "indicator-off-confirmed" || !Guid.TryParse(args[3], out var session)) return 2;
        try
        {
            string directory = Path.GetFullPath(args[2]);
            for (var d = new DirectoryInfo(directory); d is not null; d = d.Parent)
                if (d.Exists && d.Attributes.HasFlag(FileAttributes.ReparsePoint)) return 2;
            byte[] Read(string suffix)
            {
                string path = Path.Combine(directory, session + suffix);
                if (File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint)) throw new IOException("Linked diagnostic evidence.");
                using var f = File.OpenRead(path);
                if (f.Length is <= 0 or > 65536) throw new InvalidDataException("Invalid release evidence size.");
                byte[] bytes = new byte[(int)f.Length]; f.ReadExactly(bytes); return bytes;
            }
            byte[] hostBytes = Read(".host.json"), workerBytes = Read(".worker.json");
            using var host = JsonDocument.Parse(hostBytes); using var worker = JsonDocument.Parse(workerBytes);
            var h = host.RootElement; var w = worker.RootElement;
            int pid = w.GetProperty("pid").GetInt32();
            bool exists;
            try { using var process = Process.GetProcessById(pid); exists = !process.HasExited; }
            catch (ArgumentException) { exists = false; }
            var evidence = new CameraReleaseEvidence(h.GetProperty("Session").GetGuid(),
                w.GetProperty("session").GetGuid(), h.GetProperty("WorkerPid").GetInt32(), pid,
                h.GetProperty("ExitConfirmed").GetBoolean(), h.GetProperty("CurrentObservationCleared").GetBoolean(),
                h.GetProperty("State").GetString() ?? "", h.GetProperty("StartedUtc").GetDateTimeOffset(),
                DateTimeOffset.FromUnixTimeMilliseconds((long)(w.GetProperty("startedUnix").GetDouble() * 1000)),
                exists, true);
            if (evidence.Session != session || !evidence.IsSufficient) return 2;
            using var budget = new CameraBudget(args[1]);
            string receipt = Path.Combine(directory, session + ".release-review.json");
            using (var f = new FileStream(receipt, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(f, new { Session = session, Review = "Explicit indicator-off confirmation plus verified process exit",
                    NativeApiAcknowledgement = false, HostHash = Convert.ToHexString(SHA256.HashData(hostBytes)),
                    WorkerHash = Convert.ToHexString(SHA256.HashData(workerBytes)), ChargedMs = budget.SessionLimitMilliseconds,
                    ReviewedUtc = DateTimeOffset.UtcNow, QuotaRenewed = false });
                f.Flush(true);
            }
            budget.ConfirmIndependentlyVerifiedRelease(evidence);
            return 0;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or
            JsonException or InvalidOperationException or FormatException or OverflowException or KeyNotFoundException or ArgumentException)
        { Trace.TraceWarning("Explicit camera release review refused incomplete evidence."); return 2; }
    }
}
