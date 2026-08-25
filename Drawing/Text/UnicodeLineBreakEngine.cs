using System.Globalization;

namespace Cerneala.Drawing.Text;

internal enum UnicodeLineWrapMode
{
    NoWrap,
    Word,
    Character
}

internal enum UnicodeLineTrimMode
{
    CharacterEllipsis,
    WordEllipsis
}

internal readonly record struct UnicodeLineSegment(
    int SourceStart,
    int SourceLength,
    string Text,
    float Width,
    bool IsTrimmed = false);

internal readonly record struct UnicodeTextElement(
    int Start,
    int Length,
    string Text)
{
    public int End => Start + Length;
}

internal static class UnicodeLineBreakEngine
{
    internal const string Ellipsis = "…";

    public static IReadOnlyList<UnicodeLineSegment> BreakLines(
        string text,
        float availableWidth,
        UnicodeLineWrapMode mode,
        Func<int, int, float> measure)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(measure);

        if (text.Length == 0)
        {
            return [new UnicodeLineSegment(0, 0, string.Empty, 0)];
        }

        List<UnicodeLineSegment> lines = [];
        foreach ((int start, int length) in EnumerateParagraphs(text))
        {
            if (mode == UnicodeLineWrapMode.NoWrap ||
                float.IsPositiveInfinity(availableWidth) ||
                availableWidth <= 0)
            {
                AddLine(text, start, length, measure, lines);
                continue;
            }

            WrapParagraph(text, start, length, availableWidth, mode, measure, lines);
        }

        return lines;
    }

    public static UnicodeLineSegment CollapseLine(
        string source,
        UnicodeLineSegment line,
        float availableWidth,
        UnicodeLineTrimMode mode,
        Func<int, int, float> measure,
        Func<string, float> measureLiteral,
        bool forceEllipsis)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(measure);
        ArgumentNullException.ThrowIfNull(measureLiteral);

        if (!forceEllipsis && line.Width <= availableWidth)
        {
            return line;
        }

        if (!float.IsFinite(availableWidth) || availableWidth <= 0)
        {
            return new UnicodeLineSegment(line.SourceStart, 0, string.Empty, 0, true);
        }

        float ellipsisWidth = measureLiteral(Ellipsis);
        if (ellipsisWidth > availableWidth)
        {
            return new UnicodeLineSegment(line.SourceStart, 0, string.Empty, 0, true);
        }

        int sourceEnd = TrimTrailingBreakWhitespace(
            source,
            line.SourceStart,
            line.SourceStart + line.SourceLength);
        if (sourceEnd == line.SourceStart)
        {
            return new UnicodeLineSegment(line.SourceStart, 0, Ellipsis, ellipsisWidth, true);
        }

        UnicodeTextElement[] elements = CreateTextElements(
            source,
            line.SourceStart,
            sourceEnd - line.SourceStart);
        int fittingCount = FindFittingPrefixCount(
            elements,
            availableWidth,
            measure,
            measureLiteral);
        if (fittingCount == 0)
        {
            return new UnicodeLineSegment(line.SourceStart, 0, Ellipsis, ellipsisWidth, true);
        }

        int prefixEnd = elements[fittingCount - 1].End;
        if (mode == UnicodeLineTrimMode.WordEllipsis)
        {
            int wordBoundary = FindLastWordBoundary(source, elements, fittingCount, line.SourceStart);
            if (wordBoundary > line.SourceStart)
            {
                prefixEnd = wordBoundary;
            }
        }

        prefixEnd = TrimTrailingBreakWhitespace(source, line.SourceStart, prefixEnd);
        string collapsed = source[line.SourceStart..prefixEnd] + Ellipsis;
        return new UnicodeLineSegment(
            line.SourceStart,
            prefixEnd - line.SourceStart,
            collapsed,
            measure(line.SourceStart, prefixEnd - line.SourceStart) + ellipsisWidth,
            true);
    }

    public static UnicodeTextElement[] CreateTextElements(
        string text,
        int start = 0,
        int length = -1)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (length < 0)
        {
            length = text.Length - start;
        }
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        if (start > text.Length - length)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }
        if (length == 0)
        {
            return [];
        }

        string slice = text.Substring(start, length);
        int[] relativeStarts = StringInfo.ParseCombiningCharacters(slice);
        UnicodeTextElement[] elements = new UnicodeTextElement[relativeStarts.Length];
        for (int index = 0; index < relativeStarts.Length; index++)
        {
            int elementStart = start + relativeStarts[index];
            int elementEnd = index + 1 < relativeStarts.Length
                ? start + relativeStarts[index + 1]
                : start + length;
            elements[index] = new UnicodeTextElement(
                elementStart,
                elementEnd - elementStart,
                text[elementStart..elementEnd]);
        }

        return elements;
    }

    public static bool IsBreakOpportunityAfter(string textElement) =>
        IsBreakWhitespace(textElement) ||
        textElement is "-" or "/" or "\\" or "," or ";" or ":";

    public static bool IsBreakWhitespace(string textElement) =>
        textElement.Length > 0 && textElement.All(char.IsWhiteSpace);

    private static void WrapParagraph(
        string text,
        int paragraphStart,
        int paragraphLength,
        float availableWidth,
        UnicodeLineWrapMode mode,
        Func<int, int, float> measure,
        List<UnicodeLineSegment> lines)
    {
        if (paragraphLength == 0)
        {
            AddLine(text, paragraphStart, 0, measure, lines);
            return;
        }

        UnicodeTextElement[] elements = CreateTextElements(text, paragraphStart, paragraphLength);
        int currentIndex = 0;
        while (currentIndex < elements.Length)
        {
            int lineStart = elements[currentIndex].Start;
            int lastFittingIndex = FindLastFittingElementIndex(
                elements,
                currentIndex,
                availableWidth,
                measure);

            if (lastFittingIndex == elements.Length - 1)
            {
                int paragraphEnd = paragraphStart + paragraphLength;
                int trimmedEnd = TrimTrailingBreakWhitespace(text, lineStart, paragraphEnd);
                AddLine(text, lineStart, trimmedEnd - lineStart, measure, lines);
                break;
            }

            int breakIndex = -1;
            int breakMeasureEnd = lineStart;
            if (mode == UnicodeLineWrapMode.Word)
            {
                for (int index = lastFittingIndex; index >= currentIndex; index--)
                {
                    if (!IsBreakOpportunityAfter(elements[index].Text))
                    {
                        continue;
                    }

                    int measureEnd = IsBreakWhitespace(elements[index].Text)
                        ? TrimTrailingBreakWhitespace(text, lineStart, elements[index].End)
                        : elements[index].End;
                    if (measureEnd > lineStart)
                    {
                        breakIndex = index;
                        breakMeasureEnd = measureEnd;
                        break;
                    }
                }
            }

            if (breakIndex >= currentIndex)
            {
                AddLine(text, lineStart, breakMeasureEnd - lineStart, measure, lines);
                currentIndex = SkipLeadingBreakWhitespace(elements, breakIndex + 1);
            }
            else if (mode == UnicodeLineWrapMode.Word &&
                lastFittingIndex >= currentIndex &&
                lastFittingIndex + 1 < elements.Length &&
                IsBreakWhitespace(elements[lastFittingIndex + 1].Text))
            {
                int fallbackEnd = elements[lastFittingIndex].End;
                AddLine(text, lineStart, fallbackEnd - lineStart, measure, lines);
                currentIndex = SkipLeadingBreakWhitespace(elements, lastFittingIndex + 1);
            }
            else if (lastFittingIndex >= currentIndex)
            {
                int fallbackEnd = elements[lastFittingIndex].End;
                AddLine(text, lineStart, fallbackEnd - lineStart, measure, lines);
                currentIndex = lastFittingIndex + 1;
            }
            else
            {
                UnicodeTextElement first = elements[currentIndex];
                AddLine(text, first.Start, first.Length, measure, lines);
                currentIndex++;
            }
        }
    }

    private static int FindLastFittingElementIndex(
        UnicodeTextElement[] elements,
        int startIndex,
        float availableWidth,
        Func<int, int, float> measure)
    {
        int lineStart = elements[startIndex].Start;
        int low = startIndex;
        int high = elements.Length - 1;
        int lastFittingIndex = startIndex - 1;
        while (low <= high)
        {
            int middle = low + ((high - low) / 2);
            int end = elements[middle].End;
            if (measure(lineStart, end - lineStart) <= availableWidth)
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

    private static int FindFittingPrefixCount(
        UnicodeTextElement[] elements,
        float availableWidth,
        Func<int, int, float> measure,
        Func<string, float> measureLiteral)
    {
        int low = 0;
        int high = elements.Length - 1;
        int fittingCount = 0;
        float ellipsisWidth = measureLiteral(Ellipsis);
        while (low <= high)
        {
            int middle = low + ((high - low) / 2);
            int length = elements[middle].End - elements[0].Start;
            if (measure(elements[0].Start, length) + ellipsisWidth <= availableWidth)
            {
                fittingCount = middle + 1;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return fittingCount;
    }

    private static int FindLastWordBoundary(
        string text,
        UnicodeTextElement[] elements,
        int fittingCount,
        int lineStart)
    {
        for (int index = fittingCount - 1; index >= 0; index--)
        {
            bool boundaryAfter = IsBreakOpportunityAfter(elements[index].Text);
            bool nextIsWhitespace = index + 1 < elements.Length &&
                IsBreakWhitespace(elements[index + 1].Text);
            if (!boundaryAfter && !nextIsWhitespace)
            {
                continue;
            }

            int end = IsBreakWhitespace(elements[index].Text)
                ? TrimTrailingBreakWhitespace(text, lineStart, elements[index].End)
                : elements[index].End;
            if (end > lineStart)
            {
                return end;
            }
        }

        return lineStart;
    }

    private static IEnumerable<(int Start, int Length)> EnumerateParagraphs(string text)
    {
        int start = 0;
        for (int index = 0; index < text.Length; index++)
        {
            if (text[index] != '\r' && text[index] != '\n')
            {
                continue;
            }

            yield return (start, index - start);
            if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
            {
                index++;
            }
            start = index + 1;
        }

        yield return (start, text.Length - start);
    }

    private static void AddLine(
        string text,
        int start,
        int length,
        Func<int, int, float> measure,
        List<UnicodeLineSegment> lines)
    {
        lines.Add(new UnicodeLineSegment(
            start,
            length,
            text.Substring(start, length),
            measure(start, length)));
    }

    private static int TrimTrailingBreakWhitespace(string text, int start, int end)
    {
        while (end > start && char.IsWhiteSpace(text[end - 1]))
        {
            end--;
        }
        return end;
    }

    private static int SkipLeadingBreakWhitespace(UnicodeTextElement[] elements, int index)
    {
        while (index < elements.Length && IsBreakWhitespace(elements[index].Text))
        {
            index++;
        }
        return index;
    }
}
