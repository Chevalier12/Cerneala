using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Decoding;

// Decoding does not depend on how the consumer partitions its reads, trims
// exactly, and reports EOF once without duplicating or dropping frames.
public sealed class SoundDecoderPartitionTests
{
    public static IEnumerable<object[]> Files() =>
        DecodingCorpus.ShortPositive().Append(["mp3-reservoir-wrap-48000-stereo-cbr64.mp3"]);

    [Theory]
    [MemberData(nameof(Files))]
    public void IrregularReadsProduceTheSamePcmAsLargeReads(string name)
    {
        string path = DecodingCorpus.PathOf(name);
        float[] whole;
        using (SoundReader reader = DecodingCorpus.Open(path))
        {
            whole = DecodingCorpus.DecodeAll(reader, _ => 16384);
        }

        Random random = new(1234);
        float[] partitioned;
        using (SoundReader reader = DecodingCorpus.Open(path))
        {
            partitioned = DecodingCorpus.DecodeAll(reader, _ => random.Next(1, 3001));
        }

        Assert.Equal(whole.Length, partitioned.Length);
        Assert.True(whole.AsSpan().SequenceEqual(partitioned), "Irregular partitions changed the decoded PCM.");
    }

    [Fact]
    public void OpusPreSkipAndEndTrimmingAreApplied()
    {
        // 312 + 3840 frames of pre-skip and a final granule mid-packet.
        CorpusExpectation file = DecodingCorpus.Describe("opus-stereo-preskip-trim.opus")!;
        using SoundReader reader = DecodingCorpus.Open(DecodingCorpus.PathOf("opus-stereo-preskip-trim.opus"));

        float[] pcm = DecodingCorpus.DecodeAll(reader);

        Assert.Equal(59256, pcm.Length / 2);
        Assert.Equal(59256, file.CanonicalFrames);
        // The skipped silence is gone: the very first 100 ms already match the signal.
        Assert.True(PcmOracle.SnrDb(file, pcm, 0, 960, 4800) >= 15);
        Assert.True(PcmOracle.SnrDb(file, pcm, 0, 54000, 59256) >= 15);
    }

    [Theory]
    [InlineData("mp3-mpeg1-32000-stereo-vbr.mp3")]
    [InlineData("mp3-mpeg2-22050-mono-vbr.mp3")]
    public void Mp3VbrLengthComesFromTheInfoTagBeforeDecoding(string name)
    {
        CorpusExpectation file = DecodingCorpus.Describe(name)!;
        using SoundReader reader = DecodingCorpus.Open(DecodingCorpus.PathOf(name));

        Assert.Equal(file.CanonicalFrames, reader.LengthFrames);
    }

    [Fact]
    public void Mp3FrameAtTheNLayerReservoirWrapIsNotDropped()
    {
        // 192-byte frames feed 156 reservoir bytes each; after frame 2047 the
        // fed bytes are exactly 39 × 8192, the state NLayer 3.0.0 mistakes for
        // a reset (it would decode frame 2048 to nothing).
        const string Name = "mp3-reservoir-wrap-48000-stereo-cbr64.mp3";
        CorpusExpectation file = DecodingCorpus.Describe(Name)!;
        using SoundReader reader = DecodingCorpus.Open(DecodingCorpus.PathOf(Name));

        float[] pcm = DecodingCorpus.DecodeAll(reader);

        Assert.Equal(file.CanonicalFrames, pcm.Length / 2);
        long wrapFrame = (2048 * 1152L) - 576 - 529;
        Assert.True(PcmOracle.SnrDb(file, pcm, 0, wrapFrame - 2304, wrapFrame + 4608) >= 20);
        Assert.InRange(PcmOracle.Lag(file, pcm, wrapFrame + 1152, 2304, 96), -1, 1);
    }

    [Fact]
    public void ReadersOfTheSameFileShareNoDecoderState()
    {
        string path = DecodingCorpus.PathOf("vorbis-44100-stereo-q4.ogg");
        float[] expected;
        using (SoundReader single = DecodingCorpus.Open(path))
        {
            expected = DecodingCorpus.DecodeAll(single);
        }

        using SoundReader first = DecodingCorpus.Open(path);
        using SoundReader second = DecodingCorpus.Open(path);
        List<float> a = [];
        List<float> b = [];
        float[] buffer = new float[777 * 2];
        bool firstDone = false;
        bool secondDone = false;
        while (!firstDone || !secondDone)
        {
            firstDone = firstDone || Step(first, a);
            secondDone = secondDone || Step(second, b);
        }

        Assert.True(expected.AsSpan().SequenceEqual(a.ToArray()));
        Assert.True(expected.AsSpan().SequenceEqual(b.ToArray()));

        bool Step(SoundReader reader, List<float> into)
        {
            SoundReadResult result = reader.ReadAsync(buffer, CancellationToken.None).AsTask().GetAwaiter().GetResult();
            into.AddRange(buffer.AsSpan(0, result.Frames * 2).ToArray());
            return result.EndOfSource;
        }
    }
}
