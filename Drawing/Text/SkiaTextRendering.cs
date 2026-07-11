using System.Runtime.CompilerServices;
using SkiaSharp;
using HarfBuzzSharp;
using HarfBuzzFont = HarfBuzzSharp.Font;

namespace Cerneala.Drawing.Text;

internal static class SkiaTextRendering
{
    private static readonly ConditionalWeakTable<SKTypeface, LineMetrics> LineMetricsCache = new();

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

    public static (float Baseline, float LineHeight) MeasureLine(SkiaFont font, float size)
    {
        LineMetrics metrics = LineMetricsCache.GetValue(font.Typeface, _ => ReadLineMetrics(font));
        return (metrics.BaselinePerEm * size, metrics.LineHeightPerEm * size);
    }

    private static LineMetrics ReadLineMetrics(SkiaFont font)
    {
        OpenTypeFontData data = OpenTypeFontData.Read(font);
        using Blob blob = data.CreatePinnedBlob();
        using Face face = new(blob, data.FaceIndex);
        using HarfBuzzFont harfBuzzFont = new(face);
        int unitsPerEm = Math.Max(1, face.UnitsPerEm);
        harfBuzzFont.SetScale(unitsPerEm, unitsPerEm);
        harfBuzzFont.SetFunctionsOpenType();

        if (harfBuzzFont.TryGetHorizontalFontExtents(out FontExtents extents))
        {
            float ascent = (float)extents.Ascender / unitsPerEm;
            float descent = (float)-extents.Descender / unitsPerEm;
            float lineGap = (float)extents.LineGap / unitsPerEm;
            return new LineMetrics(ascent + (lineGap * 0.5f), ascent + descent + lineGap);
        }

        using SKFont skFont = CreateFont(font, 1);
        SKFontMetrics metrics = skFont.Metrics;
        return new LineMetrics(
            -metrics.Ascent + (metrics.Leading * 0.5f),
            metrics.Descent - metrics.Ascent + metrics.Leading);
    }

    private sealed record LineMetrics(float BaselinePerEm, float LineHeightPerEm);
}
