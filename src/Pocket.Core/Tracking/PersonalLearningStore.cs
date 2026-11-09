using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pocket.Core.Tracking;

public sealed record LearningProfile(int Version, long Revision, bool Enabled, bool UsePersonal,
    PersonalModel? Current, PersonalModel? Previous, LearningSummary? Summary = null, LearningEvaluation? Evaluation = null)
{
    public static LearningProfile Empty => new(1, 0, false, true, null, null);
    [JsonIgnore] public bool IsValid => Version == 1 && Revision >= 0 &&
        (Current is null || Current.IsValid) && (Previous is null || Previous.IsValid) && (Summary is null || Summary.IsValid) && (Evaluation is null || Evaluation.IsValid);
}
public sealed record LearningLoad(LearningProfile Profile, bool Writable, string Notice);
public sealed class PersonalLearningStore(string directory, bool writable = true)
{
    private readonly string _directory = Path.GetFullPath(directory);
    private string PathName => Path.Combine(_directory, "personal.json");
    private sealed record Envelope(int Schema, string Payload, string Checksum);
    private static readonly JsonSerializerOptions Schema = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, AllowDuplicateProperties = false,
        RespectNullableAnnotations = true, RespectRequiredConstructorParameters = true
    };
    public LearningLoad Load()
    {
        try
        {
            RejectLinks();
            if (!File.Exists(PathName)) return new(LearningProfile.Empty, writable, "Learning disabled. No personal model.");
            return new(Read(PathName), writable, "Personal settings loaded. Camera remains off.");
        }
        catch (Exception ex) when (Expected(ex))
        { return new(LearningProfile.Empty, false, "Learning profile unreadable or incompatible. Defaults in use; original preserved. Reset learning to recover."); }
    }
    public LearningProfile Save(LearningProfile profile, long expectedRevision)
    {
        if (!writable) throw new UnauthorizedAccessException("Profile is read-only.");
        if (!profile.IsValid) throw new InvalidDataException("Invalid personal model.");
        RejectLinks();
        Directory.CreateDirectory(_directory);
        using var gate = new FileStream(PathName + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var old = File.Exists(PathName) ? Read(PathName) : LearningProfile.Empty;
        if (old.Revision != expectedRevision) throw new IOException("Learning profile changed in another window. Reopen Tracking.");
        var next = profile with { Revision = checked(expectedRevision + 1) };
        Write(next);
        return next;
    }
    // Explicit recovery/reset: only this directory's three known files. Never enumerate or touch Pocket.
    public LearningProfile Reset()
    {
        if (!writable) throw new UnauthorizedAccessException("Profile is read-only.");
        RejectLinks();
        Directory.CreateDirectory(_directory);
        using var gate = new FileStream(PathName + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        // Preserve a future schema rather than destroy data from a newer app.
        if (File.Exists(PathName))
        {
            try
            {
                using var input = File.OpenRead(PathName);
                if (input.Length > 65536) throw new InvalidDataException("Oversized profile preserved. Manual recovery required.");
                using var doc = JsonDocument.Parse(input);
                if (doc.RootElement.TryGetProperty("Schema", out var s) && s.TryGetInt32(out int v) && v > 1)
                    throw new NotSupportedException("A newer learning schema is preserved. Use a compatible application.");
            }
            catch (JsonException) { }
        }
        long revision = DateTime.UtcNow.Ticks;
        Write(LearningProfile.Empty with { Revision = revision });
        if (File.Exists(PathName + ".bak")) File.Delete(PathName + ".bak");
        return LearningProfile.Empty with { Revision = revision };
    }
    public LearningProfile RestoreBackup(long expectedRevision)
    {
        if (!writable) throw new UnauthorizedAccessException("Profile is read-only.");
        RejectLinks();
        var backup = Read(PathName + ".bak");
        return Save(backup, expectedRevision);
    }
    private static LearningProfile Read(string path)
    {
        using var f = File.OpenRead(path);
        if (f.Length is <= 0 or > 65536) throw new InvalidDataException("Invalid learning size.");
        var e = JsonSerializer.Deserialize<Envelope>(f, Schema) ?? throw new InvalidDataException("Missing envelope.");
        if (e.Schema != 1 || e.Payload is null || e.Checksum != Hash(e.Payload))
            throw new InvalidDataException("Learning checksum or schema mismatch.");
        var p = JsonSerializer.Deserialize<LearningProfile>(e.Payload, Schema);
        return p is { IsValid: true } ? p : throw new InvalidDataException("Invalid learning values.");
    }
    private void Write(LearningProfile value)
    {
        string payload = JsonSerializer.Serialize(value, Schema);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new Envelope(1, payload, Hash(payload)), Schema);
        if (bytes.Length > 65536) throw new InvalidDataException("Learning profile too large.");
        string stage = PathName + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var f = new FileStream(stage, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { f.Write(bytes); f.Flush(true); }
            RejectLinks();
            if (File.Exists(PathName)) File.Replace(stage, PathName, PathName + ".bak");
            else File.Move(stage, PathName);
        }
        finally { if (File.Exists(stage)) File.Delete(stage); }
    }
    private static string Hash(string text) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)));
    private void RejectLinks()
    {
        for (var d = new DirectoryInfo(_directory); d is not null; d = d.Parent)
            if (d.Exists && d.Attributes.HasFlag(FileAttributes.ReparsePoint))
                throw new UnauthorizedAccessException("Linked learning directories are unsupported.");
        foreach (var path in new[] { PathName, PathName + ".bak", PathName + ".lock" })
            if (File.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
                throw new UnauthorizedAccessException("Linked learning files are unsupported.");
    }
    public static bool Expected(Exception ex) => ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or
        NotSupportedException or ArgumentException or OverflowException;
}
