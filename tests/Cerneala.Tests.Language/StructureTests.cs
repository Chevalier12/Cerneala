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
              @when $DataContext.Enabled and Opacity > 0 {
                Opacity = 1;
              }
              @on Loaded {
                @animate with $Quick {
                  @from { $Action.Opacity = current; }
                  @to { Opacity = 0.5; }
                }
              }
            </Aspect>
            <PrismComposition Name="CardFx">
              @parameter Radius: float = 8;
              @layer Card { @filter Blur { Radius = 8; } }
            </PrismComposition>
          </Window.Resources>
          <Canvas>
            <Button Name="Action" Canvas.Left="12" Background="$Accent" Content="$DataContext.Title" Opacity="$Accent.Opacity" />
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
            .Where(kind => kind is not CernealaSemanticTokenKind.Label and not CernealaSemanticTokenKind.Type)
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

    [Fact]
    public void MotionReferencesAndPropertiesUseDistinctSemanticCategories()
    {
        using Fixture fixture = Fixture.Create(Markup);
        CernealaStructureService service = new();
        IReadOnlyList<CernealaSemanticToken> tokens = service.GetSemanticTokens(
            fixture.Document,
            fixture.Model);
        int referenceStart = Markup.IndexOf("$Action.Opacity", StringComparison.Ordinal);

        CernealaSemanticToken reference = Assert.Single(tokens.Where(token =>
            token.Span.Start == referenceStart && fixture.Document.Text.Substring(token.Span) == "$Action"));
        CernealaSemanticToken property = Assert.Single(tokens.Where(token =>
            token.Span.Start == referenceStart + "$Action.".Length &&
            fixture.Document.Text.Substring(token.Span) == "Opacity"));

        Assert.Equal(CernealaSemanticTokenKind.Variable, reference.Kind);
        Assert.Equal(CernealaSemanticTokenKind.Property, property.Kind);
    }

    [Fact]
    public void ResourceAndNamedControlReferencesUseDistinctSemanticCategories()
    {
        using Fixture fixture = Fixture.Create(Markup);
        CernealaStructureService service = new();
        IReadOnlyList<CernealaSemanticToken> tokens = service.GetSemanticTokens(
            fixture.Document,
            fixture.Model);
        int controlStart = Markup.IndexOf("$Action", StringComparison.Ordinal);

        CernealaSemanticToken[] resources = tokens.Where(token =>
            fixture.Document.Text.Substring(token.Span) == "$Accent").ToArray();
        CernealaSemanticToken control = Assert.Single(tokens.Where(token =>
            token.Span.Start == controlStart && fixture.Document.Text.Substring(token.Span) == "$Action"));

        Assert.Equal(2, resources.Length);
        Assert.All(resources, resource => Assert.Equal(CernealaSemanticTokenKind.Label, resource.Kind));
        Assert.Equal(CernealaSemanticTokenKind.Variable, control.Kind);
    }

    [Fact]
    public void SyntaxOnlyTokensProvideImmediateEditorColoringBeforeWorkspaceLoad()
    {
        const string markup = """
            <Window>
              <Window.Resources>
                <SolidColorBrush Name="Accent" />
                <Aspect TargetType="Button">
                  @when IsEnabled and $DataContext.Enabled and IsMouseOver {
                    Background = $Accent;
                  }
                </Aspect>
              </Window.Resources>
              <Button Name="Action" Background="$Accent:TwoWay" />
              <TextBlock Opacity="$Action.Opacity" />
            </Window>
            """;
        using Fixture fixture = Fixture.Create(markup);
        CernealaStructureService service = new();

        IReadOnlyList<CernealaSemanticToken> tokens = service.GetSemanticTokens(
            fixture.Document,
            model: null);

        AssertToken(tokens, fixture.Document, "Window", CernealaSemanticTokenKind.Keyword);
        AssertToken(tokens, fixture.Document, "Background", CernealaSemanticTokenKind.Property);
        AssertToken(tokens, fixture.Document, "$Accent", CernealaSemanticTokenKind.Label);
        AssertToken(tokens, fixture.Document, "$Action", CernealaSemanticTokenKind.Variable);
        AssertToken(tokens, fixture.Document, "Opacity", CernealaSemanticTokenKind.Property);
        AssertToken(tokens, fixture.Document, "TwoWay", CernealaSemanticTokenKind.EnumMember);
        AssertToken(tokens, fixture.Document, "IsEnabled", CernealaSemanticTokenKind.ConditionProperty);
        AssertToken(tokens, fixture.Document, "IsMouseOver", CernealaSemanticTokenKind.ConditionProperty);
        AssertToken(tokens, fixture.Document, "$DataContext", CernealaSemanticTokenKind.Variable);
        AssertToken(tokens, fixture.Document, "Enabled", CernealaSemanticTokenKind.Property);
    }

    [Fact]
    public void BindingModesUseTheEnumMemberSemanticCategory()
    {
        const string markup = """
            <Window>
              <Window.Resources>
                <SolidColorBrush Name="CyanBrush" />
              </Window.Resources>
              <TextBlock Foreground="$CyanBrush:OneWay" />
            </Window>
            """;
        using Fixture fixture = Fixture.Create(markup);
        CernealaStructureService service = new();

        CernealaSemanticToken mode = Assert.Single(service
            .GetSemanticTokens(fixture.Document, fixture.Model)
            .Where(token => fixture.Document.Text.Substring(token.Span) == "OneWay"));

        Assert.Equal(CernealaSemanticTokenKind.EnumMember, mode.Kind);
    }

    [Fact]
    public void EveryMarkupTypeUsesTheKeywordSemanticCategory()
    {
        const string markup = """
            <Window>
              <Window.Resources>
                <Aspect TargetType="TextBlock" />
              </Window.Resources>
              <StackPanel>
                <TextBlock />
                <TextBlock />
              </StackPanel>
            </Window>
            """;
        using Fixture fixture = Fixture.Create(markup);
        CernealaStructureService service = new();
        IReadOnlyList<CernealaSemanticToken> tokens = service.GetSemanticTokens(
            fixture.Document,
            fixture.Model);

        foreach ((string typeName, int expectedOccurrences) in new[]
        {
            ("Window", 2),
            ("StackPanel", 2),
            ("TextBlock", 3)
        })
        {
            CernealaSemanticToken[] typeTokens = tokens.Where(token =>
                fixture.Document.Text.Substring(token.Span) == typeName).ToArray();

            Assert.Equal(expectedOccurrences, typeTokens.Length);
            Assert.All(typeTokens, token => Assert.Equal(CernealaSemanticTokenKind.Keyword, token.Kind));
        }
    }

    [Fact]
    public void AspectAssignmentsUsePropertySemanticCategory()
    {
        using Fixture fixture = Fixture.Create(Markup);
        CernealaStructureService service = new();
        IReadOnlyList<CernealaSemanticToken> tokens = service.GetSemanticTokens(
            fixture.Document,
            fixture.Model);
        int propertyStart = Markup.IndexOf("Opacity = 1", StringComparison.Ordinal);

        CernealaSemanticToken property = Assert.Single(tokens.Where(token =>
            token.Span.Start == propertyStart && fixture.Document.Text.Substring(token.Span) == "Opacity"));

        Assert.Equal(CernealaSemanticTokenKind.Property, property.Kind);
    }

    [Fact]
    public void AspectConditionPropertiesUsePropertySemanticCategory()
    {
        const string markup = """
            <Window>
              <Window.Resources>
                <Aspect TargetType="Button">
                  @when IsEnabled and (Opacity > 0) {
                    @if value == true or IsMouseOver {
                      Opacity = 1;
                    }
                  }
                </Aspect>
              </Window.Resources>
            </Window>
            """;
        using Fixture fixture = Fixture.Create(markup);
        CernealaStructureService service = new();
        IReadOnlyList<CernealaSemanticToken> tokens = service.GetSemanticTokens(
            fixture.Document,
            fixture.Model);

        foreach (string propertyName in new[] { "IsEnabled", "Opacity", "IsMouseOver" })
        {
            int conditionStart = markup.IndexOf(propertyName, StringComparison.Ordinal);
            CernealaSemanticToken property = Assert.Single(tokens.Where(token =>
                token.Span.Start == conditionStart && fixture.Document.Text.Substring(token.Span) == propertyName));

            Assert.Equal(CernealaSemanticTokenKind.ConditionProperty, property.Kind);
        }

        Assert.DoesNotContain(tokens, token =>
            token.Kind == CernealaSemanticTokenKind.ConditionProperty &&
            fixture.Document.Text.Substring(token.Span) is "value" or "true" or "and" or "or");
    }

    [Fact]
    public void MotionHandleDeclarationAndUseShareTheLabelSemanticCategory()
    {
        const string markup = """
            <Window>
              <Window.Resources>
                <MotionClip Name="Pulse" TargetType="Button" />
                <Aspect Name="Animated" TargetType="Button">
                  @handle Loading;
                  @on Loaded {
                    @run $Pulse as Loading;
                  }
                </Aspect>
              </Window.Resources>
            </Window>
            """;
        using Fixture fixture = Fixture.Create(markup);
        CernealaStructureService service = new();

        CernealaSemanticToken[] handles = service.GetSemanticTokens(fixture.Document, fixture.Model)
            .Where(token => fixture.Document.Text.Substring(token.Span) == "Loading")
            .ToArray();

        Assert.Equal(2, handles.Length);
        Assert.All(handles, token => Assert.Equal("Label", token.Kind.ToString()));
        Assert.Equal(CernealaSemanticTokenModifiers.Declaration, handles[0].Modifiers);
        Assert.Equal(CernealaSemanticTokenModifiers.None, handles[1].Modifiers);
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

    private static void AssertToken(
        IReadOnlyList<CernealaSemanticToken> tokens,
        CernealaDocument document,
        string text,
        CernealaSemanticTokenKind kind) => Assert.Contains(tokens, token =>
            document.Text.Substring(token.Span) == text && token.Kind == kind);

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
            CernealaDocument document = new("View.crn", SourceText.From(markup, version));
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
