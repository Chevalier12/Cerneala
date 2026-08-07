using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal readonly record struct PrismFlowXDogAnalysis(
    float[] Luminance,
    float[] Alpha,
    Vector2[] Tangents,
    float[] Response);

internal static class PrismCharcoalFilter
{
    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        ReadOnlySpan<Vector4> source,
        int width,
        int height)
    {
        PrismFlowXDogAnalysis analysis = Analyze(
            source,
            width,
            height,
            Math.Clamp((int)plan.Options6.X, 2, 4),
            Math.Clamp((int)plan.Options6.Y, 1, 3),
            Math.Clamp(plan.Options5.X, 0.5f, 4),
            Math.Clamp(plan.Options5.Y, 0.75f, 6.4f),
            Math.Clamp((int)plan.Options5.Z, 2, 8),
            Math.Clamp(plan.Options6.Z, 0.9f, 1),
            Math.Clamp((int)plan.Options5.W, 3, 8));
        return Composite(
            plan,
            analysis.Luminance,
            analysis.Alpha,
            analysis.Response,
            width,
            height);
    }

    internal static PrismFlowXDogAnalysis Analyze(
        ReadOnlySpan<Vector4> source,
        int width,
        int height,
        int etfRadius,
        int refinements,
        float sigma,
        float extendedSigma,
        int normalRadius,
        float rho,
        int flowRadius)
    {
        int length = checked(width * height);
        float[] luminance = new float[length];
        float[] alpha = new float[length];
        for (int index = 0; index < length; index++)
        {
            Vector4 pixel = source[index];
            alpha[index] = Math.Clamp(pixel.W, 0, 1);
            if (pixel.W > 0.00001f)
            {
                Vector3 straight = new(pixel.X, pixel.Y, pixel.Z);
                straight /= pixel.W;
                luminance[index] = Math.Clamp(
                    Vector3.Dot(straight, new Vector3(0.2126f, 0.7152f, 0.0722f)),
                    0,
                    1);
            }
        }

        CreateInitialEtf(
            luminance,
            alpha,
            width,
            height,
            out Vector2[] tangents,
            out float[] magnitudes);

        for (int iteration = 0; iteration < refinements; iteration++)
        {
            tangents = RefineEtf(
                tangents,
                magnitudes,
                alpha,
                width,
                height,
                etfRadius);
        }

        float[] normalDog = ComputeNormalDog(
            luminance,
            tangents,
            width,
            height,
            sigma,
            extendedSigma,
            normalRadius,
            rho);
        float[] flowDog = IntegrateAlongFlow(
            normalDog,
            tangents,
            width,
            height,
            flowRadius);

        return new PrismFlowXDogAnalysis(
            luminance,
            alpha,
            tangents,
            flowDog);
    }

    private static void CreateInitialEtf(
        float[] luminance,
        float[] alpha,
        int width,
        int height,
        out Vector2[] tangents,
        out float[] magnitudes)
    {
        tangents = new Vector2[luminance.Length];
        magnitudes = new float[luminance.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float center = luminance[(y * width) + x];
                float topLeft = SampleOpaqueLuminance(
                    luminance, alpha, width, height, x - 1, y - 1, center);
                float top = SampleOpaqueLuminance(
                    luminance, alpha, width, height, x, y - 1, center);
                float topRight = SampleOpaqueLuminance(
                    luminance, alpha, width, height, x + 1, y - 1, center);
                float left = SampleOpaqueLuminance(
                    luminance, alpha, width, height, x - 1, y, center);
                float right = SampleOpaqueLuminance(
                    luminance, alpha, width, height, x + 1, y, center);
                float bottomLeft = SampleOpaqueLuminance(
                    luminance, alpha, width, height, x - 1, y + 1, center);
                float bottom = SampleOpaqueLuminance(
                    luminance, alpha, width, height, x, y + 1, center);
                float bottomRight = SampleOpaqueLuminance(
                    luminance, alpha, width, height, x + 1, y + 1, center);

                float gradientX =
                    -topLeft + topRight - (2 * left) + (2 * right) -
                    bottomLeft + bottomRight;
                float gradientY =
                    -topLeft - (2 * top) - topRight + bottomLeft +
                    (2 * bottom) + bottomRight;
                float magnitude = MathF.Sqrt(
                    (gradientX * gradientX) + (gradientY * gradientY));
                int index = (y * width) + x;
                tangents[index] = magnitude > 0.00001f
                    ? new Vector2(-gradientY, gradientX) / magnitude
                    : Vector2.UnitX;
                magnitudes[index] = Math.Clamp(magnitude * 0.25f, 0, 1);
            }
        }
    }

    private static Vector2[] RefineEtf(
        Vector2[] tangents,
        float[] magnitudes,
        float[] alpha,
        int width,
        int height,
        int radius)
    {
        Vector2[] output = new Vector2[tangents.Length];
        float spatialDenominator = MathF.Max(2 * radius * radius, 1);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int centerIndex = (y * width) + x;
                Vector2 center = tangents[centerIndex];
                Vector2 sum = center;
                float totalWeight = 1;
                for (int offsetY = -radius; offsetY <= radius; offsetY++)
                {
                    for (int offsetX = -radius; offsetX <= radius; offsetX++)
                    {
                        if (offsetX == 0 && offsetY == 0)
                        {
                            continue;
                        }

                        int sampleX = Math.Clamp(x + offsetX, 0, width - 1);
                        int sampleY = Math.Clamp(y + offsetY, 0, height - 1);
                        int sampleIndex = (sampleY * width) + sampleX;
                        Vector2 neighbor = tangents[sampleIndex];
                        float alignment = Vector2.Dot(center, neighbor);
                        float spatial = MathF.Exp(
                            -((offsetX * offsetX) + (offsetY * offsetY)) /
                            spatialDenominator);
                        float direction = MathF.Abs(alignment);
                        float magnitude = 0.5f *
                            (1 + MathF.Tanh(
                                (magnitudes[sampleIndex] - magnitudes[centerIndex]) * 4));
                        float coverage = MathF.Exp(
                            -MathF.Abs(alpha[sampleIndex] - alpha[centerIndex]) * 8);
                        float weight = spatial * direction * magnitude * coverage;
                        sum += neighbor * (alignment < 0 ? -weight : weight);
                        totalWeight += weight;
                    }
                }

                sum /= MathF.Max(totalWeight, 0.00001f);
                output[centerIndex] = sum.LengthSquared() > 0.000001f
                    ? Vector2.Normalize(sum)
                    : center;
            }
        }

        return output;
    }

    private static float[] ComputeNormalDog(
        float[] luminance,
        Vector2[] tangents,
        int width,
        int height,
        float sigma,
        float extendedSigma,
        int radius,
        float rho)
    {
        float[] output = new float[luminance.Length];
        float narrowDenominator = 2 * sigma * sigma;
        float broadDenominator = 2 * extendedSigma * extendedSigma;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                Vector2 tangent = tangents[index];
                Vector2 normal = new(-tangent.Y, tangent.X);
                float narrow = 0;
                float broad = 0;
                float narrowWeight = 0;
                float broadWeight = 0;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    float distanceSquared = offset * offset;
                    float narrowGaussian = MathF.Exp(
                        -distanceSquared / narrowDenominator);
                    float broadGaussian = MathF.Exp(
                        -distanceSquared / broadDenominator);
                    float sample = SampleBilinear(
                        luminance,
                        width,
                        height,
                        x + (normal.X * offset),
                        y + (normal.Y * offset));
                    narrow += sample * narrowGaussian;
                    broad += sample * broadGaussian;
                    narrowWeight += narrowGaussian;
                    broadWeight += broadGaussian;
                }

                output[index] =
                    (narrow / MathF.Max(narrowWeight, 0.00001f)) -
                    (rho * broad / MathF.Max(broadWeight, 0.00001f));
            }
        }

        return output;
    }

    private static float[] IntegrateAlongFlow(
        float[] normalDog,
        Vector2[] tangents,
        int width,
        int height,
        int radius)
    {
        float[] output = new float[normalDog.Length];
        float sigma = MathF.Max(radius * 0.5f, 1);
        float denominator = 2 * sigma * sigma;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                float sum = normalDog[index];
                float totalWeight = 1;
                for (int direction = -1; direction <= 1; direction += 2)
                {
                    Vector2 position = new(x, y);
                    Vector2 previous = tangents[index] * direction;
                    for (int step = 1; step <= radius; step++)
                    {
                        position += previous;
                        Vector2 next = SampleBilinear(
                            tangents,
                            width,
                            height,
                            position.X,
                            position.Y);
                        if (Vector2.Dot(previous, next) < 0)
                        {
                            next = -next;
                        }
                        if (next.LengthSquared() > 0.000001f)
                        {
                            previous = Vector2.Normalize(next);
                        }

                        float weight = MathF.Exp(-(step * step) / denominator);
                        sum += SampleBilinear(
                            normalDog,
                            width,
                            height,
                            position.X,
                            position.Y) * weight;
                        totalWeight += weight;
                    }
                }

                output[index] = sum / totalWeight;
            }
        }

        return output;
    }

    private static Vector4[] Composite(
        PrismCatalogFilterPlan plan,
        float[] luminance,
        float[] alpha,
        float[] flowDog,
        int width,
        int height)
    {
        Vector4[] output = new Vector4[luminance.Length];
        float detail = Math.Clamp(plan.Options2.X / 10, 0, 1);
        float balance = Math.Clamp(plan.Options4.X / 100, 0, 1);
        float edgeThreshold = float.Lerp(0.045f, 0.012f, detail);
        float toneThreshold = float.Lerp(0.22f, 0.82f, balance);
        float toneStrength = float.Lerp(0.3f, 0.82f, balance);
        Vector3 background = Vector3.Clamp(
            new Vector3(plan.Options0.X, plan.Options0.Y, plan.Options0.Z),
            Vector3.Zero,
            Vector3.One);
        Vector3 foreground = Vector3.Clamp(
            new Vector3(plan.Options3.X, plan.Options3.Y, plan.Options3.Z),
            Vector3.Zero,
            Vector3.One);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                float line = SmoothStep(
                    edgeThreshold * 0.35f,
                    edgeThreshold,
                    MathF.Abs(flowDog[index]));
                float tone = 1 - SmoothStep(
                    toneThreshold - 0.24f,
                    toneThreshold + 0.24f,
                    luminance[index]);
                float grain = 0.72f +
                    (Hash(x + 17, y + 43) * 0.28f);
                float charcoal = Math.Clamp(
                    MathF.Max(line, tone * toneStrength * grain),
                    0,
                    1);
                Vector3 straight = Vector3.Lerp(
                    background,
                    foreground,
                    charcoal);
                output[index] = new Vector4(straight * alpha[index], alpha[index]);
            }
        }

        return output;
    }

    private static float SampleOpaqueLuminance(
        float[] luminance,
        float[] alpha,
        int width,
        int height,
        int x,
        int y,
        float fallback)
    {
        x = Math.Clamp(x, 0, width - 1);
        y = Math.Clamp(y, 0, height - 1);
        int index = (y * width) + x;
        return alpha[index] > 0.00001f ? luminance[index] : fallback;
    }

    private static float SampleBilinear(
        float[] values,
        int width,
        int height,
        float x,
        float y)
    {
        x = Math.Clamp(x, 0, width - 1);
        y = Math.Clamp(y, 0, height - 1);
        int x0 = (int)MathF.Floor(x);
        int y0 = (int)MathF.Floor(y);
        int x1 = Math.Min(x0 + 1, width - 1);
        int y1 = Math.Min(y0 + 1, height - 1);
        float horizontal = x - x0;
        float vertical = y - y0;
        float top = float.Lerp(
            values[(y0 * width) + x0],
            values[(y0 * width) + x1],
            horizontal);
        float bottom = float.Lerp(
            values[(y1 * width) + x0],
            values[(y1 * width) + x1],
            horizontal);
        return float.Lerp(top, bottom, vertical);
    }

    private static Vector2 SampleBilinear(
        Vector2[] values,
        int width,
        int height,
        float x,
        float y)
    {
        x = Math.Clamp(x, 0, width - 1);
        y = Math.Clamp(y, 0, height - 1);
        int x0 = (int)MathF.Floor(x);
        int y0 = (int)MathF.Floor(y);
        int x1 = Math.Min(x0 + 1, width - 1);
        int y1 = Math.Min(y0 + 1, height - 1);
        float horizontal = x - x0;
        float vertical = y - y0;
        Vector2 top = Vector2.Lerp(
            values[(y0 * width) + x0],
            values[(y0 * width) + x1],
            horizontal);
        Vector2 bottom = Vector2.Lerp(
            values[(y1 * width) + x0],
            values[(y1 * width) + x1],
            horizontal);
        return Vector2.Lerp(top, bottom, vertical);
    }

    private static float SmoothStep(float start, float end, float value)
    {
        float amount = Math.Clamp((value - start) / (end - start), 0, 1);
        return amount * amount * (3 - (2 * amount));
    }

    private static float Hash(int x, int y)
    {
        uint value = unchecked((uint)((x * 374761393) + (y * 668265263)));
        value = (value ^ (value >> 13)) * 1274126177u;
        value ^= value >> 16;
        return (value & 0x00ffffffu) / 16777215f;
    }
}
