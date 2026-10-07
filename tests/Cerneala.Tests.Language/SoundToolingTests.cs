using Cerneala.Language.Features;
using Cerneala.Language.Semantics;
using Cerneala.Language.Text;

namespace Cerneala.Tests.Language;

// Editor features for SoundClip and Sound actions, served from the same bound
// model that the generator consumes.
public sealed class SoundToolingTests
{
    private const string Caret = "|caret|";

    private const string Clips = """
        <SoundClip Name="Tone">Source = "audio/tone.wav"; @parameter Cut: float = 900; @modifier LowPass { Cutoff = Cut; }</SoundClip>
        <MotionClip Name="Pulse" TargetType="Button">@animate { @to { Opacity = 0.5; } }</MotionClip>
        """;

    [Theory]
    [InlineData("<SoundClip Name=\"T\">\n  |caret|</SoundClip>", new[] { "Loop", "Source", "Volume" })]
    [InlineData("<SoundClip Name=\"T\">Source = \"a.wav\"; @|caret|</SoundClip>", new[] { "@modifier", "@parameter" })]
    [InlineData("<SoundClip Name=\"T\">Source = \"a.wav\"; @modifier |caret|</SoundClip>", new[] { "Delay", "LowPass" })]
    [InlineData("<SoundClip Name=\"T\">Source = \"a.wav\"; @modifier Delay { |caret| }</SoundClip>", new[] { "Feedback", "Mix", "Time" })]
    [InlineData("<SoundClip Name=\"T\">Source = \"a.wav\"; @parameter Cut: float = 900; @modifier LowPass { Cutoff = |caret| }</SoundClip>", new[] { "Cut" })]
    [InlineData("<SoundClip Name=\"T\">Source = \"a.wav\"; Loop = |caret|</SoundClip>", new[] { "false", "true" })]
    [InlineData("<SoundClip Name=\"T\">Source = \"a.wav\"; @parameter Cut: |caret|</SoundClip>", new[] { "float" })]
    public void SoundClipBodyCompletesPropertiesDirectivesModifiersAndParameters(string clip, string[] expected)
    {
        Assert.Equal(expected, Labels("<Border><Border.Resources>" + clip + "</Border.Resources></Border>"));
    }

    [Theory]
    [InlineData("@on Click { @sound $|caret| }", new[] { "$Tone" })]
    [InlineData("@on Click { @sound $Tone(|caret|) }", new[] { "Cut", "Loop", "Volume" })]
    [InlineData("@on Click { @sound $Tone(Volume = 0.2, Loop = |caret|) }", new[] { "false", "true" })]
    [InlineData("@handle Playback; @on Click { @sound $Tone as |caret| }", new[] { "Playback" })]
    [InlineData("@handle Playback; @on Click { @pause |caret| }", new[] { "Playback" })]
    [InlineData("@handle Playback; @on Click { @resume Pla|caret| }", new[] { "Playback" })]
    [InlineData("@handle Playback; @on Click { @seek Playback |caret| }", new[] { "to" })]
    [InlineData("@handle Playback; @on Click { @seek Playback to |caret| }", new[] { "0s", "30s", "500ms" })]
    public void SoundActionsCompleteClipsArgumentsHandlesAndSeekTargets(string aspect, string[] expected)
    {
        Assert.Equal(expected, Labels(Button(aspect)));
    }

    [Fact]
    public void ActionBodiesOfferSoundKeywordsAndResourcesOfferSoundClip()
    {
        IReadOnlyList<string> keywords = Labels(Button("@on Click { @|caret| }"));
        Assert.Contains("@sound", keywords);
        Assert.Contains("@pause", keywords);
        Assert.Contains("@resume", keywords);
        Assert.Contains("@seek", keywords);
        Assert.Contains("@animate", keywords);
        Assert.Contains("SoundClip", Labels("<Border><Border.Resources><|caret|</Border.Resources></Border>"));
    }

    [Fact]
    public void SoundClipCallSignatureListsOverridableValues()
    {
        string marked = Button("@on Click { @sound $Tone(Volume = 0.2, |caret|) }");
        int offset = marked.IndexOf(Caret, StringComparison.Ordinal);
        string text = marked.Replace(Caret, string.Empty, StringComparison.Ordinal);
        CernealaSemanticModel model = LanguagePipelineHarness.BindSemanticModel("Signature.crn", text, out IDisposable lifetime);
        using (lifetime)
        {
            CernealaSignatureHelp help = Assert.IsType<CernealaSignatureHelp>(new CernealaCompletionService().GetSignatureHelp(model.Document, offset, model));
            Assert.Equal("Tone(Volume, Loop, Cut)", Assert.Single(help.Signatures).Label);
            Assert.Equal(1, help.ActiveParameter);
        }
    }

    [Fact]
    public void SoundReferencesNavigateToTheirDeclarationsAndAreTokenized()
    {
        string text = Button("@handle Playback; @on Click { @sound $Tone(Cut = 400) as Playback; @pause Playback; }");
        CernealaSemanticModel model = LanguagePipelineHarness.BindSemanticModel("Navigation.crn", text, out IDisposable lifetime);
        using (lifetime)
        {
            Assert.Empty(model.Diagnostics);
            CernealaNavigationService navigation = new();
            AssertDefinition(navigation, model, text, Last(text, "$Tone") + 1, First(text, "\"Tone\"") + 1, "Tone");
            AssertDefinition(navigation, model, text, Last(text, "Playback"), First(text, "Playback"), "Playback");
            AssertDefinition(navigation, model, text, Last(text, "Cut"), First(text, "Cut"), "Cut");
            AssertDefinition(navigation, model, text, First(text, "= Cut") + 2, First(text, "Cut"), "Cut");

            CernealaSemanticToken[] tokens = new CernealaStructureService().GetSemanticTokens(model.Document, model).ToArray();
            Assert.Equal("Keyword", TokenAt(tokens, text, First(text, "@sound")));
            Assert.Equal("Keyword", TokenAt(tokens, text, First(text, "@modifier")));
            Assert.Equal("Function", TokenAt(tokens, text, First(text, "LowPass")));
            Assert.Equal("Parameter", TokenAt(tokens, text, Last(text, "Cut")));
            Assert.Equal("Property", TokenAt(tokens, text, First(text, "Source")));
            Assert.Equal("Label", TokenAt(tokens, text, Last(text, "Playback")));
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
