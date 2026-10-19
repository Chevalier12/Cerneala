namespace Cerneala.Tests.Architecture;

// Blanks comments and string or character literals in C# source text, keeping line breaks, so that a text scan
// for forbidden terms sees only code.
internal static class CSharpSourceText
{
    internal static string RemoveCommentsAndStringLiterals(string text)
    {
        char[] searchableText = text.ToCharArray();

        for (int i = 0; i < searchableText.Length; i++)
        {
            if (searchableText[i] == '/' && i + 1 < searchableText.Length)
            {
                if (searchableText[i + 1] == '/')
                {
                    i = ClearUntilLineEnd(searchableText, i);
                    continue;
                }

                if (searchableText[i + 1] == '*')
                {
                    i = ClearBlockComment(searchableText, i);
                    continue;
                }
            }

            if (searchableText[i] == '@' && i + 1 < searchableText.Length && searchableText[i + 1] == '"')
            {
                i = ClearVerbatimStringLiteral(searchableText, i);
                continue;
            }

            if (searchableText[i] == '"' && CountQuoteRun(searchableText, i) >= 3)
            {
                i = ClearRawStringLiteral(searchableText, i, CountQuoteRun(searchableText, i));
                continue;
            }

            if (searchableText[i] == '"')
            {
                i = ClearStringLiteral(searchableText, i);
                continue;
            }

            if (searchableText[i] == '\'')
            {
                i = ClearCharacterLiteral(searchableText, i);
            }
        }

        return new string(searchableText);
    }

    private static int ClearUntilLineEnd(char[] text, int start)
    {
        int i = start;
        for (; i < text.Length && text[i] is not '\r' and not '\n'; i++)
        {
            text[i] = ' ';
        }

        return i;
    }

    private static int ClearBlockComment(char[] text, int start)
    {
        for (int i = start; i < text.Length; i++)
        {
            if (i + 1 < text.Length && text[i] == '*' && text[i + 1] == '/')
            {
                text[i] = ' ';
                text[i + 1] = ' ';
                return i + 1;
            }

            ClearNonLineEnding(text, i);
        }

        return text.Length - 1;
    }

    private static int ClearStringLiteral(char[] text, int start)
    {
        for (int i = start; i < text.Length; i++)
        {
            bool escaped = i > start && text[i - 1] == '\\' && !IsEscapedEscape(text, i - 1, start);
            char current = text[i];
            ClearNonLineEnding(text, i);

            if (current == '"' && i > start && !escaped)
            {
                return i;
            }

            if (current is '\r' or '\n')
            {
                return i;
            }
        }

        return text.Length - 1;
    }

    private static int ClearVerbatimStringLiteral(char[] text, int start)
    {
        text[start] = ' ';
        text[start + 1] = ' ';

        for (int i = start + 2; i < text.Length; i++)
        {
            if (text[i] == '"' && i + 1 < text.Length && text[i + 1] == '"')
            {
                text[i] = ' ';
                text[i + 1] = ' ';
                i++;
                continue;
            }

            char current = text[i];
            ClearNonLineEnding(text, i);

            if (current == '"')
            {
                return i;
            }
        }

        return text.Length - 1;
    }

    private static int ClearRawStringLiteral(char[] text, int start, int quoteRunLength)
    {
        for (int i = start; i < text.Length; i++)
        {
            if (i > start && CountQuoteRun(text, i) >= quoteRunLength)
            {
                for (int j = 0; j < quoteRunLength; j++)
                {
                    text[i + j] = ' ';
                }

                return i + quoteRunLength - 1;
            }

            ClearNonLineEnding(text, i);
        }

        return text.Length - 1;
    }

    private static int CountQuoteRun(char[] text, int start)
    {
        int quoteCount = 0;
        for (int i = start; i < text.Length && text[i] == '"'; i++)
        {
            quoteCount++;
        }

        return quoteCount;
    }

    private static int ClearCharacterLiteral(char[] text, int start)
    {
        for (int i = start; i < text.Length; i++)
        {
            bool escaped = i > start && text[i - 1] == '\\' && !IsEscapedEscape(text, i - 1, start);
            char current = text[i];
            ClearNonLineEnding(text, i);

            if (current == '\'' && i > start && !escaped)
            {
                return i;
            }

            if (current is '\r' or '\n')
            {
                return i;
            }
        }

        return text.Length - 1;
    }

    private static bool IsEscapedEscape(char[] text, int slashIndex, int literalStart)
    {
        int slashCount = 0;
        for (int i = slashIndex; i > literalStart && text[i] == '\\'; i--)
        {
            slashCount++;
        }

        return slashCount % 2 == 0;
    }

    private static void ClearNonLineEnding(char[] text, int index)
    {
        if (text[index] is not '\r' and not '\n')
        {
            text[index] = ' ';
        }
    }
}
