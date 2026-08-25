using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Cerneala.Drawing.Text;

namespace Cerneala.Drawing;

public enum DrawTextWrapping
{
    NoWrap,
    Word,
    Character
}

public enum DrawTextAlignment
{
    Start,
    Center,
    End,
    Justify
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
        float scale = 1)
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

        MaxWidth = maxWidth;
        MaxHeight = maxHeight;
        Wrapping = wrapping;
        Alignment = alignment;
        LineSpacing = lineSpacing;
        MaxLines = maxLines;
        Trimming = trimming;
        Direction = direction;
        Scale = scale;
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
        DrawRect bounds)
    {
        TextRun = textRun;
        Position = position;
        Brush = brush;
        Opacity = opacity;
        Direction = direction;
        Bounds = bounds;
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
}

public sealed class DrawTextLayoutLine
{
    private readonly ReadOnlyCollection<DrawTextLayoutRun> runs;

    internal DrawTextLayoutLine(
        DrawTextLayoutRun[] runs,
        DrawRect bounds,
        float baseline,
        DrawTextDirection direction,
        bool isTrimmed)
    {
        this.runs = Array.AsReadOnly(runs);
        Bounds = bounds;
        Baseline = baseline;
        Direction = direction;
        IsTrimmed = isTrimmed;
    }

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
                .Append(options.Scale);
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
        bool IsWhitespace);

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
            float lineHeight = ResolveLineHeight(ranges, candidate, options.Scale) * options.LineSpacing;
            bool exceedsLines = options.MaxLines > 0 && accepted.Count >= options.MaxLines;
            bool exceedsHeight = measuredHeight + lineHeight > options.MaxHeight;
            if (exceedsLines || exceedsHeight)
            {
                break;
            }

            accepted.Add(candidate);
            measuredHeight += lineHeight;
        }

        bool omitted = accepted.Count < candidates.Count;
        if (accepted.Count == 0 && candidates.Count > 0 && options.MaxLines != 0 && options.MaxHeight > 0)
        {
            accepted.Add(candidates[0]);
        }

        if (accepted.Count > 0 && options.Trimming != DrawTextTrimming.None)
        {
            int lastIndex = accepted.Count - 1;
            UnicodeLineSegment last = accepted[lastIndex];
            bool widthOverflow = float.IsFinite(options.MaxWidth) && last.Width > options.MaxWidth;
            if (omitted || widthOverflow)
            {
                DrawTextSpan ellipsisStyle = FindSpan(ranges, last.SourceStart + Math.Max(0, last.SourceLength - 1));
                accepted[lastIndex] = UnicodeLineBreakEngine.CollapseLine(
                    text,
                    last,
                    options.MaxWidth,
                    options.Trimming == DrawTextTrimming.WordEllipsis
                        ? UnicodeLineTrimMode.WordEllipsis
                        : UnicodeLineTrimMode.CharacterEllipsis,
                    Measure,
                    value => MeasureLiteral(value, ellipsisStyle, options.Scale),
                    forceEllipsis: omitted);
            }
        }

        List<DrawTextLayoutLine> lines = [];
        float y = 0;
        float maximumRight = 0;
        for (int index = 0; index < accepted.Count; index++)
        {
            UnicodeLineSegment segment = accepted[index];
            DrawTextDirection baseDirection = ResolveBaseDirection(segment.Text, options.Direction);
            float lineHeight = ResolveLineHeight(ranges, segment, options.Scale);
            float advanceHeight = lineHeight * options.LineSpacing;
            float baseline = ResolveBaseline(ranges, segment, options.Scale, lineHeight);
            bool justify = options.Alignment == DrawTextAlignment.Justify &&
                index < accepted.Count - 1 && !segment.IsTrimmed;
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
                    DrawRect runBounds = new(x, y, fragment.Width, lineHeight);
                    runs.Add(new DrawTextLayoutRun(
                        textRun,
                        new DrawPoint(x, y + baseline),
                        fragment.Brush,
                        fragment.Opacity,
                        MapDirection(fragment.Direction),
                        runBounds));
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
                segment.IsTrimmed));
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
            width += MeasureLiteral(
                text[overlapStart..overlapEnd],
                range.Span,
                scale);
        }
        return width;
    }

    private static float MeasureLiteral(string value, DrawTextSpan span, float scale)
    {
        DrawTextRun run = new(span.Font, value, span.Size * scale);
        return TextShaper.Default.TryShape(run, out TextShapeResult result)
            ? MathF.Abs(result.AdvanceWidth)
            : StringInfo.ParseCombiningCharacters(value).Length * span.Size * scale * 0.5f;
    }

    private static float ResolveLineHeight(
        SpanRange[] ranges,
        UnicodeLineSegment line,
        float scale)
    {
        float height = 0;
        int end = line.SourceStart + Math.Max(1, line.SourceLength);
        foreach (SpanRange range in ranges)
        {
            if (range.End < line.SourceStart || range.Start >= end)
            {
                continue;
            }
            DrawTextRun run = new(range.Span.Font, "Mg", range.Span.Size * scale);
            float candidate = TextShaper.Default.TryMeasureLineHeight(run, out float measured)
                ? measured
                : range.Span.Size * scale * 1.2f;
            height = MathF.Max(height, candidate);
        }
        return height > 0 ? height : ranges[0].Span.Size * scale * 1.2f;
    }

    private static float ResolveBaseline(
        SpanRange[] ranges,
        UnicodeLineSegment line,
        float scale,
        float lineHeight)
    {
        DrawTextSpan span = FindSpan(ranges, line.SourceStart);
        DrawTextRun run = new(span.Font, "Mg", span.Size * scale);
        return TextShaper.Default.TryMeasureBaseline(run, out float baseline)
            ? baseline
            : lineHeight * 0.8f;
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
        foreach (UnicodeTextElement element in UnicodeLineBreakEngine.CreateTextElements(
            text,
            segment.SourceStart,
            segment.SourceLength))
        {
            DrawTextSpan span = FindSpan(ranges, element.Start);
            IDrawFont font = ResolveFont(span, element.Text, scale);
            UnicodeTextDirection direction = UnicodeBidiEngine.GetDirection(element.Text);
            if (direction == UnicodeTextDirection.Neutral)
            {
                direction = logical.Count > 0
                    ? logical[^1].Direction
                    : baseDirection == DrawTextDirection.RightToLeft
                        ? UnicodeTextDirection.RightToLeft
                        : UnicodeTextDirection.LeftToRight;
            }
            AddOrMerge(
                logical,
                new Fragment(
                    element.Text,
                    font,
                    span.Size * scale,
                    span.Brush,
                    span.Opacity,
                    direction,
                    MeasureLiteral(element.Text, span, scale),
                    UnicodeLineBreakEngine.IsBreakWhitespace(element.Text)),
                preserveClusters);
        }

        if (segment.IsTrimmed && segment.Text.EndsWith(UnicodeLineBreakEngine.Ellipsis, StringComparison.Ordinal))
        {
            DrawTextSpan span = FindSpan(ranges, Math.Max(segment.SourceStart, sourceEnd - 1));
            UnicodeTextDirection direction = logical.Count > 0
                ? logical[^1].Direction
                : baseDirection == DrawTextDirection.RightToLeft
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
                    false),
                preserveClusters);
        }

        if (baseDirection == DrawTextDirection.RightToLeft)
        {
            logical.Reverse();
        }
        return logical.ToArray();
    }

    private static void AddOrMerge(List<Fragment> fragments, Fragment candidate, bool preserveClusters)
    {
        if (!preserveClusters && fragments.Count > 0)
        {
            Fragment previous = fragments[^1];
            if (ReferenceEquals(previous.Font, candidate.Font) &&
                ReferenceEquals(previous.Brush, candidate.Brush) &&
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

    private static DrawTextSpan FindSpan(SpanRange[] ranges, int sourceIndex)
    {
        foreach (SpanRange range in ranges)
        {
            if (sourceIndex >= range.Start && sourceIndex < range.End)
            {
                return range.Span;
            }
        }
        return ranges[^1].Span;
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
