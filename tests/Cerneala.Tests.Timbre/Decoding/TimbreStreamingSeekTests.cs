using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Decoding;

// Reader-level seek sequences (forward, backward, back to the start) land on
// the exact decoded frame for every codec, with and without resampling. A seek
// that fails part-way returns to the old position, or fails every later read.
public sealed class TimbreStreamingSeekTests
{
    public static IEnumerable<object[]> Files()
    {
        yield return ["mp3-mpeg1-44100-stereo-cbr128.mp3"];
        yield return ["mp3-mpeg2-22050-mono-vbr.mp3"];
        yield return ["vorbis-44100-stereo-q4.ogg"];
        yield return ["vorbis-48000-stereo-q2.ogg"];
        yield return [WavFixture.Write("seek-sequence.wav", 44100, 2, 24, seconds: 2)];
    }

    // The 48 kHz files fail inside their decoder's seek; the 44.1 kHz WAV
    // fails while the converter restarts its resampler before the target.
    public static IEnumerable<object[]> FailingSeekFiles()
    {
        yield return ["mp3-mpeg1-48000-mono-cbr64.mp3"];
        yield return ["vorbis-48000-stereo-q2.ogg"];
        yield return ["opus-stereo-48000-96k.opus"];
        yield return [WavFixture.Write("seek-failure-reader.wav", 44100, 2, 24, seconds: 2)];
    }

    [Theory]
    [MemberData(nameof(Files))]
    public async Task SeekSequencesLandOnTheExactDecodedFrame(string name)
    {
        string path = PathOf(name);
        float[] expected = Decode(path);
        long length = expected.Length / 2;
        using TimbreReader reader = DecodingCorpus.Open(path);
        float[] buffer = new float[960];
        await reader.ReadAsync(buffer, CancellationToken.None);
        foreach (long target in new[] { length / 2, 0, length - 3000, 4801, 0, 1, length / 3 })
        {
            await reader.SeekAsync(target, CancellationToken.None);
            TimbreReadResult result = await reader.ReadAsync(buffer, CancellationToken.None);

            Assert.Equal(480, result.Frames);
            Assert.True(
                buffer.AsSpan().SequenceEqual(expected.AsSpan((int)(target * 2), 960)),
                $"The first frames after seeking to {target} differ from the continuous decode.");
        }
    }

    [Theory]
    [MemberData(nameof(FailingSeekFiles))]
    public async Task ASeekThatFailsPartWayReturnsToTheOldPosition(string name)
    {
        string path = PathOf(name);
        float[] expected = Decode(path);
        long target = expected.Length / 4;
        long lastSeekRead = await LastSeekReadAsync(path, target);
        using ObservedFileStream stream = new(path);
        using TimbreReader reader = DecodingCorpus.Open(stream, path);
        float[] buffer = new float[960];
        await reader.ReadAsync(buffer, CancellationToken.None);

        stream.FailRead = (read, _) => read == lastSeekRead;
        AssertIOFailure(await Assert.ThrowsAnyAsync<Exception>(() => reader.SeekAsync(target, CancellationToken.None).AsTask()));

        // Every frame after the failure is the next frame of the continuous decode.
        float[] continued = await ReadAsync(reader, 4800);
        float[] reference = expected[960..(960 + continued.Length)];
        // A restarted Opus decoder is close to, not identical with, the continuous decode.
        TimbreRig.AssertPcm(reference, continued, tolerance: name.EndsWith(".opus", StringComparison.Ordinal) ? 0.05f : 0);
    }

    [Theory]
    [MemberData(nameof(FailingSeekFiles))]
    public async Task ASeekThatFailsPartWayAndCannotReturnFailsEveryLaterRead(string name)
    {
        string path = PathOf(name);
        float[] expected = Decode(path);
        long target = expected.Length / 4;
        long lastSeekRead = await LastSeekReadAsync(path, target);
        using ObservedFileStream stream = new(path);
        using TimbreReader reader = DecodingCorpus.Open(stream, path);
        float[] buffer = new float[960];
        await reader.ReadAsync(buffer, CancellationToken.None);

        bool healed = false;
        stream.FailRead = (read, _) => !healed && read >= lastSeekRead;
        AssertIOFailure(await Assert.ThrowsAnyAsync<Exception>(() => reader.SeekAsync(target, CancellationToken.None).AsTask()));
        healed = true;

        // The stream works again, but the decoder no longer knows where it is:
        // frames converted before the seek still play, then reading fails.
        List<float> played = [];
        Exception? error = null;
        for (int read = 0; read < 20 && error is null; read++)
        {
            try
            {
                TimbreReadResult result = await reader.ReadAsync(buffer, CancellationToken.None);
                played.AddRange(buffer[..(result.Frames * 2)]);
            }
            catch (Exception exception)
            {
                error = exception;
            }
        }

        AssertIOFailure(error ?? throw new Xunit.Sdk.XunitException("Reading after the failed return never failed."));
        TimbreRig.AssertPcm(expected[960..(960 + played.Count)], played.ToArray(), tolerance: 0);
        AssertIOFailure(await Assert.ThrowsAnyAsync<Exception>(() => reader.SeekAsync(0, CancellationToken.None).AsTask()));
    }

    // The number of the last stream read a seek to `target` makes after one
    // 480-frame read, measured on a separate reader: decoding is deterministic.
    private static async Task<long> LastSeekReadAsync(string path, long target)
    {
        using ObservedFileStream stream = new(path);
        using TimbreReader reader = DecodingCorpus.Open(stream, path);
        await reader.ReadAsync(new float[960], CancellationToken.None);
        long before = stream.Reads;
        await reader.SeekAsync(target, CancellationToken.None);
        Assert.True(stream.Reads > before, "The seek read nothing from the stream.");
        return stream.Reads - 1;
    }

    private static async Task<float[]> ReadAsync(TimbreReader reader, int frames)
    {
        float[] pcm = new float[frames * 2];
        int filled = 0;
        while (filled < frames)
        {
            TimbreReadResult result = await reader.ReadAsync(pcm.AsMemory(filled * 2), CancellationToken.None);
            filled += result.Frames;
            if (result.EndOfSource)
            {
                break;
            }
        }

        return pcm[..(filled * 2)];
    }

    // The storage error itself, not a decoder error wrapping it.
    private static void AssertIOFailure(Exception error) =>
        Assert.True(error.GetType() == typeof(IOException), $"Expected the stream's IOException, got {error}.");

    private static float[] Decode(string path)
    {
        using TimbreReader whole = DecodingCorpus.Open(path);
        return DecodingCorpus.DecodeAll(whole);
    }

    private static string PathOf(string name) => Path.IsPathRooted(name) ? name : DecodingCorpus.PathOf(name);
}
