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
        TextRunStyle style,
        float availableWidth,
        DrawPoint position,
        DrawColor color)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);
        ArgumentNullException.ThrowIfNull(text);

        TextMeasureResult measurement = textMeasurer.Measure(text, style, availableWidth);
        if (text.Length == 0)
        {
            return measurement;
        }

        ResolvedTextFont font = fontResolver.Resolve(style.FontFamily, style.FontSize * style.Scale);
        drawingContext.DrawText(style.ToDrawTextRun(font, text), position, style.Color);
        return measurement;
    }
}
