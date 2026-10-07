using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Decoding;

// Every positive corpus file and the synthesized WAV matrix decode, through
// the production decoder seam, to canonical stereo float32 48 kHz PCM that
// matches the generator's closed-form signal.
public sealed class SoundDecoderConformanceTests
{
    // Thresholds sit below the probe's worst case for each codec
    // (stage-0 probe-summary.md: MP3 25.8 dB, Vorbis 25.5 dB, Opus 27.4 dB,
    // 16/24-bit WAV through the resampler 61 dB, 8-bit WAV 39.8 dB).
    private const double LossySnrDb = 20;
    private const double WavSnrDb = 55;
    private const double EightBitWavSnrDb = 35;

    public static IEnumerable<object[]> Corpus() => DecodingCorpus.Positive();

    public static IEnumerable<object[]> WavMatrix()
    {
        foreach ((int bits, bool isFloat, bool extensible) in new[] { (8, false, false), (16, false, false), (24, false, false), (32, false, false), (32, true, false), (24, false, true), (32, true, true) })
        {
            yield return [44100, 2, bits, isFloat, extensible];
            yield return [48000, 1, bits, isFloat, extensible];
        }

        foreach (int rate in new[] { 8000, 11025, 16000, 22050, 32000, 88200, 96000, 176400, 192000 })
        {
            yield return [rate, 1, 16, false, false];
            yield return [rate, 2, 24, false, false];
        }
    }

    [Theory]
    [MemberData(nameof(Corpus))]
    public void CorpusFileDecodesToTheClosedFormSignal(string name)
    {
        CorpusExpectation file = DecodingCorpus.Describe(name)!;
        using SoundReader reader = DecodingCorpus.Open(DecodingCorpus.PathOf(name));
        long? declared = reader.LengthFrames;

        float[] pcm = DecodingCorpus.DecodeAll(reader);

        long frames = pcm.Length / 2;
        if (file.Gapless)
        {
            Assert.Equal(file.CanonicalFrames, declared);
            Assert.Equal(file.CanonicalFrames, frames);
        }
        else
        {
            // No Info tag: nothing is trimmed and the total is only known at
            // the end; the decoded frames cover delay + signal + < 2 padding frames.
            long minimum = ((file.SourceFrames + DecodingCorpus.UntaggedMp3Delay) * DecodingCorpus.Rate) / file.SampleRate;
            long maximum = ((file.SourceFrames + DecodingCorpus.UntaggedMp3Delay + 2304) * DecodingCorpus.Rate) / file.SampleRate;
            Assert.InRange(frames, minimum, maximum);
            Assert.Equal(frames, reader.LengthFrames);
        }

        AssertMatchesOracle(file, pcm, LossySnrDb);
    }

    [Theory]
    [MemberData(nameof(WavMatrix))]
    public void WavMatrixDecodesExactlyToTheQuantizedSignal(int sampleRate, int channels, int bits, bool isFloat, bool extensible)
    {
        string path = WavFixture.Write($"matrix-{sampleRate}-{channels}-{bits}-{isFloat}-{extensible}.wav", sampleRate, channels, bits, isFloat, seconds: 1.0, extensible: extensible);
        CorpusExpectation file = new("wav", sampleRate, channels, sampleRate, 1.0, 0, Gapless: true);
        using SoundReader reader = DecodingCorpus.Open(path);

        float[] pcm = DecodingCorpus.DecodeAll(reader);

        Assert.Equal(file.CanonicalFrames, reader.LengthFrames);
        Assert.Equal(file.CanonicalFrames, pcm.Length / 2);
        AssertMatchesOracle(file, pcm, bits == 8 ? EightBitWavSnrDb : WavSnrDb);
    }

    private static void AssertMatchesOracle(CorpusExpectation file, float[] pcm, double thresholdDb)
    {
        Assert.All(pcm, sample => Assert.True(float.IsFinite(sample)));
        long frames = pcm.Length / 2;

        // Alignment: the best match is at lag 0 (±1 frame of rounding).
        int lag = PcmOracle.Lag(file, pcm, start: 9600, length: 4800, maxLag: 96);
        Assert.InRange(lag, -1, 1);

        // Channel order and fidelity per channel.
        double left = PcmOracle.SnrDb(file, pcm, 0);
        double right = PcmOracle.SnrDb(file, pcm, 1);
        Assert.True(left >= thresholdDb, $"Left channel SNR {left:F2} dB is below {thresholdDb} dB.");
        Assert.True(right >= thresholdDb, $"Right channel SNR {right:F2} dB is below {thresholdDb} dB.");
        if (file.Channels == 1)
        {
            for (long frame = 0; frame < frames; frame++)
            {
                Assert.Equal(pcm[frame * 2], pcm[(frame * 2) + 1]);
            }
        }
        else
        {
            CorpusExpectation swapped = file with { Channels = 1 };
            Assert.True(PcmOracle.SnrDb(swapped, pcm, 1) < 0, "The right channel matches the left source channel: channels are swapped or downmixed.");
        }

        // Level and DC against the oracle over the same frames.
        for (int channel = 0; channel < 2; channel++)
        {
            (double mean, double rms) = PcmOracle.Level(pcm, channel);
            double expectedRms = PcmOracle.OracleRms(file, channel, frames);
            Assert.InRange(mean, -0.01, 0.01);
            Assert.InRange(rms / expectedRms, 0.85, 1.15);
        }
    }
}
