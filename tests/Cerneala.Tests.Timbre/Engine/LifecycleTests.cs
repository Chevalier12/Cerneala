using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Engine;

public sealed class LifecycleTests
{
    private static readonly Func<long, int, float> Signal = DeterministicSoundReader.DefaultSignal;

    [Fact]
    public async Task CompletionRequiresEndOfSourceAndAnObservedDrain()
    {
        using TimbreRig rig = new();
        SoundPlayback playback = rig.Scope.Play(TimbreRig.Clip(new DeterministicSoundSourceFactory(1000)));
        await rig.ReadyAsync(playback);
        rig.Output.Release();
        await rig.Output.WaitForSubmittedFramesAsync(1440);
        await rig.SyncAsync();

        float[] expected = TimbreRig.Expected(1440, (frame, channel) => frame < 1000 ? Signal(frame, channel) : 0f);
        TimbreRig.AssertPcm(expected, rig.Output.Read(0, 1440));
        Assert.Equal(1440, rig.Output.SubmittedFrames); // three blocks: the last holds frames 960..999
        Assert.Equal(SoundPlaybackState.Playing, playback.State);

        rig.Output.Consume(960);
        await rig.SyncAsync();
        Assert.Equal(SoundPlaybackState.Playing, playback.State);
        Assert.False(playback.Completion.IsCompleted);

        rig.Output.Consume(480);
        SoundPlaybackResult result = await TimbreRig.CompletionAsync(playback);
        Assert.Equal(SoundPlaybackState.Completed, result.State);
        Assert.Null(result.Error);
        Assert.False(result.TailTruncated);
        Assert.Equal(SoundPlaybackState.Completed, playback.State);
        Assert.Same(result, await playback.Completion);
        Assert.Equal(1, rig.Runtime.GetDiagnostics().PlaybacksCompleted);
        Assert.Throws<InvalidOperationException>(() => playback.Volume = 0.5f);
        await TimbreRig.ReleasedAsync(playback);
    }

    [Fact]
    public async Task ReaderFailureFailsOnlyThatPlaybackAndIsNotEndOfSource()
    {
        using TimbreRig rig = new();
        DeterministicSoundSourceFactory failing = new(48000, maxFramesPerRead: 256) { Configure = reader => reader.FailAtFrame = 3000 };
        SoundPlayback broken = rig.Scope.Play(TimbreRig.Clip(failing, SoundLoading.Streaming));
        SoundPlayback healthy = rig.Scope.Play(TimbreRig.Clip(new DeterministicSoundSourceFactory(48000)));
        await rig.ReadyAsync(broken, healthy);
        rig.Output.Release();

        SoundPlaybackResult result = await TimbreRig.CompletionAsync(broken);

        Assert.Equal(SoundPlaybackState.Failed, result.State);
        Assert.Equal(SoundErrorKind.SourceUnavailable, result.Error!.Kind);
        Assert.IsType<IOException>(result.Error.InnerException);
        Assert.NotEqual(SoundPlaybackState.Failed, healthy.State);
        await TimbreRig.ReleasedAsync(broken);
        Assert.Equal(0, failing.LiveReaders);
        Assert.Equal(1, rig.Runtime.GetDiagnostics().PlaybacksFailed);
    }

    [Fact]
    public async Task PreloadReaderFailureFailsThePlayback()
    {
        using TimbreRig rig = new(hold: false);
        DeterministicSoundSourceFactory failing = new(4000) { Configure = reader => reader.FailAtFrame = 10 };

        SoundPlaybackResult result = await TimbreRig.CompletionAsync(rig.Scope.Play(TimbreRig.Clip(failing)));

        Assert.Equal(SoundPlaybackState.Failed, result.State);
        Assert.Equal(SoundErrorKind.SourceUnavailable, result.Error!.Kind);
    }

    [Fact]
    public async Task NonFiniteSamplesFailWithInvalidData()
    {
        using TimbreRig rig = new(hold: false);
        DeterministicSoundSourceFactory corrupt = new(4000, (frame, channel) => frame == 100 ? float.NaN : 0.1f);

        SoundPlaybackResult preload = await TimbreRig.CompletionAsync(rig.Scope.Play(TimbreRig.Clip(corrupt)));
        SoundPlaybackResult streaming = await TimbreRig.CompletionAsync(rig.Scope.Play(TimbreRig.Clip(corrupt, SoundLoading.Streaming)));

        Assert.Equal(SoundErrorKind.InvalidData, preload.Error!.Kind);
        Assert.Equal(SoundErrorKind.InvalidData, streaming.Error!.Kind);
    }

    [Fact]
    public async Task RepeatedSynchronousEmptyReadsViolateTheReaderContract()
    {
        using TimbreRig rig = new(hold: false);
        DeterministicSoundSourceFactory violating = new(4000) { Configure = reader => reader.ViolateReadinessContract = true };

        SoundPlaybackResult result = await TimbreRig.CompletionAsync(rig.Scope.Play(TimbreRig.Clip(violating, SoundLoading.Streaming)));

        Assert.Equal(SoundPlaybackState.Failed, result.State);
        Assert.Equal(SoundErrorKind.InvalidData, result.Error!.Kind);
    }

    [Fact]
    public async Task FileSourcesFailExplicitlyWithoutADecoderOrFile()
    {
        string directory = Path.Combine(Path.GetTempPath(), "timbre-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            // Content no registered decoder recognizes, despite the extension.
            await File.WriteAllBytesAsync(Path.Combine(directory, "tone.wav"), "plain text, not audio"u8.ToArray());
            using TimbreRig rig = new(options => options.BaseDirectory = directory, hold: false);

            SoundPlaybackResult unsupported = await TimbreRig.CompletionAsync(rig.Scope.Play(new SoundClip("tone.wav")));
            SoundPlaybackResult missing = await TimbreRig.CompletionAsync(rig.Scope.Play(new SoundClip("missing.wav")));
            SoundPlaybackResult nonSeekable = await TimbreRig.CompletionAsync(rig.Scope.Play(new SoundClip(SoundSource.FromStream(() => new NonSeekableStream()))));

            Assert.Equal(SoundErrorKind.UnsupportedFormat, unsupported.Error!.Kind);
            Assert.Equal(SoundErrorKind.SourceUnavailable, missing.Error!.Kind);
            Assert.Equal(SoundErrorKind.SourceUnavailable, nonSeekable.Error!.Kind);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task MissingOutputFailsPlaybacksWithDeviceUnavailable()
    {
        using SoundRuntime runtime = new();
        using SoundScope scope = runtime.CreateScope();

        SoundPlaybackResult result = await TimbreRig.CompletionAsync(scope.Play(TimbreRig.Clip(new DeterministicSoundSourceFactory(4800))));

        Assert.Equal(SoundPlaybackState.Failed, result.State);
        Assert.Equal(SoundErrorKind.DeviceUnavailable, result.Error!.Kind);
    }

    [Fact]
    public async Task OutputOpenFailureIsNotRetriedUntilTheNextPlay()
    {
        using TimbreRig rig = new(hold: false);
        rig.Output.OpenFailure = new InvalidOperationException("device busy");
        SoundClip clip = TimbreRig.Clip(new DeterministicSoundSourceFactory(4800));

        SoundPlaybackResult first = await TimbreRig.CompletionAsync(rig.Scope.Play(clip));
        await rig.SyncAsync();
        Assert.Equal(SoundErrorKind.DeviceUnavailable, first.Error!.Kind);
        Assert.Same(rig.Output.OpenFailure, first.Error.InnerException);
        Assert.Equal(1, rig.Output.OpenCount);

        rig.Output.OpenFailure = null;
        SoundPlayback second = rig.Scope.Play(clip);
        await rig.Output.WaitForSubmittedFramesAsync(TimbreRig.Block);
        Assert.Equal(2, rig.Output.OpenCount);
        Assert.Equal(SoundPlaybackState.Playing, second.State);
    }

    [Fact]
    public async Task DeviceLossFailsActivePlaybacksClosesTheOutputAndPlayReopens()
    {
        using TimbreRig rig = new();
        SoundClip clip = TimbreRig.Clip(new DeterministicSoundSourceFactory(48000));
        SoundPlayback first = rig.Scope.Play(clip);
        SoundPlayback second = rig.Scope.Play(clip);
        await rig.StartAsync(first, second);

        rig.Output.LoseDevice(new IOException("unplugged"));
        SoundPlaybackResult result = await TimbreRig.CompletionAsync(first);
        await TimbreRig.CompletionAsync(second);
        await rig.SyncAsync();

        Assert.Equal(SoundErrorKind.DeviceUnavailable, result.Error!.Kind);
        Assert.Equal(SoundPlaybackState.Failed, second.State);
        Assert.Equal(1, rig.Output.CloseCount);
        Assert.False(rig.Output.IsOpen);

        SoundPlayback third = rig.Scope.Play(clip);
        await rig.Output.WaitForSubmittedFramesAsync(TimbreRig.Budget + TimbreRig.Block);
        Assert.Equal(2, rig.Output.OpenCount);
        Assert.Equal(SoundPlaybackState.Playing, third.State);
    }

    [Fact]
    public async Task CancelKeepsQueuedPcmAndDoesNotClearTheSharedOutput()
    {
        using TimbreRig rig = new();
        SoundClip clip = TimbreRig.Clip(new DeterministicSoundSourceFactory(48000));
        SoundPlayback canceled = rig.Scope.Play(clip);
        SoundPlayback other = rig.Scope.Play(clip, start => start.Volume = 0.5f);
        await rig.StartAsync(canceled, other);

        canceled.Cancel();
        await rig.SyncAsync();

        Assert.Equal(TimbreRig.Budget, rig.Output.QueuedFrames);
        Assert.Equal(TimbreRig.Budget, rig.Output.SubmittedFrames);
        float[] block = await rig.NextBlockAsync();
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Block, Signal, 0.5f, TimbreRig.Budget), block);
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, Signal, 1.5f), rig.Output.Read(0, TimbreRig.Budget));
        await TimbreRig.ReleasedAsync(canceled);
        Assert.Equal(1, rig.Runtime.GetDiagnostics().PlaybacksCanceled);
    }

    [Fact]
    public async Task ReplacementWithQueuedPcmIsolatesOldNewAndUnrelatedVoices()
    {
        using TimbreRig rig = new();
        SoundClip clip = TimbreRig.Clip(new DeterministicSoundSourceFactory(48000));
        SoundHandle slot = rig.Scope.CreateHandle();
        SoundPlayback old = rig.Scope.Play(clip, handle: slot);
        SoundPlayback unrelated = rig.Scope.Play(clip, start => start.Volume = 0.25f);
        await rig.StartAsync(old, unrelated);

        SoundPlayback replacement = rig.Scope.Play(clip, start => start.Volume = 0.5f, handle: slot);
        await rig.ReadyAsync(replacement);
        float[] block = await rig.NextBlockAsync();

        Assert.Equal(SoundPlaybackState.Canceled, old.State);
        TimbreRig.AssertPcm(
            TimbreRig.Sum(
                TimbreRig.Expected(TimbreRig.Block, Signal, 0.25f, TimbreRig.Budget),
                TimbreRig.Expected(TimbreRig.Block, Signal, 0.5f, 0)),
            block);
    }

    [Fact]
    public async Task ParameterPublicationAffectsOnlyBlocksProducedAfterIt()
    {
        using TimbreRig rig = new();
        SoundPlayback playback = rig.Scope.Play(TimbreRig.Clip(new DeterministicSoundSourceFactory(48000)));
        await rig.StartAsync(playback);

        playback.Volume = 0.5f;
        float[] block = await rig.NextBlockAsync();

        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, Signal), rig.Output.Read(0, TimbreRig.Budget));
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Block, Signal, 0.5f, TimbreRig.Budget), block);
        Assert.Throws<ArgumentOutOfRangeException>(() => playback.Volume = 1.5f);
        Assert.Throws<ArgumentOutOfRangeException>(() => playback.Volume = float.NaN);
        Assert.Equal(0.5f, playback.Volume);
    }

    [Fact]
    public async Task SetValidatesDescriptorsPerPlayback()
    {
        using TimbreRig rig = new();
        SoundParameter<float> declared = new("Declared", 1f);
        SoundParameter<float> foreign = new("Declared", 1f);
        SoundClip clip = new(SoundSource.FromReader(new DeterministicSoundSourceFactory(48000).Open), parameters: [declared]);
        SoundPlayback first = rig.Scope.Play(clip);
        SoundPlayback second = rig.Scope.Play(clip);
        await rig.StartAsync(first, second);

        first.Set(declared, 5f);
        Assert.Throws<ArgumentException>(() => first.Set(foreign, 5f));
        Assert.Throws<ArgumentNullException>(() => first.Set<float>(null!, 5f));
        Assert.Throws<ArgumentOutOfRangeException>(() => first.Set(declared, float.NegativeInfinity));
        second.Cancel();
        Assert.Throws<InvalidOperationException>(() => second.Set(declared, 5f));
    }

    [Fact]
    public async Task StreamingUnderrunPadsCountedSilenceWithoutSkippingContent()
    {
        using TimbreRig rig = new();
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        DeterministicSoundSourceFactory factory = new(48000, maxFramesPerRead: 480);
        factory.Configure = reader => reader.ReadGate = _ => reader.Position >= 960 ? gate.Task : Task.CompletedTask;
        SoundPlayback playback = rig.Scope.Play(TimbreRig.Clip(factory, SoundLoading.Streaming));
        await rig.ReadyAsync(playback);
        DeterministicSoundReader reader = factory.Readers.Single();
        await reader.WaitForReadCountAsync(3);

        rig.Output.Release();
        await rig.Output.WaitForSubmittedFramesAsync(TimbreRig.Budget);
        float[] pcm = rig.Output.Read(0, TimbreRig.Budget);
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, (frame, channel) => frame < 960 ? Signal(frame, channel) : 0f), pcm);
        Assert.Equal(960, rig.Runtime.GetDiagnostics().UnderrunFrames);
        Assert.Equal(TimbreRig.FramesToTime(960), playback.Position);

        reader.ReadGate = null;
        gate.SetResult();
        await rig.SettledAsync(playback);
        float[] block = await rig.NextBlockAsync();
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Block, Signal, 1f, 960), block);
    }

    [Fact]
    public async Task ReplacedPlaybackWorkerNeverPublishesIntoTheNewOccupant()
    {
        using TimbreRig rig = new();
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<DeterministicSoundReader> opened = new(TaskCreationOptions.RunContinuationsAsynchronously);
        DeterministicSoundSourceFactory slow = new(48000, (_, _) => 0.9f, maxFramesPerRead: 512)
        {
            Configure = reader =>
            {
                reader.ReadGate = token => gate.Task.WaitAsync(token);
                opened.TrySetResult(reader);
            }
        };
        SoundHandle slot = rig.Scope.CreateHandle();
        SoundPlayback old = rig.Scope.Play(TimbreRig.Clip(slow, SoundLoading.Streaming), handle: slot);
        DeterministicSoundReader oldReader = await HarnessWait.WithTimeout(opened.Task, null, "Slow reader was not opened.");
        await oldReader.WaitForReadCountAsync(1);

        SoundPlayback current = rig.Scope.Play(TimbreRig.Clip(new DeterministicSoundSourceFactory(48000)), handle: slot);
        gate.SetResult();
        await TimbreRig.ReleasedAsync(old);
        float[] pcm = await rig.StartAsync(current);

        Assert.Equal(SoundPlaybackState.Canceled, old.State);
        Assert.True(oldReader.IsDisposed);
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, Signal), pcm);
        Assert.Equal(0, slow.LiveReaders);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().LiveSourcePumps);
        Assert.Equal(TimeSpan.Zero, old.Position);
    }

    [Fact]
    public async Task TwoScopesShareTheRuntimeAndAreIsolated()
    {
        using TimbreRig rig = new();
        SoundClip clip = TimbreRig.Clip(new DeterministicSoundSourceFactory(48000));
        using SoundScope windowA = rig.Runtime.CreateScope();
        using SoundScope windowB = rig.Runtime.CreateScope();
        SoundPlayback a = windowA.Play(clip);
        SoundPlayback b = windowB.Play(clip, start => start.Volume = 0.5f);
        await rig.StartAsync(a, b);

        a.Cancel();
        windowA.Dispose();
        float[] block = await rig.NextBlockAsync();

        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Block, Signal, 0.5f, TimbreRig.Budget), block);
        Assert.Equal(SoundPlaybackState.Playing, b.State);
        Assert.Equal(1, rig.Output.OpenCount);
        Assert.Equal(0, rig.Output.CloseCount);
    }

    [Fact]
    public async Task PrepareAsyncPreloadsSoPlayIsReadyFromTheCache()
    {
        using TimbreRig rig = new();
        DeterministicSoundSourceFactory factory = new(4800);
        SoundClip clip = TimbreRig.Clip(factory);

        await rig.Runtime.PrepareAsync(clip);
        Assert.Equal(1, factory.OpenCount);
        Assert.Equal(0, factory.LiveReaders);
        Assert.Equal(4800L * 8, rig.Runtime.GetDiagnostics().CacheBytes);
        SoundPlayback playback = rig.Scope.Play(clip);

        Assert.True(playback.WhenReady.IsCompleted);
        Assert.Equal(TimeSpan.FromSeconds(0.1), playback.Duration);
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, Signal), await rig.StartAsync(playback));
        Assert.Equal(1, factory.OpenCount);
        Assert.Equal(0, rig.Output.OpenCount - 1);
    }

    [Fact]
    public async Task LoadingPolicyRespectsAutoThresholdAndPreloadLimits()
    {
        using TimbreRig rig = new(options =>
        {
            options.AutoPreloadMaxBytes = 1000 * 8;
            options.MaxPreloadBytes = 2000 * 8;
        }, hold: false);
        DeterministicSoundSourceFactory small = new(1000);
        DeterministicSoundSourceFactory large = new(1001);
        DeterministicSoundSourceFactory tooLarge = new(2001);

        await rig.Runtime.PrepareAsync(TimbreRig.Clip(small, SoundLoading.Auto));
        SoundException refused = await Assert.ThrowsAsync<SoundException>(() => rig.Runtime.PrepareAsync(TimbreRig.Clip(tooLarge)));
        SoundPlaybackResult refusedPlay = await TimbreRig.CompletionAsync(rig.Scope.Play(TimbreRig.Clip(tooLarge)));
        SoundClip autoLarge = TimbreRig.Clip(large, SoundLoading.Auto);
        await rig.Runtime.PrepareAsync(autoLarge);
        await rig.ReadyAsync(rig.Scope.Play(autoLarge), rig.Scope.Play(autoLarge));

        Assert.Equal(SoundErrorKind.ResourceLimitExceeded, refused.Kind);
        Assert.Equal(SoundErrorKind.ResourceLimitExceeded, refusedPlay.Error!.Kind);
        Assert.Equal(1000L * 8, rig.Runtime.GetDiagnostics().CacheBytes);
        Assert.Equal(3, large.OpenCount); // prepare probe + one streaming reader per playback
        Assert.Equal(0, tooLarge.Readers.Sum(reader => reader.FramesRead));
    }

    [Fact]
    public async Task CacheEvictsOnlyUnpinnedPayloadsAndRefusesWhenPinnedDataFillsIt()
    {
        using TimbreRig rig = new(options => options.MaxCacheBytes = 3000 * 8);
        DeterministicSoundSourceFactory first = new(2000);
        DeterministicSoundSourceFactory second = new(2000);
        DeterministicSoundSourceFactory third = new(2000);
        SoundClip firstClip = TimbreRig.Clip(first);

        await rig.Runtime.PrepareAsync(firstClip);
        await rig.Runtime.PrepareAsync(TimbreRig.Clip(second)); // evicts the unpinned first payload
        Assert.Equal(2000L * 8, rig.Runtime.GetDiagnostics().CacheBytes);

        SoundPlayback pinned = rig.Scope.Play(firstClip, start => start.Loop = true);
        await rig.ReadyAsync(pinned); // reloads first and evicts second
        Assert.Equal(2, first.OpenCount);
        SoundException refused = await Assert.ThrowsAsync<SoundException>(() => rig.Runtime.PrepareAsync(TimbreRig.Clip(third)));
        Assert.Equal(SoundErrorKind.ResourceLimitExceeded, refused.Kind);
        Assert.Equal(1, rig.Runtime.GetDiagnostics().CacheEntries);

        pinned.Cancel();
        await TimbreRig.ReleasedAsync(pinned);
        await rig.Runtime.PrepareAsync(TimbreRig.Clip(third));
        Assert.Equal(1, rig.Runtime.GetDiagnostics().CacheEntries);
    }

    private sealed class NonSeekableStream : MemoryStream
    {
        public override bool CanSeek => false;
    }
}
