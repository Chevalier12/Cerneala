namespace Cerneala.Tests.Timbre.Dsp;

// Independent double-precision references. The low-pass oracle is the RBJ
// Audio EQ Cookbook biquad (Q = 1/sqrt(2)) in direct form I, a different
// structure from the engine's TPT state-variable filter with the same
// bilinear-transformed Butterworth transfer function.
internal static class DspOracle
{
    public const int SampleRate = 48000;

    public static double[] LowPass(double[] input, double cutoff)
    {
        double w0 = 2 * Math.PI * cutoff / SampleRate;
        double alpha = Math.Sin(w0) / (2 / Math.Sqrt(2));
        double cos = Math.Cos(w0);
        double a0 = 1 + alpha;
        double b0 = (1 - cos) / 2 / a0;
        double b1 = (1 - cos) / a0;
        double b2 = b0;
        double a1 = -2 * cos / a0;
        double a2 = (1 - alpha) / a0;
        double[] output = new double[input.Length];
        double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
        for (int n = 0; n < input.Length; n++)
        {
            double y = (b0 * input[n]) + (b1 * x1) + (b2 * x2) - (a1 * y1) - (a2 * y2);
            x2 = x1;
            x1 = input[n];
            y2 = y1;
            y1 = y;
            output[n] = y;
        }

        return output;
    }

    // w[n] = x[n] + feedback * w[n - D]; y[n] = (1 - mix) * x[n] + mix * w[n - D].
    public static double[] Delay(double[] input, int delayFrames, double feedback, double mix)
    {
        double[] w = new double[input.Length];
        double[] output = new double[input.Length];
        for (int n = 0; n < input.Length; n++)
        {
            double delayed = n >= delayFrames ? w[n - delayFrames] : 0;
            w[n] = input[n] + (feedback * delayed);
            output[n] = ((1 - mix) * input[n]) + (mix * delayed);
        }

        return output;
    }

    // |H(e^jw)| of the bilinear-transformed 2-pole Butterworth low-pass.
    public static double Magnitude(double frequency, double cutoff) =>
        1 / Math.Sqrt(1 + Math.Pow(Math.Tan(Math.PI * frequency / SampleRate) / Math.Tan(Math.PI * cutoff / SampleRate), 4));

    public static (double[] Left, double[] Right) Channels(Func<long, int, float> signal, long frames)
    {
        double[] left = new double[frames];
        double[] right = new double[frames];
        for (long frame = 0; frame < frames; frame++)
        {
            left[frame] = signal(frame, 0);
            right[frame] = signal(frame, 1);
        }

        return (left, right);
    }

    public static float[] Interleave(double[] left, double[] right, double gain = 1, int start = 0, int frames = -1)
    {
        if (frames < 0)
        {
            frames = left.Length - start;
        }

        float[] result = new float[frames * 2];
        for (int frame = 0; frame < frames; frame++)
        {
            int source = start + frame;
            result[frame * 2] = source < left.Length ? (float)(left[source] * gain) : 0f;
            result[(frame * 2) + 1] = source < right.Length ? (float)(right[source] * gain) : 0f;
        }

        return result;
    }

    public static float[] Interleave(float[] mono2, int start, int frames)
    {
        float[] result = new float[frames * 2];
        Array.Copy(mono2, start * 2, result, 0, frames * 2);
        return result;
    }

    // Amplitude of a sinusoid at `frequency` over whole periods (single DFT bin).
    public static double Amplitude(ReadOnlySpan<float> interleaved, int channel, int startFrame, int frames, double frequency)
    {
        double re = 0, im = 0;
        for (int frame = 0; frame < frames; frame++)
        {
            double phase = 2 * Math.PI * frequency * (startFrame + frame) / SampleRate;
            double sample = interleaved[((startFrame + frame) * 2) + channel];
            re += sample * Math.Cos(phase);
            im += sample * Math.Sin(phase);
        }

        return 2 * Math.Sqrt((re * re) + (im * im)) / frames;
    }

    public static double[] Concat(params double[][] parts) => parts.SelectMany(part => part).ToArray();

    public static double[] Zeros(int count) => new double[count];

    // Deterministic broadband test signal in [-0.5, 0.5].
    public static float Noise(long frame, int channel)
    {
        ulong state = (ulong)(frame * 2 + channel + 1) * 6364136223846793005UL + 1442695040888963407UL;
        state ^= state >> 33;
        state *= 0xff51afd7ed558ccdUL;
        state ^= state >> 33;
        return (float)((state >> 40) / (double)(1UL << 24)) - 0.5f;
    }
}
