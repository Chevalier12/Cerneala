using Cerneala.Language.Diagnostics;
using Cerneala.Language.Features;
using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Text;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cerneala.Tests.Language;

public sealed class FormattingTests
{
    [Fact]
    public void CanonicalFormattingPreservesCommentsLiteralTextMotionPrismAndAttributeOrder()
    {
        const string input = """
            <Window
            Title="Demo"
            Width="800">
            <!-- comment -->
            <Window.Resources>
            <Aspect Name="Interactive" TargetType="Border">
            @when IsMouseOver
            {
            Opacity = 1;
            }
            </Aspect>
            <PrismClip Name="Fx">
            @layer Card
            {
            @filter Blur { Radius = 8; }
            }
            </PrismClip>
            </Window.Resources>
            <TextBlock>
              literal text keeps its authored leading whitespace
            </TextBlock>
            </Window>
            """;
        const string expected = """
            <Window
              Title="Demo"
              Width="800">
              <!-- comment -->
              <Window.Resources>
                <Aspect Name="Interactive" TargetType="Border">
                  @when IsMouseOver
                  {
                    Opacity = 1;
                  }
                </Aspect>
                <PrismClip Name="Fx">
                  @layer Card
                  {
                    @filter Blur { Radius = 8; }
                  }
                </PrismClip>
              </Window.Resources>
              <TextBlock>
              literal text keeps its authored leading whitespace
              </TextBlock>
            </Window>
            """;
        CernealaFormattingService formatter = new();
        CernealaDocument document = Document(input);

        string formatted = Apply(input, formatter.FormatDocument(
            document,
            new CernealaFormattingOptions(2, InsertSpaces: true)));

        Assert.Equal(expected, formatted);
        Assert.Empty(formatter.FormatDocument(
            Document(formatted, version: 2),
            new CernealaFormattingOptions(2, InsertSpaces: true)));
        Assert.Equal(
            document.Syntax.DescendantElements().Select(element => element.Attributes.Select(attribute => attribute.NameToken.Text)),
            Document(formatted).Syntax.DescendantElements().Select(element => element.Attributes.Select(attribute => attribute.NameToken.Text)));
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void RangeAndOnTypeFormattingTouchOnlyTheSelectedLinesAndToleratePartialMarkup(string newLine)
    {
        string input = string.Join(newLine,
            "<Window>",
            "<StackPanel>",
            "<Button />",
            "</StackPanel>",
            "<TextBlock",
            "Text=\"partial\"");
        CernealaFormattingService formatter = new();
        CernealaDocument document = Document(input);
        int buttonStart = input.IndexOf("<Button", StringComparison.Ordinal);
        int stackClose = input.IndexOf("</StackPanel>", StringComparison.Ordinal) + "</StackPanel>".Length;

        string rangeFormatted = Apply(input, formatter.FormatRange(
            document,
            new TextSpan(buttonStart, stackClose - buttonStart),
            new CernealaFormattingOptions(2, InsertSpaces: true)));
        Assert.StartsWith($"<Window>{newLine}<StackPanel>{newLine}", rangeFormatted, StringComparison.Ordinal);
        Assert.Contains($"    <Button />{newLine}  </StackPanel>", rangeFormatted, StringComparison.Ordinal);
        Assert.EndsWith($"<TextBlock{newLine}Text=\"partial\"", rangeFormatted, StringComparison.Ordinal);

        int attributeOffset = input.IndexOf("Text=", StringComparison.Ordinal) + 2;
        string onType = Apply(input, formatter.FormatOnType(
            document,
            attributeOffset,
            new CernealaFormattingOptions(2, InsertSpaces: true)));
        Assert.Contains($"<TextBlock{newLine}    Text=\"partial\"", onType, StringComparison.Ordinal);
    }

    // A quoted attribute value is data, not layout: the leading spaces of its continuation lines belong to the value.
    [Theory]
    [InlineData("\n", false)]
    [InlineData("\r\n", false)]
    [InlineData("\n", true)]
    [InlineData("\r\n", true)]
    public void FormattingKeepsTheTextOfAMultiLineAttributeValue(string newLine, bool multiLine)
    {
        string value = multiLine ? "line one" + newLine + "      line two" : "line one line two";
        string input = "<Window>" + newLine + "<TextBlock Text=\"" + value + "\" />" + newLine + "</Window>";
        CernealaFormattingService formatter = new();
        CernealaDocument document = Document(input);
        CernealaFormattingOptions options = new(2, InsertSpaces: true);
        int textBlockStart = input.IndexOf("<TextBlock", StringComparison.Ordinal);
        int textBlockEnd = input.IndexOf("/>", StringComparison.Ordinal) + 2;

        string formatted = Apply(input, formatter.FormatDocument(document, options));
        string rangeFormatted = Apply(input, formatter.FormatRange(
            document,
            new TextSpan(textBlockStart, textBlockEnd - textBlockStart),
            options));

        Assert.Equal(
            document.Syntax.DescendantElements().Select(ElementShape),
            Document(formatted, version: 2).Syntax.DescendantElements().Select(ElementShape));
        Assert.Equal(
            document.Syntax.DescendantElements().Select(ElementShape),
            Document(rangeFormatted, version: 2).Syntax.DescendantElements().Select(ElementShape));
        Assert.StartsWith("<Window>" + newLine + "  <TextBlock Text=\"", formatted, StringComparison.Ordinal);
        Assert.Empty(formatter.FormatDocument(Document(formatted, version: 2), options));
    }

    // A whitespace-only line inside a multi-line attribute value is part of the string: formatting must not blank it.
    [Theory]
    [InlineData("\n", false)]
    [InlineData("\r\n", false)]
    [InlineData("\n", true)]
    [InlineData("\r\n", true)]
    public void FormattingKeepsAWhitespaceOnlyLineInsideAMultiLineAttributeValue(string newLine, bool whitespaceLine)
    {
        string value = whitespaceLine ? "line one" + newLine + "      " + newLine + "line two" : "line one line two";
        string input = "<Window>" + newLine + "<TextBlock Text=\"" + value + "\" />" + newLine + "</Window>";
        CernealaFormattingService formatter = new();
        CernealaDocument document = Document(input);

        string formatted = Apply(input, formatter.FormatDocument(document, new CernealaFormattingOptions(2, InsertSpaces: true)));

        string after = ElementShape(Document(formatted, version: 2).Syntax.DescendantElements().Single(element => element.Name == "TextBlock"));
        string[] valueLines = after[(after.IndexOf("Text=\"", StringComparison.Ordinal) + 6)..^1].Split(newLine);
        Assert.True(
            whitespaceLine ? valueLines.Length == 3 && valueLines[1] == "      " : valueLines.Length == 1 && valueLines[0] == value,
            $"whitespaceLine={whitespaceLine}: value after formatting {Escape(after)} (the middle line must stay six spaces).");
    }

    [Fact]
    public void CanonicalFormattingProducesNoSecondPassDiffOnTheApprovedPresentationCorpus()
    {
        CernealaFormattingService formatter = new();
        string root = FindRepositoryRoot();
        string presentation = Path.Combine(root, "CernealaPresentation");

        foreach (string path in Directory.GetFiles(presentation, "*.crn", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            string text = File.ReadAllText(path);
            CernealaDocument document = new(path, SourceText.From(text));
            IReadOnlyList<CernealaFormattingEdit> edits = formatter.FormatDocument(
                document,
                new CernealaFormattingOptions(4, InsertSpaces: true));
            string formatted = Apply(text, edits);
            CernealaDocument baseline = new(path, SourceText.From(formatted, version: 2));

            Assert.Empty(formatter.FormatDocument(
                baseline,
                new CernealaFormattingOptions(4, InsertSpaces: true)));
            Assert.Equal(
                document.Syntax.DescendantElements().Select(ElementShape),
                baseline.Syntax.DescendantElements().Select(ElementShape));
        }
    }

    [Fact]
    public void DeterministicQuickFixesRemoveTheirTargetDiagnosticsAndFixAllRejectsOverlaps()
    {
        CernealaCodeActionService service = new();
        using Fixture typo = Fixture.Create("<Window><Button Widht=\"10\" Heigth=\"20\" /></Window>");
        Assert.Equal(2, typo.Model.Diagnostics.Count(diagnostic => diagnostic.Id == "CERNEALAUI003"));
        Assert.Contains(typo.Model.Symbols, symbol =>
            symbol.Kind == CernealaSemanticSymbolKind.Element && symbol.Name == "Button" && symbol.TypeSymbol is not null);
        IReadOnlyList<CernealaCodeAction> actions = service.GetCodeActions(
            typo.Document,
            typo.Model,
            typo.Document.Syntax.Span,
            [],
            [],
            includeFixAll: true);
        Assert.Contains(actions, action => action.Title == "Change property to Width");
        Assert.Contains(actions, action => action.Title == "Change property to Height");
        CernealaCodeAction fixAll = Assert.Single(actions, action => action.Kind == "source.fixAll.cerneala");
        string fixedText = Apply(typo.Document.Text.ToString(), fixAll.Edits);
        using Fixture fixedFixture = Fixture.Create(fixedText, version: 2);
        Assert.DoesNotContain(fixedFixture.Model.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI003");
        Assert.Empty(fixedFixture.Document.Syntax.Diagnostics);

        using Fixture missing = Fixture.Create("<Window><StackPanel><Button />");
        IReadOnlyList<CernealaCodeAction> missingActions = service.GetCodeActions(
            missing.Document,
            missing.Model,
            missing.Document.Syntax.Span,
            [],
            [],
            includeFixAll: true);
        Assert.Contains(missingActions, action => action.Title.StartsWith("Add closing tag", StringComparison.Ordinal));
        Assert.DoesNotContain(missingActions, action => action.Kind == "source.fixAll.cerneala");
        CernealaCodeAction closing = Assert.Single(missingActions, action => action.Title == "Add closing tag </StackPanel>");
        using Fixture closed = Fixture.Create(Apply(missing.Document.Text.ToString(), closing.Edits), version: 2);
        Assert.DoesNotContain(closed.Model.Diagnostics, diagnostic =>
            diagnostic.Id == "CERNEALAUI001" && diagnostic.Message.Contains("StackPanel", StringComparison.Ordinal));
    }

    [Fact]
    public void NamespaceEventAndPropertyElementActionsAreScopedToProvablyUniqueTargets()
    {
        CernealaCodeActionService service = new();
        using Fixture alias = Fixture.Create("<Window><widgets:FancyPanel /></Window>");
        Assert.Contains(alias.Model.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI002");
        Assert.Single(alias.Model.Compilation.FindTypes("FancyPanel"));
        CernealaCodeAction aliasAction = Assert.Single(service.GetCodeActions(
            alias.Document,
            alias.Model,
            alias.Document.Syntax.Span,
            [],
            [],
            includeFixAll: false), action => action.Title.StartsWith("Add xmlns:widgets", StringComparison.Ordinal));
        using Fixture aliased = Fixture.Create(Apply(alias.Document.Text.ToString(), aliasAction.Edits), version: 2);
        Assert.DoesNotContain(aliased.Model.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI002");

        const string companion = """
            using Cerneala.UI.Controls;
            namespace Fixture;
            public sealed partial class View : Window
            {
            }
            public sealed class FancyPanel : Button { }
            """;
        using Fixture eventFixture = Fixture.Create("<Window Loaded=\"OnLoaded\" />", companion: companion);
        CernealaAdditionalDocument code = new(eventFixture.CodePath, SourceText.From(companion));
        CernealaCodeAction eventAction = Assert.Single(service.GetCodeActions(
            eventFixture.Document,
            eventFixture.Model,
            eventFixture.Document.Syntax.Span,
            [new CernealaCodeActionDiagnostic("CERNEALAUI009", eventFixture.Document.Syntax.Span)],
            [code],
            includeFixAll: false), action => action.Title == "Create event handler OnLoaded");
        string generatedCode = Apply(companion, eventAction.Edits);
        Assert.Contains(
            "private void OnLoaded(Fixture.View sender, Cerneala.UI.Input.RoutedEventArgs args)",
            generatedCode,
            StringComparison.Ordinal);
        Assert.Contains(
            LanguagePipelineHarness.AnalyzePairedSourceGenerator(
                eventFixture.Document.Path,
                eventFixture.Document.Text.ToString(),
                eventFixture.CodePath,
                companion),
            diagnostic => diagnostic.Id == "CERNEALAUI009");
        Assert.DoesNotContain(
            LanguagePipelineHarness.AnalyzePairedSourceGenerator(
                eventFixture.Document.Path,
                eventFixture.Document.Text.ToString(),
                eventFixture.CodePath,
                generatedCode),
            diagnostic => diagnostic.Id == "CERNEALAUI009");

        using Fixture childEventFixture = Fixture.Create(
            "<Window><Slider ValueChanged=\"OnVolume\" /></Window>",
            companion: companion);
        CernealaCodeAction childEventAction = Assert.Single(service.GetCodeActions(
            childEventFixture.Document,
            childEventFixture.Model,
            childEventFixture.Document.Syntax.Span,
            [new CernealaCodeActionDiagnostic("CERNEALAUI009", childEventFixture.Document.Syntax.Span)],
            [new CernealaAdditionalDocument(childEventFixture.CodePath, SourceText.From(companion))],
            includeFixAll: false), action => action.Title == "Create event handler OnVolume");
        string childGeneratedCode = Apply(companion, childEventAction.Edits);
        Assert.Contains(
            "private void OnVolume(Cerneala.UI.Controls.Slider sender, Cerneala.UI.Input.RoutedPropertyChangedEventArgs<float> e)",
            childGeneratedCode,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            LanguagePipelineHarness.AnalyzePairedSourceGenerator(
                childEventFixture.Document.Path,
                childEventFixture.Document.Text.ToString(),
                childEventFixture.CodePath,
                childGeneratedCode),
            diagnostic => diagnostic.Id == "CERNEALAUI009");

        using Fixture mouseEventFixture = Fixture.Create(
            "<Window><Button MouseDown=\"OnPress\" /></Window>",
            companion: companion);
        CernealaCodeAction mouseEventAction = Assert.Single(service.GetCodeActions(
            mouseEventFixture.Document,
            mouseEventFixture.Model,
            mouseEventFixture.Document.Syntax.Span,
            [new CernealaCodeActionDiagnostic("CERNEALAUI009", mouseEventFixture.Document.Syntax.Span)],
            [new CernealaAdditionalDocument(mouseEventFixture.CodePath, SourceText.From(companion))],
            includeFixAll: false), action => action.Title == "Create event handler OnPress");
        string mouseGeneratedCode = Apply(companion, mouseEventAction.Edits);
        Assert.Contains(
            "private void OnPress(Cerneala.UI.Controls.Button sender, Cerneala.UI.Input.MouseButtonEventArgs args)",
            mouseGeneratedCode,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            LanguagePipelineHarness.AnalyzePairedSourceGenerator(
                mouseEventFixture.Document.Path,
                mouseEventFixture.Document.Text.ToString(),
                mouseEventFixture.CodePath,
                mouseGeneratedCode),
            diagnostic => diagnostic.Id == "CERNEALAUI009");

        using Fixture propertyElement = Fixture.Create("""
            <Window>
              <Button>
                <Button.Width>120</Button.Width>
              </Button>
            </Window>
            """);
        CernealaCodeAction conversion = Assert.Single(service.GetCodeActions(
            propertyElement.Document,
            propertyElement.Model,
            propertyElement.Document.Syntax.Span,
            [],
            [],
            includeFixAll: false), action => action.Kind == "refactor.rewrite");
        string converted = Apply(propertyElement.Document.Text.ToString(), conversion.Edits);
        using Fixture convertedFixture = Fixture.Create(converted, version: 2);
        Assert.Contains("Width=\"120\"", converted, StringComparison.Ordinal);
        Assert.DoesNotContain(convertedFixture.Model.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI001");
    }

    private static CernealaDocument Document(string text, long version = 1) =>
        new("View.crn", SourceText.From(text, version));

    private static string ElementShape(Cerneala.Language.Syntax.ElementSyntax element) =>
        element.Name + "|" + string.Join(
            ";",
            element.Attributes.Select(attribute => attribute.NameToken.Text + "=" + attribute.ValueToken.Text));

    private static string Escape(string text) => text.Replace("\r", "\\r").Replace("\n", "\\n");

    [Theory]
    [InlineData("Fish", "Text=\"Fish\"")]
    [InlineData("Fish &amp; Chips", "Text=\"Fish &amp; Chips\"")]
    [InlineData("a &lt; b", "Text=\"a &lt; b\"")]
    [InlineData("say \"hi\"", "Text=\"say &quot;hi&quot;\"")]
    public void PropertyElementToAttributeConversionKeepsXmlEntitiesSingleEscaped(string elementText, string expectedAttribute)
    {
        CernealaCodeActionService service = new();
        using Fixture propertyElement = Fixture.Create(
            "<Window><TextBlock><TextBlock.Text>" + elementText + "</TextBlock.Text></TextBlock></Window>");
        CernealaCodeAction conversion = Assert.Single(service.GetCodeActions(
            propertyElement.Document,
            propertyElement.Model,
            propertyElement.Document.Syntax.Span,
            [],
            [],
            includeFixAll: false), action => action.Kind == "refactor.rewrite");
        string converted = Apply(propertyElement.Document.Text.ToString(), conversion.Edits);

        Assert.Contains(expectedAttribute, converted, StringComparison.Ordinal);
        Assert.DoesNotContain("&amp;amp;", converted, StringComparison.Ordinal);
        Assert.DoesNotContain("&amp;lt;", converted, StringComparison.Ordinal);
    }

    private static string Apply(string source, IReadOnlyList<CernealaFormattingEdit> edits)
    {
        foreach (CernealaFormattingEdit edit in edits.OrderByDescending(edit => edit.Span.Start))
        {
            source = source.Substring(0, edit.Span.Start) + edit.NewText + source.Substring(edit.Span.End);
        }

        return source;
    }

    private static string Apply(string source, IReadOnlyList<CernealaTextEdit> edits)
    {
        foreach (CernealaTextEdit edit in edits.OrderByDescending(edit => edit.Span.Start))
        {
            source = source.Substring(0, edit.Span.Start) + edit.NewText + source.Substring(edit.Span.End);
        }

        return source;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Cerneala repository root.");
    }

    private sealed class Fixture : IDisposable
    {
        private Fixture(CernealaDocument document, CernealaCompilation compilation, string codePath)
        {
            Document = document;
            Compilation = compilation;
            CodePath = codePath;
            Model = compilation.GetSemanticModel(document.Path);
        }

        public CernealaDocument Document { get; }

        public CernealaCompilation Compilation { get; }

        public CernealaSemanticModel Model { get; }

        public string CodePath { get; }

        public static Fixture Create(string markup, long version = 1, string? companion = null)
        {
            string codePath = Path.GetFullPath("View.crn.cs");
            companion ??= """
                using Cerneala.UI.Controls;
                namespace Fixture;
                public sealed partial class View : Window { }
                public sealed class FancyPanel : Button { }
                """;
            CSharpCompilation roslyn = CSharpCompilation.Create(
                "FormattingTests",
                [CSharpSyntaxTree.ParseText(companion, path: codePath)],
                PlatformReferences().Append(MetadataReference.CreateFromFile(typeof(UIElement).Assembly.Location)),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            CernealaDocument document = new(Path.GetFullPath("View.crn"), SourceText.From(markup, version));
            CernealaCompilation compilation = new(
                new RoslynCompilationSymbols(roslyn),
                [document],
                AnalysisMode.Editor);
            return new Fixture(document, compilation, codePath);
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
