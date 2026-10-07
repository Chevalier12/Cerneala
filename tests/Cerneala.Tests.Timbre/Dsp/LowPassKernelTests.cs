using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre.Dsp;

namespace Cerneala.Tests.Timbre.Dsp;

// Tolerances: the kernel runs in float32 (epsilon 1.19e-7) with recursive
// state; over 9600 samples at cutoffs >= 200 Hz the accumulated error versus the
// double-precision oracle stays well below 2e-5 for unit-scale inputs.
public sealed class LowPassKernelTests
{
    private const float Tolerance = 2e-5f;

    [Theory]
    [InlineData(200f)]
    [InlineData(1200f)]
    [InlineData(6000f)]
    [InlineData(20000f)]
    public void ImpulseResponseMatchesTheRbjButterworthBiquad(float cutoff)
    {
        const int frames = 9600;
        float[] samples = new float[frames * 2];
        samples[0] = 1f;
        samples[1] = -0.5f;
        LowPassKernel kernel = new();
        kernel.SetCutoff(cutoff);

        kernel.Process(samples, frames);

        double[] left = new double[frames];
        double[] right = new double[frames];
        left[0] = 1;
        right[0] = -0.5;
        TimbreRig.AssertPcm(DspOracle.Interleave(DspOracle.LowPass(left, cutoff), DspOracle.LowPass(right, cutoff)), samples, Tolerance);
    }

    [Fact]
    public void DcGainIsUnity()
    {
        const int frames = 48000;
        float[] samples = new float[frames * 2];
        Array.Fill(samples, 0.5f);
        LowPassKernel kernel = new();
        kernel.SetCutoff(1200f);

        kernel.Process(samples, frames);

        Assert.InRange(samples[^2], 0.5f - 1e-5f, 0.5f + 1e-5f);
        Assert.InRange(samples[^1], 0.5f - 1e-5f, 0.5f + 1e-5f);
    }

    [Theory]
    [InlineData(200.0)]
    [InlineData(1000.0)]
    [InlineData(4000.0)]
    [InlineData(12000.0)]
    public void SinusoidsFollowTheMagnitudeOracleBelowAtAndAboveCutoff(double frequency)
    {
        const double cutoff = 1000;
        const int frames = 48000;
        float[] samples = new float[frames * 2];
        for (int frame = 0; frame < frames; frame++)
        {
            float value = (float)Math.Sin(2 * Math.PI * frequency * frame / DspOracle.SampleRate);
            samples[frame * 2] = value;
            samples[(frame * 2) + 1] = 0.25f * value;
        }

        LowPassKernel kernel = new();
        kernel.SetCutoff((float)cutoff);
        kernel.Process(samples, frames);

        // Measure over the last 24000 frames: whole periods for every tested
        // frequency, long after the filter transient has decayed.
        double expected = DspOracle.Magnitude(frequency, cutoff);
        double left = DspOracle.Amplitude(samples, 0, 24000, 24000, frequency);
        double right = DspOracle.Amplitude(samples, 1, 24000, 24000, frequency);
        Assert.InRange(left, expected - 1e-4, expected + 1e-4);
        Assert.InRange(right, (0.25 * expected) - 1e-4, (0.25 * expected) + 1e-4);
        if (frequency == cutoff)
        {
            Assert.InRange(20 * Math.Log10(left), -3.02, -3.0);
        }
    }

    [Fact]
    public void ChannelsAreProcessedIndependently()
    {
        const int frames = 4800;
        float[] both = new float[frames * 2];
        float[] leftOnly = new float[frames * 2];
        for (int frame = 0; frame < frames; frame++)
        {
            both[frame * 2] = DspOracle.Noise(frame, 0);
            both[(frame * 2) + 1] = DspOracle.Noise(frame, 1);
            leftOnly[frame * 2] = both[frame * 2];
        }

        LowPassKernel first = new();
        LowPassKernel second = new();
        first.SetCutoff(900f);
        second.SetCutoff(900f);
        first.Process(both, frames);
        second.Process(leftOnly, frames);

        for (int frame = 0; frame < frames; frame++)
        {
            Assert.Equal(both[frame * 2], leftOnly[frame * 2]);
            Assert.Equal(0f, leftOnly[(frame * 2) + 1]);
        }
    }

    [Fact]
    public void IrregularPartitionsMatchASingleBlockExactly()
    {
        const int frames = 5000;
        float[] whole = new float[frames * 2];
        for (int index = 0; index < whole.Length; index++)
        {
            whole[index] = DspOracle.Noise(index / 2, index % 2);
        }

        float[] partitioned = (float[])whole.Clone();
        LowPassKernel single = new();
        LowPassKernel split = new();
        single.SetCutoff(2500f);
        split.SetCutoff(2500f);
        single.Process(whole, frames);

        int[] sizes = [1, 37, 480, 2, 999, 13, 1024, 7];
        int offset = 0;
        int next = 0;
        while (offset < frames)
        {
            int count = Math.Min(sizes[next++ % sizes.Length], frames - offset);
            split.Process(partitioned.AsSpan(offset * 2), count);
            offset += count;
        }

        Assert.Equal(whole, partitioned);
    }

    [Fact]
    public void CutoffModulationStaysFiniteAndBoundedAndResetClearsState()
    {
        const int block = 480;
        float[] samples = new float[block * 2];
        LowPassKernel kernel = new();
        float peak = 0;
        for (int iteration = 0; iteration < 2000; iteration++)
        {
            for (int index = 0; index < samples.Length; index++)
            {
                samples[index] = DspOracle.Noise((iteration * block) + (index / 2), index % 2);
            }

            kernel.SetCutoff(iteration % 2 == 0 ? 20f : 20000f);
            kernel.Process(samples, block);
            foreach (float sample in samples)
            {
                Assert.True(float.IsFinite(sample));
                peak = Math.Max(peak, Math.Abs(sample));
            }
        }

        Assert.True(peak < 2f, $"peak {peak}");
        Assert.Equal(20000f, kernel.Cutoff);

        kernel.Reset();
        float[] silence = new float[block * 2];
        kernel.Process(silence, block);
        Assert.All(silence, sample => Assert.Equal(0f, sample));
    }
}
