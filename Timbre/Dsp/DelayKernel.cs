using Cerneala.Timbre.Catalog;

namespace Cerneala.Timbre.Dsp;

// Feedback delay with linear dry/wet, per channel and integer frames:
// w[n] = x[n] + feedback * w[n - D], y[n] = (1 - mix) * x[n] + mix * w[n - D].
// A time change moves the read position immediately (no interpolation).
internal sealed class DelayKernel
{
    private const float Subnormal = 1e-20f;

    private readonly float[] left;
    private readonly float[] right;
    private int write;
    private float feedback;
    private float mix;

    internal DelayKernel(int capacityFrames)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacityFrames);
        left = new float[capacityFrames];
        right = new float[capacityFrames];
        DelayFrames = capacityFrames;
    }

    internal int CapacityFrames => left.Length;

    internal int DelayFrames { get; private set; }

    internal long StateBytes => 2L * left.Length * sizeof(float);

    internal static int ToDelayFrames(float seconds) =>
        (int)Math.Round(seconds * (double)TimbreCatalog.SampleRate, MidpointRounding.AwayFromZero);

    internal void Set(float seconds, float feedback, float mix)
    {
        DelayFrames = Math.Min(ToDelayFrames(seconds), left.Length);
        this.feedback = feedback;
        this.mix = mix;
    }

    internal void Process(Span<float> interleaved, int frames)
    {
        int capacity = left.Length;
        int delay = DelayFrames;
        float fb = feedback;
        float wet = mix;
        float dry = 1f - mix;
        int position = write;
        for (int frame = 0; frame < frames; frame++)
        {
            int read = position - delay;
            if (read < 0)
            {
                read += capacity;
            }

            int index = frame * 2;
            float x = interleaved[index];
            float delayed = left[read];
            float line = x + (fb * delayed);
            left[position] = Math.Abs(line) < Subnormal ? 0f : line;
            interleaved[index] = (dry * x) + (wet * delayed);

            x = interleaved[index + 1];
            delayed = right[read];
            line = x + (fb * delayed);
            right[position] = Math.Abs(line) < Subnormal ? 0f : line;
            interleaved[index + 1] = (dry * x) + (wet * delayed);

            if (++position == capacity)
            {
                position = 0;
            }
        }

        write = position;
    }

    internal void Reset()
    {
        Array.Clear(left);
        Array.Clear(right);
        write = 0;
    }
}
