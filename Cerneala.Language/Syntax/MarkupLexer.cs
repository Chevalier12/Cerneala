using Cerneala.Language.Text;

namespace Cerneala.Language.Syntax;

internal sealed class MarkupLexer
{
    private readonly SourceText source;
    private readonly string text;
    private readonly List<SyntaxToken> tokens = new();
    private int position;
    private bool insideTag;

    public MarkupLexer(SourceText source)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        text = source.ToString();
    }

    public IReadOnlyList<SyntaxToken> Lex()
    {
        while (position < source.Length)
        {
            if (!insideTag)
            {
                LexContent();
            }
            else
            {
                LexTag();
            }
        }

        tokens.Add(new SyntaxToken(
            SyntaxKind.EndOfFileToken,
            new TextSpan(source.Length, 0),
            string.Empty));
        return tokens;
    }

    private void LexContent()
    {
        if (StartsWith("<!--"))
        {
            LexDelimited(SyntaxKind.CommentToken, "-->");
            return;
        }

        if (StartsWith("<![CDATA["))
        {
            LexDelimited(SyntaxKind.CDataToken, "]]>");
            return;
        }

        if (StartsWith("<?"))
        {
            LexDelimited(SyntaxKind.ProcessingInstructionToken, "?>");
            return;
        }

        if (IsMarkupOpening(position))
        {
            Add(SyntaxKind.LessThanToken, position, 1);
            position++;
            insideTag = true;
            return;
        }

        int start = position;
        position++;
        while (position < source.Length &&
            !StartsWith("<!--") &&
            !StartsWith("<![CDATA[") &&
            !StartsWith("<?") &&
            !IsMarkupOpening(position))
        {
            position++;
        }

        Add(SyntaxKind.TextToken, start, position - start);
    }

    private void LexTag()
    {
        int start = position;
        char character = text[position];
        if (char.IsWhiteSpace(character))
        {
            position++;
            while (position < source.Length && char.IsWhiteSpace(text[position]))
            {
                position++;
            }

            Add(SyntaxKind.WhitespaceToken, start, position - start);
            return;
        }

        switch (character)
        {
            case '>':
                Add(SyntaxKind.GreaterThanToken, position, 1);
                position++;
                insideTag = false;
                return;
            case '<':
                Add(SyntaxKind.LessThanToken, position, 1);
                position++;
                return;
            case '/':
                Add(SyntaxKind.SlashToken, position, 1);
                position++;
                return;
            case '=':
                Add(SyntaxKind.EqualsToken, position, 1);
                position++;
                return;
            case '\'' or '"':
                LexString(character);
                return;
        }

        if (IsNameStart(character))
        {
            position++;
            while (position < source.Length && IsNamePart(text[position]))
            {
                position++;
            }

            Add(SyntaxKind.NameToken, start, position - start);
            return;
        }

        Add(SyntaxKind.BadToken, position, 1);
        position++;
    }

    private void LexString(char quote)
    {
        int start = position++;
        while (position < source.Length)
        {
            if (text[position++] == quote)
            {
                break;
            }
        }

        Add(SyntaxKind.StringToken, start, position - start);
    }

    private void LexDelimited(SyntaxKind kind, string terminator)
    {
        int start = position;
        int end = text.IndexOf(terminator, position, StringComparison.Ordinal);
        position = end < 0 ? source.Length : end + terminator.Length;
        Add(kind, start, position - start);
    }

    private bool IsMarkupOpening(int offset)
    {
        if (offset >= source.Length || text[offset] != '<' || offset + 1 >= source.Length)
        {
            return false;
        }

        char next = text[offset + 1];
        return IsNameStart(next) || next is '/' or '!' or '?';
    }

    private bool StartsWith(string value)
    {
        return position + value.Length <= source.Length &&
            string.CompareOrdinal(text, position, value, 0, value.Length) == 0;
    }

    private void Add(SyntaxKind kind, int start, int length)
    {
        TextSpan span = new(start, length);
        tokens.Add(new SyntaxToken(kind, span, source.Substring(span)));
    }

    private static bool IsNameStart(char character) =>
        char.IsLetter(character) || character is '_' or ':';

    private static bool IsNamePart(char character) =>
        IsNameStart(character) || char.IsDigit(character) || character is '-' or '.';
}
