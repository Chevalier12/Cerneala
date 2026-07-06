using System.Globalization;

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
        List<TextLine> lines = [];
        int[] textElementStarts = StringInfo.ParseCombiningCharacters(text);
        int lineStart = 0;
        int lineLength = 0;

        for (int i = 0; i < textElementStarts.Length; i++)
        {
            int elementStart = textElementStarts[i];
            int elementEnd = i + 1 < textElementStarts.Length ? textElementStarts[i + 1] : text.Length;
            int elementLength = elementEnd - elementStart;

            if (lineLength > 0 && lineLength + elementLength > maxCharsPerLine)
            {
                AddLine(text, lineStart, lineLength, charWidth, lines);
                lineStart = elementStart;
                lineLength = elementLength;
                continue;
            }

            lineLength += elementLength;
        }

        AddLine(text, lineStart, lineLength, charWidth, lines);
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

    private static void AddLine(string text, int start, int length, float charWidth, List<TextLine> lines)
    {
        string line = text.Substring(start, length);
        lines.Add(new TextLine(line, MeasureTextWidth(line, charWidth)));
    }
}
