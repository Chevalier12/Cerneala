using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;

public sealed class TileMapSource2DTests
{
    private static readonly ImageReference Picture = new(new ResourceId<ImageResource>("Picture"));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DataResidencyChargeIsExplicitNullableMetadataAndRejectsNegativeValues(bool grid)
    {
        SceneSpatialEntry2D spatial = new("chunk", new(0, 0, 10, 10), null);
        TileMapChunkInfo2D Create(long? charge) => grid
            ? new(spatial, new TileMapBounds2D(0, 0, 1, 1), [], [], dataResidencyBytes: charge)
            : new(spatial, 1, [Picture], dataResidencyBytes: charge);

        TileMapChunkInfo2D omitted = grid
            ? new(spatial, new TileMapBounds2D(0, 0, 1, 1), [], []) : new(spatial, 1, [Picture]);
        Assert.Null(omitted.DataResidencyBytes);
        foreach (long? charge in new long?[] { null, 0, 1, TileMap2D.WarmCacheBudgetBytes, long.MaxValue })
        {
            TileMapChunkInfo2D info = Create(charge);
            TileMapCatalog2D catalog = new("Map", [info], tileSize: grid ? new DrawSize(10, 10) : null);
            Assert.Equal(charge, Assert.Single(catalog.Chunks).DataResidencyBytes);
            Assert.Same(spatial, Assert.Single(catalog.Entries));
        }
        Assert.Equal("dataResidencyBytes", Assert.Throws<ArgumentOutOfRangeException>(() => Create(-1)).ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(long.MinValue));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(2097152L)]
    [InlineData(long.MaxValue)]
    public async Task RequiredAcquisitionsAcceptUnknownAndOversizedChargesWithOpaquePayloads(long? charge)
    {
        SceneSpatialEntry2D spatial = new("opaque", new(0, 0, 10, 10), null);
        TileMapChunkInfo2D info = new(spatial, new TileMapBounds2D(0, 0, 1, 1), [], [], dataResidencyBytes: charge);
        TileMapCatalog2D catalog = new("Map", [info], tileSize: new(10, 10));
        int loads = 0, releases = 0;
        TileMapSource2D source = new(catalog, (_, requested, _) =>
        {
            Assert.Same(info, requested);
            loads++;
            TileChunk2D grid = new(default, 1, 1, [default], properties:
                new Dictionary<string, object?> { ["opaque"] = new byte[2 * 1024 * 1024] });
            return ValueTask.FromResult(new SceneSpatialLease2D<TileMapChunkData2D>(new(grid, []), _ => releases++));
        });
        Assert.Equal(0, loads);
        await using SceneSpatialResidency2D<TileMapChunkData2D> residency = new(source);
        using (SceneSpatialRegion2D<TileMapChunkData2D> region = await residency.AcquireAsync(spatial.Bounds))
        {
            TileChunk2D grid = region.GetValue("opaque").Grid!;
            Assert.Equal(2 * 1024 * 1024, Assert.IsType<byte[]>(grid.Properties["opaque"]).Length);
            Assert.Equal(1, loads);
            Assert.Equal(1, residency.ResidentCount);
            Assert.Equal(0, releases);
        }
        Assert.Equal(1, releases);
        Assert.Equal(0, residency.ResidentCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InMemoryChargeIsZeroBecauseAcquisitionsBorrowSourceOwnedPayloads(bool grid)
    {
        byte[] opaque = new byte[2 * 1024 * 1024];
        TileMap2DModel model = grid
            ? new("Map", new DrawSize(10, 10), [],
                [new TileChunk2D(default, 1, 1, [default], properties: new Dictionary<string, object?> { ["opaque"] = opaque })])
            : new([new Tile(Picture, width: 10, height: 10)]);
        TileMapSource2D source = TileMapSource2D.FromModel(model);
        TileMapChunkInfo2D info = Assert.Single(source.Catalog.Chunks);
        Assert.Equal(0L, info.DataResidencyBytes);
        TileMapChunkData2D original;
        using (SceneSpatialLease2D<TileMapChunkData2D> first = await source.LoadAsync(info.Spatial)) { original = first.Value; }
        using SceneSpatialLease2D<TileMapChunkData2D> again = await source.LoadAsync(info.Spatial);
        Assert.Same(original, again.Value);
        if (grid) { Assert.Same(opaque, again.Value.Grid!.Properties["opaque"]); }
        else { Assert.Same(model.Tiles[0], again.Value.Placements[0]); }
    }

    [Fact]
    public async Task InMemoryAdapterPreservesTheCompleteSnapshotAndNaturalDimensionsWithoutLoadingImages()
    {
        Tile[] tiles = [new(Picture, x: -5, y: 7), new(Picture, x: 40, width: 9), new(Picture, y: 100, height: 3)];
        TileMap2DModel model = new(tiles);
        Dictionary<string, DrawSize> sizes = new() { ["Picture"] = new(20, 30) };
        TileMapSource2D source = TileMapSource2D.FromModel(model, sizes);
        sizes["Picture"] = new(999, 999);

        TileMapChunkInfo2D info = Assert.Single(source.Catalog.Chunks);
        Assert.Equal(new DrawRect(-5, 0, 54, 103), info.Spatial.Bounds);
        Assert.Null(info.Spatial.CollisionBounds);
        Assert.True(source.Catalog.TryGetImageSize(Picture, out DrawSize size));
        Assert.Equal(new DrawSize(20, 30), size);
        Assert.Equal(new DrawRect(40, 0, 9, 30), source.Catalog.GetPlacementDestination(tiles[1]));
        Assert.Equal(new DrawRect(0, 100, 20, 3), source.Catalog.GetPlacementDestination(tiles[2]));
        using (SceneSpatialLease2D<TileMapChunkData2D> lease = await source.LoadAsync(info.Spatial))
        {
            Assert.Equal(tiles, lease.Value.Placements);
            Assert.Same(tiles[0], lease.Value.Placements[0]);
        }
        Assert.Equal(tiles, model.Tiles);
        using SceneSpatialLease2D<TileMapChunkData2D> again = await source.LoadAsync(info.Spatial);
        Assert.Same(tiles[0], again.Value.Placements[0]);
    }

    [Fact]
    public void MissingNaturalDimensionMetadataIsAnExplicitErrorButExplicitDimensionsNeedNone()
    {
        foreach (Tile tile in new[] { new Tile(Picture), new Tile(Picture, width: 7), new Tile(Picture, height: 8) })
        {
            Assert.Throws<ArgumentException>(() => TileMapSource2D.FromModel(new TileMap2DModel([tile])));
        }
        TileMapSource2D explicitSize = TileMapSource2D.FromModel(new TileMap2DModel([new Tile(Picture, width: 7, height: 8)]));
        Assert.Equal(new DrawRect(0, 0, 7, 8), Assert.Single(explicitSize.Entries).Bounds);
    }

    [Fact]
    public async Task GridCatalogIncludesActualRemoteCollisionEnvelopeButDefinitionsRequireAnAcquisition()
    {
        TileColliderDescriptor2D collider = new(TileColliderShape2D.Box, width: 4, height: 6, offsetX: 1000);
        TileSet2D set = new("Set", new("Atlas"), [new TileDefinition2D(1, new(0, 0, 16, 16), collider: collider)]);
        TileChunk2D chunk = new(new(-2, 3), 2, 1, [new(1), default]);
        TileMap2DModel model = new("Map", new(16, 16), [set], [chunk], offset: new(50, 70));
        TileMapSource2D source = TileMapSource2D.FromModel(model);

        TileMapChunkInfo2D info = Assert.Single(source.Catalog.Chunks);
        Assert.Equal(new DrawRect(-32, 48, 32, 16), info.Spatial.Bounds);
        Assert.Equal(new DrawRect(968, 48, 4, 6), info.Spatial.CollisionBounds);
        Assert.Equal(new DrawPoint(50, 70), source.Catalog.Offset);
        Assert.Equal(1, info.ExpandedColliderCount);
        Assert.Equal([1], info.TileIds);
        Assert.Equal(set.AtlasResourceId, Assert.Single(info.Images).ResourceId);
        using SceneSpatialLease2D<TileMapChunkData2D> payload = await source.LoadAsync(info.Spatial);
        Assert.True(payload.Value.TryResolveTile(1, out TileSet2D? foundSet, out TileDefinition2D? definition));
        Assert.Equal(set.Id, foundSet!.Id);
        Assert.Same(set.Tiles[0], definition);
        Assert.Same(collider, definition!.Collider);
    }

    [Fact]
    public async Task ReadingMetadataNeverCallsTheLoaderAndOverlappingRegionsShareAnAcquisition()
    {
        TileMapCatalog2D catalog = Catalog([Info("near", 0), Info("far", 1000)]);
        List<string> loads = [], releases = [];
        TileMapSource2D source = new(catalog, (_, info, _) =>
        {
            loads.Add(info.Spatial.Id);
            return ValueTask.FromResult(new SceneSpatialLease2D<TileMapChunkData2D>(Payload(info), _ => releases.Add(info.Spatial.Id)));
        });
        Assert.Same(catalog, source.Catalog);
        Assert.Equal(2, source.Entries.Count);
        Assert.Empty(loads);

        using SceneSpatialResidency2D<TileMapChunkData2D> residency = new(source);
        SceneSpatialRegion2D<TileMapChunkData2D> first = await residency.AcquireAsync(new DrawRect(0, 0, 5, 5));
        SceneSpatialRegion2D<TileMapChunkData2D> second = await residency.AcquireAsync(new DrawRect(0, 0, 9, 9));
        Assert.Equal(["near"], loads);
        Assert.Same(first.GetValue("near"), second.GetValue("near"));
        first.Dispose();
        Assert.Empty(releases);
        second.Dispose();
        Assert.Equal(["near"], releases);
        Assert.Equal(0, residency.ResidentCount);
        using SceneSpatialRegion2D<TileMapChunkData2D> far = await residency.AcquireAsync(new DrawRect(1000, 0, 5, 5));
        Assert.Equal(["near", "far"], loads);
    }

    [Fact]
    public async Task PublicationChangesTheCatalogAtomicallyAndDoesNotRevokeAnOwnedOldPayload()
    {
        TileMapCatalog2D before = Catalog([Info("old", 0)]);
        TileMapSource2D source = new(before, (_, info, _) => ValueTask.FromResult(new SceneSpatialLease2D<TileMapChunkData2D>(Payload(info))));
        using SceneSpatialResidency2D<TileMapChunkData2D> residency = new(source);
        using SceneSpatialRegion2D<TileMapChunkData2D> region = await residency.AcquireAsync(new DrawRect(0, 0, 5, 5));
        TileMapCatalog2D replacement = Catalog([Info("next", 1000)]);
        int publications = 0;
        source.Changed += (_, _) =>
        {
            Assert.Same(replacement, source.Catalog);
            Assert.Same(replacement.Entries, source.Entries);
            publications++;
        };
        await Task.Run(() => source.SetCatalog(replacement));
        Assert.False(region.IsCurrent);
        Assert.NotNull(region.GetValue("old"));
        Assert.Equal(1, publications);
        Assert.Equal("old", Assert.Single(before.Entries).Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => source.LoadAsync(before.Entries[0]).AsTask());
    }

    [Theory]
    [InlineData("count")]
    [InlineData("image")]
    [InlineData("visual")]
    [InlineData("collision")]
    [InlineData("kind")]
    public async Task InvalidPayloadIsRejectedAndItsLeaseReleasedExactlyOnce(string violation)
    {
        TileMapChunkInfo2D info = Info("a", 0);
        TileMapChunkData2D invalid = violation switch
        {
            "count" => new([new Tile(Picture, width: 10, height: 10), new Tile(Picture, width: 10, height: 10)]),
            "image" => new([new Tile(new ImageReference(new ResourceId<ImageResource>("Other")), width: 10, height: 10)]),
            "visual" => new([new Tile(Picture, x: 100, width: 10, height: 10)]),
            "collision" => new([new Tile(Picture, new TileColliderDescriptor2D(TileColliderShape2D.Box, width: 1, height: 1), width: 10, height: 10)]),
            _ => new(new TileChunk2D(default, 1, 1, [default]), [])
        };
        int releases = 0;
        TileMapSource2D source = new(Catalog([info]), (_, _, _) =>
            ValueTask.FromResult(new SceneSpatialLease2D<TileMapChunkData2D>(invalid, _ => releases++)));

        await Assert.ThrowsAsync<ArgumentException>(() => source.LoadAsync(info.Spatial).AsTask());
        Assert.Equal(1, releases);
    }

    [Fact]
    public async Task ValidationAndReleaseFailuresAreBothObservable()
    {
        TileMapChunkInfo2D info = Info("a", 0);
        TileMapSource2D source = new(Catalog([info]), (_, _, _) => ValueTask.FromResult(
            new SceneSpatialLease2D<TileMapChunkData2D>(new([]), _ => throw new IOException("release"))));
        AggregateException failure = await Assert.ThrowsAsync<AggregateException>(() => source.LoadAsync(info.Spatial).AsTask());
        Assert.IsType<ArgumentException>(failure.InnerExceptions[0]);
        Assert.Equal("release", failure.InnerExceptions[1].Message);
    }

    [Fact]
    public async Task LateCancelledCompletionIsReleasedWithoutPublishingAResident()
    {
        TileMapChunkInfo2D info = Info("a", 0);
        TaskCompletionSource<SceneSpatialLease2D<TileMapChunkData2D>> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int releases = 0;
        TileMapSource2D source = new(Catalog([info]), (_, _, _) => new(completion.Task));
        SceneSpatialResidency2D<TileMapChunkData2D> residency = new(source);
        using CancellationTokenSource cancellation = new();
        Task<SceneSpatialRegion2D<TileMapChunkData2D>> request = residency.AcquireAsync(new DrawRect(0, 0, 5, 5), cancellationToken: cancellation.Token).AsTask();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        completion.SetResult(new(Payload(info), _ => Interlocked.Increment(ref releases)));
        await residency.DisposeAsync();
        Assert.Equal(1, releases);
        Assert.Equal(0, residency.ResidentCount);
    }

    [Fact]
    public async Task LateValidPayloadReleaseFailureRemainsObservableByAsyncResidencyDisposal()
    {
        TileMapChunkInfo2D info = Info("a", 0);
        TaskCompletionSource<SceneSpatialLease2D<TileMapChunkData2D>> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TileMapSource2D source = new(Catalog([info]), (_, _, _) => new(completion.Task));
        SceneSpatialResidency2D<TileMapChunkData2D> residency = new(source);
        using CancellationTokenSource cancellation = new();
        Task<SceneSpatialRegion2D<TileMapChunkData2D>> request = residency.AcquireAsync(new DrawRect(0, 0, 5, 5), cancellationToken: cancellation.Token).AsTask();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        completion.SetResult(new(Payload(info), _ => throw new IOException("late release")));

        AggregateException failure = await Assert.ThrowsAsync<AggregateException>(() => residency.DisposeAsync().AsTask());
        Assert.Contains(failure.Flatten().InnerExceptions, error => error is IOException && error.Message == "late release");
    }

    [Fact]
    public async Task RepeatedNearFarAndEmptyInterestsReleaseEveryGeneratedPayload()
    {
        TileMapCatalog2D catalog = Catalog([Info("near", 0), Info("far", 1000)]);
        int loads = 0, releases = 0;
        TileMapSource2D source = new(catalog, (_, info, _) =>
        {
            loads++;
            return ValueTask.FromResult(new SceneSpatialLease2D<TileMapChunkData2D>(Payload(info), _ => releases++));
        });
        await using SceneSpatialResidency2D<TileMapChunkData2D> residency = new(source);
        for (int cycle = 0; cycle < 64; cycle++)
        {
            foreach (int x in new[] { 0, 1000, 2000 })
            {
                using (SceneSpatialRegion2D<TileMapChunkData2D> region = await residency.AcquireAsync(new DrawRect(x, 0, 5, 5)))
                {
                    Assert.Equal(x == 2000 ? 0 : 1, region.Entries.Count);
                    Assert.Equal(x == 2000 ? 0 : 1, residency.ResidentCount);
                }
                Assert.Equal(0, residency.ResidentCount);
                Assert.Equal(loads, releases);
            }
        }
        Assert.Equal(128, loads);
        Assert.Equal(128, releases);
    }

    [Fact]
    public async Task LoaderErrorsNullAndAlreadyDisposedLeasesAreNotEmptySuccesses()
    {
        TileMapChunkInfo2D info = Info("a", 0);
        TileMapCatalog2D catalog = Catalog([info]);
        TileMapSource2D ioFailure = new(catalog, (_, _, _) => throw new IOException("read failed"));
        Assert.Equal("read failed", (await Assert.ThrowsAsync<IOException>(() => ioFailure.LoadAsync(info.Spatial).AsTask())).Message);
        TileMapSource2D nullLease = new(catalog, (_, _, _) => ValueTask.FromResult<SceneSpatialLease2D<TileMapChunkData2D>>(null!));
        await Assert.ThrowsAsync<InvalidOperationException>(() => nullLease.LoadAsync(info.Spatial).AsTask());
        SceneSpatialLease2D<TileMapChunkData2D> disposed = new(Payload(info));
        disposed.Dispose();
        TileMapSource2D disposedLease = new(catalog, (_, _, _) => ValueTask.FromResult(disposed));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => disposedLease.LoadAsync(info.Spatial).AsTask());
        int calls = 0;
        TileMapSource2D cancelled = new(catalog, (_, _, _) => { calls++; return ValueTask.FromResult(new SceneSpatialLease2D<TileMapChunkData2D>(Payload(info))); });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled.LoadAsync(info.Spatial, new CancellationToken(true)).AsTask());
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task FreePlacementChunkBoundariesKeepDeclarationOrderAndBorrowExistingImages()
    {
        BorrowedImage image = new();
        ImageReference direct = new(image);
        Tile[] tiles = Enumerable.Range(0, 257).Select(index => new Tile(direct, x: index % 2, y: index)).ToArray();
        TileMapSource2D source = TileMapSource2D.FromModel(new TileMap2DModel(tiles));
        Assert.Equal([256, 1], source.Catalog.Chunks.Select(info => info.TileCount));
        Assert.True(source.Catalog.TryGetImageSize(direct, out DrawSize size));
        Assert.Equal(new DrawSize(20, 30), size);
        List<Tile> returned = [];
        foreach (SceneSpatialEntry2D entry in source.Entries)
        {
            using SceneSpatialLease2D<TileMapChunkData2D> lease = await source.LoadAsync(entry);
            returned.AddRange(lease.Value.Placements);
        }
        Assert.Equal(tiles, returned);
        Assert.False(image.Disposed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CollisionGeometryMustFitItsDeclaredEnvelopeEvenWhenItsCountMatches(bool grid)
    {
        TileColliderDescriptor2D descriptor = new(TileColliderShape2D.Segment, points: "0,0 0,10", offsetX: 100);
        SceneSpatialEntry2D spatial = new("chunk", new(0, 0, 10, 10), new DrawRect(0, 0, 0, 10));
        TileMapChunkInfo2D info = grid ? new(spatial, new TileMapBounds2D(0, 0, 1, 1), [1], [new(new ResourceId<ImageResource>("Atlas"))], 1) : new(spatial, 1, [Picture], 1);
        TileSet2D set = new("Set", new("Atlas"), [new TileDefinition2D(1, new(0, 0, 10, 10), collider: descriptor)]);
        TileMapCatalog2D catalog = new("Map", [info], tileSize: grid ? new DrawSize(10, 10) : null);
        TileMapChunkData2D data = grid ? new(new TileChunk2D(default, 1, 1, [new TileCell2D(1)]), [set]) : new([new Tile(Picture, descriptor, width: 10, height: 10)]);
        TileMapSource2D source = new(catalog, (_, _, _) => ValueTask.FromResult(new SceneSpatialLease2D<TileMapChunkData2D>(data)));
        ArgumentException failure = await Assert.ThrowsAsync<ArgumentException>(() => source.LoadAsync(spatial).AsTask());
        Assert.Equal("Loaded collision geometry exceeds its declared bounds.", failure.Message);
    }

    private sealed class BorrowedImage : IDrawImage, IDisposable
    {
        public int Width => 20;
        public int Height => 30;
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }

    [Fact]
    public void MetadataCollectionsAreCopiedAndInvalidGeometryCannotBePublished()
    {
        List<TileMapChunkInfo2D> chunks = [Info("a", 0)];
        TileMapCatalog2D catalog = Catalog(chunks);
        chunks.Clear();
        Assert.Single(catalog.Chunks);
        Assert.Throws<NotSupportedException>(() => ((IList<TileMapChunkInfo2D>)catalog.Chunks).Clear());
        Assert.Throws<ArgumentException>(() => Catalog([Info("a", 0), Info("a", 100)]));
        Assert.Throws<ArgumentException>(() => new TileMapCatalog2D("Bad", [Info("a", 0)], imageSizes: new Dictionary<string, DrawSize> { ["Picture"] = default }));
        TileMapChunkInfo2D wrongGrid = new(new SceneSpatialEntry2D("a", new(0, 0, 1, 1), null), new TileMapBounds2D(0, 0, 2, 2), [], []);
        Assert.Throws<ArgumentException>(() => new TileMapCatalog2D("Bad", [wrongGrid], tileSize: new(10, 10)));
    }

    [Fact]
    public void GridMetadataRejectsOverlapButDoesNotNeedUnloadedDefinitions()
    {
        TileMapChunkInfo2D first = GridInfo("first", 0, 4, []);
        TileMapChunkInfo2D overlap = GridInfo("overlap", 3, 2, []);
        Assert.Throws<ArgumentException>(() => new TileMapCatalog2D("Map", [first, overlap], tileSize: new(10, 10)));
        TileMapCatalog2D unloaded = new("Map", [GridInfo("unloaded", 0, 1, [99])], tileSize: new(10, 10));
        Assert.Equal([99], Assert.Single(unloaded.Chunks).TileIds);
        TileMapCatalog2D sparse = new("Map", [first, GridInfo("remote", -100_000, 2, [])], tileSize: new(10, 10));
        Assert.Equal(2, sparse.Chunks.Count);
    }

    [Fact]
    public async Task GridPayloadMustMatchCoordinatesRevisionAndDeclaredTileIds()
    {
        TileMapChunkInfo2D info = GridInfo("a", 0, 1, []);
        foreach (TileChunk2D grid in new[]
        {
            new TileChunk2D(new(1, 0), 1, 1, [default]),
            new TileChunk2D(default, 1, 1, [default], version: 2),
            new TileChunk2D(default, 1, 1, [new TileCell2D(1)])
        })
        {
            TileSet2D set = new("Set", new("Atlas"), [new TileDefinition2D(1, new(0, 0, 10, 10))]);
            TileMapSource2D source = new(new("Map", [info], tileSize: new(10, 10)),
                (_, _, _) => ValueTask.FromResult(new SceneSpatialLease2D<TileMapChunkData2D>(
                    new(grid, grid.Tiles.Any(cell => cell.TileId != 0) ? [set] : []))));
            await Assert.ThrowsAsync<ArgumentException>(() => source.LoadAsync(info.Spatial).AsTask());
        }
    }

    private static TileMapCatalog2D Catalog(IEnumerable<TileMapChunkInfo2D> chunks) => new("Map", chunks);
    private static TileMapChunkInfo2D Info(string id, float x) =>
        new(new SceneSpatialEntry2D(id, new(x, 0, 10, 10), null), 1, [Picture]);
    private static TileMapChunkData2D Payload(TileMapChunkInfo2D info) =>
        new([new Tile(Picture, x: info.Spatial.Bounds.X, width: 10, height: 10)]);
    private static TileMapChunkInfo2D GridInfo(string id, int x, int width, IEnumerable<int> ids) =>
        new(new SceneSpatialEntry2D(id, new(x * 10, 0, width * 10, 10), null), new TileMapBounds2D(x, 0, width, 1), ids, []);
}
