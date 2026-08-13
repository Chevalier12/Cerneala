using Cerneala.Language.Text;

namespace Cerneala.Language.Syntax;

internal sealed class MarkupParser
{
    private readonly SourceText source;
    private readonly IReadOnlyList<SyntaxToken> tokens;
    private readonly List<SyntaxDiagnostic> diagnostics = new();
    private int position;

    public MarkupParser(SourceText source)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        tokens = new MarkupLexer(source).Lex();
    }

    public static DocumentSyntax Parse(SourceText source) => new MarkupParser(source).ParseDocument();

    public DocumentSyntax ParseDocument()
    {
        List<SyntaxNode> children = new();
        while (Current.Kind != SyntaxKind.EndOfFileToken)
        {
            int before = position;
            SyntaxNode node = ParseNode();
            children.Add(node);
            if (node is TextSyntax text &&
                text.Kind == SyntaxKind.Text &&
                !string.IsNullOrWhiteSpace(text.Token.Text))
            {
                Report("CERNEALASYN001", "Top-level text is not allowed outside the root element.", text.Span);
            }

            if (position == before)
            {
                children.Add(new ErrorSyntax(Read()));
            }
        }

        return new DocumentSyntax(source, children, tokens, diagnostics);
    }

    private SyntaxNode ParseNode()
    {
        return Current.Kind switch
        {
            SyntaxKind.CommentToken => new TextSyntax(SyntaxKind.Comment, Read()),
            SyntaxKind.CDataToken => new TextSyntax(SyntaxKind.CData, Read()),
            SyntaxKind.TextToken => new TextSyntax(SyntaxKind.Text, Read()),
            SyntaxKind.ProcessingInstructionToken => new TextSyntax(SyntaxKind.Text, Read()),
            SyntaxKind.LessThanToken when IsClosingTag() => ParseUnexpectedClosingTag(),
            SyntaxKind.LessThanToken => ParseElement(),
            _ => ParseUnexpectedToken()
        };
    }

    private ElementSyntax ParseElement()
    {
        int diagnosticBaseline = diagnostics.Count;
        int start = Current.Span.Start;
        SyntaxToken lessThan = Match(SyntaxKind.LessThanToken);
        SkipWhitespace();
        SyntaxToken name = Current.Kind == SyntaxKind.NameToken
            ? Read()
            : Missing(SyntaxKind.NameToken);
        if (name.IsMissing)
        {
            ReportElement(diagnosticBaseline, "Expected an element name.", name.Span);
        }

        List<AttributeSyntax> attributes = new();
        SyntaxToken openEnd;
        bool selfClosing = false;
        while (true)
        {
            SkipWhitespace();
            if (Current.Kind == SyntaxKind.GreaterThanToken)
            {
                openEnd = Read();
                break;
            }

            if (Current.Kind == SyntaxKind.SlashToken && Peek(1).Kind == SyntaxKind.GreaterThanToken)
            {
                openEnd = Read();
                Read();
                selfClosing = true;
                break;
            }

            if (Current.Kind is SyntaxKind.LessThanToken or SyntaxKind.EndOfFileToken)
            {
                openEnd = Missing(SyntaxKind.GreaterThanToken);
                ReportElement(diagnosticBaseline, "The opening tag is missing '>'.", openEnd.Span);
                break;
            }

            if (Current.Kind == SyntaxKind.NameToken)
            {
                attributes.Add(ParseAttribute(diagnosticBaseline));
                continue;
            }

            SyntaxToken unexpected = Read();
            ReportElement(diagnosticBaseline, "Unexpected token in an opening tag.", unexpected.Span);
        }

        List<SyntaxNode> children = new();
        SyntaxToken closeLessThan = Missing(SyntaxKind.LessThanToken);
        SyntaxToken closeSlash = Missing(SyntaxKind.SlashToken);
        SyntaxToken closeName = Missing(SyntaxKind.NameToken);
        SyntaxToken closeGreaterThan = Missing(SyntaxKind.GreaterThanToken);
        if (!selfClosing)
        {
            while (Current.Kind != SyntaxKind.EndOfFileToken)
            {
                if (IsClosingTag())
                {
                    string candidate = PeekClosingName();
                    if (!string.Equals(candidate, name.Text, StringComparison.Ordinal))
                    {
                        break;
                    }

                    closeLessThan = Read();
                    SkipWhitespace();
                    closeSlash = Match(SyntaxKind.SlashToken);
                    SkipWhitespace();
                    closeName = Current.Kind == SyntaxKind.NameToken ? Read() : Missing(SyntaxKind.NameToken);
                    SkipWhitespace();
                    if (Current.Kind == SyntaxKind.GreaterThanToken)
                    {
                        closeGreaterThan = Read();
                    }
                    else
                    {
                        closeGreaterThan = Missing(SyntaxKind.GreaterThanToken);
                        ReportElement(diagnosticBaseline, "The closing tag is missing '>'.", closeGreaterThan.Span);
                    }

                    break;
                }

                int before = position;
                children.Add(ParseNode());
                if (position == before)
                {
                    children.Add(new ErrorSyntax(Read()));
                }
            }

            if (closeLessThan.IsMissing)
            {
                int missingOffset = Current.Span.Start;
                closeLessThan = Missing(SyntaxKind.LessThanToken, missingOffset);
                closeSlash = Missing(SyntaxKind.SlashToken, missingOffset);
                closeName = Missing(SyntaxKind.NameToken, missingOffset);
                closeGreaterThan = Missing(SyntaxKind.GreaterThanToken, missingOffset);
                ReportElement(
                    diagnosticBaseline,
                    "Element '" + name.Text + "' is missing its closing tag.",
                    new TextSpan(missingOffset, 0));
            }
        }

        int end = LastConsumedEnd(start);
        SyntaxKind kind = name.Text.IndexOf('.') >= 0 ? SyntaxKind.PropertyElement : SyntaxKind.Element;
        return new ElementSyntax(
            kind,
            lessThan,
            name,
            attributes,
            openEnd,
            children,
            closeLessThan,
            closeSlash,
            closeName,
            closeGreaterThan,
            new TextSpan(start, Math.Max(0, end - start)));
    }

    private AttributeSyntax ParseAttribute(int diagnosticBaseline)
    {
        SyntaxToken name = Read();
        int start = name.Span.Start;
        SkipWhitespace();
        SyntaxToken equals = Current.Kind == SyntaxKind.EqualsToken
            ? Read()
            : Missing(SyntaxKind.EqualsToken);
        if (equals.IsMissing)
        {
            ReportElement(diagnosticBaseline, "Attribute '" + name.Text + "' requires '='.", equals.Span);
        }

        SkipWhitespace();
        SyntaxToken value = Current.Kind == SyntaxKind.StringToken
            ? Read()
            : Missing(SyntaxKind.StringToken);
        if (value.IsMissing)
        {
            ReportElement(diagnosticBaseline, "Attribute '" + name.Text + "' requires a quoted value.", value.Span);
        }
        else if (value.Text.Length < 2 || value.Text[0] != value.Text[value.Text.Length - 1])
        {
            ReportElement(diagnosticBaseline, "Attribute '" + name.Text + "' has an unterminated quote.", value.Span);
        }

        int end = Math.Max(name.Span.End, Math.Max(equals.Span.End, value.Span.End));
        return new AttributeSyntax(name, equals, value, new TextSpan(start, end - start));
    }

    private SyntaxNode ParseUnexpectedClosingTag()
    {
        int start = Current.Span.Start;
        while (Current.Kind is not SyntaxKind.GreaterThanToken and not SyntaxKind.EndOfFileToken)
        {
            Read();
        }

        if (Current.Kind == SyntaxKind.GreaterThanToken)
        {
            Read();
        }

        int end = LastConsumedEnd(start);
        TextSpan span = new(start, Math.Max(0, end - start));
        Report("CERNEALASYN001", "Unexpected closing tag.", span);
        return new ErrorSyntax(new SyntaxToken(SyntaxKind.BadToken, span, source.Substring(span)));
    }

    private SyntaxNode ParseUnexpectedToken()
    {
        SyntaxToken token = Read();
        Report("CERNEALASYN001", "Unexpected token '" + token.Text + "'.", token.Span);
        return new ErrorSyntax(token);
    }

    private bool IsClosingTag()
    {
        if (Current.Kind != SyntaxKind.LessThanToken)
        {
            return false;
        }

        int index = position + 1;
        while (PeekAbsolute(index).Kind == SyntaxKind.WhitespaceToken)
        {
            index++;
        }

        return PeekAbsolute(index).Kind == SyntaxKind.SlashToken;
    }

    private string PeekClosingName()
    {
        int index = position + 1;
        while (PeekAbsolute(index).Kind == SyntaxKind.WhitespaceToken)
        {
            index++;
        }

        if (PeekAbsolute(index).Kind == SyntaxKind.SlashToken)
        {
            index++;
        }

        while (PeekAbsolute(index).Kind == SyntaxKind.WhitespaceToken)
        {
            index++;
        }

        SyntaxToken token = PeekAbsolute(index);
        return token.Kind == SyntaxKind.NameToken ? token.Text : string.Empty;
    }

    private void SkipWhitespace()
    {
        while (Current.Kind == SyntaxKind.WhitespaceToken)
        {
            Read();
        }
    }

    private SyntaxToken Match(SyntaxKind kind)
    {
        return Current.Kind == kind ? Read() : Missing(kind);
    }

    private SyntaxToken Missing(SyntaxKind kind) => Missing(kind, Current.Span.Start);

    private static SyntaxToken Missing(SyntaxKind kind, int offset) => SyntaxToken.Missing(kind, offset);

    private SyntaxToken Read()
    {
        SyntaxToken token = Current;
        if (position < tokens.Count - 1)
        {
            position++;
        }

        return token;
    }

    private SyntaxToken Peek(int offset) => PeekAbsolute(position + offset);

    private SyntaxToken PeekAbsolute(int index) => tokens[Math.Min(index, tokens.Count - 1)];

    private SyntaxToken Current => Peek(0);

    private int LastConsumedEnd(int fallback)
    {
        return position == 0 ? fallback : tokens[Math.Min(position - 1, tokens.Count - 1)].Span.End;
    }

    private void ReportElement(int baseline, string message, TextSpan span)
    {
        if (diagnostics.Count == baseline)
        {
            Report("CERNEALASYN001", message, span);
        }
    }

    private void Report(string id, string message, TextSpan span)
    {
        int start = Math.Min(span.Start, source.Length);
        int length = Math.Min(span.Length, source.Length - start);
        diagnostics.Add(new SyntaxDiagnostic(id, message, new TextSpan(start, length)));
    }
}
