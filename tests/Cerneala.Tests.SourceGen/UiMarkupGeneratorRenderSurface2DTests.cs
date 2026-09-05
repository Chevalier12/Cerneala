using System;
using System.IO;
using System.Linq;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void RenderSurface2DHostsRetainedContentFromMarkup()
    {
        const string markup = """
            <RenderSurface2D>
              <Button Content="Overlay" />
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator(
            "RenderSurface2DHost.crn",
            markup,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        RenderSurface2D surface = Assert.IsType<RenderSurface2D>(
            InvokeCreate(stream, "Cerneala.GeneratedUi.RenderSurface2DHostFactory"));
        Button overlay = Assert.IsType<Button>(surface.Content);

        Assert.Equal("Overlay", overlay.Content);
    }

    [Fact]
    public void RenderSurface2DDeclaresImageResourceAndUsesTypedSpriteReference()
    {
        const string markup = """
            <RenderSurface2D
                xmlns:resources="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
              <RenderSurface2D.Resources>
                <resources:ImageResource Name="WorldAtlas" Source="Assets/world.png" />
              </RenderSurface2D.Resources>
              <RenderSurface2D.Scene>
                <Scene2D>
                  <Sprite2D SourceResourceId="$WorldAtlas" />
                  <Sprite2D SourceResourceId="$WorldAtlas" />
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator(
            "RenderSurface2DImageResource.crn",
            markup,
            out Compilation compilation);

        Assert.DoesNotContain(
            result.Diagnostics,
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        RenderSurface2D surface = Assert.IsType<RenderSurface2D>(
            InvokeCreate(stream, "Cerneala.GeneratedUi.RenderSurface2DImageResourceFactory"));
        Assert.NotNull(surface.Scene);
        Assert.Equal(2, surface.Scene.Children.Count);
        ResourceId<ImageResource> expectedId = new("WorldAtlas");
        Assert.True(surface.Resources.TryGetResource(expectedId, out ImageResource? atlas));
        Assert.Equal("Assets/world.png", atlas.Path);
        Sprite2D first = Assert.IsType<Sprite2D>(surface.Scene.Children[0]);
        Sprite2D second = Assert.IsType<Sprite2D>(surface.Scene.Children[1]);
        Assert.Equal(expectedId, first.SourceResourceId);
        Assert.Equal(expectedId, second.SourceResourceId);
    }

    [Fact]
    public void RenderSurface2DSceneGroupTransformOriginCompilesFromSceneSpaceMarkup()
    {
        const string markup = """
            <RenderSurface2D>
              <RenderSurface2D.Scene>
                <Scene2D TransformOrigin="2,3" TranslateX="7" ScaleX="-2" />
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator(
            "RenderSurface2DSceneTransform.crn",
            markup,
            out Compilation compilation);

        Assert.DoesNotContain(
            result.Diagnostics,
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        RenderSurface2D surface = Assert.IsType<RenderSurface2D>(
            InvokeCreate(stream, "Cerneala.GeneratedUi.RenderSurface2DSceneTransformFactory"));
        Scene2D scene = Assert.IsType<Scene2D>(surface.Scene);
        Assert.Equal(new Cerneala.Drawing.DrawPoint(2, 3), scene.TransformOrigin);
        Assert.Equal(7, scene.TranslateX);
        Assert.Equal(-2, scene.ScaleX);
    }

    [Fact]
    public void RenderSurface2DSceneMarkupUsesRealAspectMotionPrismAndTemplatesSyntax()
    {
        const string markup = """
            <RenderSurface2D
                xmlns:resources="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
              <RenderSurface2D.Resources>
                <resources:ImageResource Name="WorldAtlas" Source="Assets/world.png" />
              </RenderSurface2D.Resources>
              <RenderSurface2D.Scene>
                <Scene2D OrderMode="LayerThenY"
                         TranslateX="32"
                         TransformOrigin="128,96">
                  <Scene2D.Aspect>
                    @on Loaded
                    {
                      @animate with Tween(100ms)
                      {
                        @to { TranslateY = 8; }
                      }
                    }
                  </Scene2D.Aspect>
                  @prism
                  {
                    @layer GroupContent { Opacity = 1; @filter Blur { Radius = 1; } }
                  }
                  <Scene2D Layer="1">
                    <Scene2D.Aspect>
                      @on Loaded
                      {
                        @animate with Tween(100ms)
                        {
                          @to { Opacity = 0.75; }
                        }
                      }
                    </Scene2D.Aspect>
                    @prism
                    {
                      @layer LayerContent { Opacity = 1; @filter Blur { Radius = 1; } }
                    }
                    <SceneItems2D>
                      @templates
                      {
                        <ContentTemplate DataType="System.String">
                          <Sprite2D SourceResourceId="$WorldAtlas">
                            <Sprite2D.Aspect>
                              @on Loaded
                              {
                                @animate with Tween(100ms)
                                {
                                  @to { Opacity = 0.5; }
                                }
                              }
                            </Sprite2D.Aspect>
                            @prism
                            {
                              @layer SpriteContent { Opacity = 1; @filter Blur { Radius = 1; } }
                            }
                          </Sprite2D>
                        </ContentTemplate>
                      }
                    </SceneItems2D>
                  </Scene2D>
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator(
            "RenderSurface2DSceneEffects.crn",
            markup,
            out Compilation compilation);

        Assert.DoesNotContain(
            result.Diagnostics,
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
    }

    [Fact]
    public void RenderSurface2DSceneMotionRejectsStructuralLayerPropertyWithExplicitDiagnostic()
    {
        const string markup = """
            <RenderSurface2D>
              <RenderSurface2D.Scene>
                <Scene2D>
                  <Scene2D.Aspect>
                    @on Loaded
                    {
                      @animate with Tween(100ms)
                      {
                        @to { Layer = 2; }
                      }
                    }
                  </Scene2D.Aspect>
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator(
            "RenderSurface2DStructuralMotionDiagnostic.crn",
            markup,
            out _);

        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics.Where(candidate =>
                candidate.Severity == DiagnosticSeverity.Error));
        Assert.Equal("CERNEALAUI006", diagnostic.Id);
        Assert.Contains("Layer", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void RenderSurface2DScenePrismRejectsInvalidCompositionWithExplicitDiagnostic()
    {
        const string markup = """
            <RenderSurface2D>
              <RenderSurface2D.Scene>
                <Scene2D>
                  @prism
                  {
                    @filter Blur
                    {
                      Radius = 1;
                    }
                  }
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator(
            "RenderSurface2DInvalidPrismComposition.crn",
            markup,
            out _);

        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics.Where(candidate =>
                candidate.Severity == DiagnosticSeverity.Error));
        Assert.Equal("PRISM1003", diagnostic.Id);
        Assert.Contains(
            "@filter is not allowed directly inside @prism",
            diagnostic.GetMessage(),
            StringComparison.Ordinal);
    }
}
