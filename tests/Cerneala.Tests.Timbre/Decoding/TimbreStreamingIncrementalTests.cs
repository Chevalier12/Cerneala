using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Decoding;

// Streaming decodes on the source pump, produces its first PCM long before
// the source has been read, and keeps the same buffers and reservations for a
// 2 s and a 300 s file of the same codec.
public sealed class TimbreStreamingIncrementalTests
{
    public static IEnumerable<object[]> ShortAndLong()
    {
        yield return ["mp3-mpeg2-22050-mono-vbr.mp3", "mp3-long-22050-mono-cbr64.mp3"];
        yield return ["vorbis-22050-mono-q2.ogg", "vorbis-long-22050-mono-q2.ogg"];
        yield return ["opus-mono-16000-24k.opus", "opus-long-mono-32k.opus"];
        yield return [WavFixture.Write("stream-short.wav", 22050, 1, 16, seconds: 2), WavFixture.Write("stream-long.wav", 22050, 1, 16, seconds: 300)];
    }

    [Theory]
    [MemberData(nameof(ShortAndLong))]
    public async Task FirstPcmPrecedesReadingTheSourceAndBuffersDoNotGrowWithDuration(string shortName, string longName)
    {
        (long shortRing, long shortReader, _) = await PlayFirstQueueAsync(PathOf(shortName), barrierFraction: null);
        (long longRing, long longReader, long longRead) = await PlayFirstQueueAsync(PathOf(longName), barrierFraction: 0.05);

        // Same ring and same decoder reservation regardless of the duration.
        Assert.Equal(shortRing, longRing);
        Assert.Equal(shortReader, longReader);
        Assert.True(longRead < new FileInfo(PathOf(longName)).Length / 20, $"{longRead} bytes were read before the first PCM.");
    }

    [Fact]
    public async Task SourceReadsRunOnTheSourcePumpNotOnTheMixerOrTheCaller()
    {
        ObservedSource source = new(DecodingCorpus.PathOf("vorbis-44100-stereo-q4.ogg"));
        using TimbreRig rig = new();
        int caller = Environment.CurrentManagedThreadId;

        TimbrePlayback playback = rig.Scope.Play(source.Clip);
        await rig.StartAsync(playback);
        await rig.NextBlockAsync();

        ObservedFileStream stream = source.Single;
        lock (stream.ReaderThreads)
        {
            Assert.DoesNotContain("Timbre mixer", stream.ReaderThreads);
            Assert.DoesNotContain(caller, stream.ReaderThreadIds);
        }
    }

    // Plays a streaming file until the first full software queue; with a
    // barrier, reads past that fraction of the file block during the run.
    private static async Task<(long Ring, long Reader, long BytesAtFirstPcm)> PlayFirstQueueAsync(string path, double? barrierFraction)
    {
        ObservedSource source = new(path);
        if (barrierFraction is double fraction)
        {
            // The bounded tail scan at open (Ogg final page, MP3 trailing tags) stays allowed.
            source.Configure = stream => stream.BlockAt((long)(stream.Length * fraction), stream.Length - (64 * 1024));
        }

        float[] expected = source.Decode();
        using TimbreRig rig = new();
        TimbrePlayback playback = rig.Scope.Play(source.Clip);

        float[] first = await rig.StartAsync(playback);

        TimbreRig.AssertPcm(expected[..first.Length], first, tolerance: 0);
        ObservedFileStream stream = source.Single;
        TimbreRuntimeDiagnostics diagnostics = rig.Runtime.GetDiagnostics();
        long read = stream.BytesRead;
        Assert.False(stream.WhenBlocked.IsCompleted, "A read reached the barrier before the first PCM.");

        playback.Cancel();
        stream.Release();
        rig.Output.Consume(TimbreRig.Block); // make room for the 5 ms release fade
        await TimbreRig.ReleasedAsync(playback);
        Assert.True(stream.IsDisposed);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().ReaderMemoryBytes);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().StreamingBufferBytes);
        return (diagnostics.StreamingBufferBytes, diagnostics.ReaderMemoryBytes, read);
    }

    private static string PathOf(string name) => Path.IsPathRooted(name) ? name : DecodingCorpus.PathOf(name);
}
