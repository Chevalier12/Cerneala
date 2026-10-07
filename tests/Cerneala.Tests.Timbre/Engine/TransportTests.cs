using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Engine;

public sealed class TransportTests
{
    private static readonly Func<long, int, float> Signal = DeterministicSoundReader.DefaultSignal;

    [Fact]
    public async Task PausePreservesPositionAndResumeContinuesWithoutRestart()
    {
        using TimbreRig rig = new();
        SoundClip clip = TimbreRig.Clip(new DeterministicSoundSourceFactory(48000));
        SoundPlayback paused = rig.Scope.Play(clip);
        SoundPlayback other = rig.Scope.Play(clip, start => start.Volume = 0.5f);
        await rig.StartAsync(paused, other);

        paused.Pause();
        paused.Pause();
        float[] whilePaused = await rig.NextBlockAsync();
        Assert.Equal(SoundPlaybackState.Paused, paused.State);
        Assert.Equal(TimbreRig.FramesToTime(TimbreRig.Budget), paused.Position);
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Block, Signal, 0.5f, TimbreRig.Budget), whilePaused);

        paused.Resume();
        paused.Resume();
        float[] resumed = await rig.NextBlockAsync();
        Assert.Equal(SoundPlaybackState.Playing, paused.State);
        TimbreRig.AssertPcm(
            TimbreRig.Sum(
                TimbreRig.Expected(TimbreRig.Block, Signal, 1f, TimbreRig.Budget),
                TimbreRig.Expected(TimbreRig.Block, Signal, 0.5f, TimbreRig.Budget + TimbreRig.Block)),
            resumed);

        SoundRuntimeDiagnostics diagnostics = rig.Runtime.GetDiagnostics();
        Assert.Equal(1, diagnostics.PausesApplied);
        Assert.Equal(1, diagnostics.ResumesApplied);
    }

    [Fact]
    public async Task PauseWhilePendingHoldsTheFirstPcmUntilResume()
    {
        using TimbreRig rig = new();
        SoundPlayback playback = rig.Scope.Play(TimbreRig.Clip(new DeterministicSoundSourceFactory(48000)));
        playback.Pause();
        Assert.Equal(SoundPlaybackState.Paused, playback.State);
        await rig.ReadyAsync(playback);
        rig.Output.Release();
        await rig.SyncAsync();

        Assert.Equal(0, rig.Output.SubmittedFrames);
        Assert.Equal(SoundPlaybackState.Paused, playback.State);

        rig.Output.Hold(); // observe the resumed state before the mixer may run
        playback.Resume();
        Assert.Equal(SoundPlaybackState.Pending, playback.State);
        rig.Output.Release();
        await rig.Output.WaitForSubmittedFramesAsync(TimbreRig.Budget);
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, Signal), rig.Output.Read(0, TimbreRig.Budget));
        Assert.Equal(SoundPlaybackState.Playing, playback.State);
    }

    [Fact]
    public async Task TerminalPlaybackRejectsTransportAndMutationsButCancelIsIdempotent()
    {
        using TimbreRig rig = new();
        SoundParameter<float> unused = new("Unused", 1f);
        SoundClip clip = new(SoundSource.FromReader(new DeterministicSoundSourceFactory(48000).Open), parameters: [unused]);
        SoundPlayback playback = rig.Scope.Play(clip);
        playback.Cancel();

        playback.Cancel();
        Assert.Throws<InvalidOperationException>(() => playback.Pause());
        Assert.Throws<InvalidOperationException>(() => playback.Resume());
        Assert.Throws<InvalidOperationException>(() => playback.Volume = 0.5f);
        Assert.Throws<InvalidOperationException>(() => playback.Set(unused, 2f));
        await Assert.ThrowsAsync<InvalidOperationException>(() => playback.SeekAsync(TimeSpan.Zero));
        Assert.Equal(SoundPlaybackState.Canceled, playback.State);
        Assert.Equal(SoundPlaybackState.Canceled, (await TimbreRig.CompletionAsync(playback)).State);
    }

    [Fact]
    public async Task SeekMovesToTheExactFrameAndKeepsAnActivePlaybackActive()
    {
        using TimbreRig rig = new();
        SoundPlayback playback = rig.Scope.Play(TimbreRig.Clip(new DeterministicSoundSourceFactory(48000)));
        await rig.StartAsync(playback);

        Task seek = playback.SeekAsync(TimbreRig.FramesToTime(24000));
        await HarnessWait.WithTimeout(seek, null, "Seek did not complete.");
        Assert.Equal(TimbreRig.FramesToTime(24000), playback.Position);
        float[] block = await rig.NextBlockAsync();

        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Block, Signal, 1f, 24000), block);
        Assert.Equal(SoundPlaybackState.Playing, playback.State);
        Assert.Equal(1, rig.Runtime.GetDiagnostics().SeeksCompleted);
    }

    [Fact]
    public async Task SeekWhilePausedStaysPausedAtTheTarget()
    {
        using TimbreRig rig = new();
        SoundPlayback playback = rig.Scope.Play(TimbreRig.Clip(new DeterministicSoundSourceFactory(48000)));
        await rig.StartAsync(playback);
        playback.Pause();

        await HarnessWait.WithTimeout(playback.SeekAsync(TimbreRig.FramesToTime(1000)), null, "Seek did not complete.");
        rig.Output.ConsumeAll();
        await rig.SyncAsync();

        Assert.Equal(SoundPlaybackState.Paused, playback.State);
        Assert.Equal(TimbreRig.FramesToTime(1000), playback.Position);
        Assert.Equal(TimbreRig.Budget, rig.Output.SubmittedFrames);

        playback.Resume();
        await rig.Output.WaitForSubmittedFramesAsync(TimbreRig.Budget * 2);
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, Signal, 1f, 1000), rig.Output.Read(TimbreRig.Budget, TimbreRig.Budget));
    }

    [Fact]
    public async Task SeekRejectsNegativeAndBeyondKnownDurationSynchronously()
    {
        using TimbreRig rig = new();
        SoundPlayback playback = rig.Scope.Play(TimbreRig.Clip(new DeterministicSoundSourceFactory(48000)));
        await rig.StartAsync(playback);

        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = playback.SeekAsync(TimeSpan.FromTicks(-1)); });
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = playback.SeekAsync(TimeSpan.FromSeconds(1.01)); });
        Assert.Equal(TimbreRig.FramesToTime(TimbreRig.Budget), playback.Position);
    }

    [Fact]
    public async Task SeekToTheEndCompletesANonLoopingPlayback()
    {
        using TimbreRig rig = new(hold: false);
        SoundPlayback playback = rig.Scope.Play(TimbreRig.Clip(new DeterministicSoundSourceFactory(48000)));
        await rig.ReadyAsync(playback);
        await HarnessWait.WithTimeout(playback.SeekAsync(TimeSpan.FromSeconds(1)), null, "Seek did not complete.");
        rig.Output.ConsumeAll();

        await rig.SyncAsync();
        rig.Output.ConsumeAll();
        SoundPlaybackResult result = await TimbreRig.CompletionAsync(playback);

        Assert.Equal(SoundPlaybackState.Completed, result.State);
    }

    [Fact]
    public async Task LatestStreamingSeekWinsAndTheSupersededRequestIsCanceled()
    {
        using TimbreRig rig = new();
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        DeterministicSoundSourceFactory factory = new(48000, maxFramesPerRead: 512);
        SoundPlayback playback = rig.Scope.Play(TimbreRig.Clip(factory, SoundLoading.Streaming));
        await rig.StartAsync(playback);
        DeterministicSoundReader reader = factory.Readers.Single();
        reader.ReadGate = token => gate.Task.WaitAsync(token);
        int reads = reader.ReadCount;
        rig.Output.ConsumeAll(); // the pump refills and blocks in the gate
        await rig.Output.WaitForSubmittedFramesAsync(TimbreRig.Budget * 2); // queue full again: no capacity until the next consume
        await reader.WaitForReadCountAsync(reads + 1);

        Task first = playback.SeekAsync(TimbreRig.FramesToTime(10000));
        Task second = playback.SeekAsync(TimbreRig.FramesToTime(30000));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => HarnessWait.WithTimeout(first, null, "Superseded seek did not finish."));
        Assert.False(second.IsCompleted);

        reader.ReadGate = null;
        gate.SetResult();
        await HarnessWait.WithTimeout(second, null, "Latest seek did not complete.");
        await rig.SettledAsync(playback);

        Assert.Equal([30000L], reader.SeekTargets);
        Assert.Equal(TimbreRig.FramesToTime(30000), playback.Position);
        long submitted = rig.Output.SubmittedFrames;
        rig.Output.ConsumeAll();
        await rig.Output.WaitForSubmittedFramesAsync(submitted + TimbreRig.Block);
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Block, Signal, 1f, 30000), rig.Output.Read(submitted, TimbreRig.Block));
        Assert.Equal(1, rig.Runtime.GetDiagnostics().SeeksSuperseded);
    }

    [Fact]
    public async Task CancelCancelsAPendingSeek()
    {
        using TimbreRig rig = new();
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        DeterministicSoundSourceFactory factory = new(48000, maxFramesPerRead: 512);
        SoundPlayback playback = rig.Scope.Play(TimbreRig.Clip(factory, SoundLoading.Streaming));
        await rig.StartAsync(playback);
        DeterministicSoundReader reader = factory.Readers.Single();
        reader.ReadGate = token => gate.Task.WaitAsync(token);
        int reads = reader.ReadCount;
        rig.Output.ConsumeAll();
        await reader.WaitForReadCountAsync(reads + 1);

        Task seek = playback.SeekAsync(TimbreRig.FramesToTime(5000));
        playback.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => HarnessWait.WithTimeout(seek, null, "Seek did not finish."));
        await TimbreRig.ReleasedAsync(playback);
        Assert.Empty(reader.SeekTargets);
        Assert.True(reader.IsDisposed);
    }

    [Fact]
    public async Task SeekBeyondAnUnknownDurationFaultsTheRequestAndPlaybackContinues()
    {
        using TimbreRig rig = new();
        DeterministicSoundSourceFactory factory = new(48000, reportLength: false, maxFramesPerRead: 512);
        SoundPlayback playback = rig.Scope.Play(TimbreRig.Clip(factory, SoundLoading.Streaming));
        await rig.StartAsync(playback);
        Assert.Null(playback.Duration);

        Task seek = playback.SeekAsync(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => HarnessWait.WithTimeout(seek, null, "Seek did not finish."));

        float[] block = await rig.NextBlockAsync();
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Block, Signal, 1f, TimbreRig.Budget), block);
        Assert.Equal(SoundPlaybackState.Playing, playback.State);
    }

    [Fact]
    public async Task PreloadedLoopRepeatsTheWholeSourceWithoutGapsOrCompletion()
    {
        using TimbreRig rig = new();
        SoundPlayback playback = rig.Scope.Play(TimbreRig.Clip(new DeterministicSoundSourceFactory(700), loop: true));

        float[] pcm = await rig.StartAsync(playback);
        float[] next = await rig.NextBlockAsync();

        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, (frame, channel) => Signal(frame % 700, channel)), pcm);
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Block, (frame, channel) => Signal((frame + TimbreRig.Budget) % 700, channel)), next);
        Assert.True(playback.Loop);
        Assert.Equal(SoundPlaybackState.Playing, playback.State);
        Assert.False(playback.Completion.IsCompleted);
        Assert.Equal(3, rig.Runtime.GetDiagnostics().LoopWraps);

        playback.Cancel();
        Assert.Equal(SoundPlaybackState.Canceled, (await TimbreRig.CompletionAsync(playback)).State);
    }

    [Fact]
    public async Task StreamingLoopRepeatsWithoutGapsUnderIrregularReads()
    {
        using TimbreRig rig = new();
        SoundPlayback playback = rig.Scope.Play(
            TimbreRig.Clip(new DeterministicSoundSourceFactory(611, maxFramesPerRead: 97), SoundLoading.Streaming, loop: true));

        float[] pcm = await rig.StartAsync(playback);

        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, (frame, channel) => Signal(frame % 611, channel)), pcm);
        Assert.False(playback.Completion.IsCompleted);
    }

    [Fact]
    public async Task StartLoopOverridesTheClipDefaultAsAnImmutableSnapshot()
    {
        using TimbreRig rig = new();
        SoundClip clip = TimbreRig.Clip(new DeterministicSoundSourceFactory(300), loop: true);
        SoundPlayback once = rig.Scope.Play(clip, start => start.Loop = false);
        SoundPlayback looping = rig.Scope.Play(clip);

        float[] pcm = await rig.StartAsync(once, looping);

        Assert.False(once.Loop);
        Assert.True(looping.Loop);
        Assert.True(clip.Loop);
        float[] expected = TimbreRig.Sum(
            TimbreRig.Expected(TimbreRig.Budget, (frame, channel) => frame < 300 ? Signal(frame, channel) : 0f),
            TimbreRig.Expected(TimbreRig.Budget, (frame, channel) => Signal(frame % 300, channel)));
        TimbreRig.AssertPcm(expected, pcm);
    }

    [Fact]
    public async Task LoopingAnEmptySourceCompletes()
    {
        using TimbreRig rig = new(hold: false);
        SoundPlayback playback = rig.Scope.Play(TimbreRig.Clip(new DeterministicSoundSourceFactory(0), loop: true));

        SoundPlaybackResult result = await TimbreRig.CompletionAsync(playback);

        Assert.Equal(SoundPlaybackState.Completed, result.State);
    }
}
