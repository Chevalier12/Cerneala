using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;
using static Cerneala.Drawing.Prism.Filters.PrismCatalogFilterMath;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismCatalogReliefMath
{
    internal static Vector4[] BasRelief(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        const float minimumWeight = 0.000001f;
        float smoothness = Math.Clamp(
            Option(plan, "Smoothness", 3),
            0,
            15);
        int radius = Math.Clamp(
            (int)MathF.Round(plan.Passes[0].RadiusX),
            1,
            8);
        float epsilon = 0.0025f * (1 + smoothness);
        float detail = Math.Clamp(
            Option(plan, "Detail", 13),
            0,
            64) * 0.25f;
        Vector2 lightDirection = BasReliefLightDirection(
            (int)MathF.Round(Option(plan, "LightDirection", 5)));
        Vector4 foreground = OptionVector(
            plan,
            "Foreground",
            new Vector4(0, 0, 0, 1));
        Vector4 background = OptionVector(
            plan,
            "Background",
            new Vector4(1, 1, 1, 1));
        (float[] alpha, float[] guidedLuminance, _) = GuidedFilter(
            source,
            width,
            height,
            radius,
            epsilon);
        Vector4[] result = new Vector4[source.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                float pixelAlpha = alpha[index];
                if (pixelAlpha <= minimumWeight)
                {
                    continue;
                }

                Vector2 gradient = GuidedScharrGradient(
                    guidedLuminance,
                    width,
                    height,
                    x,
                    y,
                    radius: 1);
                Vector3 normal = Vector3.Normalize(
                    new Vector3(
                        -gradient.X * detail,
                        -gradient.Y * detail,
                        1));
                float shade = Math.Clamp(
                    0.5f +
                        (0.5f * Vector2.Dot(
                            new Vector2(normal.X, normal.Y),
                            lightDirection)),
                    0,
                    1);
                result[index] = Associated(
                    Vector3.Lerp(
                        new Vector3(
                            foreground.X,
                            foreground.Y,
                            foreground.Z),
                        new Vector3(
                            background.X,
                            background.Y,
                            background.Z),
                        shade),
                    pixelAlpha);
            }
        }

        return result;
    }

    internal static (
        float[] Alpha,
        float[] GuidedLuminance,
        Vector3[] GuidedColor) GuidedFilter(
        Vector4[] source,
        int width,
        int height,
        int radius,
        float epsilon)
    {
        const float minimumWeight = 0.000001f;
        int pixelCount = checked(width * height);
        float[] alpha = new float[pixelCount];
        float[] luminance = new float[pixelCount];
        float[] weightedLuminance = new float[pixelCount];
        float[] weightedSquare = new float[pixelCount];
        for (int index = 0; index < pixelCount; index++)
        {
            Vector4 pixel = source[index];
            float pixelAlpha = Math.Clamp(pixel.W, 0, 1);
            float value = pixelAlpha <= minimumWeight
                ? 0
                : StraightLuminance(Unpremultiply(pixel));
            alpha[index] = pixelAlpha;
            luminance[index] = value;
            weightedLuminance[index] = value * pixelAlpha;
            weightedSquare[index] = value * value * pixelAlpha;
        }

        float[] meanAlpha = GuidedBoxBlur(
            alpha,
            width,
            height,
            radius);
        float[] meanWeightedLuminance = GuidedBoxBlur(
            weightedLuminance,
            width,
            height,
            radius);
        float[] meanWeightedSquare = GuidedBoxBlur(
            weightedSquare,
            width,
            height,
            radius);
        float[] weightedA = new float[pixelCount];
        float[] weightedB = new float[pixelCount];
        for (int index = 0; index < pixelCount; index++)
        {
            float weight = meanAlpha[index];
            if (weight <= minimumWeight || alpha[index] <= minimumWeight)
            {
                continue;
            }

            float mean = meanWeightedLuminance[index] / weight;
            float variance = MathF.Max(
                0,
                (meanWeightedSquare[index] / weight) -
                    (mean * mean));
            float a = variance / (variance + epsilon);
            float b = mean - (a * mean);
            weightedA[index] = a * alpha[index];
            weightedB[index] = b * alpha[index];
        }

        float[] meanWeightedA = GuidedBoxBlur(
            weightedA,
            width,
            height,
            radius);
        float[] meanWeightedB = GuidedBoxBlur(
            weightedB,
            width,
            height,
            radius);
        float[] guidedLuminance = new float[pixelCount];
        Vector3[] guidedColor = new Vector3[pixelCount];
        for (int index = 0; index < pixelCount; index++)
        {
            float pixelAlpha = alpha[index];
            if (pixelAlpha <= minimumWeight)
            {
                continue;
            }

            float weight = meanAlpha[index];
            float meanA = weight <= minimumWeight
                ? 0
                : meanWeightedA[index] / weight;
            float meanB = weight <= minimumWeight
                ? luminance[index]
                : meanWeightedB[index] / weight;
            float guided = Math.Clamp(
                (meanA * luminance[index]) + meanB,
                0,
                1);
            Vector3 straight = Vector3.Clamp(
                Unpremultiply(source[index]),
                Vector3.Zero,
                Vector3.One);
            guidedLuminance[index] = guided;
            guidedColor[index] = luminance[index] <= minimumWeight
                ? new Vector3(guided)
                : Vector3.Clamp(
                    straight * (guided / luminance[index]),
                    Vector3.Zero,
                    Vector3.One);
        }

        return (alpha, guidedLuminance, guidedColor);
    }

    private static float[] GuidedBoxBlur(
        float[] source,
        int width,
        int height,
        int radius)
    {
        float[] horizontal = new float[source.Length];
        float[] output = new float[source.Length];
        float inverseDiameter = 1f / ((2 * radius) + 1);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float sum = 0;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    int sampleX = Math.Clamp(x + offset, 0, width - 1);
                    sum += source[(y * width) + sampleX];
                }
                horizontal[(y * width) + x] =
                    sum * inverseDiameter;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float sum = 0;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    int sampleY = Math.Clamp(y + offset, 0, height - 1);
                    sum += horizontal[(sampleY * width) + x];
                }
                output[(y * width) + x] =
                    sum * inverseDiameter;
            }
        }

        return output;
    }

    internal static Vector2 GuidedScharrGradient(
        float[] luminance,
        int width,
        int height,
        int x,
        int y,
        int radius)
    {
        float topLeft = PosterEdgesSample(
            luminance, width, height, x - radius, y - radius);
        float top = PosterEdgesSample(
            luminance, width, height, x, y - radius);
        float topRight = PosterEdgesSample(
            luminance, width, height, x + radius, y - radius);
        float left = PosterEdgesSample(
            luminance, width, height, x - radius, y);
        float right = PosterEdgesSample(
            luminance, width, height, x + radius, y);
        float bottomLeft = PosterEdgesSample(
            luminance, width, height, x - radius, y + radius);
        float bottom = PosterEdgesSample(
            luminance, width, height, x, y + radius);
        float bottomRight = PosterEdgesSample(
            luminance, width, height, x + radius, y + radius);
        float horizontal =
            (3 * (topRight - topLeft)) +
            (10 * (right - left)) +
            (3 * (bottomRight - bottomLeft));
        float vertical =
            (3 * (bottomLeft - topLeft)) +
            (10 * (bottom - top)) +
            (3 * (bottomRight - topRight));
        return new Vector2(horizontal, vertical) / 16;
    }

    private static Vector2 BasReliefLightDirection(int code) =>
        code switch
        {
            0 => new Vector2(0, -1),
            1 => Vector2.Normalize(new Vector2(1, -1)),
            2 => new Vector2(1, 0),
            3 => Vector2.Normalize(new Vector2(1, 1)),
            4 => new Vector2(0, 1),
            5 => Vector2.Normalize(new Vector2(-1, 1)),
            6 => new Vector2(-1, 0),
            7 => Vector2.Normalize(new Vector2(-1, -1)),
            _ => Vector2.Normalize(new Vector2(-1, 1))
        };

    private static float PosterEdgesSample(
        float[] source,
        int width,
        int height,
        int x,
        int y) =>
        source[
            (Math.Clamp(y, 0, height - 1) * width) +
            Math.Clamp(x, 0, width - 1)];
}
