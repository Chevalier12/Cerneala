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
        ArgumentNullException.ThrowIfNull(text);
        float wrappingWidth = NormalizeWrappingWidth(aspect, availableWidth);
        ResolvedTextFont font = fontResolver.Resolve(aspect);
        TextLayoutKey key = new(text, font.Identity, aspect.FontSize, aspect.Wrapping, wrappingWidth, aspect.Trimming, aspect.Scale);

        lock (layoutCacheSync)
        {
            return layoutCache.GetOrAdd(key, _ =>
            {
                IReadOnlyList<TextLine> lines = lineBreakService.BreakLines(text, aspect, font, wrappingWidth);
                float width = lines.Count == 0 ? 0 : lines.Max(line => line.Width);
                float lineHeight = TextLineMetrics.MeasureLineHeight(aspect, font);
                float height = lineHeight * Math.Max(1, lines.Count);
                return new TextMeasureResult(new LayoutSize(width, height), lines.Count, key, font.Identity, lines);
            });
        }
    }

    private static float NormalizeWrappingWidth(TextAspect aspect, float availableWidth)
    {
        if (aspect.Wrapping == TextWrapping.NoWrap || float.IsPositiveInfinity(availableWidth))
        {
            return float.PositiveInfinity;
        }

        if (availableWidth <= 0 || float.IsNaN(availableWidth))
        {
            return 0;
        }

        return availableWidth;
    }
}
