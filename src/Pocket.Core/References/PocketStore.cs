using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pocket.Core.References;

public sealed record PocketReference([property: JsonRequired] Guid Id, [property: JsonRequired] DateTimeOffset CreatedUtc,
    [property: JsonRequired] int Width, [property: JsonRequired] int Height, [property: JsonRequired] string Sha256);
public sealed record PocketLibrary(IReadOnlyList<PocketReference> Items, string? RecoveryNotice = null);

public sealed class PocketStore(string path)
{
    public const int MaxPixels = 16_777_216;
    public const int MaxDimension = 8192;
    public const int MaxAssetBytes = 64 * 1024 * 1024;
    private const int MaxReferences = 200;
    private const int MaxManifestBytes = 128 * 1024;
    private const long RecoveryByteBudget = 256L * 1024 * 1024;
    private readonly string _root = Path.GetFullPath(path);
    private readonly object _gate = new();
    private static readonly JsonSerializerOptions Schema = new()
    {
        AllowDuplicateProperties = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    private sealed record Manifest([property: JsonRequired] int Version, [property: JsonRequired] PocketReference[] Items);
    private string Index => Path.Combine(_root, "manifest.json");
    private string Backup => Path.Combine(_root, "manifest.backup.json");
    private string Assets => Path.Combine(_root, "assets");
    private string Asset(Guid id) => Path.Combine(Assets, id.ToString("N") + ".png");

    public static bool SupportsSize(int width, int height) =>
        width is > 0 and <= MaxDimension && height is > 0 and <= MaxDimension && (long)width * height <= MaxPixels;

    public PocketLibrary Load()
    {
        lock (_gate)
        {
            VerifyDirectories();
            PocketReference[]? indexed = null;
            try
            {
                if (Exists(Index)) indexed = ReadManifest(Index).Items;
                else if (!Exists(Backup) && !AssetPaths().Any()) return new([]);
            }
            catch (NotSupportedException)
            {
                return new([], "This library uses a newer format. Existing files were preserved.");
            }
            catch (InvalidDataException) { /* Recover read-only below. */ }
            var recovered = indexed?.ToDictionary(item => item.Id) ?? new Dictionary<Guid, PocketReference>();
            try
            {
                if (indexed is null && Exists(Backup))
                    foreach (var item in ReadManifest(Backup).Items) recovered.Add(item.Id, item);
            }
            catch (Exception ex) when (ex is InvalidDataException or NotSupportedException) { }
            long remaining = RecoveryByteBudget;
            bool unindexed = false;
            foreach (var file in AssetPaths().Take(MaxReferences + 1))
            {
                if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(file), "N", out var id) ||
                    id == Guid.Empty || recovered.ContainsKey(id)) continue;
                unindexed = true;
                if (recovered.Count == MaxReferences || remaining <= 0) break;
                try
                {
                    var bytes = ReadBounded(file, (int)Math.Min(MaxAssetBytes, remaining));
                    remaining -= bytes.Length;
                    var (width, height) = Dimensions(bytes);
                    recovered[id] = new(id, File.GetLastWriteTimeUtc(file), width, height, Digest(bytes));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException) { }
            }
            if (indexed is not null && !unindexed) return new(indexed);
            return new(recovered.Values.OrderByDescending(item => item.CreatedUtc).ToArray(),
                "The library needs recovery. Available references are shown read-only. Original files were preserved.");
        }
    }

    public PocketReference Save(Guid id, byte[] approvedCrop)
    {
        if (id == Guid.Empty) throw new ArgumentException("A reference ID is required.", nameof(id));
        var (width, height) = Dimensions(approvedCrop);
        var item = new PocketReference(id, DateTimeOffset.UtcNow, width, height, Digest(approvedCrop));
        lock (_gate)
        {
            VerifyDirectories();
            bool indexed = Exists(Index);
            if (!indexed && (Exists(Backup) || AssetPaths().Any(file => !file.Equals(Asset(id), StringComparison.OrdinalIgnoreCase))))
                throw new InvalidDataException("The library needs recovery before new references can be saved.");
            var current = indexed ? ReadManifest(Index).Items : [];
            if (current.FirstOrDefault(row => row.Id == id) is { } existing)
            {
                if (existing.Sha256 != item.Sha256) throw new InvalidDataException("Reference identity conflict.");
                _ = Open(existing);
                return existing;
            }
            if (current.Length >= MaxReferences) throw new InvalidDataException("The library has reached 200 references.");
            Directory.CreateDirectory(Assets);
            VerifyDirectories();
            var asset = Asset(id);
            if (Exists(asset))
            {
                if (Digest(ReadBounded(asset, MaxAssetBytes)) != item.Sha256)
                    throw new InvalidDataException("Reference asset conflict.");
            }
            else
            {
                WriteAtomic(asset, approvedCrop, temporary =>
                {
                    if (Digest(ReadBounded(temporary, MaxAssetBytes)) != item.Sha256)
                        throw new IOException("Asset verification failed.");
                    File.Move(temporary, asset);
                });
            }
            byte[] manifest = JsonSerializer.SerializeToUtf8Bytes(new Manifest(1, [.. current, item]), Schema);
            WriteAtomic(Index, manifest, temporary =>
            {
                if (!indexed) File.Move(temporary, Index);
                else
                {
                    _ = Exists(Backup);
                    File.Replace(temporary, Index, Backup);
                }
            });
            return item;
        }
    }

    public byte[] Open(PocketReference item)
    {
        VerifyDirectories();
        var bytes = ReadBounded(Asset(item.Id), MaxAssetBytes);
        if (Dimensions(bytes) != (item.Width, item.Height) || Digest(bytes) != item.Sha256)
            throw new InvalidDataException("The saved image is damaged.");
        return bytes;
    }

    private Manifest ReadManifest(string file)
    {
        var bytes = ReadBounded(file, MaxManifestBytes);
        Manifest manifest;
        try
        {
            using var document = JsonDocument.Parse(bytes);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("Version", out var version) && version.TryGetInt32(out int number) && number > 1)
                throw new NotSupportedException("This library uses a newer format.");
            manifest = JsonSerializer.Deserialize<Manifest>(bytes, Schema) ?? throw new InvalidDataException("Empty index.");
        }
        catch (JsonException ex) { throw new InvalidDataException("The library index is damaged.", ex); }
        if (manifest.Version != 1 || manifest.Items is null || manifest.Items.Length > MaxReferences ||
            manifest.Items.Any(item => item is null || item.Id == Guid.Empty || !SupportsSize(item.Width, item.Height) ||
                item.Sha256 is not { Length: 64 } || !item.Sha256.All(Uri.IsHexDigit)) ||
            manifest.Items.Select(item => item.Id).Distinct().Count() != manifest.Items.Length)
            throw new InvalidDataException("The library index is damaged.");
        return manifest;
    }

    private void VerifyDirectories()
    {
        foreach (var directory in new[] { _root, Assets })
            if (Exists(directory) && (File.GetAttributes(directory) & FileAttributes.Directory) == 0)
                throw new IOException("A Pocket storage directory is unavailable.");
    }

    private IEnumerable<string> AssetPaths() => Exists(Assets) ? Directory.EnumerateFiles(Assets, "*.png") : [];

    private static bool Exists(string path)
    {
        try
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new UnauthorizedAccessException("Linked Pocket storage paths are unsupported.");
            return true;
        }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
    }

    private static byte[] ReadBounded(string path, int max)
    {
        _ = Exists(path);
        using var file = File.OpenRead(path);
        if (file.Length is <= 0 || file.Length > max) throw new InvalidDataException("File size is unsupported.");
        var bytes = new byte[(int)file.Length];
        file.ReadExactly(bytes);
        return bytes;
    }

    private static void WriteAtomic(string destination, byte[] bytes, Action<string> commit)
    {
        string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        bool created = false;
        try
        {
            using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                created = true;
                file.Write(bytes);
                file.Flush(flushToDisk: true);
            }
            commit(temporary);
        }
        finally { if (created) File.Delete(temporary); }
    }

    private static (int Width, int Height) Dimensions(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is < 45 or > MaxAssetBytes ||
            !bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ||
            BinaryPrimitives.ReadInt32BigEndian(bytes[8..]) != 13 || !bytes[12..16].SequenceEqual("IHDR"u8) ||
            !bytes[^8..^4].SequenceEqual("IEND"u8))
            throw new InvalidDataException("Expected a bounded PNG image.");
        int width = BinaryPrimitives.ReadInt32BigEndian(bytes[16..]);
        int height = BinaryPrimitives.ReadInt32BigEndian(bytes[20..]);
        if (!SupportsSize(width, height)) throw new InvalidDataException("Image dimensions are unsupported.");
        return (width, height);
    }

    private static string Digest(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}
