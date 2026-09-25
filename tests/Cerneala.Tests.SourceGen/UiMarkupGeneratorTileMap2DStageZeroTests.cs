using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            <RenderSurface2D xmlns:r="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
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
                  <TileMap2D Name="Ground" Layer="0">
                    <Tile Image="$VillageTerrain" ImageWidth="16" ImageHeight="16" X="0" Y="0" Width="16" Height="16" />
                  </TileMap2D>
                  <TileMap2D Name="Buildings" Layer="1">
                    <TileMap2D.Aspect>
                      @on Loaded { @animate with Tween(100ms) { @to { Opacity = 0.8; } } }
                    </TileMap2D.Aspect>
                    @prism { @layer MapContent { @filter Blur { Radius = 1; } } }
                    <Tile Image="$VillageStructures" ImageWidth="16" ImageHeight="16" X="32" Y="0" Width="16" Height="16" />
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

        Assert.Contains("TileMap2D.FromModel", Assert.Single(result.GeneratedSources).SourceText.ToString());
        Assembly assembly = EmitBindingTestAssembly(compilation);
        RenderSurface2D surface = Assert.IsType<RenderSurface2D>(InvokeBindingTestCreate(
            assembly, "Cerneala.GeneratedUi.TileMap2DRealSyntaxFactory"));
        Scene2D scene = Assert.IsType<Scene2D>(surface.Scene);
        Assert.Equal(3, scene.Children.Count);
        TileMap2D ground = Assert.IsType<TileMap2D>(scene.Children[0]);
        TileMap2D buildings = Assert.IsType<TileMap2D>(scene.Children[1]);
        Sprite2D door = Assert.IsType<Sprite2D>(scene.Children[2]);
        UIRoot root = new(512, 256);
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
                command.SpriteBatch!.Sprites.Any(sprite => sprite.Destination == new DrawRect(32, 0, 16, 16)));
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
                <Tile Image="$Atlas" ImageWidth="32" ImageHeight="32" X="1.5" Y="-2.5" />
                <Tile Image="$Atlas" X="1.5" Y="-2.5" />
              </TileMap2D>
            </Scene2D>
            """;
        GeneratorRunResult result = RunGenerator("IndependentTileData.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assembly assembly = EmitBindingTestAssembly(compilation);
        Scene2D scene = Assert.IsType<Scene2D>(InvokeBindingTestCreate(assembly, "Cerneala.GeneratedUi.IndependentTileDataFactory"));
        TileMap2D map = Assert.IsType<TileMap2D>(Assert.Single(scene.Children));
        Assert.Empty(map.LogicalChildren);
        RenderSurface2D surface = new() { Scene = scene };
        UIRoot root = new(512, 256);
        root.SetImageLoader(new TestImageLoader(new Dictionary<string, IDrawImage>(StringComparer.Ordinal)
        {
            ["atlas.png"] = new TestImage("atlas")
        }));
        root.VisualChildren.Add(surface);
        root.ProcessFrame();
        try
        {
            DrawSpriteBatch batch = Assert.Single(RecordSurface(surface)
                .Where(command => command.Kind == DrawCommandKind.DrawSpriteBatch)
                .Select(command => command.SpriteBatch!));
            Assert.Equal(2, batch.Sprites.Count);
            Assert.All(batch.Sprites, sprite => Assert.Equal(new DrawRect(1.5f, -2.5f, 32, 32), sprite.Destination));
        }
        finally { root.VisualChildren.Remove(surface); }
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

    private sealed class TestImageLoader(IReadOnlyDictionary<string, IDrawImage> images) : IAsyncImageLoader
    {
        public IDrawImage Load(string path) => images[path];
        public ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Load(path));
    }
}
