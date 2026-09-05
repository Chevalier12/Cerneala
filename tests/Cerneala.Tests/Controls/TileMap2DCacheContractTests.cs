using Cerneala.Drawing;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Resources;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class TileMap2DCacheContractTests
{
    [Fact]
    [Trait("TileMapStage", "2")]
    public void WarmFramesAndCameraOrNodeTransformsReuseEveryChunkSegment()
    {
        CacheFixture fixture = CacheFixture.Create();

        DrawCommandList coldCommands = Record(fixture.Surface);
        TileMap2DDiagnosticsSnapshot cold = fixture.Map.GetDiagnosticsSnapshot();
        DrawSpriteBatch[] coldBatches = Batches(coldCommands);
        Assert.Equal(2, cold.BatchesRebuilt);
        Assert.Equal(0, cold.BatchesReused);
        Assert.True(cold.RetainedBytes > 0);

        DrawCommandList warmCommands = Record(fixture.Surface);
        TileMap2DDiagnosticsSnapshot warm = fixture.Map.GetDiagnosticsSnapshot();
        Assert.Equal(0, warm.BatchesRebuilt);
        Assert.Equal(2, warm.BatchesReused);
        Assert.Equal(cold.RetainedBytes, warm.RetainedBytes);
        Assert.Equal(coldBatches, Batches(warmCommands), ReferenceEqualityComparer.Instance);

        fixture.Map.TranslateX = 7;
        fixture.Surface.ViewBox = new DrawRect(8, 0, 64, 32);
        DrawCommandList transformedCommands = Record(fixture.Surface);
        TileMap2DDiagnosticsSnapshot transformed = fixture.Map.GetDiagnosticsSnapshot();
        Assert.Equal(0, transformed.BatchesRebuilt);
        Assert.Equal(2, transformed.BatchesReused);
        Assert.Equal(coldBatches, Batches(transformedCommands), ReferenceEqualityComparer.Instance);
    }

    [Fact]
    [Trait("TileMapStage", "2")]
    public void ChunkAndTilesetVersionsInvalidateOnlyTheirDependentSegments()
    {
        CacheFixture fixture = CacheFixture.Create();
        Record(fixture.Surface);
        DrawSpriteBatch[] warmBatches = Batches(Record(fixture.Surface));

        fixture.Map.Model = CreateModel(
            terrainChunkVersion: 2,
            structureChunkVersion: 1,
            terrainSetVersion: 1,
            terrainFirstFlip: TileFlip2D.Horizontal);
        DrawCommandList chunkMutationCommands = Record(fixture.Surface);
        TileMap2DDiagnosticsSnapshot chunkMutation = fixture.Map.GetDiagnosticsSnapshot();
        Assert.Equal(1, chunkMutation.BatchesRebuilt);
        Assert.Equal(1, chunkMutation.BatchesReused);
        Assert.Equal(1, chunkMutation.TileInvalidations);

        DrawSpriteBatch[] afterChunkMutation = Batches(chunkMutationCommands);
        Assert.NotSame(warmBatches[0], afterChunkMutation[0]);
        Assert.Same(warmBatches[1], afterChunkMutation[1]);
        fixture.Map.Model = CreateModel(
            terrainChunkVersion: 2,
            structureChunkVersion: 1,
            terrainSetVersion: 2,
            terrainFirstFlip: TileFlip2D.Horizontal,
            terrainSourceX: 16);
        DrawCommandList tilesetMutationCommands = Record(fixture.Surface);
        TileMap2DDiagnosticsSnapshot tilesetMutation = fixture.Map.GetDiagnosticsSnapshot();
        DrawSpriteBatch[] afterTilesetMutation = Batches(tilesetMutationCommands);
        Assert.Equal(1, tilesetMutation.BatchesRebuilt);
        Assert.Equal(1, tilesetMutation.BatchesReused);
        Assert.NotSame(afterChunkMutation[0], afterTilesetMutation[0]);
        Assert.Same(afterChunkMutation[1], afterTilesetMutation[1]);
    }

    [Fact]
    [Trait("TileMapStage", "2")]
    public void PromotionAndDemotionRebuildOnlyTheOwningChunkAndReleaseSparseLifecycle()
    {
        CacheFixture fixture = CacheFixture.Create();
        Record(fixture.Surface);
        Record(fixture.Surface);

        TileCellKey2D key = new("Ground", 0, 0);
        TileInstance2D promoted = fixture.Map.Promote(key);
        using IDisposable prism = AttachPrism(promoted, "PromotedTile");
        using IDisposable motionSession = GeneratedMarkup.AttachMotionSession(promoted);
        int triggerAttachCount = 0;
        int triggerDetachCount = 0;
        GeneratedMarkup.AddMotionTrigger(
            motionSession,
            () => triggerAttachCount++,
            () => triggerDetachCount++);
        MotionHandle motion = GeneratedMarkup.StartMotionProperty(
            motionSession,
            promoted,
            UIElement.ScaleProperty,
            hasFrom: false,
            from: default,
            toCurrent: false,
            to: 1.25f,
            MotionFactory.Tween<float>(TimeSpan.FromSeconds(10)),
            new MotionPropertyStartOptions());
        DrawCommandList promotedCommands = Record(fixture.Surface);
        TileMap2DDiagnosticsSnapshot promotedSnapshot = fixture.Map.GetDiagnosticsSnapshot();
        Assert.Equal(1, promotedSnapshot.BatchesRebuilt);
        Assert.Equal(1, promotedSnapshot.BatchesReused);
        Assert.Equal(0, promotedSnapshot.BatchSplits);
        Assert.Single(promotedCommands, static command => command.Kind == DrawCommandKind.DrawImage);
        Assert.Same(fixture.Root, promoted.Root);
        Assert.True(motion.IsActive);
        Assert.True(PrismAttachment.TryGetInstance(promoted, out _));
        Assert.Equal(1, triggerAttachCount);

        Assert.True(fixture.Map.Demote(key));
        DrawCommandList demotedCommands = Record(fixture.Surface);
        TileMap2DDiagnosticsSnapshot demotedSnapshot = fixture.Map.GetDiagnosticsSnapshot();
        Assert.Equal(1, demotedSnapshot.BatchesRebuilt);
        Assert.Equal(1, demotedSnapshot.BatchesReused);
        Assert.DoesNotContain(demotedCommands, static command => command.Kind == DrawCommandKind.DrawImage);
        Assert.Null(promoted.Root);
        Assert.Null(promoted.OwnerLayer);
        Assert.False(fixture.Map.TryGetPromoted(key, out _));
        Assert.True(motion.IsCanceled);
        Assert.False(PrismAttachment.TryGetInstance(promoted, out _));
        Assert.Equal(1, triggerDetachCount);
        Assert.Equal(0, fixture.Root.Motion.Properties.BindingCount);
        fixture.Root.ProcessFrame();
        Assert.False(fixture.Root.Motion.HasActiveMotion);
    }

    [Fact]
    [Trait("TileMapStage", "2")]
    public void DetachRootChangeAndBackendStateLossDropOwnedCachesWithoutDisposingSharedAtlases()
    {
        CacheFixture fixture = CacheFixture.Create();
        Record(fixture.Surface);
        Assert.True(fixture.Map.GetDiagnosticsSnapshot().RetainedBytes > 0);

        object backendOwner = new();
        TestBackendState backendState = new();
        ((IRenderSurface2DFrameSource)fixture.Surface).SetBackendState(backendOwner, backendState);
        ((IRenderSurface2DFrameSource)fixture.Surface).SetBackendState(backendOwner, null);
        Assert.True(backendState.IsDisposed);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().RetainedBytes);
        Assert.False(fixture.Terrain.IsDisposed);
        Assert.False(fixture.Structures.IsDisposed);

        Record(fixture.Surface);
        Assert.Equal(2, fixture.Map.GetDiagnosticsSnapshot().BatchesRebuilt);
        fixture.Root.VisualChildren.Remove(fixture.Surface);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().RetainedBytes);
        Assert.False(fixture.Terrain.IsDisposed);
        Assert.False(fixture.Structures.IsDisposed);

        UIRoot replacementRoot = new();
        replacementRoot.SetImageLoader(fixture.Loader);
        replacementRoot.VisualChildren.Add(fixture.Surface);
        Record(fixture.Surface);
        Assert.Equal(2, fixture.Map.GetDiagnosticsSnapshot().BatchesRebuilt);
    }

    [Fact]
    [Trait("TileMapStage", "2")]
    public void PartialNegativeBoundaryAndEmptyChunksHaveStableCacheKeys()
    {
        TileMap2DModel sparse = new(
            new DrawSize(16, 16),
            [TerrainSet()],
            [
                new TileLayer2DModel(
                    "Sparse",
                    [
                        new TileChunk2D(new TileCoordinate2D(-2, -1), 2, 1, [new TileCell2D(1), new TileCell2D(0)]),
                        new TileChunk2D(new TileCoordinate2D(4, 3), 1, 2, [new TileCell2D(0), new TileCell2D(1)])
                    ]),
                new TileLayer2DModel("Empty", [])
            ]);
        CacheFixture fixture = CacheFixture.Create(sparse);

        DrawCommandList cold = Record(fixture.Surface);
        Assert.Single(Batches(cold));
        Assert.Equal(1, fixture.Map.GetDiagnosticsSnapshot().BatchesRebuilt);
        DrawCommandList warm = Record(fixture.Surface);
        Assert.Equal(1, fixture.Map.GetDiagnosticsSnapshot().BatchesReused);
        Assert.Equal(Batches(cold), Batches(warm), ReferenceEqualityComparer.Instance);
    }

    [Fact]
    [Trait("TileMapStage", "2")]
    public void WarmFrameAllocationDoesNotScaleWithTilesInsideOneCachedChunk()
    {
        CacheFixture small = CacheFixture.Create(SingleChunkModel(2, 2));
        CacheFixture large = CacheFixture.Create(SingleChunkModel(64, 64));
        for (int iteration = 0; iteration < 4; iteration++)
        {
            Record(small.Surface);
            Record(large.Surface);
        }

        long smallAllocated = MeasureWarmAllocation(small.Surface);
        long largeAllocated = MeasureWarmAllocation(large.Surface);

        Assert.True(
            largeAllocated <= smallAllocated + 8_192,
            $"Warm allocation scaled with cached tiles: small={smallAllocated:N0} B, large={largeAllocated:N0} B.");
        Assert.Equal(0, large.Map.GetDiagnosticsSnapshot().BatchesRebuilt);
        Assert.Equal(1, large.Map.GetDiagnosticsSnapshot().BatchesReused);
        Assert.True(large.Map.GetDiagnosticsSnapshot().RetainedObjects > small.Map.GetDiagnosticsSnapshot().RetainedObjects);
    }

    [Fact]
    [Trait("TileMapStage", "2")]
    public void PromotedNodeCountIsLinearInPromotionsRatherThanStaticTiles()
    {
        CacheFixture fixture = CacheFixture.Create(SingleChunkModel(64, 64));
        TileCellKey2D[] keys = Enumerable.Range(0, 32)
            .Select(static x => new TileCellKey2D("Ground", x, 0))
            .ToArray();

        foreach (TileCellKey2D key in keys)
        {
            fixture.Map.Promote(key);
        }

        TileLayer2D layer = Assert.Single(fixture.Map.Layers);
        Assert.Equal(4_096, fixture.Map.Model!.Layers[0].Chunks[0].Tiles.Count);
        Assert.Equal(keys.Length, layer.PromotedTiles.Count);
        Assert.Equal(keys.Length, layer.LogicalChildren.Count);
        Assert.Single(fixture.Map.LogicalChildren);

        Assert.All(keys, key => Assert.True(fixture.Map.Demote(key)));
        Assert.Empty(layer.PromotedTiles);
        Assert.Empty(layer.LogicalChildren);
    }

    [Fact]
    [Trait("TileMapStage", "3")]
    public void LargeSparseMapQueriesOnlyChunksNearTheViewportBeforeEnumeratingTiles()
    {
        CacheFixture fixture = CacheFixture.Create(LargeSparseModel());
        fixture.Surface.ViewBox = new DrawRect(520, 520, 64, 64);

        DrawCommandList commands = Record(
            fixture.Surface,
            new DrawRect(0, 0, 64, 64));
        TileMap2DDiagnosticsSnapshot snapshot = fixture.Map.GetDiagnosticsSnapshot();

        Assert.Equal(258, snapshot.TotalChunks);
        Assert.True(
            snapshot.CandidateChunks <= 4,
            $"Spatial query returned {snapshot.CandidateChunks} candidates for one visible chunk.");
        Assert.Equal(1, snapshot.VisibleChunks);
        Assert.Equal(64, snapshot.CandidateTiles);
        Assert.Equal(64, snapshot.DrawnTiles);
        Assert.Single(Batches(commands));
    }

    [Fact]
    [Trait("TileMapStage", "3")]
    public void ViewBoxPanAndZoomKeepEveryPixelAndChunkBoundaryTileWithoutOverdrawingFarChunks()
    {
        CacheFixture fixture = CacheFixture.Create(BoundaryStripModel());

        fixture.Surface.ViewBox = new DrawRect(0, 0, 32, 16);
        Assert.Equal(
            [0f, 16f],
            StaticDestinations(Record(fixture.Surface, new DrawRect(0, 0, 32, 16)))
                .Select(static destination => destination.X));

        fixture.Surface.ViewBox = new DrawRect(16, 0, 32, 16);
        Assert.Equal(
            [16f, 32f],
            StaticDestinations(Record(fixture.Surface, new DrawRect(0, 0, 32, 16)))
                .Select(static destination => destination.X));

        fixture.Surface.ViewBox = new DrawRect(16, 0, 16, 16);
        Assert.Equal(
            [16f],
            StaticDestinations(Record(fixture.Surface, new DrawRect(0, 0, 16, 16)))
                .Select(static destination => destination.X));

        fixture.Surface.ViewBox = new DrawRect(15.5f, 0, 32, 16);
        Assert.Equal(
            [0f, 16f, 32f],
            StaticDestinations(Record(fixture.Surface, new DrawRect(0, 0, 32, 16)))
                .Select(static destination => destination.X));
    }

    [Fact]
    [Trait("TileMapStage", "3")]
    public void RotationScaleEmptyViewportAndNonInvertibleTransformCullConservatively()
    {
        CacheFixture fixture = CacheFixture.Create(BoundaryStripModel());
        fixture.Surface.ViewBox = new DrawRect(-16, -16, 64, 64);
        fixture.Map.Rotation = MathF.PI / 4;
        fixture.Map.Scale = 0.75f;

        DrawCommandList transformed = Record(
            fixture.Surface,
            new DrawRect(0, 0, 64, 64));
        Assert.Contains(
            StaticDestinations(transformed),
            static destination => destination.X == 0);

        DrawCommandList empty = Record(fixture.Surface, default);
        Assert.Empty(Batches(empty));
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().VisibleChunks);

        fixture.Map.ScaleX = 0;
        DrawCommandList nonInvertible = Record(
            fixture.Surface,
            new DrawRect(0, 0, 64, 64));
        Assert.Equal(5, fixture.Map.GetDiagnosticsSnapshot().VisibleChunks);
        Assert.Equal(5, StaticDestinations(nonInvertible).Count);
    }

    [Fact]
    [Trait("TileMapStage", "3")]
    public void PromotedMotionPrismTileIsCulledIndividuallyWithoutDoubleDrawOrStateLoss()
    {
        CacheFixture fixture = CacheFixture.Create(SingleChunkModel(4, 1));
        TileInstance2D promoted = fixture.Map.Promote(new TileCellKey2D("Ground", 3, 0));
        using IDisposable prism = AttachPrism(promoted, "CulledPromotedTile");
        using IDisposable motionSession = GeneratedMarkup.AttachMotionSession(promoted);
        MotionHandle motion = GeneratedMarkup.StartMotionProperty(
            motionSession,
            promoted,
            UIElement.ScaleProperty,
            hasFrom: false,
            from: default,
            toCurrent: false,
            to: 1.25f,
            MotionFactory.Tween<float>(TimeSpan.FromSeconds(10)),
            new MotionPropertyStartOptions());

        fixture.Surface.ViewBox = new DrawRect(0, 0, 16, 16);
        DrawCommandList outside = Record(fixture.Surface, new DrawRect(0, 0, 16, 16));
        Assert.DoesNotContain(outside, static command => command.Kind == DrawCommandKind.DrawImage);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().PromotedInstancesVisible);
        Assert.Equal(1, fixture.Map.GetDiagnosticsSnapshot().PromotedInstancesCulled);

        fixture.Surface.ViewBox = new DrawRect(48, 0, 16, 16);
        DrawCommandList inside = Record(fixture.Surface, new DrawRect(0, 0, 16, 16));
        Assert.Single(inside, static command => command.Kind == DrawCommandKind.DrawImage);
        DrawCommand prismBegin = Assert.Single(
            inside,
            static command => command.Kind == DrawCommandKind.BeginPrism);
        Assert.Equal(new DrawRect(0, 0, 16, 16), prismBegin.PrismScope!.Value.ControlBounds);
        Assert.Equal(1, fixture.Map.GetDiagnosticsSnapshot().PromotedInstancesVisible);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().PromotedInstancesCulled);

        fixture.Surface.ViewBox = new DrawRect(64, 0, 16, 16);
        DrawCommandList edge = Record(fixture.Surface, new DrawRect(0, 0, 16, 16));
        Assert.Single(edge, static command => command.Kind == DrawCommandKind.DrawImage);

        fixture.Surface.ViewBox = new DrawRect(80, 0, 16, 16);
        DrawCommandList outsideAgain = Record(fixture.Surface, new DrawRect(0, 0, 16, 16));
        Assert.DoesNotContain(outsideAgain, static command => command.Kind == DrawCommandKind.DrawImage);
        Assert.True(motion.IsActive);
        Assert.True(PrismAttachment.TryGetInstance(promoted, out _));
        Assert.Equal(1, fixture.Map.GetDiagnosticsSnapshot().PromotedInstancesCulled);
        Assert.DoesNotContain(
            StaticDestinations(outsideAgain),
            static destination => destination.X == 48);
    }

    private static TileMap2DModel CreateModel(
        long terrainChunkVersion = 1,
        long structureChunkVersion = 1,
        long terrainSetVersion = 1,
        TileFlip2D terrainFirstFlip = TileFlip2D.None,
        float terrainSourceX = 0) =>
        new(
            new DrawSize(16, 16),
            [TerrainSet(terrainSetVersion, terrainSourceX), StructureSet()],
            [
                new TileLayer2DModel(
                    "Ground",
                    [
                        new TileChunk2D(
                            new TileCoordinate2D(0, 0),
                            2,
                            1,
                            [new TileCell2D(1, terrainFirstFlip), new TileCell2D(1)],
                            terrainChunkVersion),
                        new TileChunk2D(
                            new TileCoordinate2D(2, 0),
                            2,
                            1,
                            [new TileCell2D(100), new TileCell2D(100)],
                            structureChunkVersion)
                    ],
                    version: Math.Max(terrainChunkVersion, structureChunkVersion))
            ],
            new TileMapBounds2D(0, 0, 4, 1),
            version: Math.Max(Math.Max(terrainChunkVersion, structureChunkVersion), terrainSetVersion));

    private static TileMap2DModel SingleChunkModel(int width, int height) =>
        new(
            new DrawSize(16, 16),
            [TerrainSet()],
            [new TileLayer2DModel(
                "Ground",
                [new TileChunk2D(
                    new TileCoordinate2D(0, 0),
                    width,
                    height,
                    Enumerable.Repeat(new TileCell2D(1), checked(width * height)))])],
            new TileMapBounds2D(0, 0, width, height));

    private static TileMap2DModel LargeSparseModel()
    {
        List<TileChunk2D> chunks = [];
        for (int chunkY = 0; chunkY < 16; chunkY++)
        {
            for (int chunkX = 0; chunkX < 16; chunkX++)
            {
                chunks.Add(FilledChunk(chunkX * 8, chunkY * 8, 8, 8));
            }
        }
        chunks.Add(FilledChunk(-1_000_000, -1_000_000, 8, 8));
        chunks.Add(FilledChunk(1_000_000, 1_000_000, 8, 8));
        return new TileMap2DModel(
            new DrawSize(16, 16),
            [TerrainSet()],
            [new TileLayer2DModel("Ground", chunks)]);
    }

    private static TileMap2DModel BoundaryStripModel() =>
        new(
            new DrawSize(16, 16),
            [TerrainSet()],
            [new TileLayer2DModel(
                "Ground",
                Enumerable.Range(-1, 5)
                    .Select(static x => FilledChunk(x, 0, 1, 1)))],
            new TileMapBounds2D(-1, 0, 5, 1));

    private static TileChunk2D FilledChunk(int x, int y, int width, int height) =>
        new(
            new TileCoordinate2D(x, y),
            width,
            height,
            Enumerable.Repeat(new TileCell2D(1), checked(width * height)));

    private static TileSet2D TerrainSet(long version = 1, float sourceX = 0) =>
        new(
            "Terrain",
            new ResourceId<ImageResource>("VillageTerrain"),
            [new TileDefinition2D(1, new DrawRect(sourceX, 0, 16, 16))],
            version);

    private static TileSet2D StructureSet() =>
        new(
            "Structures",
            new ResourceId<ImageResource>("VillageStructures"),
            [new TileDefinition2D(100, new DrawRect(16, 0, 16, 16))]);

    private static DrawCommandList Record(RenderSurface2D surface) =>
        Record(surface, new DrawRect(0, 0, 128, 64));

    private static DrawCommandList Record(RenderSurface2D surface, DrawRect bounds)
    {
        DrawCommandList commands = new();
        ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, bounds);
        return commands;
    }

    private static DrawSpriteBatch[] Batches(DrawCommandList commands) =>
        commands
            .Where(static command => command.Kind == DrawCommandKind.DrawSpriteBatch)
            .Select(static command => command.SpriteBatch!)
            .ToArray();

    private static IReadOnlyList<DrawRect> StaticDestinations(DrawCommandList commands) =>
        Batches(commands)
            .SelectMany(static batch => batch.Sprites)
            .Select(static sprite => sprite.Destination)
            .OrderBy(static destination => destination.X)
            .ThenBy(static destination => destination.Y)
            .ToArray();

    private static IDisposable AttachPrism(UIElement element, string name) =>
        GeneratedMarkup.AttachPrism(
            element,
            () => new PrismInstance(
                PrismTestData.Composition(name, PrismTestData.Layer(1, "Content"))));

    private static long MeasureWarmAllocation(RenderSurface2D surface)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        Record(surface);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private sealed record CacheFixture(
        UIRoot Root,
        RenderSurface2D Surface,
        TileMap2D Map,
        TestImage Terrain,
        TestImage Structures,
        TestImageLoader Loader)
    {
        internal static CacheFixture Create(TileMap2DModel? model = null)
        {
            TestImage terrain = new("terrain");
            TestImage structures = new("structures");
            TestImageLoader loader = new(new Dictionary<string, IDrawImage>(StringComparer.Ordinal)
            {
                ["terrain.png"] = terrain,
                ["structures.png"] = structures
            });
            TileMap2D map = new() { Model = model ?? CreateModel() };
            Scene2D scene = new();
            scene.Children.Add(map);
            RenderSurface2D surface = new() { Scene = scene };
            surface.Resources.SetResource(
                new ResourceId<ImageResource>("VillageTerrain"),
                new ImageResource("terrain.png"));
            surface.Resources.SetResource(
                new ResourceId<ImageResource>("VillageStructures"),
                new ImageResource("structures.png"));
            UIRoot root = new();
            root.SetImageLoader(loader);
            root.VisualChildren.Add(surface);
            return new CacheFixture(root, surface, map, terrain, structures, loader);
        }
    }

    private sealed class TestImage(string name) : IDrawImage, IDisposable
    {
        public string Name { get; } = name;

        public int Width => 64;

        public int Height => 64;

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class TestImageLoader(IReadOnlyDictionary<string, IDrawImage> images) : IImageLoader
    {
        public IDrawImage Load(string path) => images[path];
    }

    private sealed class TestBackendState : IRenderSurface2DBackendState
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
