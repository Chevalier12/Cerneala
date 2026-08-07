using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismThresholdAnalysis
{
    internal const int BinCount = 256;

    public static float Calculate(
        ReadOnlySpan<PrismPremultipliedColor> pixels,
        PrismColorProfile workingProfile,
        float fallback = 0.5f)
    {
        if (!float.IsFinite(fallback) || fallback is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(fallback));
        }

        Span<int> histogram = stackalloc int[BinCount];
        int sampleCount = 0;
        foreach (PrismPremultipliedColor pixel in pixels)
        {
            if (pixel.Alpha <= 0)
            {
                continue;
            }

            PrismPremultipliedColor linear =
                PrismAdjustmentMath.ConvertProfile(
                    pixel,
                    workingProfile,
                    PrismColorProfile.LinearSrgb);
            Vector3 straight = new(
                (float)(linear.Red / linear.Alpha),
                (float)(linear.Green / linear.Alpha),
                (float)(linear.Blue / linear.Alpha));
            float luminance = Vector3.Dot(
                straight,
                new Vector3(0.2126f, 0.7152f, 0.0722f));
            int bin = (int)MathF.Round(
                Math.Clamp(luminance, 0, 1) * (BinCount - 1));
            histogram[bin]++;
            sampleCount++;
        }

        return Calculate(histogram, sampleCount, fallback);
    }

    internal static float Calculate(
        ReadOnlySpan<int> histogram,
        int sampleCount,
        float fallback = 0.5f)
    {
        if (histogram.Length != BinCount)
        {
            throw new ArgumentException(
                $"Threshold histograms require {BinCount} bins.",
                nameof(histogram));
        }
        if (sampleCount < 0 ||
            !float.IsFinite(fallback) ||
            fallback is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                sampleCount < 0 ? nameof(sampleCount) : nameof(fallback));
        }

        long weightedTotal = 0;
        long counted = 0;
        int nonemptyBinCount = 0;
        int onlyNonemptyBin = 0;
        for (int bin = 0; bin < BinCount; bin++)
        {
            if (histogram[bin] < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(histogram));
            }
            if (histogram[bin] > 0)
            {
                nonemptyBinCount++;
                onlyNonemptyBin = bin;
            }
            counted += histogram[bin];
            weightedTotal += (long)bin * histogram[bin];
        }
        if (sampleCount == 0 || counted != sampleCount)
        {
            return fallback;
        }
        if (nonemptyBinCount == 1)
        {
            return onlyNonemptyBin / (float)(BinCount - 1);
        }

        long backgroundCount = 0;
        long backgroundWeighted = 0;
        double bestVariance = 0;
        int bestBin = 0;
        for (int bin = 0; bin < BinCount - 1; bin++)
        {
            backgroundCount += histogram[bin];
            backgroundWeighted += (long)bin * histogram[bin];
            long foregroundCount = sampleCount - backgroundCount;
            if (backgroundCount == 0 || foregroundCount == 0)
            {
                continue;
            }

            double backgroundMean =
                (double)backgroundWeighted / backgroundCount;
            double foregroundMean =
                (double)(weightedTotal - backgroundWeighted) /
                foregroundCount;
            double difference = backgroundMean - foregroundMean;
            double variance =
                backgroundCount * (double)foregroundCount *
                difference * difference;
            if (variance > bestVariance)
            {
                bestVariance = variance;
                bestBin = bin;
            }
        }

        return bestVariance > 0
            ? bestBin / (float)(BinCount - 1)
            : fallback;
    }
}
