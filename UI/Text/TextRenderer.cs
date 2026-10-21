using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

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
        DrawPoint position)
    {
        return Render(
            drawingContext,
            text,
            aspect,
            new LayoutSize(availableWidth, float.PositiveInfinity),
            position);
    }

    public virtual TextMeasureResult Render(
        DrawingContext drawingContext,
        string text,
        TextAspect aspect,
        LayoutSize availableSize,
        DrawPoint position)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);
        ArgumentNullException.ThrowIfNull(text);

        TextMeasureResult measurement = textMeasurer.Measure(text, aspect, availableSize);
        Brush? foreground = aspect.Foreground;
        if (text.Length == 0 || foreground is null)
        {
            return measurement;
        }

        ResolvedTextFont font = fontResolver.Resolve(aspect);
        float lineHeight = TextLineMetrics.MeasureLineHeight(aspect, font);
        float baseline = TextLineMetrics.MeasureBaseline(aspect, font);
        for (int i = 0; i < measurement.Lines.Count; i++)
        {
            TextLine line = measurement.Lines[i];
            DrawPoint linePosition = new(position.X, position.Y + (i * lineHeight) + baseline);
            drawingContext.DrawText(aspect.ToDrawTextRun(font, line.Text), linePosition, foreground);
        }

        return measurement;
    }

    // Draws the lines of the paragraph that MeasureParagraph lays out for the same arguments: one DrawText command per
    // layout run with the foreground of the span the run came from, then one FillRectangle per decoration of the span.
    public virtual TextParagraphLayout RenderParagraph(
        DrawingContext drawingContext,
        IReadOnlyList<TextSpan> spans,
        TextAspect paragraphAspect,
        TextParagraphOptions options,
        LayoutSize availableSize,
        DrawPoint position)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);
        ArgumentNullException.ThrowIfNull(spans);
        ArgumentNullException.ThrowIfNull(options);

        TextParagraphLayout paragraph = textMeasurer.MeasureParagraph(spans, paragraphAspect, options, availableSize);
        foreach (DrawTextLayoutLine line in paragraph.Layout.Lines)
        {
            foreach (DrawTextLayoutRun run in line.Runs)
            {
                if ((uint)run.SpanIndex >= (uint)spans.Count)
                {
                    continue;
                }

                TextSpan span = spans[run.SpanIndex];
                if (span.Aspect.Foreground is not { } foreground)
                {
                    continue;
                }

                float baselineY = position.Y + run.Position.Y;
                drawingContext.DrawText(run.TextRun, new DrawPoint(position.X + run.Position.X, baselineY), foreground);
                if (span.Decorations != TextDecorations.None)
                {
                    DrawDecorations(drawingContext, span.Decorations, foreground, run, position, baselineY);
                }
            }
        }

        return paragraph;
    }

    private static void DrawDecorations(
        DrawingContext drawingContext,
        TextDecorations decorations,
        Brush foreground,
        DrawTextLayoutRun run,
        DrawPoint position,
        float baselineY)
    {
        float x = position.X + run.Bounds.X;
        float width = run.Bounds.Width;
        if (width <= 0)
        {
            return;
        }

        TextDecorationMetrics metrics = TextShaper.Default.TryMeasureDecorations(run.TextRun, out TextDecorationMetrics measured)
            ? measured
            : TextDecorationMetrics.CreateFallback(run.TextRun.Size);
        if (decorations.HasFlag(TextDecorations.Underline))
        {
            drawingContext.FillRectangle(
                new DrawRect(x, baselineY + metrics.UnderlineOffset, width, metrics.UnderlineThickness),
                foreground);
        }

        if (decorations.HasFlag(TextDecorations.Strikethrough))
        {
            // The offset is the distance from the baseline to the bottom of the stroke (SKFontMetrics.StrikeoutPosition),
            // unlike the underline offset, which is the distance to the top of the stroke.
            drawingContext.FillRectangle(
                new DrawRect(
                    x,
                    baselineY + metrics.StrikethroughOffset - metrics.StrikethroughThickness,
                    width,
                    metrics.StrikethroughThickness),
                foreground);
        }

        if (decorations.HasFlag(TextDecorations.Overline))
        {
            drawingContext.FillRectangle(
                new DrawRect(x, position.Y + run.Bounds.Y, width, metrics.UnderlineThickness),
                foreground);
        }

        if (decorations.HasFlag(TextDecorations.Baseline))
        {
            drawingContext.FillRectangle(
                new DrawRect(x, baselineY, width, metrics.UnderlineThickness),
                foreground);
        }
    }
}
