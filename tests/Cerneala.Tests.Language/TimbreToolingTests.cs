using Cerneala.Language.Features;
using Cerneala.Language.Semantics;
using Cerneala.Language.Text;

namespace Cerneala.Tests.Language;

// Editor features for TimbreClip, @timbre and the Timbre commands, served from
// the same bound model that the generator consumes.
public sealed class TimbreToolingTests
{
    private const string Caret = "|caret|";

    private const string Clips = """
        <TimbreClip Name="Tone">@parameter Cut: float = 900; @sound Tone { Source = "audio/tone.wav"; @modifier LowPass { Cutoff = Cut; } }</TimbreClip>
        <MotionClip Name="Pulse" TargetType="Button">@animate { @to { Opacity = 0.5; } }</MotionClip>
        """;

    [Theory]
    [InlineData("<TimbreClip Name=\"T\">\n  @sound A { |caret| }</TimbreClip>", new[] { "AutoPlay", "Loop", "Source", "Volume" })]
    [InlineData("<TimbreClip Name=\"T\">@|caret|</TimbreClip>", new[] { "@parameter", "@sound" })]
    [InlineData("<TimbreClip Name=\"T\">@sound A { Source = \"a.wav\"; @|caret| }</TimbreClip>", new[] { "@modifier" })]
    [InlineData("<TimbreClip Name=\"T\">@sound A { Source = \"a.wav\"; @modifier |caret| }</TimbreClip>", new[] { "Delay", "LowPass" })]
    [InlineData("<TimbreClip Name=\"T\">@sound A { Source = \"a.wav\"; @modifier Delay { |caret| } }</TimbreClip>", new[] { "Feedback", "Mix", "Time" })]
    [InlineData("<TimbreClip Name=\"T\">@parameter Cut: float = 900; @sound A { Source = \"a.wav\"; @modifier LowPass { Cutoff = |caret| } }</TimbreClip>", new[] { "Cut" })]
    [InlineData("<TimbreClip Name=\"T\">@sound A { Source = \"a.wav\"; Loop = |caret| }</TimbreClip>", new[] { "false", "true" })]
    [InlineData("<TimbreClip Name=\"T\">@sound A { Source = \"a.wav\"; AutoPlay = |caret| }</TimbreClip>", new[] { "false", "true" })]
    [InlineData("<TimbreClip Name=\"T\">@parameter Cut: |caret|</TimbreClip>", new[] { "float" })]
    public void TimbreClipBodyCompletesPropertiesDirectivesModifiersAndParameters(string clip, string[] expected)
    {
        Assert.Equal(expected, Labels("<Border><Border.Resources>" + clip + "</Border.Resources></Border>"));
    }

    [Theory]
    [InlineData("@timbre $|caret|", new[] { "$Tone" })]
    [InlineData("@timbre $Tone(|caret|)", new[] { "Cut" })]
    [InlineData("@timbre { @parameter Gain: float = 900; @sound A { Source = \"a.wav\"; @modifier LowPass { Cutoff = |caret| } } }", new[] { "Gain" })]
    [InlineData("@timbre $Tone; @on Click { @play |caret| }", new[] { "$self.timbre.Tone" })]
    [InlineData("@timbre $Tone; @on Click { @pause $self.timbre.|caret| }", new[] { "Tone" })]
    [InlineData("@timbre $Tone; @on Click { @resume $self.timbre.To|caret| }", new[] { "Tone" })]
    [InlineData("@timbre $Tone; @on Click { @seek $self.timbre.Tone |caret| }", new[] { "to" })]
    [InlineData("@timbre $Tone; @on Click { @seek $self.timbre.Tone to |caret| }", new[] { "0s", "30s", "500ms" })]
    public void TimbreAttachmentsAndCommandsCompleteClipsParametersSoundsAndSeekTargets(string aspect, string[] expected)
    {
        Assert.Equal(expected, Labels(Button(aspect)));
    }

    [Fact]
    public void AspectBodiesOfferTimbreKeywordsAndResourcesOfferTimbreClip()
    {
        IReadOnlyList<string> commands = Labels(Button("@on Click { @|caret| }"));
        Assert.Contains("@play", commands);
        Assert.Contains("@stop", commands);
        Assert.Contains("@pause", commands);
        Assert.Contains("@resume", commands);
        Assert.Contains("@seek", commands);
        Assert.Contains("@animate", commands);
        Assert.DoesNotContain("@timbre", commands);
        IReadOnlyList<string> top = Labels(Button("@|caret|"));
        Assert.Contains("@timbre", top);
        Assert.Contains("@prism", top);
        Assert.DoesNotContain("@play", top);
        Assert.Contains("TimbreClip", Labels("<Border><Border.Resources><|caret|</Border.Resources></Border>"));
    }

    [Fact]
    public void TimbreClipCallSignatureListsClipParameters()
    {
        string marked = Button("@timbre $Tone(|caret|)");
        int offset = marked.IndexOf(Caret, StringComparison.Ordinal);
        string text = marked.Replace(Caret, string.Empty, StringComparison.Ordinal);
        CernealaSemanticModel model = LanguagePipelineHarness.BindSemanticModel("Signature.crn", text, out IDisposable lifetime);
        using (lifetime)
        {
            CernealaSignatureHelp help = Assert.IsType<CernealaSignatureHelp>(new CernealaCompletionService().GetSignatureHelp(model.Document, offset, model));
            Assert.Equal("Tone(Cut)", Assert.Single(help.Signatures).Label);
            Assert.Equal(0, help.ActiveParameter);
        }
    }

    [Fact]
    public void TimbreReferencesNavigateToTheirDeclarationsAndAreTokenized()
    {
        string text = Button("@timbre $Tone(Cut = 400); @on Click { @play $self.timbre.Tone; @pause $self.timbre.Tone; }");
        CernealaSemanticModel model = LanguagePipelineHarness.BindSemanticModel("Navigation.crn", text, out IDisposable lifetime);
        using (lifetime)
        {
            Assert.Empty(model.Diagnostics);
            CernealaNavigationService navigation = new();
            AssertDefinition(navigation, model, text, Last(text, "$Tone") + 1, First(text, "\"Tone\"") + 1, "Tone");
            AssertDefinition(navigation, model, text, Last(text, "timbre.Tone") + "timbre.".Length, First(text, "@sound Tone") + "@sound ".Length, "Tone");
            AssertDefinition(navigation, model, text, Last(text, "Cut"), First(text, "Cut"), "Cut");
            AssertDefinition(navigation, model, text, First(text, "= Cut") + 2, First(text, "Cut"), "Cut");

            CernealaSemanticToken[] tokens = new CernealaStructureService().GetSemanticTokens(model.Document, model).ToArray();
            Assert.Equal("Keyword", TokenAt(tokens, text, First(text, "@timbre")));
            Assert.Equal("Keyword", TokenAt(tokens, text, First(text, "@play")));
            Assert.Equal("Keyword", TokenAt(tokens, text, First(text, "@sound")));
            Assert.Equal("Keyword", TokenAt(tokens, text, First(text, "@modifier")));
            Assert.Equal("Function", TokenAt(tokens, text, First(text, "LowPass")));
            Assert.Equal("Parameter", TokenAt(tokens, text, Last(text, "Cut")));
            Assert.Equal("Property", TokenAt(tokens, text, First(text, "Source")));
            Assert.Equal("Variable", TokenAt(tokens, text, Last(text, "timbre.Tone") + "timbre.".Length));
        }
    }

    private static void AssertDefinition(
        CernealaNavigationService navigation,
        CernealaSemanticModel model,
        string text,
        int offset,
        int expectedStart,
        string expectedText)
    {
        CernealaLocation definition = Assert.Single(navigation.GetDefinitions(model, offset));
        Assert.Equal(expectedStart, definition.Span.Start);
        Assert.Equal(expectedText, text.Substring(definition.Span.Start, definition.Span.Length));
    }

    private static string TokenAt(IEnumerable<CernealaSemanticToken> tokens, string text, int offset) =>
        Assert.Single(tokens, token => token.Span.Start == offset).Kind.ToString();

    private static int First(string text, string value) => text.IndexOf(value, StringComparison.Ordinal);

    private static int Last(string text, string value) => text.LastIndexOf(value, StringComparison.Ordinal);

    private static IReadOnlyList<string> Labels(string marked)
    {
        int offset = marked.IndexOf(Caret, StringComparison.Ordinal);
        string text = marked.Replace(Caret, string.Empty, StringComparison.Ordinal);
        CernealaSemanticModel model = LanguagePipelineHarness.BindSemanticModel("Completion.crn", text, out IDisposable lifetime);
        using (lifetime)
        {
            return new CernealaCompletionService()
                .GetCompletions(model.Document, model, offset)
                .Select(item => item.Label)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(label => label, StringComparer.Ordinal)
                .ToArray();
        }
    }

    private static string Button(string aspect) =>
        "<Button Content=\"Play\"><Button.Resources>" + Clips + "</Button.Resources><Button.Aspect>" + aspect + "</Button.Aspect></Button>";
}
