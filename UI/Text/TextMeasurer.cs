using System.Globalization;
using System.Text;
using Cerneala.Drawing;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.UI.Text;

public class TextMeasurer
{
    private static readonly Brush ParagraphPlaceholderBrush = new SolidColorBrush(Color.Black);

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

    // Lays out the spans as one paragraph through the Drawing text layout engine. The engine result does not depend on the
    // brushes of the spans (a layout run names its span by index, and the renderer takes the brush from the span), so
    // one placeholder brush serves every span and the cached layout is shared by spans that differ only in brush.
    public virtual TextParagraphLayout MeasureParagraph(
        IReadOnlyList<TextSpan> spans,
        TextAspect paragraphAspect,
        TextParagraphOptions options,
        LayoutSize availableSize)
    {
        ArgumentNullException.ThrowIfNull(spans);
        ArgumentNullException.ThrowIfNull(options);
        float wrappingWidth = NormalizeParagraphWidth(paragraphAspect, options, availableSize.Width);
        float maxHeight = paragraphAspect.Trimming == TextTrimming.None
            ? float.PositiveInfinity
            : NormalizeParagraphHeight(availableSize.Height);
        ResolvedTextFont[] fonts = new ResolvedTextFont[spans.Count];
        for (int index = 0; index < fonts.Length; index++)
        {
            fonts[index] = fontResolver.Resolve(spans[index].Aspect, paragraphAspect.Scale);
        }

        string key = CreateParagraphKey(spans, fonts, paragraphAspect, options, wrappingWidth, maxHeight);
        lock (layoutCacheSync)
        {
            return layoutCache.GetOrAddParagraph(
                key,
                () => BuildParagraph(spans, fonts, paragraphAspect, options, wrappingWidth, maxHeight, key));
        }
    }

    private static TextParagraphLayout BuildParagraph(
        IReadOnlyList<TextSpan> spans,
        ResolvedTextFont[] fonts,
        TextAspect paragraphAspect,
        TextParagraphOptions options,
        float wrappingWidth,
        float maxHeight,
        string key)
    {
        DrawTextSpan[] drawSpans = new DrawTextSpan[spans.Count];
        bool hasText = false;
        for (int index = 0; index < drawSpans.Length; index++)
        {
            TextSpan span = spans[index];
            drawSpans[index] = new DrawTextSpan(span.Text, fonts[index].Font, span.Aspect.FontSize, ParagraphPlaceholderBrush);
            hasText |= span.Text.Length > 0;
        }

        DrawTextLayout layout = DrawTextLayoutEngine.Build(
            drawSpans,
            CreateLayoutOptions(paragraphAspect, options, wrappingWidth, maxHeight, options.MaxLines));
        if (layout.Lines.Count == 0 && drawSpans.Length > 0 && float.IsFinite(maxHeight))
        {
            // As a trimmed TextBlock always shows its first line, also when the available height is smaller than a line.
            layout = DrawTextLayoutEngine.Build(
                drawSpans,
                CreateLayoutOptions(paragraphAspect, options, wrappingWidth, float.PositiveInfinity, maxLines: 1));
        }

        float width = 0;
        foreach (DrawTextLayoutLine line in layout.Lines)
        {
            // The width the line breaker measured, so that laying the text out again at this width breaks the same lines.
            width = MathF.Max(width, line.MeasuredWidth);
        }

        return new TextParagraphLayout(
            new LayoutSize(width, layout.Bounds.Height),
            layout.Lines.Count,
            layout,
            key);
    }

    private static DrawTextLayoutOptions CreateLayoutOptions(
        TextAspect paragraphAspect,
        TextParagraphOptions options,
        float wrappingWidth,
        float maxHeight,
        int maxLines)
    {
        return new DrawTextLayoutOptions(
            maxWidth: wrappingWidth,
            maxHeight: maxHeight,
            wrapping: paragraphAspect.Wrapping switch
            {
                TextWrapping.NoWrap => DrawTextWrapping.NoWrap,
                TextWrapping.WrapWithOverflow => DrawTextWrapping.WordWithOverflow,
                _ => DrawTextWrapping.Word
            },
            alignment: options.Alignment switch
            {
                TextAlignment.Right => DrawTextAlignment.Right,
                TextAlignment.Center => DrawTextAlignment.Center,
                TextAlignment.Justify => DrawTextAlignment.Justify,
                _ => DrawTextAlignment.Left
            },
            maxLines: maxLines,
            trimming: paragraphAspect.Trimming switch
            {
                TextTrimming.CharacterEllipsis => DrawTextTrimming.CharacterEllipsis,
                TextTrimming.WordEllipsis => DrawTextTrimming.WordEllipsis,
                _ => DrawTextTrimming.None
            },
            scale: paragraphAspect.Scale,
            lineHeight: options.LineHeight,
            lineStackingStrategy: options.LineStackingStrategy == LineStackingStrategy.BlockLineHeight
                ? DrawLineStackingStrategy.BlockLineHeight
                : DrawLineStackingStrategy.MaxHeight);
    }

    private static string CreateParagraphKey(
        IReadOnlyList<TextSpan> spans,
        ResolvedTextFont[] fonts,
        TextAspect paragraphAspect,
        TextParagraphOptions options,
        float wrappingWidth,
        float maxHeight)
    {
        StringBuilder key = new();
        key.Append(CultureInfo.InvariantCulture, $"{(int)paragraphAspect.Wrapping}|{(int)paragraphAspect.Trimming}|{paragraphAspect.Scale:R}|{wrappingWidth:R}|{maxHeight:R}|{(int)options.Alignment}|{options.MaxLines}|{options.LineHeight:R}|{(int)options.LineStackingStrategy}");
        for (int index = 0; index < fonts.Length; index++)
        {
            TextSpan span = spans[index];
            key.Append(CultureInfo.InvariantCulture, $"\u001F{span.Text.Length}:{span.Text}|{fonts[index].Identity}|{span.Aspect.FontSize:R}");
        }

        return key.ToString();
    }

    // A paragraph with NoWrap, no trimming and left alignment does not use the available width, so it keeps one layout for
    // every width; with another alignment or with trimming the width is the line width the lines align or trim to.
    private static float NormalizeParagraphWidth(TextAspect aspect, TextParagraphOptions options, float availableWidth)
    {
        if (float.IsPositiveInfinity(availableWidth) ||
            (aspect.Wrapping == TextWrapping.NoWrap &&
                aspect.Trimming == TextTrimming.None &&
                options.Alignment == TextAlignment.Left))
        {
            return float.PositiveInfinity;
        }

        if (availableWidth <= 0 || float.IsNaN(availableWidth))
        {
            return 0;
        }

        return availableWidth;
    }

    private static float NormalizeParagraphHeight(float availableHeight)
    {
        if (float.IsPositiveInfinity(availableHeight))
        {
            return availableHeight;
        }

        return availableHeight > 0 && float.IsFinite(availableHeight) ? availableHeight : 0;
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
