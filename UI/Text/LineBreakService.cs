using System.Globalization;
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
        float Measure(string value) => MeasureTextWidth(value, aspect, font);
        if (text.Length == 0)
        {
            return [new TextLine(string.Empty, 0)];
        }

        List<TextLine> lines = [];
        foreach (string paragraph in EnumerateParagraphs(text))
        {
            if (aspect.Wrapping == TextWrapping.NoWrap || float.IsPositiveInfinity(availableWidth) || availableWidth <= 0)
            {
                AddLine(paragraph, 0, paragraph.Length, Measure, lines);
                continue;
            }

            WrapParagraph(paragraph, availableWidth, Measure, lines);
        }

        return lines;
    }

    private static void WrapParagraph(
        string paragraph,
        float availableWidth,
        Func<string, float> measure,
        List<TextLine> lines)
    {
        if (paragraph.Length == 0)
        {
            AddLine(paragraph, 0, 0, measure, lines);
            return;
        }

        TextElement[] elements = CreateTextElements(paragraph);
        int currentIndex = 0;
        while (currentIndex < elements.Length)
        {
            int lineStart = elements[currentIndex].Start;
            int lastFittingIndex = FindLastFittingElementIndex(
                paragraph,
                elements,
                currentIndex,
                availableWidth,
                measure);

            if (lastFittingIndex == elements.Length - 1)
            {
                int trimmedEnd = TrimTrailingBreakWhitespace(paragraph, lineStart, paragraph.Length);
                AddLine(paragraph, lineStart, trimmedEnd - lineStart, measure, lines);
                break;
            }

            int breakIndex = -1;
            int breakMeasureEnd = lineStart;
            for (int i = lastFittingIndex; i >= currentIndex; i--)
            {
                if (!IsBreakOpportunityAfter(elements[i].Text))
                {
                    continue;
                }

                int measureEnd = IsBreakWhitespace(elements[i].Text)
                    ? TrimTrailingBreakWhitespace(paragraph, lineStart, elements[i].End)
                    : elements[i].End;
                if (measureEnd > lineStart)
                {
                    breakIndex = i;
                    breakMeasureEnd = measureEnd;
                    break;
                }
            }

            if (breakIndex >= currentIndex)
            {
                AddLine(paragraph, lineStart, breakMeasureEnd - lineStart, measure, lines);
                currentIndex = SkipLeadingBreakWhitespace(elements, breakIndex + 1);
            }
            else if (lastFittingIndex >= currentIndex)
            {
                int fallbackEnd = elements[lastFittingIndex].End;
                AddLine(paragraph, lineStart, fallbackEnd - lineStart, measure, lines);
                currentIndex = lastFittingIndex + 1;
            }
            else
            {
                TextElement first = elements[currentIndex];
                AddLine(paragraph, first.Start, first.End - first.Start, measure, lines);
                currentIndex++;
            }
        }
    }

    private static int FindLastFittingElementIndex(
        string paragraph,
        TextElement[] elements,
        int startIndex,
        float availableWidth,
        Func<string, float> measure)
    {
        int lineStart = elements[startIndex].Start;
        int low = startIndex;
        int high = elements.Length - 1;
        int lastFittingIndex = startIndex - 1;
        while (low <= high)
        {
            int middle = low + ((high - low) / 2);
            int end = elements[middle].End;
            float width = measure(paragraph.Substring(lineStart, end - lineStart));
            if (width <= availableWidth)
            {
                lastFittingIndex = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return lastFittingIndex;
    }

    private static IEnumerable<string> EnumerateParagraphs(string text)
    {
        int start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '\r' && text[i] != '\n')
            {
                continue;
            }

            yield return text[start..i];
            if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
            {
                i++;
            }

            start = i + 1;
        }

        yield return text[start..];
    }

    public float MeasureTextWidth(string text, TextAspect aspect)
    {
        ArgumentNullException.ThrowIfNull(text);
        return MeasureTextWidth(text, GetCharacterWidth(aspect));
    }

    public float MeasureTextWidth(string text, TextAspect aspect, ResolvedTextFont font)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(font);
        DrawTextRun run = aspect.ToDrawTextRun(font, text);
        return TextShaper.Default.TryShape(run, out TextShapeResult shape)
            ? shape.AdvanceWidth
            : MeasureTextWidth(text, GetCharacterWidth(aspect));
    }

    private static float GetCharacterWidth(TextAspect aspect)
    {
        return aspect.FontSize * aspect.Scale * 0.5f;
    }

    private static float MeasureTextWidth(string text, float charWidth)
    {
        return text.Length * charWidth;
    }

    private static void AddLine(string text, int start, int length, Func<string, float> measure, List<TextLine> lines)
    {
        string line = text.Substring(start, length);
        lines.Add(new TextLine(line, measure(line)));
    }

    private static TextElement[] CreateTextElements(string text)
    {
        int[] starts = StringInfo.ParseCombiningCharacters(text);
        TextElement[] elements = new TextElement[starts.Length];
        for (int i = 0; i < starts.Length; i++)
        {
            int start = starts[i];
            int end = i + 1 < starts.Length ? starts[i + 1] : text.Length;
            elements[i] = new TextElement(start, end, text[start..end]);
        }

        return elements;
    }

    private static bool IsBreakOpportunityAfter(string textElement)
    {
        return IsBreakWhitespace(textElement) || textElement is "-" or "/" or "\\" or "," or ";" or ":";
    }

    private static bool IsBreakWhitespace(string textElement)
    {
        return textElement.Length > 0 && textElement.All(char.IsWhiteSpace);
    }

    private static int TrimTrailingBreakWhitespace(string text, int start, int end)
    {
        while (end > start && char.IsWhiteSpace(text[end - 1]))
        {
            end--;
        }

        return end;
    }

    private static int SkipLeadingBreakWhitespace(TextElement[] elements, int index)
    {
        while (index < elements.Length && IsBreakWhitespace(elements[index].Text))
        {
            index++;
        }

        return index;
    }

    private readonly record struct TextElement(int Start, int End, string Text);
}
