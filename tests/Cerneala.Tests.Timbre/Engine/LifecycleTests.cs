using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Engine;

public sealed class LifecycleTests
{
    private static readonly Func<long, int, float> Signal = DeterministicTimbreReader.DefaultSignal;

    [Fact]
    public async Task CompletionRequiresEndOfSourceAndAnObservedDrain()
    {
        using TimbreRig rig = new();
        TimbrePlayback playback = rig.Scope.Play(TimbreRig.Clip(new DeterministicTimbreSourceFactory(1000)));
        await rig.ReadyAsync(playback);
        rig.Output.Release();
        await rig.Output.WaitForSubmittedFramesAsync(1440);
        await rig.SyncAsync();

        float[] expected = TimbreRig.Expected(1440, (frame, channel) => frame < 1000 ? Signal(frame, channel) : 0f);
        TimbreRig.AssertPcm(expected, rig.Output.Read(0, 1440));
        Assert.Equal(1440, rig.Output.SubmittedFrames); // three blocks: the last holds frames 960..999
        Assert.Equal(TimbrePlaybackState.Playing, playback.State);

        rig.Output.Consume(960);
        await rig.SyncAsync();
        Assert.Equal(TimbrePlaybackState.Playing, playback.State);
        Assert.False(playback.Completion.IsCompleted);

        rig.Output.Consume(480);
        TimbrePlaybackResult result = await TimbreRig.CompletionAsync(playback);
        Assert.Equal(TimbrePlaybackState.Completed, result.State);
        Assert.Null(result.Error);
        Assert.False(result.TailTruncated);
        Assert.Equal(TimbrePlaybackState.Completed, playback.State);
        Assert.Same(result, await playback.Completion);
        Assert.Equal(1, rig.Runtime.GetDiagnostics().PlaybacksCompleted);
        Assert.Throws<InvalidOperationException>(() => playback.Volume = 0.5f);
        await TimbreRig.ReleasedAsync(playback);
    }

    [Fact]
    public async Task ReaderFailureFailsOnlyThatPlaybackAndIsNotEndOfSource()
    {
        using TimbreRig rig = new();
        DeterministicTimbreSourceFactory failing = new(48000, maxFramesPerRead: 256) { Configure = reader => reader.FailAtFrame = 3000 };
        TimbrePlayback broken = rig.Scope.Play(TimbreRig.Clip(failing, TimbreLoading.Streaming));
        TimbrePlayback healthy = rig.Scope.Play(TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000)));
        await rig.ReadyAsync(broken, healthy);
        rig.Output.Release();

        TimbrePlaybackResult result = await TimbreRig.CompletionAsync(broken);

        Assert.Equal(TimbrePlaybackState.Failed, result.State);
        Assert.Equal(TimbreErrorKind.SourceUnavailable, result.Error!.Kind);
        Assert.IsType<IOException>(result.Error.InnerException);
        Assert.NotEqual(TimbrePlaybackState.Failed, healthy.State);
        await TimbreRig.ReleasedAsync(broken);
        Assert.Equal(0, failing.LiveReaders);
        Assert.Equal(1, rig.Runtime.GetDiagnostics().PlaybacksFailed);
    }

    [Fact]
    public async Task PreloadReaderFailureFailsThePlayback()
    {
        using TimbreRig rig = new(hold: false);
        DeterministicTimbreSourceFactory failing = new(4000) { Configure = reader => reader.FailAtFrame = 10 };

        TimbrePlaybackResult result = await TimbreRig.CompletionAsync(rig.Scope.Play(TimbreRig.Clip(failing)));

        Assert.Equal(TimbrePlaybackState.Failed, result.State);
        Assert.Equal(TimbreErrorKind.SourceUnavailable, result.Error!.Kind);
    }

    [Fact]
    public async Task NonFiniteSamplesFailWithInvalidData()
    {
        using TimbreRig rig = new(hold: false);
        DeterministicTimbreSourceFactory corrupt = new(4000, (frame, channel) => frame == 100 ? float.NaN : 0.1f);

        TimbrePlaybackResult preload = await TimbreRig.CompletionAsync(rig.Scope.Play(TimbreRig.Clip(corrupt)));
        TimbrePlaybackResult streaming = await TimbreRig.CompletionAsync(rig.Scope.Play(TimbreRig.Clip(corrupt, TimbreLoading.Streaming)));

        Assert.Equal(TimbreErrorKind.InvalidData, preload.Error!.Kind);
        Assert.Equal(TimbreErrorKind.InvalidData, streaming.Error!.Kind);
    }

    [Fact]
    public async Task RepeatedSynchronousEmptyReadsViolateTheReaderContract()
    {
        using TimbreRig rig = new(hold: false);
        DeterministicTimbreSourceFactory violating = new(4000) { Configure = reader => reader.ViolateReadinessContract = true };

        TimbrePlaybackResult result = await TimbreRig.CompletionAsync(rig.Scope.Play(TimbreRig.Clip(violating, TimbreLoading.Streaming)));

        Assert.Equal(TimbrePlaybackState.Failed, result.State);
        Assert.Equal(TimbreErrorKind.InvalidData, result.Error!.Kind);
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

            TimbrePlaybackResult unsupported = await TimbreRig.CompletionAsync(rig.Scope.Play(new TimbreSound("tone.wav")));
            TimbrePlaybackResult missing = await TimbreRig.CompletionAsync(rig.Scope.Play(new TimbreSound("missing.wav")));
            TimbrePlaybackResult nonSeekable = await TimbreRig.CompletionAsync(rig.Scope.Play(new TimbreSound(TimbreSource.FromStream(() => new NonSeekableStream()))));

            Assert.Equal(TimbreErrorKind.UnsupportedFormat, unsupported.Error!.Kind);
            Assert.Equal(TimbreErrorKind.SourceUnavailable, missing.Error!.Kind);
            Assert.Equal(TimbreErrorKind.SourceUnavailable, nonSeekable.Error!.Kind);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task MissingOutputFailsPlaybacksWithDeviceUnavailable()
    {
        using TimbreRuntime runtime = new();
        using TimbreScope scope = runtime.CreateScope();

        TimbrePlaybackResult result = await TimbreRig.CompletionAsync(scope.Play(TimbreRig.Clip(new DeterministicTimbreSourceFactory(4800))));

        Assert.Equal(TimbrePlaybackState.Failed, result.State);
        Assert.Equal(TimbreErrorKind.DeviceUnavailable, result.Error!.Kind);
    }

    [Fact]
    public async Task OutputOpenFailureIsNotRetriedUntilTheNextPlay()
    {
        using TimbreRig rig = new(hold: false);
        rig.Output.OpenFailure = new InvalidOperationException("device busy");
        TimbreSound clip = TimbreRig.Clip(new DeterministicTimbreSourceFactory(4800));

        TimbrePlaybackResult first = await TimbreRig.CompletionAsync(rig.Scope.Play(clip));
        await rig.SyncAsync();
        Assert.Equal(TimbreErrorKind.DeviceUnavailable, first.Error!.Kind);
        Assert.Same(rig.Output.OpenFailure, first.Error.InnerException);
        Assert.Equal(1, rig.Output.OpenCount);

        rig.Output.OpenFailure = null;
        TimbrePlayback second = rig.Scope.Play(clip);
        await rig.Output.WaitForSubmittedFramesAsync(TimbreRig.Block);
        Assert.Equal(2, rig.Output.OpenCount);
        Assert.Equal(TimbrePlaybackState.Playing, second.State);
    }

    [Fact]
    public async Task DeviceLossFailsActivePlaybacksClosesTheOutputAndPlayReopens()
    {
        using TimbreRig rig = new();
        TimbreSound clip = TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000));
        TimbrePlayback first = rig.Scope.Play(clip);
        TimbrePlayback second = rig.Scope.Play(clip);
        await rig.StartAsync(first, second);

        rig.Output.LoseDevice(new IOException("unplugged"));
        TimbrePlaybackResult result = await TimbreRig.CompletionAsync(first);
        await TimbreRig.CompletionAsync(second);
        await rig.SyncAsync();

        Assert.Equal(TimbreErrorKind.DeviceUnavailable, result.Error!.Kind);
        Assert.Equal(TimbrePlaybackState.Failed, second.State);
        Assert.Equal(1, rig.Output.CloseCount);
        Assert.False(rig.Output.IsOpen);

        TimbrePlayback third = rig.Scope.Play(clip);
        await rig.Output.WaitForSubmittedFramesAsync(TimbreRig.Budget + TimbreRig.Block);
        Assert.Equal(2, rig.Output.OpenCount);
        Assert.Equal(TimbrePlaybackState.Playing, third.State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeviceLossReportedWhileTheOutputIsOpeningFailsThePlayback(bool lossDuringOpen)
    {
        LossDuringOpenOutput output = new(lossDuringOpen);
        using TimbreRig rig = new(options => options.Output = output);
        output.Inner = rig.Output;
        TimbreSound clip = TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000));
        TimbrePlayback playback = rig.Scope.Play(clip);

        if (!lossDuringOpen)
        {
            await rig.StartAsync(playback);
            Assert.Equal(TimbrePlaybackState.Playing, playback.State);
            Assert.Equal(0, rig.Output.CloseCount);
            return;
        }

        await rig.ReadyAsync(playback);
        rig.Output.Release(); // let the runtime mix, as the control does
        Exception? timeout = await Record.ExceptionAsync(async () => await TimbreRig.CompletionAsync(playback));
        Assert.True(
            timeout is null,
            $"The playback never ended after the device loss during Open (state {playback.State}, output open: {rig.Output.IsOpen}, Close calls: {rig.Output.CloseCount}): {timeout?.Message}");
        TimbrePlaybackResult result = await playback.Completion;

        Assert.Equal(TimbrePlaybackState.Failed, result.State);
        Assert.Equal(TimbreErrorKind.DeviceUnavailable, result.Error!.Kind);
        await rig.SyncAsync();
        Assert.Equal(1, rig.Output.CloseCount);
    }

    [Fact]
    public async Task StaleDeviceLossReportedWhileTheOutputIsClosedDoesNotFailTheNextPlayback()
    {
        LossDuringOpenOutput output = new(lossDuringOpen: false);
        using TimbreRig rig = new(options => options.Output = output);
        output.Inner = rig.Output;
        TimbreSound clip = TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000));
        TimbrePlayback first = rig.Scope.Play(clip);
        await rig.StartAsync(first);
        rig.Output.LoseDevice(new IOException("unplugged"));
        await TimbreRig.CompletionAsync(first);
        await rig.SyncAsync();
        Assert.Equal(1, rig.Output.CloseCount);

        // A late callback of the closed device reaches the runtime's client again.
        output.Client!.NotifyDeviceLost(new IOException("late callback"));
        await rig.SyncAsync();
        TimbrePlayback second = rig.Scope.Play(clip);
        await rig.Output.WaitForSubmittedFramesAsync(TimbreRig.Budget + TimbreRig.Block);
        await rig.SyncAsync();

        Assert.Equal(TimbrePlaybackState.Playing, second.State);
        Assert.Equal(2, rig.Output.OpenCount);
        Assert.Equal(1, rig.Output.CloseCount);
    }

    [Fact]
    public async Task CancelKeepsQueuedPcmAndDoesNotClearTheSharedOutput()
    {
        using TimbreRig rig = new();
        TimbreSound clip = TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000));
        TimbrePlayback canceled = rig.Scope.Play(clip);
        TimbrePlayback other = rig.Scope.Play(clip, start => start.Volume = 0.5f);
        await rig.StartAsync(canceled, other);

        canceled.Cancel();
        await rig.SyncAsync();

        Assert.Equal(TimbreRig.Budget, rig.Output.QueuedFrames);
        Assert.Equal(TimbreRig.Budget, rig.Output.SubmittedFrames);
        float[] block = await rig.NextBlockAsync();
        TimbreRig.AssertPcm(TimbreRig.Sum(
            TimbreRig.ExpectedRamp(TimbreRig.Block, Signal, 1f, 0f, TimbreRig.Budget),
            TimbreRig.Expected(TimbreRig.Block, Signal, 0.5f, TimbreRig.Budget)), block);
        Assert.Equal(TimbreRig.FramesToTime(TimbreRig.Budget + TimbreRig.FadeFrames), canceled.Position);
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Block, Signal, 0.5f, TimbreRig.Budget + TimbreRig.Block), await rig.NextBlockAsync());
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, Signal, 1.5f), rig.Output.Read(0, TimbreRig.Budget));
        await TimbreRig.ReleasedAsync(canceled);
        Assert.Equal(1, rig.Runtime.GetDiagnostics().PlaybacksCanceled);
    }

    [Fact]
    public async Task ReplacementWithQueuedPcmIsolatesOldNewAndUnrelatedVoices()
    {
        using TimbreRig rig = new();
        TimbreSound clip = TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000));
        TimbreHandle slot = rig.Scope.CreateHandle();
        TimbrePlayback old = rig.Scope.Play(clip, handle: slot);
        TimbrePlayback unrelated = rig.Scope.Play(clip, start => start.Volume = 0.25f);
        await rig.StartAsync(old, unrelated);

        TimbrePlayback replacement = rig.Scope.Play(clip, start => start.Volume = 0.5f, handle: slot);
        await rig.ReadyAsync(replacement);
        float[] block = await rig.NextBlockAsync();

        Assert.Equal(TimbrePlaybackState.Canceled, old.State);
        TimbreRig.AssertPcm(
            TimbreRig.Sum(
                TimbreRig.Expected(TimbreRig.Block, Signal, 0.25f, TimbreRig.Budget),
                TimbreRig.ExpectedRamp(TimbreRig.Block, Signal, 0f, 0.5f, 0),
                TimbreRig.ExpectedRamp(TimbreRig.Block, Signal, 1f, 0f, TimbreRig.Budget)),
            block);
        Assert.Equal(TimbreRig.FramesToTime(TimbreRig.Budget + TimbreRig.FadeFrames), old.Position);
        await TimbreRig.ReleasedAsync(old);
    }

    [Fact]
    public async Task ParameterPublicationAffectsOnlyBlocksProducedAfterIt()
    {
        using TimbreRig rig = new();
        TimbrePlayback playback = rig.Scope.Play(TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000)));
        await rig.StartAsync(playback);

        playback.Volume = 0.5f;
        float[] block = await rig.NextBlockAsync();

        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, Signal), rig.Output.Read(0, TimbreRig.Budget));
        TimbreRig.AssertPcm(TimbreRig.ExpectedRamp(TimbreRig.Block, Signal, 1f, 0.5f, TimbreRig.Budget), block);
        Assert.Throws<ArgumentOutOfRangeException>(() => playback.Volume = 1.5f);
        Assert.Throws<ArgumentOutOfRangeException>(() => playback.Volume = float.NaN);
        Assert.Equal(0.5f, playback.Volume);
    }

    [Fact]
    public async Task SetValidatesDescriptorsPerPlayback()
    {
        using TimbreRig rig = new();
        TimbreParameter<float> declared = new("Declared", 1f);
        TimbreParameter<float> foreign = new("Declared", 1f);
        TimbreSound clip = new(TimbreSource.FromReader(new DeterministicTimbreSourceFactory(48000).Open), parameters: [declared]);
        TimbrePlayback first = rig.Scope.Play(clip);
        TimbrePlayback second = rig.Scope.Play(clip);
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
        // The source stalls after its first full software queue plus one block:
        // the stream starts unpadded, and the later starvation is padded.
        const int stall = TimbreRig.Budget + TimbreRig.Block;
        using TimbreRig rig = new();
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        DeterministicTimbreSourceFactory factory = new(48000, maxFramesPerRead: 480);
        factory.Configure = reader => reader.ReadGate = _ => reader.Position >= stall ? gate.Task : Task.CompletedTask;
        TimbrePlayback playback = rig.Scope.Play(TimbreRig.Clip(factory, TimbreLoading.Streaming));
        await rig.ReadyAsync(playback);
        DeterministicTimbreReader reader = factory.Readers.Single();
        await reader.WaitForReadCountAsync((stall / TimbreRig.Block) + 1);

        rig.Output.Release();
        await rig.Output.WaitForSubmittedFramesAsync(TimbreRig.Budget);
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, Signal), rig.Output.Read(0, TimbreRig.Budget));
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Block, Signal, 1f, TimbreRig.Budget), await rig.NextBlockAsync());
        Assert.Equal(0, rig.Runtime.GetDiagnostics().UnderrunFrames);

        // While blocks are still queued the late source defers mixing; only an
        // output about to run dry gets a padded block.
        long beforePadding = rig.Output.SubmittedFrames;
        rig.Output.Consume(TimbreRig.Block);
        await rig.SyncAsync();
        Assert.Equal(beforePadding, rig.Output.SubmittedFrames);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().UnderrunFrames);

        rig.Output.ConsumeAll();
        await rig.Output.WaitForSubmittedFramesAsync(beforePadding + TimbreRig.Block);
        await rig.SyncAsync();
        Assert.Equal(beforePadding + TimbreRig.Block, rig.Output.SubmittedFrames);
        float[] padded = rig.Output.Read(beforePadding, TimbreRig.Block);
        TimbreRig.AssertPcm(new float[TimbreRig.Block * 2], padded);
        Assert.Equal(TimbreRig.Block, rig.Runtime.GetDiagnostics().UnderrunFrames);
        Assert.Equal(TimbreRig.FramesToTime(stall), playback.Position);

        reader.ReadGate = null;
        gate.SetResult();
        await rig.SettledAsync(playback);
        // The block right after the padding resumes exactly where the source stalled.
        await rig.Output.WaitForSubmittedFramesAsync(beforePadding + (2 * TimbreRig.Block));
        float[] block = rig.Output.Read(beforePadding + TimbreRig.Block, TimbreRig.Block);
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Block, Signal, 1f, stall), block);
    }

    [Fact]
    public async Task ReplacedPlaybackWorkerNeverPublishesIntoTheNewOccupant()
    {
        using TimbreRig rig = new();
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<DeterministicTimbreReader> opened = new(TaskCreationOptions.RunContinuationsAsynchronously);
        DeterministicTimbreSourceFactory slow = new(48000, (_, _) => 0.9f, maxFramesPerRead: 512)
        {
            Configure = reader =>
            {
                reader.ReadGate = token => gate.Task.WaitAsync(token);
                opened.TrySetResult(reader);
            }
        };
        TimbreHandle slot = rig.Scope.CreateHandle();
        TimbrePlayback old = rig.Scope.Play(TimbreRig.Clip(slow, TimbreLoading.Streaming), handle: slot);
        DeterministicTimbreReader oldReader = await HarnessWait.WithTimeout(opened.Task, null, "Slow reader was not opened.");
        await oldReader.WaitForReadCountAsync(1);

        TimbrePlayback current = rig.Scope.Play(TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000)), handle: slot);
        gate.SetResult();
        await TimbreRig.ReleasedAsync(old);
        float[] pcm = await rig.StartAsync(current);

        Assert.Equal(TimbrePlaybackState.Canceled, old.State);
        Assert.True(oldReader.IsDisposed);
        TimbreRig.AssertPcm(TimbreRig.ExpectedRamp(TimbreRig.Budget, Signal, 0f, 1f), pcm);
        Assert.Equal(0, slow.LiveReaders);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().LiveSourcePumps);
        Assert.Equal(TimeSpan.Zero, old.Position);
    }

    [Fact]
    public async Task TwoScopesShareTheRuntimeAndAreIsolated()
    {
        using TimbreRig rig = new();
        TimbreSound clip = TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000));
        using TimbreScope windowA = rig.Runtime.CreateScope();
        using TimbreScope windowB = rig.Runtime.CreateScope();
        TimbrePlayback a = windowA.Play(clip);
        TimbrePlayback b = windowB.Play(clip, start => start.Volume = 0.5f);
        await rig.StartAsync(a, b);

        a.Cancel();
        windowA.Dispose();
        float[] block = await rig.NextBlockAsync();

        TimbreRig.AssertPcm(TimbreRig.Sum(
            TimbreRig.ExpectedRamp(TimbreRig.Block, Signal, 1f, 0f, TimbreRig.Budget),
            TimbreRig.Expected(TimbreRig.Block, Signal, 0.5f, TimbreRig.Budget)), block);
        Assert.Equal(TimbrePlaybackState.Playing, b.State);
        Assert.Equal(1, rig.Output.OpenCount);
        Assert.Equal(0, rig.Output.CloseCount);
    }

    [Fact]
    public async Task PrepareAsyncPreloadsSoPlayIsReadyFromTheCache()
    {
        using TimbreRig rig = new();
        DeterministicTimbreSourceFactory factory = new(4800);
        TimbreSound clip = TimbreRig.Clip(factory);

        await rig.Runtime.PrepareAsync(clip);
        Assert.Equal(1, factory.OpenCount);
        Assert.Equal(0, factory.LiveReaders);
        Assert.Equal(4800L * 8, rig.Runtime.GetDiagnostics().CacheBytes);
        TimbrePlayback playback = rig.Scope.Play(clip);

        Assert.True(playback.WhenReady.IsCompleted);
        Assert.Equal(TimeSpan.FromSeconds(0.1), playback.Duration);
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, Signal), await rig.StartAsync(playback));
        Assert.Equal(1, factory.OpenCount);
        Assert.Equal(0, rig.Output.OpenCount - 1);
    }

    // 922337203686 frames is the first length whose frames * TicksPerSecond
    // exceeds long.MaxValue; its exact duration still fits a TimeSpan.
    [Theory]
    [InlineData(48000L, 10_000_000L)]
    [InlineData(922337203686L, 192153584101250L)]
    [InlineData(long.MaxValue, long.MaxValue)]
    public async Task StreamingPlaybackDurationIsExactOrClampedForAnyReportedLength(long lengthFrames, long expectedTicks)
    {
        using TimbreRig rig = new();
        TimbrePlayback playback = rig.Scope.Play(TimbreRig.Clip(new DeterministicTimbreSourceFactory(lengthFrames), TimbreLoading.Streaming));
        try
        {
            await rig.ReadyAsync(playback);

            Assert.Equal(TimeSpan.FromTicks(expectedTicks), playback.Duration);
        }
        finally
        {
            playback.Cancel();
        }
    }

    [Fact]
    public async Task LoadingPolicyRespectsAutoThresholdAndPreloadLimits()
    {
        using TimbreRig rig = new(options =>
        {
            options.AutoPreloadMaxBytes = 1000 * 8;
            options.MaxPreloadBytes = 2000 * 8;
        }, hold: false);
        DeterministicTimbreSourceFactory small = new(1000);
        DeterministicTimbreSourceFactory large = new(1001);
        DeterministicTimbreSourceFactory tooLarge = new(2001);

        await rig.Runtime.PrepareAsync(TimbreRig.Clip(small, TimbreLoading.Auto));
        TimbreException refused = await Assert.ThrowsAsync<TimbreException>(() => rig.Runtime.PrepareAsync(TimbreRig.Clip(tooLarge)));
        TimbrePlaybackResult refusedPlay = await TimbreRig.CompletionAsync(rig.Scope.Play(TimbreRig.Clip(tooLarge)));
        TimbreSound autoLarge = TimbreRig.Clip(large, TimbreLoading.Auto);
        await rig.Runtime.PrepareAsync(autoLarge);
        await rig.ReadyAsync(rig.Scope.Play(autoLarge), rig.Scope.Play(autoLarge));

        Assert.Equal(TimbreErrorKind.ResourceLimitExceeded, refused.Kind);
        Assert.Equal(TimbreErrorKind.ResourceLimitExceeded, refusedPlay.Error!.Kind);
        Assert.Equal(1000L * 8, rig.Runtime.GetDiagnostics().CacheBytes);
        Assert.Equal(3, large.OpenCount); // prepare probe + one streaming reader per playback
        Assert.Equal(0, tooLarge.Readers.Sum(reader => reader.FramesRead));
    }

    [Theory]
    [InlineData(1000L, TimbreLoading.Preload, false)]
    [InlineData(2001L, TimbreLoading.Preload, true)]
    [InlineData(long.MaxValue, TimbreLoading.Preload, true)]
    [InlineData(long.MaxValue, TimbreLoading.Auto, false)]
    public async Task HugeReportedLengthIsRefusedAsAResourceLimitOrStreamedNeverAsSourceUnavailable(
        long lengthFrames, TimbreLoading loading, bool expectResourceLimitFailure)
    {
        using TimbreRig rig = new(options =>
        {
            options.AutoPreloadMaxBytes = 1000 * 8;
            options.MaxPreloadBytes = 2000 * 8;
        }, hold: false);
        TimbreSound sound = TimbreRig.Clip(new DeterministicTimbreSourceFactory(lengthFrames), loading);

        TimbreErrorKind? prepareError = null;
        try
        {
            await rig.Runtime.PrepareAsync(sound);
        }
        catch (TimbreException exception)
        {
            prepareError = exception.Kind;
        }

        Assert.Equal(expectResourceLimitFailure ? TimbreErrorKind.ResourceLimitExceeded : null, prepareError);

        TimbrePlayback playback = rig.Scope.Play(sound);
        try
        {
            if (expectResourceLimitFailure)
            {
                await TimbreRig.CompletionAsync(playback);
            }
            else
            {
                await HarnessWait.WithTimeout(
                    Task.WhenAny(playback.Completion, playback.WhenReady),
                    null,
                    "Playback neither became ready nor completed.");
            }

            string observed = $"state={playback.State}";
            TimbreException? error = null;
            if (playback.Completion.IsCompleted)
            {
                TimbrePlaybackResult result = await playback.Completion;
                error = result.Error;
                observed = $"state={result.State}, error={error?.Kind}: {error?.Message}";
            }

            Assert.True(
                expectResourceLimitFailure
                    ? error?.Kind == TimbreErrorKind.ResourceLimitExceeded
                    : playback.State != TimbrePlaybackState.Failed,
                $"length={lengthFrames}, loading={loading}: {observed}.");
        }
        finally
        {
            playback.Cancel();
        }
    }

    // A negative total violates the reader contract (InvalidData), whatever the
    // value and the loading mode, and never surfaces as an arithmetic failure.
    [Theory]
    [InlineData(-1L, TimbreLoading.Preload)]
    [InlineData(-1L, TimbreLoading.Auto)]
    [InlineData(-1L, TimbreLoading.Streaming)]
    [InlineData(long.MinValue, TimbreLoading.Preload)]
    [InlineData(long.MinValue, TimbreLoading.Auto)]
    [InlineData(long.MinValue, TimbreLoading.Streaming)]
    public async Task NegativeReportedLengthFailsAsInvalidDataInEveryLoadingMode(long lengthFrames, TimbreLoading loading)
    {
        using TimbreRig rig = new(hold: false);
        int reads = 0;
        TimbreSound sound = new(
            TimbreSource.FromReader(() => new FixedLengthReader(lengthFrames, () => Interlocked.Increment(ref reads)), "negative"),
            1f,
            false,
            loading);

        TimbreException prepared = await Assert.ThrowsAsync<TimbreException>(() => rig.Runtime.PrepareAsync(sound));
        TimbrePlaybackResult played = await TimbreRig.CompletionAsync(rig.Scope.Play(sound));

        Assert.Equal(TimbreErrorKind.InvalidData, prepared.Kind);
        Assert.Contains(lengthFrames.ToString(System.Globalization.CultureInfo.InvariantCulture), prepared.Message, StringComparison.Ordinal);
        Assert.Equal(TimbrePlaybackState.Failed, played.State);
        Assert.Equal(TimbreErrorKind.InvalidData, played.Error!.Kind);
        Assert.Contains(lengthFrames.ToString(System.Globalization.CultureInfo.InvariantCulture), played.Error.Message, StringComparison.Ordinal);
        Assert.Equal(0, Volatile.Read(ref reads));
    }

    // One preloaded payload is one float[]: a declared length above
    // Array.MaxLength / 2 frames cannot be held, whatever the configured limits.
    [Theory]
    [InlineData(0x7FFFFFC7L / 2 + 1)]
    [InlineData(1L << 31)]
    public async Task PreloadLargerThanOneManagedArrayIsRefusedAsAResourceLimitBeforeReading(long lengthFrames)
    {
        const long limit = 64L << 30;
        Assert.Equal(0x7FFFFFC7, Array.MaxLength);
        using TimbreRig rig = new(options =>
        {
            options.MaxPreloadBytes = limit;
            options.MaxCacheBytes = limit;
        }, hold: false);
        DeterministicTimbreSourceFactory factory = new(lengthFrames);
        TimbreSound sound = TimbreRig.Clip(factory);

        TimbreException prepared = await Assert.ThrowsAsync<TimbreException>(() => rig.Runtime.PrepareAsync(sound));
        TimbrePlaybackResult played = await TimbreRig.CompletionAsync(rig.Scope.Play(sound));

        Assert.Equal(TimbreErrorKind.ResourceLimitExceeded, prepared.Kind);
        Assert.Equal(TimbreErrorKind.ResourceLimitExceeded, played.Error!.Kind);
        Assert.Equal(0, factory.Readers.Sum(reader => reader.FramesRead));
        Assert.Equal(0, rig.Runtime.GetDiagnostics().CacheBytes);
    }

    [Fact]
    public async Task CacheEvictsOnlyUnpinnedPayloadsAndRefusesWhenPinnedDataFillsIt()
    {
        using TimbreRig rig = new(options => options.MaxCacheBytes = 3000 * 8);
        DeterministicTimbreSourceFactory first = new(2000);
        DeterministicTimbreSourceFactory second = new(2000);
        DeterministicTimbreSourceFactory third = new(2000);
        TimbreSound firstClip = TimbreRig.Clip(first);

        await rig.Runtime.PrepareAsync(firstClip);
        await rig.Runtime.PrepareAsync(TimbreRig.Clip(second)); // evicts the unpinned first payload
        Assert.Equal(2000L * 8, rig.Runtime.GetDiagnostics().CacheBytes);

        TimbrePlayback pinned = rig.Scope.Play(firstClip, start => start.Loop = true);
        await rig.ReadyAsync(pinned); // reloads first and evicts second
        Assert.Equal(2, first.OpenCount);
        TimbreException refused = await Assert.ThrowsAsync<TimbreException>(() => rig.Runtime.PrepareAsync(TimbreRig.Clip(third)));
        Assert.Equal(TimbreErrorKind.ResourceLimitExceeded, refused.Kind);
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

    // Reports any length, including an invalid one, and ends at once.
    private sealed class FixedLengthReader(long lengthFrames, Action onRead) : TimbreReader
    {
        public override long? LengthFrames => lengthFrames;

        public override ValueTask<TimbreReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken)
        {
            onRead();
            return ValueTask.FromResult(new TimbreReadResult(0, true));
        }

        public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    // Opens the inner sink and, when asked, reports a device loss to the runtime's client before Open returns.
    private sealed class LossDuringOpenOutput(bool lossDuringOpen) : ITimbreOutput
    {
        public DeterministicTimbreOutput Inner { get; set; } = null!;

        public ITimbreOutputClient? Client { get; private set; }

        public int QueuedFrames => Inner.QueuedFrames;

        public void Open(ITimbreOutputClient client)
        {
            Client = client;
            Inner.Open(client);
            if (lossDuringOpen)
            {
                client.NotifyDeviceLost(new IOException("unplugged"));
            }
        }

        public void Submit(ReadOnlySpan<float> samples) => Inner.Submit(samples);

        public void Close() => Inner.Close();
    }
}
