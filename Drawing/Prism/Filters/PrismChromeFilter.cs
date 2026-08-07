using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismChromeFilter
{
    private const float MinimumAlpha = 0.000001f;

    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        Vector4 settings = plan.Options2;
        int radius = Math.Clamp((int)MathF.Round(settings.Y), 1, 8);
        float sigma = Math.Clamp(settings.X, 0.5f, 4);
        Vector4[] horizontal = BlurLuminance(
            source,
            width,
            height,
            radius,
            sigma,
            horizontal: true);
        Vector4[] blurred = BlurLuminance(
            horizontal,
            width,
            height,
            radius,
            sigma,
            horizontal: false);
        Vector4[] result = new Vector4[source.Length];
        float detailGain = Math.Clamp(settings.Z, 1, 8.5f);
        float reflectionWidth = Math.Clamp(
            settings.W,
            0.035f,
            0.115f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                float alpha = source[index].W;
                if (alpha <= MinimumAlpha)
                {
                    result[index] = Vector4.Zero;
                    continue;
                }

                Vector2 gradient = ScharrGradient(
                    blurred,
                    width,
                    height,
                    x,
                    y);
                Vector3 normal = Vector3.Normalize(
                    new Vector3(
                        -gradient.X * detailGain,
                        -gradient.Y * detailGain,
                        1));
                Vector3 reflected = Vector3.Reflect(
                    -Vector3.UnitZ,
                    normal);
                float heightValue = StraightLuminance(blurred[index]);
                float environmentCoordinate = Math.Clamp(
                    0.5f +
                        (reflected.Y * 0.36f) +
                        (reflected.X * 0.1f) +
                        ((heightValue - 0.5f) * 0.08f),
                    0,
                    1);
                float chrome = ChromeRamp(
                    environmentCoordinate,
                    reflectionWidth);
                result[index] = new Vector4(
                    chrome * alpha,
                    chrome * alpha,
                    chrome * alpha,
                    alpha);
            }
        }

        return result;
    }

    private static Vector4[] BlurLuminance(
        Vector4[] source,
        int width,
        int height,
        int radius,
        float sigma,
        bool horizontal)
    {
        Vector4[] result = new Vector4[source.Length];
        float inverseTwoSigmaSquared = 1 / (2 * sigma * sigma);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int centerIndex = (y * width) + x;
                float centerAlpha = source[centerIndex].W;
                if (centerAlpha <= MinimumAlpha)
                {
                    result[centerIndex] = Vector4.Zero;
                    continue;
                }

                float weightedLuminance = 0;
                float coverageWeight = 0;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    int sampleX = Math.Clamp(
                        x + (horizontal ? offset : 0),
                        0,
                        width - 1);
                    int sampleY = Math.Clamp(
                        y + (horizontal ? 0 : offset),
                        0,
                        height - 1);
                    Vector4 sample = source[(sampleY * width) + sampleX];
                    float weight = MathF.Exp(
                        -(offset * offset) * inverseTwoSigmaSquared) *
                        sample.W;
                    weightedLuminance += StraightLuminance(sample) * weight;
                    coverageWeight += weight;
                }

                float luminance = coverageWeight <= MinimumAlpha
                    ? 0
                    : weightedLuminance / coverageWeight;
                result[centerIndex] = new Vector4(
                    luminance * centerAlpha,
                    luminance * centerAlpha,
                    luminance * centerAlpha,
                    centerAlpha);
            }
        }
        return result;
    }

    private static Vector2 ScharrGradient(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        float topLeft = Height(source, width, height, x - 1, y - 1);
        float top = Height(source, width, height, x, y - 1);
        float topRight = Height(source, width, height, x + 1, y - 1);
        float left = Height(source, width, height, x - 1, y);
        float right = Height(source, width, height, x + 1, y);
        float bottomLeft = Height(source, width, height, x - 1, y + 1);
        float bottom = Height(source, width, height, x, y + 1);
        float bottomRight = Height(source, width, height, x + 1, y + 1);
        return new Vector2(
            (3 * (topRight - topLeft) +
                10 * (right - left) +
                3 * (bottomRight - bottomLeft)) / 16,
            (3 * (bottomLeft - topLeft) +
                10 * (bottom - top) +
                3 * (bottomRight - topRight)) / 16);
    }

    private static float Height(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y) =>
        StraightLuminance(
            source[
                (Math.Clamp(y, 0, height - 1) * width) +
                Math.Clamp(x, 0, width - 1)]);

    private static float StraightLuminance(Vector4 color) =>
        color.W <= MinimumAlpha
            ? 0
            : color.X / color.W;

    private static float ChromeRamp(float value, float width)
    {
        float broadWidth = width * 1.5f;
        float narrowWidth = width * 0.65f;
        float chrome =
            0.045f +
            (0.52f * GaussianLobe(value, 0.12f, broadWidth)) +
            (0.92f * GaussianLobe(value, 0.34f, narrowWidth)) +
            (0.18f * GaussianLobe(value, 0.5f, broadWidth)) +
            (0.98f * GaussianLobe(value, 0.7f, narrowWidth)) +
            (0.62f * GaussianLobe(value, 0.92f, width)) -
            (0.28f * GaussianLobe(value, 0.58f, narrowWidth));
        return Math.Clamp(chrome, 0, 1);
    }

    private static float GaussianLobe(
        float value,
        float center,
        float width)
    {
        float normalized = (value - center) / MathF.Max(width, 0.0001f);
        return MathF.Exp(-(normalized * normalized));
    }
}
