using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;
using Cerneala.Timbre.Dsp;

namespace Cerneala.Tests.Timbre.Dsp;

// DSP through the concrete runtime: playbacks, mixer thread, feeds, and sink.
public sealed class DspChainEngineTests
{
    private const int Block = TimbreRig.Block;
    private const int Budget = TimbreRig.Budget;
    private const float Tolerance = 2e-5f;

    private static float Impulse(long frame, int channel) => frame == 0 ? (channel == 0 ? 1f : 0.5f) : 0f;

    [Fact]
    public async Task LowPassClipMatchesTheOracleTimesPostChainVolume()
    {
        using TimbreRig rig = new();
        TimbrePlayback playback = rig.Scope.Play(
            Clip(48000, DspOracle.Noise, modifiers: [new LowPass(cutoff: 1500f)]),
            start => start.Volume = 0.5f);

        float[] pcm = await rig.StartAsync(playback);

        (double[] left, double[] right) = DspOracle.Channels(DspOracle.Noise, Budget);
        TimbreRig.AssertPcm(DspOracle.Interleave(DspOracle.LowPass(left, 1500), DspOracle.LowPass(right, 1500), 0.5), pcm, Tolerance);
    }

    [Fact]
    public async Task ChainRunsInDeclarationOrderWithIndependentStageState()
    {
        using TimbreRig rig = new();
        TimbreSound clip = Clip(48000, DspOracle.Noise, modifiers: [new Delay(time: 300f / 48000f, feedback: 0.6f, mix: 0.4f), new LowPass(cutoff: 2000f)]);
        TimbrePlayback playback = rig.Scope.Play(clip);

        float[] pcm = await rig.StartAsync(playback);

        Assert.Equal([typeof(DelayKernel), typeof(LowPassKernel)], TimbreDspChain.Create(clip)!.StageKinds);
        (double[] left, double[] right) = DspOracle.Channels(DspOracle.Noise, Budget);
        double[] l = DspOracle.LowPass(DspOracle.Delay(left, 300, 0.6, 0.4), 2000);
        double[] r = DspOracle.LowPass(DspOracle.Delay(right, 300, 0.6, 0.4), 2000);
        TimbreRig.AssertPcm(DspOracle.Interleave(l, r), pcm, Tolerance);
        Assert.Null(TimbreDspChain.Create(new TimbreSound("plain.wav")));
    }

    [Fact]
    public async Task TwoPlaybacksOfOneModifiedClipKeepIndependentDspState()
    {
        using TimbreRig rig = new();
        TimbreSound clip = Clip(48000, Impulse, modifiers: [new Delay(time: 500f / 48000f, feedback: 0.5f, mix: 0.5f)]);
        TimbrePlayback first = rig.Scope.Play(clip);
        await rig.StartAsync(first);

        TimbrePlayback second = rig.Scope.Play(clip);
        await rig.ReadyAsync(second);
        float[] block = await rig.NextBlockAsync();

        (double[] left, double[] right) = DspOracle.Channels(Impulse, Budget + Block);
        double[] l = DspOracle.Delay(left, 500, 0.5, 0.5);
        double[] r = DspOracle.Delay(right, 500, 0.5, 0.5);
        float[] expected = TimbreRig.Sum(
            DspOracle.Interleave(l, r, start: Budget, frames: Block),
            DspOracle.Interleave(l, r, start: 0, frames: Block));
        TimbreRig.AssertPcm(expected, block, Tolerance);
    }

    [Fact]
    public async Task PlainAndModifiedPlaybacksShareOneMix()
    {
        using TimbreRig rig = new();
        TimbrePlayback plain = rig.Scope.Play(Clip(48000, DspOracle.Noise), start => start.Volume = 0.5f);
        TimbrePlayback modified = rig.Scope.Play(Clip(48000, Impulse, modifiers: [new LowPass(cutoff: 3000f), new Delay(time: 700f / 48000f, feedback: 0.3f, mix: 0.6f)]));

        float[] pcm = await rig.StartAsync(plain, modified);

        (double[] left, double[] right) = DspOracle.Channels(Impulse, Budget);
        float[] chain = DspOracle.Interleave(
            DspOracle.Delay(DspOracle.LowPass(left, 3000), 700, 0.3, 0.6),
            DspOracle.Delay(DspOracle.LowPass(right, 3000), 700, 0.3, 0.6));
        TimbreRig.AssertPcm(TimbreRig.Sum(TimbreRig.Expected(Budget, DspOracle.Noise, 0.5f), chain), pcm, Tolerance);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().ClippedSamples);
    }

    [Fact]
    public async Task DryAndWetEndpointsThroughTheEngine()
    {
        using TimbreRig rig = new();
        TimbrePlayback dry = rig.Scope.Play(Clip(48000, DspOracle.Noise, modifiers: [new Delay(time: 0.01f, feedback: 0.5f, mix: 0f)]));
        float[] pcm = await rig.StartAsync(dry);
        TimbreRig.AssertPcm(TimbreRig.Expected(Budget, DspOracle.Noise), pcm, 0f);
        dry.Cancel();

        using TimbreRig wetRig = new();
        TimbrePlayback wet = wetRig.Scope.Play(Clip(48000, DspOracle.Noise, modifiers: [new Delay(time: 0.01f, feedback: 0f, mix: 1f)]));
        float[] wetPcm = await wetRig.StartAsync(wet);
        TimbreRig.AssertPcm(
            TimbreRig.Expected(Budget, (frame, channel) => frame >= 480 ? DspOracle.Noise(frame - 480, channel) : 0f),
            wetPcm,
            0f);
    }

    // Finite source samples whose feedback overflows the delay line: the
    // output is still hard-clipped to ±1, never NaN. The overflowing playback
    // fails with InvalidData and contributes nothing from the block in which
    // its chain produced a non-finite sample; the other playback continues.
    [Fact]
    public async Task OverflowingDelayFeedbackStillReachesTheOutputHardClipped()
    {
        using TimbreRig rig = new();
        TimbrePlayback overflowing = rig.Scope.Play(Clip(48000, (_, _) => 3e38f, modifiers: [new Delay(time: 0.001f, feedback: 0.95f, mix: 0f)]));
        TimbrePlayback steady = rig.Scope.Play(Clip(48000, (_, _) => 0.25f));

        float[] pcm = await rig.StartAsync(overflowing, steady);

        for (int index = 0; index < pcm.Length; index++)
        {
            Assert.True(pcm[index] is >= -1f and <= 1f, $"Sample {index} (frame {index / 2}) is {pcm[index]}.");
        }

        TimbrePlaybackResult result = await TimbreRig.CompletionAsync(overflowing);
        Assert.Equal(TimbrePlaybackState.Failed, result.State);
        Assert.Equal(TimbreErrorKind.InvalidData, result.Error!.Kind);
        TimbreRig.AssertPcm(TimbreRig.Expected(Budget, (_, _) => 0.25f), pcm, 0f);
        Assert.Equal(TimbrePlaybackState.Playing, steady.State);
    }

    [Fact]
    public async Task ParameterChangesApplyFromTheNextBlockAndInvalidValuesKeepThePreviousOne()
    {
        using TimbreRig rig = new();
        TimbreParameter<float> cutoff = new("ToneCutoff", 1200f);
        Func<long, int, float> sine = (frame, channel) => (float)Math.Sin(2 * Math.PI * 4000 * frame / 48000) * (channel == 0 ? 1f : 0.5f);
        TimbreSound clip = Clip(480000, sine, parameters: [cutoff], modifiers: [new LowPass(cutoff: cutoff)]);
        TimbrePlayback playback = rig.Scope.Play(clip);
        float[] initial = await rig.StartAsync(playback);

        (double[] left, double[] right) = DspOracle.Channels(sine, Budget + Block);
        float[] oldCutoff = DspOracle.Interleave(DspOracle.LowPass(left, 1200), DspOracle.LowPass(right, 1200));
        TimbreRig.AssertPcm(DspOracle.Interleave(oldCutoff, 0, Budget), initial, Tolerance);

        Assert.Throws<ArgumentOutOfRangeException>(() => playback.Set(cutoff, 25000f));
        Assert.Throws<ArgumentOutOfRangeException>(() => playback.Set(cutoff, float.NaN));
        float[] unchanged = await rig.NextBlockAsync();
        TimbreRig.AssertPcm(DspOracle.Interleave(oldCutoff, Budget, Block), unchanged, Tolerance);

        playback.Set(cutoff, 6000f);
        List<float> after = [];
        for (int block = 0; block < 40; block++)
        {
            after.AddRange(await rig.NextBlockAsync());
        }

        float[] settled = after.ToArray();
        Assert.All(settled, sample => Assert.True(float.IsFinite(sample)));
        double amplitude = DspOracle.Amplitude(settled, 0, Block * 20, Block * 20, 4000);
        double expected = DspOracle.Magnitude(4000, 6000);
        Assert.InRange(amplitude, expected - 1e-3, expected + 1e-3);
        Assert.True(Math.Abs(expected - DspOracle.Magnitude(4000, 1200)) > 0.5);
        Assert.Equal(1200f, cutoff.DefaultValue);
    }

    [Fact]
    public async Task DelayTailContinuesAfterEndOfSourceAndCompletesAfterTheCriterionAndDrain()
    {
        using TimbreRig rig = new(hold: false);
        TimbrePlayback playback = rig.Scope.Play(
            Clip(Block, Impulse, modifiers: [new Delay(time: 600f / 48000f, feedback: 0.5f, mix: 1f)]),
            start => start.Volume = 0.25f);

        TimbrePlaybackResult result = await DrainAsync(rig, playback);

        // Oracle: pre-volume chain output; the tail ends at the first frame after
        // the source where the last 600 frames all stayed below 1e-6.
        int total = Block * 40;
        (double[] left, double[] right) = DspOracle.Channels(Impulse, Block);
        double[] l = DspOracle.Delay(DspOracle.Concat(left, DspOracle.Zeros(total - Block)), 600, 0.5, 1);
        double[] r = DspOracle.Delay(DspOracle.Concat(right, DspOracle.Zeros(total - Block)), 600, 0.5, 1);
        int end = TailEnd(l, r, Block, 600);
        int expectedFrames = ((end / Block) + 1) * Block;

        Assert.Equal(TimbrePlaybackState.Completed, result.State);
        Assert.False(result.TailTruncated);
        Assert.Equal(expectedFrames, rig.Output.SubmittedFrames);
        TimbreRig.AssertPcm(DspOracle.Interleave(l, r, 0.25, 0, expectedFrames), rig.Output.Read(0, expectedFrames), Tolerance);
        Assert.Equal(TimbrePlaybackState.Completed, playback.State);
    }

    [Fact]
    public async Task VolumeIsAppliedAfterTheChainSoAZeroVolumeTailLastsTheSame()
    {
        using TimbreRig rig = new(hold: false);
        TimbrePlayback playback = rig.Scope.Play(
            Clip(Block, Impulse, modifiers: [new Delay(time: 600f / 48000f, feedback: 0.5f, mix: 1f)]),
            start => start.Volume = 0f);

        await DrainAsync(rig, playback);

        Assert.Equal(26 * Block + Block, rig.Output.SubmittedFrames);
        Assert.All(rig.Output.ReadAll(), sample => Assert.Equal(0f, sample));
    }

    [Fact]
    public async Task DefaultTailCapTruncatesALongEchoAtThirtySeconds()
    {
        using TimbreRig rig = new(hold: false);
        TimbrePlayback playback = rig.Scope.Play(Clip(Block, Impulse, modifiers: [new Delay(time: 2f, feedback: 0.95f, mix: 1f)]));
        Assert.Equal(2L * 96000 * sizeof(float), rig.Runtime.GetDiagnostics().DspStateBytes);

        TimbrePlaybackResult result = await DrainAsync(rig, playback, maxBlocks: 4000);

        Assert.Equal(TimbrePlaybackState.Completed, result.State);
        Assert.True(result.TailTruncated);
        Assert.Equal(Block + (30L * 48000), rig.Output.SubmittedFrames);
        await TimbreRig.ReleasedAsync(playback);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().DspStateBytes);
    }

    [Fact]
    public async Task ConfiguredTailCapAndLowPassOnlyTail()
    {
        using TimbreRig capped = new(options => options.DelayTailCap = TimeSpan.FromMilliseconds(30), hold: false);
        TimbrePlaybackResult truncated = await DrainAsync(capped, capped.Scope.Play(Clip(Block, Impulse, modifiers: [new Delay(time: 0.01f, feedback: 0.9f, mix: 1f)])));
        Assert.True(truncated.TailTruncated);
        Assert.Equal(Block * 4, capped.Output.SubmittedFrames); // source block + three 10 ms tail blocks

        using TimbreRig filtered = new(hold: false);
        TimbrePlaybackResult natural = await DrainAsync(filtered, filtered.Scope.Play(Clip(Block, Impulse, modifiers: [new LowPass(cutoff: 8000f)])));
        Assert.False(natural.TailTruncated);
        Assert.InRange(filtered.Output.SubmittedFrames, Block * 2, Block * 3);
    }

    [Fact]
    public async Task CancelStopsTheTailAndPauseFreezesIt()
    {
        using TimbreRig rig = new();
        TimbreSound clip = Clip(Block, Impulse, modifiers: [new Delay(time: 600f / 48000f, feedback: 0.5f, mix: 1f)]);
        TimbrePlayback paused = rig.Scope.Play(clip);
        await rig.StartAsync(paused); // source block + three tail blocks

        paused.Pause();
        rig.Output.ConsumeAll();
        await rig.SyncAsync();
        Assert.Equal(Budget + Block, rig.Output.SubmittedFrames);

        paused.Resume();
        TimbrePlaybackResult result = await DrainAsync(rig, paused);
        int total = Block * 40;
        (double[] left, double[] right) = DspOracle.Channels(Impulse, Block);
        double[] l = DspOracle.Delay(DspOracle.Concat(left, DspOracle.Zeros(total - Block)), 600, 0.5, 1);
        double[] r = DspOracle.Delay(DspOracle.Concat(right, DspOracle.Zeros(total - Block)), 600, 0.5, 1);
        int frames = (int)rig.Output.SubmittedFrames;
        TimbreRig.AssertPcm(PausedSequence(DspOracle.Interleave(l, r), frames), rig.Output.Read(0, frames), Tolerance);
        Assert.Equal(TimbrePlaybackState.Completed, result.State);

        using TimbreRig cancelRig = new();
        TimbrePlayback canceled = cancelRig.Scope.Play(clip);
        await cancelRig.StartAsync(canceled);
        canceled.Cancel();
        cancelRig.Output.ConsumeAll();
        await cancelRig.SyncAsync();
        Assert.Equal(Budget + Block, cancelRig.Output.SubmittedFrames);
        TimbreRig.AssertPcm(TimbreRig.Ramp(DspOracle.Interleave(l, r, start: Budget, frames: Block), 1f, 0f),
            cancelRig.Output.Read(Budget, Block), Tolerance);
        await TimbreRig.ReleasedAsync(canceled);
        Assert.False((await TimbreRig.CompletionAsync(canceled)).TailTruncated);
    }

    [Fact]
    public async Task SuccessfulSeekResetsDspStateWhileARejectedSeekDoesNot()
    {
        using TimbreRig rig = new();
        TimbreSound clip = Clip(48000, Impulse, modifiers: [new Delay(time: 500f / 48000f, feedback: 0.5f, mix: 0.5f)]);
        TimbrePlayback playback = rig.Scope.Play(clip);
        await rig.StartAsync(playback);

        Task seek = playback.SeekAsync(TimeSpan.Zero);
        await rig.NextBlockAsync();
        await HarnessWait.WithTimeout(seek, null, "Seek did not complete.");
        float[] afterSeek = await rig.NextBlockAsync();

        (double[] left, double[] right) = DspOracle.Channels(Impulse, Block);
        TimbreRig.AssertPcm(TimbreRig.Ramp(DspOracle.Interleave(DspOracle.Delay(left, 500, 0.5, 0.5), DspOracle.Delay(right, 500, 0.5, 0.5))), afterSeek, Tolerance);

        using TimbreRig streamingRig = new();
        DeterministicTimbreSourceFactory unknown = new(48000, Impulse, reportLength: false, maxFramesPerRead: 512);
        TimbrePlayback streaming = streamingRig.Scope.Play(new TimbreSound(
            TimbreSource.FromReader(unknown.Open),
            loading: TimbreLoading.Streaming,
            modifiers: [new Delay(time: 500f / 48000f, feedback: 0.5f, mix: 0.5f)]));
        await streamingRig.StartAsync(streaming);
        Task rejected = streaming.SeekAsync(TimeSpan.FromSeconds(9));
        await streamingRig.NextBlockAsync();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => HarnessWait.WithTimeout(rejected, null, "Seek did not finish."));
        float[] continued = await streamingRig.NextBlockAsync();

        (double[] longLeft, double[] longRight) = DspOracle.Channels(Impulse, Budget + TimbreRig.FadeFrames + Block);
        float[] continuous = DspOracle.Interleave(DspOracle.Delay(longLeft, 500, 0.5, 0.5), DspOracle.Delay(longRight, 500, 0.5, 0.5));
        TimbreRig.AssertPcm(TimbreRig.Ramp(DspOracle.Interleave(continuous, Budget + TimbreRig.FadeFrames, Block)), continued, Tolerance);
    }

    [Fact]
    public async Task NaturalLoopAndPauseResumePreserveDspStateAcrossBoundaries()
    {
        using TimbreRig rig = new();
        Func<long, int, float> late = (frame, channel) => frame == 900 ? (channel == 0 ? 1f : -1f) : 0f;
        TimbrePlayback playback = rig.Scope.Play(
            Clip(1000, late, loop: true, modifiers: [new Delay(time: 300f / 48000f, feedback: 0.5f, mix: 0.5f)]));
        float[] first = await rig.StartAsync(playback);
        playback.Pause();
        rig.Output.ConsumeAll();
        await rig.SyncAsync();
        playback.Resume();
        await rig.Output.WaitForSubmittedFramesAsync(Budget * 2);

        int frames = Budget * 2;
        Func<long, int, float> periodic = (frame, channel) => late(frame % 1000, channel);
        (double[] left, double[] right) = DspOracle.Channels(periodic, frames);
        float[] expected = DspOracle.Interleave(DspOracle.Delay(left, 300, 0.5, 0.5), DspOracle.Delay(right, 300, 0.5, 0.5));
        TimbreRig.AssertPcm(PausedSequence(expected, frames), rig.Output.Read(0, frames), Tolerance);
        Assert.Equal(0.5f, first[1200 * 2], 5); // Mix · w[900]: the echo lands after the loop boundary
        Assert.False(playback.Completion.IsCompleted);
    }

    [Fact]
    public async Task ReaderReturningMoreFramesThanRequestedFailsInsteadOfReachingTheMix()
    {
        using TimbreRig rig = new(hold: false);
        TimbrePlaybackResult result = await TimbreRig.CompletionAsync(rig.Scope.Play(
            new TimbreSound(TimbreSource.FromReader(() => new OverReportingReader()), loading: TimbreLoading.Streaming, modifiers: [new LowPass()])));

        Assert.Equal(TimbreErrorKind.InvalidData, result.Error!.Kind);
        Assert.Equal(0, rig.Output.SubmittedFrames);
    }

    private static TimbreSound Clip(
        long frames,
        Func<long, int, float> signal,
        bool loop = false,
        IEnumerable<TimbreParameter>? parameters = null,
        IEnumerable<TimbreModifier>? modifiers = null) =>
        new(TimbreSource.FromReader(new DeterministicTimbreSourceFactory(frames, signal).Open), loop: loop, loading: TimbreLoading.Preload, parameters: parameters, modifiers: modifiers);

    // The oracle advances DSP for 240 fade frames, freezes for the remainder
    // of the output block, then continues from that exact preserved state.
    private static float[] PausedSequence(float[] continuous, int outputFrames) =>
        continuous[..(Budget * 2)]
            .Concat(TimbreRig.Ramp(DspOracle.Interleave(continuous, Budget, Block), 1f, 0f))
            .Concat(TimbreRig.Ramp(DspOracle.Interleave(continuous, Budget + TimbreRig.FadeFrames, outputFrames - Budget - Block)))
            .ToArray();

    private static async Task<TimbrePlaybackResult> DrainAsync(TimbreRig rig, TimbrePlayback playback, int maxBlocks = 400)
    {
        await rig.ReadyAsync(playback);
        rig.Output.Release();
        for (int iteration = 0; iteration < maxBlocks && !playback.Completion.IsCompleted; iteration++)
        {
            await rig.SyncAsync();
            rig.Output.ConsumeAll();
        }

        return await TimbreRig.CompletionAsync(playback);
    }

    private static int TailEnd(double[] left, double[] right, int sourceFrames, int window)
    {
        int silent = 0;
        for (int frame = sourceFrames; frame < left.Length; frame++)
        {
            silent = Math.Max(Math.Abs(left[frame]), Math.Abs(right[frame])) < 1e-6 ? silent + 1 : 0;
            if (silent >= window)
            {
                return frame;
            }
        }

        throw new InvalidOperationException("The oracle tail did not end inside the analyzed range.");
    }

    private sealed class OverReportingReader : TimbreReader
    {
        public override long? LengthFrames => null;

        public override ValueTask<TimbreReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new TimbreReadResult((destination.Length / 2) + 1, endOfSource: false));

        public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }
}
