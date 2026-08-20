using Cerneala.Language.Diagnostics;
using Cerneala.Language.Prism.Catalog;
using Cerneala.Language.Syntax.Embedded;

namespace Cerneala.Tests.Language;

public sealed class EmbeddedSyntaxTests
{
    [Fact]
    public void BindingGrammarProducesTypedSegmentsModesAndAbsoluteSpans()
    {
        EmbeddedParseResult<BindingValueSyntax> direct = BindingSyntaxParser.Parse(
            "$DataContext.Customer.Name:TwoWay",
            absoluteOffset: 17);

        Assert.Empty(direct.Diagnostics);
        Assert.Equal(BindingValueKind.Direct, direct.Syntax.Kind);
        Assert.Equal(BindingModeSyntax.TwoWay, direct.Syntax.Binding!.Mode);
        Assert.Equal(["DataContext", "Customer", "Name"], direct.Syntax.Binding.Segments.Select(segment => segment.Name));
        Assert.Equal(17, direct.Syntax.Binding.Span.Start);
        Assert.Equal(17 + "$DataContext.Customer.Name".Length, direct.Syntax.Binding.ModeSpan.Start);
    }

    [Fact]
    public void InterpolationKeepsLiteralAndBindingFragmentsWithoutEmitterTypes()
    {
        EmbeddedParseResult<BindingValueSyntax> result = BindingSyntaxParser.Parse(
            "Hello $DataContext.Name, \\$cash",
            absoluteOffset: 100);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(BindingValueKind.Interpolation, result.Syntax.Kind);
        BindingFragmentSyntax binding = Assert.Single(result.Syntax.Fragments.OfType<BindingFragmentSyntax>());
        Assert.Equal("$DataContext.Name", binding.Binding.Text);
        Assert.Equal(106, binding.Span.Start);
    }

    [Theory]
    [InlineData("$DataContext.")]
    [InlineData("$DataContext.Name:")]
    [InlineData("Hello $DataContext.Name:TwoWay")]
    public void BindingRecoveryReportsOnePrimaryDiagnostic(string text)
    {
        EmbeddedParseResult<BindingValueSyntax> result = BindingSyntaxParser.Parse(text, absoluteOffset: 9);

        Assert.Equal(BindingValueKind.Invalid, result.Syntax.Kind);
        Assert.Single(result.Diagnostics);
        Assert.Equal("CERNEALAUI007", result.Diagnostics[0].Id);
        Assert.InRange(result.Diagnostics[0].Span.Start, 9, 9 + text.Length);
    }

    [Fact]
    public void GeneralDirectiveGrammarFindsTemplatesConditionsAssignmentsAndComparators()
    {
        const string text = "@template { <Border /> } @when Value { @if value <= 4 and Name != \"x\" { Opacity = 0.5; } @default { Opacity = 1; } }";
        EmbeddedParseResult<DirectiveDocumentSyntax> result = DirectiveSyntaxParser.Parse(text, absoluteOffset: 31);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(["@template", "@when", "@if", "@default"], result.Syntax.Directives.Select(item => item.Keyword));
        Assert.Equal(2, result.Syntax.Assignments.Count);
        Assert.All(result.Syntax.Directives, directive => Assert.True(directive.Span.Start >= 31));
    }

    [Fact]
    public void MotionGrammarOwnsEveryCurrentDirective()
    {
        string text = string.Join(" ", new[]
        {
            "@when", "@if", "@on", "@presence", "@layout", "@scroll", "@drag", "@gesture",
            "@set", "@animate", "@keyframes", "@stagger", "@parallel", "@sequence", "@run",
            "@cancel", "@handle", "@parameter", "@from", "@to"
        });
        EmbeddedParseResult<DirectiveDocumentSyntax> result = MotionSyntaxParser.Parse(text);

        Assert.DoesNotContain(
            result.Diagnostics,
            diagnostic => diagnostic.Message.StartsWith("Unknown", StringComparison.Ordinal));
        Assert.Equal(20, result.Syntax.Directives.Count);
    }

    [Theory]
    [InlineData("@layout id $self.Tag with Tween(100ms, Linear)")]
    [InlineData("@drag with Spring(500, 40)")]
    [InlineData("@gesture press with Tween(100ms, Linear)")]
    [InlineData("@run $LoadingSequence as Loading")]
    [InlineData("@cancel Loading")]
    [InlineData("@handle Loading")]
    [InlineData("@parameter Delay: Time = 100ms")]
    public void MotionStatementDirectiveWithoutSemicolonProducesSyntaxDiagnostic(string text)
    {
        EmbeddedParseResult<DirectiveDocumentSyntax> result = MotionSyntaxParser.Parse(text);

        EmbeddedDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("CERNEALAUI020", diagnostic.Id);
        Assert.Equal(LanguageDiagnosticSeverity.Error, diagnostic.GetSeverity(AnalysisMode.Editor));
        Assert.Contains("must end with ';'", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MotionAssignmentWithoutSemicolonProducesSyntaxDiagnostic()
    {
        const string text = """
            @to
            {
                $VisualStage.Opacity = 1
            }
            """;
        EmbeddedParseResult<DirectiveDocumentSyntax> result = MotionSyntaxParser.Parse(text);

        EmbeddedDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("CERNEALAUI020", diagnostic.Id);
        Assert.Equal(LanguageDiagnosticSeverity.Error, diagnostic.GetSeverity(AnalysisMode.Editor));
        Assert.Equal("$VisualStage.Opacity = 1", text.Substring(diagnostic.Span.Start, diagnostic.Span.Length));
    }

    [Fact]
    public void MotionDirectiveHeaderOptionDoesNotRequireSemicolon()
    {
        const string text = """
            @on Loaded
            {
                @scroll source $Scroller axis horizontal allowLayout = true
                {
                    Width = 20..80;
                }
            }
            """;

        EmbeddedParseResult<DirectiveDocumentSyntax> result = MotionSyntaxParser.Parse(text);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void PrismGrammarOwnsExactlySevenDirectivesAndRecoversDelimiters()
    {
        const string valid = "@prism { @parameter Radius: Float = 4; @group G { @layer L { @filter Blur { } @style Fill { } @mask { } } } }";
        EmbeddedParseResult<DirectiveDocumentSyntax> parsed = PrismSyntaxParser.Parse(valid, absoluteOffset: 5);
        EmbeddedParseResult<DirectiveDocumentSyntax> incomplete = PrismSyntaxParser.Parse("@prism $Glow(Radius = \"4\"", absoluteOffset: 40);

        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(7, parsed.Syntax.Directives.Select(item => item.Keyword).Distinct(StringComparer.Ordinal).Count());
        EmbeddedDiagnostic diagnostic = Assert.Single(incomplete.Diagnostics);
        Assert.Equal("PRISM1002", diagnostic.Id);
        Assert.True(diagnostic.IsTransient);
        Assert.Equal(LanguageDiagnosticSeverity.Information, diagnostic.GetSeverity(AnalysisMode.Editor));
        Assert.Equal(LanguageDiagnosticSeverity.Error, diagnostic.GetSeverity(AnalysisMode.Build));
    }

    [Theory]
    [InlineData("@when Ready { @if value == True { Opacity = 1; }")]
    [InlineData("@when Name == \"unfinished { Opacity = 1; }")]
    public void DirectiveRecoveryHandlesUnfinishedNestingAndQuotes(string text)
    {
        EmbeddedParseResult<DirectiveDocumentSyntax> result = DirectiveSyntaxParser.Parse(text, absoluteOffset: 12);

        EmbeddedDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.True(diagnostic.IsTransient);
        Assert.InRange(diagnostic.Span.End, 12, 12 + text.Length);
    }

    [Fact]
    public void PrismCatalogLoadsInTheHostAgnosticAssembly()
    {
        PrismLanguageCatalog catalog = PrismLanguageCatalog.LoadDefault();

        Assert.Equal(1, catalog.SchemaVersion);
        Assert.Equal("1.0.0", catalog.CatalogVersion);
        Assert.True(catalog.Symbols.Count > 100);
        Assert.Contains(catalog.Symbols, symbol => symbol.Symbol == "Blur" && symbol.Kind == "filter");
        Assert.All(catalog.Symbols, symbol => Assert.True(symbol.StableId > 0));
    }
}
