namespace Cerneala.UI.Text;

public sealed class LineBreakService
{
    public static LineBreakService Default { get; } = new();

    public IReadOnlyList<TextLine> BreakLines(string text, TextRunStyle style, float availableWidth)
    {
        ArgumentNullException.ThrowIfNull(text);
        float charWidth = GetCharacterWidth(style);
        if (text.Length == 0)
        {
            return [new TextLine(string.Empty, 0)];
        }

        if (style.Wrapping == TextWrapping.NoWrap || float.IsPositiveInfinity(availableWidth) || availableWidth <= 0)
        {
            return [new TextLine(text, MeasureTextWidth(text, charWidth))];
        }

        int maxCharsPerLine = Math.Max(1, (int)MathF.Floor(availableWidth / charWidth));
        List<TextLine> lines = new();
        int index = 0;
        while (index < text.Length)
        {
            int count = Math.Min(maxCharsPerLine, text.Length - index);
            string line = text.Substring(index, count);
            lines.Add(new TextLine(line, MeasureTextWidth(line, charWidth)));
            index += count;
        }

        return lines;
    }

    public float MeasureTextWidth(string text, TextRunStyle style)
    {
        ArgumentNullException.ThrowIfNull(text);
        return MeasureTextWidth(text, GetCharacterWidth(style));
    }

    private static float GetCharacterWidth(TextRunStyle style)
    {
        return style.FontSize * style.Scale * 0.5f;
    }

    private static float MeasureTextWidth(string text, float charWidth)
    {
        return text.Length * charWidth;
    }
}
