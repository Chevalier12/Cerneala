using System.Text.Json;
using Cerneala.Language.Semantics;
using Cerneala.Language.Timbre;

namespace Cerneala.Tests.Language;

public sealed class TimbreSemanticTests
{
    public static IEnumerable<object[]> CorpusCases() =>
        TimbreCorpus.Load().Select(item => new object[] { item.Id });

    [Fact]
    public void TimbreClipAttachmentAndCommandBindWithoutDiagnostics()
    {
        const string markup = """
            <Button Content="Play">
              <Button.Resources>
                <TimbreClip Name="Tone">
                  @sound Tone { Source = "audio/tone.wav"; }
                </TimbreClip>
              </Button.Resources>
              <Button.Aspect>
                @timbre $Tone;
                @on Click { @play $self.timbre.Tone; }
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
        Assert.Equal(["ToneCutoff", "EchoMix"], clip.Parameters.Select(parameter => parameter.Name));
        BoundTimbreParameter cutoff = clip.Parameters[0];
        BoundTimbreParameter mix = clip.Parameters[1];
        Assert.Equal((1200f, 20f, 20000f), (cutoff.DefaultValue, cutoff.Minimum, cutoff.Maximum));
        Assert.Equal((0.15f, 0f, 1f), (mix.DefaultValue, mix.Minimum, mix.Maximum));
        BoundTimbreSound sound = Assert.Single(clip.Sounds);
        Assert.Equal(("Confirm", "audio/confirm.wav", 0.8f, false, false), (sound.Name, sound.Source, sound.Volume, sound.Loop, sound.AutoPlay));
        Assert.Equal([cutoff, mix], sound.Parameters);
        Assert.Equal(["LowPass", "Delay"], sound.Modifiers.Select(modifier => modifier.Kind));
        BoundTimbreModifierInput lowPassCutoff = Assert.Single(sound.Modifiers[0].Inputs);
        Assert.Same(cutoff, lowPassCutoff.Parameter);
        Assert.Equal(["Time", "Feedback", "Mix"], sound.Modifiers[1].Inputs.Select(input => input.Name));
        Assert.Equal(0.12f, sound.Modifiers[1].Inputs[0].Value);
        Assert.Equal(0.2f, sound.Modifiers[1].Inputs[1].Value);
        Assert.Same(mix, sound.Modifiers[1].Inputs[2].Parameter);
    }

    [Fact]
    public void ParameterFeedingSeveralInputsUsesTheirIntersectedRange()
    {
        BoundTimbreClip clip = BindClip(TimbreCorpus.Get("timbre.clip.sharedParameter").Markup, "Wash");

        BoundTimbreParameter amount = Assert.Single(clip.Parameters);
        Assert.Equal((0.3f, 0f, 0.95f), (amount.DefaultValue, amount.Minimum, amount.Maximum));
        BoundTimbreSound sound = Assert.Single(clip.Sounds);
        Assert.Equal(["Delay", "LowPass"], sound.Modifiers.Select(modifier => modifier.Kind));
        Assert.Empty(sound.Modifiers[1].Inputs);
    }

    [Fact]
    public void EachSoundDeclaresOnlyTheClipParametersItsModifiersUse()
    {
        BoundTimbreClip clip = BindClip(TimbreCorpus.Get("timbre.clip.parameterAcrossSounds").Markup, "Pair");

        BoundTimbreParameter cut = Assert.Single(clip.Parameters);
        Assert.Equal(["Low", "High", "Plain"], clip.Sounds.Select(sound => sound.Name));
        Assert.Same(cut, Assert.Single(clip.FindSound("Low")!.Parameters));
        Assert.Same(cut, Assert.Single(clip.FindSound("High")!.Parameters));
        Assert.Empty(clip.FindSound("Plain")!.Parameters);
    }

    [Fact]
    public void AttachmentArgumentsAndCommandsBindInSourceOrder()
    {
        const string markup = """
            <StackPanel>
              <Border Name="Speaker">
                <Border.Aspect>@timbre { @sound Music { Source = "audio/music.ogg"; Loop = true; AutoPlay = true; } }</Border.Aspect>
              </Border>
              <Button Content="x">
                <Button.Resources>
                  <TimbreClip Name="Clip">@parameter Cut: float = 900; @sound Tone { Source = "audio/tone.wav"; Volume = 0.6; @modifier LowPass { Cutoff = Cut; } }</TimbreClip>
                </Button.Resources>
                <Button.Aspect>
                  @timbre $Clip(Cut = 120ms);
                  @on Click { @play $self.timbre.Tone; @pause $Speaker.timbre.Music; }
                  @on Loaded { @seek $self.timbre.Tone to 1500ms; @resume $Speaker.timbre.Music; @stop $self.timbre.Tone; }
                </Button.Aspect>
              </Button>
            </StackPanel>
            """;

        CernealaSemanticModel model = LanguagePipelineHarness.BindSemanticModel("Commands.crn", markup, out IDisposable lifetime);
        using (lifetime)
        {
            Assert.Contains(model.Diagnostics, diagnostic =>
                diagnostic.Id == "CERNEALAUI032" && diagnostic.Message.Contains("Duration literal '120ms'", StringComparison.Ordinal));
        }

        string valid = markup.Replace("Cut = 120ms", "Cut = 400", StringComparison.Ordinal);
        model = LanguagePipelineHarness.BindSemanticModel("Commands.crn", valid, out lifetime);
        using (lifetime)
        {
            Assert.Empty(model.Diagnostics);
            BoundTimbreAspect speaker = model.Timbre.Aspects.Values.Single(aspect => aspect.Commands.Count == 0);
            BoundTimbreSound music = Assert.Single(speaker.Attachment!.Clip.Sounds);
            Assert.Null(speaker.Attachment.ResourceName);
            Assert.Equal(("Music", true, true), (music.Name, music.Loop, music.AutoPlay));

            BoundTimbreAspect button = model.Timbre.Aspects.Values.Single(aspect => aspect.Commands.Count > 0);
            Assert.Equal("Clip", button.Attachment!.ResourceName);
            Assert.Equal(("Cut", 400f), (button.Attachment.Arguments.Single().Name, button.Attachment.Arguments.Single().Value));
            Assert.Equal(
                [TimbreCommandKind.Play, TimbreCommandKind.Pause, TimbreCommandKind.Seek, TimbreCommandKind.Resume, TimbreCommandKind.Stop],
                button.Commands.Select(command => command.Kind));
            Assert.Equal(
                [TimbreCommandTarget.Self, TimbreCommandTarget.Named, TimbreCommandTarget.Self, TimbreCommandTarget.Named, TimbreCommandTarget.Self],
                button.Commands.Select(command => command.Target));
            Assert.Equal(["Tone", "Music", "Tone", "Music", "Tone"], button.Commands.Select(command => command.Sound));
            Assert.Equal("Speaker", button.Commands[1].TargetName);
            Assert.Equal(TimeSpan.FromMilliseconds(1500).Ticks, button.Commands[2].SeekTicks);

            BoundTimbreClip clip = Assert.Single(model.Timbre.Clips.Values);
            Assert.Equal(0.6f, clip.Sounds.Single().Volume);
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
            BoundTimbreAttachment attachment = Assert.Single(model.Timbre.Aspects.Values).Attachment!;
            Assert.Equal("audio/inner.wav", attachment.Clip.Sounds.Single().Source);
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
