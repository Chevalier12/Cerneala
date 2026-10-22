using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Documents;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;
using Cerneala.UI.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace Cerneala.Tests.SourceGen;

// Rich text markup (issue #683): the inline elements of a TextBlock, the new TextBlock properties and flags enum values.
public sealed partial class UiMarkupGeneratorTests
{
    private const string InlineDirectiveMessage = "Inline elements are not supported inside directive content.";

    [Fact]
    public void RichTextNewTextBlockPropertiesAreAssignedFromAttributes()
    {
        TextBlock block = Assert.IsType<TextBlock>(CreateRichFixture(
            "RichTextProperties",
            """
            <TextBlock Text="x" TextAlignment="Center" FontWeight="Bold" FontStyle="Italic" FontStretch="Condensed"
                       TextDecorations="Underline, Strikethrough" LineHeight="24" LineStackingStrategy="BlockLineHeight"
                       MaxLines="3" TextWrapping="WrapWithOverflow" />
            """));

        Assert.Equal("x", block.Text);
        Assert.Equal(TextAlignment.Center, block.TextAlignment);
        Assert.Equal(Cerneala.Drawing.FontWeight.Bold, block.FontWeight);
        Assert.Equal(Cerneala.Drawing.FontStyle.Italic, block.FontStyle);
        Assert.Equal(Cerneala.Drawing.FontStretch.Condensed, block.FontStretch);
        Assert.Equal(TextDecorations.Underline | TextDecorations.Strikethrough, block.TextDecorations);
        Assert.Equal(24f, block.LineHeight);
        Assert.Equal(LineStackingStrategy.BlockLineHeight, block.LineStackingStrategy);
        Assert.Equal(3, block.MaxLines);
        Assert.Equal(TextWrapping.WrapWithOverflow, block.TextWrapping);
    }

    [Theory]
    [InlineData("Underline", TextDecorations.Underline)]
    [InlineData("underline", TextDecorations.Underline)]
    [InlineData("None", TextDecorations.None)]
    [InlineData("Underline, Strikethrough", TextDecorations.Underline | TextDecorations.Strikethrough)]
    [InlineData("Underline,Strikethrough,Overline", TextDecorations.Underline | TextDecorations.Strikethrough | TextDecorations.Overline)]
    [InlineData("  overline ,  BASELINE  ", TextDecorations.Overline | TextDecorations.Baseline)]
    [InlineData("Underline, Underline", TextDecorations.Underline)]
    public void RichTextFlagsEnumAttributesCombineTheListedNames(string value, TextDecorations expected)
    {
        TextBlock block = Assert.IsType<TextBlock>(CreateRichFixture(
            "RichTextFlags",
            "<TextBlock TextDecorations=\"" + value + "\" />"));

        Assert.Equal(expected, block.TextDecorations);
    }

    [Fact]
    public void RichTextFlagsEnumEmitsAParenthesizedBitwiseOr()
    {
        GeneratorRunResult result = RunGenerator(
            "RichTextFlagsSource.crn",
            "<TextBlock TextDecorations=\"Underline, Strikethrough\" />",
            out _);

        Assert.Empty(result.Diagnostics);
        Assert.Contains(
            "(global::Cerneala.UI.Text.TextDecorations.Underline | global::Cerneala.UI.Text.TextDecorations.Strikethrough)",
            SingleGeneratedSource(result));
    }

    [Theory]
    [InlineData("Underline, Nope")]
    [InlineData("Nope")]
    [InlineData("Underline,,Strikethrough")]
    [InlineData("Underline,")]
    [InlineData("")]
    public void RichTextFlagsEnumWithAnUnknownNameReportsTheInvalidValueDiagnostic(string value)
    {
        GeneratorRunResult result = RunGenerator(
            "RichTextInvalidFlags.crn",
            "<TextBlock TextDecorations=\"" + value + "\" />",
            out _);

        AssertDiagnostic(result, "CERNEALAUI004", "RichTextInvalidFlags.crn");
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void RichTextAnEnumWithoutTheFlagsAttributeDoesNotAcceptACommaList()
    {
        GeneratorRunResult result = RunGenerator(
            "RichTextCommaEnum.crn",
            "<TextBlock TextAlignment=\"Left, Right\" />",
            out _);

        AssertDiagnostic(result, "CERNEALAUI004", "RichTextCommaEnum.crn");
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void RichTextFlagsEnumWorksInAnAspectAssignment()
    {
        UIElement root = CreateRichFixture(
            "RichTextAspectFlags",
            """
            <StackPanel>
              <StackPanel.Resources>
                <Aspect Name="Deco" TargetType="TextBlock">
                  @default { TextDecorations = Underline, Overline; }
                </Aspect>
              </StackPanel.Resources>
              <TextBlock Aspect="$Deco" Text="x" />
            </StackPanel>
            """);

        AttachAndProcess(root);
        TextBlock block = Assert.IsType<TextBlock>(((StackPanel)root).VisualChildren[0]);
        Assert.Equal(TextDecorations.Underline | TextDecorations.Overline, block.TextDecorations);
    }

    [Fact]
    public void RichTextEveryInlineElementBuildsTheMatchingInlineTree()
    {
        TextBlock block = Assert.IsType<TextBlock>(CreateRichFixture(
            "RichTextTree",
            """
            <TextBlock>
              <Run Text="a" />
              <Span><Bold><Run Text="b" /></Bold></Span>
              <Italic><LineBreak /></Italic>
            </TextBlock>
            """));

        Assert.Equal(
            "Run(\"a\"),Run(\" \"),Span[Bold[Run(\"b\")]],Italic[LineBreak]",
            DescribeInlines(block.Inlines));
        Assert.Equal("a b\n", block.Text);
        Assert.Equal(Cerneala.Drawing.FontWeight.Bold, Assert.IsType<Bold>(Assert.IsType<Span>(block.Inlines[2]).Inlines[0]).FontWeight);
    }

    [Fact]
    public void RichTextInlineAttributesAreAssignedOnTheInlineObjects()
    {
        TextBlock block = Assert.IsType<TextBlock>(CreateRichFixture(
            "RichTextInlineAttributes",
            """
            <TextBlock>
              <Run Text="r" FontStyle="Italic" FontSize="20" FontFamily="Consolas" FontStretch="Expanded"
                   TextDecorations="Underline, Overline" Foreground="#FF0000" />
              <Bold Foreground="#00FF00" FontWeight="Light" TextDecorations="Strikethrough"><Run Text="b" /></Bold>
            </TextBlock>
            """));

        Run run = Assert.IsType<Run>(block.Inlines[0]);
        Assert.Equal("r", run.Text);
        Assert.Equal(Cerneala.Drawing.FontStyle.Italic, run.FontStyle);
        Assert.Equal(20f, run.FontSize);
        Assert.Equal("Consolas", run.FontFamily);
        Assert.Equal(Cerneala.Drawing.FontStretch.Expanded, run.FontStretch);
        Assert.Equal(TextDecorations.Underline | TextDecorations.Overline, run.TextDecorations);
        Assert.Equal(new Cerneala.Drawing.Color(255, 0, 0), Assert.IsType<SolidColorBrush>(run.Foreground).Color);

        Bold bold = Assert.IsType<Bold>(block.Inlines[2]);
        Assert.Equal(Cerneala.Drawing.FontWeight.Light, bold.FontWeight);
        Assert.Equal(TextDecorations.Strikethrough, bold.TextDecorations);
        Assert.Equal(new Cerneala.Drawing.Color(0, 255, 0), Assert.IsType<SolidColorBrush>(bold.Foreground).Color);
    }

    [Theory]
    // Text, an element, a space between elements.
    [InlineData("<TextBlock>Hello <Bold>world</Bold></TextBlock>", "Run(\"Hello \"),Bold[Run(\"world\")]", "Hello world")]
    [InlineData("<TextBlock><Bold>a</Bold> <Italic>b</Italic></TextBlock>", "Bold[Run(\"a\")],Run(\" \"),Italic[Run(\"b\")]", "a b")]
    // Whitespace next to a LineBreak is removed.
    [InlineData("<TextBlock>one<LineBreak/>  two</TextBlock>", "Run(\"one\"),LineBreak,Run(\"two\")", "one\ntwo")]
    [InlineData("<TextBlock>a <LineBreak/> b<Bold>x</Bold></TextBlock>", "Run(\"a\"),LineBreak,Run(\"b\"),Bold[Run(\"x\")]", "a\nbx")]
    [InlineData("<TextBlock><Bold>a</Bold> <LineBreak/> <Bold>b</Bold></TextBlock>", "Bold[Run(\"a\")],LineBreak,Bold[Run(\"b\")]", "a\nb")]
    [InlineData("<TextBlock><LineBreak/>x<Bold>y</Bold></TextBlock>", "LineBreak,Run(\"x\"),Bold[Run(\"y\")]", "\nxy")]
    // Whitespace at the start and at the end of the host content is removed.
    [InlineData("<TextBlock>  lead <Bold>x</Bold> trail  </TextBlock>", "Run(\"lead \"),Bold[Run(\"x\")],Run(\" trail\")", "lead x trail")]
    [InlineData("<TextBlock>   <Bold>x</Bold>   </TextBlock>", "Bold[Run(\"x\")]", "x")]
    // Runs of spaces, tabs and new lines collapse to one space.
    [InlineData("<TextBlock>a   \t\n   b<Bold>x</Bold></TextBlock>", "Run(\"a b\"),Bold[Run(\"x\")]", "a bx")]
    // Indented multi-line markup.
    [InlineData("<TextBlock>\n    <Bold>a</Bold>\n    <Italic>b</Italic>\n    <Bold>c</Bold>\n</TextBlock>",
        "Bold[Run(\"a\")],Run(\" \"),Italic[Run(\"b\")],Run(\" \"),Bold[Run(\"c\")]", "a b c")]
    [InlineData("<TextBlock>\n    Some\n    <Bold>bold\n        words</Bold>\n    and more.\n</TextBlock>",
        "Run(\"Some \"),Bold[Run(\"bold words\")],Run(\" and more.\")", "Some bold words and more.")]
    // Nested spans, a span with text only, empty spans.
    [InlineData("<TextBlock>x <Span>a <Italic>b</Italic> c</Span> y</TextBlock>",
        "Run(\"x \"),Span[Run(\"a \"),Italic[Run(\"b\")],Run(\" c\")],Run(\" y\")", "x a b c y")]
    [InlineData("<TextBlock><Span> hi  there </Span></TextBlock>", "Span[Run(\"hi there\")]", "hi there")]
    [InlineData("<TextBlock><Span/><Bold></Bold></TextBlock>", "Span[],Bold[]", "")]
    // Run content.
    [InlineData("<TextBlock><Run>abc</Run></TextBlock>", "Run(\"abc\")", "abc")]
    [InlineData("<TextBlock><Run>\n   abc\n     def\n</Run></TextBlock>", "Run(\"abc def\")", "abc def")]
    // The rules apply to the whole TextBlock content, across element boundaries: one space for a run of whitespace,
    // nothing at the very start and the very end, nothing next to a LineBreak.
    [InlineData("<TextBlock>Click <Bold> here </Bold>now</TextBlock>",
        "Run(\"Click \"),Bold[Run(\"here \")],Run(\"now\")", "Click here now")]
    [InlineData("<TextBlock><Bold>a</Bold><Italic> b</Italic></TextBlock>", "Bold[Run(\"a\")],Italic[Run(\" b\")]", "a b")]
    [InlineData("<TextBlock><Bold>a </Bold> b</TextBlock>", "Bold[Run(\"a \")],Run(\"b\")", "a b")]
    [InlineData("<TextBlock><Run> abc </Run></TextBlock>", "Run(\"abc\")", "abc")]
    [InlineData("<TextBlock>x<Run> abc </Run>y</TextBlock>", "Run(\"x\"),Run(\" abc \"),Run(\"y\")", "x abc y")]
    [InlineData("<TextBlock><Bold> a <LineBreak/> b </Bold></TextBlock>", "Bold[Run(\"a\"),LineBreak,Run(\"b\")]", "a\nb")]
    // The Text attribute is never altered.
    [InlineData("<TextBlock><Run Text=\"  a  \"/></TextBlock>", "Run(\"  a  \")", "  a  ")]
    // Comments do not split text; entities are decoded.
    [InlineData("<TextBlock>a<!-- c --> b<Bold>x</Bold></TextBlock>", "Run(\"a b\"),Bold[Run(\"x\")]", "a bx")]
    [InlineData("<TextBlock>a &amp; b<Bold>x</Bold></TextBlock>", "Run(\"a & b\"),Bold[Run(\"x\")]", "a & bx")]
    public void RichTextTextPiecesBecomeRunsWithWpfWhitespaceRules(string markup, string expectedTree, string expectedText)
    {
        TextBlock block = Assert.IsType<TextBlock>(CreateRichFixture("RichTextWhitespace", markup));

        Assert.Equal(expectedTree, DescribeInlines(block.Inlines));
        Assert.Equal(expectedText, block.Text);
    }

    [Fact]
    public void RichTextRunContentSetsTheRunText()
    {
        TextBlock block = Assert.IsType<TextBlock>(CreateRichFixture(
            "RichTextRunContent",
            "<TextBlock><Run FontStyle=\"Italic\">abc</Run></TextBlock>"));

        Run run = Assert.IsType<Run>(Assert.Single(block.Inlines));
        Assert.Equal("abc", run.Text);
        Assert.Equal(Cerneala.Drawing.FontStyle.Italic, run.FontStyle);
    }

    [Fact]
    public void RichTextEmitsTheRunsAndInlinesAsGeneratedCode()
    {
        GeneratorRunResult result = RunGenerator(
            "RichTextSource.crn",
            "<TextBlock>Hello <Bold>world</Bold></TextBlock>",
            out _);

        Assert.Empty(result.Diagnostics);
        string source = SingleGeneratedSource(result);
        Assert.Contains("new global::Cerneala.UI.Documents.Run(\"Hello \")", source);
        Assert.Contains("new global::Cerneala.UI.Documents.Run(\"world\")", source);
        Assert.Contains(".Inlines.Add(", source);
    }

    [Fact]
    public void RichTextAnElementWithOnlyTextKeepsTheTextProperty()
    {
        GeneratorRunResult result = RunGenerator(
            "RichTextTextOnly.crn",
            "<TextBlock>  Hello   world  </TextBlock>",
            out Compilation compilation);

        Assert.Empty(result.Diagnostics);
        string source = SingleGeneratedSource(result);
        Assert.Contains(".SetValue(global::Cerneala.UI.Controls.TextBlock.TextProperty, \"Hello   world\", global::Cerneala.UI.Core.UiPropertyValueSource.MarkupBase);", source);
        Assert.DoesNotContain("Inlines", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Documents.Run", source, StringComparison.Ordinal);
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void RichTextWorksInsideAReactiveDocument()
    {
        UIElement root = CreateRichFixture(
            "RichTextReactive",
            """
            <StackPanel>
              <Button>@when IsMouseOver { @if value == True { Content = "hover"; } }</Button>
              <TextBlock>Hello <Bold FontStyle="Italic">world</Bold></TextBlock>
            </StackPanel>
            """);

        TextBlock block = Assert.IsType<TextBlock>(((StackPanel)root).VisualChildren[1]);
        Assert.Equal("Run(\"Hello \"),Bold[Run(\"world\")]", DescribeInlines(block.Inlines));
        Assert.Equal(Cerneala.Drawing.FontStyle.Italic, Assert.IsType<Bold>(block.Inlines[1]).FontStyle);
    }

    [Fact]
    public void RichTextAnInlineUnderAnotherParentReportsTheUnsupportedChildDiagnostic()
    {
        GeneratorRunResult result = RunGenerator(
            "RichTextWrongParent.crn",
            "<Button><Run Text=\"x\" /></Button>",
            out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI003", "RichTextWrongParent.crn");
        Assert.Contains("#child", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void RichTextAnInlineUnderAPanelReportsTheUnsupportedChildDiagnostic()
    {
        GeneratorRunResult result = RunGenerator(
            "RichTextPanelParent.crn",
            "<StackPanel><Bold>x</Bold></StackPanel>",
            out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI003", "RichTextPanelParent.crn");
        Assert.Contains("#child", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Empty(result.GeneratedSources);
    }

    [Theory]
    [InlineData("<TextBlock><Button Content=\"x\" /></TextBlock>")]
    [InlineData("<TextBlock><Bold><Button Content=\"x\" /></Bold></TextBlock>")]
    [InlineData("<TextBlock>text <Bold>b</Bold><Button Content=\"x\" /></TextBlock>")]
    public void RichTextAnElementUnderATextBlockOrSpanKeepsTheUnsupportedChildDiagnostic(string markup)
    {
        GeneratorRunResult result = RunGenerator("RichTextElementChild.crn", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI003", "RichTextElementChild.crn");
        Assert.Contains("#child", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void RichTextAnInlineAsTheDocumentRootIsNotSupported()
    {
        GeneratorRunResult result = RunGenerator("RichTextRoot.crn", "<Run Text=\"x\" />", out _);

        AssertDiagnostic(result, "CERNEALAUI002", "RichTextRoot.crn");
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void RichTextAnInlineInsideDirectiveContentOfATextBlockReportsTheDirectiveDiagnostic()
    {
        GeneratorRunResult result = RunGenerator(
            "RichTextDirectiveBody.crn",
            "<TextBlock>@when IsEnabled { @if value == True { <Bold>x</Bold> } }</TextBlock>",
            out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI006", "RichTextDirectiveBody.crn");
        Assert.Contains(InlineDirectiveMessage, diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void RichTextAnInlineNextToDirectivesReportsTheDirectiveDiagnostic()
    {
        GeneratorRunResult result = RunGenerator(
            "RichTextDirectiveSibling.crn",
            "<TextBlock>@when IsEnabled { @if value == True { Foreground = \"Red\"; } }<Bold>x</Bold></TextBlock>",
            out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI006", "RichTextDirectiveSibling.crn");
        Assert.Contains(InlineDirectiveMessage, diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void RichTextAnInlineInsideDirectiveContentOfAPanelIsRejected()
    {
        GeneratorRunResult result = RunGenerator(
            "RichTextPanelDirective.crn",
            "<StackPanel>@when IsEnabled { @if value == True { <Run Text=\"x\" /> } }</StackPanel>",
            out _);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI003" || diagnostic.Id == "CERNEALAUI006");
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void RichTextAnInlineThatHostsDirectivesIsRejected()
    {
        GeneratorRunResult result = RunGenerator(
            "RichTextInlineDirective.crn",
            "<TextBlock><Bold>@when IsEnabled { @if value == True { FontStyle = Italic; } }x</Bold></TextBlock>",
            out _);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI006");
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void RichTextAnInlineInsideATemplateRootIsRejected()
    {
        GeneratorRunResult result = RunGenerator(
            "RichTextTemplateRoot.crn",
            "<Button>@template { <Run Text=\"x\" /> }</Button>",
            out _);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Empty(result.GeneratedSources);
    }

    // The binding helpers attach to a UIElement owner, and an inline is not an element, so a binding on an inline attribute
    // would generate code that does not compile. It must be a diagnostic, not uncompilable output.
    [Theory]
    [InlineData("<Run Text=\"$DataContext.Name\" />")]
    [InlineData("<Bold FontFamily=\"$DataContext.Name\">x</Bold>")]
    [InlineData("<Span FontFamily=\"$DataContext.Name\">x</Span>")]
    public void RichTextABindingOnAnInlineAttributeReportsTheInvalidBindingSourceDiagnostic(string inline)
    {
        const string inputSource = """
            using System.ComponentModel;
            namespace TestInput;

            public sealed class InlineBindingViewModel : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler? PropertyChanged;
                public string Name { get; set; } = "Ana";
            }
            """;
        GeneratorRunResult result = RunGeneratorWithInput(
            "RichTextBinding.crn",
            "<TextBlock DataType=\"TestInput.InlineBindingViewModel\">" + inline + "</TextBlock>",
            inputSource,
            out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI007", "RichTextBinding.crn");
        Assert.Contains("inline elements", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void RichTextAPropertyElementOnAnInlineAssignsTheProperty()
    {
        TextBlock block = Assert.IsType<TextBlock>(CreateRichFixture(
            "RichTextPropertyElement",
            "<TextBlock><Bold><Bold.Foreground><SolidColorBrush Color=\"#FF0000\" /></Bold.Foreground>x</Bold></TextBlock>"));

        Bold bold = Assert.IsType<Bold>(Assert.Single(block.Inlines));
        SolidColorBrush brush = Assert.IsType<SolidColorBrush>(bold.Foreground);
        Assert.Equal(new Color(255, 0, 0), brush.Color);
    }

    // The Inlines collection is filled from the content of the element; a property element for it is not supported and is
    // reported instead of generating code.
    [Theory]
    [InlineData("<TextBlock><TextBlock.Inlines><Run Text=\"x\" /></TextBlock.Inlines></TextBlock>")]
    [InlineData("<TextBlock><Span><Span.Inlines><Run Text=\"x\" /></Span.Inlines></Span></TextBlock>")]
    public void RichTextAnInlinesPropertyElementIsReportedNotGenerated(string markup)
    {
        GeneratorRunResult result = RunGenerator("RichTextInlinesPropertyElement.crn", markup, out _);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI002" || diagnostic.Id == "CERNEALAUI003");
        Assert.Empty(result.GeneratedSources);
    }
    private static UIElement CreateRichFixture(string name, string markup, string? inputSource = null)
    {
        GeneratorRunResult result = inputSource is null
            ? RunGenerator(name + ".crn", markup, out Compilation compilation)
            : RunGeneratorWithInput(name + ".crn", markup, inputSource, out compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        return InvokeCreate(stream, "Cerneala.GeneratedUi." + name + "Factory");
    }

    private static string DescribeInlines(IEnumerable<Inline> inlines) =>
        string.Join(",", inlines.Select(DescribeInline));

    private static string DescribeInline(Inline inline) => inline switch
    {
        Run run => "Run(\"" + run.Text.Replace("\n", "\\n") + "\")",
        LineBreak => "LineBreak",
        Span span => span.GetType().Name + "[" + DescribeInlines(span.Inlines) + "]",
        _ => inline.GetType().Name
    };
}
