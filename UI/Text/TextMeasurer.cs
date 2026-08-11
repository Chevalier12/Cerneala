using Cerneala.UI.Layout;

namespace Cerneala.UI.Text;

public class TextMeasurer
{
    private readonly FontResolver fontResolver;
    private readonly LineBreakService lineBreakService;
    private readonly TextLayoutCache layoutCache;
    private readonly object layoutCacheSync = new();

    public TextMeasurer()
        : this(FontResolver.Default, LineBreakService.Default, new TextLayoutCache())
    {
    }

    public TextMeasurer(FontResolver fontResolver, LineBreakService lineBreakService, TextLayoutCache layoutCache)
    {
        this.fontResolver = fontResolver ?? throw new ArgumentNullException(nameof(fontResolver));
        this.lineBreakService = lineBreakService ?? throw new ArgumentNullException(nameof(lineBreakService));
        this.layoutCache = layoutCache ?? throw new ArgumentNullException(nameof(layoutCache));
    }

    public static TextMeasurer Default { get; } = new();

    public TextLayoutCache LayoutCache => layoutCache;

    public virtual TextMeasureResult Measure(string text, TextAspect aspect, float availableWidth)
    {
        return Measure(text, aspect, new LayoutSize(availableWidth, float.PositiveInfinity));
    }

    public virtual TextMeasureResult Measure(string text, TextAspect aspect, LayoutSize availableSize)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(aspect);
        float wrappingWidth = NormalizeWrappingWidth(aspect, availableSize.Width);
        ResolvedTextFont font = fontResolver.Resolve(aspect);
        TextLayoutKey baseKey = new(
            text,
            font.Identity,
            aspect.FontSize,
            aspect.Wrapping,
            wrappingWidth,
            aspect.Trimming,
            aspect.Scale);

        lock (layoutCacheSync)
        {
            TextMeasureResult fullResult = layoutCache.GetOrAdd(baseKey, _ =>
            {
                IReadOnlyList<TextLine> lines = lineBreakService.BreakLines(text, aspect, font, wrappingWidth);
                float width = lines.Count == 0 ? 0 : lines.Max(line => line.Width);
                float lineHeight = TextLineMetrics.MeasureLineHeight(aspect, font);
                float height = lineHeight * Math.Max(1, lines.Count);
                return new TextMeasureResult(new LayoutSize(width, height), lines.Count, baseKey, font.Identity, lines);
            });

            int visibleLineCount = CalculateVisibleLineCount(
                fullResult.Lines.Count,
                fullResult.Size.Height / Math.Max(1, fullResult.Lines.Count),
                availableSize.Height,
                aspect.Trimming);
            if (visibleLineCount >= fullResult.Lines.Count)
            {
                return fullResult;
            }

            TextLayoutKey collapsedKey = baseKey with { VisibleLineCount = visibleLineCount };
            return layoutCache.GetOrAdd(collapsedKey, _ =>
            {
                TextLine[] lines = fullResult.Lines.Take(visibleLineCount).ToArray();
                TextLine collapsedLastLine = lineBreakService.CollapseLine(
                    lines[^1],
                    aspect,
                    font,
                    wrappingWidth,
                    forceEllipsis: true);
                lines[^1] = collapsedLastLine;
                float lineHeight = fullResult.Size.Height / Math.Max(1, fullResult.Lines.Count);
                float width = MathF.Max(fullResult.Size.Width, collapsedLastLine.Width);
                return new TextMeasureResult(
                    new LayoutSize(width, lineHeight * visibleLineCount),
                    visibleLineCount,
                    collapsedKey,
                    font.Identity,
                    lines);
            });
        }
    }

    private static float NormalizeWrappingWidth(TextAspect aspect, float availableWidth)
    {
        if (float.IsPositiveInfinity(availableWidth) ||
            (aspect.Wrapping == TextWrapping.NoWrap && aspect.Trimming == TextTrimming.None))
        {
            return float.PositiveInfinity;
        }

        if (availableWidth <= 0 || float.IsNaN(availableWidth))
        {
            return 0;
        }

        return availableWidth;
    }

    private static int CalculateVisibleLineCount(
        int lineCount,
        float lineHeight,
        float availableHeight,
        TextTrimming trimming)
    {
        if (lineCount <= 1 || trimming == TextTrimming.None || float.IsPositiveInfinity(availableHeight))
        {
            return lineCount;
        }

        if (!float.IsFinite(availableHeight) || availableHeight <= 0)
        {
            return 1;
        }

        int fittingLines = (int)MathF.Floor(availableHeight / lineHeight);
        return Math.Clamp(Math.Max(1, fittingLines), 1, lineCount);
    }
}
