using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.Scene2D.Packages;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Scene2DPackages;

public sealed partial class Scene2DPackageTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreparedChunkHeadersDeclareTheirAcquisitionOwnedDataCharge(bool freePlacement)
    {
        using Fixture fixture = new();
        Scene2DDocument document = freePlacement
            ? new([new Scene2DLevel("level", [new TileMap2DModel([
                new Tile(new ImageReference(new ResourceId<ImageResource>("atlas")),
                    new TileColliderDescriptor2D(TileColliderShape2D.Box, width: 10, height: 10,
                        properties: new Dictionary<string, object?> { ["bulk"] = new byte[65_536] }),
                    width: 10, height: 10)
            ])])], [new(new("atlas"), "nested/atlas.bin", new(20, 10))])
            : fixture.Document();
        await fixture.WriteAsync(document);

        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        TileMapCatalog2D catalog = Assert.Single(fixture.ReadIndex().Levels[0].Maps).Catalog;

        Assert.Equal(new[] { Assert.Single(document.Levels[0].TileMaps).Id }, Assert.Single(package.Levels).TileMapIds);
        Assert.NotEmpty(catalog.Chunks);
        Assert.All(catalog.Chunks, info =>
        {
            Assert.True(info.DataResidencyBytes.HasValue, "A prepared chunk must declare its data charge before acquisition.");
            Assert.True(info.DataResidencyBytes.Value >= 65_536, "The charge must include its acquisition-owned bulk property, not only cells or placements.");
        });
    }

    [Fact]
    public async Task EarlierCpv2UnknownChargesRemainReadableWithoutPretendingTheyAreZero()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        PackageIndex index = fixture.ReadIndex();
        PackageMap map = index.Levels[0].Maps[0];
        TileMapCatalog2D original = map.Catalog;
        TileMapChunkInfo2D[] unknown = original.Chunks.Select(info => new TileMapChunkInfo2D(
            info.Spatial, info.Cells!.Value, info.TileIds, info.Images, info.ExpandedColliderCount)).ToArray();
        TileMapCatalog2D legacy = new(original.Id, unknown, original.TileSize, original.Bounds,
            original.Order, original.IsVisible, original.Offset, original.Opacity, original.Tint, original.Version, original.ImageSizes);
        await fixture.WriteIndexAsync(index with
        {
            Levels = [index.Levels[0] with { Maps = [map with { Catalog = legacy }] }]
        });

        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        Assert.All(fixture.ReadIndex().Levels[0].Maps[0].Catalog.Chunks,
            info => Assert.Null(info.DataResidencyBytes));
        TileMap2DModel restored = await package.Levels[0].LoadMapModelAsync("map");
        Assert.Equal(65_536, Assert.IsType<byte[]>(restored.Chunks[0].Properties["bulk"]).Length);
    }

    [Fact]
    public async Task DirectoryIsSelfContainedAndPreservesHeadersAndExplicitMetadata()
    {
        using Fixture fixture = new();
        Scene2DDocument document = fixture.Document();
        await fixture.WriteAsync(document);
        File.Delete(Path.Combine(fixture.Input, "nested", "atlas.bin"));
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        Assert.Equal(new[] { "nested/atlas.bin", "scripts/quest.txt" }, package.ReferencedFiles);
        Assert.Equal(new byte[] { 11, 22, 33 }, await File.ReadAllBytesAsync(package.GetFilePath("nested/atlas.bin")));
        Assert.Equal("quest data", await File.ReadAllTextAsync(package.GetFilePath("scripts/quest.txt")));
        Assert.Throws<ArgumentException>(() => package.GetFilePath("not-a-dependency.txt"));
        Scene2DPackageLevel level = Assert.Single(package.Levels);
        Assert.Equal("level", level.Id);
        Assert.Equal(new DrawPoint(200, -300), level.WorldOffset);
        Assert.Equal(new DrawSize(10, 10), level.TileSize);
        PackageMap map = Assert.Single(fixture.ReadIndex().Levels[0].Maps);
        Assert.Equal(new[] { "map" }, level.TileMapIds);
        Assert.Equal(2, map.Catalog.Chunks.Count);
        Assert.All(map.Catalog.Chunks, info => Assert.True(info.DataResidencyBytes is >= 65_536));
        Assert.Equal(new DrawPoint(2, 3), map.Catalog.Offset);
        Assert.Equal(7, map.Catalog.Version);
        Scene2DPackageMetadata docMetadata = await package.LoadMetadataAsync();
        Scene2DPackageMetadata levelMetadata = await level.LoadMetadataAsync();
        Scene2DPackageMetadata mapMetadata = await level.LoadMapMetadataAsync("map");
        Assert.Equal("document", docMetadata.Properties["scope"]);
        Assert.Equal("level", levelMetadata.Properties["scope"]);
        Assert.Equal("map", mapMetadata.Properties["scope"]);
        Assert.Equal(2, Assert.Single(mapMetadata.TileSets).Tiles.Count); // Includes unused authoring definition.
        TileMapChunkData2D data = await package.LoadAsync<TileMapChunkData2D>(map.Chunks[0], CancellationToken.None);
        Assert.Single(Assert.Single(data.TileSets).Tiles); // Only the used definition is required to stream.
        Assert.Equal(65_536, Assert.IsType<byte[]>(data.Grid!.Properties["bulk"]).Length);
        Assert.Equal("{\"tag\":1.0}", Assert.IsType<SceneJsonValue2D>(data.TileSets[0].Tiles[0].Properties["json"]).Value.GetRawText());
        Scene2DEntity entity = await level.LoadEntityAsync(Assert.Single(level.EntityIds));
        Assert.Equal(PackageValueCodec.Encode(document.Levels[0].Entities[0]), PackageValueCodec.Encode(entity));
        TilePromotion2D promotion = await level.LoadPromotionAsync(Assert.Single(level.PromotionCells));
        Assert.Equal(PackageValueCodec.Encode(document.Levels[0].Promotions[0]), PackageValueCodec.Encode(promotion));
        Assert.Equal("map", promotion.Cell.MapId);
    }

    [Fact]
    public async Task OpeningAndOneRegionDoNotReadAnUnrequestedCorruptChunkOrMetadata()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        PackageIndex index = fixture.ReadIndex();
        fixture.Corrupt(index.Metadata);
        fixture.Corrupt(index.Levels[0].Maps[0].Chunks[1]);
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        PackageBlock[] chunks = index.Levels[0].Maps[0].Chunks;
        TileMapChunkData2D first = await package.LoadAsync<TileMapChunkData2D>(chunks[0], CancellationToken.None);
        Assert.Equal(0, first.Grid!.Origin.X);
        await Assert.ThrowsAsync<InvalidDataException>(async () => await package.LoadAsync<TileMapChunkData2D>(chunks[1], CancellationToken.None));
        await Assert.ThrowsAsync<InvalidDataException>(async () => await package.LoadMetadataAsync());
        TileMapChunkData2D firstAgain = await package.LoadAsync<TileMapChunkData2D>(chunks[0], CancellationToken.None);
        Assert.Equal(first.Grid.Tiles, firstAgain.Grid!.Tiles);
        Assert.NotSame(first, firstAgain);
    }

    [Fact]
    public async Task IndependentRandomAccessReadsAndCancellationDoNotShareAStreamPosition()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        PackageBlock[] chunks = fixture.ReadIndex().Levels[0].Maps[0].Chunks;
        using CancellationTokenSource canceled = new();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await package.LoadAsync<TileMapChunkData2D>(chunks[0], canceled.Token));
        var requests = Enumerable.Range(0, 32).Select(async index =>
        {
            TileMapChunkData2D data = await package.LoadAsync<TileMapChunkData2D>(chunks[index % 2], CancellationToken.None);
            Assert.Equal(index % 2 * 10, data.Grid!.Origin.X);
            Assert.Equal(2, data.Grid.Tiles.Count);
        });
        await Task.WhenAll(requests);
        TileMapChunkData2D region = await package.LoadAsync<TileMapChunkData2D>(chunks[0], CancellationToken.None);
        Assert.Equal(0, region.Grid!.Origin.X);
    }

    [Fact]
    public async Task DisposeRetiresTheFileOwnerButNotExistingOrAlreadyStartedAcquisitions()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        PackageBlock[] chunks = fixture.ReadIndex().Levels[0].Maps[0].Chunks;
        ValueTask<TileMapChunkData2D> started = package.LoadAsync<TileMapChunkData2D>(chunks[0], CancellationToken.None);
        package.Dispose();
        package.Dispose();
        TileMapChunkData2D acquired = await started;
        Assert.Equal(2, acquired.Grid!.Tiles.Count);
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await package.LoadAsync<TileMapChunkData2D>(chunks[1], CancellationToken.None));
        Assert.Throws<ObjectDisposedException>(() => package.GetFilePath("nested/atlas.bin"));
        await package.DisposeAsync();
        Assert.Equal(2, acquired.Grid.Tiles.Count); // Returned values belong to the caller, not to the retired reader.
        using FileStream exclusive = new(fixture.DataPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    [Fact]
    public async Task ReleasedPalettesAndBulkMetadataCollectWhilePackageAndSourceRemainAlive()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        PackageBlock[] chunks = fixture.ReadIndex().Levels[0].Maps[0].Chunks;
        List<WeakReference> references = [];
        for (int iteration = 0; iteration < 32; iteration++) { references.AddRange(await ReadAndRelease(package, chunks[iteration % 2])); }
        references.AddRange(await ReadMetadataAndRelease(package));
        // An await continuation can run inside the last loader's SetResult,
        // before its stack-local decoded value has returned. Collect only after
        // that completion stack unwinds, not from inside the producer itself.
        await Task.Yield();
        for (int collection = 0; collection < 3; collection++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        Assert.All(references, reference => Assert.False(reference.IsAlive));
        GC.KeepAlive(package);
    }

    [Fact]
    public async Task RealHeadlessRegionPreparationAddsAndReleasesDiskBackedCollisions()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        TileMap2D map = package.Levels[0].CreateTileMap("map");
        var scene = new global::Cerneala.UI.Controls.Scene2D();
        scene.Children.Add(map);
        using SceneSimulationContext2D context = new(scene);
        for (int iteration = 0; iteration < 16; iteration++)
        {
            int x = iteration % 2 * 100;
            Task<SceneCollisionRegion2D> request = scene.CollisionWorld.PrepareRegionAsync(new(x - 5, -10, 30, 40)).AsTask();
            Assert.True(SpinWait.SpinUntil(() => { context.Update(); return request.IsCompleted; }, TimeSpan.FromSeconds(5)));
            using SceneCollisionRegion2D region = await request;
            Assert.Single(map.LogicalChildren);
            Assert.Single(scene.CollisionWorld.Raycast(new(x + 5, -5), Vector2.UnitY, 20));
            region.Dispose();
            context.Update();
            Assert.Empty(map.LogicalChildren);
        }
    }

    [Fact]
    public async Task WriterNeverOverwritesAndRemovesOnlyItsOwnPartialOutput()
    {
        using Fixture fixture = new();
        Directory.CreateDirectory(fixture.Output);
        string sentinel = Path.Combine(fixture.Output, "user.txt");
        await File.WriteAllTextAsync(sentinel, "keep");
        await Assert.ThrowsAsync<IOException>(() => fixture.WriteAsync());
        Assert.Equal("keep", await File.ReadAllTextAsync(sentinel));
        string unsupported = Path.Combine(fixture.Root, "unsupported");
        await Assert.ThrowsAsync<NotSupportedException>(() => Scene2DPackageWriter.WriteAsync(unsupported,
            new Scene2DDocument([], [], properties: new Dictionary<string, object?> { ["unsupported"] = new object() }), fixture.Input));
        Assert.False(Directory.Exists(unsupported));
        Assert.Empty(Directory.GetDirectories(fixture.Root, ".cerneala-package-*"));
        using CancellationTokenSource canceled = new();
        IEnumerable<string> CancelDuringEnumeration()
        {
            yield return "scripts/quest.txt";
            canceled.Cancel();
        }
        string canceledOutput = Path.Combine(fixture.Root, "canceled");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Scene2DPackageWriter.WriteAsync(canceledOutput, fixture.Document(),
            fixture.Input, CancelDuringEnumeration(), canceled.Token));
        Assert.False(Directory.Exists(canceledOutput));
        Assert.Empty(Directory.GetDirectories(fixture.Root, ".cerneala-package-*"));
    }

    [Theory]
    [InlineData("../secret.txt")]
    [InlineData("/secret.txt")]
    [InlineData("C:\\secret.txt")]
    [InlineData("nested/../secret.txt")]
    [InlineData("NUL.txt")]
    [InlineData("nested/atlas.bin:stream")]
    [InlineData("nested/atlas.bin.")]
    public async Task PackagePathsCannotEscapeOrAliasTheAssetRoot(string path)
    {
        using Fixture fixture = new();
        await Assert.ThrowsAsync<ArgumentException>(() => Scene2DPackageWriter.WriteAsync(fixture.Output, fixture.Document(), fixture.Input, [path]));
        Assert.False(Directory.Exists(fixture.Output));
    }

    [Fact]
    public async Task ReadLimitsAreCheckedBeforePayloadAllocationAndDoNotPoisonSmallerReads()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        await Assert.ThrowsAsync<InvalidDataException>(() => Scene2DPackage.OpenAsync(fixture.Output, new() { MaxCatalogBytes = 5 }));
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output, new() { MaxPayloadBytes = 1024 });
        PackageBlock chunk = fixture.ReadIndex().Levels[0].Maps[0].Chunks[0];
        await Assert.ThrowsAsync<InvalidDataException>(async () => await package.LoadAsync<TileMapChunkData2D>(chunk, CancellationToken.None));
        Scene2DPackageMetadata metadata = await package.LoadMetadataAsync();
        Assert.Equal("document", metadata.Properties["scope"]);
    }

    [Fact]
    public async Task CorruptCatalogChecksumsRangesAndDataLengthsFailAtOpen()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        byte[] original = await File.ReadAllBytesAsync(fixture.CatalogPath);
        byte[] corrupt = (byte[])original.Clone();
        corrupt[10] ^= 1;
        await File.WriteAllBytesAsync(fixture.CatalogPath, corrupt);
        await Assert.ThrowsAsync<InvalidDataException>(() => Scene2DPackage.OpenAsync(fixture.Output));
        await File.WriteAllBytesAsync(fixture.CatalogPath, original);
        PackageIndex index = fixture.ReadIndex();
        foreach (PackageIndex invalid in new[]
        {
            index with { Metadata = index.Metadata with { Offset = 1 } },
            index with { Metadata = index.Metadata with { Length = int.MaxValue } },
            index with { Metadata = index.Metadata with { Hash = [1] } },
            index with { DataLength = index.DataLength + 1 },
            index with { Files = ["../secret.txt"] }
        })
        {
            await fixture.WriteIndexAsync(invalid);
            await Assert.ThrowsAsync<InvalidDataException>(() => Scene2DPackage.OpenAsync(fixture.Output));
        }
        await File.WriteAllBytesAsync(fixture.CatalogPath, original);
        using (FileStream data = new(fixture.DataPath, FileMode.Open, FileAccess.Write)) { data.SetLength(index.DataLength - 1); }
        await Assert.ThrowsAsync<InvalidDataException>(() => Scene2DPackage.OpenAsync(fixture.Output));
    }

    [Fact]
    public async Task PreparingTheSameInputProducesIdenticalBytesAndNoEditorFileCopies()
    {
        using Fixture fixture = new();
        Scene2DDocument document = fixture.Document();
        await fixture.WriteAsync(document);
        string second = Path.Combine(fixture.Root, "second");
        await Scene2DPackageWriter.WriteAsync(second, document, fixture.Input, ["scripts/quest.txt"]);
        Assert.Equal(await File.ReadAllBytesAsync(fixture.CatalogPath), await File.ReadAllBytesAsync(Path.Combine(second, PackageFiles.CatalogName)));
        Assert.Equal(await File.ReadAllBytesAsync(fixture.DataPath), await File.ReadAllBytesAsync(Path.Combine(second, PackageFiles.DataName)));
        Assert.False(File.Exists(Path.Combine(fixture.Output, "assets", "editor.tmj")));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference[]> ReadAndRelease(Scene2DPackage package, PackageBlock block)
    {
        TileMapChunkData2D value = await package.LoadAsync<TileMapChunkData2D>(block, CancellationToken.None);
        return [new(value), new(value.Grid!), new(value.Grid!.Properties["bulk"]!),
            new(value.TileSets[0]), new(value.TileSets[0].Tiles[0]), new(value.TileSets[0].Tiles[0].Collider!)];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference[]> ReadMetadataAndRelease(Scene2DPackage package)
    {
        Scene2DPackageMetadata value = await package.Levels[0].LoadMapMetadataAsync("map");
        return [new(value), new(value.Properties), new(value.TileSets[0]), new(value.TileSets[0].Tiles[1])];
    }

    private sealed class Fixture : IDisposable
    {
        internal string Root { get; } = Path.Combine(Path.GetTempPath(), "CernealaScenePackageTests-" + Guid.NewGuid().ToString("N"));
        internal string Input => Path.Combine(Root, "input");
        internal string Output => Path.Combine(Root, "package");
        internal string CatalogPath => Path.Combine(Output, PackageFiles.CatalogName);
        internal string DataPath => Path.Combine(Output, PackageFiles.DataName);

        internal Fixture()
        {
            Directory.CreateDirectory(Path.Combine(Input, "nested"));
            Directory.CreateDirectory(Path.Combine(Input, "scripts"));
            File.WriteAllBytes(Path.Combine(Input, "nested", "atlas.bin"), [11, 22, 33]);
            File.WriteAllText(Path.Combine(Input, "scripts", "quest.txt"), "quest data");
            File.WriteAllText(Path.Combine(Input, "editor.tmj"), "not a declared dependency");
        }

        internal Scene2DDocument Document(IEnumerable<Scene2DEntity>? entities = null)
        {
            using JsonDocument json = JsonDocument.Parse("{\"tag\":1.0}");
            Dictionary<string, object?> properties = new() { ["json"] = new SceneJsonValue2D(json.RootElement) };
            TileDefinition2D first = new(1, new(0, 0, 10, 10), properties, new(TileColliderShape2D.Box, width: 10, height: 10, properties: properties));
            TileSet2D set = new("set", new("atlas"), [first, new(2, new(10, 0, 10, 10))]);
            TileChunk2D Chunk(int x) => new(new(x, 0), 2, 1, [new(1), new(1)], 3,
                new Dictionary<string, object?> { ["bulk"] = new byte[65_536] });
            TileMap2DModel map = new("map", new(10, 10), [set], [Chunk(0), Chunk(10)], offset: new(2, 3), version: 7,
                properties: new Dictionary<string, object?> { ["scope"] = "map" });
            Scene2DLevel level = new("level", [map], new(200, -300),
                entities ?? [new("e", "map", new(10, 20), default, properties: properties)],
                [new(new("map", 0, 0), properties: properties)], new Dictionary<string, object?> { ["scope"] = "level" }, [set], new(10, 10));
            return new([level], [new(new("atlas"), "nested/atlas.bin", new(20, 10))], properties: new Dictionary<string, object?> { ["scope"] = "document" });
        }

        internal Task WriteAsync(Scene2DDocument? document = null) => Scene2DPackageWriter.WriteAsync(Output, document ?? Document(), Input, ["scripts/quest.txt"]);
        internal PackageIndex ReadIndex() => (PackageIndex)PackageValueCodec.Decode(File.ReadAllBytes(CatalogPath)[..^32])!;
        internal async Task WriteIndexAsync(PackageIndex index)
        {
            byte[] bytes = PackageValueCodec.Encode(index);
            await File.WriteAllBytesAsync(CatalogPath, [.. bytes, .. SHA256.HashData(bytes)]);
        }
        internal void Corrupt(PackageBlock block)
        {
            using FileStream data = new(DataPath, FileMode.Open, FileAccess.Write, FileShare.None);
            data.Position = block.Offset;
            data.WriteByte(0);
        }

        public void Dispose()
        {
            string parent = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string resolved = Path.GetFullPath(Root);
            if (!resolved.StartsWith(parent, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(resolved).StartsWith("CernealaScenePackageTests-", StringComparison.Ordinal))
            { throw new InvalidOperationException("Unsafe fixture cleanup target."); }
            Directory.Delete(resolved, recursive: true);
        }
    }
}
