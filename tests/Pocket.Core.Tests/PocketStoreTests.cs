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
        Assert.Equal(items, reopened.Load().Items);
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
        Assert.Equal(new[] { first, second }, store.Load().Items);
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
        File.WriteAllText(folder.Index, """{"Version":2,"Items":[]}""");
        Assert.Contains("newer format", store.Load().RecoveryNotice);
        Assert.Throws<NotSupportedException>(() => store.Save(Guid.NewGuid(), Png()));
        Assert.Equal("""{"Version":2,"Items":[]}""", File.ReadAllText(folder.Index));
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
            _folder.Delete();
        }
    }
}
