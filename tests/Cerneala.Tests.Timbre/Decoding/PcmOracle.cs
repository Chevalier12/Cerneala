using Cerneala.Timbre.Corpus;

namespace Cerneala.Tests.Timbre.Decoding;

// Compares canonical stereo 48 kHz PCM with CorpusSignal, the generator's
// closed-form source signal. Mono sources carry channel 0 on both channels.
internal static class PcmOracle
{
    public static double Expected(CorpusExpectation file, int channel, long frame)
    {
        double seconds = file.StartSeconds + ((double)frame / DecodingCorpus.Rate);
        return seconds < 0 || seconds >= file.SignalSeconds ? 0 : CorpusSignal.Sample(file.Channels == 1 ? 0 : channel, seconds, file.Period);
    }

    // Signal-to-error ratio in dB of one channel over [start, end).
    public static double SnrDb(CorpusExpectation file, float[] pcm, int channel, long start = 0, long end = long.MaxValue, long pcmOrigin = 0)
    {
        double energy = 0;
        double error = 0;
        long last = Math.Min(end, pcmOrigin + (pcm.Length / 2));
        for (long frame = Math.Max(start, pcmOrigin); frame < last; frame++)
        {
            double expected = Expected(file, channel, frame);
            double difference = pcm[((frame - pcmOrigin) * 2) + channel] - expected;
            energy += expected * expected;
            error += difference * difference;
        }

        return 10 * Math.Log10(energy / Math.Max(error, 1e-30));
    }

    // Shift (frames) at which the left channel best matches the oracle over
    // [start, start + length); positive means the PCM is late.
    public static int Lag(CorpusExpectation file, float[] pcm, long start, int length, int maxLag, long pcmOrigin = 0)
    {
        int best = 0;
        double bestScore = double.NegativeInfinity;
        for (int lag = -maxLag; lag <= maxLag; lag++)
        {
            double score = 0;
            for (long frame = start; frame < start + length; frame++)
            {
                long index = frame + lag - pcmOrigin;
                if (index >= 0 && index < pcm.Length / 2)
                {
                    score += pcm[index * 2] * Expected(file, 0, frame);
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = lag;
            }
        }

        return best;
    }

    public static (double Mean, double Rms) Level(float[] pcm, int channel)
    {
        double sum = 0;
        double squares = 0;
        int frames = pcm.Length / 2;
        for (int frame = 0; frame < frames; frame++)
        {
            float sample = pcm[(frame * 2) + channel];
            sum += sample;
            squares += sample * sample;
        }

        return (sum / frames, Math.Sqrt(squares / frames));
    }

    public static double OracleRms(CorpusExpectation file, int channel, long frames)
    {
        double squares = 0;
        for (long frame = 0; frame < frames; frame++)
        {
            double value = Expected(file, channel, frame);
            squares += value * value;
        }

        return Math.Sqrt(squares / frames);
    }
}
