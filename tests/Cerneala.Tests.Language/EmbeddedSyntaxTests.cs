using Cerneala.Language.Diagnostics;
using Cerneala.Language.Prism.Catalog;
using Cerneala.Language.Semantics;
using Cerneala.Language.Syntax.Embedded;
using Cerneala.Language.Text;

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

    // Mirrors UiMarkupGenerator.ParseMarkupExpression: bool targets take any value with '$'
    // that is not one whole binding token; string targets only '$'-leading values with an
    // operator; other targets any parsed value with an operator.
    [Theory]
    [InlineData("$DataContext.A and $DataContext.B", true, false, "Expression")]
    [InlineData("($DataContext.A)", true, false, "Expression")]
    [InlineData("IsFocused and $DataContext.B", true, false, "Expression")]
    [InlineData("$DataContext.Count &gt; 0", true, false, "Expression")]
    [InlineData("  $DataContext.A or $self.IsFocused  ", true, false, "Expression")]
    [InlineData("$DataContext.A", true, false, "None")]
    [InlineData("$DataContext.A:TwoWay", true, false, "None")]
    [InlineData("$DataContext.A:Two", true, false, "None")]
    [InlineData("$Accent", true, false, "None")]
    [InlineData("No binding", true, false, "None")]
    [InlineData("$DataContext.A and", true, false, "Rejected")]
    [InlineData("Hello $DataContext.Name", true, false, "Rejected")]
    [InlineData("$DataContext.A:TwoWay and $DataContext.B", true, false, "Rejected")]
    [InlineData("($DataContext.A and $DataContext.B):OneWay", true, false, "Rejected")]
    [InlineData("($DataContext.A)", false, false, "None")]
    [InlineData("IsFocused and $DataContext.B", false, false, "NonBooleanTarget")]
    [InlineData("$DataContext.Count &lt; 3", false, false, "NonBooleanTarget")]
    [InlineData("IsFocused and $DataContext.B", false, true, "None")]
    [InlineData("Hello $DataContext.Name", false, true, "None")]
    [InlineData("$DataContext.A and $DataContext.B", false, true, "NonBooleanTarget")]
    [InlineData("$DataContext.A and", false, false, "None")]
    public void PropertyExpressionShapeMatchesTheGeneratorDetectionRule(
        string text,
        bool booleanTarget,
        bool stringTarget,
        string expected)
    {
        BindingExpressionSyntax syntax = BindingExpressionSyntaxParser.Parse(text);

        Assert.Equal(expected, syntax.Classify(booleanTarget, stringTarget).ToString());
    }

    [Fact]
    public void PropertyExpressionSourcesKeepDocumentSpansAcrossEntitiesAndWhitespace()
    {
        const string text = "  $DataContext.Count &lt; 3 or $self.IsFocused ";
        BindingExpressionSyntax syntax = BindingExpressionSyntaxParser.Parse(text, absoluteOffset: 40);

        Assert.Equal("$DataContext.Count < 3 or $self.IsFocused", syntax.Text);
        Assert.Equal(42, syntax.Span.Start);
        Assert.Equal(text.TrimEnd().Length - 2, syntax.Span.Length);
        Assert.Collection(
            syntax.Sources,
            source =>
            {
                Assert.Equal("$DataContext.Count", source.Text);
                Assert.Equal(42, source.Span.Start);
            },
            source =>
            {
                Assert.Equal("$self.IsFocused", source.Text);
                Assert.Equal(40 + text.IndexOf("$self", StringComparison.Ordinal), source.Span.Start);
                Assert.Equal(["self", "IsFocused"], source.Segments.Select(segment => segment.Name));
            });
    }

    [Fact]
    public void GeneralDirectiveGrammarFindsTemplatesConditionsAssignmentsAndComparators()
    {
        const string text = "@template { <Border /> } @templates { <ContentTemplate><TextBlock /></ContentTemplate> } @when Value { @if value <= 4 and Name != \"x\" { Opacity = 0.5; } @default { Opacity = 1; } }";
        EmbeddedParseResult<DirectiveDocumentSyntax> result = DirectiveSyntaxParser.Parse(text, absoluteOffset: 31);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(["@template", "@templates", "@when", "@if", "@default"], result.Syntax.Directives.Select(item => item.Keyword));
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

    [Theory]
    [InlineData("null", (int)PrismValueModelKind.NullLiteral)]
    [InlineData("$Image", (int)PrismValueModelKind.ResourceReference)]
    [InlineData("$self.Opacity", (int)PrismValueModelKind.DirectReference)]
    [InlineData("$self.Opacity:OneWay", (int)PrismValueModelKind.Binding)]
    [InlineData("#FF123456", (int)PrismValueModelKind.ColorLiteral)]
    [InlineData("\"literal\"", (int)PrismValueModelKind.StringLiteral)]
    [InlineData("'literal'", (int)PrismValueModelKind.StringLiteral)]
    [InlineData("\"$self.Opacity\"", (int)PrismValueModelKind.StringLiteral)]
    [InlineData("(1, 2)", (int)PrismValueModelKind.TupleLiteral)]
    [InlineData("true", (int)PrismValueModelKind.BooleanLiteral)]
    [InlineData("FALSE", (int)PrismValueModelKind.BooleanLiteral)]
    [InlineData("-12", (int)PrismValueModelKind.NumberLiteral)]
    [InlineData("1.25e-3", (int)PrismValueModelKind.NumberLiteral)]
    [InlineData("Radius", (int)PrismValueModelKind.Identifier)]
    [InlineData("", (int)PrismValueModelKind.Identifier)]
    public void PrismModelValuesPreserveClassificationTextAndAbsoluteSpan(string value, int expectedKind)
    {
        const string prefix = "Value = ";
        EmbeddedParseResult<PrismClipModelSyntax> parsed =
            PrismSyntaxParser.ParseComposition(prefix + value + ";", absoluteOffset: 17);

        Assert.Empty(parsed.Diagnostics);
        PrismValueModelSyntax actual = Assert.IsType<PrismAssignmentModelSyntax>(
            Assert.Single(parsed.Syntax.Members)).Value;
        Assert.Equal((PrismValueModelKind)expectedKind, actual.Kind);
        Assert.Equal(value, actual.Text);
        Assert.Equal(17 + prefix.Length, actual.Span.Start);
        Assert.Equal(value.Length, actual.Span.Length);
    }

    [Fact]
    public void PrismCompositionTerminatesOnALoneClosingBrace() => AssertPrismCompositionTerminates("}");

    [Fact]
    public void PrismCompositionTerminatesOnAClosingBraceAfterABalancedLayer() =>
        AssertPrismCompositionTerminates("@layer A { } }");

    private static void AssertPrismCompositionTerminates(string text)
    {
        // Warm up the JIT with an input that terminates, so the 500 ms bound measures the loop, not compilation.
        EmbeddedParseResult<PrismClipModelSyntax> warm = PrismSyntaxParser.ParseComposition("@layer A { }", 0);
        Assert.NotNull(warm);

        EmbeddedParseResult<PrismClipModelSyntax>? result = null;
        Thread worker = new(() => result = PrismSyntaxParser.ParseComposition(text, 0)) { IsBackground = true };
        worker.Start();
        if (!worker.Join(TimeSpan.FromMilliseconds(500)))
        {
            Environment.FailFast(
                "PrismSyntaxParser.ParseComposition did not return within 500 ms for the input \"" + text + "\".");
        }

        Assert.NotNull(result);
        Assert.Contains(result!.Diagnostics, diagnostic => diagnostic.Id == "PRISM1003");
        Assert.True(result.Diagnostics.Count < 10, "Diagnostic count was " + result.Diagnostics.Count + ".");
    }

    // The string values are: a, backslash, backslash (an escaped backslash); a (no escape); and a, backslash,
    // quote, b (an escaped quote, which does not close the string).
    [Theory]
    [InlineData("\"a\\\\\"")]
    [InlineData("\"a\"")]
    [InlineData("\"a\\\"b\"")]
    public void DirectiveAssignmentValueEndsAtTheClosingQuote(string value)
    {
        string text = "@default { Text = " + value + "; Opacity = 1; }";
        EmbeddedParseResult<DirectiveDocumentSyntax> result = DirectiveSyntaxParser.Parse(text);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(["Text", "Opacity"], result.Syntax.Assignments.Select(assignment => assignment.Name));
        TextSpan span = result.Syntax.Assignments[0].ValueSpan;
        Assert.Equal(value, text.Substring(span.Start, span.Length));
    }

    // Every scanner that skips quoted directive values must close the string at the quote after an escaped backslash.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AspectAttachmentScannerClosesTheStringAfterAnEscapedBackslash(bool escaped)
    {
        string text = "Text = \"a" + (escaped ? "\\\\" : string.Empty) + "\"; @prism $Fx;";

        IReadOnlyList<AspectAttachmentSyntax> attachments = AspectAttachmentScanner.Scan(text, 0);

        Assert.True(attachments.Count == 1, $"escaped={escaped}: {attachments.Count} @prism attachment(s) found in {text} (expected 1).");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AspectAttachmentBlockAndStatementEndAfterAnEscapedBackslash(bool escaped)
    {
        string value = "\"a" + (escaped ? "\\\\" : string.Empty) + "\"";
        string block = "@prism { Value = " + value + "; }";
        string statement = "@timbre $Clip(Path = " + value + ");";
        string text = block + " " + statement + " Opacity = 1;";

        IReadOnlyList<AspectAttachmentSyntax> attachments = AspectAttachmentScanner.Scan(text, 0);

        Assert.True(
            attachments.Count == 2 &&
            attachments[0].Extent.Length == block.Length && attachments[0].Terminated &&
            attachments[1].Extent.Length == statement.Length && attachments[1].Terminated,
            $"escaped={escaped}: attachments {string.Join(" | ", attachments.Select(a => a.Keyword + " " + a.Extent + " terminated=" + a.Terminated))} in {text}.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrismReadValueClosesTheStringAfterAnEscapedBackslash(bool escaped)
    {
        string text = "Value = \"a" + (escaped ? "\\\\" : string.Empty) + "\"; Other = 1;";

        EmbeddedParseResult<PrismClipModelSyntax> parsed = PrismSyntaxParser.ParseComposition(text, 0);

        Assert.True(
            parsed.Syntax.Members.Count == 2,
            $"escaped={escaped}: {parsed.Syntax.Members.Count} member(s) parsed from {text} (expected 2); diagnostics: {string.Join(" | ", parsed.Diagnostics.Select(d => d.Id + " " + d.Message))}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrismSkipQuotedClosesTheStringAfterAnEscapedBackslash(bool escaped)
    {
        string text = "\"a" + (escaped ? "\\\\" : string.Empty) + "\" @prism $P;";

        EmbeddedParseResult<IReadOnlyList<PrismApplicationModelSyntax>> parsed = PrismSyntaxParser.ParseApplications(text, 0);

        Assert.True(parsed.Syntax.Count == 1, $"escaped={escaped}: {parsed.Syntax.Count} @prism application(s) found in {text} (expected 1).");
    }

    // FindDelimiter, FindMatchingBrace and SplitTopLevel are private static helpers of CernealaSemanticModel, called by reflection.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MotionFindDelimiterAndFindMatchingBraceCloseTheStringAfterAnEscapedBackslash(bool escaped)
    {
        string body = "Text = \"a" + (escaped ? "\\\\" : string.Empty) + "\";";
        string text = "{ " + body + " } tail;";
        System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
        System.Reflection.MethodInfo findDelimiter = typeof(CernealaSemanticModel).GetMethod("FindDelimiter", flags)!;
        System.Reflection.MethodInfo findMatchingBrace = typeof(CernealaSemanticModel).GetMethod("FindMatchingBrace", flags)!;

        int delimiter = (int)findDelimiter.Invoke(null, [text, 2, ';'])!;
        int brace = (int)findMatchingBrace.Invoke(null, [text, 0])!;

        Assert.True(
            delimiter == 2 + body.Length - 1 && brace == text.IndexOf('}'),
            $"escaped={escaped}: FindDelimiter={delimiter} (expected {2 + body.Length - 1}), FindMatchingBrace={brace} (expected {text.IndexOf('}')}) for {text}.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MotionSplitTopLevelClosesTheStringAfterAnEscapedBackslash(bool escaped)
    {
        string first = "Text = \"a" + (escaped ? "\\\\" : string.Empty) + "\"";
        string text = first + ", Opacity = 1";
        System.Reflection.MethodInfo splitTopLevel = typeof(CernealaSemanticModel).GetMethod(
            "SplitTopLevel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        string[] parts = ((IEnumerable<string>)splitTopLevel.Invoke(null, [text, ','])!).ToArray();

        Assert.True(
            parts.SequenceEqual([first, "Opacity = 1"]),
            $"escaped={escaped}: SplitTopLevel returned [{string.Join(" | ", parts)}] for {text}.");
    }

    // FindDirectiveBlocks decides which @default block an assignment belongs to.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AspectDefaultBlockEndsAtItsClosingBraceAfterAnEscapedBackslash(bool escaped)
    {
        string text = "a" + (escaped ? "\\\\" : string.Empty);
        string markup = "<TextBlock Aspect=\"$A\"><TextBlock.Resources><Aspect Name=\"A\" TargetType=\"TextBlock\">" +
            "@default { Text = \"" + text + "\"; Opacity = 1; } @default { Opacity = 0.5; }</Aspect></TextBlock.Resources></TextBlock>";
        CernealaSemanticModel model = LanguagePipelineHarness.BindSemanticModel("DefaultBlocks.crn", markup, out IDisposable lifetime);
        using (lifetime)
        {
            string[] duplicates = model.Diagnostics.Where(d => d.Message.Contains("more than once", StringComparison.Ordinal)).Select(d => d.Message).ToArray();

            Assert.True(duplicates.Length == 0, $"escaped={escaped}: false duplicate diagnostics: {string.Join(" | ", duplicates)}");
        }
    }
}
