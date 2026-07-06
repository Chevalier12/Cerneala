using Cerneala.Drawing;
namespace Cerneala.Drawing.Text;

public sealed class TextShaper
{
    private readonly SkiaTextShaper skiaTextShaper = new();

    public static TextShaper Default { get; } = new();

    public bool TryShape(DrawTextRun textRun, out TextShapeResult result)
    {
        ArgumentNullException.ThrowIfNull(textRun);

        if (textRun.Font is not SkiaFont)
        {
            result = default;
            return false;
        }

        result = skiaTextShaper.Shape(textRun);
        return true;
    }

    public bool TryMeasureLineHeight(DrawTextRun textRun, out float lineHeight)
    {
        ArgumentNullException.ThrowIfNull(textRun);

        if (textRun.Font is not SkiaFont)
        {
            lineHeight = 0;
            return false;
        }

        RasterizedText rasterizedText = new SkiaTextRasterizer(skiaTextShaper).Rasterize(
            new DrawTextRun(textRun.Font, "Ag", textRun.Size),
            DrawColor.White);
        lineHeight = rasterizedText.Height;
        return true;
    }

    public bool TryMeasureCaretVerticalMetrics(DrawTextRun textRun, out TextCaretVerticalMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(textRun);

        if (textRun.Font is not SkiaFont font)
        {
            metrics = default;
            return false;
        }

        RasterizedText rasterizedText = new SkiaTextRasterizer(skiaTextShaper).Rasterize(
            new DrawTextRun(font, "Ag", textRun.Size),
            DrawColor.White);
        metrics = new TextCaretVerticalMetrics(0, rasterizedText.Height);
        return true;
    }
}
