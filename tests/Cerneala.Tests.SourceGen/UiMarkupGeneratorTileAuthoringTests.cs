using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void TileAuthoringUsesImagesPixelPositionsAndOptionalIndependentSizesWithoutPerTileUiElements()
    {
        const string markup = """
            <RenderSurface2D xmlns:r="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
              <RenderSurface2D.Resources>
                <r:ImageResource Name="Grass" Source="grass.png" />
                <r:ImageResource Name="House" Source="house.png" />
              </RenderSurface2D.Resources>
              <RenderSurface2D.Scene>
                <Scene2D>
                  <TileMap2D>
                    <Tile Image="$Grass" X="-2.5" Y="4.25" />
                    <Tile Image="$Grass" X="20" Y="12" Width="7" Height="9" />
                    <Tile Image="$House" X="15" Y="10" Width="80" />
                    <Tile Image="$Grass" X="25" Y="15" Height="6" />
                  </TileMap2D>
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator("PixelTiles.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        Assembly assembly = EmitBindingTestAssembly(compilation);
        RenderSurface2D surface = Assert.IsType<RenderSurface2D>(InvokeBindingTestCreate(
            assembly, "Cerneala.GeneratedUi.PixelTilesFactory"));
        TestImage grass = new("grass");
        TestImage house = new("house");
        UIRoot root = new();
        root.SetImageLoader(new TestImageLoader(new Dictionary<string, IDrawImage>
        {
            ["grass.png"] = grass,
            ["house.png"] = house
        }));
        root.VisualChildren.Add(surface);
        root.ProcessFrame();

        TileMap2D map = Assert.IsType<TileMap2D>(Assert.Single(Assert.IsType<Scene2D>(surface.Scene).Children));
        Assert.DoesNotContain(map.LogicalChildren, child => child is Sprite2D or TileInstance2D);
        Assert.All(map.Layers, layer => Assert.Empty(layer.PromotedTiles));

        DrawSpriteBatch[] batches = RecordSurface(surface)
            .Where(command => command.Kind == DrawCommandKind.DrawSpriteBatch)
            .Select(command => command.SpriteBatch!)
            .ToArray();
        Assert.Equal(3, batches.Length);
        Assert.Equal(new IDrawImage[] { grass, house, grass }, batches.Select(batch => batch.Image));
        Assert.Equal(
            new[]
            {
                new DrawRect(-2.5f, 4.25f, 32, 32),
                new DrawRect(20, 12, 7, 9),
                new DrawRect(15, 10, 80, 32),
                new DrawRect(25, 15, 32, 6)
            },
            batches.SelectMany(batch => batch.Sprites).Select(sprite => sprite.Destination));
    }

    [Theory]
    [InlineData("<TileMap2D><TileMap2D.Model /></TileMap2D>")]
    [InlineData("<TileMap2D><TileInstance2D X=\"0\" Y=\"0\" /></TileMap2D>")]
    [InlineData("<TileMap2D><TileMap2D.Layers><TileLayer2D LayerId=\"Ground\" /></TileMap2D.Layers></TileMap2D>")]
    [InlineData("<TileMap2D><Sprite2D /></TileMap2D>")]
    [InlineData("<TileMap2D>1,2,3;4,5,6</TileMap2D>")]
    [InlineData("<TileMap2D TileSize=\"16,16\" />")]
    public void TileAuthoringRejectsAlternativeMapMarkup(string mapMarkup)
    {
        string markup = """
            <RenderSurface2D DataType="Cerneala.UI.Controls.TileMap2DModel">
              <RenderSurface2D.Scene><Scene2D>
            """ + mapMarkup + "</Scene2D></RenderSurface2D.Scene></RenderSurface2D>";

        GeneratorRunResult result = RunGenerator("LegacyTileAuthoring.crn", markup, out _);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData("<TileMap2D Model=\"$DataContext\"><Tile Image=\"$Grass\" /></TileMap2D>")]
    [InlineData("<TileMap2D><TileLayer2D LayerId=\"Ground\" /><Tile Image=\"$Grass\" /></TileMap2D>")]
    [InlineData("<TileMap2D><Tile Image=\"$Grass\" /><TileLayer2D LayerId=\"Ground\" /></TileMap2D>")]
    public void TileAuthoringRejectsMixingPlacementsWithImportedModelPresentation(string mapMarkup)
    {
        string markup = """
            <RenderSurface2D DataType="Cerneala.UI.Controls.TileMap2DModel"
                xmlns:r="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
              <RenderSurface2D.Resources><r:ImageResource Name="Grass" Source="grass.png" /></RenderSurface2D.Resources>
              <RenderSurface2D.Scene><Scene2D>
            """ + mapMarkup + "</Scene2D></RenderSurface2D.Scene></RenderSurface2D>";
        GeneratorRunResult result = RunGenerator("MixedTileAuthoring.crn", markup, out _);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI005" &&
            diagnostic.GetMessage().Contains("cannot be combined", StringComparison.Ordinal));
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void TileAuthoringRejectsModelAssignmentThroughAspect()
    {
        const string markup = """
            <TileMap2D>
              <TileMap2D.Aspect>@default { Model = null; }</TileMap2D.Aspect>
            </TileMap2D>
            """;
        GeneratorRunResult result = RunGenerator("TileAspectModel.crn", markup, out _);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI005" &&
            diagnostic.GetMessage().Contains("not Model or Layers", StringComparison.Ordinal));
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void TileAuthoringKeepsMapLevelAspectMotionAndPrism()
    {
        const string markup = """
            <TileMap2D xmlns:r="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
              <TileMap2D.Resources><r:ImageResource Name="Art" Source="art.png" /></TileMap2D.Resources>
              <TileMap2D.Aspect>
                @on Loaded { @animate with Tween(100ms) { @from { Opacity = 0.5; } @to { Opacity = 1; } } }
              </TileMap2D.Aspect>
              @prism { @layer Content { @filter Blur { Radius = 1; } } }
              <Tile Image="$Art" X="10" Y="20" />
            </TileMap2D>
            """;
        GeneratorRunResult result = RunGenerator("TileEffects.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        string generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("AttachPrism", generated);
        Assert.Contains("AttachMotionSession", generated);
        Assert.Contains("new global::Cerneala.UI.Controls.Tile(", generated);
    }

    [Theory]
    [InlineData("<Tile />")]
    [InlineData("<Tile Image=\"grass.png\" />")]
    [InlineData("<Tile Image=\"$Brush\" />")]
    [InlineData("<Tile Image=\"$Grass\" Width=\"-1\" />")]
    [InlineData("<Tile Image=\"$Grass\" X=\"NaN\" />")]
    [InlineData("<Tile Image=\"$Grass\" Y=\"Infinity\" />")]
    [InlineData("<Tile Image=\"$Grass\" Height=\"NaN\" />")]
    [InlineData("<Tile Image=\"$Grass\" X=\"$DataContext.X:OneWay\" />")]
    [InlineData("<Tile Image=\"$Grass\" Name=\"GrassTile\" />")]
    [InlineData("<Tile Image=\"$Grass\" SourceRect=\"0,0,16,16\" />")]
    [InlineData("<Tile Image=\"$Grass\"><Sprite2D /></Tile>")]
    public void TileAuthoringRejectsInvalidPlacementDeclarations(string tile)
    {
        string markup = """
            <TileMap2D xmlns:r="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
              <TileMap2D.Resources>
                <r:ImageResource Name="Grass" Source="grass.png" />
                <SolidColorBrush Name="Brush" Color="Green" />
              </TileMap2D.Resources>
            """ + tile + "</TileMap2D>";
        GeneratorRunResult result = RunGenerator("InvalidTile.crn", markup, out _);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }
}
