using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;

namespace Cerneala.Drawing.Prism.Filters;

internal static partial class PrismNeighborhoodMath
{
    internal static Vector4 Median3x3(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Span<Vector4> values = stackalloc Vector4[9];
        Span<float> ranks = stackalloc float[9];
        int index = 0;
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                Vector4 sample = Sample(
                    source,
                    width,
                    height,
                    x + offsetX,
                    y + offsetY,
                    edgeMode: 0);
                values[index] = sample;
                ranks[index] = Luminance(sample);
                index++;
            }
        }
        MedianSortNetwork(values, ranks);
        return values[4];
    }

    private static void MedianSortNetwork(
        Span<Vector4> values,
        Span<float> ranks)
    {
        CompareExchange(ref values[0], ref ranks[0], ref values[1], ref ranks[1]);
        CompareExchange(ref values[2], ref ranks[2], ref values[3], ref ranks[3]);
        CompareExchange(ref values[4], ref ranks[4], ref values[5], ref ranks[5]);
        CompareExchange(ref values[6], ref ranks[6], ref values[7], ref ranks[7]);

        CompareExchange(ref values[1], ref ranks[1], ref values[2], ref ranks[2]);
        CompareExchange(ref values[3], ref ranks[3], ref values[4], ref ranks[4]);
        CompareExchange(ref values[5], ref ranks[5], ref values[6], ref ranks[6]);
        CompareExchange(ref values[7], ref ranks[7], ref values[8], ref ranks[8]);

        CompareExchange(ref values[0], ref ranks[0], ref values[1], ref ranks[1]);
        CompareExchange(ref values[2], ref ranks[2], ref values[3], ref ranks[3]);
        CompareExchange(ref values[4], ref ranks[4], ref values[5], ref ranks[5]);
        CompareExchange(ref values[6], ref ranks[6], ref values[7], ref ranks[7]);

        CompareExchange(ref values[1], ref ranks[1], ref values[2], ref ranks[2]);
        CompareExchange(ref values[3], ref ranks[3], ref values[4], ref ranks[4]);
        CompareExchange(ref values[5], ref ranks[5], ref values[6], ref ranks[6]);
        CompareExchange(ref values[7], ref ranks[7], ref values[8], ref ranks[8]);

        CompareExchange(ref values[0], ref ranks[0], ref values[1], ref ranks[1]);
        CompareExchange(ref values[2], ref ranks[2], ref values[3], ref ranks[3]);
        CompareExchange(ref values[4], ref ranks[4], ref values[5], ref ranks[5]);
        CompareExchange(ref values[6], ref ranks[6], ref values[7], ref ranks[7]);

        CompareExchange(ref values[1], ref ranks[1], ref values[2], ref ranks[2]);
        CompareExchange(ref values[3], ref ranks[3], ref values[4], ref ranks[4]);
        CompareExchange(ref values[5], ref ranks[5], ref values[6], ref ranks[6]);
        CompareExchange(ref values[7], ref ranks[7], ref values[8], ref ranks[8]);

        CompareExchange(ref values[0], ref ranks[0], ref values[1], ref ranks[1]);
        CompareExchange(ref values[2], ref ranks[2], ref values[3], ref ranks[3]);
        CompareExchange(ref values[4], ref ranks[4], ref values[5], ref ranks[5]);
        CompareExchange(ref values[6], ref ranks[6], ref values[7], ref ranks[7]);

        CompareExchange(ref values[1], ref ranks[1], ref values[2], ref ranks[2]);
        CompareExchange(ref values[3], ref ranks[3], ref values[4], ref ranks[4]);
        CompareExchange(ref values[5], ref ranks[5], ref values[6], ref ranks[6]);
        CompareExchange(ref values[7], ref ranks[7], ref values[8], ref ranks[8]);

        CompareExchange(ref values[0], ref ranks[0], ref values[1], ref ranks[1]);
        CompareExchange(ref values[2], ref ranks[2], ref values[3], ref ranks[3]);
        CompareExchange(ref values[4], ref ranks[4], ref values[5], ref ranks[5]);
        CompareExchange(ref values[6], ref ranks[6], ref values[7], ref ranks[7]);
    }

    private static void CompareExchange(
        ref Vector4 left,
        ref float leftRank,
        ref Vector4 right,
        ref float rightRank)
    {
        if (rightRank >= leftRank)
        {
            return;
        }

        (left, right) = (right, left);
        (leftRank, rightRank) = (rightRank, leftRank);
    }

    internal static Vector4 AdaptiveThresholdedMedian(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        int maximumRadius,
        float threshold)
    {
        Vector4 center = source[(y * width) + x];
        float centerLuminance = Luminance(center);
        Vector4 fallback = center;
        Span<Vector4> values = stackalloc Vector4[49];
        int boundedRadius = Math.Clamp(maximumRadius, 1, 3);
        for (int radius = 1; radius <= boundedRadius; radius++)
        {
            int count = 0;
            for (int offsetY = -radius; offsetY <= radius; offsetY++)
            {
                for (int offsetX = -radius; offsetX <= radius; offsetX++)
                {
                    values[count++] = Sample(
                        source,
                        width,
                        height,
                        x + offsetX,
                        y + offsetY,
                        edgeMode: 0);
                }
            }

            Span<Vector4> window = values[..count];
            window.Sort(
                static (left, right) =>
                    Luminance(left).CompareTo(
                        Luminance(right)));
            Vector4 median = window[count / 2];
            fallback = median;
            float minimum = Luminance(window[0]);
            float medianLuminance = Luminance(median);
            float maximum = Luminance(window[^1]);
            if (medianLuminance <= minimum ||
                medianLuminance >= maximum)
            {
                continue;
            }

            bool centerIsImpulse =
                centerLuminance <= minimum ||
                centerLuminance >= maximum;
            return centerIsImpulse &&
                MathF.Abs(centerLuminance - medianLuminance) > threshold
                    ? PreserveCoverage(center, median)
                    : center;
        }

        return MathF.Abs(
            centerLuminance - Luminance(fallback)) > threshold
                ? PreserveCoverage(center, fallback)
                : center;
    }

    internal static Vector4[] ApplyProgressiveDespeckle(
        Vector4[] original,
        int width,
        int height,
        float threshold,
        float radius,
        int iterationCount)
    {
        if (radius <= 0 || iterationCount <= 0)
        {
            return (Vector4[])original.Clone();
        }

        bool[] impulses = new bool[original.Length];
        Vector4[] detection = (Vector4[])original.Clone();
        for (int iteration = 0; iteration < iterationCount; iteration++)
        {
            Vector4[] output = new Vector4[detection.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (y * width) + x;
                    Vector4 center = detection[index];
                    Vector4 median = DespeckleMedian(
                        detection,
                        impulses: null,
                        goodOnly: false,
                        width,
                        height,
                        x,
                        y,
                        radius,
                        out _);
                    bool detected = MathF.Abs(
                        Luminance(center) -
                        Luminance(median)) > threshold;
                    impulses[index] |= detected;
                    output[index] = detected
                        ? PreserveCoverage(center, median)
                        : center;
                }
            }
            detection = output;
        }

        Vector4[] current = (Vector4[])original.Clone();
        for (int iteration = 0; iteration < iterationCount; iteration++)
        {
            Vector4[] output = (Vector4[])current.Clone();
            bool[] remaining = (bool[])impulses.Clone();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (y * width) + x;
                    if (!impulses[index])
                    {
                        continue;
                    }

                    Vector4 median = DespeckleMedian(
                        current,
                        impulses,
                        goodOnly: true,
                        width,
                        height,
                        x,
                        y,
                        radius,
                        out bool found);
                    if (!found)
                    {
                        continue;
                    }

                    output[index] = PreserveCoverage(
                        original[index],
                        median);
                    remaining[index] = false;
                }
            }
            current = output;
            impulses = remaining;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                if (!impulses[index])
                {
                    continue;
                }

                Vector4 median = DespeckleMedian(
                    current,
                    impulses: null,
                    goodOnly: false,
                    width,
                    height,
                    x,
                    y,
                    radius,
                    out _);
                current[index] = PreserveCoverage(
                    original[index],
                    median);
            }
        }

        return current;
    }

    private static Vector4 DespeckleMedian(
        Vector4[] source,
        bool[]? impulses,
        bool goodOnly,
        int width,
        int height,
        int x,
        int y,
        float radius,
        out bool found)
    {
        Span<Vector4> samples =
            stackalloc Vector4[21];
        int count = 0;
        int kernelCount = radius <= 1.5f ? 9 : DespeckleKernel.Length;
        float scale = radius <= 1.5f ? 1 : radius / 2;
        for (int sampleIndex = 0;
            sampleIndex < kernelCount;
            sampleIndex++)
        {
            (int kernelX, int kernelY) =
                DespeckleKernel[sampleIndex];
            int offsetX = (int)MathF.Round(
                kernelX * scale,
                MidpointRounding.AwayFromZero);
            int offsetY = (int)MathF.Round(
                kernelY * scale,
                MidpointRounding.AwayFromZero);
            int sampleX = Math.Clamp(x + offsetX, 0, width - 1);
            int sampleY = Math.Clamp(y + offsetY, 0, height - 1);
            int sourceIndex = (sampleY * width) + sampleX;
            if (goodOnly && impulses![sourceIndex])
            {
                continue;
            }
            samples[count++] = source[sourceIndex];
        }

        found = count > 0;
        if (!found)
        {
            return source[(y * width) + x];
        }
        samples[..count].Sort(
            static (left, right) =>
                Luminance(left).CompareTo(
                    Luminance(right)));
        return samples[(count - 1) / 2];
    }

    private static Vector4 PreserveCoverage(
        Vector4 center,
        Vector4 replacement)
    {
        float alpha = Math.Clamp(center.W, 0, 1);
        Vector3 straight = Vector3.Clamp(
            Unpremultiply(replacement),
            Vector3.Zero,
            Vector3.One);
        return new Vector4(straight * alpha, alpha);
    }

    internal static Vector4 AddNoise(
        PrismNeighborhoodPlan plan,
        Vector4 center,
        int x,
        int y)
    {
        uint seed =
            ((uint)plan.Options1.X << 16) |
            (uint)plan.Options0.W;
        bool gaussian = plan.Options0.Y > 0.5f;
        float red = AddNoiseSample(x, y, seed, 0, gaussian);
        float green = plan.Options0.Z > 0.5f
            ? red
            : AddNoiseSample(x, y, seed, 1, gaussian);
        float blue = plan.Options0.Z > 0.5f
            ? red
            : AddNoiseSample(x, y, seed, 2, gaussian);
        Vector3 noise = new(red, green, blue);
        Vector3 straight = Vector3.Clamp(
            Unpremultiply(center) +
                (noise * plan.Options0.X),
            Vector3.Zero,
            Vector3.One);
        return new Vector4(
            straight * center.W,
            center.W);
    }

    internal static Vector4 ReplaceOutlier(
        Vector4 center,
        Vector4 median,
        float threshold)
    {
        float difference = MathF.Abs(
            Vector3.Dot(
                Unpremultiply(center) -
                    Unpremultiply(median),
                LuminanceWeights));
        return difference > threshold ? median : center;
    }

    internal static Vector4[] ApplyDomainTransformPass(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height)
    {
        Vector4[] output = new Vector4[source.Length];
        bool horizontal =
            pass.Kind == PrismNeighborhoodPassKind.Horizontal;
        int radius = (int)MathF.Max(
            pass.RadiusX,
            pass.RadiusY);
        int iteration = Math.Clamp(pass.SampleCount, 0, 2);
        float iterationSigma = iteration switch
        {
            0 => plan.Options2.X,
            1 => plan.Options2.Y,
            _ => plan.Options2.Z
        };
        float spatialSigma = MathF.Max(plan.Options1.Y, 0.000001f);
        float preserveDetails = Math.Clamp(plan.Options0.Y, 0, 1);
        float rangeSigma =
            0.025f + (0.175f * (1 - preserveDetails));
        float lumaMix = Math.Clamp(
            MathF.Max(plan.Options0.X, plan.Options0.W) / 3,
            0,
            1);
        float chromaMix = Math.Clamp(
            plan.Options0.Z / 3,
            0,
            1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                Vector4 center = source[index];
                if (center.W <= 0.000001f)
                {
                    output[index] = center;
                    continue;
                }

                Vector3 centerYCoCg = RgbToYCoCg(
                    Vector3.Clamp(
                        Unpremultiply(center),
                        Vector3.Zero,
                        Vector3.One));
                Vector3 total = centerYCoCg;
                float totalWeight = 1;

                for (int direction = -1;
                    direction <= 1;
                    direction += 2)
                {
                    Vector3 previous = centerYCoCg;
                    float domainDistance = 0;
                    for (int step = 1; step <= radius; step++)
                    {
                        int sampleX = horizontal
                            ? x + (direction * step)
                            : x;
                        int sampleY = horizontal
                            ? y
                            : y + (direction * step);
                        Vector4 sample = SampleClamped(
                            source,
                            width,
                            height,
                            sampleX,
                            sampleY);
                        Vector3 sampleYCoCg = RgbToYCoCg(
                            Vector3.Clamp(
                                Unpremultiply(sample),
                                Vector3.Zero,
                                Vector3.One));
                        domainDistance += 1 +
                            ((spatialSigma / rangeSigma) *
                                DomainColorDistance(
                                    sampleYCoCg,
                                    previous));
                        previous = sampleYCoCg;
                        float alphaWeight = Math.Clamp(
                            1 - (MathF.Abs(sample.W - center.W) * 8),
                            0,
                            1);
                        float weight = MathF.Exp(
                            -MathF.Sqrt(2) *
                            domainDistance /
                            MathF.Max(iterationSigma, 0.000001f)) *
                            alphaWeight;
                        total += sampleYCoCg * weight;
                        totalWeight += weight;
                    }
                }

                Vector3 filtered = total / totalWeight;
                Vector3 mixed = new(
                    centerYCoCg.X +
                        ((filtered.X - centerYCoCg.X) * lumaMix),
                    centerYCoCg.Y +
                        ((filtered.Y - centerYCoCg.Y) * chromaMix),
                    centerYCoCg.Z +
                        ((filtered.Z - centerYCoCg.Z) * chromaMix));
                Vector3 straight = Vector3.Clamp(
                    YCoCgToRgb(mixed),
                    Vector3.Zero,
                    Vector3.One);
                output[index] = new Vector4(
                    straight * center.W,
                    center.W);
            }
        }

        return output;
    }

    internal static Vector4[] ApplyJpegDeblockPass(
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height)
    {
        Vector4[] output = new Vector4[source.Length];
        bool horizontal =
            pass.Kind ==
            PrismNeighborhoodPassKind.JpegDeblockHorizontal;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                Vector4 center = source[index];
                int coordinate = horizontal ? x : y;
                int phase = coordinate % 8;
                if (center.W <= 0.000001f ||
                    (phase != 0 && phase != 7))
                {
                    output[index] = center;
                    continue;
                }

                int direction = phase == 7 ? 1 : -1;
                int acrossX = horizontal ? x + direction : x;
                int acrossY = horizontal ? y : y + direction;
                int innerX = horizontal ? x - direction : x;
                int innerY = horizontal ? y : y - direction;
                int acrossInnerX =
                    horizontal ? x + (direction * 2) : x;
                int acrossInnerY =
                    horizontal ? y : y + (direction * 2);
                Vector4 across = SampleClamped(
                    source,
                    width,
                    height,
                    acrossX,
                    acrossY);
                Vector4 inner = SampleClamped(
                    source,
                    width,
                    height,
                    innerX,
                    innerY);
                Vector4 acrossInner = SampleClamped(
                    source,
                    width,
                    height,
                    acrossInnerX,
                    acrossInnerY);

                Vector3 centerYCoCg = RgbToYCoCg(
                    Unpremultiply(center));
                Vector3 acrossYCoCg = RgbToYCoCg(
                    Unpremultiply(across));
                float boundary = DomainColorDistance(
                    centerYCoCg,
                    acrossYCoCg);
                float local = MathF.Max(
                    DomainColorDistance(
                        centerYCoCg,
                        RgbToYCoCg(Unpremultiply(inner))),
                    DomainColorDistance(
                        acrossYCoCg,
                        RgbToYCoCg(Unpremultiply(acrossInner))));
                float alphaWeight = Math.Clamp(
                    1 - (MathF.Abs(across.W - center.W) * 8),
                    0,
                    1);
                float gate =
                    Math.Clamp((boundary - local) * 12, 0, 1) *
                    (1 - SmoothStep(0.12f, 0.35f, boundary)) *
                    alphaWeight;
                Vector3 straight = Vector3.Clamp(
                    Vector3.Lerp(
                        Unpremultiply(center),
                        Unpremultiply(across),
                        0.35f * gate),
                    Vector3.Zero,
                    Vector3.One);
                output[index] = new Vector4(
                    straight * center.W,
                    center.W);
            }
        }
        return output;
    }

    internal static Vector4 RecombineReduceNoise(
        PrismNeighborhoodPlan plan,
        Vector4 original,
        Vector4 filtered)
    {
        if (original.W <= 0.000001f)
        {
            return original;
        }

        Vector3 originalYCoCg = RgbToYCoCg(
            Vector3.Clamp(
                Unpremultiply(original),
                Vector3.Zero,
                Vector3.One));
        Vector3 filteredYCoCg = RgbToYCoCg(
            Vector3.Clamp(
                Unpremultiply(filtered),
                Vector3.Zero,
                Vector3.One));
        float strength = Math.Clamp(plan.Options0.X, 0, 1);
        float preserve = Math.Clamp(plan.Options0.Y, 0, 1);
        float colorNoise = Math.Clamp(plan.Options0.Z, 0, 1);
        float sharpen = Math.Clamp(plan.Options0.W, 0, 1);
        bool removeJpeg = plan.Options1.X > 0.5f;
        float lumaMix = MathF.Max(
            strength,
            removeJpeg ? 0.65f : 0);
        float chromaMix = MathF.Max(
            colorNoise,
            removeJpeg ? 0.5f : 0);
        float detail = originalYCoCg.X - filteredYCoCg.X;
        float outputY =
            originalYCoCg.X +
            ((filteredYCoCg.X - originalYCoCg.X) * lumaMix) +
            (detail * ((preserve * strength) + (sharpen * 0.5f)));
        Vector3 combined = new(
            outputY,
            originalYCoCg.Y +
                ((filteredYCoCg.Y - originalYCoCg.Y) * chromaMix),
            originalYCoCg.Z +
                ((filteredYCoCg.Z - originalYCoCg.Z) * chromaMix));
        Vector3 straight = Vector3.Clamp(
            YCoCgToRgb(combined),
            Vector3.Zero,
            Vector3.One);
        return new Vector4(
            straight * original.W,
            original.W);
    }

    private static Vector4 SampleClamped(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y) =>
        source[
            (Math.Clamp(y, 0, height - 1) * width) +
            Math.Clamp(x, 0, width - 1)];

    private static Vector3 RgbToYCoCg(Vector3 rgb)
    {
        float co = rgb.X - rgb.Z;
        float temporary = (rgb.X + rgb.Z) * 0.5f;
        float cg = rgb.Y - temporary;
        return new Vector3(
            temporary + (cg * 0.5f),
            co,
            cg);
    }

    private static Vector3 YCoCgToRgb(Vector3 color)
    {
        float temporary = color.X - (color.Z * 0.5f);
        float green = color.Z + temporary;
        float blue = temporary - (color.Y * 0.5f);
        return new Vector3(
            blue + color.Y,
            green,
            blue);
    }

    private static float DomainColorDistance(
        Vector3 first,
        Vector3 second) =>
        MathF.Abs(first.X - second.X) +
        (0.25f *
            (MathF.Abs(first.Y - second.Y) +
                MathF.Abs(first.Z - second.Z)));

    private static float AddNoiseSample(
        int x,
        int y,
        uint seed,
        uint channel,
        bool gaussian)
    {
        if (!gaussian)
        {
            return (AddNoiseUniform(x, y, seed, channel) * 2) - 1;
        }

        uint pair = channel >> 1;
        float first = MathF.Max(
            AddNoiseUniform(x, y, seed, pair * 2),
            1f / 4294967296f);
        float second = AddNoiseUniform(x, y, seed, (pair * 2) + 1);
        float radius = MathF.Sqrt(-2 * MathF.Log(first));
        float angle = 2 * MathF.PI * second;
        return radius * ((channel & 1) == 0
            ? MathF.Cos(angle)
            : MathF.Sin(angle));
    }

    private static float AddNoiseUniform(
        int x,
        int y,
        uint seed,
        uint channel)
    {
        uint input =
            unchecked((uint)x * 0x9e3779b9u) ^
            unchecked((uint)y * 0x85ebca6bu) ^
            (seed & 0xffffu) ^
            unchecked((seed >> 16) * 0x27d4eb2du) ^
            unchecked(channel * 0xc2b2ae35u);
        uint state = unchecked((input * 747796405u) + 2891336453u);
        uint word = unchecked(
            ((state >> (int)((state >> 28) + 4)) ^ state) *
            277803737u);
        uint value = (word >> 22) ^ word;
        return (float)((value + 0.5) / 4294967296.0);
    }

}
