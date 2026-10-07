using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Decoding;

// A temporary underrun pads counted silence and resumes where the source
// stopped; I/O errors and damaged data fail the playback instead of ending it.
public sealed class SoundStreamingFailureTests
{
    [Fact]
    public async Task UnderrunPadsCountedSilenceAndResumesWithoutSkippingSource()
    {
        ObservedSource source = new(WavFixture.Write("underrun.wav", 44100, 2, 16, seconds: 2));
        float[] expected = source.Decode();
        using TimbreRig rig = new();
        SoundPlayback playback = rig.Scope.Play(source.Clip);
        await rig.StartAsync(playback);
        ObservedFileStream stream = source.Single;
        stream.BlockAt(0);

        // A starved source defers mixing until the queue would run dry, so the
        // device side drains block by block until the padding appears.
        for (int step = 0; step < 400 && rig.Runtime.GetDiagnostics().UnderrunFrames == 0; step++)
        {
            await rig.SyncAsync();
            rig.Output.Consume(TimbreRig.Block);
            await rig.SyncAsync();
        }

        Assert.True(rig.Runtime.GetDiagnostics().UnderrunFrames > 0, "The blocked source never starved the mixer.");
        TimeSpan starved = playback.Position;
        await rig.NextBlockAsync();
        Assert.Equal(starved, playback.Position); // padding does not advance the source

        // Data is mixed into free queue space as soon as the source resumes.
        await rig.SyncAsync();
        long start = rig.Output.SubmittedFrames;
        stream.Release();
        await rig.SettledAsync(playback);
        await rig.SyncAsync();
        rig.Output.ConsumeAll();
        await rig.Output.WaitForSubmittedFramesAsync(start + TimbreRig.Block);
        await rig.SyncAsync();
        float[] block = rig.Output.Read(start, TimbreRig.Block);
        long frame = (long)Math.Round(playback.Position.TotalSeconds * SoundRuntime.SampleRate) - (rig.Output.SubmittedFrames - start);

        Assert.Equal((long)Math.Round(starved.TotalSeconds * SoundRuntime.SampleRate), frame);
        TimbreRig.AssertPcm(expected[(int)(frame * 2)..(int)((frame + TimbreRig.Block) * 2)], block, tolerance: 0);
        Assert.Equal(SoundPlaybackState.Playing, playback.State);
    }

    [Fact]
    public async Task ReadErrorMidStreamFailsThePlaybackAsSourceUnavailable()
    {
        ObservedSource source = new(DecodingCorpus.PathOf("mp3-mpeg1-44100-stereo-cbr128.mp3"));
        source.Configure = stream => stream.FailAtOffset = stream.Length / 3;

        SoundPlaybackResult result = await RunToCompletionAsync(source);

        Assert.Equal(SoundPlaybackState.Failed, result.State);
        Assert.Equal(SoundErrorKind.SourceUnavailable, result.Error!.Kind);
        Assert.IsType<IOException>(result.Error.InnerException);
        Assert.True(source.Single.IsDisposed);
    }

    [Theory]
    [InlineData("vorbis-corrupt-crc.ogg")]
    [InlineData("mp3-truncated.mp3")]
    [InlineData("mp3-corrupt-sync.mp3")]
    public async Task DamagedDataFailsTheStreamingPlaybackAsInvalidData(string name)
    {
        ObservedSource source = new(DecodingCorpus.PathOf(name));

        SoundPlaybackResult result = await RunToCompletionAsync(source);

        Assert.Equal(SoundPlaybackState.Failed, result.State);
        Assert.Equal(SoundErrorKind.InvalidData, result.Error!.Kind);
    }

    [Fact]
    public async Task Mp3CutAtAFrameBoundaryBeforeItsDeclaredLengthFailsInsteadOfEnding()
    {
        // The Info tag declares every frame; the copy ends cleanly after 40.
        byte[] data = File.ReadAllBytes(DecodingCorpus.PathOf("mp3-mpeg1-44100-stereo-cbr128.mp3"));
        int offset = 0;
        for (int frame = 0; frame <= 40; frame++)
        {
            offset += (144 * 128000 / 44100) + ((data[offset + 2] >> 1) & 1);
        }

        string path = Path.Combine(Path.GetTempPath(), "cerneala-timbre-cut", "mp3-cut-at-frame.mp3");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, data[..offset]);

        SoundPlaybackResult result = await RunToCompletionAsync(new ObservedSource(path));

        Assert.Equal(SoundPlaybackState.Failed, result.State);
        Assert.Equal(SoundErrorKind.InvalidData, result.Error!.Kind);
    }

    [Theory]
    [InlineData(SoundLoading.Streaming)]
    [InlineData(SoundLoading.Preload)]
    public async Task ReaderEndingBeforeItsDeclaredLengthFailsInEveryLoadingMode(SoundLoading loading)
    {
        using TimbreRig rig = new();
        SoundPlayback playback = rig.Scope.Play(new SoundClip(SoundSource.FromReader(() => new EndsEarlyReader(), "ends-early"), loading: loading));
        rig.Output.Release();
        for (int step = 0; step < 2000 && !playback.Completion.IsCompleted; step++)
        {
            rig.Output.ConsumeAll();
            await rig.SyncAsync();
        }

        SoundPlaybackResult result = await TimbreRig.CompletionAsync(playback);

        Assert.Equal(SoundPlaybackState.Failed, result.State);
        Assert.Equal(SoundErrorKind.InvalidData, result.Error!.Kind);
    }

    // Consumes output until the playback ends; every step waits for the mixer.
    private static async Task<SoundPlaybackResult> RunToCompletionAsync(ObservedSource source)
    {
        using TimbreRig rig = new();
        SoundPlayback playback = rig.Scope.Play(source.Clip);
        rig.Output.Release();
        for (int step = 0; step < 2000 && !playback.Completion.IsCompleted; step++)
        {
            rig.Output.ConsumeAll();
            await rig.SyncAsync();
        }

        SoundPlaybackResult result = await TimbreRig.CompletionAsync(playback);
        await TimbreRig.ReleasedAsync(playback);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().LiveReaders);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().ReaderMemoryBytes);
        return result;
    }

    // Declares 48 000 frames and ends after 4 800.
    private sealed class EndsEarlyReader : SoundReader
    {
        private long position;

        public override long? LengthFrames => 48000;

        public override ValueTask<SoundReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken)
        {
            int frames = (int)Math.Min(destination.Length / 2, 4800 - position);
            destination.Span[..(frames * 2)].Fill(0.1f);
            position += frames;
            return ValueTask.FromResult(new SoundReadResult(frames, position >= 4800));
        }

        public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken)
        {
            position = frame;
            return ValueTask.CompletedTask;
        }
    }
}
