using Cerneala.Language.Text;

namespace Cerneala.Language.Features;

internal sealed record CernealaFormattingOptions(int TabSize, bool InsertSpaces)
{
    public string Indent(int depth) => InsertSpaces
        ? new string(' ', Math.Max(0, depth) * Math.Max(1, TabSize))
        : new string('\t', Math.Max(0, depth));
}

internal sealed record CernealaFormattingEdit(TextSpan Span, string NewText);

internal sealed record CernealaCodeActionDiagnostic(string Id, TextSpan Span);

internal sealed record CernealaAdditionalDocument(string Path, SourceText Text);

internal sealed record CernealaCodeAction(
    string Title,
    string Kind,
    bool IsPreferred,
    IReadOnlyList<string> DiagnosticIds,
    IReadOnlyList<CernealaTextEdit> Edits);
