using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Decoding;

// Cancel at every point of a decoded stream's life ends the playback at
// once; its reader, stream and reservations are released once the blocked
// I/O returns, and nothing of it reaches another playback.
public sealed class TimbreStreamingCancelTests
{
    private const string File = "vorbis-44100-stereo-q4.ogg";

    [Fact]
    public async Task CancelWhileTheSourceIsOpening()
    {
        ObservedSource source = new(DecodingCorpus.PathOf(File)) { Configure = stream => stream.BlockAt(0) };
        using TimbreRig rig = new(hold: false);
        TimbrePlayback playback = rig.Scope.Play(source.Clip);
        await HarnessWait.WithTimeout(source.Opened, null, "The source was not opened.");
        await HarnessWait.WithTimeout(source.Single.WhenBlocked, null, "Opening did not reach the barrier.");

        playback.Cancel();
        Assert.Equal(TimbrePlaybackState.Canceled, (await TimbreRig.CompletionAsync(playback)).State);

        source.Single.Release();
        await AssertReleasedAsync(rig, playback, source.Single);
    }

    [Fact]
    public async Task CancelWhileTheDecoderReads()
    {
        ObservedSource source = new(DecodingCorpus.PathOf(File));
        using TimbreRig rig = new();
        TimbrePlayback playback = rig.Scope.Play(source.Clip);
        await rig.StartAsync(playback);
        await StreamingDrive.UntilBlockedAsync(rig, playback, source.Single);

        playback.Cancel();
        Assert.Equal(TimbrePlaybackState.Canceled, (await TimbreRig.CompletionAsync(playback)).State);

        source.Single.Release();
        await AssertReleasedAsync(rig, playback, source.Single);
    }

    [Fact]
    public async Task CancelWhileThePumpWaitsForSpace()
    {
        ObservedSource source = new(DecodingCorpus.PathOf(File));
        using TimbreRig rig = new();
        TimbrePlayback playback = rig.Scope.Play(source.Clip);
        await rig.StartAsync(playback);
        await rig.SettledAsync(playback); // ring full, sink queue full

        playback.Cancel();

        await AssertReleasedAsync(rig, playback, source.Single);
    }

    [Fact]
    public async Task CancelAfterTheEndOfSourceWasRead()
    {
        ObservedSource source = new(WavFixture.Write("cancel-eof.wav", 48000, 2, 16, seconds: 0.1));
        using TimbreRig rig = new();
        TimbrePlayback playback = rig.Scope.Play(source.Clip);
        await rig.StartAsync(playback);
        await rig.SettledAsync(playback); // the whole 4800 frames are in the ring

        playback.Cancel();

        Assert.Equal(TimbrePlaybackState.Canceled, (await TimbreRig.CompletionAsync(playback)).State);
        await AssertReleasedAsync(rig, playback, source.Single);
    }

    [Fact]
    public async Task CancelDuringALoopRewind()
    {
        ObservedSource source = new(DecodingCorpus.PathOf("opus-stereo-preskip-trim.opus"), loop: true);
        using TimbreRig rig = new();
        TimbrePlayback playback = rig.Scope.Play(source.Clip);
        await rig.StartAsync(playback);
        ObservedFileStream stream = source.Single;

        // Block the reads of the first 4 KiB that follow a complete first pass:
        // the rewind to the first audio page.
        stream.BlockAt(0, 4096, afterBytes: stream.Length);
        for (int step = 0; step < 400 && !stream.WhenBlocked.IsCompleted; step++)
        {
            rig.Output.ConsumeAll();
            await rig.SyncAsync();
            await HarnessWait.WithTimeout(Task.WhenAny(stream.WhenBlocked, playback.WhenSettledAsync()), null, "The pump neither settled nor blocked.");
        }

        await HarnessWait.WithTimeout(stream.WhenBlocked, null, "The rewind did not reach the barrier.");
        playback.Cancel();
        Assert.Equal(TimbrePlaybackState.Canceled, (await TimbreRig.CompletionAsync(playback)).State);

        stream.Release();
        await AssertReleasedAsync(rig, playback, stream);
    }

    [Fact]
    public async Task TwoPlaybacksOfTheSameFileHaveIndependentReaders()
    {
        ObservedSource source = new(DecodingCorpus.PathOf(File));
        float[] expected = source.Decode();
        using TimbreRig rig = new();
        TimbrePlayback first = rig.Scope.Play(source.Clip);
        TimbrePlayback second = rig.Scope.Play(source.Clip, start => start.Volume = 0.5f);
        await rig.StartAsync(first, second);
        Assert.Equal(2, source.Streams.Count);

        first.Cancel();
        rig.Output.Consume(TimbreRig.Block); // capacity for the release fade
        await TimbreRig.ReleasedAsync(first);
        await rig.SettledAsync(second);
        await rig.SyncAsync();
        long start = rig.Output.SubmittedFrames;
        rig.Output.ConsumeAll();
        await rig.Output.WaitForSubmittedFramesAsync(start + TimbreRig.Block);
        await rig.SyncAsync();

        float[] block = rig.Output.Read(start, TimbreRig.Block);
        long offset = (long)Math.Round(second.Position.TotalSeconds * TimbreRuntime.SampleRate) - (rig.Output.SubmittedFrames - start);
        float[] reference = expected[(int)(offset * 2)..(int)((offset + TimbreRig.Block) * 2)].Select(sample => sample * 0.5f).ToArray();
        TimbreRig.AssertPcm(reference, block, tolerance: 1e-7f);
        Assert.Equal(1, source.Streams.Count(stream => stream.IsDisposed));
        Assert.Equal(TimbrePlaybackState.Playing, second.State);
    }

    private static async Task AssertReleasedAsync(TimbreRig rig, TimbrePlayback playback, ObservedFileStream stream)
    {
        rig.Output.Consume(TimbreRig.Block); // terminal state precedes rendering the release fade
        await TimbreRig.ReleasedAsync(playback);
        Assert.True(stream.IsDisposed);
        TimbreRuntimeDiagnostics diagnostics = rig.Runtime.GetDiagnostics();
        Assert.Equal(0, diagnostics.LiveReaders);
        Assert.Equal(0, diagnostics.LiveSourcePumps);
        Assert.Equal(0, diagnostics.ReaderMemoryBytes);
        Assert.Equal(0, diagnostics.StreamingBufferBytes);
    }
}
