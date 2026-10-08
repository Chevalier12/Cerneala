using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Definitions;

public sealed class TimbreDefinitionTests
{
    [Fact]
    public void ParameterSupportsFloatOnlyWithIdentifierNameAndFiniteDefault()
    {
        TimbreParameter<float> cutoff = new("ToneCutoff", 1200f);
        Assert.Equal("ToneCutoff", cutoff.Name);
        Assert.Equal(typeof(float), cutoff.ValueType);
        Assert.Equal(1200f, cutoff.DefaultValue);
        Assert.Equal("ToneCutoff", cutoff.ToString());

        Assert.Throws<NotSupportedException>(() => new TimbreParameter<int>("Count", 1));
        Assert.Throws<ArgumentException>(() => new TimbreParameter<float>("", 1f));
        Assert.Throws<ArgumentException>(() => new TimbreParameter<float>("1st", 1f));
        Assert.Throws<ArgumentException>(() => new TimbreParameter<float>("tone cutoff", 1f));
        Assert.Throws<ArgumentNullException>(() => new TimbreParameter<float>(null!, 1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimbreParameter<float>("Bad", float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimbreParameter<float>("Bad", float.PositiveInfinity));
        Assert.Equal("_echo2", new TimbreParameter<float>("_echo2", 0f).Name);
    }

    [Fact]
    public void InputConvertsFromConstantsAndParameters()
    {
        TimbreParameter<float> mix = new("EchoMix", 0.15f);
        TimbreInput<float> constant = 0.5f;
        TimbreInput<float> driven = mix;

        Assert.False(constant.IsParameter);
        Assert.Null(constant.Parameter);
        Assert.Equal(0.5f, constant.Value);
        Assert.True(driven.IsParameter);
        Assert.Same(mix, driven.Parameter);
        Assert.Equal(0.15f, driven.Value);
        Assert.Throws<ArgumentNullException>(() => new TimbreInput<float>((TimbreParameter<float>)null!));
        Assert.Equal(0f, default(TimbreInput<float>).Value);
    }

    [Fact]
    public void ModifiersUseCatalogDefaultsAndRejectInvalidConstants()
    {
        LowPass lowPass = new();
        Assert.Equal(1200f, lowPass.Cutoff.Value);
        Delay delay = new();
        Assert.Equal(0.12f, delay.Time.Value);
        Assert.Equal(0.20f, delay.Feedback.Value);
        Assert.Equal(0.15f, delay.Mix.Value);

        Assert.Equal(20f, new LowPass(cutoff: 20f).Cutoff.Value);
        Assert.Equal(20000f, new LowPass(cutoff: 20000f).Cutoff.Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => new LowPass(cutoff: 19f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LowPass(cutoff: 20001f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LowPass(cutoff: float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Delay(time: 0.0009f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Delay(time: 2.001f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Delay(feedback: 0.96f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Delay(feedback: -0.01f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Delay(mix: 1.01f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Delay(mix: float.NegativeInfinity));

        TimbreParameter<float> anyValue = new("AnyValue", 50000f);
        Assert.Same(anyValue, new LowPass(cutoff: anyValue).Cutoff.Parameter);
    }

    [Fact]
    public void ClipValidatesVolumeLoadingAndCopiesCollections()
    {
        TimbreParameter<float> cutoff = new("ToneCutoff", 1200f);
        List<TimbreParameter> parameters = [cutoff];
        List<TimbreModifier> modifiers = [new LowPass(cutoff: cutoff)];
        TimbreClip clip = new("audio/confirm.wav", volume: 0.8f, loop: true, loading: TimbreLoading.Streaming, parameters, modifiers);

        parameters.Clear();
        modifiers.Add(new Delay());

        Assert.Equal(0.8f, clip.Volume);
        Assert.True(clip.Loop);
        Assert.Equal(TimbreLoading.Streaming, clip.Loading);
        Assert.Equal([cutoff], clip.Parameters);
        Assert.Single(clip.Modifiers);
        Assert.IsType<LowPass>(clip.Modifiers[0]);
        Assert.Equal("audio/confirm.wav", clip.Source.Name);

        Assert.Throws<ArgumentNullException>(() => new TimbreClip(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimbreClip("a.wav", volume: 1.01f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimbreClip("a.wav", volume: -0.01f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimbreClip("a.wav", volume: float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimbreClip("a.wav", loading: (TimbreLoading)7));
        Assert.Throws<ArgumentException>(() => new TimbreClip("a.wav", parameters: [null!]));
        Assert.Throws<ArgumentException>(() => new TimbreClip("a.wav", modifiers: [null!]));
    }

    [Fact]
    public void ClipRejectsDuplicateOrUndeclaredParametersAndDefaultsOutsideFedRanges()
    {
        TimbreParameter<float> cutoff = new("ToneCutoff", 1200f);
        TimbreParameter<float> sameName = new("ToneCutoff", 800f);
        Assert.Throws<ArgumentException>(() => new TimbreClip("a.wav", parameters: [cutoff, cutoff]));
        Assert.Throws<ArgumentException>(() => new TimbreClip("a.wav", parameters: [cutoff, sameName]));
        Assert.Throws<ArgumentException>(() => new TimbreClip("a.wav", modifiers: [new LowPass(cutoff: cutoff)]));

        TimbreParameter<float> tooLow = new("TooLow", 10f);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimbreClip("a.wav", parameters: [tooLow], modifiers: [new LowPass(cutoff: tooLow)]));

        // One parameter feeding Cutoff and Mix must satisfy both ranges.
        TimbreParameter<float> shared = new("Shared", 1200f);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimbreClip(
            "a.wav",
            parameters: [shared],
            modifiers: [new LowPass(cutoff: shared), new Delay(mix: shared)]));

        TimbreParameter<float> unused = new("Unused", 1e9f);
        Assert.Single(new TimbreClip("a.wav", parameters: [unused]).Parameters);

        LowPass reused = new(cutoff: 900f);
        Assert.Equal(2, new TimbreClip("a.wav", modifiers: [reused, reused]).Modifiers.Count);
    }

    [Fact]
    public void DeclaringSourcesAndClipsPerformsNoIo()
    {
        int opened = 0;
        TimbreSource source = TimbreSource.FromReader(() =>
        {
            opened++;
            throw new InvalidOperationException("must not open");
        }, "never");

        TimbreClip clip = new(source);
        _ = new TimbreClip(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.wav"));
        TimbreSource stream = TimbreSource.FromStream(() => throw new InvalidOperationException("must not open"));

        Assert.Equal(0, opened);
        Assert.Equal("never", clip.Source.Name);
        Assert.Equal("never", source.ToString());
        Assert.False(string.IsNullOrEmpty(stream.Name));
        Assert.Throws<ArgumentException>(() => TimbreSource.FromFile("https://example.invalid/sound.wav"));
        Assert.Throws<ArgumentException>(() => TimbreSource.FromFile(" "));
        Assert.Throws<ArgumentNullException>(() => TimbreSource.FromFile(null!));
        Assert.Throws<ArgumentNullException>(() => TimbreSource.FromReader((Func<TimbreReader>)null!));
        Assert.Throws<ArgumentNullException>(() => TimbreSource.FromStream((Func<Stream>)null!));
        Assert.Throws<ArgumentNullException>(() => TimbreSource.FromReader((Func<TimbreMemoryBudget, TimbreReader>)null!));
        Assert.Throws<ArgumentNullException>(() => TimbreSource.FromStream((Func<TimbreMemoryBudget, Stream>)null!));
    }

    [Fact]
    public void RuntimeOptionsAreValidatedAndCopied()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimbreRuntime(new TimbreRuntimeOptions { MaxVoices = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimbreRuntime(new TimbreRuntimeOptions { MaxCacheBytes = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimbreRuntime(new TimbreRuntimeOptions { MaxPreloadBytes = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimbreRuntime(new TimbreRuntimeOptions { AutoPreloadMaxBytes = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimbreRuntime(new TimbreRuntimeOptions { StreamingMemoryLimit = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimbreRuntime(new TimbreRuntimeOptions { DelayTailCap = TimeSpan.FromSeconds(-1) }));

        using TimbreRuntime runtime = new();
        Assert.False(runtime.IsDisposed);
        runtime.Dispose();
        runtime.Dispose();
        Assert.True(runtime.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => runtime.CreateScope());
    }
}
