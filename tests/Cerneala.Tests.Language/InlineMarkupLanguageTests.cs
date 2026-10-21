using Cerneala.Language.Diagnostics;
using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Text;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using LanguageSourceText = Cerneala.Language.Text.SourceText;

namespace Cerneala.Tests.Language;

// Rich text markup (issue #683): inline elements and flags enum values in the language model.
public sealed class InlineMarkupLanguageTests
{
    private const string MarkupPath = "C:/inline-markup-fixture/View.crn";

    [Theory]
    [InlineData("<TextBlock>Hello <Bold>world</Bold></TextBlock>")]
    [InlineData("<TextBlock><Run Text=\"a\" /><Span><Italic><LineBreak /></Italic></Span></TextBlock>")]
    [InlineData("<TextBlock><Span><Bold>x</Bold><Run>y</Run></Span></TextBlock>")]
    [InlineData("<StackPanel><TextBlock>a <Italic>b</Italic></TextBlock></StackPanel>")]
    public void InlineChildrenOfTextBlockAndSpanProduceNoDiagnostics(string markup)
    {
        Assert.Empty(Diagnose(markup));
    }

    [Fact]
    public void InlineAttributesAreBoundWithoutDiagnostics()
    {
        Assert.Empty(Diagnose(
            "<TextBlock><Run Text=\"x\" FontStyle=\"Italic\" FontWeight=\"Bold\" FontSize=\"20\" " +
            "TextDecorations=\"Underline, Strikethrough\" Foreground=\"#FF0000\" /></TextBlock>"));
    }

    [Theory]
    [InlineData("<Button><Run Text=\"x\" /></Button>")]
    [InlineData("<StackPanel><Bold>x</Bold></StackPanel>")]
    [InlineData("<Border><Span /></Border>")]
    [InlineData("<TextBlock><Run><Bold>x</Bold></Run></TextBlock>")]
    public void InlineUnderAnotherParentReportsTheUnsupportedChildDiagnostic(string markup)
    {
        LanguageDiagnostic diagnostic = Assert.Single(Diagnose(markup));
        Assert.Equal("CERNEALAUI003", diagnostic.Id);
        Assert.Contains("#child", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void InlineAsTheDocumentRootIsAnUnsupportedElement()
    {
        LanguageDiagnostic diagnostic = Assert.Single(Diagnose("<Run Text=\"x\" />"));
        Assert.Equal("CERNEALAUI002", diagnostic.Id);
    }

    [Theory]
    [InlineData("<TextBlock TextDecorations=\"Underline\" />")]
    [InlineData("<TextBlock TextDecorations=\"underline\" />")]
    [InlineData("<TextBlock TextDecorations=\"Underline, Strikethrough\" />")]
    [InlineData("<TextBlock TextDecorations=\"  overline ,BASELINE \" />")]
    [InlineData("<TextBlock TextDecorations=\"None\" />")]
    public void FlagsEnumValuesAcceptCommaSeparatedNames(string markup)
    {
        Assert.Empty(Diagnose(markup));
    }

    [Theory]
    [InlineData("<TextBlock TextDecorations=\"Underline, Nope\" />", "TextBlock")]
    [InlineData("<TextBlock TextDecorations=\"Underline,\" />", "TextBlock")]
    [InlineData("<TextBlock TextDecorations=\"Underline,,Overline\" />", "TextBlock")]
    [InlineData("<TextBlock TextDecorations=\"\" />", "TextBlock")]
    [InlineData("<TextBlock TextAlignment=\"Left, Right\" />", "TextBlock")]
    [InlineData("<TextBlock><Run TextDecorations=\"Underline, Nope\" /></TextBlock>", "Run")]
    public void UnknownFlagsNamesOrCommaListsOfPlainEnumsReportTheInvalidValueDiagnostic(string markup, string owner)
    {
        LanguageDiagnostic diagnostic = Assert.Single(Diagnose(markup));
        Assert.Equal("CERNEALAUI004", diagnostic.Id);
        Assert.Contains(owner + ".", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FlagsEnumValuesAreAcceptedInAspectAssignments()
    {
        Assert.Empty(Diagnose(
            """
            <StackPanel>
              <StackPanel.Resources>
                <Aspect Name="Deco" TargetType="TextBlock">
                  @default { TextDecorations = Underline, Overline; }
                </Aspect>
              </StackPanel.Resources>
              <TextBlock Aspect="$Deco" Text="x" />
            </StackPanel>
            """));
    }

    [Fact]
    public void UnknownFlagsNamesAreRejectedInAspectAssignments()
    {
        LanguageDiagnostic diagnostic = Assert.Single(Diagnose(
            """
            <StackPanel>
              <StackPanel.Resources>
                <Aspect Name="Deco" TargetType="TextBlock">
                  @default { TextDecorations = Underline, Nope; }
                </Aspect>
              </StackPanel.Resources>
              <TextBlock Aspect="$Deco" Text="x" />
            </StackPanel>
            """));
        Assert.Equal("CERNEALAUI004", diagnostic.Id);
    }

    private static IReadOnlyList<LanguageDiagnostic> Diagnose(string markup)
    {
        string[] paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        MetadataReference[] references = paths.Append(typeof(UIElement).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase).Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        CSharpCompilation project = CSharpCompilation.Create("InlineMarkupCorpus", references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using CernealaCompilation compilation = new(new RoslynCompilationSymbols(project),
            [new CernealaDocument(MarkupPath, LanguageSourceText.From(markup))]);
        return compilation.GetSemanticModel(MarkupPath).Diagnostics.ToArray();
    }
}
