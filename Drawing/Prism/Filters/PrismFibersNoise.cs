namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismFibersNoise
{
    private const int OctaveCount = 5;
    private const float LongitudinalScale = 0.125f;
    private const float WarpScale = 0.35f;
    private const float GradientNormalization = 1.41421356237f;

    private static readonly float[] OctaveAngles =
    [
        0,
        0.067f,
        -0.049f,
        0.103f,
        -0.083f
    ];

    public static float Sample(
        float x,
        float y,
        float variance,
        float strength,
        uint seed)
    {
        float scale = 1 / MathF.Max(variance, 0.0001f);
        float transverse = x * scale;
        float longitudinal = y * scale * LongitudinalScale;
        float warp = Perlin(
            transverse * 0.25f,
            longitudinal * 0.5f,
            seed,
            107,
            227);
        transverse += warp * WarpScale;

        float total = 0;
        float amplitude = 1;
        float amplitudeSum = 0;
        float frequency = 1;
        for (int octave = 0; octave < OctaveCount; octave++)
        {
            float angle = OctaveAngles[octave];
            float cosine = MathF.Cos(angle);
            float sine = MathF.Sin(angle);
            float octaveX =
                ((cosine * transverse) -
                    (sine * longitudinal)) *
                frequency;
            float octaveY =
                ((sine * transverse) +
                    (cosine * longitudinal)) *
                frequency;
            total += Perlin(
                    octaveX,
                    octaveY,
                    seed,
                    octave * 19,
                    octave * 47) *
                amplitude;
            amplitudeSum += amplitude;
            frequency *= 2;
            amplitude *= 0.5f;
        }

        float normalized = Math.Clamp(
            (total / amplitudeSum) * GradientNormalization,
            -1,
            1);
        return Math.Clamp(
            0.5f +
                (normalized *
                    0.5f *
                    MathF.Max(strength, 0)),
            0,
            1);
    }

    private static float Perlin(
        float x,
        float y,
        uint seed,
        int seedLowOffset,
        int seedHighOffset)
    {
        int left = (int)MathF.Floor(x);
        int top = (int)MathF.Floor(y);
        float localX = x - left;
        float localY = y - top;
        float fadeX = Fade(localX);
        float fadeY = Fade(localY);

        float topLeft = Gradient(
            Hash(
                left,
                top,
                seed,
                seedLowOffset,
                seedHighOffset),
            localX,
            localY);
        float topRight = Gradient(
            Hash(
                left + 1,
                top,
                seed,
                seedLowOffset,
                seedHighOffset),
            localX - 1,
            localY);
        float bottomLeft = Gradient(
            Hash(
                left,
                top + 1,
                seed,
                seedLowOffset,
                seedHighOffset),
            localX,
            localY - 1);
        float bottomRight = Gradient(
            Hash(
                left + 1,
                top + 1,
                seed,
                seedLowOffset,
                seedHighOffset),
            localX - 1,
            localY - 1);
        return Lerp(
            Lerp(topLeft, topRight, fadeX),
            Lerp(bottomLeft, bottomRight, fadeX),
            fadeY);
    }

    private static float Gradient(
        float hash,
        float x,
        float y)
    {
        const float diagonal = 0.70710678118f;
        float gradientX = hash < 0.5f ? 1 : -1;
        float gradientY =
            Fraction(hash * 2) < 0.5f ? 1 : -1;
        return diagonal *
            ((gradientX * x) + (gradientY * y));
    }

    private static float Hash(
        int x,
        int y,
        uint seed,
        int seedLowOffset,
        int seedHighOffset)
    {
        float seedLow =
            (seed & 0xffffu) + seedLowOffset;
        float seedHigh =
            (seed >> 16) + seedHighOffset;
        float value =
            (x * 127.1f) +
            (y * 311.7f) +
            (seedLow * 0.1031f) +
            (seedHigh * 0.11369f);
        return Fraction(
            MathF.Sin(value) * 43758.5453123f);
    }

    private static float Fade(float value) =>
        value *
        value *
        value *
        ((value * ((value * 6) - 15)) + 10);

    private static float Lerp(
        float first,
        float second,
        float amount) =>
        first + ((second - first) * amount);

    private static float Fraction(float value) =>
        value - MathF.Floor(value);
}
