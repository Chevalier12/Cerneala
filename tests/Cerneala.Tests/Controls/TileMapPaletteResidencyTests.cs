using System.Runtime.CompilerServices;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;
using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class TileMapPaletteResidencyTests
{
    [Fact]
    public async Task UsedSetsSharingAnAtlasKeepTheirDefinitionsButDeclareOneImageDependency()
    {
        ResourceId<ImageResource> atlas = new("shared");
        TileDefinition2D first = new(1, new(0, 0, 10, 10));
        TileDefinition2D second = new(2, new(10, 0, 10, 10));
        TileMap2DModel model = new("map", new DrawSize(10, 10),
            [new TileSet2D("first", atlas, [first]), new TileSet2D("second", atlas, [second])],
            [new TileChunk2D(default, 2, 1, [new TileCell2D(1), new TileCell2D(2)])]);
        TileMapSource2D source = TileMapSource2D.FromModel(model);
        TileMapChunkInfo2D info = Assert.Single(source.Catalog.Chunks);
        Assert.Equal(atlas, Assert.Single(info.Images).ResourceId);
        using SceneSpatialLease2D<TileMapChunkData2D> lease = await source.LoadAsync(info.Spatial);
        Assert.Equal(2, lease.Value.TileSets.Count);
        Assert.True(lease.Value.TryResolveTile(1, out TileSet2D? firstSet, out TileDefinition2D? foundFirst));
        Assert.True(lease.Value.TryResolveTile(2, out TileSet2D? secondSet, out TileDefinition2D? foundSecond));
        Assert.Equal("first", firstSet!.Id);
        Assert.Equal("second", secondSet!.Id);
        Assert.Same(first, foundFirst);
        Assert.Same(second, foundSecond);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("unused")]
    [InlineData("duplicate-id")]
    [InlineData("duplicate-set")]
    [InlineData("null")]
    [InlineData("empty")]
    public void GridPayloadRequiresExactlyItsUsedDefinitions(string invalid)
    {
        TileChunk2D grid = new(default, 1, 1, [new TileCell2D(1)]);
        TileDefinition2D first = new(1, new(0, 0, 10, 10));
        TileSet2D set = new("set", new("atlas"), [first]);
        TileSet2D[] CreateSets() => invalid switch
        {
            "missing" => [],
            "unused" => [new("set", new("atlas"), [first, new TileDefinition2D(2, new(0, 0, 10, 10))])],
            "duplicate-id" => [set, new("another", new("atlas"), [first])],
            "duplicate-set" => [set, set],
            "null" => [null!],
            _ => [new("set", new("atlas"), [])]
        };
        Assert.Throws<ArgumentException>(() => new TileMapChunkData2D(grid, CreateSets()));
    }

    [Fact]
    public void PayloadCollectionsAreImmutableAndUnknownIdsDoNotLoadOrResolve()
    {
        TileDefinition2D tile = new(1, new(0, 0, 10, 10));
        TileSet2D set = new("set", new("atlas"), [tile]);
        List<TileSet2D> sets = [set];
        TileMapChunkData2D data = new(new TileChunk2D(default, 1, 1, [new TileCell2D(1)]), sets);
        sets.Clear();
        Assert.Same(set, Assert.Single(data.TileSets));
        Assert.Throws<NotSupportedException>(() => ((IList<TileSet2D>)data.TileSets).Clear());
        foreach (int id in new[] { int.MinValue, -1, 0, 2, int.MaxValue })
        {
            Assert.False(data.TryResolveTile(id, out TileSet2D? missingSet, out TileDefinition2D? missingTile));
            Assert.Null(missingSet);
            Assert.Null(missingTile);
        }
        Assert.Empty(new TileMapChunkData2D(new TileChunk2D(default, 1, 1, [default]), []).TileSets);
        Assert.Empty(new TileMapChunkData2D([]).TileSets);
    }

    [Theory]
    [InlineData("image")]
    [InlineData("source-rectangle")]
    [InlineData("id")]
    [InlineData("collider-count")]
    public async Task AcquiredPaletteMustMatchDeclaredDependenciesGeometryAndColliderCount(string invalid)
    {
        ImageReference atlas = new(new ResourceId<ImageResource>("atlas"));
        TileMapChunkInfo2D info = new(new("chunk", new(0, 0, 10, 10), new DrawRect(0, 0, 10, 10)),
            new TileMapBounds2D(0, 0, 1, 1), invalid == "id" ? [] : [1], invalid == "image" ? [] : [atlas]);
        TileMapCatalog2D catalog = new("map", [info], new(10, 10),
            imageSizes: new Dictionary<string, DrawSize> { ["atlas"] = new(10, 10) });
        int releases = 0;
        TileMapSource2D source = new(catalog, (_, _, _) =>
        {
            TileDefinition2D tile = new(1, new(invalid == "source-rectangle" ? 10 : 0, 0, 10, 10),
                collider: invalid == "collider-count" ? new(TileColliderShape2D.Box, width: 10, height: 10) : null);
            TileMapChunkData2D data = new(new TileChunk2D(default, 1, 1, [new TileCell2D(1)]),
                [new TileSet2D("set", new("atlas"), [tile])]);
            return ValueTask.FromResult(new SceneSpatialLease2D<TileMapChunkData2D>(data, _ => releases++));
        });
        Assert.Equal(0, releases);
        await Assert.ThrowsAsync<ArgumentException>(() => source.LoadAsync(info.Spatial).AsTask());
        Assert.Equal(1, releases);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FullPaletteAndMetadataCollectAfterTheLastRegionAcross32Cycles(bool render)
    {
        using StreamingFixture fixture = new(render);
        for (int iteration = 0; iteration < 32; iteration++)
        {
            ExerciseAcquisitions(fixture, iteration);
            Collect();
            Assert.All(fixture.Retired, reference => Assert.False(reference.IsAlive,
                "The live source, map, collision or render cache retained a retired payload object."));
        }
        Assert.Equal(32 * 8, fixture.Retired.Count);
        Assert.Equal(render ? 32 : 0, fixture.ImageLoads);
        Assert.Equal(fixture.ImageLoads, fixture.ImageReleases);
        GC.KeepAlive(fixture);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ExerciseAcquisitions(StreamingFixture fixture, int iteration)
    {
        fixture.Show(true);
        using SceneCollisionRegion2D first = fixture.Prepare();
        using SceneCollisionRegion2D second = fixture.Prepare();
        Assert.Equal(iteration + 1, fixture.Loads);
        Assert.Equal(iteration, fixture.Releases);
        Assert.Equal(2, fixture.Map.LogicalChildren.Count); // Different opaque properties must not coalesce.
        Assert.Single(fixture.Scene.CollisionWorld.Raycast(new(5, -5), Vector2.UnitY, 20));
        Assert.Single(fixture.Scene.CollisionWorld.Raycast(new(15, -5), Vector2.UnitY, 20));
        Collect();
        Assert.All(fixture.Retired.TakeLast(8), reference => Assert.True(reference.IsAlive));
        first.Dispose();
        fixture.Show(false);
        Assert.True(second.IsReady);
        Assert.Equal(iteration, fixture.Releases);
        Assert.Equal(2, fixture.Map.LogicalChildren.Count);
        Assert.Equal(fixture.ImageLoads, fixture.ImageReleases);
        second.Dispose();
        fixture.Pump();
        Assert.Equal(iteration + 1, fixture.Releases);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().ResidentDataChunks);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().RetainedObjects);
        Assert.Empty(fixture.Map.LogicalChildren);
    }

    private static void Collect()
    {
        for (int cycle = 0; cycle < 3; cycle++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    private sealed class StreamingFixture : IDisposable, IAsyncImageLoader
    {
        internal readonly Scene2D Scene = new();
        internal readonly TileMap2D Map = new();
        internal readonly List<WeakReference> Retired = [];
        internal int Loads, Releases, ImageLoads, ImageReleases;
        private readonly UIRoot? root;
        private readonly RenderSurface2D? surface;
        private readonly SceneSimulationContext2D? context;

        internal StreamingFixture(bool render)
        {
            TileMapChunkInfo2D info = new(new("chunk", new(0, 0, 20, 10), new DrawRect(0, 0, 20, 10)),
                new TileMapBounds2D(0, 0, 2, 1), [1, 2], [new(new ResourceId<ImageResource>("atlas"))], expandedColliderCount: 2);
            Map.Source = new(new("map", [info], new(10, 10)), (_, _, _) =>
                ValueTask.FromResult(CreateAcquisition()));
            Scene.Children.Add(Map);
            if (render)
            {
                root = new(100, 100);
                root.SetImageLoader(this);
                surface = new() { Scene = Scene, ViewBox = new(5000, 0, 100, 100) };
                surface.Resources.SetResource(new ResourceId<ImageResource>("atlas"), new ImageResource("atlas.png"));
                root.VisualChildren.Add(surface);
            }
            else { context = new(Scene); }
            Pump();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private SceneSpatialLease2D<TileMapChunkData2D> CreateAcquisition()
        {
            byte[] bulk = new byte[65_536];
            Dictionary<string, object?> properties = new() { ["bulk"] = bulk };
            TileColliderDescriptor2D firstCollider = new(TileColliderShape2D.Box, width: 10, height: 10,
                properties: new Dictionary<string, object?> { ["bulk"] = bulk, ["material"] = "wood" });
            TileColliderDescriptor2D secondCollider = new(TileColliderShape2D.Box, width: 10, height: 10,
                properties: new Dictionary<string, object?> { ["bulk"] = bulk, ["material"] = "stone" });
            TileDefinition2D first = new(1, new(0, 0, 10, 10), properties, firstCollider);
            TileDefinition2D second = new(2, new(0, 0, 10, 10), properties, secondCollider);
            TileSet2D set = new("set", new("atlas"), [first, second], properties: properties);
            TileChunk2D grid = new(default, 2, 1, [new TileCell2D(1), new TileCell2D(2)], properties: properties);
            TileMapChunkData2D data = new(grid, [set]);
            Retired.AddRange([new(data), new(grid), new(set), new(first), new(second), new(firstCollider), new(secondCollider), new(bulk)]);
            Loads++;
            return new(data, _ => Releases++);
        }

        internal SceneCollisionRegion2D Prepare()
        {
            Task<SceneCollisionRegion2D> request = Scene.CollisionWorld.PrepareRegionAsync(new(0, -10, 20, 30)).AsTask();
            Assert.True(SpinWait.SpinUntil(() => { Pump(); return request.IsCompleted; }, TimeSpan.FromSeconds(5)));
            return request.GetAwaiter().GetResult();
        }

        internal void Show(bool visible)
        {
            if (surface is not null) { surface.ViewBox = new(visible ? 0 : 5000, 0, 100, 100); }
            Pump();
        }

        internal void Pump()
        {
            if (root is null) { context!.Update(); return; }
            root.ProcessFrame();
            ((ITimeSensitiveRenderElement)surface!).UpdateRenderTime(TimeSpan.Zero);
            DrawCommandList commands = new();
            ((IRenderSurface2DFrameSource)surface!).RecordFrame(commands, new(0, 0, 100, 100));
        }

        public IDrawImage Load(string path) => throw new InvalidOperationException("No synchronous image path.");
        public ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            ImageLoads++;
            return ValueTask.FromResult<IDrawImage>(new TestImage(() => ImageReleases++));
        }
        public void Dispose()
        {
            if (root is not null) { root.VisualChildren.Remove(surface!); }
            context?.Dispose();
        }
    }

    private sealed class TestImage(Action release) : IDrawImage, IDisposable
    {
        public int Width => 10;
        public int Height => 10;
        public void Dispose() => release();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void AStandaloneCatalogDoesNotRetainDefinitionsCollidersOrOpaqueProperties(int target)
    {
        var (catalog, references) = CreateCatalog();
        for (int cycle = 0; cycle < 3; cycle++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        Assert.False(references[target].IsAlive);
        GC.KeepAlive(catalog);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (TileMapCatalog2D, WeakReference[]) CreateCatalog()
    {
        object metadata = new byte[65_536];
        Dictionary<string, object?> properties = new() { ["Bulk"] = metadata };
        TileColliderDescriptor2D collider = new(TileColliderShape2D.Box, properties: properties);
        TileDefinition2D definition = new(1, new DrawRect(0, 0, 1, 1), properties, collider);
        TileSet2D set = new("set", new ResourceId<ImageResource>("atlas"), [definition], properties: properties);
        TileMap2DModel model = new("map", new DrawSize(1, 1), [set],
            [new TileChunk2D(default, 1, 1, [new TileCell2D(1)], properties: properties)], properties: properties);
        TileMapCatalog2D catalog = TileMapSource2D.FromModel(model).Catalog;
        return (catalog, [new(definition), new(collider), new(metadata)]);
    }
}
