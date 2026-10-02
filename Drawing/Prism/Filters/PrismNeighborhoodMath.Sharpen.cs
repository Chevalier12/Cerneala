using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;

namespace Cerneala.Drawing.Prism.Filters;

internal static partial class PrismNeighborhoodMath
{
    internal static Vector4 SampleRichardsonLucyPsf(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        bool correction)
    {
        Vector4 center = source[(y * width) + x];
        Vector3 fallback = correction
            ? Vector3.One
            : SharpenStraight(center, Vector3.Zero);
        Vector3 total = Vector3.Zero;
        float totalWeight = 0;
        int count = Math.Clamp(pass.SampleCount, 1, 17);
        float radius = MathF.Max(0, plan.Options0.Y);
        int remove = (int)plan.Options0.W;
        if (remove == 2)
        {
            float angle = plan.Options1.X;
            Vector2 direction = new(
                MathF.Cos(angle),
                -MathF.Sin(angle));
            for (int index = 0; index < count; index++)
            {
                float position = count <= 1
                    ? 0
                    : ((index / (count - 1f)) * 2) - 1;
                Accumulate(position * radius * direction, 1);
            }
        }
        else
        {
            int half = count / 2;
            float step = half == 0 ? 0 : radius / half;
            float sigma = MathF.Max(radius / 3f, 0.000001f);
            float inverseVariance = 0.5f / (sigma * sigma);
            for (int sampleY = -half; sampleY <= half; sampleY++)
            {
                for (int sampleX = -half; sampleX <= half; sampleX++)
                {
                    Vector2 offset = new(
                        sampleX * step,
                        sampleY * step);
                    float distanceSquared = offset.LengthSquared();
                    if (remove == 1 &&
                        distanceSquared > radius * radius)
                    {
                        continue;
                    }
                    float weight = remove == 0
                        ? MathF.Exp(-distanceSquared * inverseVariance)
                        : 1;
                    Accumulate(offset, weight);
                }
            }
        }

        Vector3 result = totalWeight > 0.000001f
            ? total / totalWeight
            : fallback;
        if (correction)
        {
            return new Vector4(result, 1);
        }
        return new Vector4(
            Vector3.Clamp(result, Vector3.Zero, Vector3.One) * center.W,
            center.W);

        void Accumulate(Vector2 offset, float weight)
        {
            Vector4 sample = SampleBilinear(
                source,
                width,
                height,
                x + offset.X,
                y + offset.Y,
                edgeMode: 0);
            Vector3 straight = correction
                ? new Vector3(sample.X, sample.Y, sample.Z)
                : SharpenStraight(sample, fallback);
            total += straight * weight;
            totalWeight += weight;
        }
    }

    internal static Vector4 RichardsonLucyRatio(
        Vector4 original,
        Vector4 blurred,
        float reduceNoise)
    {
        Vector3 observed = SharpenStraight(original, Vector3.Zero);
        Vector3 estimate = SharpenStraight(blurred, observed);
        Vector3 ratio = new(
            observed.X / MathF.Max(estimate.X, 1f / 4096),
            observed.Y / MathF.Max(estimate.Y, 1f / 4096),
            observed.Z / MathF.Max(estimate.Z, 1f / 4096));
        ratio = Vector3.Clamp(ratio, Vector3.Zero, new Vector3(16));
        float updateStrength =
            1 - Math.Clamp(reduceNoise, 0, 1);
        return new Vector4(
            Vector3.Lerp(Vector3.One, ratio, updateStrength),
            1);
    }

    internal static Vector4 RichardsonLucyUpdate(
        Vector4 estimate,
        Vector4 correction)
    {
        Vector3 straight = SharpenStraight(estimate, Vector3.Zero);
        Vector3 factor = Vector3.Clamp(
            new Vector3(correction.X, correction.Y, correction.Z),
            Vector3.Zero,
            new Vector3(16));
        straight = Vector3.Clamp(
            straight * factor,
            Vector3.Zero,
            Vector3.One);
        return new Vector4(straight * estimate.W, estimate.W);
    }

    internal static Vector4 SmartSharpenRecombine(
        PrismNeighborhoodPlan plan,
        Vector4[] original,
        int width,
        int height,
        int x,
        int y,
        Vector4 restored)
    {
        int index = (y * width) + x;
        Vector4 source = original[index];
        if (source.W <= 0.000001f)
        {
            return source;
        }

        Vector3 sourceStraight = SharpenStraight(source, Vector3.Zero);
        Vector3 restoredStraight =
            SharpenStraight(restored, sourceStraight);
        float shadowLuminance = LocalLuminance(
            original,
            width,
            height,
            x,
            y,
            plan.Options1.W);
        float highlightLuminance = LocalLuminance(
            original,
            width,
            height,
            x,
            y,
            plan.Options2.Z);
        float shadowWidth = Math.Clamp(plan.Options1.Z, 0, 1);
        float highlightWidth = Math.Clamp(plan.Options2.Y, 0, 1);
        float shadowProtection = shadowWidth <= 0
            ? 0
            : (1 - SmoothStep(
                0,
                shadowWidth,
                shadowLuminance)) *
                Math.Clamp(plan.Options1.Y, 0, 1);
        float highlightProtection = highlightWidth <= 0
            ? 0
            : SmoothStep(
                1 - highlightWidth,
                1,
                highlightLuminance) *
                Math.Clamp(plan.Options2.X, 0, 1);
        float strength =
            MathF.Max(0, plan.Options0.X) *
            (1 - MathF.Max(shadowProtection, highlightProtection));
        Vector3 straight = Vector3.Clamp(
            sourceStraight +
                ((restoredStraight - sourceStraight) * strength),
            Vector3.Zero,
            Vector3.One);
        return new Vector4(straight * source.W, source.W);
    }

    private static float LocalLuminance(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        float radius)
    {
        if (radius <= 0.000001f)
        {
            return Luminance(source[(y * width) + x]);
        }

        float total = Luminance(source[(y * width) + x]);
        const int count = 17;
        for (int index = 1; index < count; index++)
        {
            float fraction = index / (count - 1f);
            float angle = index * 2.39996323f;
            float distance = MathF.Sqrt(fraction) * radius;
            total += Luminance(SampleBilinear(
                source,
                width,
                height,
                x + (MathF.Cos(angle) * distance),
                y + (MathF.Sin(angle) * distance),
                edgeMode: 0));
        }
        return total / count;
    }

    internal static Vector4 ContrastAdaptiveSharpen(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        float amount)
    {
        Vector4 center = source[(y * width) + x];
        if (center.W <= 0.000001f)
        {
            return center;
        }

        Vector3 centerStraight = Vector3.Clamp(
            Unpremultiply(center),
            Vector3.Zero,
            Vector3.One);
        Vector3 north = SharpenStraight(
            Sample(source, width, height, x, y - 1, edgeMode: 0),
            centerStraight);
        Vector3 west = SharpenStraight(
            Sample(source, width, height, x - 1, y, edgeMode: 0),
            centerStraight);
        Vector3 east = SharpenStraight(
            Sample(source, width, height, x + 1, y, edgeMode: 0),
            centerStraight);
        Vector3 south = SharpenStraight(
            Sample(source, width, height, x, y + 1, edgeMode: 0),
            centerStraight);

        Vector3 straight = ContrastAdaptiveSharpenStraight(
            centerStraight,
            north,
            west,
            east,
            south,
            amount);
        return new Vector4(straight * center.W, center.W);
    }

    internal static Vector4 SobelGatedContrastAdaptiveSharpen(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        float amount,
        float threshold)
    {
        Vector4 center = source[(y * width) + x];
        if (center.W <= 0.000001f)
        {
            return center;
        }

        Vector3 centerStraight = Vector3.Clamp(
            Unpremultiply(center),
            Vector3.Zero,
            Vector3.One);
        Vector3 northWest = SharpenStraight(
            Sample(source, width, height, x - 1, y - 1, edgeMode: 0),
            centerStraight);
        Vector3 north = SharpenStraight(
            Sample(source, width, height, x, y - 1, edgeMode: 0),
            centerStraight);
        Vector3 northEast = SharpenStraight(
            Sample(source, width, height, x + 1, y - 1, edgeMode: 0),
            centerStraight);
        Vector3 west = SharpenStraight(
            Sample(source, width, height, x - 1, y, edgeMode: 0),
            centerStraight);
        Vector3 east = SharpenStraight(
            Sample(source, width, height, x + 1, y, edgeMode: 0),
            centerStraight);
        Vector3 southWest = SharpenStraight(
            Sample(source, width, height, x - 1, y + 1, edgeMode: 0),
            centerStraight);
        Vector3 south = SharpenStraight(
            Sample(source, width, height, x, y + 1, edgeMode: 0),
            centerStraight);
        Vector3 southEast = SharpenStraight(
            Sample(source, width, height, x + 1, y + 1, edgeMode: 0),
            centerStraight);

        float northWestLuma = Vector3.Dot(northWest, LuminanceWeights);
        float northLuma = Vector3.Dot(north, LuminanceWeights);
        float northEastLuma = Vector3.Dot(northEast, LuminanceWeights);
        float westLuma = Vector3.Dot(west, LuminanceWeights);
        float eastLuma = Vector3.Dot(east, LuminanceWeights);
        float southWestLuma = Vector3.Dot(southWest, LuminanceWeights);
        float southLuma = Vector3.Dot(south, LuminanceWeights);
        float southEastLuma = Vector3.Dot(southEast, LuminanceWeights);
        float gradientX =
            (northEastLuma + (2 * eastLuma) + southEastLuma) -
            (northWestLuma + (2 * westLuma) + southWestLuma);
        float gradientY =
            (southWestLuma + (2 * southLuma) + southEastLuma) -
            (northWestLuma + (2 * northLuma) + northEastLuma);
        float edgeMagnitude = Math.Clamp(
            MathF.Sqrt(
                (gradientX * gradientX) +
                (gradientY * gradientY)) * 0.25f,
            0,
            1);

        float edgeThreshold = Math.Clamp(threshold, 0, 1);
        float knee = MathF.Max(edgeThreshold * 0.5f, 1f / 255f);
        float kneeStart = MathF.Max(0, edgeThreshold - knee);
        float kneeEnd = MathF.Min(1, edgeThreshold + knee);
        float gate = Math.Clamp(
            (edgeMagnitude - kneeStart) / (kneeEnd - kneeStart),
            0,
            1);
        gate = gate * gate * (3 - (2 * gate));

        Vector3 sharpened = ContrastAdaptiveSharpenStraight(
            centerStraight,
            north,
            west,
            east,
            south,
            amount);
        Vector3 straight = Vector3.Lerp(centerStraight, sharpened, gate);
        return new Vector4(straight * center.W, center.W);
    }

    private static Vector3 ContrastAdaptiveSharpenStraight(
        Vector3 center,
        Vector3 north,
        Vector3 west,
        Vector3 east,
        Vector3 south,
        float amount)
    {
        Vector3 minimum = Vector3.Min(
            Vector3.Min(Vector3.Min(north, west), center),
            Vector3.Min(east, south));
        Vector3 maximum = Vector3.Max(
            Vector3.Max(Vector3.Max(north, west), center),
            Vector3.Max(east, south));
        Vector3 amplitude = new(
            SharpenAmplitude(minimum.X, maximum.X),
            SharpenAmplitude(minimum.Y, maximum.Y),
            SharpenAmplitude(minimum.Z, maximum.Z));
        float strength = Math.Clamp(amount, 0, 1);
        float peak = -strength / (8 - (3 * strength));
        Vector3 weight = amplitude * peak;
        Vector3 denominator = Vector3.One + (weight * 4);
        return Vector3.Clamp(
            Vector3.Divide(
                center +
                    ((north + west + east + south) * weight),
                denominator),
            Vector3.Zero,
            Vector3.One);
    }

    private static Vector3 SharpenStraight(
        Vector4 sample,
        Vector3 fallback) =>
        sample.W > 0.000001f
            ? Vector3.Clamp(
                Unpremultiply(sample),
                Vector3.Zero,
                Vector3.One)
            : fallback;

    internal static Vector4 BinomialHighBoost(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        float amount)
    {
        Vector4 center = source[(y * width) + x];
        if (center.W <= 0.000001f)
        {
            return center;
        }

        Vector3 centerStraight = SharpenStraight(center, Vector3.Zero);
        Vector3 blurred =
            SharpenStraight(
                Sample(source, width, height, x - 1, y - 1, edgeMode: 0),
                centerStraight) +
            (SharpenStraight(
                Sample(source, width, height, x, y - 1, edgeMode: 0),
                centerStraight) * 2) +
            SharpenStraight(
                Sample(source, width, height, x + 1, y - 1, edgeMode: 0),
                centerStraight) +
            (SharpenStraight(
                Sample(source, width, height, x - 1, y, edgeMode: 0),
                centerStraight) * 2) +
            (centerStraight * 4) +
            (SharpenStraight(
                Sample(source, width, height, x + 1, y, edgeMode: 0),
                centerStraight) * 2) +
            SharpenStraight(
                Sample(source, width, height, x - 1, y + 1, edgeMode: 0),
                centerStraight) +
            (SharpenStraight(
                Sample(source, width, height, x, y + 1, edgeMode: 0),
                centerStraight) * 2) +
            SharpenStraight(
                Sample(source, width, height, x + 1, y + 1, edgeMode: 0),
                centerStraight);
        blurred /= 16;
        float strength = Math.Clamp(amount, 0, 1) * 2;
        Vector3 straight = Vector3.Clamp(
            centerStraight + ((centerStraight - blurred) * strength),
            Vector3.Zero,
            Vector3.One);
        return new Vector4(straight * center.W, center.W);
    }

    private static float SharpenAmplitude(
        float minimum,
        float maximum)
    {
        if (maximum <= 0.000001f)
        {
            return 0;
        }

        float headroom = MathF.Min(minimum, 1 - maximum);
        return MathF.Sqrt(Math.Clamp(headroom / maximum, 0, 1));
    }

    internal static Vector4 UnsharpHighBoost(
        Vector4 original,
        Vector4 blurred,
        float amount,
        float threshold)
    {
        if (original.W <= 0.000001f)
        {
            return original;
        }

        Vector3 originalStraight = Vector3.Clamp(
            Unpremultiply(original),
            Vector3.Zero,
            Vector3.One);
        Vector3 detail =
            originalStraight - Unpremultiply(blurred);
        float difference = MathF.Abs(
            Vector3.Dot(detail, LuminanceWeights));
        float center = Math.Clamp(threshold, 0, 1);
        float knee = MathF.Max(center * 0.5f, 1f / 255f);
        float kneeStart = MathF.Max(0, center - knee);
        float kneeEnd = MathF.Min(1, center + knee);
        float gate = Math.Clamp(
            (difference - kneeStart) /
                MathF.Max(kneeEnd - kneeStart, 0.000001f),
            0,
            1);
        gate = gate * gate * (3 - (2 * gate));
        Vector3 straight = Vector3.Clamp(
            originalStraight + (detail * amount * gate),
            Vector3.Zero,
            Vector3.One);
        return new Vector4(
            straight * original.W,
            original.W);
    }

    internal static Vector4 HighPass(
        Vector4 center,
        Vector4 blurred) =>
        new(
            new Vector3(center.W * 0.5f) +
                new Vector3(center.X, center.Y, center.Z) -
                new Vector3(blurred.X, blurred.Y, blurred.Z),
            center.W);

}
