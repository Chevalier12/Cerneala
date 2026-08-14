using Cerneala.Language.Text;

namespace Cerneala.Language.Features;

internal sealed record CernealaLocation(string Path, TextSpan Span);

internal sealed record CernealaHoverInfo(
    string Signature,
    string Category,
    string? DeclaringType,
    string? InheritedFrom,
    string? DefaultValue,
    string? Documentation,
    string? DiagnosticExplanation,
    string? AssemblyName,
    bool IsDeprecated);

internal enum CernealaDocumentHighlightKind
{
    Text,
    Read,
    Write
}

internal sealed record CernealaDocumentHighlight(TextSpan Span, CernealaDocumentHighlightKind Kind);

internal sealed record CernealaPrepareRenameResult(
    TextSpan? Span,
    string? Placeholder,
    string? Error)
{
    public bool CanRename => Span is not null && Error is null;
}

internal sealed record CernealaTextEdit(string Path, TextSpan Span, string NewText);

internal sealed record CernealaRenameResult(
    IReadOnlyList<CernealaTextEdit> Edits,
    string? Error)
{
    public bool Succeeded => Error is null;
}
