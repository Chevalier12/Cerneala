using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Decoding;

// Reader-level seek sequences (forward, backward, back to the start) land on
// the exact decoded frame for every codec, with and without resampling.
public sealed class SoundStreamingSeekTests
{
    public static IEnumerable<object[]> Files()
    {
        yield return ["mp3-mpeg1-44100-stereo-cbr128.mp3"];
        yield return ["mp3-mpeg2-22050-mono-vbr.mp3"];
        yield return ["vorbis-44100-stereo-q4.ogg"];
        yield return ["vorbis-48000-stereo-q2.ogg"];
        yield return [WavFixture.Write("seek-sequence.wav", 44100, 2, 24, seconds: 2)];
    }

    [Theory]
    [MemberData(nameof(Files))]
    public async Task SeekSequencesLandOnTheExactDecodedFrame(string name)
    {
        string path = Path.IsPathRooted(name) ? name : DecodingCorpus.PathOf(name);
        float[] expected;
        using (SoundReader whole = DecodingCorpus.Open(path))
        {
            expected = DecodingCorpus.DecodeAll(whole);
        }

        long length = expected.Length / 2;
        using SoundReader reader = DecodingCorpus.Open(path);
        float[] buffer = new float[960];
        await reader.ReadAsync(buffer, CancellationToken.None);
        foreach (long target in new[] { length / 2, 0, length - 3000, 4801, 0, 1, length / 3 })
        {
            await reader.SeekAsync(target, CancellationToken.None);
            SoundReadResult result = await reader.ReadAsync(buffer, CancellationToken.None);

            Assert.Equal(480, result.Frames);
            Assert.True(
                buffer.AsSpan().SequenceEqual(expected.AsSpan((int)(target * 2), 960)),
                $"The first frames after seeking to {target} differ from the continuous decode.");
        }
    }
}
