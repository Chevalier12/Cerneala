using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

internal static class ColorPickerColorMath
{
    public static Color FromHsv(float hue, float saturation, float value, byte alpha = 255)
    {
        float normalizedHue = NormalizeHue(hue);
        float normalizedSaturation = Math.Clamp(saturation, 0, 1);
        float normalizedValue = Math.Clamp(value, 0, 1);
        float chroma = normalizedValue * normalizedSaturation;
        float segment = normalizedHue / 60;
        float x = chroma * (1 - MathF.Abs((segment % 2) - 1));
        (float red, float green, float blue) = segment switch
        {
            < 1 => (chroma, x, 0f),
            < 2 => (x, chroma, 0f),
            < 3 => (0f, chroma, x),
            < 4 => (0f, x, chroma),
            < 5 => (x, 0f, chroma),
            _ => (chroma, 0f, x)
        };
        float match = normalizedValue - chroma;
        return new Color(
            ToByte(red + match),
            ToByte(green + match),
            ToByte(blue + match),
            alpha);
    }

    public static void ToHsv(Color color, out float hue, out float saturation, out float value)
    {
        float red = color.R / 255f;
        float green = color.G / 255f;
        float blue = color.B / 255f;
        float maximum = MathF.Max(red, MathF.Max(green, blue));
        float minimum = MathF.Min(red, MathF.Min(green, blue));
        float delta = maximum - minimum;

        hue = delta <= float.Epsilon
            ? 0
            : maximum == red
                ? 60 * (((green - blue) / delta) % 6)
                : maximum == green
                    ? 60 * (((blue - red) / delta) + 2)
                    : 60 * (((red - green) / delta) + 4);
        hue = NormalizeHue(hue);
        saturation = maximum <= float.Epsilon ? 0 : delta / maximum;
        value = maximum;
    }

    private static float NormalizeHue(float hue)
    {
        float normalized = hue % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }

    private static byte ToByte(float value)
    {
        return (byte)Math.Clamp(MathF.Round(value * 255), 0, 255);
    }
}
