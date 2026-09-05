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
    public void TileMapMarkupUsesRealAspectMotionPrismSyntaxAtMapLayerAndPromotedTileScopes()
    {
        const string markup = """
            <RenderSurface2D DataType="Cerneala.UI.Controls.TileMap2DModel">
              <RenderSurface2D.Scene>
                <Scene2D>
                <TileMap2D Model="$DataContext:OneWay">
                  <TileMap2D.Aspect>
                    @on Loaded
                    {
                      @animate with Tween(100ms)
                      {
                        @to { Opacity = 0.9; }
                      }
                    }
                  </TileMap2D.Aspect>
                  @prism
                  {
                    @layer MapContent { Opacity = 1; @filter Blur { Radius = 1; } }
                  }
                  <TileLayer2D LayerId="Buildings">
                    <TileLayer2D.Aspect>
                      @on Loaded
                      {
                        @animate with Tween(100ms)
                        {
                          @to { Opacity = 0.8; }
                        }
                      }
                    </TileLayer2D.Aspect>
                    @prism
                    {
                      @layer LayerContent { Opacity = 1; @filter Blur { Radius = 1; } }
                    }
                    <TileInstance2D X="18" Y="11">
                      <TileInstance2D.Aspect>
                        @on Loaded
                        {
                          @animate with Tween(100ms)
                          {
                            @to { Tint = #FFFFCC; }
                          }
                        }
                      </TileInstance2D.Aspect>
                      @prism
                      {
                        @layer TileContent { Opacity = 1; @filter Blur { Radius = 2; } }
                      }
                    </TileInstance2D>
                  </TileLayer2D>
                </TileMap2D>
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator(
            "TileMap2DRealSyntax.crn",
            markup,
            out Compilation compilation);

        Assert.DoesNotContain(
            result.Diagnostics,
            static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(
            compilation.GetDiagnostics(),
            static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        TileMap2DModel model = CreateMarkupVillageModel();
        Assembly assembly = EmitBindingTestAssembly(compilation);
        RenderSurface2D surface = Assert.IsType<RenderSurface2D>(InvokeBindingTestCreate(
            assembly,
            "Cerneala.GeneratedUi.TileMap2DRealSyntaxFactory",
            model));
        UIRoot root = new();
        TestImage terrain = new("terrain");
        TestImage structures = new("structures");
        root.SetImageLoader(new TestImageLoader(new Dictionary<string, IDrawImage>(StringComparer.Ordinal)
        {
            ["terrain.png"] = terrain,
            ["structures.png"] = structures
        }));
        surface.Resources.SetResource(
            new ResourceId<ImageResource>("VillageTerrain"),
            new ImageResource("terrain.png"));
        surface.Resources.SetResource(
            new ResourceId<ImageResource>("VillageStructures"),
            new ImageResource("structures.png"));
        root.VisualChildren.Add(surface);
        root.ProcessFrame();

        Scene2D scene = Assert.IsType<Scene2D>(surface.Scene);
        TileMap2D map = Assert.IsType<TileMap2D>(Assert.Single(scene.Children));
        Assert.Same(model, map.Model);
        Assert.Equal(2, map.Layers.Count);
        TileLayer2D buildings = Assert.Single(map.Layers, static layer => layer.LayerId == "Buildings");
        Assert.Single(buildings.PromotedTiles);
        Assert.Equal(2, map.LogicalChildren.Count);

        DrawCommandList commands = RecordSurface(surface);
        DrawCommand[] images = commands
            .Where(static command => command.Kind is DrawCommandKind.DrawImage or DrawCommandKind.DrawSpriteBatch)
            .ToArray();
        Assert.Equal(3, images.Length);
        Assert.Equal([terrain, structures, structures], images.Select(static command => command.Image ?? command.SpriteBatch?.Image));
        Assert.Contains(images, static command =>
            command.Kind == DrawCommandKind.DrawSpriteBatch &&
            command.SpriteBatch!.Sprites.Any(sprite => sprite.Options.Flip == DrawImageFlip.Horizontal));
        Assert.DoesNotContain(images, static command =>
            command.Kind == DrawCommandKind.DrawSpriteBatch &&
            command.SpriteBatch!.Sprites.Any(sprite => sprite.Destination.X == 16 && sprite.Destination.Y == 16));
    }

    [Fact]
    [Trait("TileMapStage", "0")]
    public void DuplicatePromotedCoordinateProducesALocatedGeneratorDiagnostic()
    {
        const string markup = """
            <RenderSurface2D>
              <RenderSurface2D.Scene>
                <Scene2D>
                <TileMap2D>
                  <TileLayer2D LayerId="Buildings">
                    <TileInstance2D X="18" Y="11" />
                    <TileInstance2D X="18" Y="11" />
                  </TileLayer2D>
                </TileMap2D>
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator(
            "TileMap2DDuplicatePromotedCell.crn",
            markup,
            out _);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics.Where(static candidate =>
            candidate.Severity == DiagnosticSeverity.Error &&
            candidate.GetMessage().Contains("18,11", StringComparison.Ordinal)));
        FileLinePositionSpan lineSpan = diagnostic.Location.GetLineSpan();
        Assert.Equal("TileMap2DDuplicatePromotedCell.crn", lineSpan.Path);
        Assert.True(lineSpan.StartLinePosition.Line > 0);
    }

    [Fact]
    [Trait("TileMapStage", "1")]
    public void DuplicateLayerIdProducesALocatedGeneratorDiagnostic()
    {
        const string markup = """
            <RenderSurface2D>
              <RenderSurface2D.Scene>
                <Scene2D>
                  <TileMap2D>
                    <TileLayer2D LayerId="Buildings" />
                    <TileLayer2D LayerId="Buildings" />
                  </TileMap2D>
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator("TileMap2DDuplicateLayer.crn", markup, out _);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics.Where(static candidate =>
            candidate.Severity == DiagnosticSeverity.Error &&
            candidate.GetMessage().Contains("Buildings", StringComparison.Ordinal)));
        FileLinePositionSpan lineSpan = diagnostic.Location.GetLineSpan();
        Assert.Equal("TileMap2DDuplicateLayer.crn", lineSpan.Path);
        Assert.True(lineSpan.StartLinePosition.Line > 0);
    }

    [Theory]
    [InlineData("<TileLayer2D><TileInstance2D X=\"0\" Y=\"0\" /></TileLayer2D>", "LayerId")]
    [InlineData("<TileLayer2D LayerId=\"Buildings\"><TileInstance2D X=\"bad\" Y=\"0\" /></TileLayer2D>", "X")]
    [InlineData("<TileLayer2D LayerId=\"Buildings\"><TileInstance2D X=\"0\" /></TileLayer2D>", "Y")]
    [Trait("TileMapStage", "1")]
    public void InvalidLayerOrPromotedCoordinateProducesALocatedGeneratorDiagnostic(
        string declaration,
        string expectedProperty)
    {
        string markup = $"""
            <RenderSurface2D>
              <RenderSurface2D.Scene>
                <Scene2D>
                  <TileMap2D>{declaration}</TileMap2D>
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator("TileMap2DInvalidDeclaration.crn", markup, out _);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics.Where(static candidate =>
            candidate.Severity == DiagnosticSeverity.Error));
        Assert.Contains(expectedProperty, diagnostic.GetMessage(), StringComparison.Ordinal);
        FileLinePositionSpan lineSpan = diagnostic.Location.GetLineSpan();
        Assert.Equal("TileMap2DInvalidDeclaration.crn", lineSpan.Path);
        Assert.True(lineSpan.StartLinePosition.Line > 0);
    }

    private static TileMap2DModel CreateMarkupVillageModel() =>
        new(
            new DrawSize(16, 16),
            [
                new TileSet2D(
                    "Terrain",
                    new ResourceId<ImageResource>("VillageTerrain"),
                    [new TileDefinition2D(1, new DrawRect(0, 0, 16, 16))]),
                new TileSet2D(
                    "Structures",
                    new ResourceId<ImageResource>("VillageStructures"),
                    [new TileDefinition2D(100, new DrawRect(16, 0, 16, 16))])
            ],
            [
                new TileLayer2DModel(
                    "Ground",
                    [new TileChunk2D(
                        new TileCoordinate2D(0, 0),
                        2,
                        1,
                        [new TileCell2D(1, TileFlip2D.Horizontal), new TileCell2D(100, TileFlip2D.Vertical)])],
                    order: 0),
                new TileLayer2DModel(
                    "Buildings",
                    [new TileChunk2D(
                        new TileCoordinate2D(18, 11),
                        2,
                        1,
                        [new TileCell2D(100), new TileCell2D(0)])],
                    order: 1)
            ],
            new TileMapBounds2D(0, 0, 20, 12));

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
