namespace Cerneala.Timbre.Corpus;

// Closed-form oracle of every synthetic corpus file. It is shared by the
// generator and the tests, so expected PCM never comes from a decoder.
// Channel 0 is a rising chirp, channel 1 a falling chirp; both repeat with
// the given period, which makes every position inside a period unique.
public static class CorpusSignal
{
    public const double LeftAmplitude = 0.5;
    public const double RightAmplitude = 0.3;

    public static double Sample(int channel, double seconds, double period)
    {
        double u = seconds % period;
        (double amplitude, double from, double to) = channel == 0
            ? (LeftAmplitude, 200.0, 1200.0)
            : (RightAmplitude, 1500.0, 400.0);
        double phase = 2.0 * Math.PI * ((from * u) + ((to - from) * u * u / (2.0 * period)));
        return amplitude * Math.Sin(phase);
    }

    // Interleaved samples of `frames` frames at `sampleRate`; a mono file
    // carries channel 0.
    public static float[] Render(int sampleRate, int channels, long frames, double period)
    {
        float[] samples = new float[frames * channels];
        for (long frame = 0; frame < frames; frame++)
        {
            double seconds = (double)frame / sampleRate;
            for (int channel = 0; channel < channels; channel++)
            {
                samples[(frame * channels) + channel] = (float)Sample(channel, seconds, period);
            }
        }

        return samples;
    }
}
