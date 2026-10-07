using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Engine;

public sealed class PlaybackIdentityTests
{
    private static readonly Func<long, int, float> Signal = DeterministicSoundReader.DefaultSignal;

    [Fact]
    public async Task TwoPlaybacksOfOneClipHaveIndependentPlayheadsValuesAndState()
    {
        using TimbreRig rig = new();
        DeterministicSoundSourceFactory factory = new(48000);
        SoundClip clip = TimbreRig.Clip(factory);
        SoundPlayback first = rig.Scope.Play(clip);

        float[] initial = await rig.StartAsync(first);
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, Signal), initial);

        SoundPlayback second = rig.Scope.Play(clip, start => start.Volume = 0.25f);
        await rig.ReadyAsync(second);
        float[] block = await rig.NextBlockAsync();

        TimbreRig.AssertPcm(
            TimbreRig.Sum(
                TimbreRig.Expected(TimbreRig.Block, Signal, 1f, TimbreRig.Budget),
                TimbreRig.Expected(TimbreRig.Block, Signal, 0.25f, 0)),
            block);
        Assert.NotSame(first, second);
        Assert.Equal(TimbreRig.FramesToTime(TimbreRig.Budget + TimbreRig.Block), first.Position);
        Assert.Equal(TimbreRig.FramesToTime(TimbreRig.Block), second.Position);
        Assert.Equal(1f, first.Volume);
        Assert.Equal(0.25f, second.Volume);
        Assert.Equal(1f, clip.Volume);
        Assert.Equal(SoundPlaybackState.Playing, first.State);
        Assert.Equal(SoundPlaybackState.Playing, second.State);
        Assert.Same(clip, second.Clip);
        Assert.Equal(TimeSpan.FromSeconds(1), first.Duration);
        Assert.Equal(1, factory.OpenCount); // one immutable preloaded payload, two playheads
    }

    [Fact]
    public async Task StreamingPlaybacksOpenIndependentReaders()
    {
        using TimbreRig rig = new();
        DeterministicSoundSourceFactory factory = new(4800, maxFramesPerRead: 333);
        SoundClip clip = TimbreRig.Clip(factory, SoundLoading.Streaming);
        SoundPlayback first = rig.Scope.Play(clip);
        SoundPlayback second = rig.Scope.Play(clip);

        float[] pcm = await rig.StartAsync(first, second);

        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, Signal, 2f), pcm);
        Assert.Equal(2, factory.OpenCount);
        Assert.NotSame(factory.Readers[0], factory.Readers[1]);
    }

    [Fact]
    public async Task StartOverridesApplyToTheFirstBlockWithoutChangingTheDefinition()
    {
        using TimbreRig rig = new();
        DeterministicSoundSourceFactory factory = new(48000);
        SoundParameter<float> unused = new("Unused", 3f);
        SoundClip clip = new(SoundSource.FromReader(factory.Open), volume: 0.5f, parameters: [unused]);
        int invocations = 0;
        SoundStartOptions? captured = null;

        SoundPlayback playback = rig.Scope.Play(clip, start =>
        {
            invocations++;
            captured = start;
            Assert.Equal(0.5f, start.Volume);
            Assert.False(start.Loop);
            start.Volume = 0.2f;
            start.Set(unused, 7f);
        });

        Assert.Equal(SoundPlaybackState.Pending, playback.State);
        Assert.Equal(0.2f, playback.Volume);
        float[] pcm = await rig.StartAsync(playback);

        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, Signal, 0.2f), pcm);
        Assert.Equal(1, invocations);
        Assert.Equal(0.5f, clip.Volume);
        Assert.Equal(3f, unused.DefaultValue);
        Assert.Throws<InvalidOperationException>(() => captured!.Volume = 1f);
        Assert.Throws<InvalidOperationException>(() => captured!.Set(unused, 1f));
        Assert.Throws<InvalidOperationException>(() => _ = captured!.Loop);
    }

    [Fact]
    public async Task PlaybackIsPendingUntilItsFirstBlockIsMixed()
    {
        using TimbreRig rig = new();
        SoundPlayback playback = rig.Scope.Play(TimbreRig.Clip(new DeterministicSoundSourceFactory(48000)));
        await rig.ReadyAsync(playback);
        await rig.SyncAsync();

        Assert.Equal(SoundPlaybackState.Pending, playback.State);
        Assert.Equal(TimeSpan.Zero, playback.Position);
        Assert.Equal(0, rig.Output.SubmittedFrames);

        await rig.StartAsync(playback);
        Assert.Equal(SoundPlaybackState.Playing, playback.State);
    }

    [Fact]
    public async Task RuntimeChangesOnOnePlaybackDoNotAffectAnother()
    {
        using TimbreRig rig = new();
        SoundParameter<float> unused = new("Unused", 3f);
        SoundClip clip = new(SoundSource.FromReader(new DeterministicSoundSourceFactory(48000).Open), parameters: [unused]);
        SoundPlayback first = rig.Scope.Play(clip);
        SoundPlayback second = rig.Scope.Play(clip);
        await rig.StartAsync(first, second);

        first.Volume = 0.5f;
        first.Set(unused, 9f);
        float[] block = await rig.NextBlockAsync();

        TimbreRig.AssertPcm(
            TimbreRig.Sum(
                TimbreRig.Expected(TimbreRig.Block, Signal, 0.5f, TimbreRig.Budget),
                TimbreRig.Expected(TimbreRig.Block, Signal, 1f, TimbreRig.Budget)),
            block);
        Assert.Equal(1f, second.Volume);
        Assert.True(rig.Runtime.GetDiagnostics().ParameterPublications >= 2);
    }
}
