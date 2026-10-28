using System.Globalization;

namespace Cerneala.Drawing.Text;

internal enum UnicodeLineWrapMode
{
    NoWrap,
    Word,
    Character,
    // As Word, but a word that does not fit stays whole on its line and overflows, up to the next break opportunity.
    WordWithOverflow
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

    // measureEllipsis gives the width of the ellipsis after kept text that ends at the given source index. The ellipsis has
    // the style of the last kept character, or of the line start when nothing is kept, so with styled spans its width
    // depends on where the kept text ends.
    public static UnicodeLineSegment CollapseLine(
        string source,
        UnicodeLineSegment line,
        float availableWidth,
        UnicodeLineTrimMode mode,
        Func<int, int, float> measure,
        Func<int, float> measureEllipsis,
        bool forceEllipsis)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(measure);
        ArgumentNullException.ThrowIfNull(measureEllipsis);

        if (!forceEllipsis && line.Width <= availableWidth)
        {
            return line;
        }

        if (!float.IsFinite(availableWidth) || availableWidth <= 0)
        {
            return new UnicodeLineSegment(line.SourceStart, 0, string.Empty, 0, true);
        }

        float lineStartEllipsisWidth = measureEllipsis(line.SourceStart);
        if (lineStartEllipsisWidth > availableWidth)
        {
            return new UnicodeLineSegment(line.SourceStart, 0, string.Empty, 0, true);
        }

        int sourceEnd = TrimTrailingBreakWhitespace(
            source,
            line.SourceStart,
            line.SourceStart + line.SourceLength);
        if (sourceEnd == line.SourceStart)
        {
            return new UnicodeLineSegment(line.SourceStart, 0, Ellipsis, lineStartEllipsisWidth, true);
        }

        UnicodeTextElement[] elements = CreateTextElements(
            source,
            line.SourceStart,
            sourceEnd - line.SourceStart);
        bool Fits(int end) =>
            measure(line.SourceStart, end - line.SourceStart) + measureEllipsis(end) <= availableWidth;
        int fittingCount = FindFittingPrefixCount(
            elements,
            line.SourceStart,
            availableWidth,
            measure,
            measureEllipsis);
        if (fittingCount == 0)
        {
            return new UnicodeLineSegment(line.SourceStart, 0, Ellipsis, lineStartEllipsisWidth, true);
        }

        int prefixEnd = elements[fittingCount - 1].End;
        if (mode == UnicodeLineTrimMode.WordEllipsis)
        {
            int wordBoundary = FindLastWordBoundary(source, elements, fittingCount, line.SourceStart, Fits);
            if (wordBoundary > line.SourceStart)
            {
                prefixEnd = wordBoundary;
            }
        }

        string collapsed = source[line.SourceStart..prefixEnd] + Ellipsis;
        return new UnicodeLineSegment(
            line.SourceStart,
            prefixEnd - line.SourceStart,
            collapsed,
            measure(line.SourceStart, prefixEnd - line.SourceStart) + measureEllipsis(prefixEnd),
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

    // Break white space is the white space after which a line can break, and which a line drops at its end and skips at its
    // start: every char.IsWhiteSpace character except the no-break spaces U+00A0, U+2007 and U+202F. Those are glue
    // (UAX #14 class GL): no break before or after them, and they stay in the line as text.
    public static bool IsBreakWhitespace(string textElement) =>
        textElement.Length > 0 && textElement.All(IsBreakWhitespace);

    private static bool IsBreakWhitespace(char value) =>
        char.IsWhiteSpace(value) && !IsGlue(value);

    private static bool IsGlue(char value) =>
        value is ' ' or ' ' or ' ';

    // A break after the element, unless glue follows an element that is not break white space or a hyphen (UAX #14 rule
    // LB12a: no break before glue, except after spaces and hyphens).
    private static bool IsBreakOpportunityAfter(UnicodeTextElement[] elements, int index) =>
        IsBreakOpportunityAfter(elements[index].Text) &&
        (index + 1 >= elements.Length ||
            !IsGlue(elements[index + 1].Text[0]) ||
            IsBreakWhitespace(elements[index].Text) ||
            elements[index].Text == "-");

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
                text,
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
            bool wordBreaks = mode is UnicodeLineWrapMode.Word or UnicodeLineWrapMode.WordWithOverflow;
            if (wordBreaks)
            {
                for (int index = lastFittingIndex; index >= currentIndex; index--)
                {
                    if (!IsBreakOpportunityAfter(elements, index))
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
            else if (wordBreaks &&
                lastFittingIndex >= currentIndex &&
                lastFittingIndex + 1 < elements.Length &&
                IsBreakWhitespace(elements[lastFittingIndex + 1].Text))
            {
                int fallbackEnd = elements[lastFittingIndex].End;
                AddLine(text, lineStart, fallbackEnd - lineStart, measure, lines);
                currentIndex = SkipLeadingBreakWhitespace(elements, lastFittingIndex + 1);
            }
            else if (mode == UnicodeLineWrapMode.WordWithOverflow)
            {
                // No break opportunity in the fitting range: the word overflows to its own end.
                int overflowIndex = FindOverflowBreakIndex(text, elements, Math.Max(lastFittingIndex + 1, currentIndex), lineStart);
                if (overflowIndex < 0)
                {
                    int paragraphEnd = paragraphStart + paragraphLength;
                    int trimmedEnd = TrimTrailingBreakWhitespace(text, lineStart, paragraphEnd);
                    AddLine(text, lineStart, trimmedEnd - lineStart, measure, lines);
                    break;
                }

                int overflowEnd = IsBreakWhitespace(elements[overflowIndex].Text)
                    ? TrimTrailingBreakWhitespace(text, lineStart, elements[overflowIndex].End)
                    : elements[overflowIndex].End;
                AddLine(text, lineStart, overflowEnd - lineStart, measure, lines);
                currentIndex = SkipLeadingBreakWhitespace(elements, overflowIndex + 1);
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

    private static int FindOverflowBreakIndex(
        string text,
        UnicodeTextElement[] elements,
        int startIndex,
        int lineStart)
    {
        for (int index = startIndex; index < elements.Length; index++)
        {
            if (!IsBreakOpportunityAfter(elements, index))
            {
                continue;
            }

            int measureEnd = IsBreakWhitespace(elements[index].Text)
                ? TrimTrailingBreakWhitespace(text, lineStart, elements[index].End)
                : elements[index].End;
            if (measureEnd > lineStart)
            {
                return index;
            }
        }

        return -1;
    }

    private static int FindLastFittingElementIndex(
        string text,
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
            // The white space at the end of a line is dropped when the line breaks there, so it does not have to fit:
            // a line that is exactly as wide as the available width keeps the word that ends at its edge.
            int end = IsBreakWhitespace(elements[middle].Text)
                ? TrimTrailingBreakWhitespace(text, lineStart, elements[middle].End)
                : elements[middle].End;
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

    // The number of leading elements that fit together with the ellipsis after them. Kept text never ends in break white
    // space, because the line drops it before the ellipsis, so only the ends of the other elements are candidates. The
    // ellipsis width depends on where the kept text ends, so the search runs over the groups of consecutive candidates
    // with the same ellipsis width, from the last group back: within a group the kept width grows with every candidate,
    // so a binary search finds the last one that fits.
    private static int FindFittingPrefixCount(
        UnicodeTextElement[] elements,
        int lineStart,
        float availableWidth,
        Func<int, int, float> measure,
        Func<int, float> measureEllipsis)
    {
        List<int> candidates = [];
        for (int index = 0; index < elements.Length; index++)
        {
            if (!IsBreakWhitespace(elements[index].Text))
            {
                candidates.Add(index);
            }
        }

        float[] ellipsisWidths = new float[candidates.Count];
        for (int index = 0; index < candidates.Count; index++)
        {
            ellipsisWidths[index] = measureEllipsis(elements[candidates[index]].End);
        }

        int groupEnd = candidates.Count;
        while (groupEnd > 0)
        {
            float ellipsisWidth = ellipsisWidths[groupEnd - 1];
            int groupStart = groupEnd - 1;
            while (groupStart > 0 && ellipsisWidths[groupStart - 1] == ellipsisWidth)
            {
                groupStart--;
            }

            int low = groupStart;
            int high = groupEnd - 1;
            int fitting = -1;
            while (low <= high)
            {
                int middle = low + ((high - low) / 2);
                int end = elements[candidates[middle]].End;
                if (measure(lineStart, end - lineStart) + ellipsisWidth <= availableWidth)
                {
                    fitting = middle;
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            if (fitting >= 0)
            {
                return candidates[fitting] + 1;
            }

            groupEnd = groupStart;
        }

        return 0;
    }

    // The last word boundary in the fitting prefix after which the kept text fits with its ellipsis. A boundary follows a
    // break opportunity, and comes before break white space, a line break or the end of the text; so the end of a line
    // that ends at a break is a boundary, and the end of a line broken inside a word is not.
    private static int FindLastWordBoundary(
        string text,
        UnicodeTextElement[] elements,
        int fittingCount,
        int lineStart,
        Func<int, bool> fits)
    {
        for (int index = fittingCount - 1; index >= 0; index--)
        {
            int elementEnd = elements[index].End;
            bool boundaryAfter = IsBreakOpportunityAfter(elements, index);
            bool whitespaceAfter = elementEnd == text.Length || IsBreakWhitespace(text[elementEnd]);
            if (!boundaryAfter && !whitespaceAfter)
            {
                continue;
            }

            int end = IsBreakWhitespace(elements[index].Text)
                ? TrimTrailingBreakWhitespace(text, lineStart, elementEnd)
                : elementEnd;
            if (end > lineStart && fits(end))
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
        while (end > start && IsBreakWhitespace(text[end - 1]))
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
