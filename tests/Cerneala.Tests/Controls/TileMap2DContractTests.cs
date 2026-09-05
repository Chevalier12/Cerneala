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
    [Trait("TileMapStage", "1")]
    public void ModelCopiesInputsAndValidatesIdsBoundsChunksAndVersions()
    {
        TileCell2D[] cells = [new(1), new(0, TileFlip2D.Horizontal)];
        TileChunk2D chunk = new(new TileCoordinate2D(0, 0), 2, 1, cells);
        cells[0] = new TileCell2D(0);
        TileMap2DModel model = new(
            new DrawSize(16, 16),
            [TerrainSet()],
            [new TileLayer2DModel("Ground", [chunk])],
            new TileMapBounds2D(0, 0, 2, 1));

        Assert.Equal(1, chunk.Tiles[0].TileId);
        Assert.True(model.TryResolveTile(1, out TileSet2D? tileSet, out TileDefinition2D? tile));
        Assert.Equal("Terrain", tileSet!.Id);
        Assert.Equal(new DrawRect(0, 0, 16, 16), tile!.SourceRect);
        Assert.Throws<ArgumentException>(() => new TileMap2DModel(
            new DrawSize(16, 16),
            [TerrainSet()],
            [new TileLayer2DModel("Ground", [new TileChunk2D(new TileCoordinate2D(0, 0), 1, 1, [new TileCell2D(99)])])],
            new TileMapBounds2D(0, 0, 1, 1)));
        Assert.Throws<ArgumentException>(() => new TileLayer2DModel(
            "Overlap",
            [
                new TileChunk2D(new TileCoordinate2D(0, 0), 2, 2, EmptyCells(4)),
                new TileChunk2D(new TileCoordinate2D(1, 1), 2, 2, EmptyCells(4))
            ]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TileChunk2D(
            new TileCoordinate2D(0, 0), 1, 1, [new TileCell2D(0)], version: 0));

        TileMap2DModel sparse = new(
            new DrawSize(16, 16),
            [TerrainSet()],
            [new TileLayer2DModel("Sparse", [new TileChunk2D(new TileCoordinate2D(-16, 48), 1, 1, [new TileCell2D(1)])])]);
        Assert.Null(sparse.Bounds);
    }

    [Fact]
    [Trait("TileMapStage", "1")]
    public void MultipleAtlasesSourceRectsAndLayerOrderRecordThroughSpriteBatches()
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
        Assert.Equal(2, map.Layers.Count);
        Assert.Equal(2, map.LogicalChildren.Count);
        Assert.DoesNotContain(map.LogicalChildren, static child => child is TileInstance2D);
        Assert.Same(root, surface.Root);
    }

    [Fact]
    [Trait("TileMapStage", "1")]
    public void VillageNodeCountTracksLayersAndPromotionsRatherThanStaticTiles()
    {
        TileMapVillageFixture fixture = TileMapVillageFixture.Create();
        TileSet2D terrain = new(
            "Terrain",
            new ResourceId<ImageResource>("VillageTerrain"),
            Enumerable.Range(1, 12)
                .Select(static id => new TileDefinition2D(id, new DrawRect((id - 1) * 16, 0, 16, 16))));
        TileSet2D structures = new(
            "Structures",
            new ResourceId<ImageResource>("VillageStructures"),
            Enumerable.Range(100, 8)
                .Select(static id => new TileDefinition2D(id, new DrawRect((id - 100) * 16, 0, 16, 16))));
        TileLayer2DModel[] layers = fixture.FiniteChunks
            .GroupBy(static chunk => chunk.Layer)
            .OrderBy(static group => group.Key)
            .Select(static group => new TileLayer2DModel(
                $"Layer{group.Key}",
                group.Select(static chunk => new TileChunk2D(
                    new TileCoordinate2D(
                        chunk.OriginX * TileMapVillageFixture.ChunkSize,
                        chunk.OriginY * TileMapVillageFixture.ChunkSize),
                    TileMapVillageFixture.ChunkSize,
                    TileMapVillageFixture.ChunkSize,
                    chunk.Cells.Select(static cell => new TileCell2D(
                        cell.TileId,
                        (TileFlip2D)(int)cell.Flip)))),
                order: group.Key))
            .ToArray();
        TileMap2D map = new()
        {
            Model = new TileMap2DModel(
                new DrawSize(16, 16),
                [terrain, structures],
                layers,
                new TileMapBounds2D(
                    0,
                    0,
                    TileMapVillageFixture.WidthInTiles,
                    TileMapVillageFixture.HeightInTiles))
        };

        Assert.Equal(36_864, fixture.FiniteChunks.Sum(static chunk => chunk.Cells.Count));
        Assert.Equal(3, map.Layers.Count);
        Assert.Equal(3, map.LogicalChildren.Count);
        Assert.All(map.Layers, static layer => Assert.Empty(layer.PromotedTiles));

        TileInstance2D promoted = map.Promote(new TileCellKey2D("Layer0", 0, 0), tileId: 1);
        Assert.Single(map.Layers[0].PromotedTiles);
        Assert.Contains(promoted, map.Layers[0].LogicalChildren);
        Assert.Equal(3, map.LogicalChildren.Count);
    }

    [Fact]
    [Trait("TileMapStage", "1")]
    public void PromotionOccupiesTheStaticSlotAndDemotionReturnsTheCellToItsBatch()
    {
        (_, RenderSurface2D surface, TileMap2D map, TestImage terrain, TestImage structures) = CreateSurface();
        TileCellKey2D key = new("Ground", 1, 0);

        TileInstance2D promoted = map.Promote(key);
        Assert.Same(promoted, map.Promote(key));
        DrawCommand[] promotedDraws = Record(surface).Where(IsImageCommand).ToArray();

        Assert.Equal(3, promotedDraws.Length);
        Assert.Equal(DrawCommandKind.DrawSpriteBatch, promotedDraws[0].Kind);
        Assert.Equal(DrawCommandKind.DrawImage, promotedDraws[1].Kind);
        Assert.Equal(DrawCommandKind.DrawSpriteBatch, promotedDraws[2].Kind);
        Assert.Equal([terrain, structures, structures], promotedDraws.Select(ImageOf));
        Assert.Equal(new DrawRect(16, 0, 16, 16), promotedDraws[1].ImageSource);
        Assert.Equal(DrawImageFlip.Vertical, promotedDraws[1].ImageFlip);
        Assert.Equal(new DrawRect(0, 0, 16, 16), promotedDraws[1].Rect);
        DrawCommandList promotedCommands = Record(surface);
        int promotedIndex = promotedCommands.ToList().FindIndex(static command => command.Kind == DrawCommandKind.DrawImage);
        DrawCommand promotedTransform = promotedCommands
            .Take(promotedIndex)
            .Last(command => command.Kind == DrawCommandKind.PushTransform);
        Assert.Equal(16, promotedTransform.Transform.M31);
        Assert.Equal(0, promotedTransform.Transform.M32);
        Assert.True(map.TryGetPromoted(key, out TileInstance2D? same));
        Assert.Same(promoted, same);

        Assert.True(map.Demote(key));
        Assert.False(map.Demote(key));
        DrawCommand[] demotedDraws = Record(surface).Where(IsImageCommand).ToArray();
        Assert.All(demotedDraws, static command => Assert.Equal(DrawCommandKind.DrawSpriteBatch, command.Kind));
        Assert.Equal(3, demotedDraws.Length);
    }

    [Fact]
    [Trait("TileMapStage", "1")]
    public void EmptyAndMissingPromotionsRequireAnExplicitValidReplacement()
    {
        (_, _, TileMap2D map, _, _) = CreateSurface();

        Assert.Throws<InvalidOperationException>(() => map.Promote(new TileCellKey2D("Overlay", 1, 0)));
        TileInstance2D replacement = map.Promote(new TileCellKey2D("Overlay", 1, 0), tileId: 1);
        Assert.Equal(1, replacement.TileId);
        Assert.Throws<ArgumentException>(() => map.Promote(new TileCellKey2D("Missing", 0, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.Promote(new TileCellKey2D("Ground", 5, 5)));
        Assert.Throws<ArgumentException>(() => new TileCellKey2D("", new TileCoordinate2D(0, 0)));
    }

    [Fact]
    [Trait("TileMapStage", "1")]
    public void MarkupStylePromotionRejectsMissingCoordinatesAndUsesItsOwnPresentation()
    {
        (_, RenderSurface2D surface, TileMap2D map, _, _) = CreateSurface();
        TileLayer2D ground = map.Layers.Single(candidate => candidate.LayerId == "Ground");
        ground.PromotedTiles.Add(new TileInstance2D { X = 5, Y = 5, TileId = 1 });

        Assert.Throws<InvalidOperationException>(() => Record(surface));

        ground.PromotedTiles.Clear();
        TileInstance2D promoted = map.Promote(new TileCellKey2D("Ground", 0, 0));
        promoted.Opacity = 0.5f;
        DrawCommandList commands = Record(surface);
        int imageIndex = commands.ToList().FindIndex(static command => command.Kind == DrawCommandKind.DrawImage);
        Assert.True(imageIndex > 0);
        Assert.Contains(
            commands.Take(imageIndex),
            static command => command.Kind == DrawCommandKind.PushOpacity && command.Opacity == 0.5f);

        promoted.Visibility = Visibility.Hidden;
        Assert.DoesNotContain(Record(surface), static command => command.Kind == DrawCommandKind.DrawImage);
    }

    [Fact]
    [Trait("TileMapStage", "1")]
    public void MapLayerAndPromotedTileKeepAspectMotionAndNestedPrismScopes()
    {
        ManualMotionClock clock = new();
        (UIRoot root, RenderSurface2D surface, TileMap2D map, _, _) = CreateSurface(clock);
        TileLayer2D layer = map.Layers.Single(candidate => candidate.LayerId == "Ground");
        TileInstance2D tile = map.Promote(new TileCellKey2D("Ground", 0, 0));
        map.Aspect = new ElementAspect([new ElementAspectValue(UIElement.OpacityProperty, 0.9f)]);
        layer.Aspect = new ElementAspect([new ElementAspectValue(TileLayer2D.TintProperty, Color.CornflowerBlue)]);
        tile.Aspect = new ElementAspect([new ElementAspectValue(TileInstance2D.TintProperty, Color.White)]);
        using IDisposable mapPrism = AttachPrism(map, "Map");
        using IDisposable layerPrism = AttachPrism(layer, "Layer");
        using IDisposable tilePrism = AttachPrism(tile, "Tile");
        root.ProcessFrame();

        Assert.Equal(0.9f, map.Opacity);
        Assert.Equal(Color.CornflowerBlue, layer.Tint);
        Cerneala.UI.Motion.Core.MotionHandle handle = tile.Motion()
            .Animate(TileInstance2D.TintProperty)
            .To(Color.Black)
            .With(MotionFactory.Tween<Color>(TimeSpan.FromMilliseconds(100)));
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();
        Assert.NotEqual(Color.White, tile.Tint);
        Assert.NotEqual(Color.Black, tile.Tint);

        DrawCommandList commands = Record(surface);
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        Assert.Equal(3, analysis.Scopes.Count());
        Assert.Contains(analysis.Scopes, static scope => scope.Depth == 0);
        Assert.Contains(analysis.Scopes, static scope => scope.Depth == 1);
        Assert.Contains(analysis.Scopes, static scope => scope.Depth == 2);
        var tileScope = Assert.Single(analysis.Scopes, static scope => scope.Depth == 2);
        Assert.Equal(new DrawRect(0, 0, 16, 16), tileScope.Bounds);
        DrawCommand[] tileScopeImages = commands
            .Skip(tileScope.BeginCommandIndex + 1)
            .Take(tileScope.EndCommandIndex - tileScope.BeginCommandIndex - 1)
            .Where(IsImageCommand)
            .ToArray();
        Assert.Single(tileScopeImages);
        Assert.Equal(DrawCommandKind.DrawImage, tileScopeImages[0].Kind);
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
        Assert.Equal(0, diagnostics.BatchSplits);
        Assert.Equal(0, diagnostics.PromotedInstancesVisible);
    }

    [Fact]
    [Trait("TileMapStage", "4")]
    public void PromotedPrismTileSplitsOnlyItsSemanticSlotAndReportsTheSplit()
    {
        (_, RenderSurface2D surface, TileMap2D map, TestImage terrain, TestImage structures) =
            CreateBatchingSurface(
                [
                    new TileCell2D(1),
                    new TileCell2D(100),
                    new TileCell2D(1),
                    new TileCell2D(100),
                    new TileCell2D(1)
                ]);
        TileInstance2D promoted = map.Promote(new TileCellKey2D("Batch", 1, 0));
        using IDisposable prism = AttachPrism(promoted, "Promoted");

        DrawCommandList commands = Record(surface);
        DrawCommand[] draws = commands.Where(IsImageCommand).ToArray();

        Assert.Equal(
            [
                DrawCommandKind.DrawSpriteBatch,
                DrawCommandKind.DrawImage,
                DrawCommandKind.DrawSpriteBatch,
                DrawCommandKind.DrawSpriteBatch
            ],
            draws.Select(static draw => draw.Kind));
        Assert.Equal([terrain, structures, terrain, structures], draws.Select(ImageOf));
        Assert.Equal(
            [new DrawRect(32, 0, 16, 16), new DrawRect(64, 0, 16, 16)],
            draws[2].SpriteBatch!.Sprites.Select(static sprite => sprite.Destination));

        int promotedIndex = commands.ToList().FindIndex(static command => command.Kind == DrawCommandKind.DrawImage);
        int beginIndex = commands.ToList().FindLastIndex(
            promotedIndex,
            static command => command.Kind == DrawCommandKind.BeginPrism);
        int endIndex = commands.ToList().FindIndex(
            promotedIndex,
            static command => command.Kind == DrawCommandKind.EndPrism);
        Assert.True(beginIndex >= 0 && beginIndex < promotedIndex);
        Assert.True(endIndex > promotedIndex);

        TileMap2DDiagnosticsSnapshot diagnostics = map.GetDiagnosticsSnapshot();
        Assert.Equal(4, diagnostics.DrawCommands);
        Assert.Equal(1, diagnostics.BatchSplits);
        Assert.Equal(1, diagnostics.Promotions);
        Assert.Equal(1, diagnostics.PromotedInstancesVisible);
    }

    [Fact]
    [Trait("TileMapStage", "4")]
    public void AtlasBatchingNeverMovesCommandsAcrossLayerOrder()
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
            Model = new TileMap2DModel(
                new DrawSize(16, 16),
                [TerrainSet(), StructureSet()],
                [
                    new TileLayer2DModel(
                        "Late",
                        [new TileChunk2D(new TileCoordinate2D(0, 0), 2, 1, [new TileCell2D(1), new TileCell2D(100)])],
                        order: 10,
                        tint: Color.Red),
                    new TileLayer2DModel(
                        "Early",
                        [new TileChunk2D(new TileCoordinate2D(0, 0), 2, 1, [new TileCell2D(100), new TileCell2D(1)])],
                        order: -10,
                        tint: Color.Blue)
                ],
                new TileMapBounds2D(0, 0, 2, 1))
        };
        Scene2D scene = new();
        scene.Children.Add(map);
        RenderSurface2D surface = new() { Scene = scene };
        surface.Resources.SetResource(terrainId, new ImageResource("terrain.png"));
        surface.Resources.SetResource(structureId, new ImageResource("structures.png"));
        UIRoot root = new();
        root.SetImageLoader(loader);
        root.VisualChildren.Add(surface);

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
        TileMap2DModel model = new(
            new DrawSize(16, 16),
            [TerrainSet(), StructureSet()],
            [
                new TileLayer2DModel(
                    "Ground",
                    [new TileChunk2D(new TileCoordinate2D(0, 0), 2, 1, [new TileCell2D(1), new TileCell2D(100, TileFlip2D.Vertical)])],
                    order: 0),
                new TileLayer2DModel(
                    "Overlay",
                    [new TileChunk2D(new TileCoordinate2D(0, 0), 2, 1, [new TileCell2D(100), new TileCell2D(0)])],
                    order: 1)
            ],
            new TileMapBounds2D(0, 0, 2, 1));
        TileMap2D map = new() { Model = model };
        Scene2D scene = new();
        scene.Children.Add(map);
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
            Model = new TileMap2DModel(
                new DrawSize(16, 16),
                [TerrainSet(), StructureSet()],
                [
                    new TileLayer2DModel(
                        "Batch",
                        [new TileChunk2D(new TileCoordinate2D(0, 0), cells.Count, 1, cells)])
                ],
                new TileMapBounds2D(0, 0, cells.Count, 1))
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
