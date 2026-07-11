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
            int lastBreakIndex = -1;
            int lastBreakMeasureEnd = lineStart;
            int lastBreakWrapEnd = lineStart;
            bool emitted = false;

            for (int i = currentIndex; i < elements.Length; i++)
            {
                int elementEnd = elements[i].End;
                float width = measure(paragraph.Substring(lineStart, elementEnd - lineStart));
                if (width > availableWidth)
                {
                    if (lastBreakIndex >= currentIndex && lastBreakMeasureEnd > lineStart)
                    {
                        AddLine(paragraph, lineStart, lastBreakMeasureEnd - lineStart, measure, lines);
                        currentIndex = SkipLeadingBreakWhitespace(elements, IndexAtOrAfter(elements, lastBreakWrapEnd));
                    }
                    else if (i > currentIndex)
                    {
                        int fallbackEnd = elements[i - 1].End;
                        AddLine(paragraph, lineStart, fallbackEnd - lineStart, measure, lines);
                        currentIndex = i;
                    }
                    else
                    {
                        AddLine(paragraph, elements[i].Start, elements[i].End - elements[i].Start, measure, lines);
                        currentIndex = i + 1;
                    }

                    emitted = true;
                    break;
                }

                if (IsBreakOpportunityAfter(elements[i].Text))
                {
                    lastBreakIndex = i;
                    lastBreakMeasureEnd = IsBreakWhitespace(elements[i].Text)
                        ? TrimTrailingBreakWhitespace(paragraph, lineStart, elementEnd)
                        : elementEnd;
                    lastBreakWrapEnd = elementEnd;
                }
            }

            if (!emitted)
            {
                AddLine(paragraph, lineStart, TrimTrailingBreakWhitespace(paragraph, lineStart, paragraph.Length) - lineStart, measure, lines);
                break;
            }
        }
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

    private static int IndexAtOrAfter(TextElement[] elements, int position)
    {
        int index = 0;
        while (index < elements.Length && elements[index].End <= position)
        {
            index++;
        }

        return index;
    }

    private readonly record struct TextElement(int Start, int End, string Text);
}
