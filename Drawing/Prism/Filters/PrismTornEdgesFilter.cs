using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;




internal static class PrismTornEdgesFilter
{
    private const float MinimumWeight = 0.000001f;

    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        float sigma = Math.Clamp(plan.Options5.X, 0.5f, 3.75f);
        float extendedSigma = Math.Clamp(
            plan.Options5.Y,
            sigma + 0.25f,
            4);
        int extendedRadius = Math.Clamp(
            (int)MathF.Round(plan.Options5.Z),
            1,
            12);
        int narrowRadius = Math.Clamp(
            (int)MathF.Ceiling(sigma * 3),
            1,
            extendedRadius);
        float threshold = Math.Clamp(plan.Options5.W, 0, 1);
        float sharpen = Math.Clamp(plan.Options6.X, 8, 48);
        float noiseAmplitude = Math.Clamp(plan.Options6.Y, 0, 0.2f);
        float noiseFrequency = Math.Clamp(plan.Options6.Z, 0.01f, 0.5f);
        float transitionWidth = Math.Clamp(plan.Options6.W, 0.05f, 0.25f);
        (Vector2[] narrow, Vector2[] extended) =
            PrismXDogLuminance.Build(
                source,
                width,
                height,
                sigma,
                narrowRadius,
                extendedSigma,
                extendedRadius);

        Vector3 foreground = Color(plan.Options2);
        Vector3 background = Color(plan.Options0);
        Vector4[] result = new Vector4[source.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                float alpha = Math.Clamp(source[index].W, 0, 1);
                if (alpha <= MinimumWeight)
                {
                    result[index] = Vector4.Zero;
                    continue;
                }

                float response =
                    ((sharpen + 1) *
                        PrismXDogLuminance.Resolve(narrow[index])) -
                    (sharpen *
                        PrismXDogLuminance.Resolve(extended[index]));
                float distance = MathF.Abs(response - threshold);
                float transition = Math.Clamp(
                    distance / transitionWidth,
                    0,
                    1);
                float transitionWeight =
                    1 - (transition * transition * (3 - (2 * transition)));
                float noise = Fbm(
                    (x + 0.5f) * noiseFrequency,
                    (y + 0.5f) * noiseFrequency);
                float perturbedThreshold = threshold +
                    (noise * noiseAmplitude * transitionWeight);
                Vector3 color = response < perturbedThreshold
                    ? foreground
                    : background;
                result[index] = new Vector4(color * alpha, alpha);
            }
        }

        return result;
    }

    private static Vector3 Color(Vector4 option) =>
        Vector3.Clamp(
            new Vector3(option.X, option.Y, option.Z),
            Vector3.Zero,
            Vector3.One);

    private static float Fbm(float x, float y)
    {
        float total = 0;
        float normalization = 0;
        float amplitude = 0.5f;
        for (int octave = 0; octave < 4; octave++)
        {
            total += ValueNoise(x, y) * amplitude;
            normalization += amplitude;
            x = (x * 2.03f) + 19.1f;
            y = (y * 2.03f) + 7.7f;
            amplitude *= 0.5f;
        }
        return total / normalization;
    }

    private static float ValueNoise(float x, float y)
    {
        int cellX = (int)MathF.Floor(x);
        int cellY = (int)MathF.Floor(y);
        float fractionX = x - cellX;
        float fractionY = y - cellY;
        float fadeX = Fade(fractionX);
        float fadeY = Fade(fractionY);
        float lower = float.Lerp(
            Hash(cellX, cellY),
            Hash(cellX + 1, cellY),
            fadeX);
        float upper = float.Lerp(
            Hash(cellX, cellY + 1),
            Hash(cellX + 1, cellY + 1),
            fadeX);
        return float.Lerp(lower, upper, fadeY);
    }

    private static float Fade(float value) =>
        value * value * value *
        ((value * ((value * 6) - 15)) + 10);

    private static float Hash(int x, int y)
    {
        uint hash = unchecked(
            ((uint)x * 0x8da6b343u) ^
            ((uint)y * 0xd8163841u) ^
            0xcb1ab31fu);
        hash ^= hash >> 16;
        hash *= 0x7feb352du;
        hash ^= hash >> 15;
        hash *= 0x846ca68bu;
        hash ^= hash >> 16;
        return (((hash & 0x00ffffffu) / 16777215f) * 2) - 1;
    }
}
