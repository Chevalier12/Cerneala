using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Cerneala.Drawing.Text;

namespace Cerneala.Drawing;

public enum DrawTextWrapping
{
    NoWrap,
    Word,
    Character,
    WordWithOverflow
}

public enum DrawTextAlignment
{
    Start,
    Center,
    End,
    Justify,
    Left,
    Right
}

public enum DrawLineStackingStrategy
{
    MaxHeight,
    BlockLineHeight
}

public enum DrawTextTrimming
{
    None,
    CharacterEllipsis,
    WordEllipsis
}

public enum DrawTextDirection
{
    Auto,
    LeftToRight,
    RightToLeft
}

public sealed class DrawTextSpan
{
    private readonly ReadOnlyCollection<IDrawFont> fallbackFonts;

    public DrawTextSpan(
        string text,
        IDrawFont font,
        float size,
        IDrawBrush brush,
        float opacity = 1,
        IEnumerable<IDrawFont>? fallbackFonts = null)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Font = font ?? throw new ArgumentNullException(nameof(font));
        DrawArgument.ThrowIfNotValidTextSize(size, nameof(size));
        Brush = brush ?? throw new ArgumentNullException(nameof(brush));
        ThrowIfNotOpacity(opacity);
        IDrawFont[] fallbacks = fallbackFonts?.ToArray() ?? [];
        if (fallbacks.Any(static candidate => candidate is null))
        {
            throw new ArgumentException("Fallback fonts cannot contain null values.", nameof(fallbackFonts));
        }

        Size = size;
        Opacity = opacity;
        this.fallbackFonts = Array.AsReadOnly(fallbacks);
    }

    public DrawTextSpan(
        string text,
        IDrawFont font,
        float size,
        Color color,
        float opacity = 1,
        IEnumerable<IDrawFont>? fallbackFonts = null)
        : this(text, font, size, new DrawTextSolidBrush(color), opacity, fallbackFonts)
    {
    }

    public string Text { get; }

    public IDrawFont Font { get; }

    public float Size { get; }

    public IDrawBrush Brush { get; }

    public float Opacity { get; }

    public IReadOnlyList<IDrawFont> FallbackFonts => fallbackFonts;

    private static void ThrowIfNotOpacity(float opacity)
    {
        if (!float.IsFinite(opacity) || opacity < 0 || opacity > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity));
        }
    }

    private sealed record DrawTextSolidBrush(Color Color) : IDrawBrush
    {
        public DrawBrushKind Kind => DrawBrushKind.SolidColor;

        public float Opacity => 1;

        public Color? SolidColor => Color;

        public DrawBrushDescriptor CreateDescriptor() =>
            new SolidDrawBrushDescriptor(Color, 1);
    }
}

public sealed record DrawTextLayoutOptions
{
    public DrawTextLayoutOptions(
        float maxWidth = float.PositiveInfinity,
        float maxHeight = float.PositiveInfinity,
        DrawTextWrapping wrapping = DrawTextWrapping.NoWrap,
        DrawTextAlignment alignment = DrawTextAlignment.Start,
        float lineSpacing = 1,
        int maxLines = 0,
        DrawTextTrimming trimming = DrawTextTrimming.None,
        DrawTextDirection direction = DrawTextDirection.Auto,
        float scale = 1,
        float lineHeight = float.NaN,
        DrawLineStackingStrategy lineStackingStrategy = DrawLineStackingStrategy.MaxHeight)
    {
        ThrowIfNotConstraint(maxWidth, nameof(maxWidth));
        ThrowIfNotConstraint(maxHeight, nameof(maxHeight));
        if (!Enum.IsDefined(wrapping))
        {
            throw new ArgumentOutOfRangeException(nameof(wrapping));
        }
        if (!Enum.IsDefined(alignment))
        {
            throw new ArgumentOutOfRangeException(nameof(alignment));
        }
        if (!float.IsFinite(lineSpacing) || lineSpacing <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lineSpacing));
        }
        ArgumentOutOfRangeException.ThrowIfNegative(maxLines);
        if (!Enum.IsDefined(trimming))
        {
            throw new ArgumentOutOfRangeException(nameof(trimming));
        }
        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }
        if (!float.IsFinite(scale) || scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale));
        }
        if (!float.IsNaN(lineHeight) && (!float.IsFinite(lineHeight) || lineHeight <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(lineHeight));
        }
        if (!Enum.IsDefined(lineStackingStrategy))
        {
            throw new ArgumentOutOfRangeException(nameof(lineStackingStrategy));
        }

        MaxWidth = maxWidth;
        MaxHeight = maxHeight;
        Wrapping = wrapping;
        Alignment = alignment;
        LineSpacing = lineSpacing;
        MaxLines = maxLines;
        Trimming = trimming;
        Direction = direction;
        Scale = scale;
        LineHeight = lineHeight;
        LineStackingStrategy = lineStackingStrategy;
    }

    public float MaxWidth { get; }

    public float MaxHeight { get; }

    public DrawTextWrapping Wrapping { get; }

    public DrawTextAlignment Alignment { get; }

    public float LineSpacing { get; }

    public int MaxLines { get; }

    public DrawTextTrimming Trimming { get; }

    public DrawTextDirection Direction { get; }

    public float Scale { get; }

    // NaN keeps the height of the line content.
    public float LineHeight { get; }

    public DrawLineStackingStrategy LineStackingStrategy { get; }

    private static void ThrowIfNotConstraint(float value, string parameterName)
    {
        if ((!float.IsFinite(value) && !float.IsPositiveInfinity(value)) || value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

public sealed class DrawTextLayoutRun
{
    internal DrawTextLayoutRun(
        DrawTextRun textRun,
        DrawPoint position,
        IDrawBrush brush,
        float opacity,
        DrawTextDirection direction,
        DrawRect bounds,
        int spanIndex)
    {
        TextRun = textRun;
        Position = position;
        Brush = brush;
        Opacity = opacity;
        Direction = direction;
        Bounds = bounds;
        SpanIndex = spanIndex;
    }

    public DrawTextRun TextRun { get; }

    public string Text => TextRun.Text;

    public IDrawFont Font => TextRun.Font;

    public float Size => TextRun.Size;

    public DrawPoint Position { get; }

    public IDrawBrush Brush { get; }

    public float Opacity { get; }

    public DrawTextDirection Direction { get; }

    public DrawRect Bounds { get; }

    // The index of the layout's source span that this run's text came from.
    internal int SpanIndex { get; }
}

public sealed class DrawTextLayoutLine
{
    private readonly ReadOnlyCollection<DrawTextLayoutRun> runs;

    internal DrawTextLayoutLine(
        DrawTextLayoutRun[] runs,
        DrawRect bounds,
        float baseline,
        DrawTextDirection direction,
        bool isTrimmed,
        float measuredWidth)
    {
        this.runs = Array.AsReadOnly(runs);
        Bounds = bounds;
        Baseline = baseline;
        Direction = direction;
        IsTrimmed = isTrimmed;
        MeasuredWidth = measuredWidth;
    }

    // The width of the line text as the line breaker measured it. A wrapping width that is at least this wide breaks the
    // line the same way again. Bounds.Width can differ from it: it is the sum of the widths of the runs, and it includes
    // the justification.
    internal float MeasuredWidth { get; }

    public IReadOnlyList<DrawTextLayoutRun> Runs => runs;

    public DrawRect Bounds { get; }

    public float Baseline { get; }

    public DrawTextDirection Direction { get; }

    public bool IsTrimmed { get; }

    public string Text => string.Concat(runs.Select(static run => run.Text));
}

public sealed class DrawTextLayout
{
    private static long nextStableId;
    private readonly ReadOnlyCollection<DrawTextLayoutLine> lines;

    internal DrawTextLayout(DrawTextLayoutLine[] lines, DrawRect bounds, DrawTextLayoutOptions options)
    {
        this.lines = Array.AsReadOnly(lines);
        Bounds = bounds;
        Options = options;
        StableId = Interlocked.Increment(ref nextStableId);
    }

    public IReadOnlyList<DrawTextLayoutLine> Lines => lines;

    public DrawRect Bounds { get; }

    public DrawTextLayoutOptions Options { get; }

    public long StableId { get; }
}

public sealed class DrawTextLayoutBuilder
{
    private readonly List<DrawTextSpan> spans = [];

    public DrawTextLayoutBuilder AddSpan(DrawTextSpan span)
    {
        spans.Add(span ?? throw new ArgumentNullException(nameof(span)));
        return this;
    }

    public DrawTextLayoutBuilder AddSpan(
        string text,
        IDrawFont font,
        float size,
        IDrawBrush brush,
        float opacity = 1)
    {
        return AddSpan(new DrawTextSpan(text, font, size, brush, opacity));
    }

    public DrawTextLayoutBuilder AddSpan(
        string text,
        IDrawFont font,
        float size,
        Color color,
        float opacity = 1)
    {
        return AddSpan(new DrawTextSpan(text, font, size, color, opacity));
    }

    public DrawTextLayout Build(DrawTextLayoutOptions? options = null)
    {
        options ??= new DrawTextLayoutOptions();
        return DrawTextLayoutCache.GetOrCreate(spans, options);
    }
}

internal static class DrawTextLayoutCache
{
    private const int MaximumEntries = 256;
    private static readonly object Sync = new();
    private static readonly Dictionary<DrawTextLayoutCacheKey, DrawTextLayout> Entries = [];
    private static readonly Queue<DrawTextLayoutCacheKey> InsertionOrder = [];

    public static DrawTextLayout GetOrCreate(
        IReadOnlyList<DrawTextSpan> spans,
        DrawTextLayoutOptions options)
    {
        DrawTextLayoutCacheKey key = DrawTextLayoutCacheKey.Create(spans, options);
        lock (Sync)
        {
            if (Entries.TryGetValue(key, out DrawTextLayout? existing))
            {
                return existing;
            }

            DrawTextLayout created = DrawTextLayoutEngine.Build(spans, options);
            Entries.Add(key, created);
            InsertionOrder.Enqueue(key);
            while (Entries.Count > MaximumEntries && InsertionOrder.TryDequeue(out DrawTextLayoutCacheKey oldest))
            {
                Entries.Remove(oldest);
            }
            return created;
        }
    }

    private readonly record struct DrawTextLayoutCacheKey(string Value)
    {
        public static DrawTextLayoutCacheKey Create(
            IReadOnlyList<DrawTextSpan> spans,
            DrawTextLayoutOptions options)
        {
            System.Text.StringBuilder key = new();
            key.Append(options.MaxWidth).Append('|')
                .Append(options.MaxHeight).Append('|')
                .Append((int)options.Wrapping).Append('|')
                .Append((int)options.Alignment).Append('|')
                .Append(options.LineSpacing).Append('|')
                .Append(options.MaxLines).Append('|')
                .Append((int)options.Trimming).Append('|')
                .Append((int)options.Direction).Append('|')
                .Append(options.Scale).Append('|')
                .Append(options.LineHeight).Append('|')
                .Append((int)options.LineStackingStrategy);
            foreach (DrawTextSpan span in spans)
            {
                key.Append('\u001F').Append(span.Text)
                    .Append('|').Append(RuntimeHelpers.GetHashCode(span.Font))
                    .Append('|').Append(span.Size)
                    .Append('|').Append(RuntimeHelpers.GetHashCode(span.Brush))
                    .Append('|').Append(span.Opacity);
                foreach (IDrawFont fallback in span.FallbackFonts)
                {
                    key.Append('|').Append(RuntimeHelpers.GetHashCode(fallback));
                }
            }
            return new DrawTextLayoutCacheKey(key.ToString());
        }
    }
}

internal static class DrawTextLayoutEngine
{
    private readonly record struct SpanRange(DrawTextSpan Span, int Start, int Length)
    {
        public int End => Start + Length;
    }

    private readonly record struct Fragment(
        string Text,
        IDrawFont Font,
        float Size,
        IDrawBrush Brush,
        float Opacity,
        UnicodeTextDirection Direction,
        float Width,
        bool IsWhitespace,
        int SpanIndex);

    public static DrawTextLayout Build(
        IReadOnlyList<DrawTextSpan> spans,
        DrawTextLayoutOptions options)
    {
        ArgumentNullException.ThrowIfNull(spans);
        ArgumentNullException.ThrowIfNull(options);
        if (spans.Count == 0)
        {
            return new DrawTextLayout([], new DrawRect(0, 0, 0, 0), options);
        }

        string text = string.Concat(spans.Select(static span => span.Text));
        SpanRange[] ranges = CreateRanges(spans);
        float Measure(int start, int length) => MeasureRange(text, ranges, start, length, options.Scale);
        IReadOnlyList<UnicodeLineSegment> candidates = UnicodeLineBreakEngine.BreakLines(
            text,
            options.MaxWidth,
            MapWrapping(options.Wrapping),
            Measure);
        List<UnicodeLineSegment> accepted = [];
        float measuredHeight = 0;
        for (int index = 0; index < candidates.Count; index++)
        {
            UnicodeLineSegment candidate = candidates[index];
            float lineHeight = ApplyLineHeight(ResolveLineMetrics(ranges, candidate, options.Scale).Height, options) *
                options.LineSpacing;
            bool exceedsLines = options.MaxLines > 0 && accepted.Count >= options.MaxLines;
            bool exceedsHeight = measuredHeight + lineHeight > options.MaxHeight;
            if (exceedsLines || exceedsHeight)
            {
                break;
            }

            accepted.Add(candidate);
            measuredHeight += lineHeight;
        }

        if (accepted.Count == 0 && candidates.Count > 0 && options.MaxLines != 0 && options.MaxHeight > 0)
        {
            accepted.Add(candidates[0]);
        }

        // After the first-line fallback: a first line that is the whole text hides nothing.
        bool omitted = accepted.Count < candidates.Count;

        // As in WPF, every line wider than MaxWidth collapses, and the last line also when later lines are omitted.
        if (options.Trimming != DrawTextTrimming.None)
        {
            float[] ellipsisWidths = new float[ranges.Length];
            Array.Fill(ellipsisWidths, float.NaN);
            for (int index = 0; index < accepted.Count; index++)
            {
                UnicodeLineSegment line = accepted[index];
                bool hidesLaterLines = omitted && index == accepted.Count - 1;
                bool widthOverflow = float.IsFinite(options.MaxWidth) && line.Width > options.MaxWidth;
                if (!hidesLaterLines && !widthOverflow)
                {
                    continue;
                }

                accepted[index] = UnicodeLineBreakEngine.CollapseLine(
                    text,
                    line,
                    options.MaxWidth,
                    options.Trimming == DrawTextTrimming.WordEllipsis
                        ? UnicodeLineTrimMode.WordEllipsis
                        : UnicodeLineTrimMode.CharacterEllipsis,
                    Measure,
                    end => MeasureEllipsis(FindEllipsisSpanIndex(ranges, line.SourceStart, end)),
                    forceEllipsis: hidesLaterLines);
            }

            float MeasureEllipsis(int spanIndex)
            {
                if (float.IsNaN(ellipsisWidths[spanIndex]))
                {
                    ellipsisWidths[spanIndex] = MeasureLiteral(UnicodeLineBreakEngine.Ellipsis, ranges[spanIndex].Span, options.Scale);
                }

                return ellipsisWidths[spanIndex];
            }
        }

        List<DrawTextLayoutLine> lines = [];
        float y = 0;
        float maximumRight = 0;
        for (int index = 0; index < accepted.Count; index++)
        {
            UnicodeLineSegment segment = accepted[index];
            DrawTextDirection baseDirection = ResolveBaseDirection(segment.Text, options.Direction);
            LineMetrics contentMetrics = ResolveLineMetrics(ranges, segment, options.Scale);
            float contentHeight = contentMetrics.Height;
            float contentBaseline = contentMetrics.Baseline;
            float lineHeight = ApplyLineHeight(contentHeight, options);
            float advanceHeight = lineHeight * options.LineSpacing;
            float baseline = lineHeight == contentHeight
                ? contentBaseline
                : contentBaseline * lineHeight / contentHeight;
            // As in WPF, the line that ends a paragraph (before a hard line break or at the end of the text) is not justified.
            // The candidates decide it, so a line after which MaxLines or MaxHeight hides more of the paragraph is justified.
            bool justify = options.Alignment == DrawTextAlignment.Justify &&
                !segment.IsTrimmed && !EndsParagraph(text, candidates, index);
            Fragment[] fragments = CreateFragments(text, ranges, segment, options.Scale, baseDirection, justify);
            float contentWidth = fragments.Sum(static fragment => fragment.Width);
            float layoutWidth = float.IsFinite(options.MaxWidth)
                ? options.MaxWidth
                : contentWidth;
            float justification = justify
                ? ResolveJustification(layoutWidth, contentWidth, fragments)
                : 0;
            float x = ResolveAlignmentOffset(options.Alignment, baseDirection, layoutWidth, contentWidth);
            List<DrawTextLayoutRun> runs = [];
            foreach (Fragment fragment in fragments)
            {
                if (fragment.Text.Length > 0)
                {
                    DrawTextRun textRun = new(fragment.Font, fragment.Text, fragment.Size);
                    // A justified space includes its share of the extra width, so the runs of the line touch each other.
                    float runWidth = fragment.IsWhitespace ? fragment.Width + justification : fragment.Width;
                    DrawRect runBounds = new(x, y, runWidth, lineHeight);
                    runs.Add(new DrawTextLayoutRun(
                        textRun,
                        new DrawPoint(x, y + baseline),
                        fragment.Brush,
                        fragment.Opacity,
                        MapDirection(fragment.Direction),
                        runBounds,
                        fragment.SpanIndex));
                }
                x += fragment.Width;
                if (fragment.IsWhitespace)
                {
                    x += justification;
                }
            }

            float actualWidth = MathF.Max(0, x - ResolveAlignmentOffset(options.Alignment, baseDirection, layoutWidth, contentWidth));
            float lineX = ResolveAlignmentOffset(options.Alignment, baseDirection, layoutWidth, contentWidth);
            DrawRect bounds = new(lineX, y, actualWidth, lineHeight);
            lines.Add(new DrawTextLayoutLine(
                runs.ToArray(),
                bounds,
                y + baseline,
                baseDirection,
                segment.IsTrimmed,
                segment.Width));
            maximumRight = MathF.Max(maximumRight, bounds.Right);
            y += advanceHeight;
        }

        float width = float.IsFinite(options.MaxWidth) && lines.Count > 0
            ? options.MaxWidth
            : maximumRight;
        float height = lines.Count == 0
            ? 0
            : MathF.Min(options.MaxHeight, lines[^1].Bounds.Bottom);
        return new DrawTextLayout(lines.ToArray(), new DrawRect(0, 0, width, height), options);
    }

    private static bool EndsParagraph(string text, IReadOnlyList<UnicodeLineSegment> lines, int index)
    {
        if (index == lines.Count - 1)
        {
            return true;
        }

        // A soft wrap only skips the white space between two lines. A hard line break between them ends the paragraph.
        UnicodeLineSegment line = lines[index];
        int end = line.SourceStart + line.SourceLength;
        return text.AsSpan(end, lines[index + 1].SourceStart - end).IndexOfAny('\r', '\n') >= 0;
    }

    private static SpanRange[] CreateRanges(IReadOnlyList<DrawTextSpan> spans)
    {
        SpanRange[] ranges = new SpanRange[spans.Count];
        int start = 0;
        for (int index = 0; index < spans.Count; index++)
        {
            DrawTextSpan span = spans[index];
            ranges[index] = new SpanRange(span, start, span.Text.Length);
            start += span.Text.Length;
        }
        return ranges;
    }

    private static float MeasureRange(
        string text,
        SpanRange[] ranges,
        int start,
        int length,
        float scale)
    {
        if (length == 0)
        {
            return 0;
        }

        int end = start + length;
        float width = 0;
        foreach (SpanRange range in ranges)
        {
            int overlapStart = Math.Max(start, range.Start);
            int overlapEnd = Math.Min(end, range.End);
            if (overlapEnd <= overlapStart)
            {
                continue;
            }
            width += MeasureSpanText(text, overlapStart, overlapEnd, range.Span, scale);
        }
        return width;
    }

    // The width of text[start..end] from one span, measured with the fonts it is drawn with: each grapheme cluster with
    // the font that ResolveFont selects for it, and consecutive clusters with the same font together.
    private static float MeasureSpanText(string text, int start, int end, DrawTextSpan span, float scale)
    {
        if (span.FallbackFonts.Count == 0)
        {
            return MeasureLiteral(text[start..end], span, scale);
        }

        float size = span.Size * scale;
        float width = 0;
        int sameFontStart = start;
        IDrawFont? sameFont = null;
        foreach (UnicodeTextElement element in UnicodeLineBreakEngine.CreateTextElements(text, start, end - start))
        {
            IDrawFont font = ResolveFont(span, element.Text, scale);
            if (sameFont is not null && !ReferenceEquals(font, sameFont))
            {
                width += MeasureLiteral(text[sameFontStart..element.Start], sameFont, size);
                sameFontStart = element.Start;
            }
            sameFont = font;
        }

        return sameFont is null ? width : width + MeasureLiteral(text[sameFontStart..end], sameFont, size);
    }

    private static float MeasureLiteral(string value, DrawTextSpan span, float scale) =>
        MeasureLiteral(value, span.Font, span.Size * scale);

    private static float MeasureLiteral(string value, IDrawFont font, float size)
    {
        DrawTextRun run = new(font, value, size);
        return TextShaper.Default.TryShape(run, out TextShapeResult result)
            ? MathF.Abs(result.AdvanceWidth)
            : StringInfo.ParseCombiningCharacters(value).Length * size * 0.5f;
    }

    // The content height of a line and the distance from its top to the shared baseline. Every span on the line
    // contributes: the baseline is the largest baseline of the spans (so no glyph rises above the top of the line) and the
    // height is that baseline plus the largest part below the baseline.
    private static LineMetrics ResolveLineMetrics(
        SpanRange[] ranges,
        UnicodeLineSegment line,
        float scale)
    {
        bool found = false;
        SpanMetrics highest = default;
        SpanMetrics deepest = default;
        int end = line.SourceStart + Math.Max(1, line.SourceLength);
        foreach (SpanRange range in ranges)
        {
            if (range.End <= line.SourceStart || range.Start >= end)
            {
                continue;
            }

            Include(MeasureSpan(range.Span, scale));
        }

        if (!found)
        {
            // A line that no span overlaps, as the empty line after a trailing line break, has the style of the span
            // that holds its start.
            Include(MeasureSpan(FindSpan(ranges, line.SourceStart), scale));
        }

        // One span gives both extremes in the common case, and then its measured height is used as it is.
        float height = highest == deepest ? highest.Height : highest.Baseline + deepest.Descent;
        return new LineMetrics(height, highest.Baseline);

        void Include(SpanMetrics metrics)
        {
            if (!found || metrics.Baseline > highest.Baseline)
            {
                highest = metrics;
            }

            if (!found || metrics.Descent > deepest.Descent)
            {
                deepest = metrics;
            }

            found = true;
        }
    }

    private static SpanMetrics MeasureSpan(DrawTextSpan span, float scale)
    {
        DrawTextRun run = new(span.Font, "Mg", span.Size * scale);
        float height = TextShaper.Default.TryMeasureLineHeight(run, out float measuredHeight)
            ? measuredHeight
            : span.Size * scale * 1.2f;
        float baseline = TextShaper.Default.TryMeasureBaseline(run, out float measuredBaseline)
            ? measuredBaseline
            : height * 0.8f;
        return new SpanMetrics(height, baseline, height - baseline);
    }

    private readonly record struct SpanMetrics(float Height, float Baseline, float Descent);

    private readonly record struct LineMetrics(float Height, float Baseline);

    // The line box height: the content height, or the requested line height by the stacking strategy.
    private static float ApplyLineHeight(float contentHeight, DrawTextLayoutOptions options)
    {
        if (float.IsNaN(options.LineHeight))
        {
            return contentHeight;
        }

        return options.LineStackingStrategy == DrawLineStackingStrategy.BlockLineHeight
            ? options.LineHeight
            : MathF.Max(options.LineHeight, contentHeight);
    }

    private static Fragment[] CreateFragments(
        string text,
        SpanRange[] ranges,
        UnicodeLineSegment segment,
        float scale,
        DrawTextDirection baseDirection,
        bool preserveClusters)
    {
        List<Fragment> logical = [];
        int sourceEnd = segment.SourceStart + segment.SourceLength;
        UnicodeTextElement[] elements = UnicodeLineBreakEngine.CreateTextElements(
            text,
            segment.SourceStart,
            segment.SourceLength);
        UnicodeTextDirection[] directions = ResolveDirections(elements, baseDirection);
        for (int index = 0; index < elements.Length; index++)
        {
            UnicodeTextElement element = elements[index];
            int spanIndex = FindSpanIndex(ranges, element.Start);
            DrawTextSpan span = ranges[spanIndex].Span;
            IDrawFont font = ResolveFont(span, element.Text, scale);
            UnicodeTextDirection direction = directions[index];
            AddOrMerge(
                logical,
                new Fragment(
                    element.Text,
                    font,
                    span.Size * scale,
                    span.Brush,
                    span.Opacity,
                    direction,
                    MeasureLiteral(element.Text, font, span.Size * scale),
                    // Justification stretches every white space, also a no-break space, which is no break white space.
                    element.Text.All(char.IsWhiteSpace),
                    spanIndex),
                preserveClusters);
        }

        if (segment.IsTrimmed && segment.Text.EndsWith(UnicodeLineBreakEngine.Ellipsis, StringComparison.Ordinal))
        {
            int spanIndex = FindEllipsisSpanIndex(ranges, segment.SourceStart, sourceEnd);
            DrawTextSpan span = ranges[spanIndex].Span;
            // The ellipsis is a neutral at the end of the line, so it takes the paragraph direction (UAX #9 rules N1 and N2)
            // and ReorderVisually keeps it at the visual end of the line.
            UnicodeTextDirection direction = baseDirection == DrawTextDirection.RightToLeft
                ? UnicodeTextDirection.RightToLeft
                : UnicodeTextDirection.LeftToRight;
            AddOrMerge(
                logical,
                new Fragment(
                    UnicodeLineBreakEngine.Ellipsis,
                    span.Font,
                    span.Size * scale,
                    span.Brush,
                    span.Opacity,
                    direction,
                    MeasureLiteral(UnicodeLineBreakEngine.Ellipsis, span, scale),
                    false,
                    spanIndex),
                preserveClusters);
        }

        ReorderVisually(logical, baseDirection);
        return logical.ToArray();
    }

    // The direction of every text element of a line. A sequence of neutrals between two strong elements of the same
    // direction takes that direction; between different directions it takes the embedding direction (Unicode UAX #9 rules
    // N1 and N2). The start and the end of the line count as the embedding direction. So in a left-to-right line the space
    // in "שלום efgh" is left-to-right and the one in "שלום עולם" is right-to-left.
    private static UnicodeTextDirection[] ResolveDirections(UnicodeTextElement[] elements, DrawTextDirection baseDirection)
    {
        UnicodeTextDirection embedding = baseDirection == DrawTextDirection.RightToLeft
            ? UnicodeTextDirection.RightToLeft
            : UnicodeTextDirection.LeftToRight;
        UnicodeTextDirection[] directions = new UnicodeTextDirection[elements.Length];
        for (int index = 0; index < elements.Length; index++)
        {
            directions[index] = UnicodeBidiEngine.GetDirection(elements[index].Text);
        }

        int neutralStart = 0;
        while (neutralStart < directions.Length)
        {
            if (directions[neutralStart] != UnicodeTextDirection.Neutral)
            {
                neutralStart++;
                continue;
            }

            int neutralEnd = neutralStart + 1;
            while (neutralEnd < directions.Length && directions[neutralEnd] == UnicodeTextDirection.Neutral)
            {
                neutralEnd++;
            }

            UnicodeTextDirection before = neutralStart > 0 ? directions[neutralStart - 1] : embedding;
            UnicodeTextDirection after = neutralEnd < directions.Length ? directions[neutralEnd] : embedding;
            Array.Fill(directions, before == after ? before : embedding, neutralStart, neutralEnd - neutralStart);
            neutralStart = neutralEnd;
        }

        return directions;
    }

    // Unicode UAX #9 rule L2: from the highest embedding level down to the lowest odd level, reverse every sequence of
    // fragments at that level or higher. A right-to-left fragment has level 1; a left-to-right fragment has the base level,
    // 0 in a left-to-right line and 2 in a right-to-left line. So the fragments of a right-to-left word in a left-to-right
    // line, and of a left-to-right word in a right-to-left line, keep the reading order of their word. This matters when
    // justification keeps every grapheme as its own fragment.
    private static void ReorderVisually(List<Fragment> fragments, DrawTextDirection baseDirection)
    {
        int leftToRightLevel = baseDirection == DrawTextDirection.RightToLeft ? 2 : 0;
        int Level(Fragment fragment) => fragment.Direction == UnicodeTextDirection.RightToLeft ? 1 : leftToRightLevel;

        for (int level = Math.Max(1, leftToRightLevel); level >= 1; level--)
        {
            int index = 0;
            while (index < fragments.Count)
            {
                if (Level(fragments[index]) < level)
                {
                    index++;
                    continue;
                }

                int end = index + 1;
                while (end < fragments.Count && Level(fragments[end]) >= level)
                {
                    end++;
                }

                fragments.Reverse(index, end - index);
                index = end;
            }
        }
    }

    private static void AddOrMerge(List<Fragment> fragments, Fragment candidate, bool preserveClusters)
    {
        if (!preserveClusters && fragments.Count > 0)
        {
            Fragment previous = fragments[^1];
            if (ReferenceEquals(previous.Font, candidate.Font) &&
                ReferenceEquals(previous.Brush, candidate.Brush) &&
                previous.SpanIndex == candidate.SpanIndex &&
                previous.Size == candidate.Size &&
                previous.Opacity == candidate.Opacity &&
                previous.Direction == candidate.Direction)
            {
                fragments[^1] = previous with
                {
                    Text = previous.Text + candidate.Text,
                    Width = previous.Width + candidate.Width,
                    IsWhitespace = false
                };
                return;
            }
        }
        fragments.Add(candidate);
    }

    private static IDrawFont ResolveFont(DrawTextSpan span, string element, float scale)
    {
        DrawTextRun primary = new(span.Font, element, span.Size * scale);
        if (!TextShaper.Default.TryShape(primary, out TextShapeResult shape) ||
            shape.GlyphIdBuffer.All(static glyph => glyph != 0))
        {
            return span.Font;
        }

        foreach (IDrawFont fallback in span.FallbackFonts)
        {
            DrawTextRun candidate = new(fallback, element, span.Size * scale);
            if (TextShaper.Default.TryShape(candidate, out TextShapeResult result) &&
                result.GlyphIdBuffer.All(static glyph => glyph != 0))
            {
                return fallback;
            }
        }
        return span.Font;
    }

    private static DrawTextSpan FindSpan(SpanRange[] ranges, int sourceIndex) =>
        ranges[FindSpanIndex(ranges, sourceIndex)].Span;

    // The ellipsis after kept text that ends at keptEnd has the style of the last kept character, or of the line start when
    // nothing is kept. Trimming measures it and the line draws it with this one span.
    private static int FindEllipsisSpanIndex(SpanRange[] ranges, int lineStart, int keptEnd) =>
        FindSpanIndex(ranges, Math.Max(lineStart, keptEnd - 1));

    private static int FindSpanIndex(SpanRange[] ranges, int sourceIndex)
    {
        for (int index = 0; index < ranges.Length; index++)
        {
            SpanRange range = ranges[index];
            if (sourceIndex >= range.Start && sourceIndex < range.End)
            {
                return index;
            }
        }
        return ranges.Length - 1;
    }

    private static DrawTextDirection ResolveBaseDirection(string text, DrawTextDirection requested)
    {
        if (requested != DrawTextDirection.Auto)
        {
            return requested;
        }
        return UnicodeBidiEngine.GetBaseDirection(text) == UnicodeTextDirection.RightToLeft
            ? DrawTextDirection.RightToLeft
            : DrawTextDirection.LeftToRight;
    }

    private static DrawTextDirection MapDirection(UnicodeTextDirection direction) =>
        direction == UnicodeTextDirection.RightToLeft
            ? DrawTextDirection.RightToLeft
            : DrawTextDirection.LeftToRight;

    private static UnicodeLineWrapMode MapWrapping(DrawTextWrapping wrapping) =>
        wrapping switch
        {
            DrawTextWrapping.NoWrap => UnicodeLineWrapMode.NoWrap,
            DrawTextWrapping.Word => UnicodeLineWrapMode.Word,
            DrawTextWrapping.Character => UnicodeLineWrapMode.Character,
            DrawTextWrapping.WordWithOverflow => UnicodeLineWrapMode.WordWithOverflow,
            _ => throw new ArgumentOutOfRangeException(nameof(wrapping))
        };

    private static float ResolveAlignmentOffset(
        DrawTextAlignment alignment,
        DrawTextDirection direction,
        float layoutWidth,
        float contentWidth)
    {
        float remaining = MathF.Max(0, layoutWidth - contentWidth);
        return alignment switch
        {
            DrawTextAlignment.Center => remaining / 2,
            DrawTextAlignment.Right => remaining,
            DrawTextAlignment.Left => 0,
            DrawTextAlignment.End when direction == DrawTextDirection.LeftToRight => remaining,
            DrawTextAlignment.Start when direction == DrawTextDirection.RightToLeft => remaining,
            _ => 0
        };
    }

    private static float ResolveJustification(
        float layoutWidth,
        float contentWidth,
        Fragment[] fragments)
    {
        int spaces = fragments.Count(static fragment => fragment.IsWhitespace);
        return spaces == 0 ? 0 : MathF.Max(0, layoutWidth - contentWidth) / spaces;
    }
}
