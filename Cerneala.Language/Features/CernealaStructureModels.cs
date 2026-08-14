using Cerneala.Language.Text;

namespace Cerneala.Language.Features;

internal enum CernealaSemanticTokenKind
{
    ElementType,
    Property,
    AttachedProperty,
    Event,
    Namespace,
    Resource,
    BindingSource,
    BindingMember,
    Directive,
    Motion,
    Prism
}

[Flags]
internal enum CernealaSemanticTokenModifiers
{
    None = 0,
    Declaration = 1
}

internal sealed record CernealaSemanticToken(
    TextSpan Span,
    CernealaSemanticTokenKind Kind,
    CernealaSemanticTokenModifiers Modifiers);

internal enum CernealaOutlineSymbolKind
{
    Root,
    Element,
    ResourceGroup,
    Resource,
    Template,
    Aspect,
    Motion,
    Prism
}

internal sealed record CernealaOutlineSymbol(
    string Name,
    string? Detail,
    CernealaOutlineSymbolKind Kind,
    TextSpan Range,
    TextSpan SelectionRange,
    IReadOnlyList<CernealaOutlineSymbol> Children);

internal sealed record CernealaWorkspaceSymbol(
    string Name,
    string Detail,
    CernealaOutlineSymbolKind Kind,
    string Path,
    TextSpan Span);

internal sealed record CernealaFoldingRange(TextSpan Span, string? Kind);

internal sealed class CernealaSelectionRange
{
    public CernealaSelectionRange(TextSpan span, CernealaSelectionRange? parent)
    {
        Span = span;
        Parent = parent;
    }

    public TextSpan Span { get; }

    public CernealaSelectionRange? Parent { get; }
}
