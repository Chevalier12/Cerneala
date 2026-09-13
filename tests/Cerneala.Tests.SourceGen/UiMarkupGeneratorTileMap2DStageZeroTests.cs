using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    [Trait("TileMapStage", "0")]
    public void TileMapMarkupUsesRealAspectMotionPrismSyntaxAtSceneMapAndSpriteScopes()
    {
        const string markup = """
            <RenderSurface2D DataType="Cerneala.UI.Controls.TileMap2DModel"
                             xmlns:r="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
              <RenderSurface2D.Resources>
                <r:ImageResource Name="VillageTerrain" Source="terrain.png" />
                <r:ImageResource Name="VillageStructures" Source="structures.png" />
              </RenderSurface2D.Resources>
              <RenderSurface2D.Scene>
                <Scene2D>
                  <Scene2D.Aspect>
                    @on Loaded { @animate with Tween(100ms) { @to { Opacity = 0.9; } } }
                  </Scene2D.Aspect>
                  @prism { @layer SceneContent { @filter Blur { Radius = 1; } } }
                  <TileMap2D Name="Ground" Layer="0" />
                  <TileMap2D Name="Buildings" Layer="1" Model="$DataContext:OneWay">
                    <TileMap2D.Aspect>
                      @on Loaded { @animate with Tween(100ms) { @to { Opacity = 0.8; } } }
                    </TileMap2D.Aspect>
                    @prism { @layer MapContent { @filter Blur { Radius = 1; } } }
                  </TileMap2D>
                  <Sprite2D Layer="2" Image="$VillageStructures"
                            X="288" Y="176" Width="16" Height="16"
                            SourceX="16" SourceY="0" SourceWidth="16" SourceHeight="16">
                    <Sprite2D.Aspect>
                      @on Loaded { @animate with Tween(100ms) { @to { Tint = #FFFFCC; } } }
                    </Sprite2D.Aspect>
                    @prism { @layer SpriteContent { @filter Blur { Radius = 2; } } }
                  </Sprite2D>
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator("TileMap2DRealSyntax.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        (TileMap2DModel groundModel, TileMap2DModel buildingsModel) = CreateMarkupVillageModels();
        Assembly assembly = EmitBindingTestAssembly(compilation);
        RenderSurface2D surface = Assert.IsType<RenderSurface2D>(InvokeBindingTestCreate(
            assembly, "Cerneala.GeneratedUi.TileMap2DRealSyntaxFactory", buildingsModel));
        Scene2D scene = Assert.IsType<Scene2D>(surface.Scene);
        Assert.Equal(3, scene.Children.Count);
        TileMap2D ground = Assert.IsType<TileMap2D>(scene.Children[0]);
        TileMap2D buildings = Assert.IsType<TileMap2D>(scene.Children[1]);
        Sprite2D door = Assert.IsType<Sprite2D>(scene.Children[2]);
        ground.Model = groundModel;
        UIRoot root = new();
        TestImage terrain = new("terrain");
        TestImage structures = new("structures");
        root.SetImageLoader(new TestImageLoader(new Dictionary<string, IDrawImage>(StringComparer.Ordinal)
        {
            ["terrain.png"] = terrain,
            ["structures.png"] = structures
        }));
        root.VisualChildren.Add(surface);
        root.ProcessFrame();
        try
        {
            Assert.Same(buildingsModel, buildings.Model);
            Assert.Same(scene, door.LogicalParent);
            Assert.Empty(ground.LogicalChildren);
            Assert.Empty(buildings.LogicalChildren);
            Assert.Equal(0, ground.Layer);
            Assert.Equal(1, buildings.Layer);
            Assert.Equal(2, door.Layer);

            DrawCommand[] images = RecordSurface(surface)
                .Where(command => command.Kind is DrawCommandKind.DrawImage or DrawCommandKind.DrawSpriteBatch)
                .ToArray();
            Assert.Equal(3, images.Length);
            Assert.Equal([terrain, structures, structures], images.Select(command => command.Image ?? command.SpriteBatch?.Image));
            Assert.Contains(images, command => command.Kind == DrawCommandKind.DrawSpriteBatch &&
                command.SpriteBatch!.Sprites.Any(sprite => sprite.Options.Flip == DrawImageFlip.Horizontal));
            Assert.DoesNotContain(images, command => command.Kind == DrawCommandKind.DrawSpriteBatch &&
                command.SpriteBatch!.Sprites.Any(sprite => sprite.Destination.X == 288 && sprite.Destination.Y == 176));
        }
        finally { root.VisualChildren.Remove(surface); }
    }

    [Fact]
    public void EqualCoordinatesRemainIndependentStaticTileDeclarations()
    {
        const string markup = """
            <Scene2D xmlns:r="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
              <Scene2D.Resources><r:ImageResource Name="Atlas" Source="atlas.png" /></Scene2D.Resources>
              <TileMap2D>
                <Tile Image="$Atlas" X="1.5" Y="-2.5" />
                <Tile Image="$Atlas" X="1.5" Y="-2.5" />
              </TileMap2D>
            </Scene2D>
            """;
        GeneratorRunResult result = RunGenerator("IndependentTileData.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assembly assembly = EmitBindingTestAssembly(compilation);
        Scene2D scene = Assert.IsType<Scene2D>(InvokeBindingTestCreate(assembly, "Cerneala.GeneratedUi.IndependentTileDataFactory"));
        TileMap2D map = Assert.IsType<TileMap2D>(Assert.Single(scene.Children));
        Assert.Equal(2, map.Model!.Tiles.Count);
        Assert.All(map.Model.Tiles, tile => { Assert.Equal(1.5f, tile.X); Assert.Equal(-2.5f, tile.Y); });
        Assert.Empty(map.LogicalChildren);
    }

    [Fact]
    public void SpriteCoordinatesMayBeBoundAndSharedWithOtherSprites()
    {
        const string markup = """
            <Scene2D>
              <Sprite2D X="1.25" Y="1.25" Width="16" Height="16" />
              <Sprite2D X="$self.Y:OneWay" Y="1.25" Width="16" Height="16" />
            </Scene2D>
            """;
        GeneratorRunResult result = RunGenerator("IndependentSprites.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    private static (TileMap2DModel Ground, TileMap2DModel Buildings) CreateMarkupVillageModels()
    {
        TileSet2D[] tileSets =
        [
            new TileSet2D(
                "Terrain",
                new ResourceId<ImageResource>("VillageTerrain"),
                [new TileDefinition2D(1, new DrawRect(0, 0, 16, 16))]),
            new TileSet2D(
                "Structures",
                new ResourceId<ImageResource>("VillageStructures"),
                [new TileDefinition2D(100, new DrawRect(16, 0, 16, 16))])
        ];
        TileMapBounds2D bounds = new(0, 0, 20, 12);
        return (
            new TileMap2DModel(
                "Ground", new DrawSize(16, 16), tileSets,
                [new TileChunk2D(
                    new TileCoordinate2D(0, 0), 2, 1,
                    [new TileCell2D(1, TileFlip2D.Horizontal), new TileCell2D(100, TileFlip2D.Vertical)])],
                bounds, order: 0),
            new TileMap2DModel(
                "Buildings", new DrawSize(16, 16), tileSets,
                [new TileChunk2D(
                    new TileCoordinate2D(18, 11), 2, 1,
                    [new TileCell2D(0), new TileCell2D(0)])],
                bounds, order: 1));
    }

    private static DrawCommandList RecordSurface(RenderSurface2D surface)
    {
        DrawCommandList commands = new();
        MethodInfo method = typeof(RenderSurface2D)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(static candidate => candidate.Name.EndsWith(".RecordFrame", StringComparison.Ordinal));
        method.Invoke(surface, [commands, new DrawRect(0, 0, 512, 256)]);
        return commands;
    }

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
