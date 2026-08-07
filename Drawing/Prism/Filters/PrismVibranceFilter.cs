using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismVibranceFilter
{
    public static Vector3 Apply(
        Vector3 color,
        Vector4 parameters,
        Vector4 options)
    {
        float vibrance = parameters.X;
        float saturation = parameters.Y;
        if (vibrance == 0 && saturation == 0)
        {
            return color;
        }

        Vector3 perceptual = EncodeSrgb(
            Vector3.Max(Vector3.Zero, color));
        Vector3 grayTransform = new(
            options.X,
            options.Y,
            options.Z);

        if (vibrance > 0)
        {
            float maximum = MathF.Max(
                perceptual.X,
                MathF.Max(perceptual.Y, perceptual.Z));
            float minimum = MathF.Min(
                perceptual.X,
                MathF.Min(perceptual.Y, perceptual.Z));
            float chroma = maximum > 0
                ? (maximum - minimum) / maximum
                : 0;
            float chromaSquared = chroma * chroma;
            float vibranceSquared = vibrance * vibrance;
            float vibranceCubed = vibranceSquared * vibrance;
            float response =
                (3 * vibrance) +
                ((-4.5f * vibranceSquared -
                    1.5f * vibrance) * chroma) +
                ((4.5f * vibranceCubed -
                    0.5f * vibrance) * chromaSquared) +
                ((-4.5f * vibranceCubed +
                    4.5f * vibranceSquared -
                    vibrance) * chromaSquared * chroma);
            if (parameters.Z > 0.5f)
            {
                response *= 1 - (0.75f * SkinToneMask(perceptual));
            }

            perceptual = ScaleChroma(
                perceptual,
                grayTransform,
                1 + MathF.Max(0, response));
        }
        else
        {
            perceptual = ScaleChroma(
                perceptual,
                grayTransform,
                1 + vibrance);
        }

        perceptual = ScaleChroma(
            perceptual,
            grayTransform,
            1 + saturation);
        float gray = Vector3.Dot(perceptual, grayTransform);
        return DecodeSrgb(ClipChromaToUnit(perceptual, gray));
    }

    private static Vector3 ScaleChroma(
        Vector3 color,
        Vector3 grayTransform,
        float scale)
    {
        float gray = Vector3.Dot(color, grayTransform);
        return new Vector3(gray) +
            ((color - new Vector3(gray)) * scale);
    }

    private static float SkinToneMask(Vector3 color)
    {
        Vector3 hsv = RgbToHsv(color);
        float hueDistance = MathF.Abs(hsv.X - 0.075f);
        hueDistance = MathF.Min(hueDistance, 1 - hueDistance);
        float hueWeight = 1 - PrismAdjustmentMath.SmoothStep(
            0.035f,
            0.16f,
            hueDistance);
        float saturationWeight =
            PrismAdjustmentMath.SmoothStep(0.1f, 0.3f, hsv.Y) *
            (1 - PrismAdjustmentMath.SmoothStep(
                0.92f,
                1,
                hsv.Y));
        float valueWeight =
            PrismAdjustmentMath.SmoothStep(0.08f, 0.25f, hsv.Z) *
            (1 - PrismAdjustmentMath.SmoothStep(
                0.98f,
                1,
                hsv.Z));
        return hueWeight * saturationWeight * valueWeight;
    }

    private static Vector3 ClipChromaToUnit(
        Vector3 color,
        float gray)
    {
        gray = Math.Clamp(gray, 0, 1);
        float maximum = MathF.Max(
            color.X,
            MathF.Max(color.Y, color.Z));
        float minimum = MathF.Min(
            color.X,
            MathF.Min(color.Y, color.Z));
        float scale = 1;
        if (minimum < 0 && gray > minimum)
        {
            scale = MathF.Min(
                scale,
                gray / (gray - minimum));
        }
        if (maximum > 1 && maximum > gray)
        {
            scale = MathF.Min(
                scale,
                (1 - gray) / (maximum - gray));
        }
        return Vector3.Clamp(
            new Vector3(gray) +
                ((color - new Vector3(gray)) * scale),
            Vector3.Zero,
            Vector3.One);
    }

    private static Vector3 EncodeSrgb(Vector3 color) =>
        new(
            EncodeSrgb(color.X),
            EncodeSrgb(color.Y),
            EncodeSrgb(color.Z));

    private static float EncodeSrgb(float value) =>
        value <= 0.0031308f
            ? value * 12.92f
            : (1.055f * MathF.Pow(value, 1 / 2.4f)) - 0.055f;

    private static Vector3 DecodeSrgb(Vector3 color) =>
        new(
            DecodeSrgb(color.X),
            DecodeSrgb(color.Y),
            DecodeSrgb(color.Z));

    private static float DecodeSrgb(float value) =>
        value <= 0.04045f
            ? value / 12.92f
            : MathF.Pow((value + 0.055f) / 1.055f, 2.4f);

    private static Vector3 RgbToHsv(Vector3 color)
    {
        float maximum = MathF.Max(
            color.X,
            MathF.Max(color.Y, color.Z));
        float minimum = MathF.Min(
            color.X,
            MathF.Min(color.Y, color.Z));
        float delta = maximum - minimum;
        float hue = 0;
        if (delta > 0.000001f)
        {
            if (maximum == color.X)
            {
                hue = ((color.Y - color.Z) / delta) % 6;
            }
            else if (maximum == color.Y)
            {
                hue = ((color.Z - color.X) / delta) + 2;
            }
            else
            {
                hue = ((color.X - color.Y) / delta) + 4;
            }
            hue = PrismAdjustmentMath.Repeat(hue / 6);
        }
        float saturation = maximum <= 0 ? 0 : delta / maximum;
        return new Vector3(hue, saturation, maximum);
    }
}
