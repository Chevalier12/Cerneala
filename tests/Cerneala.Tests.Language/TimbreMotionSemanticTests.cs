using Cerneala.Language.Features;
using Cerneala.Language.Semantics;
using Cerneala.Language.Text;
using Cerneala.Language.Timbre;

namespace Cerneala.Tests.Language;

// `$X.timbre.Sound.Property` Motion targets bind to the sound and the clip
// parameters it uses, never to a property of the element.
public sealed class TimbreMotionSemanticTests
{
    private const string Caret = "|caret|";

    private static readonly Lazy<IReadOnlyList<TimbreCorpusCase>> Corpus = new(() => TimbreCorpus.Parse("timbre-motion-corpus.json"));

    public static IEnumerable<object[]> CorpusCases() =>
        Corpus.Value.Select(item => new object[] { item.Id });

    [Fact]
    public void TimbreMotionCorpusCoversEveryFamily()
    {
        Assert.Equal(Corpus.Value.Count, Corpus.Value.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count());
        foreach (string family in new[] { "Target", "Reference", "Value", "Context" })
        {
            Assert.Contains(Corpus.Value, item => item.Family == family);
        }

        Assert.Contains(Corpus.Value, item => item.Valid);
        Assert.All(Corpus.Value.Where(item => !item.Valid), item => Assert.NotEmpty(item.Expect));
    }

    // Language and SourceGen agree on every case (id, message, span); valid
    // cases generate C# that compiles in an ordinary consumer.
    [Theory]
    [MemberData(nameof(CorpusCases))]
    public void TimbreMotionLanguageAndSourceGeneratorAgreeOnCorpus(string id)
    {
        TimbreCorpusCase item = Corpus.Value.Single(candidate => candidate.Id == id);
        string path = id + ".crn";
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze(path, item.Markup);

        Assert.Empty(result.Syntax.Diagnostics);
        Assert.Equal(Ordered(result.SemanticDiagnostics), Ordered(result.SourceGeneratorDiagnostics));
        if (item.Valid)
        {
            Assert.Empty(result.SemanticDiagnostics);
            Assert.Empty(LanguagePipelineHarness.GeneratedCompilationErrors(path, item.Markup, path + ".cs", null));
            return;
        }

        foreach (TimbreCorpusExpectation expectation in item.Expect)
        {
            Assert.Contains(result.SemanticDiagnostics, diagnostic =>
                diagnostic.Id == expectation.Id &&
                diagnostic.Message.Contains(expectation.Message, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void ClipParametersBelongOnlyToTheSoundsThatUseThem()
    {
        string markup = Button("@on Click { @play $self.timbre.Tone; }");
        CernealaSemanticModel model = LanguagePipelineHarness.BindSemanticModel("Schema.crn", markup, out IDisposable lifetime);
        using (lifetime)
        {
            Assert.Empty(model.Diagnostics);
            BoundTimbreClip clip = Assert.Single(model.Timbre.Clips.Values);
            BoundTimbreParameter cut = Assert.Single(clip.FindSound("Tone")!.Parameters);
            Assert.Equal(("Cut", 20f, 20000f), (cut.Name, cut.Minimum, cut.Maximum));
            Assert.Empty(clip.FindSound("Plain")!.Parameters);
        }
    }

    [Theory]
    [InlineData("@on Click { @animate { @to { $self.timbre.|caret| } } }", new[] { "Plain", "Tone" })]
    [InlineData("@on Click { @animate { @to { $self.timbre.To|caret| } } }", new[] { "Plain", "Tone" })]
    [InlineData("@on Click { @animate { @to { $self.timbre.Tone.|caret| } } }", new[] { "Cut", "Volume" })]
    [InlineData("@on Click { @animate { @to { $self.timbre.Plain.|caret| } } }", new[] { "Volume" })]
    public void TimbreMotionTargetsCompleteSoundsAndParameters(string aspect, string[] expected)
    {
        Assert.Equal(expected, Labels(Button(aspect)));
    }

    [Fact]
    public void TimbreMotionTargetsNavigateToSoundAndParameterDeclarations()
    {
        string text = Button("@on Click { @play $self.timbre.Tone; @animate { @to { $self.timbre.Tone.Cut = 300; $self.timbre.Tone.Volume = 0.5; } } }");
        CernealaSemanticModel model = LanguagePipelineHarness.BindSemanticModel("TimbreMotionNavigation.crn", text, out IDisposable lifetime);
        using (lifetime)
        {
            Assert.Empty(model.Diagnostics);
            CernealaNavigationService navigation = new();
            int sound = text.IndexOf("timbre.Tone.Cut", StringComparison.Ordinal) + "timbre.".Length;
            CernealaLocation soundDefinition = Assert.Single(navigation.GetDefinitions(model, sound));
            Assert.Equal(text.IndexOf("@sound Tone", StringComparison.Ordinal) + "@sound ".Length, soundDefinition.Span.Start);
            int parameter = text.IndexOf("Tone.Cut", StringComparison.Ordinal) + "Tone.".Length;
            CernealaLocation parameterDefinition = Assert.Single(navigation.GetDefinitions(model, parameter));
            Assert.Equal(text.IndexOf("Cut", StringComparison.Ordinal), parameterDefinition.Span.Start);
        }
    }

    private static IEnumerable<HarnessDiagnostic> Ordered(IEnumerable<HarnessDiagnostic> diagnostics) => diagnostics
        .OrderBy(diagnostic => diagnostic.StartLine)
        .ThenBy(diagnostic => diagnostic.StartCharacter)
        .ThenBy(diagnostic => diagnostic.Id, StringComparer.Ordinal)
        .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal);

    private static IReadOnlyList<string> Labels(string marked)
    {
        int offset = marked.IndexOf(Caret, StringComparison.Ordinal);
        string text = marked.Replace(Caret, string.Empty, StringComparison.Ordinal);
        CernealaSemanticModel model = LanguagePipelineHarness.BindSemanticModel("TimbreMotionCompletion.crn", text, out IDisposable lifetime);
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
        "<Button Content=\"Play\"><Button.Resources>" +
        "<TimbreClip Name=\"Sounds\">@parameter Cut: float = 900; " +
        "@sound Tone { Source = \"audio/tone.wav\"; @modifier LowPass { Cutoff = Cut; } } " +
        "@sound Plain { Source = \"audio/plain.wav\"; }</TimbreClip>" +
        "</Button.Resources><Button.Aspect>@timbre $Sounds; " + aspect + "</Button.Aspect></Button>";

    private const string ApprovedExample = """
        <Button Content="Confirmă">
          <Button.Resources>
            <TimbreClip Name="ConfirmTimbre">
              @parameter ToneCutoff: float = 1200;
              @parameter EchoMix: float = 0.15;
              @sound Confirm
              {
                  Source = "audio/confirm.wav";
                  Volume = 0.8;
                  @modifier LowPass { Cutoff = ToneCutoff; }
                  @modifier Delay { Time = 120ms; Feedback = 0.20; Mix = EchoMix; }
              }
            </TimbreClip>
          </Button.Resources>
          <Button.Aspect>
            @timbre $ConfirmTimbre(ToneCutoff = 800);

            @on Click
            {
                @play $self.timbre.Confirm;
                @animate with Tween(300ms, EaseOut)
                {
                    @to
                    {
                        $self.timbre.Confirm.Volume = 0.8;
                        $self.timbre.Confirm.ToneCutoff = 6000;
                    }
                }
            }
          </Button.Aspect>
        </Button>
        """;

    [Fact]
    public void TimbreMotionApprovedExampleBindsWithoutDiagnostics()
    {
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("TimbreMotionApproved.crn", ApprovedExample);

        Assert.Empty(result.Syntax.Diagnostics);
        Assert.Empty(result.SemanticDiagnostics);
    }

    [Fact]
    public void TimbreMotionTargetNeverFallsBackToAnElementProperty()
    {
        string markup = ApprovedExample
            .Replace("$self.timbre.Confirm.ToneCutoff = 6000;", string.Empty, StringComparison.Ordinal)
            .Replace("$self.timbre.Confirm.Volume = 0.8;", "$self.timbre.Confirm.Opacity = 0.5;", StringComparison.Ordinal);

        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("TimbreMotionElementFallback.crn", markup);

        Assert.Contains(result.SemanticDiagnostics, diagnostic =>
            diagnostic.Message.Contains("Opacity", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("Confirm", StringComparison.Ordinal));
    }
}
