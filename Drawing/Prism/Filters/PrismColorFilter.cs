using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismColorFilter
{
    private const float ContrastPivot = 0.18f;
    private const float D65X = 0.3127f;
    private const float D65Y = 0.3290f;
    private const float Epsilon = 0.000001f;
    private const float MaximumHalfValue = 65504;

    public static Vector3 Apply(
        Vector3 linearSrgb,
        float brightness,
        float contrast,
        float exposure,
        float saturation,
        float hueDegrees,
        float temperature,
        Vector4 tint,
        bool clamp)
    {
        bool neutral =
            brightness == 0 &&
            contrast == 1 &&
            exposure == 0 &&
            saturation == 1 &&
            hueDegrees == 0 &&
            temperature == 0 &&
            tint.W <= 0;
        if (neutral)
        {
            return clamp
                ? Vector3.Clamp(linearSrgb, Vector3.Zero, Vector3.One)
                : ClampExtended(linearSrgb);
        }

        float exposureScale = MathF.Pow(
            2,
            Math.Clamp(exposure, -16, 16));
        Vector3 graded = (linearSrgb * exposureScale) +
            new Vector3(brightness);
        graded = ((graded - new Vector3(ContrastPivot)) *
            Math.Clamp(contrast, -16, 16)) +
            new Vector3(ContrastPivot);

        if (temperature != 0 || tint.W > 0)
        {
            graded = ApplyCat16(graded, temperature, tint);
        }

        if (saturation != 1 || hueDegrees != 0)
        {
            Vector3 oklab = ToOklab(graded);
            float radians = MathF.IEEERemainder(hueDegrees, 360) *
                (MathF.PI / 180);
            float cosine = MathF.Cos(radians);
            float sine = MathF.Sin(radians);
            float chromaScale = Math.Clamp(saturation, -8, 8);
            float a = oklab.Y * chromaScale;
            float b = oklab.Z * chromaScale;
            oklab.Y = (a * cosine) - (b * sine);
            oklab.Z = (a * sine) + (b * cosine);
            graded = FromOklab(oklab);
        }

        return clamp
            ? CompressToGamut(graded)
            : ClampExtended(graded);
    }

    public static Vector3 ToOklab(Vector3 linearSrgb)
    {
        float l =
            (0.4122214708f * linearSrgb.X) +
            (0.5363325363f * linearSrgb.Y) +
            (0.0514459929f * linearSrgb.Z);
        float m =
            (0.2119034982f * linearSrgb.X) +
            (0.6806995451f * linearSrgb.Y) +
            (0.1073969566f * linearSrgb.Z);
        float s =
            (0.0883024619f * linearSrgb.X) +
            (0.2817188376f * linearSrgb.Y) +
            (0.6299787005f * linearSrgb.Z);
        float lRoot = MathF.Cbrt(l);
        float mRoot = MathF.Cbrt(m);
        float sRoot = MathF.Cbrt(s);
        return new Vector3(
            (0.2104542553f * lRoot) +
                (0.7936177850f * mRoot) -
                (0.0040720468f * sRoot),
            (1.9779984951f * lRoot) -
                (2.4285922050f * mRoot) +
                (0.4505937099f * sRoot),
            (0.0259040371f * lRoot) +
                (0.7827717662f * mRoot) -
                (0.8086757660f * sRoot));
    }

    private static Vector3 FromOklab(Vector3 oklab)
    {
        float lRoot = oklab.X +
            (0.3963377774f * oklab.Y) +
            (0.2158037573f * oklab.Z);
        float mRoot = oklab.X -
            (0.1055613458f * oklab.Y) -
            (0.0638541728f * oklab.Z);
        float sRoot = oklab.X -
            (0.0894841775f * oklab.Y) -
            (1.2914855480f * oklab.Z);
        float l = lRoot * lRoot * lRoot;
        float m = mRoot * mRoot * mRoot;
        float s = sRoot * sRoot * sRoot;
        return new Vector3(
            (4.0767416621f * l) -
                (3.3077115913f * m) +
                (0.2309699292f * s),
            (-1.2684380046f * l) +
                (2.6097574011f * m) -
                (0.3413193965f * s),
            (-0.0041960863f * l) -
                (0.7034186147f * m) +
                (1.7076147010f * s));
    }

    private static Vector3 ApplyCat16(
        Vector3 linearSrgb,
        float temperature,
        Vector4 tint)
    {
        Vector2 destinationXy = TemperatureWhitePoint(temperature);
        float tintAmount = Math.Clamp(tint.W, 0, 1);
        if (tintAmount > 0 &&
            TryChromaticity(new Vector3(tint.X, tint.Y, tint.Z), out Vector2 tintXy))
        {
            destinationXy = Vector2.Lerp(
                destinationXy,
                tintXy,
                tintAmount);
        }

        Vector3 sourceWhite = XyToXyz(new Vector2(D65X, D65Y));
        Vector3 destinationWhite = XyToXyz(destinationXy);
        Vector3 sourceLms = XyzToCat16(sourceWhite);
        Vector3 destinationLms = XyzToCat16(destinationWhite);
        Vector3 xyz = LinearSrgbToXyz(linearSrgb);
        Vector3 lms = XyzToCat16(xyz);
        lms *= new Vector3(
            destinationLms.X / sourceLms.X,
            destinationLms.Y / sourceLms.Y,
            destinationLms.Z / sourceLms.Z);
        return XyzToLinearSrgb(Cat16ToXyz(lms));
    }

    private static Vector2 TemperatureWhitePoint(float temperature)
    {
        float kelvin = Math.Clamp(
            6504 * MathF.Pow(2, -Math.Clamp(temperature, -2, 2)),
            1667,
            25000);
        float kelvin2 = kelvin * kelvin;
        float kelvin3 = kelvin2 * kelvin;
        float x = kelvin <= 4000
            ? (-0.2661239e9f / kelvin3) -
                (0.2343580e6f / kelvin2) +
                (0.8776956e3f / kelvin) +
                0.179910f
            : (-3.0258469e9f / kelvin3) +
                (2.1070379e6f / kelvin2) +
                (0.2226347e3f / kelvin) +
                0.240390f;
        float y = kelvin <= 2222
            ? (-1.1063814f * x * x * x) -
                (1.34811020f * x * x) +
                (2.18555832f * x) -
                0.20219683f
            : kelvin <= 4000
                ? (-0.9549476f * x * x * x) -
                    (1.37418593f * x * x) +
                    (2.09137015f * x) -
                    0.16748867f
                : (3.0817580f * x * x * x) -
                    (5.87338670f * x * x) +
                    (3.75112997f * x) -
                    0.37001483f;
        return new Vector2(x, y);
    }

    private static bool TryChromaticity(
        Vector3 linearSrgb,
        out Vector2 chromaticity)
    {
        Vector3 xyz = LinearSrgbToXyz(
            Vector3.Max(linearSrgb, Vector3.Zero));
        float sum = xyz.X + xyz.Y + xyz.Z;
        if (sum <= Epsilon)
        {
            chromaticity = default;
            return false;
        }

        chromaticity = new Vector2(
            Math.Clamp(xyz.X / sum, 0.01f, 0.85f),
            Math.Clamp(xyz.Y / sum, 0.01f, 0.85f));
        float excess = chromaticity.X + chromaticity.Y - 0.98f;
        if (excess > 0)
        {
            chromaticity -= new Vector2(excess * 0.5f);
        }
        return true;
    }

    private static Vector3 CompressToGamut(Vector3 linearSrgb)
    {
        if (IsInGamut(linearSrgb))
        {
            return linearSrgb;
        }

        Vector3 oklab = ToOklab(linearSrgb);
        oklab.X = Math.Clamp(oklab.X, 0, 1);
        float chroma = MathF.Sqrt(
            (oklab.Y * oklab.Y) +
            (oklab.Z * oklab.Z));
        if (chroma <= Epsilon)
        {
            return Vector3.Clamp(
                FromOklab(new Vector3(oklab.X, 0, 0)),
                Vector3.Zero,
                Vector3.One);
        }

        Vector2 direction = new(oklab.Y / chroma, oklab.Z / chroma);
        float low = 0;
        float high = chroma;
        for (int iteration = 0; iteration < 8; iteration++)
        {
            float candidate = (low + high) * 0.5f;
            Vector3 rgb = FromOklab(
                new Vector3(
                    oklab.X,
                    direction.X * candidate,
                    direction.Y * candidate));
            if (IsInGamut(rgb))
            {
                low = candidate;
            }
            else
            {
                high = candidate;
            }
        }

        return Vector3.Clamp(
            FromOklab(
                new Vector3(
                    oklab.X,
                    direction.X * low,
                    direction.Y * low)),
            Vector3.Zero,
            Vector3.One);
    }

    private static bool IsInGamut(Vector3 color) =>
        color.X >= 0 && color.X <= 1 &&
        color.Y >= 0 && color.Y <= 1 &&
        color.Z >= 0 && color.Z <= 1;

    private static Vector3 ClampExtended(Vector3 value) =>
        new(
            ClampExtended(value.X),
            ClampExtended(value.Y),
            ClampExtended(value.Z));

    private static float ClampExtended(float value) =>
        float.IsFinite(value)
            ? Math.Clamp(value, -MaximumHalfValue, MaximumHalfValue)
            : 0;

    private static Vector3 LinearSrgbToXyz(Vector3 rgb) =>
        new(
            (0.4124564f * rgb.X) + (0.3575761f * rgb.Y) + (0.1804375f * rgb.Z),
            (0.2126729f * rgb.X) + (0.7151522f * rgb.Y) + (0.0721750f * rgb.Z),
            (0.0193339f * rgb.X) + (0.1191920f * rgb.Y) + (0.9503041f * rgb.Z));

    private static Vector3 XyzToLinearSrgb(Vector3 xyz) =>
        new(
            (3.2404542f * xyz.X) - (1.5371385f * xyz.Y) - (0.4985314f * xyz.Z),
            (-0.9692660f * xyz.X) + (1.8760108f * xyz.Y) + (0.0415560f * xyz.Z),
            (0.0556434f * xyz.X) - (0.2040259f * xyz.Y) + (1.0572252f * xyz.Z));

    private static Vector3 XyzToCat16(Vector3 xyz) =>
        new(
            (0.401288f * xyz.X) + (0.650173f * xyz.Y) - (0.051461f * xyz.Z),
            (-0.250268f * xyz.X) + (1.204414f * xyz.Y) + (0.045854f * xyz.Z),
            (-0.002079f * xyz.X) + (0.048952f * xyz.Y) + (0.953127f * xyz.Z));

    private static Vector3 Cat16ToXyz(Vector3 lms) =>
        new(
            (1.86206786f * lms.X) - (1.01125463f * lms.Y) + (0.14918677f * lms.Z),
            (0.38752654f * lms.X) + (0.62144744f * lms.Y) - (0.00897398f * lms.Z),
            (-0.01584150f * lms.X) - (0.03412294f * lms.Y) + (1.04996444f * lms.Z));

    private static Vector3 XyToXyz(Vector2 xy) =>
        new(
            xy.X / MathF.Max(xy.Y, Epsilon),
            1,
            MathF.Max(0, 1 - xy.X - xy.Y) / MathF.Max(xy.Y, Epsilon));
}
