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
    public void PeerSpriteDetachReleasesItsLifecycleWithoutRebuildingStaticBatches()
    {

        CacheFixture fixture = CacheFixture.Create(CreateModel(terrainFirstTile: 0));
        Record(fixture.Surface);
        Record(fixture.Surface);
        Sprite2D sprite = new()
        {
            Image = new(new ResourceId<ImageResource>("VillageTerrain")), Width = 16, Height = 16
        };
        fixture.Surface.Scene!.Children.Add(sprite);
        using IDisposable prism = AttachPrism(sprite, "Sprite");
        using IDisposable motionSession = GeneratedMarkup.AttachMotionSession(sprite);
        int triggerAttachCount = 0, triggerDetachCount = 0;
        GeneratedMarkup.AddMotionTrigger(motionSession, () => triggerAttachCount++, () => triggerDetachCount++);
        MotionHandle motion = GeneratedMarkup.StartMotionProperty(
            motionSession, sprite, UIElement.OpacityProperty, hasFrom: false, from: default,
            toCurrent: false, to: 0.5f, MotionFactory.Tween<float>(TimeSpan.FromSeconds(10)), new MotionPropertyStartOptions());
        DrawCommandList attached = Record(fixture.Surface);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().BatchesRebuilt);
        Assert.Equal(2, fixture.Map.GetDiagnosticsSnapshot().BatchesReused);
        Assert.Single(attached, static command => command.Kind == DrawCommandKind.DrawImage);
        Assert.Same(fixture.Root, sprite.Root);
        Assert.True(motion.IsActive);
        Assert.True(PrismAttachment.TryGetInstance(sprite, out _));
        Assert.Equal(1, triggerAttachCount);

        fixture.Surface.Scene.Children.Remove(sprite);
        DrawCommandList detached = Record(fixture.Surface);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().BatchesRebuilt);
        Assert.Equal(2, fixture.Map.GetDiagnosticsSnapshot().BatchesReused);
        Assert.DoesNotContain(detached, static command => command.Kind == DrawCommandKind.DrawImage);
        Assert.Null(sprite.Root);
        Assert.Null(sprite.LogicalParent);
        Assert.True(motion.IsCanceled);
        Assert.False(PrismAttachment.TryGetInstance(sprite, out _));
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
    public void RepeatedTopologyReplacementKeepsCacheBoundedAndReleasesRetiredBatches()
    {
        CacheFixture fixture = CacheFixture.Create();
        WeakReference[] retired = ReplaceAndClearCachedModels(fixture);

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

        Assert.All(retired, reference => Assert.False(reference.IsAlive));
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().RetainedObjects);
        Assert.False(fixture.Terrain.IsDisposed);
        GC.KeepAlive(fixture);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference[] ReplaceAndClearCachedModels(CacheFixture fixture)
    {
        List<WeakReference> retired = [];
        long? retainedAtEightChunks = null;
        // Grow, shrink, then replace a fixed-size working set at fresh coordinates.
        for (int iteration = 0; iteration < 64; iteration++)
        {
            int count = iteration < 16 ? iteration + 1 : iteration < 32 ? 32 - iteration : 8;
            int origin = iteration * 32;
            TileMap2DModel model = new("Lifetime", new DrawSize(16, 16), [TerrainSet()],
                Enumerable.Range(0, count).Select(x => FilledChunk(origin + x, 0, 1, 1)));
            fixture.Map.Model = model;
            fixture.Surface.ViewBox = new DrawRect(origin * 16, 0, count * 16, 16);
            DrawSpriteBatch[] batches = Batches(Record(fixture.Surface));
            Assert.Equal(count, batches.Length);
            Assert.Equal(count, fixture.Map.GetDiagnosticsSnapshot().BatchesRebuilt);
            if (iteration >= 32)
            {
                retainedAtEightChunks ??= fixture.Map.GetDiagnosticsSnapshot().RetainedBytes;
                Assert.Equal(retainedAtEightChunks.Value, fixture.Map.GetDiagnosticsSnapshot().RetainedBytes);
            }
            retired.Add(new WeakReference(model));
            retired.AddRange(batches.Select(static batch => new WeakReference(batch)));
        }
        fixture.Map.Model = null;
        fixture.Surface.Scene!.Children.Remove(fixture.Map);
        Record(fixture.Surface);
        return retired.ToArray();
    }

    [Fact]
    [Trait("TileMapStage", "2")]
    public void PartialNegativeBoundaryAndEmptyMapsHaveStableCacheKeys()
    {

        TileMap2DModel sparse = new("Sparse", new DrawSize(16, 16), [TerrainSet()],
            [new TileChunk2D(new(-2, -1), 2, 1, [new TileCell2D(1), default]),
             new TileChunk2D(new(4, 3), 1, 2, [default, new TileCell2D(1)])]);
        CacheFixture fixture = CacheFixture.Create(sparse);
        fixture.Surface.Scene!.Children.Add(new TileMap2D { Model = new TileMap2DModel("Empty", new DrawSize(16, 16), [], []) });
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
    public void LiveNodeCountIsLinearInExplicitSpritesRatherThanStaticTiles()
    {

        CacheFixture fixture = CacheFixture.Create(SingleChunkModel(64, 64));
        Sprite2D[] sprites = Enumerable.Range(0, 32).Select(static x => new Sprite2D { X = x * 16 }).ToArray();
        Scene2D scene = fixture.Surface.Scene!;
        foreach (Sprite2D sprite in sprites) { scene.Children.Add(sprite); }
        Assert.Equal(4_096, fixture.Map.Model!.Chunks[0].Tiles.Count);
        Assert.Equal(sprites.Length + 1, scene.Children.Count);
        Assert.Empty(fixture.Map.LogicalChildren);
        Assert.All(sprites, sprite => Assert.Same(scene, sprite.LogicalParent));
        foreach (Sprite2D sprite in sprites) { scene.Children.Remove(sprite); }
        Assert.Same(fixture.Map, Assert.Single(scene.Children));
        Assert.Empty(fixture.Map.LogicalChildren);
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
    public void PeerSpriteCullsIndependentlyWhileStaticCellsStayBatched()
    {

        CacheFixture fixture = CacheFixture.Create(SingleChunkModel(4, 1, emptyCell: 3));
        Sprite2D sprite = new()
        {
            Image = new(new ResourceId<ImageResource>("VillageTerrain")), X = 48, Width = 16, Height = 16
        };
        fixture.Surface.Scene!.Children.Add(sprite);
        using IDisposable motionSession = GeneratedMarkup.AttachMotionSession(sprite);
        MotionHandle motion = GeneratedMarkup.StartMotionProperty(
            motionSession, sprite, UIElement.OpacityProperty, hasFrom: false, from: default,
            toCurrent: false, to: 0.5f, MotionFactory.Tween<float>(TimeSpan.FromSeconds(10)), new MotionPropertyStartOptions());
        fixture.Surface.ViewBox = new DrawRect(0, 0, 16, 16);
        DrawCommandList outside = Record(fixture.Surface, new DrawRect(0, 0, 16, 16));
        Assert.DoesNotContain(outside, static command => command.Kind == DrawCommandKind.DrawImage);
        Assert.DoesNotContain(StaticDestinations(outside), static destination => destination.X == 48);
        fixture.Surface.ViewBox = new DrawRect(48, 0, 16, 16);
        DrawCommandList inside = Record(fixture.Surface, new DrawRect(0, 0, 16, 16));
        Assert.Single(inside, static command => command.Kind == DrawCommandKind.DrawImage);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().BatchesRebuilt);
        Assert.Equal(1, fixture.Map.GetDiagnosticsSnapshot().BatchesReused);
        fixture.Surface.ViewBox = new DrawRect(80, 0, 16, 16);
        DrawCommandList outsideAgain = Record(fixture.Surface, new DrawRect(0, 0, 16, 16));
        Assert.DoesNotContain(outsideAgain, static command => command.Kind == DrawCommandKind.DrawImage);
        Assert.True(motion.IsActive);
        Assert.Same(fixture.Root, sprite.Root);
        Assert.DoesNotContain(StaticDestinations(outsideAgain), static destination => destination.X == 48);
    }

    private static TileMap2DModel CreateModel(
        long terrainChunkVersion = 1,
        long structureChunkVersion = 1,
        long terrainSetVersion = 1,
        TileFlip2D terrainFirstFlip = TileFlip2D.None,
        float terrainSourceX = 0,
        int terrainFirstTile = 1) =>
        new("Ground", new DrawSize(16, 16), [TerrainSet(terrainSetVersion, terrainSourceX), StructureSet()],
            [new TileChunk2D(default, 2, 1, [new TileCell2D(terrainFirstTile, terrainFirstFlip), new TileCell2D(1)], terrainChunkVersion),
             new TileChunk2D(new(2, 0), 2, 1, [new TileCell2D(100), new TileCell2D(100)], structureChunkVersion)],
            new TileMapBounds2D(0, 0, 4, 1),
            version: Math.Max(Math.Max(terrainChunkVersion, structureChunkVersion), terrainSetVersion));

    private static TileMap2DModel SingleChunkModel(int width, int height, int? emptyCell = null) =>
        new("Ground", new DrawSize(16, 16), [TerrainSet()],
            [new TileChunk2D(default, width, height,
                Enumerable.Range(0, checked(width * height)).Select(index => index == emptyCell ? default : new TileCell2D(1)))],
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
        return new TileMap2DModel("Ground", new DrawSize(16, 16), [TerrainSet()], chunks);
    }

    private static TileMap2DModel BoundaryStripModel() =>
        new("Ground", new DrawSize(16, 16), [TerrainSet()],
            Enumerable.Range(-1, 5).Select(static x => FilledChunk(x, 0, 1, 1)),
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
