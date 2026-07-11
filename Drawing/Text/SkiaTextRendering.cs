using SkiaSharp;

namespace Cerneala.Drawing.Text;

internal static class SkiaTextRendering
{
    public static SKFont CreateFont(SkiaFont font, float size)
    {
        return new SKFont(font.Typeface, size)
        {
            LinearMetrics = true,
            Hinting = SKFontHinting.Full,
            Subpixel = true,
            Edging = SKFontEdging.SubpixelAntialias,
            BaselineSnap = true
        };
    }

    public static (float Baseline, float LineHeight) MeasureLine(SKFont font)
    {
        SKFontMetrics metrics = font.Metrics;
        return (
            -metrics.Ascent + (metrics.Leading * 0.5f),
            metrics.Descent - metrics.Ascent + metrics.Leading);
    }
}
