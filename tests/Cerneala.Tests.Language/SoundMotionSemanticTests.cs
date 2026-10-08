using Cerneala.Language.Features;
using Cerneala.Language.Semantics;
using Cerneala.Language.Text;
using Cerneala.Language.Timbre;

namespace Cerneala.Tests.Language;

// `$self.sound.Handle.Parameter` Motion targets bind to the typed schema of
// the Sound handle, not to a property of the Aspect's element.
public sealed class SoundMotionSemanticTests
{
    private const string Caret = "|caret|";

    private static readonly Lazy<IReadOnlyList<SoundCorpusCase>> Corpus = new(() => SoundCorpus.Parse("sound-motion-corpus.json"));

    public static IEnumerable<object[]> CorpusCases() =>
        Corpus.Value.Select(item => new object[] { item.Id });

    [Fact]
    public void SoundMotionCorpusCoversEveryFamily()
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
    public void SoundMotionLanguageAndSourceGeneratorAgreeOnCorpus(string id)
    {
        SoundCorpusCase item = Corpus.Value.Single(candidate => candidate.Id == id);
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

        foreach (SoundCorpusExpectation expectation in item.Expect)
        {
            Assert.Contains(result.SemanticDiagnostics, diagnostic =>
                diagnostic.Id == expectation.Id &&
                diagnostic.Message.Contains(expectation.Message, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void SoundMotionHandleSchemaIntersectsEveryClipTheHandlePlays()
    {
        string markup = Corpus.Value.Single(item => item.Id == "soundMotion.commonSchema").Markup;
        CernealaSemanticModel model = LanguagePipelineHarness.BindSemanticModel("Schema.crn", markup, out IDisposable lifetime);
        using (lifetime)
        {
            Assert.Empty(model.Diagnostics);
            BoundSoundAspect aspect = Assert.Single(model.Sound.Aspects.Values);
            BoundSoundHandleParameter cut = Assert.Single(aspect.HandleParameters["Playback"]);
            Assert.Equal(("Cut", 20f, 20000f), (cut.Name, cut.Minimum, cut.Maximum));
            Assert.Null(aspect.FindHandleParameter("Playback", "Extra"));
        }
    }

    [Theory]
    [InlineData("@handle Playback; @on Click { @sound $Tone as Playback; @animate { @to { $self.sound.|caret| } } }", new[] { "Playback" })]
    [InlineData("@handle Playback; @on Click { @sound $Tone as Playback; @animate { @to { $self.sound.Pla|caret| } } }", new[] { "Playback" })]
    [InlineData("@handle Playback; @on Click { @sound $Tone as Playback; @animate { @to { $self.sound.Playback.|caret| } } }", new[] { "Cut", "Volume" })]
    [InlineData("@handle Playback; @on Click { @sound $Tone as Playback; @sound $Plain as Playback; @animate { @to { $self.sound.Playback.|caret| } } }", new[] { "Volume" })]
    public void SoundMotionTargetsCompleteHandlesAndParameters(string aspect, string[] expected)
    {
        Assert.Equal(expected, Labels(Button(aspect)));
    }

    [Fact]
    public void SoundMotionTargetsNavigateToHandleAndParameterDeclarations()
    {
        string text = Button("@handle Playback; @on Click { @sound $Tone as Playback; @animate { @to { $self.sound.Playback.Cut = 300; $self.sound.Playback.Volume = 0.5; } } }");
        CernealaSemanticModel model = LanguagePipelineHarness.BindSemanticModel("SoundMotionNavigation.crn", text, out IDisposable lifetime);
        using (lifetime)
        {
            Assert.Empty(model.Diagnostics);
            CernealaNavigationService navigation = new();
            int handle = text.IndexOf("sound.Playback.Cut", StringComparison.Ordinal) + "sound.".Length;
            CernealaLocation handleDefinition = Assert.Single(navigation.GetDefinitions(model, handle));
            Assert.Equal(text.IndexOf("Playback", StringComparison.Ordinal), handleDefinition.Span.Start);
            int parameter = text.IndexOf("Playback.Cut", StringComparison.Ordinal) + "Playback.".Length;
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
        CernealaSemanticModel model = LanguagePipelineHarness.BindSemanticModel("SoundMotionCompletion.crn", text, out IDisposable lifetime);
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
        "<SoundClip Name=\"Tone\">Source = \"audio/tone.wav\"; @parameter Cut: float = 900; @modifier LowPass { Cutoff = Cut; }</SoundClip>" +
        "<SoundClip Name=\"Plain\">Source = \"audio/plain.wav\";</SoundClip>" +
        "</Button.Resources><Button.Aspect>" + aspect + "</Button.Aspect></Button>";

    private const string ApprovedExample = """
        <Button Content="Confirmă">
          <Button.Resources>
            <SoundClip Name="ConfirmSound">
              Source = "audio/confirm.wav";
              Volume = 0.8;
              @parameter ToneCutoff: float = 1200;
              @parameter EchoMix: float = 0.15;
              @modifier LowPass { Cutoff = ToneCutoff; }
              @modifier Delay { Time = 120ms; Feedback = 0.20; Mix = EchoMix; }
            </SoundClip>
          </Button.Resources>
          <Button.Aspect>
            @handle Playback;

            @on Click
            {
                @sound $ConfirmSound(Volume = 0.2, ToneCutoff = 800) as Playback;
                @animate with Tween(300ms, EaseOut)
                {
                    @to
                    {
                        $self.sound.Playback.Volume = 0.8;
                        $self.sound.Playback.ToneCutoff = 6000;
                    }
                }
            }
          </Button.Aspect>
        </Button>
        """;

    [Fact]
    public void SoundMotionApprovedExampleBindsWithoutDiagnostics()
    {
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("SoundMotionApproved.crn", ApprovedExample);

        Assert.Empty(result.Syntax.Diagnostics);
        Assert.Empty(result.SemanticDiagnostics);
    }

    [Fact]
    public void SoundMotionTargetNeverFallsBackToAnElementProperty()
    {
        string markup = ApprovedExample
            .Replace("$self.sound.Playback.ToneCutoff = 6000;", string.Empty, StringComparison.Ordinal)
            .Replace("$self.sound.Playback.Volume = 0.8;", "$self.sound.Playback.Opacity = 0.5;", StringComparison.Ordinal);

        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("SoundMotionElementFallback.crn", markup);

        Assert.Contains(result.SemanticDiagnostics, diagnostic =>
            diagnostic.Message.Contains("Opacity", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("Playback", StringComparison.Ordinal));
    }
}
