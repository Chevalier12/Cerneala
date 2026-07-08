using Cerneala.Drawing;

namespace Cerneala.UI.Text;

public class TextRenderer
{
    private readonly FontResolver fontResolver;
    private readonly TextMeasurer textMeasurer;

    public TextRenderer()
        : this(FontResolver.Default, TextMeasurer.Default)
    {
    }

    public TextRenderer(FontResolver fontResolver, TextMeasurer textMeasurer)
    {
        this.fontResolver = fontResolver ?? throw new ArgumentNullException(nameof(fontResolver));
        this.textMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
    }

    public static TextRenderer Default { get; } = new();

    public virtual TextMeasureResult Render(
        DrawingContext drawingContext,
        string text,
        TextAspect aspect,
        float availableWidth,
        DrawPoint position,
        DrawColor color)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);
        ArgumentNullException.ThrowIfNull(text);

        TextMeasureResult measurement = textMeasurer.Measure(text, aspect, availableWidth);
        if (text.Length == 0)
        {
            return measurement;
        }

        ResolvedTextFont font = fontResolver.Resolve(aspect);
        float lineHeight = TextLineMetrics.MeasureLineHeight(aspect, font);
        for (int i = 0; i < measurement.Lines.Count; i++)
        {
            TextLine line = measurement.Lines[i];
            DrawPoint linePosition = new(position.X, position.Y + (i * lineHeight));
            drawingContext.DrawText(aspect.ToDrawTextRun(font, line.Text), linePosition, color);
        }

        return measurement;
    }
}
