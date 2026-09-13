using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Resources;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class TileMap2DContractTests
{
    [Fact]
    public void CellStorageContainsExactlyTwoInt32ValuesWithoutPaddingOrReferences()
    {
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(TileFlip2D)));
        Assert.Equal(8, System.Runtime.CompilerServices.Unsafe.SizeOf<TileCell2D>());
        Assert.False(System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<TileCell2D>());
        Assert.Equal(System.Runtime.InteropServices.LayoutKind.Sequential,
            typeof(TileCell2D).StructLayoutAttribute!.Value);
    }

    [Fact]
    public void ChunkContentComparisonPreservesExactIdsFlipsLengthsAndDefensiveCopying()
    {
        int[] ids = [0, 1, 256, 65_536, 16_777_216, 1 << 30, int.MaxValue];
        TileCell2D[] values = ids.SelectMany(id => Enumerable.Range(0, 8)
            .Select(flip => new TileCell2D(id, (TileFlip2D)flip))).ToArray();
        foreach (int length in new[] { 1, 2, 3, 4, 7, 8, 15, 16, 17, 31, 32, 33, 63, 64, 65, 255, 256, 257 })
        {
            TileCell2D[] cells = Enumerable.Range(0, length).Select(index => values[index % values.Length]).ToArray();
            TileChunk2D original = new(default, length, 1, cells);
            TileChunk2D equal = new(new TileCoordinate2D(-17, 4), length, 1, cells, version: 2);
            Assert.True(original.HasSameCells(equal));
            Assert.True(equal.HasSameCells(original));
            Assert.False(original.HasSameCells(new TileChunk2D(default, length + 1, 1, cells.Append(default))));

            for (int index = 0; index < length; index++)
            {
                TileCell2D[] changed = cells.ToArray();
                TileCell2D cell = changed[index];
                changed[index] = new TileCell2D(cell.TileId ^ (1 << 24), cell.Flip);
                Assert.False(original.HasSameCells(new TileChunk2D(default, length, 1, changed)));
                foreach (TileFlip2D flag in new[] { TileFlip2D.Horizontal, TileFlip2D.Vertical, TileFlip2D.Diagonal })
                {
                    changed[index] = new TileCell2D(cell.TileId, cell.Flip ^ flag);
                    Assert.False(original.HasSameCells(new TileChunk2D(default, length, 1, changed)));
                }
            }

            cells[0] = new TileCell2D(cells[0].TileId ^ 1, cells[0].Flip);
            Assert.True(original.HasSameCells(equal));
            Assert.Throws<NotSupportedException>(() => ((IList<TileCell2D>)original.Tiles)[0] = default);
        }
    }

    [Fact]
    [Trait("TileMapStage", "1")]
    public void ModelCopiesInputsAndValidatesIdsBoundsChunksAndVersions()
    {

        TileCell2D[] cells = [new(1), new(0, TileFlip2D.Horizontal)];
        TileChunk2D chunk = new(new TileCoordinate2D(0, 0), 2, 1, cells);
        cells[0] = default;
        TileMap2DModel model = new("Ground", new DrawSize(16, 16), [TerrainSet()], [chunk], new TileMapBounds2D(0, 0, 2, 1));
        Assert.Equal(1, chunk.Tiles[0].TileId);
        Assert.True(model.TryResolveTile(1, out TileSet2D? tileSet, out TileDefinition2D? tile));
        Assert.Equal("Terrain", tileSet!.Id);
        Assert.Equal(new DrawRect(0, 0, 16, 16), tile!.SourceRect);
        Assert.Throws<ArgumentException>(() => new TileMap2DModel(
            "Ground", new DrawSize(16, 16), [TerrainSet()],
            [new TileChunk2D(default, 1, 1, [new TileCell2D(99)])], new TileMapBounds2D(0, 0, 1, 1)));
        Assert.Throws<ArgumentException>(() => new TileMap2DModel("Overlap", new DrawSize(16, 16), [],
            [new TileChunk2D(default, 2, 2, EmptyCells(4)), new TileChunk2D(new(1, 1), 2, 2, EmptyCells(4))]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TileChunk2D(default, 1, 1, [default], version: 0));
        TileMap2DModel sparse = new("Sparse", new DrawSize(16, 16), [TerrainSet()],
            [new TileChunk2D(new(-16, 48), 1, 1, [new TileCell2D(1)])]);
        Assert.Null(sparse.Bounds);
    }

    [Fact]
    [Trait("TileMapStage", "1")]
    public void MultipleAtlasesSourceRectsAndMapOrderRecordThroughSpriteBatches()
    {
        (UIRoot root, RenderSurface2D surface, TileMap2D map, TestImage terrain, TestImage structures) = CreateSurface();

        DrawCommand[] draws = Record(surface).Where(IsImageCommand).ToArray();

        Assert.Equal(3, draws.Length);
        Assert.Equal([terrain, structures, structures], draws.Select(ImageOf));
        Assert.Equal(
            [
                new DrawRect(0, 0, 16, 16),
                new DrawRect(16, 0, 16, 16),
                new DrawRect(16, 0, 16, 16)
            ],
            draws.Select(SourceOf));
        Assert.Equal(2, surface.Scene!.Children.Count);
        Assert.Empty(map.LogicalChildren);
        Assert.All(surface.Scene.Children.OfType<TileMap2D>(), static item => Assert.Empty(item.LogicalChildren));
        Assert.Same(root, surface.Root);
    }

    [Fact]
    [Trait("TileMapStage", "1")]
    public void VillageNodeCountTracksMapsAndExplicitSpritesRatherThanStaticTiles()
    {

        TileMapVillageFixture fixture = TileMapVillageFixture.Create();
        TileSet2D terrain = new("Terrain", new ResourceId<ImageResource>("VillageTerrain"),
            Enumerable.Range(1, 12).Select(static id => new TileDefinition2D(id, new DrawRect((id - 1) * 16, 0, 16, 16))));
        TileSet2D structures = new("Structures", new ResourceId<ImageResource>("VillageStructures"),
            Enumerable.Range(100, 8).Select(static id => new TileDefinition2D(id, new DrawRect((id - 100) * 16, 0, 16, 16))));
        Scene2D scene = new() { OrderMode = SceneOrderMode.Layer };
        foreach (var group in fixture.FiniteChunks.GroupBy(static chunk => chunk.Layer))
        {
            TileMap2DModel model = new($"Map{group.Key}", new DrawSize(16, 16), [terrain, structures],
                group.Select(static chunk => new TileChunk2D(
                    new TileCoordinate2D(chunk.OriginX * TileMapVillageFixture.ChunkSize, chunk.OriginY * TileMapVillageFixture.ChunkSize),
                    TileMapVillageFixture.ChunkSize, TileMapVillageFixture.ChunkSize,
                    chunk.Cells.Select(static cell => new TileCell2D(cell.TileId, (TileFlip2D)(int)cell.Flip)))),
                new TileMapBounds2D(0, 0, TileMapVillageFixture.WidthInTiles, TileMapVillageFixture.HeightInTiles), order: group.Key);
            scene.Children.Add(new TileMap2D { Model = model, Layer = model.Order });
        }
        Assert.Equal(36_864, fixture.FiniteChunks.Sum(static chunk => chunk.Cells.Count));
        Assert.Equal(3, scene.Children.Count);
        Assert.All(scene.Children, static map => Assert.Empty(map.LogicalChildren));
        Sprite2D sprite = new() { Image = new(terrain.AtlasResourceId), Width = 16, Height = 16 };
        scene.Children.Add(sprite);
        Assert.Equal(4, scene.Children.Count);
        Assert.Same(scene, sprite.LogicalParent);
        Assert.All(scene.Children.OfType<TileMap2D>(), static map => Assert.Empty(map.LogicalChildren));
    }

    [Fact]
    [Trait("TileMapStage", "1")]
    public void CallerCanReplaceAStaticCellWithAPeerSpriteAndRestoreTheImmutableModel()
    {

        (_, RenderSurface2D surface, TileMap2D map, TestImage terrain, TestImage structures) = CreateSurface();
        TileMap2DModel original = map.Model!;
        map.Model = new("Ground", original.TileSize, original.TileSets,
            [new TileChunk2D(default, 2, 1, [new TileCell2D(1), default], version: original.Chunks[0].Version + 1)],
            original.Bounds, version: original.Version + 1);
        Sprite2D sprite = new()
        {
            Image = new(new ResourceId<ImageResource>("VillageStructures")), X = 16, Width = 16, Height = 16,
            SourceX = 16, SourceY = 0, SourceWidth = 16, SourceHeight = 16, Flip = RenderSurface2DSpriteFlip.Vertical
        };
        surface.Scene!.Children.Add(sprite);
        DrawCommand[] draws = Record(surface).Where(IsImageCommand).ToArray();
        Assert.Equal([DrawCommandKind.DrawSpriteBatch, DrawCommandKind.DrawImage, DrawCommandKind.DrawSpriteBatch],
            draws.Select(static draw => draw.Kind));
        Assert.Equal([terrain, structures, structures], draws.Select(ImageOf));
        Assert.Equal(new DrawRect(16, 0, 16, 16), draws[1].ImageSource);
        Assert.Equal(DrawImageFlip.Vertical, draws[1].ImageFlip);
        Assert.Equal(new DrawRect(16, 0, 16, 16), draws[1].Rect);
        Assert.True(map.Model.TryGetCell(new(1, 0), out TileCell2D cleared));
        Assert.Equal(0, cleared.TileId);
        Assert.True(original.TryGetCell(new(1, 0), out TileCell2D unchanged));
        Assert.Equal(100, unchanged.TileId);
        surface.Scene.Children.Remove(sprite);
        map.Model = original;
        DrawCommand[] restored = Record(surface).Where(IsImageCommand).ToArray();
        Assert.Equal(3, restored.Length);
        Assert.All(restored, static draw => Assert.Equal(DrawCommandKind.DrawSpriteBatch, draw.Kind));
    }

    [Fact]
    [Trait("TileMapStage", "1")]
    public void CellKeysRequireAStableMapIdentity()
    {

        Assert.Throws<ArgumentException>(() => new TileCellKey2D("", new TileCoordinate2D(0, 0)));
        TileCellKey2D key = new("Ground", -2, 5);
        Assert.Equal("Ground", key.MapId);
        Assert.Equal(new TileCoordinate2D(-2, 5), key.Coordinate);
    }

    [Fact]
    [Trait("TileMapStage", "1")]
    public void PeerSpritePresentationDoesNotChangeStaticMapData()
    {

        (_, RenderSurface2D surface, TileMap2D map, _, _) = CreateSurface();
        TileMap2DModel original = map.Model!;
        Sprite2D sprite = new()
        {
            Image = new(new ResourceId<ImageResource>("VillageTerrain")), Width = 16, Height = 16, Opacity = 0.5f
        };
        surface.Scene!.Children.Add(sprite);
        DrawCommand draw = Assert.Single(Record(surface), static command => command.Kind == DrawCommandKind.DrawImage);
        Assert.Equal((byte)128, draw.Color.A);
        sprite.Visibility = Visibility.Hidden;
        Assert.DoesNotContain(Record(surface), static command => command.Kind == DrawCommandKind.DrawImage);
        Assert.Same(original, map.Model);
    }

    [Fact]
    [Trait("TileMapStage", "1")]
    public void SceneAndPeerMapSpriteKeepAspectMotionAndNestedPrismScopes()
    {

        ManualMotionClock clock = new();
        (UIRoot root, RenderSurface2D surface, TileMap2D map, _, _) = CreateSurface(clock);
        Scene2D scene = surface.Scene!;
        Sprite2D tile = new() { Image = new(new ResourceId<ImageResource>("VillageTerrain")), Width = 16, Height = 16 };
        scene.Children.Add(tile);
        scene.Aspect = new ElementAspect([new ElementAspectValue(UIElement.OpacityProperty, 0.9f)]);
        map.Aspect = new ElementAspect([new ElementAspectValue(TileMap2D.TintProperty, Color.CornflowerBlue)]);
        tile.Aspect = new ElementAspect([new ElementAspectValue(Sprite2D.TintProperty, Color.White)]);
        using IDisposable scenePrism = AttachPrism(scene, "Scene");
        using IDisposable mapPrism = AttachPrism(map, "Map");
        using IDisposable tilePrism = AttachPrism(tile, "Sprite");
        root.ProcessFrame();
        Assert.Equal(0.9f, scene.Opacity);
        Assert.Equal(Color.CornflowerBlue, map.Tint);
        Cerneala.UI.Motion.Core.MotionHandle handle = tile.Motion().Animate(Sprite2D.TintProperty)
            .To(Color.Black).With(MotionFactory.Tween<Color>(TimeSpan.FromMilliseconds(100)));
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();
        Assert.NotEqual(Color.White, tile.Tint);
        Assert.NotEqual(Color.Black, tile.Tint);
        DrawCommandList commands = Record(surface);
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        Assert.Equal(3, analysis.Scopes.Count());
        Assert.Single(analysis.Scopes, static scope => scope.Depth == 0);
        Assert.Equal(2, analysis.Scopes.Count(static scope => scope.Depth == 1));
        var tileScope = Assert.Single(analysis.Scopes, scope => scope.Depth == 1 &&
            commands.Skip(scope.BeginCommandIndex + 1).Take(scope.EndCommandIndex - scope.BeginCommandIndex - 1)
                .Any(static command => command.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(new DrawRect(0, 0, 16, 16), tileScope.Bounds);
        Assert.Single(commands.Skip(tileScope.BeginCommandIndex + 1)
            .Take(tileScope.EndCommandIndex - tileScope.BeginCommandIndex - 1).Where(IsImageCommand));
        Assert.True(handle.IsActive);
    }

    [Fact]
    [Trait("TileMapStage", "4")]
    public void StaticTilesBatchByAtlasWithinOneSemanticOrderSegment()
    {
        (_, RenderSurface2D surface, TileMap2D map, TestImage terrain, TestImage structures) =
            CreateBatchingSurface(
                [new TileCell2D(1), new TileCell2D(100), new TileCell2D(1), new TileCell2D(100)]);

        DrawCommand[] draws = Record(surface).Where(IsImageCommand).ToArray();

        Assert.Equal(2, draws.Length);
        Assert.All(draws, static draw => Assert.Equal(DrawCommandKind.DrawSpriteBatch, draw.Kind));
        Assert.Equal([terrain, structures], draws.Select(ImageOf));
        Assert.Equal(
            [new DrawRect(0, 0, 16, 16), new DrawRect(32, 0, 16, 16)],
            draws[0].SpriteBatch!.Sprites.Select(static sprite => sprite.Destination));
        Assert.Equal(
            [new DrawRect(16, 0, 16, 16), new DrawRect(48, 0, 16, 16)],
            draws[1].SpriteBatch!.Sprites.Select(static sprite => sprite.Destination));
        Assert.All(draws, static draw =>
        {
            Assert.Equal(DrawSamplingMode.Point, draw.SpriteBatch!.Sampling);
            Assert.Equal(DrawAddressMode.Clamp, draw.SpriteBatch.AddressMode);
        });
        TileMap2DDiagnosticsSnapshot diagnostics = map.GetDiagnosticsSnapshot();
        Assert.Equal(2, diagnostics.DrawCommands);
        Assert.Equal(4, diagnostics.DrawnTiles);
    }

    [Fact]
    [Trait("TileMapStage", "4")]
    public void PeerSpritePrismDoesNotSplitStaticAtlasBatches()
    {

        (_, RenderSurface2D surface, TileMap2D map, TestImage terrain, TestImage structures) =
            CreateBatchingSurface([new TileCell2D(1), default, new TileCell2D(1), new TileCell2D(100), new TileCell2D(1)]);
        Sprite2D sprite = new()
        {
            Image = new(new ResourceId<ImageResource>("VillageStructures")), X = 16, Width = 16, Height = 16,
            SourceX = 16, SourceY = 0, SourceWidth = 16, SourceHeight = 16
        };
        surface.Scene!.Children.Add(sprite);
        using IDisposable prism = AttachPrism(sprite, "Sprite");
        DrawCommandList commands = Record(surface);
        DrawCommand[] draws = commands.Where(IsImageCommand).ToArray();
        Assert.Equal([DrawCommandKind.DrawSpriteBatch, DrawCommandKind.DrawSpriteBatch, DrawCommandKind.DrawImage],
            draws.Select(static draw => draw.Kind));
        Assert.Equal([terrain, structures, structures], draws.Select(ImageOf));
        Assert.Equal([new DrawRect(0, 0, 16, 16), new DrawRect(32, 0, 16, 16), new DrawRect(64, 0, 16, 16)],
            draws[0].SpriteBatch!.Sprites.Select(static sprite => sprite.Destination));
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        var scope = Assert.Single(analysis.Scopes);
        Assert.Single(commands.Skip(scope.BeginCommandIndex + 1).Take(scope.EndCommandIndex - scope.BeginCommandIndex - 1).Where(IsImageCommand));
        Assert.Equal(2, map.GetDiagnosticsSnapshot().DrawCommands);
        Record(surface);
        Assert.Equal(2, map.GetDiagnosticsSnapshot().BatchesReused);
        Assert.Equal(0, map.GetDiagnosticsSnapshot().BatchesRebuilt);
    }

    [Fact]
    [Trait("TileMapStage", "4")]
    public void AtlasBatchingNeverMovesCommandsAcrossMapOrder()
    {

        (_, RenderSurface2D surface, TileMap2D late, TestImage terrain, TestImage structures) =
            CreateBatchingSurface([new TileCell2D(1), new TileCell2D(100)]);
        late.Layer = 10;
        late.Tint = Color.Red;
        TileMap2D early = new()
        {
            Layer = -10, Tint = Color.Blue,
            Model = new TileMap2DModel("Early", new DrawSize(16, 16), [TerrainSet(), StructureSet()],
                [new TileChunk2D(default, 2, 1, [new TileCell2D(100), new TileCell2D(1)])])
        };
        surface.Scene!.OrderMode = SceneOrderMode.Layer;
        surface.Scene.Children.Add(early);
        DrawCommand[] draws = Record(surface).Where(IsImageCommand).ToArray();
        Assert.Equal([structures, terrain, terrain, structures], draws.Select(ImageOf));
        Assert.All(draws.Take(2), static draw =>
            Assert.All(draw.SpriteBatch!.Sprites, static sprite => Assert.Equal(Color.Blue, sprite.Options.Tint)));
        Assert.All(draws.Skip(2), static draw =>
            Assert.All(draw.SpriteBatch!.Sprites, static sprite => Assert.Equal(Color.Red, sprite.Options.Tint)));
    }

    private static (UIRoot Root, RenderSurface2D Surface, TileMap2D Map, TestImage Terrain, TestImage Structures)
        CreateSurface(ManualMotionClock? clock = null)
    {
        ResourceId<ImageResource> terrainId = new("VillageTerrain");
        ResourceId<ImageResource> structureId = new("VillageStructures");
        TestImage terrain = new("terrain");
        TestImage structures = new("structures");
        TestImageLoader loader = new(new Dictionary<string, IDrawImage>(StringComparer.Ordinal)
        {
            ["terrain.png"] = terrain,
            ["structures.png"] = structures
        });
        TileMap2D map = new()
        {
            Model = new TileMap2DModel("Ground", new DrawSize(16, 16), [TerrainSet(), StructureSet()],
                [new TileChunk2D(default, 2, 1, [new TileCell2D(1), new TileCell2D(100, TileFlip2D.Vertical)])],
                new TileMapBounds2D(0, 0, 2, 1))
        };
        TileMap2D overlay = new()
        {
            Layer = 1,
            Model = new TileMap2DModel("Overlay", new DrawSize(16, 16), [TerrainSet(), StructureSet()],
                [new TileChunk2D(default, 2, 1, [new TileCell2D(100), default])], new TileMapBounds2D(0, 0, 2, 1))
        };
        Scene2D scene = new() { OrderMode = SceneOrderMode.Layer };
        scene.Children.Add(map);
        scene.Children.Add(overlay);
        RenderSurface2D surface = new() { Scene = scene };
        surface.Resources.SetResource(terrainId, new ImageResource("terrain.png"));
        surface.Resources.SetResource(structureId, new ImageResource("structures.png"));
        UIRoot root = clock is null ? new UIRoot() : new UIRoot(motionClock: clock);
        root.SetImageLoader(loader);
        root.VisualChildren.Add(surface);
        return (root, surface, map, terrain, structures);
    }

    private static (UIRoot Root, RenderSurface2D Surface, TileMap2D Map, TestImage Terrain, TestImage Structures)
        CreateBatchingSurface(IReadOnlyList<TileCell2D> cells)
    {
        ResourceId<ImageResource> terrainId = new("VillageTerrain");
        ResourceId<ImageResource> structureId = new("VillageStructures");
        TestImage terrain = new("terrain");
        TestImage structures = new("structures");
        TestImageLoader loader = new(new Dictionary<string, IDrawImage>(StringComparer.Ordinal)
        {
            ["terrain.png"] = terrain,
            ["structures.png"] = structures
        });
        TileMap2D map = new()
        {
            Model = new TileMap2DModel("Batch", new DrawSize(16, 16), [TerrainSet(), StructureSet()],
                [new TileChunk2D(default, cells.Count, 1, cells)], new TileMapBounds2D(0, 0, cells.Count, 1))
        };
        Scene2D scene = new();
        scene.Children.Add(map);
        RenderSurface2D surface = new() { Scene = scene };
        surface.Resources.SetResource(terrainId, new ImageResource("terrain.png"));
        surface.Resources.SetResource(structureId, new ImageResource("structures.png"));
        UIRoot root = new();
        root.SetImageLoader(loader);
        root.VisualChildren.Add(surface);
        return (root, surface, map, terrain, structures);
    }

    private static TileSet2D TerrainSet() =>
        new(
            "Terrain",
            new ResourceId<ImageResource>("VillageTerrain"),
            [new TileDefinition2D(1, new DrawRect(0, 0, 16, 16))]);

    private static TileSet2D StructureSet() =>
        new(
            "Structures",
            new ResourceId<ImageResource>("VillageStructures"),
            [new TileDefinition2D(100, new DrawRect(16, 0, 16, 16))]);

    private static TileCell2D[] EmptyCells(int count) =>
        Enumerable.Repeat(new TileCell2D(0), count).ToArray();

    private static DrawCommandList Record(RenderSurface2D surface)
    {
        DrawCommandList commands = new();
        ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new DrawRect(0, 0, 64, 64));
        return commands;
    }

    private static bool IsImageCommand(DrawCommand command) =>
        command.Kind is DrawCommandKind.DrawImage or DrawCommandKind.DrawSpriteBatch;

    private static IDrawImage? ImageOf(DrawCommand command) =>
        command.Image ?? command.SpriteBatch?.Image;

    private static DrawRect SourceOf(DrawCommand command) =>
        command.Kind == DrawCommandKind.DrawSpriteBatch
            ? Assert.Single(command.SpriteBatch!.Sprites).Options.Source!.Value
            : command.ImageSource!.Value;

    private static IDisposable AttachPrism(UIElement element, string name) =>
        GeneratedMarkup.AttachPrism(
            element,
            () => new PrismInstance(
                PrismTestData.Composition(name, PrismTestData.Layer(1, "Content"))));

    private sealed class TestImage(string name) : IDrawImage
    {
        public string Name { get; } = name;

        public int Width => 32;

        public int Height => 32;
    }

    private sealed class TestImageLoader(IReadOnlyDictionary<string, IDrawImage> images) : IImageLoader
    {
        public IDrawImage Load(string path) => images[path];
    }
}
