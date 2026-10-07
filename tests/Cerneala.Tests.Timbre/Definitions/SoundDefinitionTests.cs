using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Definitions;

public sealed class SoundDefinitionTests
{
    [Fact]
    public void ParameterSupportsFloatOnlyWithIdentifierNameAndFiniteDefault()
    {
        SoundParameter<float> cutoff = new("ToneCutoff", 1200f);
        Assert.Equal("ToneCutoff", cutoff.Name);
        Assert.Equal(typeof(float), cutoff.ValueType);
        Assert.Equal(1200f, cutoff.DefaultValue);
        Assert.Equal("ToneCutoff", cutoff.ToString());

        Assert.Throws<NotSupportedException>(() => new SoundParameter<int>("Count", 1));
        Assert.Throws<ArgumentException>(() => new SoundParameter<float>("", 1f));
        Assert.Throws<ArgumentException>(() => new SoundParameter<float>("1st", 1f));
        Assert.Throws<ArgumentException>(() => new SoundParameter<float>("tone cutoff", 1f));
        Assert.Throws<ArgumentNullException>(() => new SoundParameter<float>(null!, 1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SoundParameter<float>("Bad", float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SoundParameter<float>("Bad", float.PositiveInfinity));
        Assert.Equal("_echo2", new SoundParameter<float>("_echo2", 0f).Name);
    }

    [Fact]
    public void InputConvertsFromConstantsAndParameters()
    {
        SoundParameter<float> mix = new("EchoMix", 0.15f);
        SoundInput<float> constant = 0.5f;
        SoundInput<float> driven = mix;

        Assert.False(constant.IsParameter);
        Assert.Null(constant.Parameter);
        Assert.Equal(0.5f, constant.Value);
        Assert.True(driven.IsParameter);
        Assert.Same(mix, driven.Parameter);
        Assert.Equal(0.15f, driven.Value);
        Assert.Throws<ArgumentNullException>(() => new SoundInput<float>((SoundParameter<float>)null!));
        Assert.Equal(0f, default(SoundInput<float>).Value);
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

        SoundParameter<float> anyValue = new("AnyValue", 50000f);
        Assert.Same(anyValue, new LowPass(cutoff: anyValue).Cutoff.Parameter);
    }

    [Fact]
    public void ClipValidatesVolumeLoadingAndCopiesCollections()
    {
        SoundParameter<float> cutoff = new("ToneCutoff", 1200f);
        List<SoundParameter> parameters = [cutoff];
        List<SoundModifier> modifiers = [new LowPass(cutoff: cutoff)];
        SoundClip clip = new("audio/confirm.wav", volume: 0.8f, loop: true, loading: SoundLoading.Streaming, parameters, modifiers);

        parameters.Clear();
        modifiers.Add(new Delay());

        Assert.Equal(0.8f, clip.Volume);
        Assert.True(clip.Loop);
        Assert.Equal(SoundLoading.Streaming, clip.Loading);
        Assert.Equal([cutoff], clip.Parameters);
        Assert.Single(clip.Modifiers);
        Assert.IsType<LowPass>(clip.Modifiers[0]);
        Assert.Equal("audio/confirm.wav", clip.Source.Name);

        Assert.Throws<ArgumentNullException>(() => new SoundClip(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SoundClip("a.wav", volume: 1.01f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SoundClip("a.wav", volume: -0.01f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SoundClip("a.wav", volume: float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SoundClip("a.wav", loading: (SoundLoading)7));
        Assert.Throws<ArgumentException>(() => new SoundClip("a.wav", parameters: [null!]));
        Assert.Throws<ArgumentException>(() => new SoundClip("a.wav", modifiers: [null!]));
    }

    [Fact]
    public void ClipRejectsDuplicateOrUndeclaredParametersAndDefaultsOutsideFedRanges()
    {
        SoundParameter<float> cutoff = new("ToneCutoff", 1200f);
        SoundParameter<float> sameName = new("ToneCutoff", 800f);
        Assert.Throws<ArgumentException>(() => new SoundClip("a.wav", parameters: [cutoff, cutoff]));
        Assert.Throws<ArgumentException>(() => new SoundClip("a.wav", parameters: [cutoff, sameName]));
        Assert.Throws<ArgumentException>(() => new SoundClip("a.wav", modifiers: [new LowPass(cutoff: cutoff)]));

        SoundParameter<float> tooLow = new("TooLow", 10f);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SoundClip("a.wav", parameters: [tooLow], modifiers: [new LowPass(cutoff: tooLow)]));

        // One parameter feeding Cutoff and Mix must satisfy both ranges.
        SoundParameter<float> shared = new("Shared", 1200f);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SoundClip(
            "a.wav",
            parameters: [shared],
            modifiers: [new LowPass(cutoff: shared), new Delay(mix: shared)]));

        SoundParameter<float> unused = new("Unused", 1e9f);
        Assert.Single(new SoundClip("a.wav", parameters: [unused]).Parameters);

        LowPass reused = new(cutoff: 900f);
        Assert.Equal(2, new SoundClip("a.wav", modifiers: [reused, reused]).Modifiers.Count);
    }

    [Fact]
    public void DeclaringSourcesAndClipsPerformsNoIo()
    {
        int opened = 0;
        SoundSource source = SoundSource.FromReader(() =>
        {
            opened++;
            throw new InvalidOperationException("must not open");
        }, "never");

        SoundClip clip = new(source);
        _ = new SoundClip(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.wav"));
        SoundSource stream = SoundSource.FromStream(() => throw new InvalidOperationException("must not open"));

        Assert.Equal(0, opened);
        Assert.Equal("never", clip.Source.Name);
        Assert.Equal("never", source.ToString());
        Assert.False(string.IsNullOrEmpty(stream.Name));
        Assert.Throws<ArgumentException>(() => SoundSource.FromFile("https://example.invalid/sound.wav"));
        Assert.Throws<ArgumentException>(() => SoundSource.FromFile(" "));
        Assert.Throws<ArgumentNullException>(() => SoundSource.FromFile(null!));
        Assert.Throws<ArgumentNullException>(() => SoundSource.FromReader(null!));
        Assert.Throws<ArgumentNullException>(() => SoundSource.FromStream(null!));
    }

    [Fact]
    public void RuntimeOptionsAreValidatedAndCopied()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SoundRuntime(new SoundRuntimeOptions { MaxVoices = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SoundRuntime(new SoundRuntimeOptions { MaxCacheBytes = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SoundRuntime(new SoundRuntimeOptions { MaxPreloadBytes = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SoundRuntime(new SoundRuntimeOptions { AutoPreloadMaxBytes = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SoundRuntime(new SoundRuntimeOptions { StreamingMemoryLimit = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SoundRuntime(new SoundRuntimeOptions { DelayTailCap = TimeSpan.FromSeconds(-1) }));

        using SoundRuntime runtime = new();
        Assert.False(runtime.IsDisposed);
        runtime.Dispose();
        runtime.Dispose();
        Assert.True(runtime.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => runtime.CreateScope());
    }
}
