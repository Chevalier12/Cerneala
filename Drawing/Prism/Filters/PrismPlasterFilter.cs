using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismPlasterFilter
{
    private const float MinimumAlpha = 0.000001f;

    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        int radius = Math.Clamp(
            (int)MathF.Round(plan.Options5.Y),
            1,
            12);
        float[] luminance = GuidedLuminanceForTesting(
            source,
            width,
            height,
            radius,
            plan.Options5.Z);
        return Composite(plan, source, luminance, width, height);
    }

    internal static float[] GuidedLuminanceForTesting(
        Vector4[] source,
        int width,
        int height,
        int radius,
        float epsilon)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }
        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }
        if (source.Length != checked(width * height))
        {
            throw new ArgumentException(
                "The source pixel count does not match its dimensions.",
                nameof(source));
        }
        if (radius is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(radius));
        }
        if (!float.IsFinite(epsilon) || epsilon <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(epsilon));
        }

        Vector3[] moments = new Vector3[source.Length];
        float[] guide = new float[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            float alpha = Math.Clamp(source[index].W, 0, 1);
            float value = Luminance(source[index]);
            guide[index] = value;
            moments[index] = new Vector3(
                alpha * value,
                alpha * value * value,
                alpha);
        }

        Vector3[] meanMoments = BoxBlur(
            BoxBlur(moments, width, height, radius, horizontal: true),
            width,
            height,
            radius,
            horizontal: false);
        Vector2[] coefficients = new Vector2[source.Length];
        for (int index = 0; index < coefficients.Length; index++)
        {
            float coverage = meanMoments[index].Z;
            if (coverage <= MinimumAlpha)
            {
                continue;
            }

            float mean = meanMoments[index].X / coverage;
            float variance = MathF.Max(
                (meanMoments[index].Y / coverage) - (mean * mean),
                0);
            float a = variance / (variance + epsilon);
            coefficients[index] = new Vector2(
                a,
                mean - (a * mean));
        }

        Vector2[] meanCoefficients = BoxBlur(
            BoxBlur(coefficients, width, height, radius, horizontal: true),
            width,
            height,
            radius,
            horizontal: false);
        float[] result = new float[source.Length];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = Math.Clamp(
                (meanCoefficients[index].X * guide[index]) +
                    meanCoefficients[index].Y,
                0,
                1);
        }
        return result;
    }

    private static Vector4[] Composite(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        float[] luminance,
        int width,
        int height)
    {
        Vector4 foregroundOption = plan.GetOption("Foreground");
        Vector4 backgroundOption = plan.GetOption("Background");
        Vector3 foreground = Vector3.Clamp(
            new Vector3(
                foregroundOption.X,
                foregroundOption.Y,
                foregroundOption.Z),
            Vector3.Zero,
            Vector3.One);
        Vector3 background = Vector3.Clamp(
            new Vector3(
                backgroundOption.X,
                backgroundOption.Y,
                backgroundOption.Z),
            Vector3.Zero,
            Vector3.One);
        float threshold = float.Lerp(
            0.22f,
            0.78f,
            plan.Options5.X);
        float smoothness = Math.Clamp(
            plan.GetOption("Smoothness").X / 15,
            0,
            1);
        float transition = float.Lerp(0.08f, 0.18f, smoothness);
        float normalStrength = plan.Options5.W;
        Vector3 light = LightDirection((int)MathF.Round(plan.Options6.X));
        Vector4[] result = new Vector4[source.Length];
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

                float heightValue = 1 - luminance[index];
                float horizontal =
                    Height(luminance, width, height, x + 1, y) -
                    Height(luminance, width, height, x - 1, y);
                float vertical =
                    Height(luminance, width, height, x, y + 1) -
                    Height(luminance, width, height, x, y - 1);
                Vector3 normal = Vector3.Normalize(
                    new Vector3(
                        -horizontal * normalStrength,
                        -vertical * normalStrength,
                        1));
                float shade = (Vector3.Dot(normal, light) - light.Z) * 0.75f;
                float surface = SmoothStep(
                    threshold - transition,
                    threshold + transition,
                    heightValue);
                Vector3 color = Vector3.Clamp(
                    Vector3.Lerp(background, foreground, surface) +
                        new Vector3(shade),
                    Vector3.Zero,
                    Vector3.One);
                result[index] = new Vector4(color * alpha, alpha);
            }
        }
        return result;
    }

    private static Vector3[] BoxBlur(
        Vector3[] source,
        int width,
        int height,
        int radius,
        bool horizontal)
    {
        Vector3[] result = new Vector3[source.Length];
        int count = (radius * 2) + 1;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 sum = Vector3.Zero;
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
                    sum += source[(sampleY * width) + sampleX];
                }
                result[(y * width) + x] = sum / count;
            }
        }
        return result;
    }

    private static Vector2[] BoxBlur(
        Vector2[] source,
        int width,
        int height,
        int radius,
        bool horizontal)
    {
        Vector2[] result = new Vector2[source.Length];
        int count = (radius * 2) + 1;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 sum = Vector2.Zero;
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
                    sum += source[(sampleY * width) + sampleX];
                }
                result[(y * width) + x] = sum / count;
            }
        }
        return result;
    }

    private static Vector3 LightDirection(int code)
    {
        const float diagonal = 0.70710678f;
        Vector2 direction = code switch
        {
            0 => new Vector2(0, -1),
            1 => new Vector2(diagonal, -diagonal),
            2 => new Vector2(1, 0),
            3 => new Vector2(diagonal, diagonal),
            4 => new Vector2(0, 1),
            5 => new Vector2(-diagonal, diagonal),
            6 => new Vector2(-1, 0),
            _ => new Vector2(-diagonal, -diagonal)
        };
        return Vector3.Normalize(
            new Vector3(direction * 0.65f, 0.76f));
    }

    private static float Height(
        float[] luminance,
        int width,
        int height,
        int x,
        int y) =>
        1 - luminance[
            (Math.Clamp(y, 0, height - 1) * width) +
            Math.Clamp(x, 0, width - 1)];

    private static float Luminance(Vector4 color)
    {
        if (color.W <= MinimumAlpha)
        {
            return 0;
        }
        return Math.Clamp(
            ((color.X / color.W) * 0.2126f) +
            ((color.Y / color.W) * 0.7152f) +
            ((color.Z / color.W) * 0.0722f),
            0,
            1);
    }

    private static float SmoothStep(float edge0, float edge1, float value)
    {
        float t = Math.Clamp((value - edge0) / (edge1 - edge0), 0, 1);
        return t * t * (3 - (2 * t));
    }
}
