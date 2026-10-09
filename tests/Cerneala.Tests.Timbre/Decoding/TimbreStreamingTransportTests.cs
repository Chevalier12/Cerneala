using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Decoding;

// Pause/Resume/SeekAsync and seek supersession on streaming decoded files:
// the first PCM after a seek is the decoded PCM at the exact target frame
// (Opus restarts its decoder with an 80 ms pre-roll, so it is compared with a
// tolerance and against the signal oracle).
public sealed class TimbreStreamingTransportTests
{
    public static IEnumerable<object[]> Files()
    {
        yield return ["mp3-mpeg1-44100-stereo-cbr128.mp3"];
        yield return ["mp3-mpeg1-32000-stereo-vbr.mp3"];
        yield return ["vorbis-44100-stereo-q4.ogg"];
        yield return ["opus-stereo-preskip-trim.opus"];
        yield return [WavFixture.Write("transport.wav", 44100, 2, 24, seconds: 2)];
    }

    [Theory]
    [MemberData(nameof(Files))]
    public async Task SeekLandsOnTheExactDecodedFrameAtStartMiddleAndNearTheEnd(string name)
    {
        ObservedSource source = new(PathOf(name));
        float[] expected = source.Decode();
        long length = expected.Length / 2;
        using TimbreRig rig = new();
        TimbrePlayback playback = rig.Scope.Play(source.Clip);
        await rig.StartAsync(playback);
        Assert.Equal(TimbreRig.FramesToTime(length), playback.Duration);

        foreach (long target in new[] { length / 2, 0, length - 3000, 4801 })
        {
            float[] block = await SeekAndReadAsync(rig, playback, target);

            AssertBlockAt(name, expected, target, block);
            Assert.Equal(TimbrePlaybackState.Playing, playback.State);
        }

        Assert.Single(source.Streams);
    }

    [Theory]
    [MemberData(nameof(Files))]
    public async Task SeekWhilePausedStaysPausedAndResumesAtTheTarget(string name)
    {
        ObservedSource source = new(PathOf(name));
        float[] expected = source.Decode();
        long target = (expected.Length / 2) / 3;
        using TimbreRig rig = new();
        TimbrePlayback playback = rig.Scope.Play(source.Clip);
        await rig.StartAsync(playback);

        playback.Pause();
        await rig.NextBlockAsync(); // render the 5 ms release before freezing
        await HarnessWait.WithTimeout(playback.SeekAsync(TimbreRig.FramesToTime(target)), null, "Seek did not complete.");
        await rig.SyncAsync();
        Assert.Equal(TimbrePlaybackState.Paused, playback.State);
        Assert.Equal(TimbreRig.FramesToTime(target), playback.Position);

        playback.Resume();
        float[] block = await DrainToNewBlockAsync(rig);

        AssertBlockAt(name, expected, target, block);
        Assert.Single(source.Streams);
    }

    [Theory]
    [MemberData(nameof(Files))]
    public async Task PauseAndResumeKeepTheReaderAndContinueWithoutRestart(string name)
    {
        ObservedSource source = new(PathOf(name));
        float[] expected = source.Decode();
        using TimbreRig rig = new();
        TimbrePlayback playback = rig.Scope.Play(source.Clip);
        await rig.StartAsync(playback);
        await StreamingDrive.NextBlockAsync(rig, playback);

        // After its release ramp, a paused playback produces nothing.
        playback.Pause();
        await rig.NextBlockAsync();
        TimeSpan paused = playback.Position;
        rig.Output.ConsumeAll();
        await rig.SyncAsync();
        long submitted = rig.Output.SubmittedFrames;
        Assert.Equal(paused, playback.Position);

        playback.Resume();
        await rig.Output.WaitForSubmittedFramesAsync(submitted + TimbreRig.Block);
        float[] resumed = rig.Output.Read(submitted, TimbreRig.Block);

        long frame = TimbreTimeFrames(paused);
        TimbreRig.AssertPcm(TimbreRig.Ramp(expected[(int)(frame * 2)..(int)((frame + TimbreRig.Block) * 2)]), resumed, tolerance: 0);
        Assert.Single(source.Streams);
        Assert.False(source.Single.IsDisposed);
    }

    [Fact]
    public async Task PauseWhilePendingHoldsTheFirstPcmUntilResume()
    {
        ObservedSource source = new(DecodingCorpus.PathOf("vorbis-44100-stereo-q4.ogg"));
        float[] expected = source.Decode();
        using TimbreRig rig = new(hold: false);
        TimbrePlayback playback = rig.Scope.Play(source.Clip);
        playback.Pause();
        await rig.ReadyAsync(playback);
        await rig.SettledAsync(playback);
        await rig.SyncAsync();

        Assert.Equal(TimeSpan.Zero, playback.Position);
        Assert.All(rig.Output.ReadAll(), sample => Assert.Equal(0f, sample));
        long submitted = rig.Output.SubmittedFrames;
        playback.Resume();
        await rig.SyncAsync();
        rig.Output.ConsumeAll();
        await rig.Output.WaitForSubmittedFramesAsync(submitted + TimbreRig.Block);

        float[] first = rig.Output.Read(submitted, TimbreRig.Block);
        TimbreRig.AssertPcm(TimbreRig.Ramp(expected[..first.Length]), first, tolerance: 0);
    }

    [Fact]
    public async Task SeekOutsideTheKnownDurationIsRejectedSynchronously()
    {
        ObservedSource source = new(DecodingCorpus.PathOf("mp3-mpeg1-44100-stereo-cbr128.mp3"));
        using TimbreRig rig = new();
        TimbrePlayback playback = rig.Scope.Play(source.Clip);
        await rig.StartAsync(playback);

        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = playback.SeekAsync(TimeSpan.FromTicks(-1)); });
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = playback.SeekAsync(TimeSpan.FromSeconds(2.001)); });
        Assert.Equal(TimbrePlaybackState.Playing, playback.State);
    }

    [Fact]
    public async Task UnknownDurationIsLearnedAtTheEndAndASeekBeyondItLeavesThePositionUnchanged()
    {
        // No Info tag: the length is unknown until the end has been decoded.
        ObservedSource source = new(DecodingCorpus.PathOf("mp3-no-info-tag-44100-stereo.mp3"), loop: true);
        float[] expected = source.Decode();
        long length = expected.Length / 2;
        using TimbreRig rig = new();
        TimbrePlayback playback = rig.Scope.Play(source.Clip);
        await rig.StartAsync(playback);
        Assert.Null(playback.Duration);

        Task beyond = playback.SeekAsync(TimeSpan.FromSeconds(10));
        await rig.NextBlockAsync();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => HarnessWait.WithTimeout(beyond, null, "Seek did not finish."));
        float[] next = await StreamingDrive.NextBlockAsync(rig, playback);
        int continuedAt = TimbreRig.Budget + TimbreRig.FadeFrames;
        TimbreRig.AssertPcm(TimbreRig.Ramp(expected[(continuedAt * 2)..((continuedAt + TimbreRig.Block) * 2)]), next, tolerance: 0);

        // Play through the end once: the wrap teaches the playback its duration.
        long blocks = (length / TimbreRig.Block) + 2;
        for (long block = 0; block < blocks; block++)
        {
            await StreamingDrive.NextBlockAsync(rig, playback);
        }

        Assert.True(rig.Runtime.GetDiagnostics().LoopWraps >= 1);
        Assert.Equal(TimbreRig.FramesToTime(length), playback.Duration);
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = playback.SeekAsync(TimbreRig.FramesToTime(length + 1)); });
    }

    [Theory]
    [InlineData(48000)]
    [InlineData(44100)]
    public async Task AFailedSeekFadesBackInAndContinuesTheSourceWithoutLosingFrames(int rate)
    {
        ObservedSource source = new(WavFixture.Write($"seek-failure-{rate}.wav", rate, 2, 24, seconds: 2));
        float[] expected = source.Decode();
        using TimbreRig rig = new();
        TimbrePlayback playback = rig.Scope.Play(source.Clip);
        await rig.StartAsync(playback);

        source.Single.FailSeeks = true;
        Task failed = playback.SeekAsync(TimbreRig.FramesToTime(30000));
        await rig.NextBlockAsync();
        await Assert.ThrowsAsync<IOException>(() => HarnessWait.WithTimeout(failed, null, "Seek did not finish."));
        source.Single.FailSeeks = false;
        Assert.Equal(TimbrePlaybackState.Playing, playback.State);

        // The rejected seek fades back in from its post-fade position. Past the
        // 8192 frames buffered before the seek, every frame is still the next
        // decoded frame of the same stream.
        int continuedAt = TimbreRig.Budget + TimbreRig.FadeFrames;
        const int Blocks = 30;
        List<float> played = [];
        for (int block = 0; block < Blocks; block++)
        {
            played.AddRange(await StreamingDrive.NextBlockAsync(rig, playback));
        }

        float[] reference = TimbreRig.Ramp(expected[(continuedAt * 2)..((continuedAt + (Blocks * TimbreRig.Block)) * 2)]);
        TimbreRig.AssertPcm(reference, played.ToArray(), tolerance: 0);
    }

    [Fact]
    public async Task LatestSeekWinsWhileTheReaderIsBlockedAndTheSupersededRequestIsCanceled()
    {
        ObservedSource source = new(DecodingCorpus.PathOf("vorbis-44100-stereo-q4.ogg"));
        float[] expected = source.Decode();
        using TimbreRig rig = new();
        TimbrePlayback playback = rig.Scope.Play(source.Clip);
        await rig.StartAsync(playback);
        ObservedFileStream stream = source.Single;
        await StreamingDrive.UntilBlockedAsync(rig, playback, stream);

        Task first = playback.SeekAsync(TimbreRig.FramesToTime(10000));
        Task second = playback.SeekAsync(TimbreRig.FramesToTime(60000));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => HarnessWait.WithTimeout(first, null, "Superseded seek did not finish."));
        Assert.False(second.IsCompleted);

        stream.Release();
        rig.Output.Consume(TimbreRig.Block);
        await rig.SyncAsync();
        await HarnessWait.WithTimeout(second, null, "Latest seek did not complete.");
        (float[] block, long frame) = await NextProducedBlockAsync(rig, playback);

        // The mixer may already have mixed whole blocks from the target.
        Assert.InRange(frame, 60000, 60000 + TimbreRig.Budget);
        Assert.Equal(0, (frame - 60000) % TimbreRig.Block);
        float[] reference = expected[(int)(frame * 2)..(int)((frame + TimbreRig.Block) * 2)];
        TimbreRig.AssertPcm(frame == 60000 ? TimbreRig.Ramp(reference) : reference, block, tolerance: 0);
        Assert.Equal(1, rig.Runtime.GetDiagnostics().SeeksSuperseded);
    }

    [Fact]
    public async Task CancelAndReplacementWithdrawAPendingSeek()
    {
        ObservedSource first = new(DecodingCorpus.PathOf("mp3-mpeg1-44100-stereo-cbr128.mp3"));
        ObservedSource second = new(DecodingCorpus.PathOf("opus-stereo-48000-96k.opus"));
        float[] secondPcm = second.Decode();
        using TimbreRig rig = new();
        TimbreHandle slot = rig.Scope.CreateHandle();
        TimbrePlayback replaced = rig.Scope.Play(first.Clip, handle: slot);
        await rig.StartAsync(replaced);
        await StreamingDrive.UntilBlockedAsync(rig, replaced, first.Single);

        Task seek = replaced.SeekAsync(TimbreRig.FramesToTime(30000));
        TimbrePlayback replacement = rig.Scope.Play(second.Clip, handle: slot);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => HarnessWait.WithTimeout(seek, null, "Seek did not finish."));
        Assert.Equal(TimbrePlaybackState.Canceled, (await TimbreRig.CompletionAsync(replaced)).State);
        first.Single.Release();
        rig.Output.Consume(TimbreRig.Block);
        await TimbreRig.ReleasedAsync(replaced);
        Assert.True(first.Single.IsDisposed);

        // Once the old queue drains, only the replacement plays, from its start.
        await rig.ReadyAsync(replacement);
        await rig.SettledAsync(replacement);
        await rig.SyncAsync();
        long start = rig.Output.SubmittedFrames;
        rig.Output.ConsumeAll();
        await rig.Output.WaitForSubmittedFramesAsync(start + TimbreRig.Block);
        await rig.SyncAsync();
        float[] block = rig.Output.Read(start, TimbreRig.Block);
        // The mixer may have produced more blocks: the first one starts that many frames earlier.
        int offset = (int)(TimbreTimeFrames(replacement.Position) - (rig.Output.SubmittedFrames - start));
        float[] replacementPcm = secondPcm[(offset * 2)..((offset + TimbreRig.Block) * 2)];
        TimbreRig.AssertPcm(offset == 0 ? TimbreRig.Ramp(replacementPcm) : replacementPcm, block, tolerance: 0);
    }

    // Seeks and returns the first block mixed from the target.
    private static async Task<float[]> SeekAndReadAsync(TimbreRig rig, TimbrePlayback playback, long target)
    {
        Task seek = playback.SeekAsync(TimbreRig.FramesToTime(target));
        await rig.NextBlockAsync();
        await HarnessWait.WithTimeout(seek, null, "Seek did not complete.");
        await rig.SettledAsync(playback);
        Assert.Equal(TimbreRig.FramesToTime(target), playback.Position);
        return await DrainToNewBlockAsync(rig);
    }

    // Consumes the whole queue and returns the next block produced after it.
    // The mixer is left idle with a full queue, so a following transport
    // command takes effect before any further block is mixed.
    private static async Task<float[]> DrainToNewBlockAsync(TimbreRig rig)
    {
        await rig.SyncAsync();
        long submitted = rig.Output.SubmittedFrames;
        rig.Output.ConsumeAll();
        await rig.Output.WaitForSubmittedFramesAsync(submitted + TimbreRig.Block);
        await rig.SyncAsync();
        return rig.Output.Read(submitted, TimbreRig.Block);
    }

    // Once the pump has filled the ring, returns the next mixed block and the
    // source frame it starts at.
    private static async Task<(float[] Block, long Frame)> NextProducedBlockAsync(TimbreRig rig, TimbrePlayback playback)
    {
        await rig.SettledAsync(playback);
        await rig.SyncAsync();
        long submitted = rig.Output.SubmittedFrames;
        long frame = TimbreTimeFrames(playback.Position);
        rig.Output.ConsumeAll();
        await rig.Output.WaitForSubmittedFramesAsync(submitted + TimbreRig.Block);
        await rig.SyncAsync();
        return (rig.Output.Read(submitted, TimbreRig.Block), frame);
    }

    private static void AssertBlockAt(string name, float[] expected, long target, float[] block)
    {
        long frames = Math.Min(TimbreRig.Block, (expected.Length / 2) - target);
        float[] reference = TimbreRig.Ramp(expected[(int)(target * 2)..(int)((target + frames) * 2)]);
        if (name.EndsWith(".opus", StringComparison.Ordinal))
        {
            // Restarted Opus decoder after an 80 ms pre-roll: close to, not
            // identical with, the continuous decode, and on the signal.
            TimbreRig.AssertPcm(reference, block[..reference.Length], tolerance: 0.05f);
            CorpusExpectation file = DecodingCorpus.Describe(name)!;
            // The first 5 ms deliberately changes amplitude; evaluate decoder SNR after it.
            Assert.True(PcmOracle.SnrDb(file, block[..reference.Length], 0, target + TimbreRig.FadeFrames, target + frames, pcmOrigin: target) >= 15);
        }
        else
        {
            TimbreRig.AssertPcm(reference, block[..reference.Length], tolerance: 0);
        }
    }

    private static long TimbreTimeFrames(TimeSpan time) => (long)Math.Round(time.TotalSeconds * TimbreRuntime.SampleRate);

    private static string PathOf(string name) => Path.IsPathRooted(name) ? name : DecodingCorpus.PathOf(name);
}
