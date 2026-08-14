using System.Text;
using Cerneala.Language.Features;
using Cerneala.Language.Semantics;
using Cerneala.Language.Text;
using Cerneala.LanguageServer.Protocol;
using Cerneala.LanguageServer.Workspace;

namespace Cerneala.LanguageServer.Features;

internal sealed class NavigationService(CernealaWorkspace workspace)
{
    private readonly CernealaNavigationService service = new();

    public Task<VersionedDocumentResult<LspHover?>?> GetHoverAsync(
        string uri,
        LspPosition position,
        CancellationToken cancellationToken) => workspace.RunDocumentRequestAsync(
            uri,
            (snapshot, requestCancellation) => Task.FromResult(GetHover(snapshot, position, requestCancellation)),
            cancellationToken);

    public Task<VersionedDocumentResult<LspLocation[]>?> GetDefinitionsAsync(
        string uri,
        LspPosition position,
        CancellationToken cancellationToken) => workspace.RunDocumentRequestAsync(
            uri,
            (snapshot, requestCancellation) => GetDefinitionsAsync(snapshot, position, requestCancellation),
            cancellationToken);

    public Task<VersionedDocumentResult<LspLocation[]>?> GetReferencesAsync(
        string uri,
        LspPosition position,
        bool includeDeclaration,
        CancellationToken cancellationToken) => workspace.RunDocumentRequestAsync(
            uri,
            (snapshot, requestCancellation) => GetReferencesAsync(
                snapshot,
                position,
                includeDeclaration,
                requestCancellation),
            cancellationToken);

    public Task<VersionedDocumentResult<LspDocumentHighlight[]>?> GetDocumentHighlightsAsync(
        string uri,
        LspPosition position,
        CancellationToken cancellationToken) => workspace.RunDocumentRequestAsync(
            uri,
            (snapshot, requestCancellation) => Task.FromResult(
                GetDocumentHighlights(snapshot, position, requestCancellation)),
            cancellationToken);

    public Task<VersionedDocumentResult<LspPrepareRenameResult>?> PrepareRenameAsync(
        string uri,
        LspPosition position,
        CancellationToken cancellationToken) => workspace.RunDocumentRequestAsync(
            uri,
            (snapshot, requestCancellation) => Task.FromResult(
                PrepareRename(snapshot, position, requestCancellation)),
            cancellationToken);

    public Task<VersionedDocumentResult<LspWorkspaceEdit>?> RenameAsync(
        string uri,
        LspPosition position,
        string newName,
        CancellationToken cancellationToken) => workspace.RunDocumentRequestAsync(
            uri,
            (snapshot, requestCancellation) => RenameAsync(
                snapshot,
                position,
                newName,
                requestCancellation),
            cancellationToken);

    private LspHover? GetHover(
        WorkspaceDocumentSnapshot snapshot,
        LspPosition position,
        CancellationToken cancellationToken)
    {
        int offset = GetOffset(snapshot.Document.Text, position);
        CernealaHoverInfo? hover = snapshot.GetSemanticModels(cancellationToken)
            .Select(model => service.GetHover(model, offset))
            .FirstOrDefault(candidate => candidate is not null);
        if (hover is null)
        {
            return null;
        }

        return new LspHover
        {
            Contents = new MarkupContent { Value = FormatHover(hover) }
        };
    }

    private async Task<LspLocation[]> GetDefinitionsAsync(
        WorkspaceDocumentSnapshot snapshot,
        LspPosition position,
        CancellationToken cancellationToken)
    {
        int offset = GetOffset(snapshot.Document.Text, position);
        IReadOnlyList<CernealaSemanticModel> models = snapshot.GetSemanticModels(cancellationToken);
        CernealaLocation[] locations = models
            .SelectMany(model => service.GetDefinitions(model, offset))
            .GroupBy(location => (NormalizePath(location.Path), location.Span))
            .Select(group => group.First())
            .ToArray();
        return await ToLspLocationsAsync(snapshot, locations, cancellationToken).ConfigureAwait(false);
    }

    private async Task<LspLocation[]> GetReferencesAsync(
        WorkspaceDocumentSnapshot snapshot,
        LspPosition position,
        bool includeDeclaration,
        CancellationToken cancellationToken)
    {
        int offset = GetOffset(snapshot.Document.Text, position);
        IReadOnlyList<CernealaSemanticModel> currentModels = snapshot.GetSemanticModels(cancellationToken);
        IReadOnlyList<CernealaSemanticModel> workspaceModels = snapshot.GetWorkspaceSemanticModels(cancellationToken);
        CernealaLocation[] locations = currentModels
            .SelectMany(model => service.GetReferences(
                model,
                workspaceModels,
                offset,
                includeDeclaration,
                cancellationToken))
            .GroupBy(location => (NormalizePath(location.Path), location.Span))
            .Select(group => group.First())
            .ToArray();
        return await ToLspLocationsAsync(snapshot, locations, cancellationToken).ConfigureAwait(false);
    }

    private LspDocumentHighlight[] GetDocumentHighlights(
        WorkspaceDocumentSnapshot snapshot,
        LspPosition position,
        CancellationToken cancellationToken)
    {
        int offset = GetOffset(snapshot.Document.Text, position);
        IReadOnlyList<CernealaSemanticModel> currentModels = snapshot.GetSemanticModels(cancellationToken);
        IReadOnlyList<CernealaSemanticModel> workspaceModels = snapshot.GetWorkspaceSemanticModels(cancellationToken);
        return currentModels
            .SelectMany(model => service.GetDocumentHighlights(model, workspaceModels, offset, cancellationToken))
            .GroupBy(highlight => highlight.Span)
            .Select(group => group.First())
            .OrderBy(highlight => highlight.Span.Start)
            .Select(highlight => new LspDocumentHighlight
            {
                Range = ToRange(snapshot.Document.Text, highlight.Span),
                Kind = highlight.Kind switch
                {
                    CernealaDocumentHighlightKind.Read => 2,
                    CernealaDocumentHighlightKind.Write => 3,
                    _ => 1
                }
            })
            .ToArray();
    }

    private LspPrepareRenameResult PrepareRename(
        WorkspaceDocumentSnapshot snapshot,
        LspPosition position,
        CancellationToken cancellationToken)
    {
        int offset = GetOffset(snapshot.Document.Text, position);
        IReadOnlyList<CernealaSemanticModel> currentModels = snapshot.GetSemanticModels(cancellationToken);
        IReadOnlyList<CernealaSemanticModel> workspaceModels = snapshot.GetWorkspaceSemanticModels(cancellationToken);
        CernealaPrepareRenameResult[] results = currentModels
            .Select(model => service.PrepareRename(model, workspaceModels, offset))
            .ToArray();
        CernealaPrepareRenameResult? accepted = results.FirstOrDefault(result => result.CanRename);
        if (accepted?.Span is not TextSpan span || results.Any(result => !result.CanRename ||
            !result.Span!.Value.Equals(span) ||
            !string.Equals(result.Placeholder, accepted.Placeholder, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(results.FirstOrDefault(result => result.Error is not null)?.Error ??
                "Rename is ambiguous across project contexts.");
        }

        return new LspPrepareRenameResult
        {
            Range = ToRange(snapshot.Document.Text, span),
            Placeholder = accepted.Placeholder!
        };
    }

    private async Task<LspWorkspaceEdit> RenameAsync(
        WorkspaceDocumentSnapshot snapshot,
        LspPosition position,
        string newName,
        CancellationToken cancellationToken)
    {
        int offset = GetOffset(snapshot.Document.Text, position);
        IReadOnlyList<CernealaSemanticModel> currentModels = snapshot.GetSemanticModels(cancellationToken);
        IReadOnlyList<CernealaSemanticModel> workspaceModels = snapshot.GetWorkspaceSemanticModels(cancellationToken);
        CernealaRenameResult[] results = currentModels
            .Select(model => service.Rename(model, workspaceModels, offset, newName, cancellationToken))
            .ToArray();
        if (results.Length == 0 || results.Any(result => !result.Succeeded))
        {
            throw new InvalidOperationException(results.FirstOrDefault(result => result.Error is not null)?.Error ??
                "Rename is unavailable without semantic project context.");
        }

        CernealaTextEdit[] edits = results.SelectMany(result => result.Edits)
            .GroupBy(edit => (NormalizePath(edit.Path), edit.Span))
            .Select(group => group.First())
            .ToArray();
        Dictionary<string, SourceText> sources = workspaceModels
            .Select(model => model.Document)
            .GroupBy(document => NormalizePath(document.Path), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Text, StringComparer.OrdinalIgnoreCase);
        sources[NormalizePath(snapshot.Document.Path)] = snapshot.Document.Text;

        Dictionary<string, LspTextEdit[]> changes = new(StringComparer.Ordinal);
        foreach (IGrouping<string, CernealaTextEdit> group in edits.GroupBy(edit => edit.Path, StringComparer.OrdinalIgnoreCase))
        {
            SourceText source = await GetSourceAsync(group.Key, sources, cancellationToken).ConfigureAwait(false);
            changes[ToUri(group.Key)] = group
                .OrderByDescending(edit => edit.Span.Start)
                .Select(edit => new LspTextEdit
                {
                    Range = ToRange(source, edit.Span),
                    NewText = edit.NewText
                })
                .ToArray();
        }

        return new LspWorkspaceEdit { Changes = changes };
    }

    private async Task<LspLocation[]> ToLspLocationsAsync(
        WorkspaceDocumentSnapshot snapshot,
        IReadOnlyList<CernealaLocation> locations,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CernealaSemanticModel> workspaceModels = snapshot.IsStandalone
            ? Array.Empty<CernealaSemanticModel>()
            : snapshot.GetWorkspaceSemanticModels(cancellationToken);
        Dictionary<string, SourceText> sources = workspaceModels
            .Select(model => model.Document)
            .GroupBy(document => NormalizePath(document.Path), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Text, StringComparer.OrdinalIgnoreCase);
        sources[NormalizePath(snapshot.Document.Path)] = snapshot.Document.Text;
        List<LspLocation> result = new();
        foreach (CernealaLocation location in locations)
        {
            SourceText source = await GetSourceAsync(location.Path, sources, cancellationToken).ConfigureAwait(false);
            result.Add(new LspLocation
            {
                Uri = ToUri(location.Path),
                Range = ToRange(source, location.Span)
            });
        }

        return result.ToArray();
    }

    private static async Task<SourceText> GetSourceAsync(
        string path,
        IReadOnlyDictionary<string, SourceText> sources,
        CancellationToken cancellationToken)
    {
        if (sources.TryGetValue(NormalizePath(path), out SourceText? source))
        {
            return source;
        }

        string text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return SourceText.From(text);
    }

    private static string FormatHover(CernealaHoverInfo hover)
    {
        StringBuilder builder = new();
        builder.Append("```csharp\n").Append(hover.Signature).Append("\n```\n\n");
        builder.Append("**").Append(hover.Category).Append("**");
        if (!string.IsNullOrWhiteSpace(hover.DeclaringType))
        {
            builder.Append("\n\nDeclared by `").Append(hover.DeclaringType).Append('`');
        }

        if (!string.IsNullOrWhiteSpace(hover.InheritedFrom))
        {
            builder.Append("\n\nInherits from `").Append(hover.InheritedFrom).Append('`');
        }

        if (!string.IsNullOrWhiteSpace(hover.DefaultValue))
        {
            builder.Append("\n\nDefault: `").Append(hover.DefaultValue).Append('`');
        }

        if (!string.IsNullOrWhiteSpace(hover.AssemblyName))
        {
            builder.Append("\n\nAssembly: `").Append(hover.AssemblyName).Append('`');
        }

        if (hover.IsDeprecated)
        {
            builder.Append("\n\nDeprecated.");
        }

        if (!string.IsNullOrWhiteSpace(hover.Documentation))
        {
            builder.Append("\n\n").Append(hover.Documentation);
        }

        if (!string.IsNullOrWhiteSpace(hover.DiagnosticExplanation))
        {
            builder.Append("\n\n").Append(hover.DiagnosticExplanation);
        }

        return builder.ToString();
    }

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

    private static string ToUri(string path) => new Uri(Path.GetFullPath(path)).AbsoluteUri;

    private static string NormalizePath(string path) => Path.GetFullPath(path)
        .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
}
