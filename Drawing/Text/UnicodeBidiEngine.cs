using System.Globalization;

namespace Cerneala.Drawing.Text;

internal enum UnicodeTextDirection
{
    Neutral,
    LeftToRight,
    RightToLeft
}

internal readonly record struct UnicodeDirectionalRun(
    int Start,
    int Length,
    UnicodeTextDirection Direction);

// The bidirectional character types of Unicode UAX #9 (table 4) that its implicit rules use. Explicit embeddings,
// overrides and isolates are not supported, so their formatting characters are neutrals like every other type.
internal enum UnicodeBidiClass : byte
{
    Neutral,
    LeftToRight,
    RightToLeft,
    ArabicLetter,
    EuropeanNumber,
    EuropeanSeparator,
    EuropeanTerminator,
    ArabicNumber,
    CommonSeparator
}

// The resolved embedding level of every UTF-16 index of a text, and the level of the paragraph that holds it. The index
// equal to the text length belongs to the paragraph that a line starting at the end of the text is in.
internal sealed class UnicodeBidiLevels(byte[] levels, byte[] paragraphLevels)
{
    public byte GetLevel(int index) => levels[index];

    public byte GetParagraphLevel(int index) => paragraphLevels[index];
}

internal static class UnicodeBidiEngine
{
    // Resolves the embedding levels of a text with the implicit rules of Unicode UAX #9. A hard line break ('\r' or '\n',
    // as in the line breaker) ends a paragraph. A null direction resolves the direction of each paragraph from its text
    // (rules P2 and P3, with the first letter or digit as GetBaseDirection does). Rule L1 depends on the line breaks, so
    // the caller applies it.
    public static UnicodeBidiLevels ResolveLevels(string text, UnicodeTextDirection? direction)
    {
        ArgumentNullException.ThrowIfNull(text);
        byte[] levels = new byte[text.Length + 1];
        byte[] paragraphLevels = new byte[text.Length + 1];
        int start = 0;
        while (start < text.Length)
        {
            int end = text.AsSpan(start).IndexOfAny('\r', '\n');
            end = end < 0 ? text.Length : start + end + 1;
            if (end < text.Length && text[end - 1] == '\r' && text[end] == '\n')
            {
                end++;
            }

            ResolveParagraph(text, start, end - start, direction, levels, paragraphLevels);
            start = end;
        }

        // A line that starts at the end of the text is in the paragraph after the last hard line break, or in the last
        // paragraph when the text does not end with one.
        bool endsParagraph = text.Length == 0 || text[^1] is '\r' or '\n';
        byte trailingLevel = endsParagraph
            ? ParagraphLevel(direction ?? UnicodeTextDirection.LeftToRight)
            : paragraphLevels[text.Length - 1];
        levels[text.Length] = trailingLevel;
        paragraphLevels[text.Length] = trailingLevel;
        return new UnicodeBidiLevels(levels, paragraphLevels);
    }

    // The bidirectional type of a text element, from its first scalar.
    public static UnicodeBidiClass GetBidiClass(string textElement)
    {
        if (string.IsNullOrEmpty(textElement))
        {
            return UnicodeBidiClass.Neutral;
        }

        int scalar = char.ConvertToUtf32(textElement, 0);
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(textElement, 0);
        if (category == UnicodeCategory.DecimalDigitNumber)
        {
            if (IsArabicNumber(scalar))
            {
                return UnicodeBidiClass.ArabicNumber;
            }

            if (IsEuropeanNumber(scalar))
            {
                return UnicodeBidiClass.EuropeanNumber;
            }
        }

        switch (GetDirection(textElement))
        {
            case UnicodeTextDirection.RightToLeft:
                return IsArabicLetter(scalar) ? UnicodeBidiClass.ArabicLetter : UnicodeBidiClass.RightToLeft;
            case UnicodeTextDirection.LeftToRight:
                return UnicodeBidiClass.LeftToRight;
        }

        return scalar switch
        {
            0x066B or 0x066C => UnicodeBidiClass.ArabicNumber,
            '+' or '-' or 0x207A or 0x207B or 0x208A or 0x208B or 0x2212 or 0xFB29 or 0xFE62 or 0xFE63 or 0xFF0B or 0xFF0D =>
                UnicodeBidiClass.EuropeanSeparator,
            ',' or '.' or '/' or ':' or 0x00A0 or 0x060C or 0x202F or 0x2044 or 0xFE50 or 0xFE52 or 0xFE55 or 0xFF0C or
                0xFF0E or 0xFF0F or 0xFF1A => UnicodeBidiClass.CommonSeparator,
            '#' or '%' or 0x00B0 or 0x00B1 or 0x066A or (>= 0x2030 and <= 0x2034) or 0x2213 or 0xFF03 or 0xFF05 =>
                UnicodeBidiClass.EuropeanTerminator,
            _ when category == UnicodeCategory.CurrencySymbol => UnicodeBidiClass.EuropeanTerminator,
            _ => UnicodeBidiClass.Neutral
        };
    }

    // One paragraph is one isolating run sequence at the paragraph level, so sos and eos are the paragraph direction.
    private static void ResolveParagraph(
        string text,
        int start,
        int length,
        UnicodeTextDirection? direction,
        byte[] levels,
        byte[] paragraphLevels)
    {
        UnicodeTextElement[] elements = UnicodeLineBreakEngine.CreateTextElements(text, start, length);
        byte paragraphLevel = ParagraphLevel(direction ?? GetBaseDirection(text.Substring(start, length)));
        UnicodeBidiClass embedding = (paragraphLevel & 1) == 0 ? UnicodeBidiClass.LeftToRight : UnicodeBidiClass.RightToLeft;
        UnicodeBidiClass[] types = new UnicodeBidiClass[elements.Length];
        for (int index = 0; index < elements.Length; index++)
        {
            types[index] = GetBidiClass(elements[index].Text);
        }

        ResolveWeakTypes(types, embedding);
        ResolveNeutralTypes(types, embedding);
        for (int index = 0; index < elements.Length; index++)
        {
            // Rules I1 and I2.
            byte level = (types[index], (paragraphLevel & 1) == 0) switch
            {
                (UnicodeBidiClass.RightToLeft, true) => (byte)(paragraphLevel + 1),
                (UnicodeBidiClass.RightToLeft, false) => paragraphLevel,
                (UnicodeBidiClass.LeftToRight, true) => paragraphLevel,
                (_, true) => (byte)(paragraphLevel + 2),
                _ => (byte)(paragraphLevel + 1)
            };
            int end = index + 1 < elements.Length ? elements[index + 1].Start : start + length;
            levels.AsSpan(elements[index].Start, end - elements[index].Start).Fill(level);
        }

        paragraphLevels.AsSpan(start, length).Fill(paragraphLevel);
    }

    // Rules W1 to W7. W1 has nothing to do: a nonspacing mark is part of the text element of its base character.
    private static void ResolveWeakTypes(UnicodeBidiClass[] types, UnicodeBidiClass embedding)
    {
        // W2: a European number after an Arabic letter is an Arabic number. W3: an Arabic letter is right-to-left.
        UnicodeBidiClass lastStrong = embedding;
        for (int index = 0; index < types.Length; index++)
        {
            switch (types[index])
            {
                case UnicodeBidiClass.LeftToRight or UnicodeBidiClass.RightToLeft:
                    lastStrong = types[index];
                    break;
                case UnicodeBidiClass.ArabicLetter:
                    lastStrong = UnicodeBidiClass.ArabicLetter;
                    types[index] = UnicodeBidiClass.RightToLeft;
                    break;
                case UnicodeBidiClass.EuropeanNumber when lastStrong == UnicodeBidiClass.ArabicLetter:
                    types[index] = UnicodeBidiClass.ArabicNumber;
                    break;
            }
        }

        // W4: one separator between two numbers of the same type takes their type ("1.5", "1,000").
        for (int index = 1; index + 1 < types.Length; index++)
        {
            UnicodeBidiClass before = types[index - 1];
            if (before == types[index + 1] &&
                ((types[index] == UnicodeBidiClass.EuropeanSeparator && before == UnicodeBidiClass.EuropeanNumber) ||
                 (types[index] == UnicodeBidiClass.CommonSeparator &&
                  before is UnicodeBidiClass.EuropeanNumber or UnicodeBidiClass.ArabicNumber)))
            {
                types[index] = before;
            }
        }

        // W5: terminators next to a European number are part of it ("50%", "$5").
        for (int index = 0; index < types.Length; index++)
        {
            if (types[index] != UnicodeBidiClass.EuropeanTerminator)
            {
                continue;
            }

            int end = index;
            while (end < types.Length && types[end] == UnicodeBidiClass.EuropeanTerminator)
            {
                end++;
            }

            if ((index > 0 && types[index - 1] == UnicodeBidiClass.EuropeanNumber) ||
                (end < types.Length && types[end] == UnicodeBidiClass.EuropeanNumber))
            {
                types.AsSpan(index, end - index).Fill(UnicodeBidiClass.EuropeanNumber);
            }

            index = end - 1;
        }

        // W6: the other separators and terminators are neutrals. W7: a European number after left-to-right text is
        // left-to-right.
        lastStrong = embedding;
        for (int index = 0; index < types.Length; index++)
        {
            switch (types[index])
            {
                case UnicodeBidiClass.EuropeanSeparator or UnicodeBidiClass.EuropeanTerminator or UnicodeBidiClass.CommonSeparator:
                    types[index] = UnicodeBidiClass.Neutral;
                    break;
                case UnicodeBidiClass.LeftToRight or UnicodeBidiClass.RightToLeft:
                    lastStrong = types[index];
                    break;
                case UnicodeBidiClass.EuropeanNumber when lastStrong == UnicodeBidiClass.LeftToRight:
                    types[index] = UnicodeBidiClass.LeftToRight;
                    break;
            }
        }
    }

    // Rules N1 and N2: neutrals between text of one direction take that direction, the others take the paragraph
    // direction. Numbers count as right-to-left text.
    private static void ResolveNeutralTypes(UnicodeBidiClass[] types, UnicodeBidiClass embedding)
    {
        for (int index = 0; index < types.Length; index++)
        {
            if (types[index] != UnicodeBidiClass.Neutral)
            {
                continue;
            }

            int end = index;
            while (end < types.Length && types[end] == UnicodeBidiClass.Neutral)
            {
                end++;
            }

            UnicodeBidiClass before = index == 0 ? embedding : StrongDirection(types[index - 1]);
            UnicodeBidiClass after = end == types.Length ? embedding : StrongDirection(types[end]);
            types.AsSpan(index, end - index).Fill(before == after ? before : embedding);
            index = end - 1;
        }
    }

    private static UnicodeBidiClass StrongDirection(UnicodeBidiClass type) =>
        type == UnicodeBidiClass.LeftToRight ? UnicodeBidiClass.LeftToRight : UnicodeBidiClass.RightToLeft;

    private static byte ParagraphLevel(UnicodeTextDirection direction) =>
        direction == UnicodeTextDirection.RightToLeft ? (byte)1 : (byte)0;

    public static UnicodeTextDirection GetBaseDirection(string? text)
    {
        foreach (UnicodeTextElement element in UnicodeLineBreakEngine.CreateTextElements(text ?? string.Empty))
        {
            UnicodeTextDirection direction = GetDirection(element.Text);
            if (direction != UnicodeTextDirection.Neutral)
            {
                return direction;
            }
        }
        return UnicodeTextDirection.LeftToRight;
    }

    public static IReadOnlyList<UnicodeDirectionalRun> GetDirectionalRuns(string? text)
    {
        string value = text ?? string.Empty;
        UnicodeTextElement[] elements = UnicodeLineBreakEngine.CreateTextElements(value);
        if (elements.Length == 0)
        {
            return [];
        }

        List<UnicodeDirectionalRun> runs = [];
        UnicodeTextDirection current = NormalizeNeutral(
            GetDirection(elements[0].Text),
            UnicodeTextDirection.LeftToRight);
        int start = elements[0].Start;
        for (int index = 1; index < elements.Length; index++)
        {
            UnicodeTextDirection direction = NormalizeNeutral(GetDirection(elements[index].Text), current);
            if (direction == current)
            {
                continue;
            }

            runs.Add(new UnicodeDirectionalRun(start, elements[index].Start - start, current));
            start = elements[index].Start;
            current = direction;
        }

        runs.Add(new UnicodeDirectionalRun(start, value.Length - start, current));
        return runs;
    }

    public static UnicodeTextDirection GetDirection(string textElement)
    {
        if (string.IsNullOrEmpty(textElement))
        {
            return UnicodeTextDirection.Neutral;
        }

        int scalar = char.ConvertToUtf32(textElement, 0);
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(textElement, 0);
        if (IsRightToLeft(scalar) && (IsLetter(category) || category is
            UnicodeCategory.LetterNumber or
            UnicodeCategory.DecimalDigitNumber))
        {
            return UnicodeTextDirection.RightToLeft;
        }

        return IsLetter(category) || category == UnicodeCategory.DecimalDigitNumber
            ? UnicodeTextDirection.LeftToRight
            : UnicodeTextDirection.Neutral;
    }

    private static bool IsLetter(UnicodeCategory category) =>
        category is
            UnicodeCategory.LowercaseLetter or
            UnicodeCategory.UppercaseLetter or
            UnicodeCategory.TitlecaseLetter or
            UnicodeCategory.ModifierLetter or
            UnicodeCategory.OtherLetter;

    private static UnicodeTextDirection NormalizeNeutral(
        UnicodeTextDirection direction,
        UnicodeTextDirection fallback) =>
        direction == UnicodeTextDirection.Neutral ? fallback : direction;

    // Arabic-Indic and Hanifi Rohingya digits.
    private static bool IsArabicNumber(int scalar) =>
        (scalar >= 0x0660 && scalar <= 0x0669) ||
        (scalar >= 0x10D30 && scalar <= 0x10D39);

    // ASCII, Extended Arabic-Indic, fullwidth, mathematical and segmented digits. Other decimal digits, as the
    // Devanagari ones, are letters of their script.
    private static bool IsEuropeanNumber(int scalar) =>
        (scalar >= '0' && scalar <= '9') ||
        (scalar >= 0x06F0 && scalar <= 0x06F9) ||
        (scalar >= 0xFF10 && scalar <= 0xFF19) ||
        (scalar >= 0x1D7CE && scalar <= 0x1D7FF) ||
        (scalar >= 0x1FBF0 && scalar <= 0x1FBF9);

    // The Arabic, Syriac and Thaana blocks and the Arabic presentation forms and mathematical symbols.
    private static bool IsArabicLetter(int scalar) =>
        (scalar >= 0x0600 && scalar <= 0x07BF) ||
        (scalar >= 0x0860 && scalar <= 0x08FF) ||
        (scalar >= 0xFB50 && scalar <= 0xFDFF) ||
        (scalar >= 0xFE70 && scalar <= 0xFEFF) ||
        (scalar >= 0x1EE00 && scalar <= 0x1EEFF);

    private static bool IsRightToLeft(int scalar) =>
        (scalar >= 0x0590 && scalar <= 0x08FF) ||
        (scalar >= 0xFB1D && scalar <= 0xFDFF) ||
        (scalar >= 0xFE70 && scalar <= 0xFEFF) ||
        (scalar >= 0x10800 && scalar <= 0x10FFF) ||
        (scalar >= 0x1E800 && scalar <= 0x1EEFF);
}
