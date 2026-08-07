using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismXDogLuminance
{
    private const float MinimumWeight = 0.000001f;

    public static (Vector2[] Narrow, Vector2[] Extended) Build(
        Vector4[] source,
        int width,
        int height,
        float narrowSigma,
        int narrowRadius,
        float extendedSigma,
        int extendedRadius)
    {
        Vector2[] luminance = BuildWeightedLuminance(source);
        return (
            GaussianBlur(
                luminance,
                width,
                height,
                narrowSigma,
                narrowRadius),
            GaussianBlur(
                luminance,
                width,
                height,
                extendedSigma,
                extendedRadius));
    }

    public static float Resolve(Vector2 weighted) =>
        weighted.Y <= MinimumWeight
            ? 0
            : weighted.X / weighted.Y;

    private static Vector2[] BuildWeightedLuminance(Vector4[] source)
    {
        Vector2[] result = new Vector2[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            float alpha = Math.Clamp(source[index].W, 0, 1);
            if (alpha <= MinimumWeight)
            {
                continue;
            }

            float luminance = Math.Clamp(
                ((source[index].X / alpha) * 0.2126f) +
                ((source[index].Y / alpha) * 0.7152f) +
                ((source[index].Z / alpha) * 0.0722f),
                0,
                1);
            result[index] = new Vector2(luminance * alpha, alpha);
        }
        return result;
    }

    private static Vector2[] GaussianBlur(
        Vector2[] source,
        int width,
        int height,
        float sigma,
        int radius)
    {
        Vector2[] horizontal = new Vector2[source.Length];
        Vector2[] result = new Vector2[source.Length];
        BlurAxis(source, horizontal, width, height, sigma, radius, true);
        BlurAxis(horizontal, result, width, height, sigma, radius, false);
        return result;
    }

    private static void BlurAxis(
        Vector2[] source,
        Vector2[] destination,
        int width,
        int height,
        float sigma,
        int radius,
        bool horizontal)
    {
        float inverseTwoSigmaSquared = 1 / (2 * sigma * sigma);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 total = Vector2.Zero;
                float totalWeight = 0;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    float weight = MathF.Exp(
                        -(offset * offset) * inverseTwoSigmaSquared);
                    int sampleX = horizontal
                        ? Math.Clamp(x + offset, 0, width - 1)
                        : x;
                    int sampleY = horizontal
                        ? y
                        : Math.Clamp(y + offset, 0, height - 1);
                    total += source[(sampleY * width) + sampleX] * weight;
                    totalWeight += weight;
                }
                destination[(y * width) + x] = total / totalWeight;
            }
        }
    }
}
