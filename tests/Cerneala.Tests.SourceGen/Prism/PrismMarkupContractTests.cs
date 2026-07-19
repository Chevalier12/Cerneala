using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    private static readonly string[] ApprovedPrismDirectives =
    [
        "@prism",
        "@parameter",
        "@layer",
        "@group",
        "@filter",
        "@style",
        "@mask",
        "@backdrop"
    ];

    [Fact]
    public void PrismLanguageExposesExactlyTheEightApprovedDirectives()
    {
        Type language = typeof(Cerneala.SourceGen.UiMarkupGenerator).Assembly
            .GetType("Cerneala.SourceGen.Prism.Syntax.PrismMarkupLanguage")!;
        PropertyInfo namesProperty = language.GetProperty(
            "DirectiveNames",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        IReadOnlyList<string> directiveNames =
            (IReadOnlyList<string>)namesProperty.GetValue(null)!;

        Assert.Equal(ApprovedPrismDirectives, directiveNames);
        Assert.Equal(
            directiveNames.Count,
            directiveNames.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void PrismFixtureShellIsOtherwiseValidCui()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <ImageBrush Name="MaskImage" Source="mask.png" />
              </StackPanel.Resources>
              <Border Name="Card" />
              <Border />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator(
            "PrismFixtureShell.cui.xml",
            markup,
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
    }

    [Fact]
    public void PrismReusableAndInlineMarkupEmitTypedDefinitionsInDeclaredOrder()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <ImageBrush Name="MaskImage" Source="mask.png" />
                <PrismComposition Name="CardFx"
                                  WorkingColorProfile="DisplayP3"
                                  GlobalLightAngle="90"
                                  GlobalLightAltitude="45">
                  @parameter GlowRadius: float = 18;
                  @parameter Tint: color = #8060D8FF;

                  @layer Foreground
                  {
                    Visible = true;
                    Opacity = 0.9;
                    Fill = 0.8;
                    BlendMode = Screen;
                    ClipToBelow = true;
                    BlendChannels = RGBA;
                    Knockout = None;
                    BlendInteriorStylesAsGroup = false;
                    BlendClippedLayersAsGroup = true;
                    TransparencyShapesLayer = true;
                    LayerMaskHidesStyles = true;
                    VectorMaskHidesStyles = false;
                    BlendIfChannel = Gray;
                    ThisLayerRange = (0, 0.1, 0.9, 1);
                    UnderlyingRange = (0, 0, 1, 1);
                    DissolveSeed = 7;

                    @filter Blur
                    {
                      Radius = GlowRadius;
                    }

                    @style OuterGlow
                    {
                      Size = GlowRadius;
                      Color = Tint;
                    }

                    @mask
                    {
                      Image = $MaskImage;
                      Channel = Luminance;
                      Feather = 2;
                      Density = 0.85;
                      Invert = false;
                    }
                  }

                  @group Effects
                  {
                    Visible = true;
                    Opacity = 0.75;
                    BlendMode = PassThrough;

                    @layer Base
                    {
                      @filter BrightnessContrast
                      {
                        Brightness = 0.1;
                        Contrast = 0.2;
                      }
                    }
                  }

                  @backdrop Glass
                  {
                    Visible = true;
                    Opacity = 0.95;

                    @filter Blur
                    {
                      Radius = 12;
                    }
                  }
                </PrismComposition>
              </StackPanel.Resources>

              <Border Name="Card">
                @prism $CardFx(
                    GlowRadius = 24,
                    Tint = #A060D8FF
                );
              </Border>

              <Border>
                @prism
                {
                  WorkingColorProfile = LinearSrgb;
                  GlobalLightAngle = 120;
                  GlobalLightAltitude = 30;

                  @layer InlineLayer
                  {
                    @style DropShadow
                    {
                      Size = 18;
                      Distance = 8;
                      Angle = 90;
                      Color = #66000000;
                    }
                  }
                }
              </Border>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator(
            "CompletePrismSyntax.cui.xml",
            markup,
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("PrismCompositionDefinition", generated, StringComparison.Ordinal);
        Assert.Contains("PrismLayerDefinition", generated, StringComparison.Ordinal);
        Assert.Contains("PrismGroupDefinition", generated, StringComparison.Ordinal);
        Assert.Contains("PrismBackdropDefinition", generated, StringComparison.Ordinal);
        Assert.Contains("PrismFilterDefinition", generated, StringComparison.Ordinal);
        Assert.Contains("PrismStyleDefinition", generated, StringComparison.Ordinal);
        Assert.Contains("PrismMaskDefinition", generated, StringComparison.Ordinal);
        Assert.Contains("PrismColorProfile.DisplayP3", generated, StringComparison.Ordinal);
        Assert.Contains("PrismBlendMode.Screen", generated, StringComparison.Ordinal);
        Assert.True(
            generated.IndexOf("\"Foreground\"", StringComparison.Ordinal) <
            generated.IndexOf("\"Effects\"", StringComparison.Ordinal));
        Assert.True(
            generated.IndexOf("\"Effects\"", StringComparison.Ordinal) <
            generated.IndexOf("\"Glass\"", StringComparison.Ordinal));
        Assert.DoesNotContain("dynamic", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Reflection", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void PrismParserReportsUnknownDirectiveAtTheExactToken()
    {
        const string markup = """
            <Border>
              @prism
              {
                @layer Surface
                {
                  @filter Blur { Radius = 4; }
                  @sparkle { Amount = 1; }
                }
              }
            </Border>
            """;

        GeneratorRunResult result = RunGenerator(
            "PrismUnknownDirective.cui.xml",
            markup,
            out _);

        AssertPrismParseDiagnosticSnapshot(
            result,
            "PRISM1001",
            markup,
            "@sparkle",
            "Prism markup in 'PrismUnknownDirective.cui.xml' is invalid: " +
            "Unknown Prism directive '@sparkle'. Exactly eight Prism directives are supported.");
    }

    [Fact]
    public void PrismParserRecoversFromMissingBraceWithoutCascading()
    {
        const string markup = """
            <Border>
              @prism
              {
                @layer Surface
                {
                  @filter Blur { Radius = 4; }
                }
            </Border>
            """;

        GeneratorRunResult result = RunGenerator(
            "PrismMissingBrace.cui.xml",
            markup,
            out _);

        AssertPrismParseDiagnosticSnapshot(
            result,
            "PRISM1002",
            markup,
            "@prism",
            "Prism markup in 'PrismMissingBrace.cui.xml' is invalid: " +
            "@prism is missing its closing '}'.");
    }

    [Fact]
    public void PrismParserRejectsKnownDirectiveInIllegalContext()
    {
        const string markup = """
            <Border>
              @prism
              {
                @filter Blur
                {
                  Radius = 4;
                }
              }
            </Border>
            """;

        GeneratorRunResult result = RunGenerator(
            "PrismIllegalChild.cui.xml",
            markup,
            out _);

        AssertPrismParseDiagnosticSnapshot(
            result,
            "PRISM1003",
            markup,
            "@filter",
            "Prism markup in 'PrismIllegalChild.cui.xml' is invalid: " +
            "@filter is not allowed directly inside @prism.");
    }

    [Fact]
    public void PrismBinderReportsUnknownPropertyAtTheExactToken()
    {
        const string markup = """
            <Border>
              @prism
              {
                @layer Surface
                {
                  @filter Blur
                  {
                    DoesNotExist = 4;
                  }
                }
              }
            </Border>
            """;

        GeneratorRunResult result = RunGenerator(
            "PrismUnknownProperty.cui.xml",
            markup,
            out _);

        AssertPrismDiagnostic(result, "PRISM2001", markup, "DoesNotExist");
    }

    [Fact]
    public void PrismBinderReportsUnknownCatalogOperationAtTheExactToken()
    {
        const string markup = """
            <Border>
              @prism
              {
                @layer Surface
                {
                  @filter NotAFilter { Amount = 1; }
                }
              }
            </Border>
            """;

        GeneratorRunResult result = RunGenerator(
            "PrismUnknownFilter.cui.xml",
            markup,
            out _);

        AssertPrismDiagnostic(result, "PRISM2002", markup, "NotAFilter");
    }

    [Fact]
    public void PrismBinderReportsDuplicateNamesAtTheExactToken()
    {
        const string markup = """
            <Border>
              @prism
              {
                @layer Duplicate { @filter Blur { Radius = 2; } }
                @layer Duplicate { @filter Blur { Radius = 4; } }
              }
            </Border>
            """;

        GeneratorRunResult result = RunGenerator(
            "PrismDuplicateName.cui.xml",
            markup,
            out _);

        AssertPrismDiagnostic(result, "PRISM2003", markup, "Duplicate");
    }

    [Fact]
    public void PrismBinderReportsMissingRequiredParameterAtTheReference()
    {
        const string markup = """
            <Border>
              <Border.Resources>
                <PrismComposition Name="NeedsRadius">
                  @parameter Radius: float;
                  @layer Surface
                  {
                    @filter Blur { Radius = Radius; }
                  }
                </PrismComposition>
              </Border.Resources>
              @prism $NeedsRadius;
            </Border>
            """;

        GeneratorRunResult result = RunGenerator(
            "PrismMissingParameter.cui.xml",
            markup,
            out _);

        AssertPrismDiagnostic(result, "PRISM2004", markup, "$NeedsRadius");
    }

    [Fact]
    public void PrismBinderRejectsLayerChildrenAtTheNestedDirective()
    {
        const string markup = """
            <Border>
              @prism
              {
                @layer Outer
                {
                  @layer Inner { @filter Blur { Radius = 2; } }
                }
              }
            </Border>
            """;

        GeneratorRunResult result = RunGenerator(
            "PrismLayerChild.cui.xml",
            markup,
            out _);

        AssertPrismDiagnostic(result, "PRISM2005", markup, "Inner");
    }

    [Fact]
    public void PrismBinderRejectsMultipleBackdropsAtTheSecondName()
    {
        const string markup = """
            <Border>
              @prism
              {
                @backdrop First { @filter Blur { Radius = 2; } }
                @backdrop Second { @filter Blur { Radius = 4; } }
              }
            </Border>
            """;

        GeneratorRunResult result = RunGenerator(
            "PrismMultipleBackdrop.cui.xml",
            markup,
            out _);

        AssertPrismDiagnostic(result, "PRISM2006", markup, "Second");
    }

    [Fact]
    public void PrismBinderRejectsBackdropThatIsNotLast()
    {
        const string markup = """
            <Border>
              @prism
              {
                @backdrop Glass { @filter Blur { Radius = 4; } }
                @layer AfterBackdrop { @filter Blur { Radius = 2; } }
              }
            </Border>
            """;

        GeneratorRunResult result = RunGenerator(
            "PrismBackdropOrder.cui.xml",
            markup,
            out _);

        AssertPrismDiagnostic(result, "PRISM2007", markup, "AfterBackdrop");
    }

    [Fact]
    public void PrismBinderRejectsClipToBelowWithoutLowerBase()
    {
        const string markup = """
            <Border>
              @prism
              {
                @layer Surface
                {
                  ClipToBelow = true;
                  @filter Blur { Radius = 2; }
                }
              }
            </Border>
            """;

        GeneratorRunResult result = RunGenerator(
            "PrismClipToBelow.cui.xml",
            markup,
            out _);

        AssertPrismDiagnostic(result, "PRISM2008", markup, "ClipToBelow");
    }

    [Fact]
    public void PrismBinderResolvesResourceScopeFromInsideATemplate()
    {
        const string markup = """
            <Button>
              <Button.Resources>
                <PrismComposition Name="TemplateFx">
                  @parameter Radius: float = 4;
                  @layer Glow
                  {
                    @filter Blur { Radius = Radius; }
                  }
                </PrismComposition>
              </Button.Resources>
              @template
              {
                <Border>
                  @prism $TemplateFx(Radius = 8);
                </Border>
              }
            </Button>
            """;

        GeneratorRunResult result = RunGenerator(
            "PrismTemplateScope.cui.xml",
            markup,
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
    }

    [Fact]
    public void PrismBinderBindsTwoResourceApplicationsWithIndependentArguments()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <PrismComposition Name="ReusableGlow">
                  @group Effects
                  {
                    @parameter Radius: float = 4;
                    @layer Glow
                    {
                      @filter Blur { Radius = Radius; }
                    }
                  }
                </PrismComposition>
              </StackPanel.Resources>
              <Border>
                @prism $ReusableGlow(Effects.Radius = 8);
              </Border>
              <Border>
                @prism $ReusableGlow(Effects.Radius = 24);
              </Border>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator(
            "PrismIndependentArguments.cui.xml",
            markup,
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
    }

    [Fact]
    public void PrismEmissionSharesDefinitionAndCreatesIndependentTypedFactories()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <PrismComposition Name="ReusableGlow">
                  @parameter LayerOpacity: float = 0.5;
                  @group Effects
                  {
                    @parameter Radius: float = 4;
                    @layer Glow
                    {
                      Opacity = LayerOpacity;
                      @filter Blur { Radius = Radius; }
                    }
                  }
                </PrismComposition>
              </StackPanel.Resources>
              <Border>
                @prism $ReusableGlow(LayerOpacity = 0.2, Effects.Radius = 8);
              </Border>
              <Border>
                @prism $ReusableGlow(LayerOpacity = 0.8, Effects.Radius = 24);
              </Border>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator(
            "PrismTypedEmission.cui.xml",
            markup,
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Equal(
            1,
            Count(
                generated,
                "PrismCompositionDefinition __CernealaPrismDefinition"));
        Assert.Equal(
            2,
            Count(generated, "PrismInstance __CernealaCreatePrism"));
        Assert.Equal(2, Count(generated, "GeneratedMarkup.AttachPrism("));
        Assert.Contains(", 8f);", generated, StringComparison.Ordinal);
        Assert.Contains(", 24f);", generated, StringComparison.Ordinal);
        Assert.Contains("SetPrismFilterNumber(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Dictionary<string", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("ResolveSymbol", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("GetEntry(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("dynamic", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Reflection", generated, StringComparison.Ordinal);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        StackPanel panel = Assert.IsType<StackPanel>(
            InvokeCreate(stream, "Cerneala.GeneratedUi.PrismTypedEmissionFactory"));
        UIRoot root = new();
        root.VisualChildren.Add(panel);
        UIElement firstElement = panel.VisualChildren[0];
        UIElement secondElement = panel.VisualChildren[1];
        PrismInstance first = GeneratedMarkup.GetPrismInstance(firstElement);
        PrismInstance second = GeneratedMarkup.GetPrismInstance(secondElement);

        Assert.Same(first.Definition, second.Definition);
        Assert.NotSame(first, second);
        Assert.Equal(0.2f, first.GetLayerState(new PrismNodeId(2)).Opacity);
        Assert.Equal(0.8f, second.GetLayerState(new PrismNodeId(2)).Opacity);

        root.VisualChildren.Remove(panel);
        Assert.False(GeneratedMarkup.TryGetPrismInstance(firstElement, out _));
        Assert.False(GeneratedMarkup.TryGetPrismInstance(secondElement, out _));
    }

    [Fact]
    public void PrismEmissionRegistersTemplateInstancesWithLifecycleCleanup()
    {
        const string markup = """
            <Button>
              <Button.Resources>
                <PrismComposition Name="TemplateFx">
                  @layer Glow
                  {
                    @filter Blur { Radius = 8; }
                  }
                </PrismComposition>
              </Button.Resources>
              @template
              {
                <Border>
                  @prism $TemplateFx;
                </Border>
              }
            </Button>
            """;

        GeneratorRunResult result = RunGenerator(
            "PrismTemplateEmission.cui.xml",
            markup,
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Equal(1, Count(generated, "GeneratedMarkup.AttachPrism("));
        Assert.Equal(
            1,
            Count(
                generated,
                "PrismCompositionDefinition __CernealaPrismDefinition"));
        Assert.Contains("PrismInstance __CernealaCreatePrism", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void PrismBinderRejectsCatalogDomainViolationsAtTheValue()
    {
        const string markup = """
            <Border>
              @prism
              {
                @layer Surface
                {
                  Opacity = 2;
                  @filter Blur { Radius = 4; }
                }
              }
            </Border>
            """;

        GeneratorRunResult result = RunGenerator(
            "PrismInvalidDomain.cui.xml",
            markup,
            out _);

        AssertPrismDiagnostic(result, "PRISM2009", markup, "2");
    }

    [Fact]
    public void PrismBinderRejectsEmptyLayersWithoutCascading()
    {
        const string markup = """
            <Border>
              @prism
              {
                @layer Empty
                {
                  Opacity = 0.5;
                }
              }
            </Border>
            """;

        GeneratorRunResult result = RunGenerator(
            "PrismEmptyLayer.cui.xml",
            markup,
            out _);

        AssertPrismDiagnostic(result, "PRISM2013", markup, "Empty");
    }

    [Fact]
    public void PrismMotionSelfOwnerAndNamedPathsCompileToStaticAccess()
    {
        MarkupFile[] files =
        [
            new("PrismMotionSelf.cui.xml", PrismMotionSelfMarkup("$self.prism.Glow.Opacity")),
            new("PrismMotionOwner.cui.xml", PrismMotionOwnerMarkup("$owner.prism.Glow.Opacity")),
            new("PrismMotionNamed.cui.xml", PrismMotionNamedMarkup("$Card.prism.Glow.Opacity"))
        ];

        GeneratorRunResult result = RunGenerator(files, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = string.Join(
            Environment.NewLine,
            result.GeneratedSources.Select(source => source.SourceText.ToString()));
        Assert.Contains("StartPrismMotionProperty(", generated, StringComparison.Ordinal);
        Assert.Contains("static prismInstance =>", generated, StringComparison.Ordinal);
        Assert.Contains(
            "new global::Cerneala.UI.Prism.Definitions.PrismNodeId(",
            generated,
            StringComparison.Ordinal);
        Assert.DoesNotContain("FindName(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("GetEntry(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("ResolveSymbol", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("GetProperty(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("dynamic", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void PrismMotionScopedNumberAndColorParametersEmitTypedCatalogBridges()
    {
        const string markup = """
            <Border>
              @prism
              {
                @layer Glow
                {
                  @parameter Radius: float = 4;
                  @parameter Tint: color = #FF203040;
                  @filter Blur { Radius = Radius; }
                  @style OuterGlow { Color = Tint; }
                }
              }
              <Border.Aspect>
                <Aspect>
                  @on Loaded
                  {
                    @animate with Tween(120ms, Linear)
                    {
                      @to
                      {
                        $self.prism.Glow.Radius = 12;
                        $self.prism.Glow.Tint = #FF607080;
                      }
                    }
                  }
                </Aspect>
              </Border.Aspect>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator(
            "PrismMotionTypedParameters.cui.xml",
            markup,
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("GetPrismFilterNumber(", generated, StringComparison.Ordinal);
        Assert.Contains("SetPrismFilterNumber(", generated, StringComparison.Ordinal);
        Assert.Contains("GetPrismStyleColor(", generated, StringComparison.Ordinal);
        Assert.Contains("SetPrismStyleColor(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Dictionary<string", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("GetEntry(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("dynamic", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Reflection", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void PrismMotionBooleanAndEnumUseDiscreteWritesAndSetUsesStaticAccess()
    {
        const string markup = """
            <Border>
              @prism
              {
                @layer Glow
                {
                  Visible = true;
                  BlendMode = Normal;
                  @filter Blur { Radius = 4; }
                }
              }
              <Border.Aspect>
                <Aspect>
                  @on Loaded
                  {
                    @sequence
                    {
                      @animate with Tween(100ms)
                      {
                        @to { $self.prism.Glow.Visible = false; }
                      }
                      @animate with Tween(100ms)
                      {
                        @to { $self.prism.Glow.BlendMode = Screen; }
                      }
                      @set { $self.prism.Glow.Visible = true; }
                    }
                  }
                </Aspect>
              </Border.Aspect>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator(
            "PrismMotionDiscrete.cui.xml",
            markup,
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Equal(2, Count(generated, "StartPrismMotionProperty("));
        Assert.Contains("SetPrismMotionProperty(", generated, StringComparison.Ordinal);
        Assert.Contains(
            "global::Cerneala.Drawing.Prism.Catalog.PrismBlendMode.Screen",
            generated,
            StringComparison.Ordinal);
        Assert.Contains(
            "static (prismInstance, prismValue) =>",
            generated,
            StringComparison.Ordinal);
        Assert.DoesNotContain("Reflection", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("dynamic", generated, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("$Missing.prism.Glow.Opacity", "$Missing", "PRISM2010")]
    [InlineData("$self.prism.MissingLayer.Opacity", "MissingLayer", "PRISM2011")]
    [InlineData("$self.prism.Glow.MissingProperty", "MissingProperty", "PRISM2012")]
    public void PrismMotionInvalidPathsReportOneBindingDiagnosticAtTheExactSegment(
        string path,
        string token,
        string expectedId)
    {
        string markup = PrismMotionSelfMarkup(path);

        GeneratorRunResult result = RunGenerator(
            "PrismMotionInvalid.cui.xml",
            markup,
            out _);

        AssertPrismDiagnostic(result, expectedId, markup, token);
    }

    private static string PrismMotionSelfMarkup(string target)
    {
        return $$"""
            <Border>
              @prism
              {
                @layer Glow { @filter Blur { Radius = 4; } }
              }
              <Border.Aspect>
                <Aspect>
                  @on Loaded
                  {
                    @animate
                    {
                      @to { {{target}} = 0.5; }
                    }
                  }
                </Aspect>
              </Border.Aspect>
            </Border>
            """;
    }

    private static string PrismMotionOwnerMarkup(string target)
    {
        return $$"""
            <Button>
              <Button.Resources>
                <Aspect Name="TemplateMotion" TargetType="Border">
                  @on Loaded
                  {
                    @animate
                    {
                      @to { {{target}} = 0.5; }
                    }
                  }
                </Aspect>
              </Button.Resources>
              @prism
              {
                @layer Glow { @filter Blur { Radius = 4; } }
              }
              @template
              {
                <Border Aspect="$TemplateMotion" />
              }
            </Button>
            """;
    }

    private static string PrismMotionNamedMarkup(string target)
    {
        return $$"""
            <StackPanel>
              <Border Name="Card">
                @prism
                {
                  @layer Glow { @filter Blur { Radius = 4; } }
                }
              </Border>
              <Border>
                <Border.Aspect>
                  <Aspect>
                    @on Loaded
                    {
                      @animate
                      {
                        @to { {{target}} = 0.5; }
                      }
                    }
                  </Aspect>
                </Border.Aspect>
              </Border>
            </StackPanel>
            """;
    }

    private static void AssertPrismParseDiagnosticSnapshot(
        GeneratorRunResult result,
        string expectedId,
        string markup,
        string expectedToken,
        string expectedMessage)
    {
        Diagnostic diagnostic = AssertPrismDiagnostic(
            result,
            expectedId,
            markup,
            expectedToken);
        int expectedStart = markup.IndexOf(expectedToken, StringComparison.Ordinal);
        TextSpan expectedSpan = new(expectedStart, expectedToken.Length);
        Assert.Equal(expectedMessage, diagnostic.GetMessage());
        Assert.Equal(expectedSpan, diagnostic.Location.SourceSpan);
        Assert.Equal(
            SourceText.From(markup).Lines.GetLinePositionSpan(expectedSpan),
            diagnostic.Location.GetLineSpan().Span);
    }

    private static Diagnostic AssertPrismDiagnostic(
        GeneratorRunResult result,
        string expectedId,
        string markup,
        string expectedToken)
    {
        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics.Where(candidate => candidate.Severity == DiagnosticSeverity.Error));
        Assert.Equal(expectedId, diagnostic.Id);
        Assert.Equal(
            expectedToken,
            markup.Substring(
                diagnostic.Location.SourceSpan.Start,
                diagnostic.Location.SourceSpan.Length));
        return diagnostic;
    }
}
