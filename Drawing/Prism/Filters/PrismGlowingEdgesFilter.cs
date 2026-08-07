using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismGlowingEdgesFilter
{
    private static readonly Vector3 GlowColor = new(0.25f, 0.6f, 1);

    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        float edgeRadius = plan.Options3.X;
        float sigma = plan.Options3.Y;
        int gaussianRadius = (int)plan.Options3.Z;
        float haloMix = plan.Options3.W;
        float brightness = MathF.Max(
            0,
            plan.GetOption("EdgeBrightness").X);

        Vector2[] edge = new Vector2[source.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                float alpha = source[index].W;
                float magnitude = ScharrMagnitude(
                    source,
                    width,
                    height,
                    x,
                    y,
                    edgeRadius);
                edge[index] = new Vector2(
                    magnitude * alpha,
                    alpha);
            }
        }

        Vector2[] horizontal = GaussianBlur(
            edge,
            width,
            height,
            sigma,
            gaussianRadius,
            horizontal: true);
        Vector2[] bloom = GaussianBlur(
            horizontal,
            width,
            height,
            sigma,
            gaussianRadius,
            horizontal: false);

        Vector4[] result = new Vector4[source.Length];
        for (int index = 0; index < result.Length; index++)
        {
            float alpha = source[index].W;
            if (alpha <= 0)
            {
                result[index] = Vector4.Zero;
                continue;
            }

            float crisp = edge[index].X / alpha;
            float soft = bloom[index].Y <= 0.000001f
                ? 0
                : bloom[index].X / bloom[index].Y;
            float intensity = Math.Clamp(
                (crisp + (soft * haloMix)) * brightness * 0.25f,
                0,
                1);
            result[index] = new Vector4(
                GlowColor * intensity * alpha,
                alpha);
        }

        return result;
    }

    private static Vector2[] GaussianBlur(
        Vector2[] source,
        int width,
        int height,
        float sigma,
        int radius,
        bool horizontal)
    {
        Vector2[] result = new Vector2[source.Length];
        float denominator = MathF.Max(2 * sigma * sigma, 0.000001f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 total = Vector2.Zero;
                float totalWeight = 0;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    int sampleX = horizontal
                        ? Math.Clamp(x + offset, 0, width - 1)
                        : x;
                    int sampleY = horizontal
                        ? y
                        : Math.Clamp(y + offset, 0, height - 1);
                    float weight = MathF.Exp(
                        -(offset * offset) / denominator);
                    total += source[(sampleY * width) + sampleX] * weight;
                    totalWeight += weight;
                }

                result[(y * width) + x] =
                    total / MathF.Max(totalWeight, 0.000001f);
            }
        }

        return result;
    }

    private static float ScharrMagnitude(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        float radius)
    {

        float topLeft = Luminance(Sample(
            source,
            width,
            height,
            x - radius,
            y - radius));
        float top = Luminance(Sample(
            source,
            width,
            height,
            x,
            y - radius));
        float topRight = Luminance(Sample(
            source,
            width,
            height,
            x + radius,
            y - radius));
        float left = Luminance(Sample(
            source,
            width,
            height,
            x - radius,
            y));
        float right = Luminance(Sample(
            source,
            width,
            height,
            x + radius,
            y));
        float bottomLeft = Luminance(Sample(
            source,
            width,
            height,
            x - radius,
            y + radius));
        float bottom = Luminance(Sample(
            source,
            width,
            height,
            x,
            y + radius));
        float bottomRight = Luminance(Sample(
            source,
            width,
            height,
            x + radius,
            y + radius));
        float gradientX =
            (3 * (topRight - topLeft)) +
            (10 * (right - left)) +
            (3 * (bottomRight - bottomLeft));
        float gradientY =
            (3 * (bottomLeft - topLeft)) +
            (10 * (bottom - top)) +
            (3 * (bottomRight - topRight));
        return Math.Clamp(
            MathF.Sqrt(
                (gradientX * gradientX) +
                (gradientY * gradientY)) / 16,
            0,
            1);
    }

    private static Vector4 Sample(
        Vector4[] source,
        int width,
        int height,
        float x,
        float y)
    {
        int left = Math.Clamp((int)MathF.Floor(x), 0, width - 1);
        int top = Math.Clamp((int)MathF.Floor(y), 0, height - 1);
        int right = Math.Clamp(left + 1, 0, width - 1);
        int bottom = Math.Clamp(top + 1, 0, height - 1);
        float amountX = Math.Clamp(x - MathF.Floor(x), 0, 1);
        float amountY = Math.Clamp(y - MathF.Floor(y), 0, 1);
        Vector4 upper = Vector4.Lerp(
            source[(top * width) + left],
            source[(top * width) + right],
            amountX);
        Vector4 lower = Vector4.Lerp(
            source[(bottom * width) + left],
            source[(bottom * width) + right],
            amountX);
        return Vector4.Lerp(upper, lower, amountY);
    }

    private static float Luminance(Vector4 color)
    {
        if (color.W <= 0.000001f)
        {
            return 0;
        }

        Vector3 straight = new(color.X, color.Y, color.Z);
        straight /= color.W;
        straight = Vector3.Clamp(straight, Vector3.Zero, Vector3.One);
        return Vector3.Dot(
            straight,
            new Vector3(0.2126f, 0.7152f, 0.0722f));
    }
}
