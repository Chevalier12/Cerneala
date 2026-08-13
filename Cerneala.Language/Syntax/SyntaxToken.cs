using Cerneala.Language.Text;

namespace Cerneala.Language.Syntax;

internal sealed class SyntaxToken
{
    public SyntaxToken(SyntaxKind kind, TextSpan span, string text, bool isMissing = false)
    {
        Kind = kind;
        Span = span;
        Text = text;
        IsMissing = isMissing;
    }

    public SyntaxKind Kind { get; }

    public TextSpan Span { get; }

    public string Text { get; }

    public bool IsMissing { get; }

    public static SyntaxToken Missing(SyntaxKind kind, int offset) =>
        new(kind, new TextSpan(offset, 0), string.Empty, isMissing: true);
}
