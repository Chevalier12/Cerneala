using Microsoft.CodeAnalysis;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void SceneCompositionUsesTileMapDataAndSpriteNodes()
    {
        const string markup = """
            <RenderSurface2D xmlns:r="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
              <RenderSurface2D.Resources>
                <r:ImageResource Name="Atlas" Source="atlas.png" />
              </RenderSurface2D.Resources>
              <RenderSurface2D.Scene>
                <Scene2D>
                  <TileMap2D Layer="0">
                    <Tile Image="$Atlas" X="18" Y="11" ImageWidth="16" ImageHeight="16">
                      <BoxCollider2D Width="16" Height="4" OffsetY="12" />
                    </Tile>
                  </TileMap2D>
                  <Sprite2D Layer="1" Image="$Atlas" X="32" Y="16" Width="16" Height="16">
                    <CircleCollider2D Radius="4" OffsetX="8" OffsetY="12" />
                  </Sprite2D>
                  <SceneItems2D Layer="2" />
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator("SingleLayerTileMap.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void RemovedSceneAbstractionsAreAbsentFromThePublicApi()
    {
        System.Reflection.Assembly assembly = typeof(Cerneala.UI.Controls.Scene2D).Assembly;
        Assert.Null(assembly.GetType("Cerneala.UI.Controls.TileLayer2D"));
        Assert.Null(assembly.GetType("Cerneala.UI.Controls.TileInstance2D"));
        Assert.Null(typeof(Cerneala.UI.Controls.TileMap2D).GetProperty("PromotedTiles"));
        Assert.Null(typeof(Cerneala.UI.Controls.TileMap2D).GetMethod("Promote"));
    }

    [Theory]
    [InlineData("TileLayer2D")]
    [InlineData("TileInstance2D")]
    public void RemovedSceneAbstractionsHaveLocatedMarkupDiagnostics(string element)
    {
        string markup = $"<Scene2D><TileMap2D><{element} /></TileMap2D></Scene2D>";
        GeneratorRunResult result = RunGenerator("RemovedSceneNode.crn", markup, out _);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics.Where(candidate => candidate.Id == "CERNEALAUI002"));
        Assert.Equal("RemovedSceneNode.crn", diagnostic.Location.GetLineSpan().Path);
        Assert.Contains(element, diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Empty(result.GeneratedSources);
    }
}
