using Cerneala.Language.Text;

namespace Cerneala.Language.Syntax;

internal sealed class SyntaxDiagnostic
{
    public SyntaxDiagnostic(string id, string message, TextSpan span)
    {
        Id = id;
        Message = message;
        Span = span;
    }

    public string Id { get; }

    public string Message { get; }

    public TextSpan Span { get; }
}
