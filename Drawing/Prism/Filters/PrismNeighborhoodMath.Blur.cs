using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;

namespace Cerneala.Drawing.Prism.Filters;

internal static partial class PrismNeighborhoodMath
{
    internal static Vector4[] ApplyBoxBlurSat(
        Vector4[] source,
        int width,
        int height,
        int radiusX,
        int radiusY,
        int edgeMode)
    {
        int paddedWidth = checked(width + (radiusX * 2));
        int paddedHeight = checked(height + (radiusY * 2));
        int stride = checked(paddedWidth + 1);
        Vector4[] sat = new Vector4[checked(stride * (paddedHeight + 1))];
        for (int y = 0; y < paddedHeight; y++)
        {
            Vector4 row = Vector4.Zero;
            for (int x = 0; x < paddedWidth; x++)
            {
                row += Sample(
                    source,
                    width,
                    height,
                    x - radiusX,
                    y - radiusY,
                    edgeMode);
                sat[((y + 1) * stride) + x + 1] =
                    sat[(y * stride) + x + 1] + row;
            }
        }

        int diameterX = checked((radiusX * 2) + 1);
        int diameterY = checked((radiusY * 2) + 1);
        float normalization = 1f / checked(diameterX * diameterY);
        Vector4[] result = new Vector4[source.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int right = x + diameterX;
                int bottom = y + diameterY;
                Vector4 sum =
                    sat[(bottom * stride) + right] -
                    sat[(y * stride) + right] -
                    sat[(bottom * stride) + x] +
                    sat[(y * stride) + x];
                result[(y * width) + x] = sum * normalization;
            }
        }
        return result;
    }

    internal static Vector4 SampleIncrementalGaussian(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        PrismNeighborhoodPass pass,
        int edgeMode,
        float sigma)
    {
        int halfTapCount = Math.Max(0, (pass.SampleCount - 1) / 2);
        if (halfTapCount == 0 || sigma <= 0)
        {
            return source[(y * width) + x];
        }

        float stepX = pass.RadiusX / halfTapCount;
        float stepY = pass.RadiusY / halfTapCount;
        float stepSquared = (stepX * stepX) + (stepY * stepY);
        float ratio = MathF.Exp(-stepSquared / (2 * sigma * sigma));
        float ratioStep = ratio * ratio;
        float weight = 1;
        float multiplier = ratio;
        Vector4 total = source[(y * width) + x];
        float totalWeight = 1;
        for (int tap = 1; tap <= halfTapCount; tap++)
        {
            weight *= multiplier;
            multiplier *= ratioStep;
            float offsetX = tap * stepX;
            float offsetY = tap * stepY;
            total += (Sample(
                source, width, height, x + offsetX, y + offsetY, edgeMode) +
                Sample(
                    source, width, height, x - offsetX, y - offsetY, edgeMode)) *
                weight;
            totalWeight += weight * 2;
        }
        return total / totalWeight;
    }

    internal static Vector4 SampleDisk(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        PrismNeighborhoodPass pass,
        int edgeMode)
    {
        int count = Math.Max(1, pass.SampleCount);
        Vector4 total = Sample(
            source,
            width,
            height,
            x,
            y,
            edgeMode);
        for (int index = 1; index < count; index++)
        {
            float fraction = (float)index / Math.Max(count - 1, 1);
            float angle = index * 2.39996323f;
            total += Sample(
                source,
                width,
                height,
                x + (
                    MathF.Cos(angle) *
                    MathF.Sqrt(fraction) *
                    pass.RadiusX),
                y + (
                    MathF.Sin(angle) *
                    MathF.Sqrt(fraction) *
                    pass.RadiusY),
                edgeMode);
        }
        return total / count;
    }

    internal static Vector4 SampleOptimizedBilinearGaussian(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        PrismNeighborhoodPass pass,
        int edgeMode)
    {
        int halfTapCount = Math.Max(0, (pass.SampleCount - 1) / 2);
        if (halfTapCount == 0)
        {
            return source[(y * width) + x];
        }

        float centerWeight = 1;
        Vector4 total = source[(y * width) + x] * centerWeight;
        float totalWeight = centerWeight;
        float stepX = pass.RadiusX / halfTapCount;
        float stepY = pass.RadiusY / halfTapCount;
        for (int firstTap = 1;
            firstTap <= halfTapCount;
            firstTap += 2)
        {
            int secondTap = firstTap + 1;
            float firstPosition = (float)firstTap / halfTapCount;
            float firstWeight = MathF.Exp(
                -3.125f * firstPosition * firstPosition);
            float secondWeight = 0;
            if (secondTap <= halfTapCount)
            {
                float secondPosition =
                    (float)secondTap / halfTapCount;
                secondWeight = MathF.Exp(
                    -3.125f * secondPosition * secondPosition);
            }

            float pairWeight = firstWeight + secondWeight;
            float pairOffset = firstTap +
                (secondWeight / MathF.Max(pairWeight, 0.000001f));
            float offsetX = pairOffset * stepX;
            float offsetY = pairOffset * stepY;
            total += (
                SampleBilinear(
                    source,
                    width,
                    height,
                    x + offsetX,
                    y + offsetY,
                    edgeMode) +
                SampleBilinear(
                    source,
                    width,
                    height,
                    x - offsetX,
                    y - offsetY,
                    edgeMode)) * pairWeight;
            totalWeight += pairWeight * 2;
        }
        return total / MathF.Max(totalWeight, 0.000001f);
    }

    internal static Vector4 SampleSmartBlur(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = source[(y * width) + x];
        Vector3 centerStraight = Unpremultiply(center);
        float radius = plan.Options0.X;
        float rangeSigma = plan.Options0.Y;
        float spatialSigma = MathF.Max(radius / 3f, 0.000001f);
        float inverseSpatialVariance = 0.5f / (spatialSigma * spatialSigma);
        float inverseRangeVariance = rangeSigma > 0
            ? 0.5f / (rangeSigma * rangeSigma)
            : 0;
        int diameter = Math.Max(1, pass.SampleCount);
        int half = diameter / 2;
        float step = half > 0 ? radius / half : 0;
        Vector4 total = Vector4.Zero;
        float totalWeight = 0;
        for (int offsetY = -half; offsetY <= half; offsetY++)
        {
            for (int offsetX = -half; offsetX <= half; offsetX++)
            {
                float sampleX = offsetX * step;
                float sampleY = offsetY * step;
                float distanceSquared =
                    (sampleX * sampleX) + (sampleY * sampleY);
                if (distanceSquared > radius * radius)
                {
                    continue;
                }
                Vector4 sample = Sample(
                    source,
                    width,
                    height,
                    x + sampleX,
                    y + sampleY,
                    (int)plan.Options1.X);
                Vector3 colorDelta = Unpremultiply(sample) - centerStraight;
                float rangeDistanceSquared = colorDelta.LengthSquared() / 3f;
                float rangeWeight = rangeSigma > 0
                    ? MathF.Exp(-rangeDistanceSquared * inverseRangeVariance)
                    : rangeDistanceSquared <= 0.0000001f ? 1 : 0;
                float spatialWeight = MathF.Exp(
                    -distanceSquared * inverseSpatialVariance);
                float weight = spatialWeight * rangeWeight;
                total += sample * weight;
                totalWeight += weight;
            }
        }

        Vector4 blurred = total / MathF.Max(totalWeight, 0.000001f);
        int mode = (int)plan.Options0.W;
        if (mode == 0)
        {
            return blurred;
        }

        float edge = Math.Clamp(
            Vector3.Distance(centerStraight, Unpremultiply(blurred)),
            0,
            1);
        Vector4 edgeColor = new(new Vector3(edge * center.W), center.W);
        return mode == 1
            ? edgeColor
            : Vector4.Lerp(center, edgeColor, edge);
    }

    internal static Vector4 SampleSurfaceBilateral(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = source[(y * width) + x];
        float centerLuminance = Vector3.Dot(
            Unpremultiply(center),
            LuminanceWeights);
        float radius = plan.Options0.X;
        float rangeSigma = plan.Options0.Y;
        float spatialSigma = MathF.Max(radius / 3f, 0.000001f);
        float inverseSpatialVariance = 0.5f / (spatialSigma * spatialSigma);
        float inverseRangeVariance = rangeSigma > 0
            ? 0.5f / (rangeSigma * rangeSigma)
            : 0;
        int diameter = Math.Max(1, pass.SampleCount);
        int half = diameter / 2;
        float step = half > 0 ? radius / half : 0;
        Vector4 total = Vector4.Zero;
        float totalWeight = 0;
        for (int offsetY = -half; offsetY <= half; offsetY++)
        {
            for (int offsetX = -half; offsetX <= half; offsetX++)
            {
                float sampleX = offsetX * step;
                float sampleY = offsetY * step;
                float distanceSquared =
                    (sampleX * sampleX) + (sampleY * sampleY);
                if (distanceSquared > radius * radius)
                {
                    continue;
                }
                Vector4 sample = Sample(
                    source,
                    width,
                    height,
                    x + sampleX,
                    y + sampleY,
                    (int)plan.Options1.X);
                float rangeDistance = Vector3.Dot(
                    Unpremultiply(sample),
                    LuminanceWeights) - centerLuminance;
                float rangeDistanceSquared = rangeDistance * rangeDistance;
                float rangeWeight = rangeSigma > 0
                    ? MathF.Exp(-rangeDistanceSquared * inverseRangeVariance)
                    : rangeDistanceSquared <= 0.0000001f ? 1 : 0;
                float spatialWeight = MathF.Exp(
                    -distanceSquared * inverseSpatialVariance);
                float weight = spatialWeight * rangeWeight;
                total += sample * weight;
                totalWeight += weight;
            }
        }
        return total / MathF.Max(totalWeight, 0.000001f);
    }

    internal static Vector4 Neighborhood3x3(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 total = Vector4.Zero;
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                total += Sample(
                    source,
                    width,
                    height,
                    x + offsetX,
                    y + offsetY,
                    edgeMode: 0);
            }
        }
        return total / 9;
    }

}
