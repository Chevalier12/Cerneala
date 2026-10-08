using System.Text.Json;
using Cerneala.Language.Semantics;
using Cerneala.Language.Timbre;

namespace Cerneala.Tests.Language;

public sealed class TimbreSemanticTests
{
    public static IEnumerable<object[]> CorpusCases() =>
        TimbreCorpus.Load().Select(item => new object[] { item.Id });

    [Fact]
    public void TimbreClipAndEventTimbreActionBindWithoutDiagnostics()
    {
        const string markup = """
            <Button Content="Play">
              <Button.Resources>
                <TimbreClip Name="Tone">
                  Source = "audio/tone.wav";
                </TimbreClip>
              </Button.Resources>
              <Button.Aspect>
                @on Click { @timbre $Tone; }
              </Button.Aspect>
            </Button>
            """;

        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("TimbreMinimal.crn", markup);

        Assert.Empty(result.Syntax.Diagnostics);
        Assert.Empty(result.SemanticDiagnostics);
    }

    [Fact]
    public void CorpusCoversEveryFrozenFamily()
    {
        IReadOnlyList<TimbreCorpusCase> cases = TimbreCorpus.Load();

        Assert.Equal(cases.Count, cases.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count());
        foreach (string family in new[] { "TimbreClip", "Action", "Reference", "Value", "Syntax", "Context" })
        {
            Assert.Contains(cases, item => item.Family == family);
        }

        Assert.All(cases.Where(item => !item.Valid), item => Assert.NotEmpty(item.Expect));
    }

    // Language and SourceGen must agree on every corpus case: identical
    // diagnostics (id, message, span) and, for valid cases, generated C#
    // that compiles in an ordinary consumer.
    [Theory]
    [MemberData(nameof(CorpusCases))]
    public void LanguageAndSourceGeneratorAgreeOnCorpus(string id)
    {
        TimbreCorpusCase item = TimbreCorpus.Get(id);

        string path = item.Path ?? id + ".crn";
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze(path, item.Markup);
        IReadOnlyList<HarnessDiagnostic> generator = item.Companion is null
            ? result.SourceGeneratorDiagnostics
            : LanguagePipelineHarness.AnalyzePairedSourceGenerator(path, item.Markup, path + ".cs", item.Companion);

        Assert.Empty(result.Syntax.Diagnostics);
        Assert.Equal(Ordered(result.SemanticDiagnostics), Ordered(generator));
        if (item.Valid)
        {
            Assert.Empty(result.SemanticDiagnostics);
            Assert.Empty(LanguagePipelineHarness.GeneratedCompilationErrors(path, item.Markup, path + ".cs", item.Companion));
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
    public void ApprovedClipBindsTypedCatalogValuesInDeclarationOrder()
    {
        BoundTimbreClip clip = BindClip(TimbreCorpus.Get("timbre.clip.approved").Markup, "ConfirmTimbre");

        Assert.True(clip.IsValid);
        Assert.Equal("audio/confirm.wav", clip.Source);
        Assert.Equal(0.8f, clip.Volume);
        Assert.False(clip.Loop);
        Assert.Equal(["ToneCutoff", "EchoMix"], clip.Parameters.Select(parameter => parameter.Name));
        BoundTimbreParameter cutoff = clip.Parameters[0];
        BoundTimbreParameter mix = clip.Parameters[1];
        Assert.Equal((1200f, 20f, 20000f), (cutoff.DefaultValue, cutoff.Minimum, cutoff.Maximum));
        Assert.Equal((0.15f, 0f, 1f), (mix.DefaultValue, mix.Minimum, mix.Maximum));
        Assert.Equal(["LowPass", "Delay"], clip.Modifiers.Select(modifier => modifier.Kind));
        BoundTimbreModifierInput lowPassCutoff = Assert.Single(clip.Modifiers[0].Inputs);
        Assert.Same(cutoff, lowPassCutoff.Parameter);
        Assert.Equal(["Time", "Feedback", "Mix"], clip.Modifiers[1].Inputs.Select(input => input.Name));
        Assert.Equal(0.12f, clip.Modifiers[1].Inputs[0].Value);
        Assert.Equal(0.2f, clip.Modifiers[1].Inputs[1].Value);
        Assert.Same(mix, clip.Modifiers[1].Inputs[2].Parameter);
    }

    [Fact]
    public void ParameterFeedingSeveralInputsUsesTheirIntersectedRange()
    {
        BoundTimbreClip clip = BindClip(TimbreCorpus.Get("timbre.clip.sharedParameter").Markup, "Wash");

        BoundTimbreParameter amount = Assert.Single(clip.Parameters);
        Assert.Equal((0.3f, 0f, 0.95f), (amount.DefaultValue, amount.Minimum, amount.Maximum));
        Assert.Equal(["Delay", "LowPass"], clip.Modifiers.Select(modifier => modifier.Kind));
        Assert.Empty(clip.Modifiers[1].Inputs);
    }

    [Fact]
    public void StartOverridesBindPerActionWithoutMutatingTheClip()
    {
        const string markup = """
            <Button Content="x">
              <Button.Resources>
                <TimbreClip Name="Music">Source = "audio/music.ogg"; Loop = true; Volume = 0.6; @parameter Cut: float = 900; @modifier LowPass { Cutoff = Cut; }</TimbreClip>
              </Button.Resources>
              <Button.Aspect>
                @handle Track;
                @on Click { @timbre $Music(Loop = false, Volume = 0.2, Cut = 120ms) as Track; }
                @on Loaded { @timbre $Music; @seek Track to 1500ms; @pause Track; @resume Track; @cancel Track; }
              </Button.Aspect>
            </Button>
            """;

        CernealaSemanticModel model = LanguagePipelineHarness.BindSemanticModel("Overrides.crn", markup, out IDisposable lifetime);
        using (lifetime)
        {
            Assert.Contains(model.Diagnostics, diagnostic =>
                diagnostic.Id == "CERNEALAUI032" && diagnostic.Message.Contains("Duration literal '120ms'", StringComparison.Ordinal));
        }

        string valid = markup.Replace("Cut = 120ms", "Cut = 400", StringComparison.Ordinal);
        model = LanguagePipelineHarness.BindSemanticModel("Overrides.crn", valid, out lifetime);
        using (lifetime)
        {
            Assert.Empty(model.Diagnostics);
            BoundTimbreAspect aspect = Assert.Single(model.Timbre.Aspects.Values);
            Assert.Equal(TimbreHandleKind.Timbre, aspect.Handles["Track"]);
            Assert.Equal(
                [TimbreActionKind.Play, TimbreActionKind.Play, TimbreActionKind.Seek, TimbreActionKind.Pause, TimbreActionKind.Resume, TimbreActionKind.Cancel],
                aspect.Actions.Select(action => action.Kind));
            BoundTimbreAction start = aspect.Actions[0];
            Assert.Equal(("Track", (bool?)false, (float?)0.2f), (start.HandleName, start.Loop, start.Volume));
            Assert.Equal(("Cut", 400f), (start.Arguments.Single().Name, start.Arguments.Single().Value));
            BoundTimbreAction plain = aspect.Actions[1];
            Assert.Null(plain.HandleName);
            Assert.Null(plain.Loop);
            Assert.Null(plain.Volume);
            Assert.Empty(plain.Arguments);
            Assert.Same(start.Clip, plain.Clip);
            Assert.Equal(TimeSpan.FromMilliseconds(1500).Ticks, aspect.Actions[2].SeekTicks);

            BoundTimbreClip clip = Assert.Single(model.Timbre.Clips.Values);
            Assert.True(clip.Loop);
            Assert.Equal(0.6f, clip.Volume);
            Assert.Equal(900f, clip.Parameters.Single().DefaultValue);
        }
    }

    [Fact]
    public void ShadowedClipBindsTheNearestScope()
    {
        CernealaSemanticModel model = LanguagePipelineHarness.BindSemanticModel(
            "Shadow.crn",
            TimbreCorpus.Get("timbre.clip.shadowing").Markup,
            out IDisposable lifetime);
        using (lifetime)
        {
            Assert.Empty(model.Diagnostics);
            BoundTimbreAction play = Assert.Single(Assert.Single(model.Timbre.Aspects.Values).Actions);
            Assert.Equal("audio/inner.wav", play.Clip!.Source);
        }
    }

    [Fact]
    public void MotionOnlyAspectsPublishNoTimbreModel()
    {
        const string markup = """
            <Button Content="x">
              <Button.Aspect>@handle Active; @on Click { @animate { @to { Opacity = 0.5; } } } @on Loaded { @cancel Active; }</Button.Aspect>
            </Button>
            """;

        CernealaSemanticModel model = LanguagePipelineHarness.BindSemanticModel("MotionOnly.crn", markup, out IDisposable lifetime);
        using (lifetime)
        {
            Assert.Empty(model.Diagnostics);
            Assert.Empty(model.Timbre.Aspects);
            Assert.Empty(model.Timbre.Clips);
        }
    }

    private static BoundTimbreClip BindClip(string markup, string name)
    {
        CernealaSemanticModel model = LanguagePipelineHarness.BindSemanticModel(name + ".crn", markup, out IDisposable lifetime);
        using (lifetime)
        {
            Assert.Empty(model.Diagnostics);
            return Assert.Single(model.Timbre.Clips.Values, clip => clip.Name == name);
        }
    }

    private static IEnumerable<HarnessDiagnostic> Ordered(IEnumerable<HarnessDiagnostic> diagnostics) => diagnostics
        .OrderBy(diagnostic => diagnostic.StartLine)
        .ThenBy(diagnostic => diagnostic.StartCharacter)
        .ThenBy(diagnostic => diagnostic.Id, StringComparer.Ordinal)
        .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal);
}

public sealed record TimbreCorpusExpectation(string Id, string Message);

public sealed record TimbreCorpusCase(
    string Id,
    string Family,
    bool Valid,
    string Markup,
    string? Note = null,
    IReadOnlyList<TimbreCorpusExpectation>? ExpectList = null,
    string? Path = null,
    string? Companion = null)
{
    public IReadOnlyList<TimbreCorpusExpectation> Expect => ExpectList ?? [];
}

internal static class TimbreCorpus
{
    private static readonly Lazy<IReadOnlyList<TimbreCorpusCase>> cases = new(() => Parse("timbre-corpus.json"));

    public static IReadOnlyList<TimbreCorpusCase> Load() => cases.Value;

    public static TimbreCorpusCase Get(string id) => Load().Single(item => item.Id == id);

    internal static IReadOnlyList<TimbreCorpusCase> Parse(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Corpus", fileName);
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.EnumerateArray().Select(item => new TimbreCorpusCase(
                item.GetProperty("id").GetString()!,
                item.GetProperty("family").GetString()!,
                item.GetProperty("valid").GetBoolean(),
                item.GetProperty("markup").GetString()!,
                item.TryGetProperty("note", out JsonElement note) ? note.GetString() : null,
                item.TryGetProperty("expect", out JsonElement expect)
                    ? expect.EnumerateArray()
                        .Select(entry => new TimbreCorpusExpectation(entry.GetProperty("id").GetString()!, entry.GetProperty("message").GetString()!))
                        .ToArray()
                    : null,
                item.TryGetProperty("path", out JsonElement documentPath) ? documentPath.GetString() : null,
                item.TryGetProperty("companion", out JsonElement companion) ? companion.GetString() : null))
            .ToArray();
    }
}
