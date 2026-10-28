using Cerneala.Language.Diagnostics;
using Cerneala.Language.Features;
using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Text;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cerneala.Tests.Language;

public sealed class MotionPrismSemanticTests
{
    private const string BindingViewModelSource = """
        using System.ComponentModel;

        namespace Demo;

        public sealed class BindingViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            public float SnapshotValue { get; set; }

            public float LiveValue { get; set; }
        }
        """;

    [Fact]
    public void MotionDirectReferencesAreSnapshotsAndOnlyExplicitModesAreBindings()
    {
        const string markup = """
            <Border DataType="Demo.BindingViewModel" Aspect="$Motion">
              <Border.Resources>
                <Aspect Name="Motion" TargetType="Border">
                  @on Loaded
                  {
                    @parallel
                    {
                      @animate { @to { Opacity = $DataContext.SnapshotValue; } }
                      @animate { @to { TranslateX = $DataContext.LiveValue:OneWay; } }
                    }
                  }
                </Aspect>
              </Border.Resources>
            </Border>
            """;

        CernealaSemanticModel model = Model("MotionBindingModes.crn", markup, BindingViewModelSource);

        Assert.Empty(model.Diagnostics.Where(IsMotionOrPrism));
        Assert.Equal(2, model.Symbols.Count(symbol => symbol.Kind == CernealaSemanticSymbolKind.BindingSegment));
        Assert.Single(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.BindingMode && symbol.Name == "OneWay");
    }

    [Fact]
    public void AspectDirectReferencesAreSnapshotsAndOnlyExplicitModesAreBindings()
    {
        const string markup = """
            <Border DataType="Demo.BindingViewModel" Aspect="$State">
              <Border.Resources>
                <Aspect Name="State" TargetType="Border">
                  @default
                  {
                    Opacity = $DataContext.SnapshotValue;
                    TranslateX = $DataContext.LiveValue:OneWay;
                  }
                </Aspect>
              </Border.Resources>
            </Border>
            """;

        CernealaSemanticModel model = Model("AspectBindingModes.crn", markup, BindingViewModelSource);

        Assert.DoesNotContain(model.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI004" || diagnostic.Id == "CERNEALAUI007");
        Assert.Equal(2, model.Symbols.Count(symbol => symbol.Kind == CernealaSemanticSymbolKind.BindingSegment));
        Assert.Single(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.BindingMode && symbol.Name == "OneWay");
    }

    [Fact]
    public void PrismDirectReferencesAreSnapshotsAndOnlyExplicitModesAreBindings()
    {
        const string markup = """
            <Border DataType="Demo.BindingViewModel">
              <Border.Aspect>
                @prism
                {
                  @layer Card
                  {
                    Opacity = $DataContext.SnapshotValue;
                    @filter Blur { Radius = $DataContext.LiveValue:OneWay; }
                  }
                }
              </Border.Aspect>
            </Border>
            """;

        CernealaSemanticModel model = Model("PrismBindingModes.crn", markup, BindingViewModelSource);

        Assert.Empty(model.Diagnostics.Where(IsMotionOrPrism));
        Assert.Equal(2, model.Symbols.Count(symbol => symbol.Kind == CernealaSemanticSymbolKind.BindingSegment));
        Assert.Single(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.BindingMode && symbol.Name == "OneWay");
    }

    [Fact]
    public void MotionTargetsEventsSpecsCompositionsParametersAndLifecycleAreTyped()
    {
        const string markup = """
            <Border Aspect="$Motion">
              <Border.Resources>
                <Tween Name="Quick" Duration="120ms" Easing="EaseOut" />
                <MotionClip Name="Fade" TargetType="Border">
                  @parameter Destination: float = 0.5;
                  @animate with $Quick { @to { Opacity = Destination; } }
                </MotionClip>
                <Aspect Name="Motion" TargetType="Border">
                  @handle Active;
                  @on Loaded { @run $Fade(Destination = 0.8) as Active; }
                  @on Unloaded { @cancel Active; }
                  @presence { enter = Tween(100ms); exit = Tween(100ms); }
                </Aspect>
              </Border.Resources>
            </Border>
            """;

        CernealaSemanticModel model = Model("TypedMotion.crn", markup);

        Assert.Empty(model.Diagnostics.Where(IsMotionOrPrism));
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.MotionEvent && symbol.Name == "Loaded" && symbol.MemberSymbol is not null);
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.MotionProperty && symbol.Name == "Opacity" && symbol.ValueType is "float" or "System.Single");
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.MotionParameter && symbol.Name == "Destination" && symbol.TypeSymbol is not null);
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.MotionSpec && symbol.Name == "Quick");
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.MotionLifecycle && symbol.Name == "@presence");
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.MotionHandle && symbol.DefinitionLocation is not null);
    }

    [Theory]
    [InlineData("@on Loaded { @run $SlideIn(Label = \"save as draft\"); }", null)]
    [InlineData("@handle Active; @on Loaded { @run $SlideIn(Label = \"save as draft\") as Active; }", "Active")]
    [InlineData("@handle Active; @on Loaded { @run $SlideIn(Label = \"C:\\\\\") as Active; }", "Active")]
    public void MotionRunStringArgumentContainingTheWordAsIsNotAHandleSeparator(string aspectBody, string? handle)
    {
        string markup = $$"""
            <Border Aspect="$Entrance">
              <Border.Resources>
                <MotionClip Name="SlideIn" TargetType="Border">
                  @parameter Label: string = "default";
                  @animate { debugName = Label; @to { TranslateY = 0; } }
                </MotionClip>
                <Aspect Name="Entrance" TargetType="Border">
                  {{aspectBody}}
                </Aspect>
              </Border.Resources>
            </Border>
            """;

        CernealaSemanticModel model = Model("MotionRunStringArgument.crn", markup);

        Assert.Empty(model.Diagnostics.Where(IsMotionOrPrism));
        Assert.Equal(
            handle is null ? Array.Empty<string>() : new[] { handle, handle },
            model.Symbols.Where(symbol => symbol.Kind == CernealaSemanticSymbolKind.MotionHandle).Select(symbol => symbol.Name));
    }

    [Theory]
    [InlineData("@on Loaded { @animate { @from { Opacity = 0; } } }", "CERNEALAUI020")]
    [InlineData("@on Loaded { @parallel { } }", "CERNEALAUI024")]
    [InlineData("@presence { enter = Tween(100ms); exit = Tween(100ms); } @presence { enter = Tween(100ms); exit = Tween(100ms); }", "CERNEALAUI025")]
    [InlineData("@on Loaded { @animate with Decay(1) { @to { Opacity = 1; } } }", "CERNEALAUI026")]
    [InlineData("@on Loaded { @animate { @to { DoesNotExist = 1; } } }", "CERNEALAUI021")]
    [InlineData("@on Missing { @animate { @to { Opacity = 1; } } }", "CERNEALAUI022")]
    [InlineData("@on Loaded { @animate with $Missing { @to { Opacity = 1; } } }", "CERNEALAUI023")]
    [InlineData("@on Loaded { @animate { @from { Opacity = $self.Opacity:OneWay; } @to { Opacity = 1; } } }", "CERNEALAUI023")]
    public void MotionDiagnosticCategoriesMatchTheSourceGenerator(string body, string expectedId)
    {
        string markup = MotionAspectMarkup(body);
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("MotionParity.crn", markup);

        HarnessDiagnostic semantic = Assert.Single(result.SemanticDiagnostics, diagnostic => diagnostic.Id == expectedId);
        HarnessDiagnostic sourceGenerator = Assert.Single(result.SourceGeneratorDiagnostics, diagnostic => diagnostic.Id == expectedId);
        Assert.Equal(sourceGenerator, semantic);
    }

    // Issue #1165: the editor reports the @if shape errors of the build, with the same message and span.
    [Theory]
    [InlineData("<Border Aspect=\"$Motion\"><Border.Resources><Aspect Name=\"Motion\" TargetType=\"Border\">@when IsMouseOver { @if IsMouseOver { Opacity = 1; } }</Aspect></Border.Resources></Border>")]
    [InlineData("<TextBlock Text=\"Idle\">@when IsMouseOver { @if IsMouseOver { Text = \"Active\"; } }</TextBlock>")]
    [InlineData("<TextBlock FontSize=\"12\">@when FontSize { @if 13 > value { Text = \"lt\"; } }</TextBlock>")]
    public void IfConditionShapeErrorsMatchTheSourceGenerator(string markup)
    {
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("IfParity.crn", markup);

        HarnessDiagnostic sourceGenerator = Assert.Single(result.SourceGeneratorDiagnostics, diagnostic => diagnostic.Id == "CERNEALAUI007");
        HarnessDiagnostic semantic = Assert.Single(result.SemanticDiagnostics, diagnostic => diagnostic.Id == "CERNEALAUI007");
        Assert.Equal(sourceGenerator, semantic);
    }

    [Theory]
    [InlineData("<Border Aspect=\"$Motion\"><Border.Resources><Aspect Name=\"Motion\" TargetType=\"Border\">@when IsMouseOver { @if IsMouseOver == true { Opacity = 1; } }</Aspect></Border.Resources></Border>")]
    [InlineData("<TextBlock FontSize=\"12\">@when FontSize { @if value > 11 { Text = \"gt\"; } }</TextBlock>")]
    public void ValidIfConditionsReportNothingInEditorAndBuild(string markup)
    {
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("IfParity.crn", markup);

        Assert.Empty(result.SourceGeneratorDiagnostics.Where(diagnostic => diagnostic.Severity == nameof(DiagnosticSeverity.Error)));
        Assert.Empty(result.SemanticDiagnostics.Where(diagnostic => diagnostic.Severity == nameof(DiagnosticSeverity.Error)));
    }

    [Theory]
    [InlineData("@on Loaded { @animate { @to { Opacity = 1; } } }")]
    [InlineData("@cancel Active;")]
    [InlineData("@handle Active;")]
    public void MotionClipBodyShapeErrorsUseInvalidDirectiveInEditorAndBuild(string body)
    {
        string markup = $$"""
            <Border>
              <Border.Resources>
                <MotionClip Name="Invalid" TargetType="Border">{{body}}</MotionClip>
              </Border.Resources>
            </Border>
            """;
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("MotionClipBodyShape.crn", markup);

        string[] semantic = result.SemanticDiagnostics
            .Where(diagnostic => diagnostic.Severity == nameof(DiagnosticSeverity.Error))
            .Select(diagnostic => diagnostic.Id)
            .ToArray();
        string[] sourceGenerator = result.SourceGeneratorDiagnostics
            .Where(diagnostic => diagnostic.Severity == nameof(DiagnosticSeverity.Error))
            .Select(diagnostic => diagnostic.Id)
            .ToArray();
        Assert.True(
            semantic.Length > 0 && semantic.All(id => id == "CERNEALAUI006") && semantic.SequenceEqual(sourceGenerator),
            $"Body '{body}': editor [{string.Join(", ", semantic)}], build [{string.Join(", ", sourceGenerator)}] (expected only CERNEALAUI006 in both).");
    }

    [Fact]
    public void PrismCatalogParametersNodesOperationsValuesAndApplicationsAreBound()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <PrismClip Name="CardFx">
                  @parameter GlowRadius: float = 18;
                  @layer Foreground
                  {
                    Opacity = 0.9;
                    @filter Blur { Radius = GlowRadius; }
                  }
                </PrismClip>
              </StackPanel.Resources>
              <Border>
                <Border.Aspect>@prism $CardFx(GlowRadius = 24);</Border.Aspect>
              </Border>
            </StackPanel>
            """;

        CernealaSemanticModel model = Model("TypedPrism.crn", markup);

        Assert.Empty(model.Diagnostics.Where(IsMotionOrPrism));
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.PrismClip && symbol.Name == "CardFx");
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.PrismParameter && symbol.Name == "GlowRadius" && symbol.TypeSymbol is not null);
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.PrismNode && symbol.Name == "Foreground");
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.PrismOperation && symbol.Name == "Blur");
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.PrismProperty && symbol.Name == "Radius" && symbol.TypeSymbol is not null);
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.ResourceReference && symbol.Name == "CardFx" && symbol.DefinitionLocation is not null);
    }

    [Theory]
    [InlineData("@layer Card { Unknown = 1; }", "PRISM2001")]
    [InlineData("@layer Card { @filter Missing { } }", "PRISM2002")]
    [InlineData("@layer Card { } @layer Card { }", "PRISM2003")]
    [InlineData("@parameter Value: object; @layer Card { }", "PRISM2004")]
    [InlineData("@layer Card { @layer Child { } }", "PRISM2005")]
    [InlineData("@layer Card { ClipToBelow = true; @filter Blur { Radius = 2; } }", "PRISM2008")]
    [InlineData("@layer Card { Opacity = 2; }", "PRISM2009")]
    [InlineData("@layer Card { @filter AdaptiveWideAngle { FocalLength = (0, 0); } }", "PRISM2009")]
    [InlineData("@layer Card { ThisLayerRange = (0, 1); @filter Blur { Radius = 1; } }", "PRISM2009")]
    [InlineData("@layer Card { UnderlyingRange = (0, 0.5, 1); @filter Blur { Radius = 1; } }", "PRISM2009")]
    [InlineData("", "PRISM2013")]
    public void PrismBindingDiagnosticsMatchTheSourceGeneratorExactly(string body, string expectedId)
    {
        string markup = "<StackPanel><StackPanel.Resources><PrismClip Name=\"Fx\">" + body +
            "</PrismClip></StackPanel.Resources></StackPanel>";
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("PrismDiagnostics.crn", markup);

        HarnessDiagnostic semantic = Assert.Single(result.SemanticDiagnostics, diagnostic => diagnostic.Id == expectedId);
        HarnessDiagnostic sourceGenerator = Assert.Single(result.SourceGeneratorDiagnostics, diagnostic => diagnostic.Id == expectedId);
        Assert.Equal(sourceGenerator, semantic);
    }

    [Theory]
    [InlineData("@parameter Range: vector = (0, 1); @layer Card { ThisLayerRange = Range; @filter Blur { Radius = 1; } }", "", "(0, 1)")]
    [InlineData("@parameter Outer: vector = (0, 0.5, 1); @layer Card { @parameter Inner: vector = Outer; UnderlyingRange = Inner; @filter Blur { Radius = 1; } }", "", "(0, 0.5, 1)")]
    [InlineData("@parameter Range: vector = (0, 1); @layer A { ThisLayerRange = Range; @filter Blur { Radius = 1; } } @layer B { ThisLayerRange = Range; @filter Blur { Radius = 1; } }", "", "(0, 1)")]
    [InlineData("@parameter Range: vector = (0, 0, 1, 1); @layer Card { ThisLayerRange = Range; @filter Blur { Radius = 1; } }", "Range = (0, 1)", "(0, 1)")]
    [InlineData("@parameter Outer: vector = (0, 0, 1, 1); @layer Card { @parameter Inner: vector = Outer; UnderlyingRange = Inner; @filter Blur { Radius = 1; } }", "Outer = (0, 0.5, 1)", "(0, 0.5, 1)")]
    public void PrismBlendRangeParameterValuesMatchTheSourceGeneratorExactly(string body, string arguments, string invalid)
    {
        string markup = "<StackPanel><StackPanel.Resources><PrismClip Name=\"Fx\">" + body +
            "</PrismClip></StackPanel.Resources><Border><Border.Aspect>@prism $Fx(" + arguments +
            ");</Border.Aspect></Border></StackPanel>";
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("PrismDiagnostics.crn", markup);

        HarnessDiagnostic semantic = Assert.Single(result.SemanticDiagnostics);
        Assert.Equal("PRISM2009", semantic.Id);
        Assert.Equal(invalid, markup.Substring(semantic.StartCharacter, semantic.EndCharacter - semantic.StartCharacter));
        Assert.EndsWith("value '" + invalid + "' must have four components.", semantic.Message, StringComparison.Ordinal);
        Assert.Equal(semantic, Assert.Single(result.SourceGeneratorDiagnostics));
    }

    [Theory]
    [InlineData("@parameter R: vector = R; @layer Card { @filter AdaptiveWideAngle { FocalLength = R; } }", "Value 'R' is not a valid vector Prism value.")]
    [InlineData("@parameter R: color = #FF0000; @layer Card { @parameter R: vector = R; @filter AdaptiveWideAngle { FocalLength = R; } }", "Prism parameter 'R' has type color, not vector.")]
    public void PrismParameterDefaultsResolveBeforeTheirOwnParameterLikeTheSourceGenerator(string body, string message)
    {
        string markup = "<StackPanel><StackPanel.Resources><PrismClip Name=\"Fx\">" + body +
            "</PrismClip></StackPanel.Resources><Border><Border.Aspect>@prism $Fx();</Border.Aspect></Border></StackPanel>";
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("PrismDiagnostics.crn", markup);

        HarnessDiagnostic semantic = Assert.Single(result.SemanticDiagnostics);
        Assert.Equal("PRISM2009", semantic.Id);
        Assert.Equal(markup.IndexOf("vector = R;", StringComparison.Ordinal) + "vector = ".Length, semantic.StartCharacter);
        Assert.Equal(semantic.StartCharacter + 1, semantic.EndCharacter);
        Assert.EndsWith(message, semantic.Message, StringComparison.Ordinal);
        Assert.Equal(semantic, Assert.Single(result.SourceGeneratorDiagnostics));
    }

    [Fact]
    public void PrismParameterDefaultReadsTheShadowedOuterParameter()
    {
        string markup = "<StackPanel><StackPanel.Resources><PrismClip Name=\"Fx\">" +
            "@parameter R: vector = (2, 2); @layer Card { @parameter R: vector = R; @filter AdaptiveWideAngle { FocalLength = R; } }" +
            "</PrismClip></StackPanel.Resources><Border><Border.Aspect>@prism $Fx();</Border.Aspect></Border></StackPanel>";
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("PrismDiagnostics.crn", markup);

        Assert.Empty(result.SemanticDiagnostics);
        Assert.Empty(result.SourceGeneratorDiagnostics);
    }

    [Fact]
    public void PrismVectorParametersNamedLikeBlendRangesAreOrdinaryVectors()
    {
        string markup = "<StackPanel><StackPanel.Resources><PrismClip Name=\"Fx\">@parameter ThisLayerRange: vector = (2, 2); " +
            "@layer Card { @filter AdaptiveWideAngle { FocalLength = ThisLayerRange; } }</PrismClip></StackPanel.Resources>" +
            "<Border><Border.Aspect>@prism $Fx(ThisLayerRange = (3, 3));</Border.Aspect></Border></StackPanel>";
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("PrismDiagnostics.crn", markup);

        Assert.Empty(result.SemanticDiagnostics);
    }

    // A value that is not a vector at all reports only that error, and an
    // argument that fails to bind still hides the outer default it replaces.
    [Theory]
    [InlineData("@parameter Range: vector = (0, 0, 1, 1, 1); @layer Card { ThisLayerRange = Range; @filter Blur { Radius = 1; } }", "", "(0, 0, 1, 1, 1)")]
    [InlineData("@parameter Outer: vector = (0, 0, 1, 1); @layer Card { @parameter Inner: vector = Outer; ThisLayerRange = Inner; @filter Blur { Radius = 1; } }", "Card.Inner = 3, Outer = (0, 1)", "3")]
    public void PrismInvalidBlendRangeParameterValuesReportOnlyTheirOwnError(string body, string arguments, string invalid)
    {
        string markup = "<StackPanel><StackPanel.Resources><PrismClip Name=\"Fx\">" + body +
            "</PrismClip></StackPanel.Resources><Border><Border.Aspect>@prism $Fx(" + arguments +
            ");</Border.Aspect></Border></StackPanel>";
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("PrismDiagnostics.crn", markup);

        HarnessDiagnostic semantic = Assert.Single(result.SemanticDiagnostics);
        Assert.Equal(invalid, markup.Substring(semantic.StartCharacter, semantic.EndCharacter - semantic.StartCharacter));
        Assert.EndsWith("Value '" + invalid + "' is not a valid vector Prism value.", semantic.Message, StringComparison.Ordinal);
    }

    // A default that fails to bind reads no outer parameter, so an argument
    // for the outer parameter never reaches the inner blend range.
    [Fact]
    public void PrismBlendRangeIgnoresAWrongTypeDefaultLink()
    {
        string markup = "<StackPanel><StackPanel.Resources><PrismClip Name=\"Fx\">" +
            "@parameter R: color = #FF0000; @layer Card { @parameter R: vector = R; ThisLayerRange = R; @filter Blur { Radius = 1; } }" +
            "</PrismClip></StackPanel.Resources><Border><Border.Aspect>@prism $Fx(R = (0, 1));</Border.Aspect></Border></StackPanel>";
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("PrismDiagnostics.crn", markup);

        Assert.Collection(
            result.SemanticDiagnostics,
            diagnostic => Assert.EndsWith("Prism parameter 'R' has type color, not vector.", diagnostic.Message, StringComparison.Ordinal),
            diagnostic =>
            {
                Assert.Equal("(0, 1)", markup.Substring(diagnostic.StartCharacter, diagnostic.EndCharacter - diagnostic.StartCharacter));
                Assert.EndsWith("Value '(0, 1)' is not a valid color Prism value.", diagnostic.Message, StringComparison.Ordinal);
            });
        Assert.Equal(result.SemanticDiagnostics, result.SourceGeneratorDiagnostics);
    }

    [Theory]
    [InlineData("ThisLayerRange")]
    [InlineData("UnderlyingRange")]
    public void PrismBlendRangesWithFourComponentsHaveNoDiagnostics(string property)
    {
        string markup = "<StackPanel><StackPanel.Resources><PrismClip Name=\"Fx\">@layer Card { " + property +
            " = (0, 0, 1, 1); @filter Blur { Radius = 1; } }</PrismClip></StackPanel.Resources></StackPanel>";
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("PrismDiagnostics.crn", markup);

        Assert.Empty(result.SemanticDiagnostics);
        Assert.Empty(result.SourceGeneratorDiagnostics);
    }

    [Fact]
    public void PrismMotionInteropResolvesTheAppliedNodeAndTypedProperty()
    {
        const string markup = """
            <Border Aspect="$Motion">
              <Border.Resources>
                <Aspect Name="Motion" TargetType="Border">
                  @prism { @layer Card { Opacity = 1; } }
                  @on Loaded { @animate { @to { $self.prism.Card.Opacity = 0.5; } } }
                </Aspect>
              </Border.Resources>
            </Border>
            """;

        CernealaSemanticModel model = Model("PrismMotion.crn", markup);

        Assert.Empty(model.Diagnostics.Where(IsMotionOrPrism));
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.MotionProperty && symbol.Name == "Opacity" && symbol.TypeSymbol is not null);
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.PrismNode && symbol.Name == "Card" && symbol.DefinitionLocation is not null);
    }

    [Fact]
    public void EditorFactsExposeKeywordsArgumentTypesAndEnumLikeValues()
    {
        Assert.Contains("@animate", CernealaLanguageFacts.MotionDirectiveKeywords);
        Assert.Contains("@prism", CernealaLanguageFacts.PrismDirectiveKeywords);
        Assert.DoesNotContain("@backdrop", CernealaLanguageFacts.PrismDirectiveKeywords);
        Assert.Contains(CernealaLanguageFacts.MotionOptions, option => option.Name == "retarget" && option.AllowedValues.Contains("PreserveProgress"));
        Assert.Contains(CernealaLanguageFacts.GetPrismSymbols("filter"), symbol => symbol == "Blur");
        Assert.Contains(CernealaLanguageFacts.GetPrismProperties("layer"), property =>
            property.Name == "BlendMode" && property.ValueType == "symbol" && property.AllowedValues.Contains("Screen"));
    }

    [Fact]
    public void IncompletePrismKeepsUnaffectedXmlSemantics()
    {
        const string markup = """
            <StackPanel>
              <Border>
                <Border.Aspect>@prism { @layer Fx { Opacity = 1;</Border.Aspect>
              </Border>
              <Button />
            </StackPanel>
            """;

        CernealaSemanticModel model = Model("IncompletePrism.crn", markup);

        Assert.Contains(model.Diagnostics, diagnostic => diagnostic.Id == "PRISM1002");
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.Element && symbol.Name == "Button");
    }

    [Fact]
    public void IncompleteMotionKeepsUnaffectedXmlSemantics()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <Aspect Name="Motion" TargetType="Border">
                  @on Loaded { @animate { @to { Opacity = 1;
                </Aspect>
              </StackPanel.Resources>
              <Button />
            </StackPanel>
            """;

        CernealaSemanticModel model = Model("IncompleteMotion.crn", markup);

        Assert.Contains(model.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI020");
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.Element && symbol.Name == "Button");
    }

    // docs/plans/2026-10-08-aspect-runtime-program.md, Etapa 1: `$Name` resolves
    // in the name scope where the Aspect is written, so a sibling is reachable
    // from an inline Aspect and from a resource Aspect alike.
    [Theory]
    [InlineData("""
        <StackPanel>
          <Border Name="Speaker" />
          <Button Content="Pause"><Button.Aspect>@on Click { @animate { @to { $Speaker.Opacity = 0.25; } } }</Button.Aspect></Button>
        </StackPanel>
        """)]
    [InlineData("""
        <StackPanel>
          <StackPanel.Resources>
            <Aspect Name="PauseMusic" TargetType="Button">@on Click { @animate { @to { $Speaker.Opacity = 0.25; } } }</Aspect>
          </StackPanel.Resources>
          <Border Name="Speaker" />
          <Button Content="Pause" Aspect="$PauseMusic" />
        </StackPanel>
        """)]
    public void NamedMotionTargetsResolveInTheDeclaringNameScope(string markup)
    {
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("DeclaringNameScope.crn", markup);

        Assert.Empty(result.SemanticDiagnostics);
        Assert.Empty(result.SourceGeneratorDiagnostics);
    }

    [Fact]
    public void ApplicationAspectCannotTargetANamedElement()
    {
        const string markup = """
            <Application>
              <Application.Resources>
                <Aspect Name="PauseMusic" TargetType="Button">@on Click { @animate { @to { $Speaker.Opacity = 0.25; } } }</Aspect>
              </Application.Resources>
            </Application>
            """;
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("App.crn", markup);

        Assert.Contains(result.SemanticDiagnostics, diagnostic => diagnostic.Id == "CERNEALAUI021");
    }

    [Fact]
    public void ValidMotionAndPrismCorpusHasZeroHostDivergence()
    {
        string[] documents =
        [
            MotionAspectMarkup("@on Loaded { @animate with Tween(100ms) { @to { Opacity = 1; } } }"),
            "<Border><Border.Aspect>@prism { @layer Card { Opacity = 0.8; @filter Blur { Radius = 8; } } }</Border.Aspect></Border>"
        ];

        foreach ((string document, int index) in documents.Select((document, index) => (document, index)))
        {
            LanguagePipelineResult result = LanguagePipelineHarness.Analyze("MotionPrism" + index + ".crn", document);
            Assert.Empty(result.SemanticDiagnostics);
            Assert.Empty(result.SourceGeneratorDiagnostics);
        }
    }

    [Fact]
    public void RealPresentationMotionAndPrismMarkupHasNoSemanticDivergences()
    {
        string repository = CorpusCatalog.RepositoryRoot();
        string presentation = Path.Combine(repository, "CernealaPresentation");
        SyntaxTree[] trees = Directory.EnumerateFiles(presentation, "*.cs", SearchOption.AllDirectories)
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path))
            .ToArray();
        CSharpCompilation compilation = CSharpCompilation.Create(
            "CernealaPresentation",
            trees,
            PlatformReferences().Append(MetadataReference.CreateFromFile(typeof(UIElement).Assembly.Location)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        CernealaDocument[] documents = Directory.EnumerateFiles(presentation, "*.crn", SearchOption.AllDirectories)
            .Select(path => new CernealaDocument(path, SourceText.From(File.ReadAllText(path))))
            .ToArray();
        using CernealaCompilation workspace = new(
            new RoslynCompilationSymbols(compilation),
            documents,
            AnalysisMode.Build);

        foreach (CernealaDocument document in documents)
        {
            CernealaSemanticModel model = workspace.GetSemanticModel(document.Path);
            Assert.DoesNotContain(model.Diagnostics, IsMotionOrPrism);
        }
    }

    private static bool IsMotionOrPrism(LanguageDiagnostic diagnostic) =>
        diagnostic.Id.StartsWith("CERNEALAUI02", StringComparison.Ordinal) ||
        diagnostic.Id.StartsWith("PRISM", StringComparison.Ordinal);

    [Theory]
    [InlineData("LinearSrgb")]
    [InlineData("Srgb")]
    [InlineData("DisplayP3")]
    [InlineData("InvalidProfile")]
    [InlineData("true")]
    [InlineData("12")]
    [InlineData("null")]
    [InlineData("(1, 2)")]
    public void PrismClipAttributeAndBlockValuesHaveTheSameSemanticResult(string value)
    {
        string attribute = "<Border><Border.Resources><PrismClip Name=\"Fx\" WorkingColorProfile=\"" +
            value + "\">@layer Card { }</PrismClip></Border.Resources></Border>";
        string block = "<Border><Border.Resources><PrismClip Name=\"Fx\">WorkingColorProfile = " +
            value + "; @layer Card { }</PrismClip></Border.Resources></Border>";
        CernealaSemanticModel attributeModel = Model("Attribute.crn", attribute);
        CernealaSemanticModel blockModel = Model("Block.crn", block);

        Assert.Equal(
            blockModel.Diagnostics.Select(diagnostic => diagnostic.Id),
            attributeModel.Diagnostics.Select(diagnostic => diagnostic.Id));
        Assert.Equal(
            blockModel.Symbols.Where(symbol => symbol.Kind == CernealaSemanticSymbolKind.PrismValue)
                .Select(symbol => (symbol.Name, symbol.ValueType, symbol.Value)),
            attributeModel.Symbols.Where(symbol => symbol.Kind == CernealaSemanticSymbolKind.PrismValue)
                .Select(symbol => (symbol.Name, symbol.ValueType, symbol.Value)));
    }

    private static string MotionAspectMarkup(string body) => $$"""
        <Border Aspect="$Motion">
          <Border.Resources>
            <Aspect Name="Motion" TargetType="Border">{{body}}</Aspect>
          </Border.Resources>
        </Border>
        """;

    private static CernealaSemanticModel Model(string path, string markup, string? source = null)
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            "MotionPrismSemanticTests",
            source is null ? null : [CSharpSyntaxTree.ParseText(source)],
            references: PlatformReferences()
                .Append(MetadataReference.CreateFromFile(typeof(UIElement).Assembly.Location)),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        CernealaDocument document = new(path, SourceText.From(markup));
        CernealaCompilation workspace = new(
            new RoslynCompilationSymbols(compilation),
            [document],
            AnalysisMode.Build);
        return workspace.GetSemanticModel(path);
    }

    private static IEnumerable<MetadataReference> PlatformReferences()
    {
        string trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");
        return trustedAssemblies.Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
    }
}
