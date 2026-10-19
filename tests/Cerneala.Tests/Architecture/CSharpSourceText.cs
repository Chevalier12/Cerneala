namespace Cerneala.Tests.Architecture;

// Blanks comments and string or character literals in C# source text, keeping line breaks, so that a text scan
// for forbidden terms sees only code. The scan reads the original text, so escape sequences are recognized after
// earlier characters have been blanked, and the code inside interpolation holes stays visible.
internal static class CSharpSourceText
{
    internal static string RemoveCommentsAndStringLiterals(string text)
    {
        char[] searchableText = text.ToCharArray();
        ScanCode(text, searchableText, 0, isInterpolationHole: false);
        return new string(searchableText);
    }

    // Returns the index after the scanned code. In an interpolation hole the scan stops at the '}' that closes the
    // hole or at the ':' that starts its format text, and returns that index.
    private static int ScanCode(string text, char[] searchableText, int start, bool isInterpolationHole)
    {
        int nesting = 0;
        int i = start;
        while (i < text.Length)
        {
            char current = text[i];
            char next = i + 1 < text.Length ? text[i + 1] : '\0';

            if (current == '/' && next == '/')
            {
                i = ClearUntilLineEnd(text, searchableText, i);
                continue;
            }

            if (current == '/' && next == '*')
            {
                i = ClearBlockComment(text, searchableText, i);
                continue;
            }

            if (TryClearStringLiteral(text, searchableText, i, out int literalEnd))
            {
                i = literalEnd;
                continue;
            }

            if (current == '\'')
            {
                i = ClearCharacterLiteral(text, searchableText, i);
                continue;
            }

            if (isInterpolationHole)
            {
                if (current is '(' or '[' or '{')
                {
                    nesting++;
                }
                else if (current is ')' or ']')
                {
                    nesting--;
                }
                else if (current == '}')
                {
                    if (nesting == 0)
                    {
                        return i;
                    }

                    nesting--;
                }
                else if (current == ':' && nesting == 0)
                {
                    if (next != ':')
                    {
                        return i;
                    }

                    i++;
                }
            }

            i++;
        }

        return i;
    }

    private static int ClearUntilLineEnd(string text, char[] searchableText, int start)
    {
        int i = start;
        for (; i < text.Length && text[i] is not '\r' and not '\n'; i++)
        {
            searchableText[i] = ' ';
        }

        return i;
    }

    private static int ClearBlockComment(string text, char[] searchableText, int start)
    {
        for (int i = start; i < text.Length; i++)
        {
            if (i > start + 2 && text[i - 1] == '*' && text[i] == '/')
            {
                searchableText[i] = ' ';
                return i + 1;
            }

            ClearNonLineEnding(text, searchableText, i);
        }

        return text.Length;
    }

    // Recognizes "...", @"...", $"...", $@"...", @$"...", """...""" and $$"""...""" at start.
    private static bool TryClearStringLiteral(string text, char[] searchableText, int start, out int end)
    {
        int i = start;
        bool isVerbatim = false;
        if (i < text.Length && text[i] == '@')
        {
            isVerbatim = true;
            i++;
        }

        int dollarCount = CountRun(text, i, '$');
        i += dollarCount;
        if (!isVerbatim && i < text.Length && text[i] == '@')
        {
            isVerbatim = true;
            i++;
        }

        int quoteCount = CountRun(text, i, '"');
        if (quoteCount == 0)
        {
            end = start;
            return false;
        }

        bool isRaw = !isVerbatim && quoteCount >= 3;
        int delimiterLength = isRaw ? quoteCount : 1;
        for (int j = start; j < i + delimiterLength; j++)
        {
            searchableText[j] = ' ';
        }

        end = isRaw
            ? ClearRawStringContent(text, searchableText, i + delimiterLength, quoteCount, dollarCount)
            : ClearQuotedStringContent(text, searchableText, i + 1, isVerbatim, dollarCount > 0);
        return true;
    }

    private static int ClearQuotedStringContent(string text, char[] searchableText, int start, bool isVerbatim, bool isInterpolated)
    {
        int i = start;
        while (i < text.Length)
        {
            char current = text[i];
            char next = i + 1 < text.Length ? text[i + 1] : '\0';

            if (!isVerbatim && current is '\r' or '\n')
            {
                return i;
            }

            if (!isVerbatim && current == '\\' && next is not '\r' and not '\n' and not '\0')
            {
                searchableText[i] = ' ';
                searchableText[i + 1] = ' ';
                i += 2;
                continue;
            }

            if (current == '"')
            {
                searchableText[i] = ' ';
                if (isVerbatim && next == '"')
                {
                    searchableText[i + 1] = ' ';
                    i += 2;
                    continue;
                }

                return i + 1;
            }

            if (isInterpolated && current is '{' or '}' && next == current)
            {
                searchableText[i] = ' ';
                searchableText[i + 1] = ' ';
                i += 2;
                continue;
            }

            if (isInterpolated && current == '{')
            {
                searchableText[i] = ' ';
                i = ClearInterpolationHole(text, searchableText, i + 1, 1);
                continue;
            }

            ClearNonLineEnding(text, searchableText, i);
            i++;
        }

        return i;
    }

    private static int ClearRawStringContent(string text, char[] searchableText, int start, int quoteCount, int dollarCount)
    {
        int i = start;
        while (i < text.Length)
        {
            if (text[i] == '"' && CountRun(text, i, '"') >= quoteCount)
            {
                for (int j = 0; j < quoteCount; j++)
                {
                    searchableText[i + j] = ' ';
                }

                return i + quoteCount;
            }

            if (dollarCount > 0 && text[i] == '{')
            {
                int braceCount = CountRun(text, i, '{');
                for (int j = 0; j < braceCount; j++)
                {
                    searchableText[i + j] = ' ';
                }

                i += braceCount;
                if (braceCount >= dollarCount)
                {
                    i = ClearInterpolationHole(text, searchableText, i, dollarCount);
                }

                continue;
            }

            ClearNonLineEnding(text, searchableText, i);
            i++;
        }

        return i;
    }

    // Keeps the hole's code, blanks its format text, and blanks the closing braces.
    private static int ClearInterpolationHole(string text, char[] searchableText, int start, int closingBraceCount)
    {
        int i = ScanCode(text, searchableText, start, isInterpolationHole: true);
        for (; i < text.Length && text[i] != '}'; i++)
        {
            ClearNonLineEnding(text, searchableText, i);
        }

        for (int j = 0; j < closingBraceCount && i < text.Length && text[i] == '}'; j++, i++)
        {
            searchableText[i] = ' ';
        }

        return i;
    }

    private static int ClearCharacterLiteral(string text, char[] searchableText, int start)
    {
        searchableText[start] = ' ';
        int i = start + 1;
        while (i < text.Length)
        {
            char current = text[i];
            if (current is '\r' or '\n')
            {
                return i;
            }

            searchableText[i] = ' ';
            if (current == '\\' && i + 1 < text.Length && text[i + 1] is not '\r' and not '\n')
            {
                searchableText[i + 1] = ' ';
                i += 2;
                continue;
            }

            i++;
            if (current == '\'')
            {
                return i;
            }
        }

        return i;
    }

    private static int CountRun(string text, int start, char character)
    {
        int count = 0;
        for (int i = start; i < text.Length && text[i] == character; i++)
        {
            count++;
        }

        return count;
    }

    private static void ClearNonLineEnding(string text, char[] searchableText, int index)
    {
        if (text[index] is not '\r' and not '\n')
        {
            searchableText[index] = ' ';
        }
    }
}
