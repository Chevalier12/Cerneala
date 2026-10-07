using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Engine;

public sealed class PlainMixTests
{
    private static readonly Func<long, int, float> Signal = DeterministicSoundReader.DefaultSignal;

    [Fact]
    public async Task SinglePreloadedPlaybackEqualsSourceTimesVolume()
    {
        using TimbreRig rig = new();
        SoundPlayback playback = rig.Scope.Play(TimbreRig.Clip(new DeterministicSoundSourceFactory(48000), volume: 0.6f));

        float[] pcm = await rig.StartAsync(playback);
        float[] next = await rig.NextBlockAsync();

        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, Signal, 0.6f), pcm);
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Block, Signal, 0.6f, TimbreRig.Budget), next);
        SoundRuntimeDiagnostics diagnostics = rig.Runtime.GetDiagnostics();
        Assert.Equal(5, diagnostics.BlocksMixed);
        Assert.Equal(0, diagnostics.ClippedSamples);
        Assert.Equal(0, diagnostics.UnderrunFrames);
        Assert.True(rig.Output.MaxQueuedFrames <= TimbreRig.Budget);
    }

    [Fact]
    public async Task StreamingWithIrregularReadsMatchesTheClosedFormSignal()
    {
        using TimbreRig rig = new();
        SoundPlayback playback = rig.Scope.Play(TimbreRig.Clip(new DeterministicSoundSourceFactory(6000, maxFramesPerRead: 37), SoundLoading.Streaming));

        float[] pcm = await rig.StartAsync(playback);

        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, Signal), pcm);
    }

    [Fact]
    public async Task OverlappingPlaybacksSumWithoutNormalization()
    {
        using TimbreRig rig = new();
        Func<long, int, float> constant = (_, channel) => channel == 0 ? 0.4f : -0.3f;
        SoundClip clip = TimbreRig.Clip(new DeterministicSoundSourceFactory(48000, constant));
        SoundPlayback first = rig.Scope.Play(clip);
        SoundPlayback second = rig.Scope.Play(clip);

        float[] pcm = await rig.StartAsync(first, second);

        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, constant, 2f), pcm);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().ClippedSamples);
    }

    [Fact]
    public async Task MixIsHardClippedAtTheOutputAndClippingIsCounted()
    {
        using TimbreRig rig = new();
        Func<long, int, float> loud = (_, channel) => channel == 0 ? 0.75f : -0.75f;
        SoundClip clip = TimbreRig.Clip(new DeterministicSoundSourceFactory(48000, loud));
        SoundPlayback first = rig.Scope.Play(clip);
        SoundPlayback second = rig.Scope.Play(clip, start => start.Volume = 0.5f);

        float[] pcm = await rig.StartAsync(first, second);

        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, (_, channel) => channel == 0 ? 1f : -1f), pcm);
        Assert.Equal(TimbreRig.Budget * 2, rig.Runtime.GetDiagnostics().ClippedSamples);
    }

    [Fact]
    public async Task VolumeZeroProducesSilenceButKeepsAdvancing()
    {
        using TimbreRig rig = new();
        SoundPlayback playback = rig.Scope.Play(TimbreRig.Clip(new DeterministicSoundSourceFactory(48000)), start => start.Volume = 0f);

        float[] pcm = await rig.StartAsync(playback);

        Assert.All(pcm, sample => Assert.Equal(0f, sample));
        Assert.Equal(TimbreRig.FramesToTime(TimbreRig.Budget), playback.Position);
    }
}
