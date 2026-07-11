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

        lineHeight = SkiaTextRendering.MeasureLine((SkiaFont)textRun.Font, textRun.Size).LineHeight;
        return true;
    }

    public bool TryMeasureBaseline(DrawTextRun textRun, out float baseline)
    {
        ArgumentNullException.ThrowIfNull(textRun);

        if (textRun.Font is not SkiaFont skiaFont)
        {
            baseline = 0;
            return false;
        }

        baseline = SkiaTextRendering.MeasureLine(skiaFont, textRun.Size).Baseline;
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

        metrics = new TextCaretVerticalMetrics(0, SkiaTextRendering.MeasureLine(font, textRun.Size).LineHeight);
        return true;
    }
}
