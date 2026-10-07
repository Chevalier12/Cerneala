using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre.Dsp;

namespace Cerneala.Tests.Timbre.Dsp;

public sealed class DelayKernelTests
{
    [Fact]
    public void DelayFramesRoundTimeToTheNearestFrame()
    {
        Assert.Equal(48, DelayKernel.ToDelayFrames(0.001f));
        Assert.Equal(5760, DelayKernel.ToDelayFrames(0.12f));
        Assert.Equal(96000, DelayKernel.ToDelayFrames(2f));
        Assert.Equal(480, DelayKernel.ToDelayFrames(0.0100104f));
    }

    [Fact]
    public void ImpulseProducesTheDryValueAndAGeometricEchoTrain()
    {
        const int frames = 3000;
        float[] samples = new float[frames * 2];
        samples[0] = 1f;
        samples[1] = 0.5f;
        DelayKernel kernel = new(480);
        kernel.Set(0.01f, 0.5f, 0.3f);

        kernel.Process(samples, frames);

        for (int frame = 0; frame < frames; frame++)
        {
            double expected = frame == 0 ? 0.7 : frame % 480 == 0 ? 0.3 * Math.Pow(0.5, (frame / 480) - 1) : 0;
            Assert.Equal(expected, samples[frame * 2], 6);
            Assert.Equal(expected * 0.5, samples[(frame * 2) + 1], 6);
        }
    }

    [Fact]
    public void MatchesTheReferenceOnBroadbandInput()
    {
        const int frames = 20000;
        float[] samples = new float[frames * 2];
        double[] left = new double[frames];
        double[] right = new double[frames];
        for (int frame = 0; frame < frames; frame++)
        {
            samples[frame * 2] = DspOracle.Noise(frame, 0);
            samples[(frame * 2) + 1] = DspOracle.Noise(frame, 1);
            left[frame] = samples[frame * 2];
            right[frame] = samples[(frame * 2) + 1];
        }

        DelayKernel kernel = new(DelayKernel.ToDelayFrames(0.0371f));
        kernel.Set(0.0371f, 0.95f, 0.6f);
        kernel.Process(samples, frames);

        int delay = DelayKernel.ToDelayFrames(0.0371f);
        TimbreRig.AssertPcm(
            DspOracle.Interleave(DspOracle.Delay(left, delay, 0.95, 0.6), DspOracle.Delay(right, delay, 0.95, 0.6)),
            samples,
            2e-5f);
    }

    [Fact]
    public void MixEndpointsAreFullyDryAndFullyWet()
    {
        const int frames = 1000;
        float[] dry = new float[frames * 2];
        float[] wet = new float[frames * 2];
        for (int index = 0; index < dry.Length; index++)
        {
            dry[index] = wet[index] = DspOracle.Noise(index / 2, index % 2);
        }

        float[] input = (float[])dry.Clone();
        DelayKernel dryKernel = new(100);
        DelayKernel wetKernel = new(100);
        dryKernel.Set(100f / 48000f, 0.5f, 0f);
        wetKernel.Set(100f / 48000f, 0f, 1f);
        dryKernel.Process(dry, frames);
        wetKernel.Process(wet, frames);

        Assert.Equal(input, dry);
        for (int frame = 0; frame < frames; frame++)
        {
            float expectedLeft = frame >= 100 ? input[(frame - 100) * 2] : 0f;
            Assert.Equal(expectedLeft, wet[frame * 2]);
        }
    }

    [Fact]
    public void ChannelsAreIndependentAndPartitionsMatchASingleBlock()
    {
        const int frames = 6000;
        float[] whole = new float[frames * 2];
        for (int frame = 0; frame < frames; frame++)
        {
            whole[frame * 2] = DspOracle.Noise(frame, 0);
        }

        float[] split = (float[])whole.Clone();
        DelayKernel single = new(777);
        DelayKernel partitioned = new(777);
        single.Set(777f / 48000f, 0.8f, 0.5f);
        partitioned.Set(777f / 48000f, 0.8f, 0.5f);
        single.Process(whole, frames);
        int offset = 0;
        foreach (int size in Enumerable.Repeat(new[] { 1, 480, 33, 1000, 2 }, 20).SelectMany(sizes => sizes))
        {
            int count = Math.Min(size, frames - offset);
            if (count == 0)
            {
                break;
            }

            partitioned.Process(split.AsSpan(offset * 2), count);
            offset += count;
        }

        Assert.Equal(frames, offset);
        Assert.Equal(whole, split);
        for (int frame = 0; frame < frames; frame++)
        {
            Assert.Equal(0f, whole[(frame * 2) + 1]);
        }
    }

    [Fact]
    public void ChangingTimeMovesTheReadPositionImmediately()
    {
        DelayKernel kernel = new(96000);
        kernel.Set(0.01f, 0f, 1f); // 480 frames
        float[] first = new float[600 * 2];
        first[0] = 1f;
        kernel.Process(first, 600);
        Assert.Equal(1f, first[480 * 2]);

        // The impulse was written at absolute frame 0. With 960 frames of delay it
        // is read again at absolute frame 960, i.e. offset 360 of the next block.
        kernel.Set(0.02f, 0f, 1f);
        float[] second = new float[600 * 2];
        kernel.Process(second, 600);
        Assert.Equal(960, kernel.DelayFrames);
        Assert.Equal(1f, second[360 * 2]);
        Assert.Equal(1f, second.Sum());
    }

    [Fact]
    public void MaximumFeedbackStaysBoundedAndResetClearsTheLine()
    {
        const int frames = 48000;
        float[] samples = new float[frames * 2];
        for (int index = 0; index < samples.Length; index++)
        {
            samples[index] = DspOracle.Noise(index / 2, index % 2);
        }

        DelayKernel kernel = new(48);
        kernel.Set(0.001f, 0.95f, 1f);
        kernel.Process(samples, frames);

        // |w| <= max|x| / (1 - feedback) = 0.5 / 0.05.
        Assert.All(samples, sample => Assert.True(float.IsFinite(sample) && Math.Abs(sample) <= 10f));
        kernel.Reset();
        float[] silence = new float[1000 * 2];
        kernel.Process(silence, 1000);
        Assert.All(silence, sample => Assert.Equal(0f, sample));
        Assert.Equal(48, kernel.CapacityFrames);
    }
}
