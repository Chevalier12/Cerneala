using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismOkhsl
{
    private const float Tau = MathF.Tau;
    private const float ToeK1 = 0.206f;
    private const float ToeK2 = 0.03f;
    private const float ToeK3 =
        (1 + ToeK1) / (1 + ToeK2);
    private const float MaximumSearchChroma = 0.5f;
    private const float AchromaticEpsilon = 0.000001f;

    public static Vector3 FromLinearSrgb(Vector3 color)
    {
        Vector3 lab = LinearSrgbToOklab(
            Vector3.Clamp(color, Vector3.Zero, Vector3.One));
        float chroma = MathF.Sqrt(
            (lab.Y * lab.Y) + (lab.Z * lab.Z));
        float hue = chroma <= AchromaticEpsilon
            ? 0
            : Repeat(MathF.Atan2(lab.Z, lab.Y) / Tau);
        float maximumChroma = chroma <= AchromaticEpsilon
            ? 0
            : MaximumChroma(lab.X, hue);
        return new Vector3(
            hue,
            maximumChroma <= AchromaticEpsilon
                ? 0
                : Math.Clamp(chroma / maximumChroma, 0, 1),
            Toe(lab.X));
    }

    public static Vector3 ToLinearSrgb(Vector3 hsl)
    {
        float hue = Repeat(hsl.X);
        float saturation = Math.Clamp(hsl.Y, 0, 1);
        float lightness = Math.Clamp(hsl.Z, 0, 1);
        float labLightness = ToeInverse(lightness);
        if (saturation <= AchromaticEpsilon ||
            labLightness <= AchromaticEpsilon ||
            labLightness >= 1 - AchromaticEpsilon)
        {
            return Vector3.Clamp(
                OklabToLinearSrgb(new Vector3(labLightness, 0, 0)),
                Vector3.Zero,
                Vector3.One);
        }

        float angle = hue * Tau;
        float chroma = saturation * MaximumChroma(
            labLightness,
            hue);
        Vector3 color = OklabToLinearSrgb(new Vector3(
            labLightness,
            chroma * MathF.Cos(angle),
            chroma * MathF.Sin(angle)));
        return Vector3.Clamp(color, Vector3.Zero, Vector3.One);
    }

    private static float MaximumChroma(
        float lightness,
        float hue)
    {
        float angle = hue * Tau;
        Vector2 direction = new(
            MathF.Cos(angle),
            MathF.Sin(angle));
        float minimum = 0;
        float maximum = MaximumSearchChroma;

        for (int iteration = 0; iteration < 10; iteration++)
        {
            float candidate = (minimum + maximum) * 0.5f;
            Vector3 rgb = OklabToLinearSrgb(new Vector3(
                lightness,
                candidate * direction.X,
                candidate * direction.Y));
            if (IsInSrgbGamut(rgb))
            {
                minimum = candidate;
            }
            else
            {
                maximum = candidate;
            }
        }

        return minimum;
    }

    private static bool IsInSrgbGamut(Vector3 color) =>
        color.X is >= 0 and <= 1 &&
        color.Y is >= 0 and <= 1 &&
        color.Z is >= 0 and <= 1;

    private static Vector3 LinearSrgbToOklab(Vector3 color)
    {
        Vector3 lms = new(
            (0.4122214708f * color.X) +
                (0.5363325363f * color.Y) +
                (0.0514459929f * color.Z),
            (0.2119034982f * color.X) +
                (0.6806995451f * color.Y) +
                (0.1073969566f * color.Z),
            (0.0883024619f * color.X) +
                (0.2817188376f * color.Y) +
                (0.6299787005f * color.Z));
        lms = new Vector3(
            MathF.Cbrt(MathF.Max(lms.X, 0)),
            MathF.Cbrt(MathF.Max(lms.Y, 0)),
            MathF.Cbrt(MathF.Max(lms.Z, 0)));
        return new Vector3(
            (0.2104542553f * lms.X) +
                (0.7936177850f * lms.Y) -
                (0.0040720468f * lms.Z),
            (1.9779984951f * lms.X) -
                (2.4285922050f * lms.Y) +
                (0.4505937099f * lms.Z),
            (0.0259040371f * lms.X) +
                (0.7827717662f * lms.Y) -
                (0.8086757660f * lms.Z));
    }

    private static Vector3 OklabToLinearSrgb(Vector3 lab)
    {
        float l = lab.X +
            (0.3963377774f * lab.Y) +
            (0.2158037573f * lab.Z);
        float m = lab.X -
            (0.1055613458f * lab.Y) -
            (0.0638541728f * lab.Z);
        float s = lab.X -
            (0.0894841775f * lab.Y) -
            (1.2914855480f * lab.Z);
        l *= l * l;
        m *= m * m;
        s *= s * s;
        return new Vector3(
            (4.0767416361f * l) -
                (3.3077115913f * m) +
                (0.2309699449f * s),
            (-1.2684380046f * l) +
                (2.6097574011f * m) -
                (0.3413193965f * s),
            (-0.0041960863f * l) -
                (0.7034186145f * m) +
                (1.7076147010f * s));
    }

    private static float Toe(float value)
    {
        float scaled = (ToeK3 * value) - ToeK1;
        return 0.5f * (scaled + MathF.Sqrt(
            (scaled * scaled) + (4 * ToeK2 * ToeK3 * value)));
    }

    private static float ToeInverse(float value) =>
        ((value * value) + (ToeK1 * value)) /
        (ToeK3 * (value + ToeK2));

    private static float Repeat(float value) =>
        value - MathF.Floor(value);
}
