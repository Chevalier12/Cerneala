using System.Globalization;

namespace Cerneala.UI.Text;

/// <summary>
/// WPF-like word and line boundaries over UTF-16 offsets. Results never split a text element.
/// </summary>
internal static class TextWordBoundaries
{
    private enum CharacterClass
    {
        LineBreak,
        Whitespace,
        Word,
        Other,
    }

    public static int GetNextWordStart(string text, int position)
    {
        text ??= string.Empty;
        int[] bounds = GetBounds(text);
        int count = bounds.Length - 1;
        int index = IndexOfElementContaining(bounds, Math.Clamp(position, 0, text.Length));
        if (index >= count)
        {
            return text.Length;
        }

        CharacterClass current = Classify(text, bounds[index]);
        if (current == CharacterClass.LineBreak)
        {
            return bounds[index + 1];
        }

        if (current != CharacterClass.Whitespace)
        {
            while (index < count && Classify(text, bounds[index]) == current)
            {
                index++;
            }
        }

        while (index < count && Classify(text, bounds[index]) == CharacterClass.Whitespace)
        {
            index++;
        }

        return bounds[index];
    }

    public static int GetPreviousWordStart(string text, int position)
    {
        text ??= string.Empty;
        int[] bounds = GetBounds(text);
        int index = IndexOfBoundaryAtOrAfter(bounds, Math.Clamp(position, 0, text.Length));
        if (index == 0)
        {
            return 0;
        }

        if (Classify(text, bounds[index - 1]) == CharacterClass.LineBreak)
        {
            return bounds[index - 1];
        }

        while (index > 0 && Classify(text, bounds[index - 1]) == CharacterClass.Whitespace)
        {
            index--;
        }

        if (index > 0)
        {
            CharacterClass previous = Classify(text, bounds[index - 1]);
            if (previous != CharacterClass.LineBreak)
            {
                while (index > 0 && Classify(text, bounds[index - 1]) == previous)
                {
                    index--;
                }
            }
        }

        return bounds[index];
    }

    /// <summary>
    /// Returns the range selected by a double click: the run of the character class under
    /// <paramref name="position"/> plus the whitespace after it. A line break or empty text gives an empty range.
    /// </summary>
    public static (int Start, int End) GetWordRange(string text, int position)
    {
        text ??= string.Empty;
        position = Math.Clamp(position, 0, text.Length);
        int[] bounds = GetBounds(text);
        int count = bounds.Length - 1;
        int index = IndexOfElementContaining(bounds, position);
        if (index >= count || Classify(text, bounds[index]) == CharacterClass.LineBreak)
        {
            index--;
        }

        if (index < 0 || Classify(text, bounds[index]) == CharacterClass.LineBreak)
        {
            return (position, position);
        }

        CharacterClass current = Classify(text, bounds[index]);
        int first = index;
        while (first > 0 && Classify(text, bounds[first - 1]) == current)
        {
            first--;
        }

        int last = index + 1;
        while (last < count && Classify(text, bounds[last]) == current)
        {
            last++;
        }

        if (current != CharacterClass.Whitespace)
        {
            while (last < count && Classify(text, bounds[last]) == CharacterClass.Whitespace)
            {
                last++;
            }
        }

        return (bounds[first], bounds[last]);
    }

    /// <summary>
    /// Returns the line that contains <paramref name="position"/>, without its line break.
    /// </summary>
    public static (int Start, int End) GetLineRange(string text, int position)
    {
        text ??= string.Empty;
        int[] bounds = GetBounds(text);
        int count = bounds.Length - 1;
        int index = IndexOfElementContaining(bounds, Math.Clamp(position, 0, text.Length));
        int caret = index >= count ? text.Length : bounds[index];

        int start = caret == 0 ? -1 : text.LastIndexOfAny(LineBreakCharacters, caret - 1);
        int end = text.IndexOfAny(LineBreakCharacters, caret);
        return (start + 1, end < 0 ? text.Length : end);
    }

    private static readonly char[] LineBreakCharacters = ['\r', '\n'];

    // Text element starts followed by text.Length, so element i spans [bounds[i], bounds[i + 1]).
    private static int[] GetBounds(string text)
    {
        int[] starts = StringInfo.ParseCombiningCharacters(text);
        int[] bounds = new int[starts.Length + 1];
        starts.CopyTo(bounds, 0);
        bounds[starts.Length] = text.Length;
        return bounds;
    }

    // Index of the element containing position; the element count when position is at the end of the text.
    private static int IndexOfElementContaining(int[] bounds, int position)
    {
        int count = bounds.Length - 1;
        for (int i = 0; i < count; i++)
        {
            if (position < bounds[i + 1])
            {
                return i;
            }
        }

        return count;
    }

    // Index of the first boundary at or after position, so a position inside an element snaps to its end.
    private static int IndexOfBoundaryAtOrAfter(int[] bounds, int position)
    {
        for (int i = 0; i < bounds.Length; i++)
        {
            if (bounds[i] >= position)
            {
                return i;
            }
        }

        return bounds.Length - 1;
    }

    private static CharacterClass Classify(string text, int elementStart)
    {
        char first = text[elementStart];
        if (first is '\r' or '\n')
        {
            return CharacterClass.LineBreak;
        }

        if (char.IsWhiteSpace(first))
        {
            return CharacterClass.Whitespace;
        }

        if (first == '_')
        {
            return CharacterClass.Word;
        }

        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(text, elementStart);
        return category switch
        {
            UnicodeCategory.UppercaseLetter or
            UnicodeCategory.LowercaseLetter or
            UnicodeCategory.TitlecaseLetter or
            UnicodeCategory.ModifierLetter or
            UnicodeCategory.OtherLetter or
            UnicodeCategory.DecimalDigitNumber or
            UnicodeCategory.LetterNumber or
            UnicodeCategory.OtherNumber or
            UnicodeCategory.NonSpacingMark or
            UnicodeCategory.SpacingCombiningMark or
            UnicodeCategory.EnclosingMark => CharacterClass.Word,
            _ => CharacterClass.Other,
        };
    }
}
