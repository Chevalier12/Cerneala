using Cerneala.Language.Features;
using Cerneala.Language.Semantics;
using Cerneala.Language.Text;
using Cerneala.LanguageServer.Protocol;
using Cerneala.LanguageServer.Workspace;

namespace Cerneala.LanguageServer.Features;

internal sealed class FormattingService(CernealaWorkspace workspace)
{
    private readonly CernealaFormattingService formatter = new();
    private readonly CernealaCodeActionService codeActions = new();

    public Task<VersionedDocumentResult<LspTextEdit[]>?> FormatDocumentAsync(
        string uri,
        FormattingOptions options,
        CancellationToken cancellationToken) => workspace.RunDocumentRequestAsync(
            uri,
            (snapshot, requestCancellation) => Task.FromResult(ToLspEdits(
                snapshot.Document.Text,
                formatter.FormatDocument(snapshot.Document, ToOptions(options), requestCancellation))),
            cancellationToken);

    public Task<VersionedDocumentResult<LspTextEdit[]>?> FormatRangeAsync(
        string uri,
        LspRange range,
        FormattingOptions options,
        CancellationToken cancellationToken) => workspace.RunDocumentRequestAsync(
            uri,
            (snapshot, requestCancellation) => Task.FromResult(ToLspEdits(
                snapshot.Document.Text,
                formatter.FormatRange(
                    snapshot.Document,
                    ToSpan(snapshot.Document.Text, range),
                    ToOptions(options),
                    requestCancellation))),
            cancellationToken);

    public Task<VersionedDocumentResult<LspTextEdit[]>?> FormatOnTypeAsync(
        string uri,
        LspPosition position,
        FormattingOptions options,
        CancellationToken cancellationToken) => workspace.RunDocumentRequestAsync(
            uri,
            (snapshot, requestCancellation) => Task.FromResult(ToLspEdits(
                snapshot.Document.Text,
                formatter.FormatOnType(
                    snapshot.Document,
                    ToOffset(snapshot.Document.Text, position),
                    ToOptions(options),
                    requestCancellation))),
            cancellationToken);

    public Task<VersionedDocumentResult<LspCodeAction[]>?> GetCodeActionsAsync(
        CodeActionParams request,
        CancellationToken cancellationToken) => workspace.RunDocumentRequestAsync(
            request.TextDocument.Uri,
            (snapshot, requestCancellation) => GetCodeActionsAsync(snapshot, request, requestCancellation),
            cancellationToken);

    private async Task<LspCodeAction[]> GetCodeActionsAsync(
        WorkspaceDocumentSnapshot snapshot,
        CodeActionParams request,
        CancellationToken cancellationToken)
    {
        TextSpan range = ToSpan(snapshot.Document.Text, request.Range);
        CernealaCodeActionDiagnostic[] diagnostics = request.Context.Diagnostics.Select(diagnostic =>
            new CernealaCodeActionDiagnostic(diagnostic.Code, ToSpan(snapshot.Document.Text, diagnostic.Range)))
            .ToArray();
        IReadOnlyList<CernealaSemanticModel> models = snapshot.GetSemanticModels(cancellationToken);
        CernealaAdditionalDocument[] companions = await GetCompanionsAsync(models, cancellationToken)
            .ConfigureAwait(false);
        bool includeFixAll = request.Context.Only is null || request.Context.Only.Length == 0 ||
            request.Context.Only.Any(kind => kind.StartsWith("source.fixAll", StringComparison.Ordinal));
        IEnumerable<CernealaCodeAction> actions = models.Count == 0
            ? codeActions.GetCodeActions(
                snapshot.Document,
                null,
                range,
                diagnostics,
                companions,
                includeFixAll,
                cancellationToken)
            : models.SelectMany(model => codeActions.GetCodeActions(
                snapshot.Document,
                model,
                range,
                diagnostics,
                companions,
                includeFixAll,
                cancellationToken));
        if (request.Context.Only is { Length: > 0 } only)
        {
            actions = actions.Where(action => only.Any(kind =>
                action.Kind.Equals(kind, StringComparison.Ordinal) ||
                action.Kind.StartsWith(kind + ".", StringComparison.Ordinal)));
        }

        Dictionary<string, SourceText> sources = companions.ToDictionary(
            document => Normalize(document.Path),
            document => document.Text,
            StringComparer.OrdinalIgnoreCase);
        sources[Normalize(snapshot.Document.Path)] = snapshot.Document.Text;
        return actions.GroupBy(ActionKey, StringComparer.Ordinal)
            .Select(group => ToLspCodeAction(group.First(), request.Context.Diagnostics, sources))
            .OrderByDescending(action => action.IsPreferred)
            .ThenBy(action => action.Title, StringComparer.Ordinal)
            .ToArray();
    }

    private static async Task<CernealaAdditionalDocument[]> GetCompanionsAsync(
        IReadOnlyList<CernealaSemanticModel> models,
        CancellationToken cancellationToken)
    {
        string[] paths = models.SelectMany(model => model.Symbols)
            .Where(symbol => symbol.Kind == CernealaSemanticSymbolKind.RootType && symbol.TypeSymbol is not null)
            .SelectMany(symbol => symbol.TypeSymbol!.Locations)
            .Select(location => location.Path)
            .Where(path => path.Length > 0 && File.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        List<CernealaAdditionalDocument> result = new();
        foreach (string path in paths)
        {
            string text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            result.Add(new CernealaAdditionalDocument(path, SourceText.From(text)));
        }

        return result.ToArray();
    }

    private static LspCodeAction ToLspCodeAction(
        CernealaCodeAction action,
        IReadOnlyList<LspDiagnostic> requestedDiagnostics,
        IReadOnlyDictionary<string, SourceText> sources)
    {
        Dictionary<string, LspTextEdit[]> changes = new(StringComparer.Ordinal);
        foreach (IGrouping<string, CernealaTextEdit> group in action.Edits.GroupBy(
            edit => Normalize(edit.Path),
            StringComparer.OrdinalIgnoreCase))
        {
            if (!sources.TryGetValue(group.Key, out SourceText? source))
            {
                continue;
            }

            changes[new Uri(group.Key).AbsoluteUri] = group.OrderByDescending(edit => edit.Span.Start)
                .Select(edit => new LspTextEdit
                {
                    Range = ToRange(source, edit.Span),
                    NewText = edit.NewText
                })
                .ToArray();
        }

        return new LspCodeAction
        {
            Title = action.Title,
            Kind = action.Kind,
            IsPreferred = action.IsPreferred,
            Diagnostics = requestedDiagnostics.Where(diagnostic => action.DiagnosticIds.Contains(
                diagnostic.Code,
                StringComparer.Ordinal)).ToArray(),
            Edit = new LspWorkspaceEdit { Changes = changes }
        };
    }

    private static LspTextEdit[] ToLspEdits(
        SourceText source,
        IReadOnlyList<CernealaFormattingEdit> edits) => edits.Select(edit => new LspTextEdit
    {
        Range = ToRange(source, edit.Span),
        NewText = edit.NewText
    }).ToArray();

    private static CernealaFormattingOptions ToOptions(FormattingOptions options) =>
        new(options.TabSize, options.InsertSpaces);

    private static int ToOffset(SourceText source, LspPosition position) =>
        source.GetOffset(new LinePosition(position.Line, position.Character));

    private static TextSpan ToSpan(SourceText source, LspRange range)
    {
        int start = ToOffset(source, range.Start);
        int end = ToOffset(source, range.End);
        return new TextSpan(start, end - start);
    }

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

    private static string Normalize(string path) => Path.GetFullPath(path)
        .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

    private static string ActionKey(CernealaCodeAction action) => action.Title + "|" + string.Join(
        ";",
        action.Edits.Select(edit => edit.Path + ":" + edit.Span.Start + ":" + edit.Span.Length + ":" + edit.NewText));
}
