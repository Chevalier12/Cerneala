using Cerneala.Drawing;
using Cerneala.Drawing.Text;

namespace Cerneala.UI.Text;

public sealed class LineBreakService
{
    public static LineBreakService Default { get; } = new();

    public IReadOnlyList<TextLine> BreakLines(
        string text,
        TextAspect aspect,
        ResolvedTextFont font,
        float availableWidth)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(font);
        IReadOnlyList<UnicodeLineSegment> segments = UnicodeLineBreakEngine.BreakLines(
            text,
            availableWidth,
            aspect.Wrapping == TextWrapping.NoWrap
                ? UnicodeLineWrapMode.NoWrap
                : UnicodeLineWrapMode.Word,
            (start, length) => MeasureTextWidth(text.Substring(start, length), aspect, font));

        if (aspect.Trimming == TextTrimming.None || float.IsPositiveInfinity(availableWidth))
        {
            return segments.Select(static segment => new TextLine(segment.Text, segment.Width)).ToArray();
        }

        return segments
            .Select(segment => CollapseSegment(text, segment, aspect, font, availableWidth, forceEllipsis: false))
            .ToArray();
    }

    internal TextLine CollapseLine(
        TextLine line,
        TextAspect aspect,
        ResolvedTextFont font,
        float availableWidth,
        bool forceEllipsis)
    {
        ArgumentNullException.ThrowIfNull(font);
        if (aspect.Trimming == TextTrimming.None || float.IsPositiveInfinity(availableWidth))
        {
            return line;
        }

        UnicodeLineSegment segment = new(0, line.Text.Length, line.Text, line.Width);
        return CollapseSegment(line.Text, segment, aspect, font, availableWidth, forceEllipsis);
    }

    public float MeasureTextWidth(string text, TextAspect aspect)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Length * aspect.FontSize * aspect.Scale * 0.5f;
    }

    public float MeasureTextWidth(string text, TextAspect aspect, ResolvedTextFont font)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(font);
        DrawTextRun run = aspect.ToDrawTextRun(font, text);
        return TextShaper.Default.TryShape(run, out TextShapeResult shape)
            ? shape.AdvanceWidth
            : MeasureTextWidth(text, aspect);
    }

    private TextLine CollapseSegment(
        string source,
        UnicodeLineSegment segment,
        TextAspect aspect,
        ResolvedTextFont font,
        float availableWidth,
        bool forceEllipsis)
    {
        UnicodeLineSegment collapsed = UnicodeLineBreakEngine.CollapseLine(
            source,
            segment,
            availableWidth,
            aspect.Trimming == TextTrimming.WordEllipsis
                ? UnicodeLineTrimMode.WordEllipsis
                : UnicodeLineTrimMode.CharacterEllipsis,
            (start, length) => MeasureTextWidth(source.Substring(start, length), aspect, font),
            value => MeasureTextWidth(value, aspect, font),
            forceEllipsis);
        return new TextLine(collapsed.Text, collapsed.Width);
    }
}
