using Microsoft.CodeAnalysis;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    [Trait("SceneImportStage", "0")]
    public void DebugOverlayUsesRealAspectMotionAndPrismMarkup()
    {
        const string markup = """
            <RenderSurface2D>
              <RenderSurface2D.Scene>
                <Scene2D>
                  <Scene2DDebugOverlay Flags="All" LineThickness="1">
                    <Scene2DDebugOverlay.Aspect>
                      @on Loaded
                      {
                        @animate with Tween(100ms)
                        {
                          @to { Opacity = 0.75; LineThickness = 2; }
                        }
                      }
                    </Scene2DDebugOverlay.Aspect>
                    @prism
                    {
                      @layer DebugPresentation
                      {
                        Opacity = 1;
                        @filter Blur { Radius = 1; }
                      }
                    }
                  </Scene2DDebugOverlay>
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator("SceneDebugOverlay.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compilation.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    [Trait("SceneImportStage", "0")]
    public void ImportedSourceBindingDoesNotRequireAParserInSourceGenerator()
    {
        const string markup = """
            <RenderSurface2D DataType="Cerneala.UI.Controls.TileMapSource2D">
              <RenderSurface2D.Scene>
                <Scene2D>
                  <TileMap2D Source="$DataContext:OneWay" />
                      <Sprite2D X="16" Y="0" Width="16" Height="16">
                        <Sprite2D.Aspect>
                          @on Loaded
                          {
                            @animate with Tween(100ms) { @to { Opacity = 0.9; } }
                          }
                        </Sprite2D.Aspect>
                        @prism { @layer Door { @filter Blur { Radius = 1; } } }
                      </Sprite2D>
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator("ImportedMapBinding.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compilation.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compilation.ReferencedAssemblyNames, static assembly => assembly.Name == "Cerneala.Scene2D.Importers");
    }
}
