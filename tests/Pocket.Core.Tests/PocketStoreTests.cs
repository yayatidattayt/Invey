using System.Buffers.Binary;
using System.Text.Json;
using Pocket.Core.References;
using Xunit;

namespace Pocket.Core.Tests;

public sealed class PocketStoreTests
{
    private static byte[] Png() => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aN2kAAAAASUVORK5CYII=");

    [Fact]
    public void TenConfirmedItemsReloadAndDuplicateConfirmationIsIdempotent()
    {
        using var folder = new StoreFolder();
        var store = new PocketStore(folder.Path);
        Assert.Empty(store.Load().Items);
        var items = Enumerable.Range(0, 10).Select(_ => store.Save(Guid.NewGuid(), Png())).ToArray();
        var reopened = new PocketStore(folder.Path);
        Assert.Equal(items.Reverse(), reopened.Load().Items);
        Assert.All(items, item => Assert.Equal(Png(), reopened.Open(item)));
        Assert.Equal(items[^1], reopened.Save(items[^1].Id, Png()));
        Assert.Equal(10, reopened.Load().Items.Count);
        Assert.Equal(10, Directory.GetFiles(folder.Assets).Length);
    }

    [Fact]
    public void InterruptedManifestReplacementPreservesEarlierGenerationAndRetries()
    {
        using var folder = new StoreFolder();
        var store = new PocketStore(folder.Path);
        var first = store.Save(Guid.NewGuid(), Png());
        var pending = Guid.NewGuid();
        using (File.Open(folder.Index, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var error = Record.Exception(() => store.Save(pending, Png()));
            Assert.True(error is IOException or UnauthorizedAccessException);
            using var oldIndex = JsonDocument.Parse(File.ReadAllBytes(folder.Index));
            Assert.Equal(first.Id, oldIndex.RootElement.GetProperty("Items")[0].GetProperty("Id").GetGuid());
            Assert.Equal(1, oldIndex.RootElement.GetProperty("Items").GetArrayLength());
            Assert.True(File.Exists(folder.Asset(pending)));
            var recoverable = new PocketStore(folder.Path).Load();
            Assert.NotNull(recoverable.RecoveryNotice);
            Assert.Contains(recoverable.Items, item => item.Id == pending);
        }
        var second = new PocketStore(folder.Path).Save(pending, Png());
        Assert.Equal(new[] { second, first }, store.Load().Items);
        Assert.Empty(Directory.GetFiles(folder.Path, "*.tmp"));
    }

    [Fact]
    public void DamagedIndexesRecoverReadOnlyAndFutureVersionIsPreserved()
    {
        using var folder = new StoreFolder();
        var store = new PocketStore(folder.Path);
        var first = store.Save(Guid.NewGuid(), Png());
        var second = store.Save(Guid.NewGuid(), Png());
        foreach (string invalid in new[] { "{", JsonSerializer.Serialize(new { Version = 1, Items = new[] { first, first } }) })
        {
            File.WriteAllText(folder.Index, invalid);
            var recovered = new PocketStore(folder.Path).Load();
            Assert.NotNull(recovered.RecoveryNotice);
            Assert.Contains(recovered.Items, item => item.Id == first.Id);
            Assert.Contains(recovered.Items, item => item.Id == second.Id);
            Assert.Throws<InvalidDataException>(() => store.Save(Guid.NewGuid(), Png()));
            Assert.Equal(invalid, File.ReadAllText(folder.Index));
        }
        File.Delete(folder.Index);
        Assert.Equal(2, store.Load().Items.Count);
        File.WriteAllText(folder.Index, """{"Version":3,"Items":[]}""");
        Assert.Contains("newer format", store.Load().RecoveryNotice);
        Assert.Throws<NotSupportedException>(() => store.Save(Guid.NewGuid(), Png()));
        Assert.Equal("""{"Version":3,"Items":[]}""", File.ReadAllText(folder.Index));
    }

    [Fact]
    public void MissingOrCorruptImagesAreIsolatedAndInvalidDimensionsNeverCommit()
    {
        using var folder = new StoreFolder();
        var store = new PocketStore(folder.Path);
        var missing = store.Save(Guid.NewGuid(), Png());
        var damaged = store.Save(Guid.NewGuid(), Png());
        var good = store.Save(Guid.NewGuid(), Png());
        File.Delete(folder.Asset(missing.Id));
        var altered = Png();
        altered[36] ^= 0x20;
        File.WriteAllBytes(folder.Asset(damaged.Id), altered);
        File.WriteAllText(Path.Combine(folder.Assets, "incomplete.tmp"), "partial");
        var reopened = new PocketStore(folder.Path);
        Assert.Equal(3, reopened.Load().Items.Count);
        Assert.Throws<FileNotFoundException>(() => reopened.Open(missing));
        Assert.Throws<InvalidDataException>(() => reopened.Open(damaged));
        Assert.Equal(Png(), reopened.Open(good));
        var oversized = Png();
        BinaryPrimitives.WriteInt32BigEndian(oversized.AsSpan(16), PocketStore.MaxDimension + 1);
        Assert.Throws<InvalidDataException>(() => reopened.Save(Guid.NewGuid(), oversized));
        Assert.Equal(3, reopened.Load().Items.Count);
        Assert.Throws<IOException>(() => new PocketStore(folder.Asset(good.Id)).Load());
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    public void ViewportRoundsOutwardAndClampsWithoutUsingDesktopOrigin(double scale)
    {
        var transform = new DisplayTransform(new(-1920, -1080), scale * 96);
        var start = transform.ToDip(new(-1899.5, -1049.5));
        var end = transform.ToDip(new(-1700.5, -900.5));
        Assert.Equal(new PixelRegion(20, 30, 200, 150),
            CaptureGeometry.Select(end, start, 1920 / scale, 1080 / scale, 1920, 1080));
        Assert.Equal(new PixelRegion(0, 0, 1920, 1080),
            CaptureGeometry.Select(new(-1, -1), new(3000, 3000), 1920 / scale, 1080 / scale, 1920, 1080));
        Assert.Null(CaptureGeometry.Select(start, start, 1920 / scale, 1080 / scale, 1920, 1080));
        Assert.Null(CaptureGeometry.Select(new(double.NaN, 0), end, 1920, 1080, 1920, 1080));
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    public void ExactAndNearEdgesDoNotExpandAtFractionalScale(double scale)
    {
        const int width = 1920, height = 1200;
        PixelRegion? Select(double x, double y, double right, double bottom) =>
            CaptureGeometry.Select(new(x / scale, y / scale), new(right / scale, bottom / scale),
                width / scale, height / scale, width, height);
        Assert.Equal(new PixelRegion(192, 264, 230, 144), Select(192, 264, 422, 408));
        Assert.Equal(new PixelRegion(1919, 1199, 1, 1), Select(1919, 1199, 1920, 1200));
        Assert.Equal(new PixelRegion(1918, 1198, 2, 2), Select(1918.75, 1198.75, 1920.5, 1200.5));
        Assert.Null(Select(20, 20, 20 + 1e-12, 21));
    }

    [Fact]
    public void ObstructedAssetPathCannotCommitAnotherReference()
    {
        using var folder = new StoreFolder();
        var store = new PocketStore(folder.Path);
        var original = store.Save(Guid.NewGuid(), Png());
        var blocked = Guid.NewGuid();
        Directory.CreateDirectory(folder.Asset(blocked));
        try
        {
            var before = File.ReadAllBytes(folder.Index);
            var error = Record.Exception(() => store.Save(blocked, Png()));
            Assert.True(error is IOException or UnauthorizedAccessException);
            Assert.Equal(before, File.ReadAllBytes(folder.Index));
            Assert.Equal(Png(), store.Open(original));
            Assert.Empty(Directory.GetFiles(folder.Assets, "*.tmp"));
        }
        finally { Directory.Delete(folder.Asset(blocked)); }
    }

    [Fact]
    public void LibraryEditsAndTombstonesSurviveRestartAndExpiry()
    {
        using var folder = new StoreFolder();
        var time = new StoreClock();
        var store = new PocketStore(folder.Path, time);
        var first = store.Save(Guid.NewGuid(), Png());
        var second = store.Save(Guid.NewGuid(), Png());
        var placement = new CardPlacement(@"\\.\DISPLAY2", -300, 80, 480, 400, true, true, 125);
        store.Rename(first.Id, "  Working image  ");
        Assert.Throws<InvalidDataException>(() => store.Rename(first.Id, " "));
        store.Place(first.Id, placement);
        store.Move(first.Id, -1);
        var reopened = new PocketStore(folder.Path, time);
        Assert.Equal(new[] { first.Id, second.Id }, reopened.Load().Items.Select(item => item.Id));
        var edited = reopened.Load().Items[0];
        Assert.Equal("Working image", edited.Title);
        Assert.Equal(placement, edited.Placement);
        Assert.Equal(Png(), reopened.Open(edited));
        reopened.Delete(first.Id);
        Assert.True(File.Exists(folder.Asset(first.Id)));
        Assert.Equal(second.Id, Assert.Single(store.Load().Items).Id);
        Assert.Equal(edited, store.Undo(first.Id));
        Assert.Equal(edited, store.Load().Items[0]);
        store.Delete(first.Id);
        time.Now += PocketStore.UndoWindow;
        Assert.Throws<InvalidDataException>(() => reopened.Undo(first.Id));
        Assert.Equal(1, reopened.PurgeExpired());
        Assert.False(File.Exists(folder.Asset(first.Id)));
        Assert.Equal(Png(), store.Open(second));
    }

    [Fact]
    public void RepairPreservesDamagedIndexAndPublishesOnlyVerifiedAssets()
    {
        using var folder = new StoreFolder();
        var store = new PocketStore(folder.Path);
        var item = store.Save(Guid.NewGuid(), Png());
        File.WriteAllText(folder.Index, "{broken");
        Assert.NotNull(store.Load().RecoveryNotice);
        store.Repair();
        var recovered = Assert.Single(store.Load().Items);
        Assert.Equal(item.Id, recovered.Id);
        Assert.Equal(Png(), store.Open(recovered));
        Assert.Null(store.Load().RecoveryNotice);
        Assert.Equal("{broken", File.ReadAllText(Assert.Single(Directory.GetFiles(folder.Path, "manifest.recovery-*.json"))));
    }

    [Fact]
    public void MigrationCopiesVerifiedDataAndPreservesTheSeparateOriginal()
    {
        using var folder = new StoreFolder();
        string old = Path.Combine(folder.Path, "PocketDualGhost");
        string current = Path.Combine(folder.Path, "Invey");
        Assert.Equal(current, InveyProfile.Resolve(folder.Path).Root);
        var oldStore = new PocketStore(old);
        var item = oldStore.Save(Guid.NewGuid(), Png());
        string legacyIndex = Path.Combine(old, "manifest.json");
        File.WriteAllText(legacyIndex, File.ReadAllText(legacyIndex).Replace("\"Version\":2", "\"Version\":1"));
        var legacyBytes = File.ReadAllBytes(legacyIndex);
        var settings = new Pocket.Core.Tracking.CalibrationPreferences(1, new(1, 0, 0, 1920, 1200, 120), 1, 0, 12, 100);
        new Pocket.Core.Tracking.CalibrationPreferencesStore(Path.Combine(old, "calibration.json")).Save(settings);
        var result = InveyProfile.Resolve(folder.Path);
        Assert.Equal(current, result.Root);
        Assert.Equal(item, Assert.Single(new PocketStore(current).Load().Items));
        Assert.Equal(Png(), new PocketStore(current).Open(item));
        Assert.Equal(settings, new Pocket.Core.Tracking.CalibrationPreferencesStore(Path.Combine(current, "calibration.json")).Load().Preferences);
        Assert.Equal(legacyBytes, File.ReadAllBytes(legacyIndex));
        oldStore.Save(Guid.NewGuid(), Png());
        Assert.Equal(current, InveyProfile.Resolve(folder.Path).Root);
        Assert.Single(new PocketStore(current).Load().Items);
    }

    [Fact]
    public void FailedOrInterruptedMigrationKeepsLegacyFilesAndDoesNotPublish()
    {
        using var folder = new StoreFolder();
        string old = Path.Combine(folder.Path, "PocketDualGhost");
        var store = new PocketStore(old);
        var item = store.Save(Guid.NewGuid(), Png());
        string asset = Path.Combine(old, "assets", item.Id.ToString("N") + ".png");
        using (File.Open(asset, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.Equal(old, InveyProfile.Resolve(folder.Path).Root);
            Assert.False(Directory.Exists(Path.Combine(folder.Path, "Invey")));
        }
        string staging = Path.Combine(folder.Path, "Invey.migration");
        Directory.CreateDirectory(staging);
        File.WriteAllText(Path.Combine(staging, "interrupted"), "keep");
        Assert.Equal(old, InveyProfile.Resolve(folder.Path).Root);
        Assert.Equal("keep", File.ReadAllText(Path.Combine(staging, "interrupted")));
        Assert.Equal(Png(), store.Open(item));
    }

    [Fact]
    public void MigrationPreservesADeletedReferenceAndItsUndo()
    {
        using var folder = new StoreFolder();
        var legacy = new PocketStore(Path.Combine(folder.Path, "PocketDualGhost"));
        var item = legacy.Save(Guid.NewGuid(), Png());
        legacy.Delete(item.Id);
        var profile = InveyProfile.Resolve(folder.Path);
        Assert.Equal(Path.Combine(folder.Path, "Invey"), profile.Root);
        var current = new PocketStore(profile.Root);
        Assert.Equal(item, Assert.Single(current.Load().Deleted!).Item);
        current.Undo(item.Id);
        Assert.Equal(Png(), current.Open(Assert.Single(current.Load().Items)));
        Assert.Empty(legacy.Load().Items);
    }

    [Fact]
    public void CardPlacementClampsNegativeOriginsAndCentersWhenDisplayIsMissing()
    {
        var placement = new CardPlacement("left", int.MinValue, int.MaxValue, 800, 500, true, true, 125);
        Assert.Equal(new PixelRegion(-1920, -500, 800, 500),
            CardPlacement.Fit(placement, "left", new(-1920, -1080, 1920, 1080)));
        Assert.Equal(new PixelRegion(560, 350, 800, 500),
            CardPlacement.Fit(placement, "primary", new(0, 0, 1920, 1200)));
    }

    private sealed class StoreClock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 11, 6, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class StoreFolder : IDisposable
    {
        private readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("invey-pocket-test-");
        public string Path => _folder.FullName;
        public string Assets => System.IO.Path.Combine(Path, "assets");
        public string Index => System.IO.Path.Combine(Path, "manifest.json");
        public string Asset(Guid id) => System.IO.Path.Combine(Assets, id.ToString("N") + ".png");
        public void Dispose()
        {
            if (Directory.Exists(Assets))
            {
                foreach (var file in Directory.EnumerateFiles(Assets)) File.Delete(file);
                Directory.Delete(Assets);
            }
            foreach (var file in _folder.EnumerateFiles()) file.Delete();
            foreach (var directory in _folder.EnumerateDirectories())
            {
                foreach (var assetDirectory in directory.EnumerateDirectories())
                {
                    foreach (var file in assetDirectory.EnumerateFiles()) file.Delete();
                    assetDirectory.Delete();
                }
                foreach (var file in directory.EnumerateFiles()) file.Delete();
                directory.Delete();
            }
            _folder.Delete();
        }
    }
}
