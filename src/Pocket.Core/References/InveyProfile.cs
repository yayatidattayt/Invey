using Pocket.Core.Tracking;

namespace Pocket.Core.References;

public sealed record InveyProfile(string Root, string? Notice = null, bool Available = true, bool Writable = true)
{
    private const long CopyBudget = 512L * 1024 * 1024;

    /// <summary>Caller must hold the shared legacy/new application instance lock.</summary>
    public static InveyProfile Resolve(string localAppData)
    {
        string legacy = Path.GetFullPath(Path.Combine(localAppData, "PocketDualGhost"));
        string canonical = Path.GetFullPath(Path.Combine(localAppData, "Invey"));
        string staging = canonical + ".migration";
        var createdFiles = new List<string>();
        bool stageCreated = false, assetDirectoryCreated = false, legacyVerified = false;
        try
        {
            if (DirectoryExists(canonical))
                return new(canonical, Directory.Exists(legacy) ? "Using the Invey library. The separate legacy profile was preserved." : null);
            bool previous = legacyVerified = DirectoryExists(legacy);
            if (!previous) return new(canonical);
            if (DirectoryExists(staging))
                return new(legacy, "A previous profile migration was interrupted. The original profile is open read-only.", Writable: false);
            var store = new PocketStore(legacy);
            var library = store.Load();
            if (library.RecoveryNotice is not null)
                return new(legacy, "The legacy library needs recovery. The original profile is open read-only.", Writable: false);
            string settings = Path.Combine(legacy, "calibration.json");
            var preferences = new CalibrationPreferencesStore(settings);
            if (File.Exists(settings)) RequireRegularFile(settings);
            var loaded = preferences.Load();
            if (loaded.Unreadable)
                return new(legacy, "Legacy calibration could not be verified. The original profile is open read-only.", Writable: false);

            Directory.CreateDirectory(staging);
            stageCreated = true;
            long remaining = CopyBudget;
            var assetsToCopy = References(library).ToArray();
            if (assetsToCopy.Length > 0)
            {
                string assets = Path.Combine(staging, "assets");
                Directory.CreateDirectory(assets);
                assetDirectoryCreated = true;
                foreach (var item in assetsToCopy)
                {
                    var bytes = store.Open(item);
                    remaining -= bytes.Length;
                    if (remaining < 0) throw new IOException("Migration exceeds its copy budget.");
                    Write(Path.Combine(assets, item.Id.ToString("N") + ".png"), bytes);
                }
            }
            string index = Path.Combine(legacy, "manifest.json");
            if (File.Exists(index)) Write(Path.Combine(staging, "manifest.json"), ReadSmall(index, PocketStore.MaxManifestBytes));
            if (File.Exists(settings)) Write(Path.Combine(staging, "calibration.json"), ReadSmall(settings, 4096));
            var copy = new PocketStore(staging);
            var check = copy.Load();
            if (check.RecoveryNotice is not null || !library.Items.SequenceEqual(check.Items) ||
                !(library.Deleted ?? []).SequenceEqual(check.Deleted ?? []) ||
                loaded != new CalibrationPreferencesStore(Path.Combine(staging, "calibration.json")).Load())
                throw new InvalidDataException("Profile migration did not verify.");
            foreach (var item in References(check)) _ = copy.Open(item);
            Directory.Move(staging, canonical);
            stageCreated = false;
            return new(canonical, "Legacy references and calibration were copied and verified. The original profile was preserved.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or NotSupportedException)
        {
            string message = "Profile migration could not be verified. The original profile is open read-only.";
            if (stageCreated)
            {
                try
                {
                    foreach (string file in createdFiles) File.Delete(file);
                    if (assetDirectoryCreated) Directory.Delete(Path.Combine(staging, "assets"));
                    Directory.Delete(staging);
                }
                catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException)
                { message += " Staged files were preserved for inspection."; }
            }
            return legacyVerified ? new(legacy, message, Writable: false) : new(canonical, "Invey profile storage is unavailable.", false);
        }

        void Write(string path, byte[] bytes)
        {
            using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            createdFiles.Add(path);
            file.Write(bytes);
            file.Flush(flushToDisk: true);
        }
    }

    private static IEnumerable<PocketReference> References(PocketLibrary library) =>
        library.Items.Concat((library.Deleted ?? []).Select(row => row.Item));

    private static bool DirectoryExists(string path)
    {
        try
        {
            if ((File.GetAttributes(path) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != FileAttributes.Directory)
                throw new UnauthorizedAccessException("Expected a regular profile directory.");
            return true;
        }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
    }

    private static void RequireRegularFile(string path)
    {
        if ((File.GetAttributes(path) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            throw new UnauthorizedAccessException("Expected a regular profile file.");
    }

    private static byte[] ReadSmall(string path, int limit)
    {
        RequireRegularFile(path);
        using var file = File.OpenRead(path);
        if (file.Length is <= 0 || file.Length > limit) throw new InvalidDataException("Profile file size is unsupported.");
        var bytes = new byte[(int)file.Length];
        file.ReadExactly(bytes);
        return bytes;
    }
}
