using Cerneala.Language.Text;

namespace Cerneala.Language.Features;

internal enum CernealaCompletionItemKind
{
    Element,
    Property,
    Event,
    Value,
    Keyword,
    Type,
    Resource,
    Variable,
    Function,
    Parameter
}

internal sealed record CernealaCompletionItem(
    string Label,
    string InsertText,
    TextSpan ReplacementSpan,
    CernealaCompletionItemKind Kind,
    string Detail,
    string SortText,
    string? TypeMetadataName = null,
    string? MemberName = null);

internal sealed record CernealaResolvedCompletion(
    string Signature,
    string? DeclaringType,
    string? Documentation,
    bool IsDeprecated,
    string? AssemblyName);

internal sealed record CernealaSignatureParameter(string Label, string? Documentation = null);

internal sealed record CernealaSignature(
    string Label,
    IReadOnlyList<CernealaSignatureParameter> Parameters,
    string? Documentation = null);

internal sealed record CernealaSignatureHelp(
    IReadOnlyList<CernealaSignature> Signatures,
    int ActiveSignature,
    int ActiveParameter);
