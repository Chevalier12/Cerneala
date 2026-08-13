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

        CernealaSemanticModel model = Model("TypedMotion.cui.xml", markup);

        Assert.Empty(model.Diagnostics.Where(IsMotionOrPrism));
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.MotionEvent && symbol.Name == "Loaded" && symbol.MemberSymbol is not null);
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.MotionProperty && symbol.Name == "Opacity" && symbol.ValueType is "float" or "System.Single");
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.MotionParameter && symbol.Name == "Destination" && symbol.TypeSymbol is not null);
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.MotionSpec && symbol.Name == "Quick");
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.MotionLifecycle && symbol.Name == "@presence");
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.MotionHandle && symbol.DefinitionLocation is not null);
    }

    [Theory]
    [InlineData("@on Loaded { @animate { @from { Opacity = 0; } } }", "CERNEALAUI020")]
    [InlineData("@on Loaded { @parallel { } }", "CERNEALAUI024")]
    [InlineData("@presence { enter = Tween(100ms); exit = Tween(100ms); } @presence { enter = Tween(100ms); exit = Tween(100ms); }", "CERNEALAUI025")]
    [InlineData("@on Loaded { @animate with Decay(1) { @to { Opacity = 1; } } }", "CERNEALAUI026")]
    [InlineData("@on Loaded { @animate { @to { DoesNotExist = 1; } } }", "CERNEALAUI021")]
    [InlineData("@on Missing { @animate { @to { Opacity = 1; } } }", "CERNEALAUI022")]
    [InlineData("@on Loaded { @animate with $Missing { @to { Opacity = 1; } } }", "CERNEALAUI023")]
    public void MotionDiagnosticCategoriesMatchTheSourceGenerator(string body, string expectedId)
    {
        string markup = MotionAspectMarkup(body);
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("MotionParity.cui.xml", markup);

        HarnessDiagnostic semantic = Assert.Single(result.SemanticDiagnostics, diagnostic => diagnostic.Id == expectedId);
        HarnessDiagnostic sourceGenerator = Assert.Single(result.SourceGeneratorDiagnostics, diagnostic => diagnostic.Id == expectedId);
        Assert.Equal(sourceGenerator, semantic);
    }

    [Fact]
    public void PrismCatalogParametersNodesOperationsValuesAndApplicationsAreBound()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <PrismComposition Name="CardFx">
                  @parameter GlowRadius: float = 18;
                  @layer Foreground
                  {
                    Opacity = 0.9;
                    @filter Blur { Radius = GlowRadius; }
                  }
                </PrismComposition>
              </StackPanel.Resources>
              <Border>
                @prism $CardFx(GlowRadius = 24);
              </Border>
            </StackPanel>
            """;

        CernealaSemanticModel model = Model("TypedPrism.cui.xml", markup);

        Assert.Empty(model.Diagnostics.Where(IsMotionOrPrism));
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.PrismComposition && symbol.Name == "CardFx");
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
    [InlineData("@backdrop A { } @backdrop B { }", "PRISM2006")]
    [InlineData("@backdrop Glass { } @layer Card { }", "PRISM2007")]
    [InlineData("@layer Card { ClipToBelow = true; @filter Blur { Radius = 2; } }", "PRISM2008")]
    [InlineData("@layer Card { Opacity = 2; }", "PRISM2009")]
    [InlineData("", "PRISM2013")]
    public void PrismBindingDiagnosticsMatchTheSourceGeneratorExactly(string body, string expectedId)
    {
        string markup = "<StackPanel><StackPanel.Resources><PrismComposition Name=\"Fx\">" + body +
            "</PrismComposition></StackPanel.Resources></StackPanel>";
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("PrismDiagnostics.cui.xml", markup);

        HarnessDiagnostic semantic = Assert.Single(result.SemanticDiagnostics, diagnostic => diagnostic.Id == expectedId);
        HarnessDiagnostic sourceGenerator = Assert.Single(result.SourceGeneratorDiagnostics, diagnostic => diagnostic.Id == expectedId);
        Assert.Equal(sourceGenerator, semantic);
    }

    [Fact]
    public void PrismMotionInteropResolvesTheAppliedNodeAndTypedProperty()
    {
        const string markup = """
            <Border Aspect="$Motion">
              <Border.Resources>
                <Aspect Name="Motion" TargetType="Border">
                  @on Loaded { @animate { @to { $self.prism.Card.Opacity = 0.5; } } }
                </Aspect>
              </Border.Resources>
              @prism { @layer Card { Opacity = 1; } }
            </Border>
            """;

        CernealaSemanticModel model = Model("PrismMotion.cui.xml", markup);

        Assert.Empty(model.Diagnostics.Where(IsMotionOrPrism));
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.MotionProperty && symbol.Name == "Opacity" && symbol.TypeSymbol is not null);
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.PrismNode && symbol.Name == "Card" && symbol.DefinitionLocation is not null);
    }

    [Fact]
    public void EditorFactsExposeKeywordsArgumentTypesAndEnumLikeValues()
    {
        Assert.Contains("@animate", CernealaLanguageFacts.MotionDirectiveKeywords);
        Assert.Contains("@prism", CernealaLanguageFacts.PrismDirectiveKeywords);
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
                @prism { @layer Fx { Opacity = 1;
              </Border>
              <Button />
            </StackPanel>
            """;

        CernealaSemanticModel model = Model("IncompletePrism.cui.xml", markup);

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

        CernealaSemanticModel model = Model("IncompleteMotion.cui.xml", markup);

        Assert.Contains(model.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI020");
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.Element && symbol.Name == "Button");
    }

    [Fact]
    public void ValidMotionAndPrismCorpusHasZeroHostDivergence()
    {
        string[] documents =
        [
            MotionAspectMarkup("@on Loaded { @animate with Tween(100ms) { @to { Opacity = 1; } } }"),
            "<Border>@prism { @layer Card { Opacity = 0.8; @filter Blur { Radius = 8; } } }</Border>"
        ];

        foreach ((string document, int index) in documents.Select((document, index) => (document, index)))
        {
            LanguagePipelineResult result = LanguagePipelineHarness.Analyze("MotionPrism" + index + ".cui.xml", document);
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
            "CernealaPresentationSemanticMotionPrism",
            trees,
            PlatformReferences().Append(MetadataReference.CreateFromFile(typeof(UIElement).Assembly.Location)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        CernealaDocument[] documents = Directory.EnumerateFiles(presentation, "*.cui.xml", SearchOption.AllDirectories)
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

    private static string MotionAspectMarkup(string body) => $$"""
        <Border Aspect="$Motion">
          <Border.Resources>
            <Aspect Name="Motion" TargetType="Border">{{body}}</Aspect>
          </Border.Resources>
        </Border>
        """;

    private static CernealaSemanticModel Model(string path, string markup)
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            "MotionPrismSemanticTests",
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
