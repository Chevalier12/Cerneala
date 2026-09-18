using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;
using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class TileMapSpatialResidencyTests
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    public async Task CompletedAsyncWarmPayloadIsReusedWhenItBecomesRequired(bool checkInputBeforeUpdate, int cameraTiming)
    {
        using Fixture fixture = new();
        TileMapSource2D backing = TileMapSource2D.FromModel(AdjacentModel(fixture.Model));
        SceneSpatialEntry2D warm = backing.Entries[1];
        TaskCompletionSource<SceneSpatialLease2D<TileMapChunkData2D>> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int loads = 0, releases = 0, allLoads = 0;
        fixture.Surface.ViewBox = new(0, 0, 10, 10);
        fixture.Map.Source = new(backing.Catalog, (_, info, cancellation) =>
        {
            Interlocked.Increment(ref allLoads);
            if (info.Spatial.Id != warm.Id) { return backing.LoadAsync(info.Spatial, cancellation); }
            return Interlocked.Increment(ref loads) == 1 ? new(pending.Task) : CreateLease();
        });
        Assert.Single(fixture.Record());
        Assert.Equal(1, loads);
        Assert.Equal(1, fixture.Map.GetDiagnosticsSnapshot().WarmPendingChunks);
        pending.SetResult(await CreateLease());
        Assert.True(SpinWait.SpinUntil(() =>
        {
            fixture.Record();
            return fixture.Map.GetDiagnosticsSnapshot().WarmChunks == 1;
        }, TimeSpan.FromSeconds(5)));

        int preparedLoads = allLoads;
        if (cameraTiming == 2) { fixture.Surface.Draw += (_, _) => ChangeCamera(); }
        else { ChangeCamera(); }
        // Native input can change the camera after the time-sensitive update,
        // before the backend asks the retained surface to record that frame.
        if (cameraTiming != 0)
        {
            ((IRenderSurface2DFrameSource)fixture.Surface).RecordFrame(new DrawCommandList(), new(0, 0, 100, 100));
            Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().WarmTilesPrepared);
            Assert.Equal(preparedLoads, allLoads);
            Assert.Equal(0, releases);
        }
        Assert.Single(fixture.Record());
        Assert.Equal(1, loads);
        Assert.Equal(0, releases);

        void ChangeCamera()
        {
            fixture.Surface.ViewBox = new(10, 0, 10, 10);
            if (checkInputBeforeUpdate) { Assert.True(fixture.Surface.CanRouteSceneInput); }
        }

        async ValueTask<SceneSpatialLease2D<TileMapChunkData2D>> CreateLease()
        {
            var acquisition = await backing.LoadAsync(warm);
            return new(acquisition.Value, _ => { acquisition.Dispose(); Interlocked.Increment(ref releases); });
        }
    }

    [Fact]
    public void SurfaceSharesPreparationFairlyWithoutClippingRequiredDrawing()
    {
        using Fixture fixture = new();
        fixture.Map.PublishModel(DenseModel());
        TileMap2D[] maps = [fixture.Map, new() { Source = TileMapTestSource.Create(DenseModel()) }, new() { Source = TileMapTestSource.Create(DenseModel()) }];
        fixture.Scene.Children.Add(maps[1]);
        fixture.Scene.Children.Add(maps[2]);
        fixture.Surface.ViewBox = new(320, 240, 480, 320);
        int[] grantedFrames = new int[maps.Length];
        for (int frame = 0; frame < 9; frame++)
        {
            Assert.Equal(27, fixture.Record().Length);
            TileMap2DDiagnosticsSnapshot[] states = maps.Select(map => map.GetDiagnosticsSnapshot()).ToArray();
            Assert.Equal(256, states.Sum(state => state.WarmTilesPrepared));
            Assert.Single(states.Where(state => state.WarmTilesPrepared > 0));
            for (int index = 0; index < maps.Length; index++)
            {
                Assert.Equal(9 * 256, states[index].DrawnTiles);
                Assert.Equal(frame == 0 ? 9 : 0, states[index].BatchesRebuilt);
                Assert.InRange(states[index].WarmChargedBytes, 0, TileMap2D.WarmCacheBudgetBytes);
                if (states[index].WarmTilesPrepared > 0) { grantedFrames[index]++; }
            }
            Assert.InRange(grantedFrames.Max() - grantedFrames.Min(), 0, 1);
        }
        Assert.All(grantedFrames, count => Assert.Equal(3, count));
    }

    [Fact]
    public void SurfaceBudgetIsIndependentAndDoesNotWasteTheTurnOfAMapWithoutOptionalWork()
    {
        using Fixture first = new();
        using Fixture second = new();
        first.Map.PublishModel(DenseModel());
        second.Map.PublishModel(DenseModel());
        TileMap2D noOptional = new() { Source = TileMapTestSource.Create(first.Model) };
        TileMap2D other = new() { Source = TileMapTestSource.Create(DenseModel()) };
        first.Scene.Children.Add(noOptional);
        first.Scene.Children.Add(other);
        first.Surface.ViewBox = second.Surface.ViewBox = new(320, 240, 480, 320);
        int firstGrants = 0, otherGrants = 0;
        for (int frame = 0; frame < 6; frame++)
        {
            first.Record();
            second.Record();
            int firstCells = first.Map.GetDiagnosticsSnapshot().WarmTilesPrepared;
            int otherCells = other.GetDiagnosticsSnapshot().WarmTilesPrepared;
            Assert.Equal(0, noOptional.GetDiagnosticsSnapshot().WarmTilesPrepared);
            Assert.Equal(256, firstCells + otherCells);
            Assert.Equal(256, second.Map.GetDiagnosticsSnapshot().WarmTilesPrepared);
            if (firstCells > 0) { firstGrants++; }
            if (otherCells > 0) { otherGrants++; }
            Assert.InRange(Math.Abs(firstGrants - otherGrants), 0, 1);
        }
        Assert.Equal(3, firstGrants);
        Assert.Equal(3, otherGrants);
    }

    [Fact]
    public void OptionalPreparationWaitsUntilRequiredSceneRecordingHasFinished()
    {
        using Fixture fixture = new();
        fixture.Map.PublishModel(DenseModel());
        fixture.Surface.ViewBox = new(320, 240, 480, 320);
        bool observed = false;
        fixture.Scene.Children.Add(new RecordingProbe(() =>
        {
            Assert.Equal(9 * 256, fixture.Map.GetDiagnosticsSnapshot().DrawnTiles);
            Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().WarmTilesPrepared);
            observed = true;
        }));
        fixture.Record();
        Assert.True(observed);
        Assert.Equal(256, fixture.Map.GetDiagnosticsSnapshot().WarmTilesPrepared);
    }

    [Fact]
    public void SmallerRemainderChunksCannotStarveAFullBudgetChunk()
    {
        using Fixture fixture = new();
        TileMap2D[] maps = [fixture.Map, new(), new()];
        for (int index = 0; index < maps.Length; index++)
        {
            int width = index == 1 ? 16 : 8;
            maps[index].PublishModel(new("mixed", new DrawSize(10, 10), DenseModel().TileSets,
                [new TileChunk2D(new(8 - width, 0), width, 16, Enumerable.Repeat(new TileCell2D(1), width * 16)),
                 new TileChunk2D(new(8, 0), width, 16, Enumerable.Repeat(new TileCell2D(1), width * 16))]));
            if (index > 0) { fixture.Scene.Children.Add(maps[index]); }
        }
        fixture.Surface.ViewBox = new(0, 0, 80, 160);
        int fullBudgetTurns = 0;
        for (int frame = 0; frame < 6; frame++)
        {
            fixture.Record();
            Assert.Equal(256, maps.Sum(map => map.GetDiagnosticsSnapshot().WarmTilesPrepared));
            if (maps[1].GetDiagnosticsSnapshot().WarmTilesPrepared == 256) { fullBudgetTurns++; }
            // Keep every map backlogged with its actual chunk size. Rotating
            // after the last remainder recipient would starve the middle map.
            foreach (TileMap2D map in maps) { map.ReleaseRenderCaches(); }
        }
        Assert.InRange(fullBudgetTurns, 2, 4);
    }

    [Fact]
    public void CompletingAnOlderFrameCannotChangeTheNewerRecordingOrPrepareMoreCells()
    {
        using Fixture fixture = new();
        fixture.Map.PublishModel(DenseModel());
        RenderSurface2DFrame first = new(new DrawCommandList(), new(320, 240, 480, 320), TimeSpan.Zero);
        RenderSurface2DFrame second = new(new DrawCommandList(), first.Bounds, TimeSpan.Zero);
        fixture.Surface.ViewBox = first.Bounds;
        fixture.Tick();
        fixture.Map.Record(new(fixture.Surface, first, System.Numerics.Matrix3x2.Identity, first.Bounds));
        fixture.Map.Record(new(fixture.Surface, second, System.Numerics.Matrix3x2.Identity, second.Bounds));
        second.Complete();
        TileMap2DDiagnosticsSnapshot current = fixture.Map.GetDiagnosticsSnapshot();
        Assert.Equal(256, current.WarmTilesPrepared);
        first.Complete();
        Assert.Equal(current, fixture.Map.GetDiagnosticsSnapshot());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CatalogPublicationOrDetachCannotResurrectPendingPreparation(bool detach)
    {
        using Fixture fixture = new();
        fixture.Map.PublishModel(DenseModel());
        RenderSurface2DFrame frame = new(new DrawCommandList(), new(320, 240, 480, 320), TimeSpan.Zero);
        fixture.Surface.ViewBox = frame.Bounds;
        fixture.Tick();
        fixture.Map.Record(new(fixture.Surface, frame, System.Numerics.Matrix3x2.Identity, frame.Bounds));
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().WarmTilesPrepared);
        if (detach) { fixture.Scene.Children.Remove(fixture.Map); }
        else { fixture.Map.PublishModel(new("empty", fixture.Model.TileSize, fixture.Model.TileSets, [])); }
        frame.Complete();
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().WarmChunks);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().RetainedBytes);
        Assert.Equal(0, fixture.Root.ImageResourceCache!.ResidentCount);
    }

    [Fact]
    public void FailedRequiredRecordingCancelsOptionalWorkAndTheNextFrameCanRecover()
    {
        using Fixture fixture = new();
        fixture.Map.PublishModel(DenseModel());
        fixture.Surface.ViewBox = new(320, 240, 480, 320);
        RenderSurface2DFrame? failedFrame = null;
        fixture.Surface.Draw += (_, frame) => failedFrame = frame;
        RecordingProbe failure = new(() => throw new InvalidOperationException("required recording failed"));
        fixture.Scene.Children.Add(failure);
        Assert.Equal("required recording failed", Assert.Throws<InvalidOperationException>(() => fixture.Record()).Message);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().WarmTilesPrepared);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().WarmChunks);
        Assert.Throws<ObjectDisposedException>(() => failedFrame!.FillRectangle(new(0, 0, 1, 1), Color.Red));
        fixture.Scene.Children.Remove(failure);
        Assert.Equal(9, fixture.Record().Length);
        Assert.Equal(256, fixture.Map.GetDiagnosticsSnapshot().WarmTilesPrepared);
    }

    private sealed class RecordingProbe(Action record) : SceneNode2D
    {
        internal override void Record(Scene2DRecordContext context) => record();
        internal override SceneBounds2D GetVisibleLocalBounds() => SceneBounds2D.Unknown;
    }

    [Fact]
    public void ReturningAcrossACameraBoundaryReusesTheWarmBatchWithoutDrawingItOffscreen()
    {
        using Fixture fixture = new();
        fixture.Map.PublishModel(AdjacentModel(fixture.Model));
        fixture.Surface.ViewBox = new(0, 0, 20, 20);
        DrawSpriteBatch first = fixture.Record()[0];
        TestImage image = fixture.Loader.Loads[0];

        fixture.Surface.ViewBox = new(10, 0, 20, 20);
        Assert.All(fixture.Record(), batch => Assert.All(batch.Sprites, sprite => Assert.True(sprite.Destination.X >= 10)));
        Assert.False(image.IsDisposed);

        fixture.Surface.ViewBox = new(0, 0, 20, 20);
        Assert.Same(first, fixture.Record()[0]);
        Assert.Single(fixture.Loader.Loads);
    }

    [Fact]
    public void AdjacentChunkWithResidentAtlasIsPreparedBeforeItBecomesVisible()
    {
        using Fixture fixture = new();
        fixture.Map.PublishModel(AdjacentModel(fixture.Model));
        fixture.Surface.ViewBox = new(0, 0, 10, 10);
        DrawSpriteBatch initial = Assert.Single(fixture.Record());
        Assert.Equal(0, Assert.Single(initial.Sprites).Destination.X);

        fixture.Surface.ViewBox = new(10, 0, 10, 10);
        DrawSpriteBatch next = Assert.Single(fixture.Record());
        Assert.Equal(10, Assert.Single(next.Sprites).Destination.X);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().BatchesRebuilt);
        Assert.Single(fixture.Loader.Loads);
    }

    [Fact]
    public void PublishingWithoutAWarmChunkRetiresItsExclusiveImageBeforeAnotherFrame()
    {
        using Fixture fixture = new();
        fixture.Map.PublishModel(AdjacentModel(fixture.Model, separateAtlas: true));
        fixture.Surface.ViewBox = new(0, 0, 10, 10);
        Assert.Single(fixture.Record());
        TestImage first = fixture.Loader.Loads[0];

        fixture.Surface.ViewBox = new(10, 0, 10, 10);
        Assert.Single(fixture.Record());
        Assert.False(first.IsDisposed);
        fixture.Map.PublishModel(new("edited", fixture.Model.TileSize, fixture.Model.TileSets,
            [new TileChunk2D(new(1, 0), 1, 1, [new TileCell2D(2)])]));
        Assert.True(first.IsDisposed);
        Assert.Equal(1, first.DisposeCount);
    }

    private static TileMap2DModel AdjacentModel(TileMap2DModel model, bool separateAtlas = false) =>
        new("adjacent", model.TileSize, model.TileSets,
            Enumerable.Range(0, 4).Select(x => new TileChunk2D(new(x, 0), 1, 1,
                [new TileCell2D(separateAtlas && x > 0 ? 2 : 1)])));

    [Fact]
    public void StableWarmViewStillObservesCellAndImageReplacement()
    {
        using Fixture fixture = new();
        fixture.Map.PublishModel(AdjacentModel(fixture.Model));
        fixture.Surface.ViewBox = new(0, 0, 10, 10);
        for (int frame = 0; frame < 4; frame++) { fixture.Record(); }

        ImageResource farResource = new("far.png");
        fixture.Surface.Resources.SetResource(new ResourceId<ImageResource>("FarAtlas"), farResource);
        using ImageResourceLease resident = fixture.Root.ImageResourceCache!.Acquire(farResource);
        fixture.Map.PublishModel(new("edited", fixture.Model.TileSize, fixture.Model.TileSets,
            Enumerable.Range(0, 4).Select(x => new TileChunk2D(new(x, 0), 1, 1,
                [new TileCell2D(x == 1 ? 2 : 1)], version: x == 1 ? 2 : 1))));
        Assert.Equal("near.png", Assert.IsType<TestImage>(Assert.Single(fixture.Record()).Image).Path);
        Assert.Equal(1, fixture.Map.GetDiagnosticsSnapshot().WarmTilesPrepared);

        fixture.Surface.ViewBox = new(10, 0, 10, 10);
        DrawSpriteBatch prepared = Assert.Single(fixture.Record());
        Assert.Same(resident.Image, prepared.Image);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().BatchesRebuilt);

        fixture.Surface.ViewBox = new(0, 0, 10, 10);
        fixture.Record();
        ImageResource replacement = new("replacement.png");
        fixture.Surface.Resources.SetResource(new ResourceId<ImageResource>("FarAtlas"), replacement);
        using ImageResourceLease replacementResident = fixture.Root.ImageResourceCache.Acquire(replacement);
        fixture.Record();
        Assert.Equal(1, fixture.Map.GetDiagnosticsSnapshot().WarmTilesPrepared);
        fixture.Surface.ViewBox = new(10, 0, 10, 10);
        DrawSpriteBatch updated = Assert.Single(fixture.Record());
        Assert.Same(replacementResident.Image, updated.Image);
        Assert.NotSame(prepared, updated);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().BatchesRebuilt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StableWarmViewDoesNotReuseRemovedOrMovedCandidates(bool reorder)
    {
        using Fixture fixture = new();
        fixture.Map.PublishModel(AdjacentModel(fixture.Model));
        fixture.Surface.ViewBox = new(0, 0, 10, 10);
        fixture.Record();
        Assert.Equal(1, fixture.Map.GetDiagnosticsSnapshot().WarmChunks);

        TileChunk2D[] replacement =
        [new(default, 1, 1, [new TileCell2D(1)]),
         new(new(200, 0), 1, 1, [new TileCell2D(1)])];
        fixture.Map.PublishModel(new("replacement", fixture.Model.TileSize, fixture.Model.TileSets,
            reorder ? replacement.Reverse() : replacement));
        Assert.Single(fixture.Record());
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().WarmChunks);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().WarmTilesPrepared);
        fixture.Surface.ViewBox = new(10, 0, 10, 10);
        Assert.Empty(fixture.Record());
        fixture.Surface.ViewBox = new(2000, 0, 10, 10);
        Assert.Equal(2000, Assert.Single(Assert.Single(fixture.Record()).Sprites).Destination.X);
    }

    [Fact]
    public void ReleasedWarmSelectionCanBePreparedAgainAtTheIdenticalView()
    {
        using Fixture fixture = new();
        fixture.Map.PublishModel(AdjacentModel(fixture.Model));
        fixture.Surface.ViewBox = new(0, 0, 10, 10);
        DrawSpriteBatch original = Assert.Single(fixture.Record());
        Assert.Equal(1, fixture.Map.GetDiagnosticsSnapshot().WarmChunks);
        fixture.Map.ReleaseRenderCaches();
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().WarmChunks);
        Assert.NotSame(original, Assert.Single(fixture.Record()));
        Assert.Equal(1, fixture.Map.GetDiagnosticsSnapshot().WarmTilesPrepared);
        fixture.Surface.ViewBox = new(10, 0, 10, 10);
        Assert.Single(fixture.Record());
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().BatchesRebuilt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SharedGridMetadataTracksRepeatedZeroAndNonzeroCollisionPublications(bool copiedCells)
    {
        TileChunk2D grid = new(default, 1, 1, [new TileCell2D(1)]);
        TileColliderDescriptor2D collider = new(TileColliderShape2D.Box, width: 10, height: 10, offsetX: 2000);
        TileMap2D.TileRenderChunk? retained = null;
        for (int iteration = 0; iteration < 64; iteration++)
        {
            bool collides = iteration % 2 != 0;
            TileSet2D tileSet = new("palette", new("Atlas"),
                [new(1, new(0, 0, 10, 10), collider: collides ? collider : null),
                 new(2, new(0, 0, 10, 10), collider: collider)], version: iteration + 1);
            if (copiedCells) { grid = new(default, 1, 1, [new TileCell2D(1)]); }
            // An unused definition has a collider in every publication. Only
            // descriptors belonging to actual cells contribute to coverage.
            TileMap2DModel model = new("published", new DrawSize(10, 10), [tileSet], [grid]);
            TileMapCatalog2D catalog = TileMapSource2D.FromModel(model).Catalog;
            TileMapChunkInfo2D info = Assert.Single(catalog.Chunks);
            if (retained is null) { retained = new(info, 0); }
            else { retained.Update(info); }
            Assert.Same(info, retained.Info);
            Assert.Equal(new TileMapBounds2D(0, 0, 1, 1), retained.Info.Cells);
            Assert.Single(retained.ImageKeys);
            Assert.Equal(collides ? SceneBounds2D.Known(new(2000, 0, 10, 10)) : SceneBounds2D.Empty,
                retained.CollisionBounds);
        }
    }

    [Fact]
    public void FullOptionalBudgetSettlesWithoutRepeatedlyBuildingAndDiscardingTheSameChunks()
    {
        using Fixture fixture = new();
        fixture.Map.PublishModel(DenseModel());
        fixture.Surface.ViewBox = new(320, 240, 480, 320);
        for (int frame = 0; frame < 64; frame++)
        {
            fixture.Record();
            TileMap2DDiagnosticsSnapshot state = fixture.Map.GetDiagnosticsSnapshot();
            Assert.InRange(state.WarmChargedBytes, 0, TileMap2D.WarmCacheBudgetBytes);
            Assert.InRange(state.WarmTilesPrepared, 0, RenderSurface2D.WarmPreparationTileBudget);
        }
        fixture.Record();
        Assert.True(fixture.Map.GetDiagnosticsSnapshot().WarmChunks > 0);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().WarmTilesPrepared);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().BatchesBuilt);
    }

    [Fact]
    public void SmallCameraMotionDoesNotReplaceIncumbentsStillInsideTheSameWarmBandAndBudget()
    {
        using Fixture fixture = new();
        fixture.Map.PublishModel(DenseModel());
        fixture.Surface.ViewBox = new(321, 241, 480, 320);
        for (int frame = 0; frame < 64; frame++) { fixture.Record(); }
        int warmChunks = fixture.Map.GetDiagnosticsSnapshot().WarmChunks;
        Assert.True(warmChunks > 0);
        for (int frame = 0; frame < 32; frame++)
        {
            // Both rectangles select the same visible chunks and the same
            // expanded-band candidates; only their relative distances change.
            fixture.Surface.ViewBox = new(frame % 2 == 0 ? 335 : 321, 241, 480, 320);
            fixture.Record();
            var state = fixture.Map.GetDiagnosticsSnapshot();
            Assert.Equal(0, state.BatchesRebuilt);
            Assert.Equal(0, state.WarmTilesPrepared);
            Assert.Equal(warmChunks, state.WarmChunks);
        }
    }

    [Fact]
    public void DetectiveWarmSnapshotIsObservationalAndCacheReleaseClearsItsResidencyCounters()
    {
        using Fixture fixture = new();
        fixture.Map.PublishModel(AdjacentModel(fixture.Model));
        fixture.Surface.ViewBox = new(0, 0, 10, 10);
        fixture.Record();
        var first = fixture.Root.Detective.CaptureTileMap(fixture.Map);
        Assert.True(first.WarmChunks > 0);
        Assert.True(first.WarmChargedBytes > 0);
        Assert.Equal(first, fixture.Root.Detective.CaptureTileMap(fixture.Map));
        Assert.Single(fixture.Loader.Loads);
        fixture.Map.ReleaseRenderCaches();
        var cleared = fixture.Root.Detective.CaptureTileMap(fixture.Map);
        Assert.Equal(0, cleared.WarmChunks);
        Assert.Equal(0, cleared.WarmChargedBytes);
        Assert.Equal(0, cleared.WarmRetainedBytes);
        Assert.Equal(0, cleared.RetainedBytes);
        // The previously recorded surface frame has an independent acquisition.
        Assert.Equal(1, fixture.Root.ImageResourceCache!.ResidentCount);
        fixture.Surface.ViewBox = new(10_000, 0, 10, 10);
        Assert.Empty(fixture.Record());
        Assert.Equal(0, fixture.Root.ImageResourceCache!.ResidentCount);
    }

    private static TileMap2DModel DenseModel() =>
        new("dense", new DrawSize(10, 10),
            [new TileSet2D("near", new("NearAtlas"), [new TileDefinition2D(1, new(0, 0, 10, 10))])],
            Enumerable.Range(0, 100).Select(index => new TileChunk2D(new(index % 10 * 16, index / 10 * 16),
                16, 16, Enumerable.Repeat(new TileCell2D(1), 256))));

    [Fact]
    public void ExhaustedSurfaceBudgetDoesNotProbeAnUncachedOptionalImage()
    {
        using Fixture fixture = new();
        fixture.Map.PublishModel(new("first-budget-recipient", new DrawSize(.625f, .625f), DenseModel().TileSets,
            [new TileChunk2D(new(0, 0), 16, 16, Enumerable.Repeat(new TileCell2D(1), 256)),
             new TileChunk2D(new(16, 0), 16, 16, Enumerable.Repeat(new TileCell2D(1), 256))]));
        TileMap2D deferred = new() { Source = TileMapTestSource.Create(AdjacentModel(fixture.Model, separateAtlas: true)) };
        fixture.Scene.Children.Add(deferred);
        fixture.Surface.ViewBox = new(0, 0, 10, 10);
        ImageResource optional = new("far.png");
        fixture.Surface.Resources.SetResource(new ResourceId<ImageResource>("FarAtlas"), optional);
        using ImageResourceLease resident = fixture.Root.ImageResourceCache!.Acquire(optional);
        TestImage image = Assert.IsType<TestImage>(resident.Image);
        image.WidthReads = 0;

        fixture.Record();

        Assert.Equal(256, fixture.Map.GetDiagnosticsSnapshot().WarmTilesPrepared);
        Assert.Equal(0, deferred.GetDiagnosticsSnapshot().WarmTilesPrepared);
        Assert.Equal(0, image.WidthReads);
    }

    [Fact]
    public void OptionalPreparationDoesNotLoadAnUnknownAtlasAndReleasesAnOversizedWarmOnlyImage()
    {
        using Fixture fixture = new();
        fixture.Map.PublishModel(AdjacentModel(fixture.Model, separateAtlas: true));
        fixture.Surface.ViewBox = new(0, 0, 10, 10);
        Assert.Single(fixture.Record());
        Assert.Equal(["near.png"], fixture.Loader.Loads.Select(image => image.Path));

        fixture.Loader.Size = 1024;
        fixture.Surface.ViewBox = new(10, 0, 10, 10);
        Assert.Single(fixture.Record());
        TestImage large = fixture.Loader.Loads.Last();
        Assert.False(large.IsDisposed);
        fixture.Surface.ViewBox = new(0, 0, 10, 10);
        Assert.Single(fixture.Record());
        Assert.True(large.IsDisposed);
        Assert.InRange(fixture.Map.GetDiagnosticsSnapshot().WarmChargedBytes, 0, TileMap2D.WarmCacheBudgetBytes);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().WarmImageBytes);
    }

    [Fact]
    public void ThrowingWarmImageReleaseStillClearsAllLocalCacheResidency()
    {
        using Fixture fixture = new();
        fixture.Map.PublishModel(AdjacentModel(fixture.Model, separateAtlas: true));
        fixture.Surface.ViewBox = new(0, 0, 10, 10);
        fixture.Record();
        TestImage near = fixture.Loader.Loads[0];
        fixture.Surface.ViewBox = new(10, 0, 10, 10);
        fixture.Record();
        Assert.False(near.IsDisposed);
        near.ThrowOnDispose = true;
        Assert.Throws<AggregateException>(() => fixture.Map.ReleaseRenderCaches());
        Assert.Equal(1, near.DisposeCount);
        var state = fixture.Root.Detective.CaptureTileMap(fixture.Map);
        Assert.Equal(0, state.WarmChunks);
        Assert.Equal(0, state.WarmChargedBytes);
        Assert.Equal(0, state.RetainedBytes);
        fixture.Surface.ViewBox = new(10_000, 0, 10, 10);
        Assert.Empty(fixture.Record());
        Assert.Equal(0, fixture.Root.ImageResourceCache!.ResidentCount);
        Assert.All(fixture.Loader.Loads, image => Assert.Equal(1, image.DisposeCount));
    }

    [Fact]
    public void PreparedWorkAndResidencyStayBoundedAcrossThirtyTwoDenseCameraCycles()
    {
        using Fixture fixture = new();
        fixture.Map.PublishModel(DenseModel());
        for (int cycle = 0; cycle < 32; cycle++)
        {
            foreach (int x in new[] { 320, 640, 10_000, 320 })
            {
                fixture.Surface.ViewBox = new(x, 240, 480, 320);
                DrawSpriteBatch[] batches = fixture.Record();
                var state = fixture.Root.Detective.CaptureTileMap(fixture.Map);
                Assert.InRange(state.WarmChargedBytes, 0, TileMap2D.WarmCacheBudgetBytes);
                Assert.InRange(state.WarmTilesPrepared, 0, RenderSurface2D.WarmPreparationTileBudget);
                Assert.Equal(batches.Length, state.DrawCommands);
                if (x == 10_000)
                {
                    Assert.Empty(batches);
                    Assert.Equal(0, state.RetainedBytes);
                    Assert.Equal(0, state.WarmChunks);
                    Assert.Equal(0, fixture.Root.ImageResourceCache!.ResidentCount);
                }
            }
        }
        fixture.Dispose();
        Assert.All(fixture.Loader.Loads, image => Assert.Equal(1, image.DisposeCount));
    }

    [Fact]
    public void CameraRetiresGridImagesAndCachedBatchesWithoutPretendingToUnloadTheCallerModel()
    {
        using Fixture fixture = new();
        Assert.Single(fixture.Record());
        Assert.Equal(["near.png"], fixture.Loader.Loads.Select(image => image.Path));
        Assert.Equal(1, fixture.Root.ImageResourceCache!.ResidentCount);
        TestImage near = Assert.Single(fixture.Loader.Loads);

        fixture.Surface.ViewBox = new(2000, 0, 100, 100);
        fixture.Tick();
        Assert.Single(fixture.Record());
        Assert.True(near.IsDisposed);
        Assert.Equal(1, fixture.Root.ImageResourceCache.ResidentCount);
        TestImage far = fixture.Loader.Loads.Last();
        Assert.Equal("far.png", far.Path);

        fixture.Surface.ViewBox = new(4000, 0, 100, 100);
        fixture.Tick();
        Assert.Empty(fixture.Record());
        Assert.True(far.IsDisposed);
        Assert.Equal(0, fixture.Root.ImageResourceCache.ResidentCount);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().RetainedObjects);
        Assert.Equal(2, fixture.Model.Chunks.Count);
    }

    [Fact]
    public async Task PreparedRegionOwnsOffCameraGridCollidersButNotTheirImages()
    {
        using Fixture fixture = new();
        fixture.Record();
        Assert.Throws<SceneCollisionRegionNotReadyException>(() => fixture.Move());
        Assert.Single(fixture.Map.LogicalChildren);

        using SceneCollisionRegion2D region = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(1980, 0, 60, 10));
        Assert.True(region.IsReady);
        Assert.Same(fixture.Map, fixture.Move().Collision!.Entity);
        Assert.Equal(2, fixture.Map.LogicalChildren.Count);
        Assert.Equal(1980, fixture.Actor.X);
        fixture.Record();
        Assert.Equal(["near.png"], fixture.Loader.Loads.Select(image => image.Path));

        region.Dispose();
        Assert.Single(fixture.Map.LogicalChildren);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() => fixture.Move());
    }

    [Fact]
    public void SimulatedMetadataKeepsTerrainCollisionWithoutKeepingOffCameraImages()
    {
        using Fixture fixture = new();
        fixture.Scene.Children.Remove(fixture.Actor);
        SceneSpatialSource2D<object> source = new(
            [new("npc", new(1980, 0, 10, 10), new DrawRect(1980, 0, 60, 10), isSimulated: true)],
            (_, _) => ValueTask.FromResult(new SceneSpatialLease2D<object>(new object())));
        SceneItems2D npcs = new() { ItemsSource = source };
        npcs.Templates.Add(new ContentTemplate<object>("npc", null, 0, _ => fixture.Actor));
        fixture.Scene.Children.Add(npcs);
        fixture.Tick();
        fixture.Record();
        Assert.Same(fixture.Map, fixture.Move().Collision!.Entity);
        Assert.Equal(["near.png"], fixture.Loader.Loads.Select(image => image.Path));
        Assert.Equal(2, fixture.Map.LogicalChildren.Count);

        source.SetEntries([]);
        fixture.Tick();
        Assert.Single(fixture.Map.LogicalChildren);
    }

    [Fact]
    public void VisibleChunkKeepsItsColliderEvenWhenDescriptorBoundsAreOutsideTheTileRectangle()
    {
        using Fixture fixture = new();
        fixture.Map.PublishModel(new("offset-collider", new DrawSize(10, 10),
            [new TileSet2D("near", new("NearAtlas"),
                [new TileDefinition2D(1, new(0, 0, 10, 10), collider:
                    new(TileColliderShape2D.Box, width: 10, height: 10, offsetX: 2000))])],
            [new TileChunk2D(default, 1, 1, [new TileCell2D(1)], version: 2)]));
        fixture.Tick();
        Assert.Single(fixture.Record());
        Assert.Single(fixture.Map.LogicalChildren);
        Assert.Same(fixture.Map, fixture.Move().Collision!.Entity);
    }

    [Fact]
    public async Task TransformedZeroWidthSegmentUsesCollisionBoundsRatherThanTileImageBounds()
    {
        using Fixture fixture = new();
        int loadsBefore = fixture.Loader.Loads.Count;
        fixture.Map.PublishModel(new("segment", new DrawSize(10, 10),
            [new TileSet2D("far", new("FarAtlas"),
                [new TileDefinition2D(2, new(0, 0, 10, 10), collider:
                    new(TileColliderShape2D.Segment, points: "0,0 0,10", offsetX: -500))])],
            [new TileChunk2D(new(200, 0), 1, 1, [new TileCell2D(2)])]));
        fixture.Map.ScaleX = 2;
        fixture.Map.Offset = new(1000, 0);
        fixture.Actor.X = 3980;
        Assert.Throws<SceneCollisionRegionNotReadyException>(() => fixture.Move());
        using SceneCollisionRegion2D region = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(3980, 5, 40, 0));
        Assert.True(region.IsReady);
        Assert.Same(fixture.Map, fixture.Move().Collision!.Entity);
        Assert.Empty(fixture.Record());
        Assert.Equal(loadsBefore, fixture.Loader.Loads.Count);
        Assert.DoesNotContain(fixture.Loader.Loads, image => image.Path == "far.png");
        region.Dispose();
        Assert.Empty(fixture.Map.LogicalChildren);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() => fixture.Move());
    }

    [Fact]
    public async Task RegionSharingCatalogRemovalAndDetachReleaseAdaptersWithoutResurrectingLeases()
    {
        using Fixture fixture = new();
        SceneCollisionRegion2D first = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(1980, 0, 60, 10));
        using SceneCollisionRegion2D second = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(1980, 0, 60, 10));
        Collider2D retained = fixture.Move().Collision!.Collider;
        first.Dispose();
        Assert.True(second.IsReady);
        Assert.Same(retained, fixture.Move().Collision!.Collider);
        fixture.Map.PublishModel(new("edited", fixture.Model.TileSize, fixture.Model.TileSets, [fixture.Model.Chunks[0]]));
        Assert.True(second.IsReady);
        Assert.Null(fixture.Move().Collision);
        Assert.Null(retained.LogicalParent);
        fixture.Map.PublishModel(fixture.Model);
        Assert.True(second.IsReady);
        Assert.NotSame(retained, fixture.Move().Collision!.Collider);
        fixture.Dispose();
        Assert.False(second.IsReady);
        Assert.Empty(fixture.Map.LogicalChildren);
        fixture.Root.VisualChildren.Add(fixture.Surface);
        fixture.Tick();
        Assert.False(second.IsReady);
        Assert.Single(fixture.Map.LogicalChildren);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() => fixture.Move());
    }

    [Fact]
    public async Task ModelPublicationInsideAnotherMaterializerDoesNotExposeRetiredTerrain()
    {
        using Fixture fixture = new();
        using SceneCollisionRegion2D region = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(1980, 0, 60, 10));
        Assert.NotNull(fixture.Move().Collision);
        CollisionHit2D? duringPublication = null;
        bool published = false;
        SceneItems2D publisher = new()
        {
            ItemsSource = new SceneSpatialSource2D<object>([new("publisher", new(0, 0, 10, 10), collisionBounds: null)],
                (_, _) => ValueTask.FromResult(new SceneSpatialLease2D<object>(new object())))
        };
        publisher.Templates.Add(new ContentTemplate<object>("publish", null, 0, _ =>
        {
            fixture.Map.PublishModel(new("edited", fixture.Model.TileSize, fixture.Model.TileSets, [fixture.Model.Chunks[0]]));
            duringPublication = fixture.Move().Collision;
            published = true;
            return new Scene2D();
        }));
        fixture.Scene.Children.Add(publisher);
        fixture.Tick();
        Assert.Null(publisher.PreparationError);
        Assert.True(published);
        Assert.Null(duringPublication);
        Assert.True(region.IsReady);
        Assert.Null(fixture.Move().Collision);
    }

    [Fact]
    public void ThirtyTwoCameraCyclesReleaseEveryRetiredImageAndReuseOnlyTheVisibleWorkingSet()
    {
        using Fixture fixture = new();
        DrawSprite2D[] reference = Assert.Single(fixture.Record()).Sprites.ToArray();
        int retainedObjects = fixture.Map.GetDiagnosticsSnapshot().RetainedObjects;
        for (int cycle = 0; cycle < 32; cycle++)
        {
            foreach (int x in new[] { 2000, 4000, 0 })
            {
                fixture.Surface.ViewBox = new(x, 0, 100, 100);
                fixture.Tick();
                DrawSpriteBatch[] batches = fixture.Record();
                Assert.Equal(x == 4000 ? 0 : 1, fixture.Root.ImageResourceCache!.ResidentCount);
                Assert.Equal(x == 4000 ? 0 : 1, fixture.Map.LogicalChildren.Count);
                Assert.Equal(x == 4000 ? 0 : retainedObjects, fixture.Map.GetDiagnosticsSnapshot().RetainedObjects);
                Assert.All(fixture.Loader.Loads.Where(image => image != fixture.Loader.Loads.Last() || x == 4000),
                    image => Assert.Equal(1, image.DisposeCount));
                if (x == 0) { Assert.Equal(reference, Assert.Single(batches).Sprites); }
            }
        }
        Assert.Equal(65, fixture.Loader.Loads.Count);
        fixture.Dispose();
        Assert.All(fixture.Loader.Loads, image => Assert.Equal(1, image.DisposeCount));
    }

    private sealed class Fixture : IDisposable
    {
        internal Fixture()
        {
            TileColliderDescriptor2D collider = new(TileColliderShape2D.Box, width: 10, height: 10);
            Model = new("terrain", new DrawSize(10, 10),
                [new TileSet2D("near", new("NearAtlas"), [new TileDefinition2D(1, new(0, 0, 10, 10), collider: collider)]),
                 new TileSet2D("far", new("FarAtlas"), [new TileDefinition2D(2, new(0, 0, 10, 10), collider: collider)])],
                [new TileChunk2D(new(0, 0), 1, 1, [new TileCell2D(1)]),
                 new TileChunk2D(new(200, 0), 1, 1, [new TileCell2D(2)])]);
            Map.PublishModel(Model);
            Scene.Children.Add(Map);
            Scene.Children.Add(Actor);
            Surface.Scene = Scene;
            Surface.Resources.SetResource(new ResourceId<ImageResource>("NearAtlas"), new ImageResource("near.png"));
            Surface.Resources.SetResource(new ResourceId<ImageResource>("FarAtlas"), new ImageResource("far.png"));
            Root.SetImageLoader(Loader);
            Root.VisualChildren.Add(Surface);
            Tick();
        }

        internal UIRoot Root { get; } = new(100, 100);
        internal Scene2D Scene { get; } = new();
        internal RenderSurface2D Surface { get; } = new() { ViewBox = new(0, 0, 100, 100) };
        internal TileMap2D Map { get; } = new();
        internal TileMap2DModel Model { get; }
        internal Sprite2D Actor { get; } = new() { X = 1980, Collider = new BoxCollider2D { Width = 10, Height = 10 } };
        internal TestLoader Loader { get; } = new();

        internal MoveCollisionResult2D Move() => Scene.CollisionWorld.MoveAndCollide(Actor.Collider!, new(40, 0));
        internal DrawSpriteBatch[] Record()
        {
            Tick();
            DrawCommandList commands = new();
            ((IRenderSurface2DFrameSource)Surface).RecordFrame(commands, new(0, 0, 100, 100));
            return commands.Where(command => command.Kind == DrawCommandKind.DrawSpriteBatch)
                .Select(command => command.SpriteBatch!).ToArray();
        }
        internal void Tick()
        {
            Root.ProcessFrame();
            ((ITimeSensitiveRenderElement)Surface).UpdateRenderTime(TimeSpan.FromMilliseconds(16));
        }
        public void Dispose() => Root.VisualChildren.Remove(Surface);
    }

    private sealed class TestLoader : IAsyncImageLoader
    {
        internal List<TestImage> Loads { get; } = [];
        internal int Size { get; set; } = 10;
        public ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Load(path));
        public IDrawImage Load(string path)
        {
            TestImage image = new(path, Size);
            Loads.Add(image);
            return image;
        }
    }

    private sealed class TestImage(string path, int size = 10) : IDrawImage, IDisposable
    {
        internal string Path { get; } = path;
        internal int WidthReads { get; set; }
        public int Width { get { WidthReads++; return size; } }
        public int Height => size;
        internal bool IsDisposed { get; private set; }
        internal int DisposeCount { get; private set; }
        internal bool ThrowOnDispose { get; set; }
        public void Dispose()
        {
            IsDisposed = true;
            DisposeCount++;
            if (ThrowOnDispose) { throw new InvalidOperationException("image release failure"); }
        }
    }
}
