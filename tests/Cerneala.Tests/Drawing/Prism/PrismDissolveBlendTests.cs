using Cerneala.Drawing.Prism.Blending;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismDissolveBlendTests
{
    [Fact]
    public void ThresholdMapIsUniformAndHighFrequencyBiased()
    {
        byte[] thresholds = PrismDissolveBlend.Thresholds.ToArray();

        Assert.Equal(
            PrismDissolveBlend.ThresholdSize *
                PrismDissolveBlend.ThresholdSize,
            thresholds.Length);
        int[] histogram = new int[PrismDissolveBlend.ThresholdSize];
        foreach (byte threshold in thresholds)
        {
            histogram[threshold]++;
        }
        Assert.All(
            histogram,
            count => Assert.Equal(
                PrismDissolveBlend.ThresholdSize,
                count));

        (double horizontal, double vertical) =
            MeasureNeighborCorrelation(thresholds);
        Assert.True(
            horizontal < -0.15,
            $"Horizontal neighbor correlation was {horizontal}.");
        Assert.True(
            vertical < -0.15,
            $"Vertical neighbor correlation was {vertical}.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0x1234)]
    [InlineData(0xffff)]
    public void SelectionHasExactMonotonicCoverage(int normalizedSeed)
    {
        int[] selectedPerCutoff = new int[257];
        for (int y = 0; y < PrismDissolveBlend.ThresholdSize; y++)
        {
            for (int x = 0; x < PrismDissolveBlend.ThresholdSize; x++)
            {
                byte threshold = PrismDissolveBlend.ThresholdAt(
                    x,
                    y,
                    normalizedSeed);
                for (int cutoff = threshold + 1;
                    cutoff < selectedPerCutoff.Length;
                    cutoff++)
                {
                    selectedPerCutoff[cutoff]++;
                }
            }
        }

        for (int cutoff = 0;
            cutoff < selectedPerCutoff.Length;
            cutoff++)
        {
            Assert.Equal(
                cutoff * PrismDissolveBlend.ThresholdSize,
                selectedPerCutoff[cutoff]);
        }
    }

    [Fact]
    public void SeedAppliesToroidalByteOffsets()
    {
        const int seed = 0x1234;
        byte expected = PrismDissolveBlend.ThresholdAt(
            0x34,
            0x12,
            0);

        Assert.Equal(
            expected,
            PrismDissolveBlend.ThresholdAt(0, 0, seed));
        Assert.Equal(
            expected,
            PrismDissolveBlend.ThresholdAt(256, 256, seed));
    }

    private static (double Horizontal, double Vertical)
        MeasureNeighborCorrelation(byte[] thresholds)
    {
        const double mean = 127.5;
        double horizontal = 0;
        double vertical = 0;
        double variance = 0;
        int size = PrismDissolveBlend.ThresholdSize;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                double centered = thresholds[(y * size) + x] - mean;
                double right = thresholds[
                    (y * size) + ((x + 1) & 0xff)] - mean;
                double down = thresholds[
                    (((y + 1) & 0xff) * size) + x] - mean;
                horizontal += centered * right;
                vertical += centered * down;
                variance += centered * centered;
            }
        }

        return (horizontal / variance, vertical / variance);
    }
}
