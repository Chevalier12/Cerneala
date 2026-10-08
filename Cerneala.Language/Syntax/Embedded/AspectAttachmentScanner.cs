using Cerneala.Language.Text;

namespace Cerneala.Language.Syntax.Embedded;

// One `@timbre` or `@prism` of an Aspect body: the statement through ';' or
// the block through its closing '}'.
internal sealed record AspectAttachmentSyntax(
    string Keyword,
    TextSpan KeywordSpan,
    TextSpan Extent,
    int Depth,
    bool HasBlock,
    bool Terminated,
    TextSpan HeaderSpan,
    TextSpan BodySpan);

// Finds the attachments of an Aspect body. Shared by the semantic model and
// the language server's project-less (syntax) diagnostics.
internal static class AspectAttachmentScanner
{
    // Finds every `@timbre`/`@prism` of an Aspect body with its extent: the
    // statement through ';' or the block through its closing '}'. Text inside
    // an attachment is not scanned further.
    public static IReadOnlyList<AspectAttachmentSyntax> Scan(string text, int offset)
    {
        List<AspectAttachmentSyntax> attachments = new();
        int depth = 0;
        char quote = '\0';
        for (int position = 0; position < text.Length; position++)
        {
            char character = text[position];
            if (quote != '\0')
            {
                if (character == quote && text[position - 1] != '\\')
                {
                    quote = '\0';
                }

                continue;
            }

            switch (character)
            {
                case '"' or '\'':
                    quote = character;
                    continue;
                case '{':
                    depth++;
                    continue;
                case '}':
                    depth = Math.Max(0, depth - 1);
                    continue;
                case '@':
                    break;
                default:
                    continue;
            }

            string? keyword = MatchKeyword(text, position, "@timbre") ?? MatchKeyword(text, position, "@prism");
            if (keyword is null || position > 0 && (char.IsLetterOrDigit(text[position - 1]) || text[position - 1] == '_'))
            {
                continue;
            }

            int headerStart = position + keyword.Length;
            int cursor = headerStart;
            while (cursor < text.Length && char.IsWhiteSpace(text[cursor]))
            {
                cursor++;
            }

            AspectAttachmentSyntax attachment;
            if (cursor < text.Length && text[cursor] == '{')
            {
                int close = FindClosingBrace(text, cursor);
                bool closed = close >= 0;
                int end = closed ? close + 1 : text.Length;
                attachment = new AspectAttachmentSyntax(
                    keyword,
                    new TextSpan(offset + position, keyword.Length),
                    new TextSpan(offset + position, end - position),
                    depth,
                    HasBlock: true,
                    Terminated: closed,
                    HeaderSpan: new TextSpan(offset + headerStart, cursor - headerStart),
                    BodySpan: new TextSpan(offset + cursor + 1, (closed ? close : text.Length) - cursor - 1));
                position = end - 1;
            }
            else
            {
                int end = FindStatementEnd(text, headerStart, out bool terminated);
                attachment = new AspectAttachmentSyntax(
                    keyword,
                    new TextSpan(offset + position, keyword.Length),
                    new TextSpan(offset + position, (terminated ? end + 1 : end) - position),
                    depth,
                    HasBlock: false,
                    Terminated: terminated,
                    HeaderSpan: new TextSpan(offset + headerStart, end - headerStart),
                    BodySpan: default);
                position = (terminated ? end + 1 : end) - 1;
            }

            attachments.Add(attachment);
        }

        return attachments;
    }

    private static string? MatchKeyword(string text, int position, string keyword) =>
        string.CompareOrdinal(text, position, keyword, 0, keyword.Length) == 0 &&
        (position + keyword.Length >= text.Length ||
            !char.IsLetterOrDigit(text[position + keyword.Length]) && text[position + keyword.Length] != '_')
            ? keyword
            : null;

    private static int FindClosingBrace(string text, int open)
    {
        int depth = 0;
        char quote = '\0';
        for (int position = open; position < text.Length; position++)
        {
            char character = text[position];
            if (quote != '\0')
            {
                if (character == quote && text[position - 1] != '\\')
                {
                    quote = '\0';
                }
            }
            else if (character is '"' or '\'')
            {
                quote = character;
            }
            else if (character == '{')
            {
                depth++;
            }
            else if (character == '}' && --depth == 0)
            {
                return position;
            }
        }

        return -1;
    }

    // Index of the terminating ';', or of the first '{', '}' or '@' that ends
    // an unterminated statement (whitespace before it excluded).
    private static int FindStatementEnd(string text, int start, out bool terminated)
    {
        char quote = '\0';
        int parentheses = 0;
        for (int position = start; position < text.Length; position++)
        {
            char character = text[position];
            if (quote != '\0')
            {
                if (character == quote && text[position - 1] != '\\')
                {
                    quote = '\0';
                }
            }
            else if (character is '"' or '\'')
            {
                quote = character;
            }
            else if (character == '(')
            {
                parentheses++;
            }
            else if (character == ')' && parentheses > 0)
            {
                parentheses--;
            }
            else if (parentheses == 0 && character == ';')
            {
                terminated = true;
                return position;
            }
            else if (parentheses == 0 && character is '{' or '}' or '@')
            {
                terminated = false;
                return TrimEnd(text, start, position);
            }
        }

        terminated = false;
        return TrimEnd(text, start, text.Length);
    }

    private static int TrimEnd(string text, int start, int end)
    {
        while (end > start && char.IsWhiteSpace(text[end - 1]))
        {
            end--;
        }

        return end;
    }

    // Blanks the attachments in a direct text buffer, keeping line breaks so
    // positions and line numbers stay stable.
    public static string Blank(string text, int offset, IEnumerable<AspectAttachmentSyntax> attachments)
    {
        char[] buffer = text.ToCharArray();
        foreach (AspectAttachmentSyntax attachment in attachments)
        {
            for (int position = attachment.Extent.Start; position < attachment.Extent.End; position++)
            {
                int index = position - offset;
                if (buffer[index] is not ('\r' or '\n'))
                {
                    buffer[index] = ' ';
                }
            }
        }

        return new string(buffer);
    }
}
