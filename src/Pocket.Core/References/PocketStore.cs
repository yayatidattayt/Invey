using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pocket.Core.References;

public sealed record PocketReference([property: JsonRequired] Guid Id, [property: JsonRequired] DateTimeOffset CreatedUtc,
    [property: JsonRequired] int Width, [property: JsonRequired] int Height, [property: JsonRequired] string Sha256,
    string? Title = null, CardPlacement? Placement = null);
public sealed record CardPlacement(string Display, int X, int Y, int Width, int Height, bool Pinned, bool Visible, int Zoom)
{
    [JsonIgnore] public bool Valid => Display is { Length: > 0 and <= 128 } && Width is > 0 and <= 8192 &&
        Height is > 0 and <= 8192 && Zoom is 0 or >= 25 and <= 400;

    public static PixelRegion Fit(CardPlacement? placement, string display, PixelRegion workArea)
    {
        if (workArea.Width <= 0 || workArea.Height <= 0) throw new ArgumentException("Invalid work area.");
        int maxWidth = Math.Max(1, workArea.Width * 9 / 10);
        int maxHeight = Math.Max(1, workArea.Height * 9 / 10);
        int width = Math.Clamp(placement?.Width ?? 700, Math.Min(maxWidth, 340), maxWidth);
        int height = Math.Clamp(placement?.Height ?? 500, Math.Min(maxHeight, 240), maxHeight);
        bool same = placement?.Display == display;
        int x = Math.Clamp(same ? placement!.X : workArea.X + (workArea.Width - width) / 2,
            workArea.X, workArea.X + workArea.Width - width);
        int y = Math.Clamp(same ? placement!.Y : workArea.Y + (workArea.Height - height) / 2,
            workArea.Y, workArea.Y + workArea.Height - height);
        return new(x, y, width, height);
    }
}
public sealed record DeletedReference(PocketReference Item, int Position, DateTimeOffset DeletedUtc);
public sealed record PocketLibrary(IReadOnlyList<PocketReference> Items, string? RecoveryNotice = null,
    IReadOnlyList<DeletedReference>? Deleted = null);

public sealed class PocketStore(string path, TimeProvider? clock = null)
{
    public const int MaxPixels = 16_777_216;
    public const int MaxDimension = 8192;
    public const int MaxAssetBytes = 64 * 1024 * 1024;
    private const int MaxReferences = 200;
    internal const int MaxManifestBytes = 512 * 1024;
    private const long RecoveryByteBudget = 256L * 1024 * 1024;
    private readonly string _root = Path.GetFullPath(path);
    private readonly object _gate = new();
    private static readonly JsonSerializerOptions Schema = new()
    {
        AllowDuplicateProperties = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    public static TimeSpan UndoWindow { get; } = TimeSpan.FromMinutes(10);
    private sealed record Manifest([property: JsonRequired] int Version, [property: JsonRequired] PocketReference[] Items,
        DeletedReference[]? Deleted = null)
    {
        [JsonIgnore] public DeletedReference[] Tombstones => Deleted ?? [];
    }
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
            Manifest? indexed = null, backup = null;
            try
            {
                if (Exists(Index)) indexed = ReadManifest(Index);
                else if (!Exists(Backup) && !AssetPaths().Any()) return new([]);
            }
            catch (NotSupportedException)
            {
                return new([], "This library uses a newer format. Existing files were preserved.");
            }
            catch (InvalidDataException) { /* Recover read-only below. */ }
            var recovered = indexed?.Items.ToDictionary(item => item.Id) ?? new Dictionary<Guid, PocketReference>();
            try
            {
                if (indexed is null && Exists(Backup))
                {
                    backup = ReadManifest(Backup);
                    foreach (var item in backup.Items) recovered.Add(item.Id, item);
                }
            }
            catch (NotSupportedException)
            { return new([], "This library backup uses a newer format. Existing files were preserved."); }
            catch (InvalidDataException) { }
            long remaining = RecoveryByteBudget;
            bool unindexed = false;
            var deleted = (indexed ?? backup)?.Tombstones ?? [];
            var deletedIds = deleted.Select(row => row.Item.Id).ToHashSet();
            foreach (var file in AssetPaths().Take(MaxReferences * 2 + 1))
            {
                if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(file), "N", out var id) ||
                    id == Guid.Empty || recovered.ContainsKey(id) || deletedIds.Contains(id)) continue;
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
            if (indexed is not null && !unindexed) return new(indexed.Items, Deleted: deleted);
            return new(recovered.Values.OrderByDescending(item => item.CreatedUtc).ToArray(),
                "The library needs recovery. Available references are shown read-only. Original files were preserved.", deleted);
        }
    }

    public PocketReference Save(Guid id, byte[] approvedCrop)
    {
        if (id == Guid.Empty) throw new ArgumentException("A reference ID is required.", nameof(id));
        var (width, height) = Dimensions(approvedCrop);
        var item = new PocketReference(id, _clock.GetUtcNow(), width, height, Digest(approvedCrop));
        lock (_gate)
        {
            VerifyDirectories();
            bool indexed = Exists(Index);
            if (!indexed && (Exists(Backup) || AssetPaths().Any(file => !file.Equals(Asset(id), StringComparison.OrdinalIgnoreCase))))
                throw new InvalidDataException("The library needs recovery before new references can be saved.");
            var manifest = indexed ? ReadManifest(Index) : new Manifest(2, []);
            var current = manifest.Items;
            if (manifest.Tombstones.Any(row => row.Item.Id == id)) throw new InvalidDataException("Reference was deleted.");
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
            Commit(manifest with { Items = [item, .. current] });
            return item;
        }
    }

    public void Rename(Guid id, string title)
    {
        title = title.Trim();
        if (title.Length is < 1 or > 120 || title.Any(char.IsControl))
            throw new InvalidDataException("Use a title between 1 and 120 characters.");
        Edit(id, item => item with { Title = title });
    }

    public void Place(Guid id, CardPlacement placement)
    {
        if (!placement.Valid) throw new InvalidDataException("Invalid card placement.");
        Edit(id, item => item with { Placement = placement });
    }

    public void Move(Guid id, int direction)
    {
        if (direction is not (-1 or 1)) throw new ArgumentOutOfRangeException(nameof(direction));
        lock (_gate)
        {
            var manifest = Editable();
            int index = IndexOf(manifest, id);
            int next = index + direction;
            if (next < 0 || next >= manifest.Items.Length) return;
            (manifest.Items[index], manifest.Items[next]) = (manifest.Items[next], manifest.Items[index]);
            Commit(manifest);
        }
    }

    public void Delete(Guid id)
    {
        lock (_gate)
        {
            var manifest = Editable();
            int index = IndexOf(manifest, id);
            if (manifest.Tombstones.Length >= MaxReferences)
                throw new InvalidDataException("Deleted references are full. Purge expired entries before deleting more.");
            Commit(manifest with { Items = manifest.Items.Where(item => item.Id != id).ToArray(),
                Deleted = [.. manifest.Tombstones, new(manifest.Items[index], index, _clock.GetUtcNow())] });
        }
    }

    public bool CanUndo(DeletedReference row) => _clock.GetUtcNow() - row.DeletedUtc < UndoWindow;

    public PocketReference Undo(Guid id)
    {
        lock (_gate)
        {
            var manifest = Editable();
            var row = manifest.Tombstones.SingleOrDefault(entry => entry.Item.Id == id);
            if (row is null || !CanUndo(row)) throw new InvalidDataException("The Undo window has expired.");
            if (manifest.Items.Length >= MaxReferences) throw new InvalidDataException("Free a library slot before restoring.");
            _ = Open(row.Item);
            var items = manifest.Items.ToList();
            items.Insert(Math.Min(row.Position, items.Count), row.Item);
            Commit(manifest with { Items = items.ToArray(), Deleted = manifest.Tombstones.Where(entry => entry != row).ToArray() });
            return row.Item;
        }
    }

    public int PurgeExpired()
    {
        lock (_gate)
        {
            var manifest = Editable();
            var expired = manifest.Tombstones.Where(row => !CanUndo(row)).ToArray();
            foreach (var row in expired)
            {
                string path = Asset(row.Item.Id);
                if (Exists(path)) File.Delete(path);
            }
            if (expired.Length > 0)
            {
                var ids = expired.Select(row => row.Item.Id).ToHashSet();
                Commit(manifest with { Deleted = manifest.Tombstones.Where(row => !ids.Contains(row.Item.Id)).ToArray() });
            }
            return expired.Length;
        }
    }

    public void Repair()
    {
        lock (_gate)
        {
            var library = Load();
            if (library.RecoveryNotice is null) return;
            if (library.RecoveryNotice.Contains("newer format", StringComparison.Ordinal))
                throw new NotSupportedException("A newer library cannot be repaired by this version.");
            var deleted = library.Deleted?.ToArray() ?? [];
            var known = library.Items.Select(item => item.Id).Concat(deleted.Select(row => row.Item.Id)).ToHashSet();
            if (AssetPaths().Take(MaxReferences * 2 + 1).Any(file =>
                !Guid.TryParseExact(Path.GetFileNameWithoutExtension(file), "N", out var id) || !known.Contains(id)))
                throw new InvalidDataException("Some assets could not be verified. Recovery remains read-only.");
            foreach (var item in library.Items) _ = Open(item);
            if (Exists(Index))
            {
                string preserved = Path.Combine(_root, "manifest.recovery-" + Guid.NewGuid().ToString("N") + ".json");
                WriteAtomic(preserved, ReadBounded(Index, MaxManifestBytes), temporary => File.Move(temporary, preserved));
            }
            Commit(new Manifest(2, library.Items.ToArray(), deleted), preserveBackup: true);
        }
    }

    private void Edit(Guid id, Func<PocketReference, PocketReference> edit)
    {
        lock (_gate)
        {
            var manifest = Editable();
            int index = IndexOf(manifest, id);
            var updated = edit(manifest.Items[index]);
            if (updated == manifest.Items[index]) return;
            manifest.Items[index] = updated;
            Commit(manifest);
        }
    }

    private Manifest Editable()
    {
        VerifyDirectories();
        return Exists(Index) ? ReadManifest(Index) : throw new InvalidDataException("The library index is unavailable.");
    }

    private static int IndexOf(Manifest manifest, Guid id)
    {
        int index = Array.FindIndex(manifest.Items, item => item.Id == id);
        return index >= 0 ? index : throw new InvalidDataException("Reference is unavailable.");
    }

    private void Commit(Manifest manifest, bool preserveBackup = false)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(manifest with { Version = 2 }, Schema);
        if (bytes.Length > MaxManifestBytes) throw new InvalidDataException("The library index has reached its size limit.");
        WriteAtomic(Index, bytes, temporary =>
        {
            if (!Exists(Index)) File.Move(temporary, Index);
            else if (preserveBackup) File.Move(temporary, Index, overwrite: true);
            else
            {
                _ = Exists(Backup);
                File.Replace(temporary, Index, Backup);
            }
        });
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
                document.RootElement.TryGetProperty("Version", out var version) && version.TryGetInt32(out int number) && number > 2)
                throw new NotSupportedException("This library uses a newer format.");
            manifest = JsonSerializer.Deserialize<Manifest>(bytes, Schema) ?? throw new InvalidDataException("Empty index.");
        }
        catch (JsonException ex) { throw new InvalidDataException("The library index is damaged.", ex); }
        if (manifest.Version is not (1 or 2) || manifest.Items is null || manifest.Items.Length > MaxReferences ||
            manifest.Tombstones.Length > MaxReferences || manifest.Items.Any(item => !Valid(item)) ||
            manifest.Tombstones.Any(row => row is null || !Valid(row.Item) || row.Position is < 0 or >= MaxReferences) ||
            manifest.Items.Select(item => item.Id).Concat(manifest.Tombstones.Select(row => row.Item.Id))
                .Distinct().Count() != manifest.Items.Length + manifest.Tombstones.Length)
            throw new InvalidDataException("The library index is damaged.");
        return manifest.Version == 1 ? manifest with { Items = manifest.Items.Reverse().ToArray() } : manifest;
    }

    private static bool Valid(PocketReference? item) => item is not null && item.Id != Guid.Empty &&
        SupportsSize(item.Width, item.Height) && item.Sha256 is { Length: 64 } && item.Sha256.All(Uri.IsHexDigit) &&
        (item.Title is null || item.Title is { Length: > 0 and <= 120 } && !item.Title.Any(char.IsControl)) &&
        (item.Placement is null || item.Placement.Valid);

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
