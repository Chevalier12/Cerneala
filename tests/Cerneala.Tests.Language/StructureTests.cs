using Cerneala.Language.Diagnostics;
using Cerneala.Language.Features;
using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Text;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cerneala.Tests.Language;

public sealed class StructureTests
{
    private const string Markup = """
        <Window xmlns:ui="clr-namespace:Cerneala.UI.Controls;assembly=Cerneala" DataType="Fixture.ViewModel" Loaded="OnLoaded">
          <!--
            editor structure
          -->
          <Window.Resources>
            <SolidColorBrush Name="Accent" />
            <Tween Name="Quick" Duration="120ms" />
            <Aspect Name="Interactive" TargetType="Border">
              @when $DataContext.Enabled {
                Opacity = 1;
              }
              @on Loaded {
                @animate with $Quick { @to { Opacity = 0.5; } }
              }
            </Aspect>
            <PrismComposition Name="CardFx">
              @parameter Radius: float = 8;
              @layer Card { @filter Blur { Radius = 8; } }
            </PrismComposition>
          </Window.Resources>
          <Canvas>
            <Button Name="Action" Canvas.Left="12" Background="$Accent" Content="$DataContext.Title" />
          </Canvas>
        </Window>
        """;

    [Fact]
    public void MixedDocumentProducesNonOverlappingSemanticCategoriesAndNavigableSymbols()
    {
        using Fixture fixture = Fixture.Create(Markup);
        CernealaStructureService service = new();

        IReadOnlyList<CernealaSemanticToken> tokens = service.GetSemanticTokens(
            fixture.Document,
            fixture.Model);
        CernealaSemanticTokenKind[] missingKinds = Enum.GetValues<CernealaSemanticTokenKind>()
            .Except(tokens.Select(token => token.Kind))
            .ToArray();
        Assert.True(missingKinds.Length == 0, "Missing semantic token kinds: " + string.Join(", ", missingKinds));
        Assert.DoesNotContain(tokens.SelectMany((left, index) => tokens.Skip(index + 1)
            .Select(right => (left, right))), pair =>
            pair.left.Span.Start < pair.right.Span.End && pair.right.Span.Start < pair.left.Span.End);

        CernealaOutlineSymbol root = Assert.Single(service.GetDocumentSymbols(fixture.Document, fixture.Model));
        Assert.Equal(CernealaOutlineSymbolKind.Root, root.Kind);
        CernealaOutlineSymbol[] outline = Flatten(root).ToArray();
        Assert.Contains(outline, symbol => symbol.Kind == CernealaOutlineSymbolKind.ResourceGroup);
        Assert.Contains(outline, symbol => symbol.Name == "Action");
        Assert.Contains(outline, symbol => symbol.Name == "Interactive" && symbol.Kind == CernealaOutlineSymbolKind.Aspect);
        Assert.Contains(outline, symbol => symbol.Name == "Quick" && symbol.Kind == CernealaOutlineSymbolKind.Motion);
        Assert.Contains(outline, symbol => symbol.Name == "CardFx" && symbol.Kind == CernealaOutlineSymbolKind.Prism);

        IReadOnlyList<CernealaWorkspaceSymbol> workspace = service.GetWorkspaceSymbols([fixture.Model], string.Empty);
        Assert.Contains(workspace, symbol => symbol.Name == "Action");
        Assert.Contains(workspace, symbol => symbol.Name == "Accent");
        Assert.Contains(workspace, symbol => symbol.Name == "Interactive");
        Assert.Contains(workspace, symbol => symbol.Name == "Quick");
        Assert.Contains(workspace, symbol => symbol.Name == "CardFx");
        Assert.DoesNotContain(workspace, symbol => symbol.Name is "Title" or "12" or "0.5");
        Assert.All(service.GetWorkspaceSymbols([fixture.Model], "Card"), symbol =>
            Assert.Contains("Card", symbol.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FoldingSelectionAndExistingTokenKindsSurviveLocalRecoverableEdits()
    {
        using Fixture original = Fixture.Create(Markup);
        CernealaStructureService service = new();
        IReadOnlyList<CernealaFoldingRange> folding = service.GetFoldingRanges(original.Document);
        Assert.Contains(folding, range => range.Kind == "comment");
        Assert.Contains(folding, range => range.Kind == "region");
        Assert.Contains(folding, range => original.Document.Text.Substring(range.Span).Contains("Window.Resources", StringComparison.Ordinal));

        int title = Markup.IndexOf("Title", StringComparison.Ordinal) + 1;
        CernealaSelectionRange selection = service.GetSelectionRange(original.Document, original.Model, title);
        TextSpan[] chain = SelectionChain(selection).ToArray();
        Assert.True(chain.Length >= 4);
        Assert.All(chain.Zip(chain.Skip(1)), pair =>
            Assert.True(pair.Second.Start <= pair.First.Start && pair.Second.End >= pair.First.End));
        Assert.Equal(original.Document.Syntax.Span, chain[^1]);

        string editedMarkup = Markup.Replace(
            "<Button Name=\"Action\"",
            "<Button Bogus=\"1\" Name=\"Action\"",
            StringComparison.Ordinal);
        using Fixture edited = Fixture.Create(editedMarkup, version: 2);
        IReadOnlyList<CernealaSemanticToken> originalTokens = service.GetSemanticTokens(original.Document, original.Model);
        IReadOnlyList<CernealaSemanticToken> editedTokens = service.GetSemanticTokens(edited.Document, edited.Model);
        foreach (string stableText in new[] { "Window", "Interactive", "@when", "@on", "CardFx", "$DataContext", "Title" })
        {
            Assert.Equal(
                TokenKinds(original.Document, originalTokens, stableText),
                TokenKinds(edited.Document, editedTokens, stableText));
        }

        Assert.NotEmpty(service.GetDocumentSymbols(edited.Document, edited.Model));
        Assert.NotEmpty(service.GetFoldingRanges(edited.Document));
    }

    private static IEnumerable<CernealaOutlineSymbol> Flatten(CernealaOutlineSymbol symbol)
    {
        yield return symbol;
        foreach (CernealaOutlineSymbol child in symbol.Children.SelectMany(Flatten))
        {
            yield return child;
        }
    }

    private static IEnumerable<TextSpan> SelectionChain(CernealaSelectionRange selection)
    {
        for (CernealaSelectionRange? current = selection; current is not null; current = current.Parent)
        {
            yield return current.Span;
        }
    }

    private static CernealaSemanticTokenKind[] TokenKinds(
        CernealaDocument document,
        IReadOnlyList<CernealaSemanticToken> tokens,
        string text) => tokens
        .Where(token => document.Text.Substring(token.Span) == text)
        .Select(token => token.Kind)
        .OrderBy(kind => kind)
        .ToArray();

    private sealed class Fixture : IDisposable
    {
        private Fixture(CernealaDocument document, CernealaCompilation compilation)
        {
            Document = document;
            Compilation = compilation;
            Model = compilation.GetSemanticModel(document.Path);
        }

        public CernealaDocument Document { get; }

        public CernealaCompilation Compilation { get; }

        public CernealaSemanticModel Model { get; }

        public static Fixture Create(string markup, long version = 1)
        {
            CSharpCompilation roslyn = CSharpCompilation.Create(
                "StructureTests",
                [CSharpSyntaxTree.ParseText("""
                    using Cerneala.UI.Controls;
                    namespace Fixture;
                    public sealed partial class View : Window { public void OnLoaded() { } }
                    public sealed class ViewModel { public string Title { get; set; } = ""; public bool Enabled { get; set; } }
                    """)],
                PlatformReferences().Append(MetadataReference.CreateFromFile(typeof(UIElement).Assembly.Location)),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            CernealaDocument document = new("View.cui.xml", SourceText.From(markup, version));
            CernealaCompilation compilation = new(
                new RoslynCompilationSymbols(roslyn),
                [document],
                AnalysisMode.Editor);
            return new Fixture(document, compilation);
        }

        public void Dispose() => Compilation.Dispose();

        private static IEnumerable<MetadataReference> PlatformReferences()
        {
            string trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
                ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");
            return trustedAssemblies.Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path));
        }
    }
}
