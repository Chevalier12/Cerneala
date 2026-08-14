using Cerneala.Language.Features;
using Cerneala.Language.Semantics;
using Cerneala.Language.Text;
using Cerneala.LanguageServer.Protocol;
using Cerneala.LanguageServer.Workspace;

namespace Cerneala.LanguageServer.Features;

internal sealed class CompletionService(CernealaWorkspace workspace)
{
    private readonly CernealaCompletionService service = new();

    public Task<VersionedDocumentResult<CompletionList>?> GetCompletionsAsync(
        string uri,
        LspPosition position,
        CancellationToken cancellationToken) => workspace.RunDocumentRequestAsync(
            uri,
            (snapshot, requestCancellation) => Task.FromResult(
                CreateCompletionList(snapshot, uri, position, requestCancellation)),
            cancellationToken);

    public Task<VersionedDocumentResult<LspCompletionItem>?> ResolveAsync(
        LspCompletionItem item,
        CancellationToken cancellationToken)
    {
        CompletionItemData data = item.Data ?? throw new ArgumentException("Completion item data is missing.", nameof(item));
        return workspace.RunDocumentRequestAsync(
            data.Uri,
            (snapshot, requestCancellation) => Task.FromResult(Resolve(snapshot, item, data, requestCancellation)),
            cancellationToken);
    }

    public Task<VersionedDocumentResult<LspSignatureHelp?>?> GetSignatureHelpAsync(
        string uri,
        LspPosition position,
        CancellationToken cancellationToken) => workspace.RunDocumentRequestAsync(
            uri,
            (snapshot, requestCancellation) =>
            {
                requestCancellation.ThrowIfCancellationRequested();
                int offset = GetOffset(snapshot.Document.Text, position);
                CernealaSemanticModel? model = snapshot.IsStandalone
                    ? null
                    : snapshot.GetSemanticModels(requestCancellation).FirstOrDefault();
                CernealaSignatureHelp? result = service.GetSignatureHelp(snapshot.Document, offset, model);
                return Task.FromResult(result is null ? null : ToLsp(result));
            },
            cancellationToken);

    private CompletionList CreateCompletionList(
        WorkspaceDocumentSnapshot snapshot,
        string uri,
        LspPosition position,
        CancellationToken cancellationToken)
    {
        int offset = GetOffset(snapshot.Document.Text, position);
        IReadOnlyList<CernealaSemanticModel> models = snapshot.IsStandalone
            ? Array.Empty<CernealaSemanticModel>()
            : snapshot.GetSemanticModels(cancellationToken);
        IEnumerable<CernealaCompletionItem> items = models.Count == 0
            ? service.GetCompletions(snapshot.Document, null, offset, cancellationToken)
            : models.SelectMany(model => service.GetCompletions(snapshot.Document, model, offset, cancellationToken));
        return new CompletionList
        {
            IsIncomplete = false,
            Items = items
                .GroupBy(item => (item.Label, item.InsertText, item.ReplacementSpan))
                .Select(group => group.First())
                .OrderBy(item => item.SortText, StringComparer.Ordinal)
                .ThenBy(item => item.Label, StringComparer.Ordinal)
                .Select(item => ToLsp(snapshot.Document.Text, uri, snapshot.Version, item))
                .ToArray()
        };
    }

    private LspCompletionItem Resolve(
        WorkspaceDocumentSnapshot snapshot,
        LspCompletionItem item,
        CompletionItemData data,
        CancellationToken cancellationToken)
    {
        if (snapshot.Version != data.Version)
        {
            throw new OperationCanceledException("The completion item belongs to an older document version.");
        }

        CernealaResolvedCompletion? resolved = snapshot.GetSemanticModels(cancellationToken)
            .Select(model => service.Resolve(model, data.TypeMetadataName, data.MemberName))
            .FirstOrDefault(candidate => candidate is not null);
        if (resolved is null)
        {
            return item;
        }

        string detail = resolved.Signature;
        if (!string.IsNullOrWhiteSpace(resolved.DeclaringType))
        {
            detail += "\nDeclared by " + resolved.DeclaringType;
        }

        if (!string.IsNullOrWhiteSpace(resolved.AssemblyName))
        {
            detail += "\nAssembly: " + resolved.AssemblyName;
        }

        return new LspCompletionItem
        {
            Label = item.Label,
            Kind = item.Kind,
            Detail = detail,
            Documentation = resolved.Documentation is null
                ? null
                : new MarkupContent { Value = resolved.Documentation },
            SortText = item.SortText,
            FilterText = item.FilterText,
            TextEdit = item.TextEdit,
            Deprecated = resolved.IsDeprecated,
            Tags = resolved.IsDeprecated ? [1] : null,
            Data = item.Data
        };
    }

    private static LspCompletionItem ToLsp(
        SourceText source,
        string uri,
        long version,
        CernealaCompletionItem item) => new()
        {
            Label = item.Label,
            Kind = ToLspKind(item.Kind),
            Detail = item.Detail,
            SortText = item.SortText,
            FilterText = item.Label,
            TextEdit = new LspTextEdit
            {
                Range = ToRange(source, item.ReplacementSpan),
                NewText = item.InsertText
            },
            Data = item.TypeMetadataName is null
                ? null
                : new CompletionItemData
                {
                    Uri = uri,
                    Version = version,
                    TypeMetadataName = item.TypeMetadataName,
                    MemberName = item.MemberName
                }
        };

    private static LspSignatureHelp ToLsp(CernealaSignatureHelp help) => new()
    {
        ActiveSignature = help.ActiveSignature,
        ActiveParameter = help.ActiveParameter,
        Signatures = help.Signatures.Select(signature => new LspSignatureInformation
        {
            Label = signature.Label,
            Documentation = signature.Documentation is null
                ? null
                : new MarkupContent { Value = signature.Documentation },
            Parameters = signature.Parameters.Select(parameter => new LspParameterInformation
            {
                Label = parameter.Label,
                Documentation = parameter.Documentation is null
                    ? null
                    : new MarkupContent { Value = parameter.Documentation }
            }).ToArray()
        }).ToArray()
    };

    private static int GetOffset(SourceText source, LspPosition position) =>
        source.GetOffset(new LinePosition(position.Line, position.Character));

    private static LspRange ToRange(SourceText source, TextSpan span)
    {
        LinePosition start = source.GetLinePosition(span.Start);
        LinePosition end = source.GetLinePosition(span.End);
        return new LspRange
        {
            Start = new LspPosition { Line = start.Line, Character = start.Character },
            End = new LspPosition { Line = end.Line, Character = end.Character }
        };
    }

    private static int ToLspKind(CernealaCompletionItemKind kind) => kind switch
    {
        CernealaCompletionItemKind.Element or CernealaCompletionItemKind.Type => 7,
        CernealaCompletionItemKind.Property => 10,
        CernealaCompletionItemKind.Event => 23,
        CernealaCompletionItemKind.Value => 12,
        CernealaCompletionItemKind.Keyword => 14,
        CernealaCompletionItemKind.Resource => 18,
        CernealaCompletionItemKind.Variable => 6,
        CernealaCompletionItemKind.Function => 3,
        CernealaCompletionItemKind.Parameter => 5,
        _ => 1
    };
}
