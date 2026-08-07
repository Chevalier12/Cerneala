using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismNotePaperFilter
{
    private const float MinimumAlpha = 0.000001f;
    private const int BlurRadius = 2;
    private static readonly Vector3 LightDirection =
        Vector3.Normalize(new Vector3(-0.42f, -0.56f, 0.72f));

    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        Vector4[] horizontal = BlurLuminance(
            source,
            width,
            height,
            horizontal: true);
        Vector4[] blurred = BlurLuminance(
            horizontal,
            width,
            height,
            horizontal: false);
        Vector4[] heightField = BuildHeightField(
            plan,
            blurred,
            width,
            height);
        return Composite(plan, source, heightField, width, height);
    }

    private static Vector4[] BlurLuminance(
        Vector4[] source,
        int width,
        int height,
        bool horizontal)
    {
        Vector4[] result = new Vector4[source.Length];
        const float Sigma = 1.15f;
        const float InverseTwoSigmaSquared = 1 / (2 * Sigma * Sigma);
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

                float weightedLuminance = 0;
                float coverageWeight = 0;
                for (int offset = -BlurRadius; offset <= BlurRadius; offset++)
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
                        -(offset * offset) * InverseTwoSigmaSquared) *
                        sample.W;
                    weightedLuminance += Luminance(sample) * weight;
                    coverageWeight += weight;
                }

                float luminance = coverageWeight <= MinimumAlpha
                    ? 0
                    : weightedLuminance / coverageWeight;
                result[index] = AssociatedGray(luminance, alpha);
            }
        }
        return result;
    }

    private static Vector4[] BuildHeightField(
        PrismCatalogFilterPlan plan,
        Vector4[] blurred,
        int width,
        int height)
    {
        float imageBalance = plan.Options5.X;
        float graininess = plan.Options5.Y;
        float threshold = float.Lerp(0.25f, 0.75f, imageBalance);
        float wavelength = float.Lerp(8, 1.6f, graininess);
        float grainAmplitude = graininess * 0.28f;
        float heightAmplitude = graininess * 0.16f;
        Vector4[] result = new Vector4[blurred.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                float alpha = blurred[index].W;
                if (alpha <= MinimumAlpha)
                {
                    result[index] = Vector4.Zero;
                    continue;
                }

                float grain = FractalNoise(x / wavelength, y / wavelength) - 0.5f;
                float tone = Luminance(blurred[index]) +
                    (grain * grainAmplitude);
                float surface = SmoothStep(
                    threshold - 0.12f,
                    threshold + 0.12f,
                    tone);
                float heightValue = Math.Clamp(
                    surface + (grain * heightAmplitude),
                    0,
                    1);
                result[index] = AssociatedGray(heightValue, alpha);
            }
        }
        return result;
    }

    private static Vector4[] Composite(
        PrismCatalogFilterPlan plan,
        Vector4[] original,
        Vector4[] heightField,
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
        float relief = plan.Options5.Z;
        float normalStrength = relief * 7;
        Vector4[] result = new Vector4[original.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                float alpha = original[index].W;
                if (alpha <= MinimumAlpha)
                {
                    result[index] = Vector4.Zero;
                    continue;
                }

                float heightValue = Height(heightField, width, height, x, y);
                float horizontal =
                    Height(heightField, width, height, x + 1, y) -
                    Height(heightField, width, height, x - 1, y);
                float vertical =
                    Height(heightField, width, height, x, y + 1) -
                    Height(heightField, width, height, x, y - 1);
                Vector3 normal = Vector3.Normalize(
                    new Vector3(
                        -horizontal * normalStrength,
                        -vertical * normalStrength,
                        1));
                float illumination = Vector3.Dot(normal, LightDirection);
                float shade =
                    (illumination - LightDirection.Z) *
                    relief *
                    1.8f;
                float surface = SmoothStep(0.28f, 0.72f, heightValue);
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

    private static float Height(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y) =>
        Luminance(
            source[
                (Math.Clamp(y, 0, height - 1) * width) +
                Math.Clamp(x, 0, width - 1)]);

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

    private static Vector4 AssociatedGray(float value, float alpha)
    {
        value = Math.Clamp(value, 0, 1) * alpha;
        return new Vector4(value, value, value, alpha);
    }

    private static float FractalNoise(float x, float y) =>
        (ValueNoise(x, y) * 0.625f) +
        (ValueNoise((x * 2) + 19.1f, (y * 2) - 7.7f) * 0.25f) +
        (ValueNoise((x * 4) - 3.4f, (y * 4) + 11.3f) * 0.125f);

    private static float ValueNoise(float x, float y)
    {
        int cellX = (int)MathF.Floor(x);
        int cellY = (int)MathF.Floor(y);
        float horizontal = SmoothCurve(x - cellX);
        float vertical = SmoothCurve(y - cellY);
        float top = float.Lerp(
            Hash(cellX, cellY),
            Hash(cellX + 1, cellY),
            horizontal);
        float bottom = float.Lerp(
            Hash(cellX, cellY + 1),
            Hash(cellX + 1, cellY + 1),
            horizontal);
        return float.Lerp(top, bottom, vertical);
    }

    private static float Hash(int x, int y)
    {
        uint value = unchecked((uint)x) * 0x8da6b343u;
        value ^= unchecked((uint)y) * 0xd8163841u;
        value ^= value >> 13;
        value *= 0xcb1ab31fu;
        value ^= value >> 16;
        return (value & 0x00ffffffu) / 16777215f;
    }

    private static float SmoothCurve(float value) =>
        value * value * (3 - (2 * value));

    private static float SmoothStep(float edge0, float edge1, float value) =>
        SmoothCurve(Math.Clamp((value - edge0) / (edge1 - edge0), 0, 1));
}
