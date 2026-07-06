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

    public virtual TextMeasureResult Measure(string text, TextRunStyle style, float availableWidth)
    {
        ArgumentNullException.ThrowIfNull(text);
        float wrappingWidth = NormalizeWrappingWidth(style, availableWidth);
        ResolvedTextFont font = fontResolver.Resolve(style);
        TextLayoutKey key = new(text, font.Identity, style.FontSize, style.Wrapping, wrappingWidth, style.Trimming, style.Scale);

        lock (layoutCacheSync)
        {
            return layoutCache.GetOrAdd(key, _ =>
            {
                IReadOnlyList<TextLine> lines = lineBreakService.BreakLines(text, style, wrappingWidth);
                float width = lines.Count == 0 ? 0 : lines.Max(line => line.Width);
                float lineHeight = TextLineMetrics.MeasureLineHeight(style, font);
                float height = lineHeight * Math.Max(1, lines.Count);
                return new TextMeasureResult(new LayoutSize(width, height), lines.Count, key, font.Identity, lines);
            });
        }
    }

    private static float NormalizeWrappingWidth(TextRunStyle style, float availableWidth)
    {
        if (style.Wrapping == TextWrapping.NoWrap || float.IsPositiveInfinity(availableWidth))
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
