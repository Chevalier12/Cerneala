using Cerneala.Timbre.Catalog;

namespace Cerneala.Timbre.Dsp;

// Two-pole Butterworth low-pass as a topology-preserving-transform state-
// variable filter (Zavalishin; Simper, "Linear Trapezoidal Integrated SVF"),
// damping k = sqrt(2). Equivalent to the bilinear-transformed Butterworth but
// stable under per-block cutoff modulation. State is per channel.
internal sealed class LowPassKernel
{
    // State below this magnitude is flushed so long silence never runs on
    // subnormal floats; it is far below the 1e-6 tail criterion.
    private const float Subnormal = 1e-20f;

    private float a1;
    private float a2;
    private float a3;
    private float leftState1;
    private float leftState2;
    private float rightState1;
    private float rightState2;

    internal float Cutoff { get; private set; } = float.NaN;

    internal void SetCutoff(float hertz)
    {
        if (hertz == Cutoff)
        {
            return;
        }

        Cutoff = hertz;
        double g = Math.Tan(Math.PI * hertz / TimbreCatalog.SampleRate);
        double k = Math.Sqrt(2.0);
        double first = 1.0 / (1.0 + (g * (g + k)));
        a1 = (float)first;
        a2 = (float)(g * first);
        a3 = (float)(g * g * first);
    }

    internal void Process(Span<float> interleaved, int frames)
    {
        float c1 = a1, c2 = a2, c3 = a3;
        float l1 = leftState1, l2 = leftState2, r1 = rightState1, r2 = rightState2;
        for (int frame = 0; frame < frames; frame++)
        {
            int index = frame * 2;
            float v3 = interleaved[index] - l2;
            float v1 = (c1 * l1) + (c2 * v3);
            float v2 = l2 + (c2 * l1) + (c3 * v3);
            l1 = (2f * v1) - l1;
            l2 = (2f * v2) - l2;
            interleaved[index] = v2;

            v3 = interleaved[index + 1] - r2;
            v1 = (c1 * r1) + (c2 * v3);
            v2 = r2 + (c2 * r1) + (c3 * v3);
            r1 = (2f * v1) - r1;
            r2 = (2f * v2) - r2;
            interleaved[index + 1] = v2;
        }

        leftState1 = Flush(l1);
        leftState2 = Flush(l2);
        rightState1 = Flush(r1);
        rightState2 = Flush(r2);
    }

    internal void Reset()
    {
        leftState1 = leftState2 = rightState1 = rightState2 = 0f;
    }

    private static float Flush(float value) => Math.Abs(value) < Subnormal ? 0f : value;
}
