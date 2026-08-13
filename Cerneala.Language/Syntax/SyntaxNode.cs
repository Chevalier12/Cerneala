using Cerneala.Language.Text;

namespace Cerneala.Language.Syntax;

internal abstract class SyntaxNode
{
    protected SyntaxNode(SyntaxKind kind, TextSpan span)
    {
        Kind = kind;
        Span = span;
    }

    public SyntaxKind Kind { get; }

    public TextSpan Span { get; }
}

internal sealed class DocumentSyntax : SyntaxNode
{
    public DocumentSyntax(
        SourceText source,
        IReadOnlyList<SyntaxNode> children,
        IReadOnlyList<SyntaxToken> tokens,
        IReadOnlyList<SyntaxDiagnostic> diagnostics) : base(SyntaxKind.Document, new TextSpan(0, source.Length))
    {
        Source = source;
        Children = children;
        Tokens = tokens;
        Diagnostics = diagnostics;
    }

    public SourceText Source { get; }

    public IReadOnlyList<SyntaxNode> Children { get; }

    public IReadOnlyList<SyntaxToken> Tokens { get; }

    public IReadOnlyList<SyntaxDiagnostic> Diagnostics { get; }

    public string ToFullString() => Source.ToString();

    public IEnumerable<ElementSyntax> DescendantElements()
    {
        foreach (SyntaxNode child in Children)
        {
            foreach (ElementSyntax element in DescendantElements(child))
            {
                yield return element;
            }
        }
    }

    private static IEnumerable<ElementSyntax> DescendantElements(SyntaxNode node)
    {
        if (node is not ElementSyntax element)
        {
            yield break;
        }

        yield return element;
        foreach (SyntaxNode child in element.Children)
        {
            foreach (ElementSyntax descendant in DescendantElements(child))
            {
                yield return descendant;
            }
        }
    }
}

internal sealed class ElementSyntax : SyntaxNode
{
    public ElementSyntax(
        SyntaxKind kind,
        SyntaxToken lessThanToken,
        SyntaxToken nameToken,
        IReadOnlyList<AttributeSyntax> attributes,
        SyntaxToken openEndToken,
        IReadOnlyList<SyntaxNode> children,
        SyntaxToken closeLessThanToken,
        SyntaxToken closeSlashToken,
        SyntaxToken closeNameToken,
        SyntaxToken closeGreaterThanToken,
        TextSpan span) : base(kind, span)
    {
        LessThanToken = lessThanToken;
        NameToken = nameToken;
        Attributes = attributes;
        OpenEndToken = openEndToken;
        Children = children;
        CloseLessThanToken = closeLessThanToken;
        CloseSlashToken = closeSlashToken;
        CloseNameToken = closeNameToken;
        CloseGreaterThanToken = closeGreaterThanToken;
    }

    public string Name => NameToken.Text;

    public SyntaxToken LessThanToken { get; }

    public SyntaxToken NameToken { get; }

    public IReadOnlyList<AttributeSyntax> Attributes { get; }

    public SyntaxToken OpenEndToken { get; }

    public IReadOnlyList<SyntaxNode> Children { get; }

    public SyntaxToken CloseLessThanToken { get; }

    public SyntaxToken CloseSlashToken { get; }

    public SyntaxToken CloseNameToken { get; }

    public SyntaxToken CloseGreaterThanToken { get; }

    public bool IsSelfClosing => OpenEndToken.Kind == SyntaxKind.SlashToken;

    public bool HasMissingTokens =>
        NameToken.IsMissing || OpenEndToken.IsMissing ||
        !IsSelfClosing && (CloseLessThanToken.IsMissing || CloseSlashToken.IsMissing ||
            CloseNameToken.IsMissing || CloseGreaterThanToken.IsMissing);
}

internal sealed class AttributeSyntax : SyntaxNode
{
    public AttributeSyntax(
        SyntaxToken nameToken,
        SyntaxToken equalsToken,
        SyntaxToken valueToken,
        TextSpan span) : base(SyntaxKind.Attribute, span)
    {
        NameToken = nameToken;
        EqualsToken = equalsToken;
        ValueToken = valueToken;
    }

    public SyntaxToken NameToken { get; }

    public SyntaxToken EqualsToken { get; }

    public SyntaxToken ValueToken { get; }
}

internal sealed class TextSyntax : SyntaxNode
{
    public TextSyntax(SyntaxKind kind, SyntaxToken token) : base(kind, token.Span)
    {
        Token = token;
    }

    public SyntaxToken Token { get; }
}

internal sealed class ErrorSyntax : SyntaxNode
{
    public ErrorSyntax(SyntaxToken token) : base(SyntaxKind.Error, token.Span)
    {
        Token = token;
    }

    public SyntaxToken Token { get; }
}
