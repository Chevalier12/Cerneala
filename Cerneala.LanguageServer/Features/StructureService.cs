using Cerneala.Language.Features;
using Cerneala.Language.Semantics;
using Cerneala.Language.Text;
using Cerneala.LanguageServer.Protocol;
using Cerneala.LanguageServer.Workspace;

namespace Cerneala.LanguageServer.Features;

internal sealed class StructureService(CernealaWorkspace workspace) : IDisposable
{
    internal const int MaximumTokenCacheEntries = 256;

    public static readonly string[] TokenTypes =
    [
        "type", "property", "event", "namespace", "variable", "keyword", "function", "parameter", "enumMember", "label", "property", "string", "function"
    ];

    private static readonly string[] VisualStudioTokenTypes =
    [
        "type", "property name", "event", "namespace", "type", "keyword", "function", "parameter", "enumMember", "keyword - control", "method name", "string", "method name"
    ];

    public static readonly string[] TokenModifiers = ["declaration"];

    public static string[] GetTokenTypes(string? host) => string.Equals(
        host,
        "visualStudio",
        StringComparison.OrdinalIgnoreCase)
            ? VisualStudioTokenTypes
            : TokenTypes;

    private readonly object cacheGate = new();
    private readonly CernealaStructureService service = new();
    private readonly Dictionary<string, TokenCacheEntry> tokenCache = new(StringComparer.Ordinal);
    private long resultSequence;

    internal int CachedDocumentCount
    {
        get
        {
            lock (cacheGate)
            {
                return tokenCache.Count;
            }
        }
    }

    public async Task<VersionedDocumentResult<LspSemanticTokens>?> GetSemanticTokensAsync(
        string uri,
        CancellationToken cancellationToken)
    {
        VersionedDocumentResult<int[]>? result = await GetEncodedSemanticTokensAsync(uri, cancellationToken)
            .ConfigureAwait(false);
        return result is null
            ? null
            : new VersionedDocumentResult<LspSemanticTokens>(result.Version, CacheFull(uri, result.Version, result.Value));
    }

    public async Task<VersionedDocumentResult<object>?> GetSemanticTokensDeltaAsync(
        string uri,
        string previousResultId,
        CancellationToken cancellationToken)
    {
        VersionedDocumentResult<int[]>? result = await GetEncodedSemanticTokensAsync(uri, cancellationToken)
            .ConfigureAwait(false);
        if (result is null)
        {
            return null;
        }

        object response;
        lock (cacheGate)
        {
            string resultId = NextResultId(result.Version);
            if (!tokenCache.TryGetValue(uri, out TokenCacheEntry? previous) ||
                !string.Equals(previous.ResultId, previousResultId, StringComparison.Ordinal))
            {
                response = new LspSemanticTokens { ResultId = resultId, Data = result.Value };
            }
            else
            {
                response = BuildDelta(previous.Data, result.Value, resultId);
            }

            tokenCache[uri] = new TokenCacheEntry(resultId, result.Value);
            TrimTokenCache(uri);
        }

        return new VersionedDocumentResult<object>(result.Version, response);
    }

    public Task<VersionedDocumentResult<LspDocumentSymbol[]>?> GetDocumentSymbolsAsync(
        string uri,
        CancellationToken cancellationToken) => workspace.RunDocumentRequestAsync(
            uri,
            (snapshot, requestCancellation) => Task.FromResult(GetDocumentSymbols(snapshot, requestCancellation)),
            cancellationToken);

    public Task<VersionedWorkspaceResult<LspSymbolInformation[]>?> GetWorkspaceSymbolsAsync(
        string query,
        CancellationToken cancellationToken) => workspace.RunWorkspaceRequestAsync(
            (models, requestCancellation) => Task.FromResult(GetWorkspaceSymbols(models, query, requestCancellation)),
            cancellationToken);

    public Task<VersionedDocumentResult<LspFoldingRange[]>?> GetFoldingRangesAsync(
        string uri,
        CancellationToken cancellationToken) => workspace.RunDocumentRequestAsync(
            uri,
            (snapshot, requestCancellation) => Task.FromResult(GetFoldingRanges(snapshot, requestCancellation)),
            cancellationToken);

    public Task<VersionedDocumentResult<LspSelectionRange[]>?> GetSelectionRangesAsync(
        string uri,
        IReadOnlyList<LspPosition> positions,
        CancellationToken cancellationToken) => workspace.RunDocumentRequestAsync(
            uri,
            (snapshot, requestCancellation) => Task.FromResult(
                GetSelectionRanges(snapshot, positions, requestCancellation)),
            cancellationToken);

    public void Clear(string uri)
    {
        lock (cacheGate)
        {
            tokenCache.Remove(uri);
        }
    }

    public void Dispose()
    {
        lock (cacheGate)
        {
            tokenCache.Clear();
        }
    }

    private Task<VersionedDocumentResult<int[]>?> GetEncodedSemanticTokensAsync(
        string uri,
        CancellationToken cancellationToken) => workspace.RunDocumentRequestAsync(
            uri,
            (snapshot, requestCancellation) => Task.FromResult(
                EncodeSemanticTokens(snapshot, requestCancellation)),
            cancellationToken);

    private int[] EncodeSemanticTokens(
        WorkspaceDocumentSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CernealaSemanticModel> models = snapshot.GetSemanticModels(cancellationToken);
        IReadOnlyList<CernealaSemanticToken> tokens = models.Count == 0
            ? service.GetSemanticTokens(snapshot.Document, null, cancellationToken)
            : models.SelectMany(model => service.GetSemanticTokens(snapshot.Document, model, cancellationToken))
                .GroupBy(token => (token.Span, token.Kind, token.Modifiers))
                .Select(group => group.First())
                .OrderBy(token => token.Span.Start)
                .ToArray();
        List<EncodedToken> segments = new();
        foreach (CernealaSemanticToken token in tokens)
        {
            AddSegments(snapshot.Document.Text, token, segments);
        }

        List<int> data = new(segments.Count * 5);
        int previousLine = 0;
        int previousCharacter = 0;
        foreach (EncodedToken token in segments.OrderBy(token => token.Line).ThenBy(token => token.Character))
        {
            int deltaLine = token.Line - previousLine;
            int deltaCharacter = deltaLine == 0 ? token.Character - previousCharacter : token.Character;
            data.Add(deltaLine);
            data.Add(deltaCharacter);
            data.Add(token.Length);
            data.Add((int)token.Kind);
            data.Add((int)token.Modifiers);
            previousLine = token.Line;
            previousCharacter = token.Character;
        }

        return data.ToArray();
    }

    private LspDocumentSymbol[] GetDocumentSymbols(
        WorkspaceDocumentSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        CernealaSemanticModel? model = snapshot.GetSemanticModels(cancellationToken).FirstOrDefault();
        return service.GetDocumentSymbols(snapshot.Document, model, cancellationToken)
            .Select(symbol => ToDocumentSymbol(snapshot.Document.Text, symbol))
            .ToArray();
    }

    private LspSymbolInformation[] GetWorkspaceSymbols(
        IReadOnlyList<CernealaSemanticModel> models,
        string query,
        CancellationToken cancellationToken)
    {
        Dictionary<string, SourceText> sources = models.Select(model => model.Document)
            .GroupBy(document => Path.GetFullPath(document.Path), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Text, StringComparer.OrdinalIgnoreCase);
        return service.GetWorkspaceSymbols(models, query, cancellationToken)
            .Where(symbol => sources.ContainsKey(Path.GetFullPath(symbol.Path)))
            .Select(symbol => new LspSymbolInformation
            {
                Name = symbol.Name,
                Kind = ToSymbolKind(symbol.Kind),
                ContainerName = symbol.Detail,
                Location = new LspLocation
                {
                    Uri = new Uri(Path.GetFullPath(symbol.Path)).AbsoluteUri,
                    Range = ToRange(sources[Path.GetFullPath(symbol.Path)], symbol.Span)
                }
            })
            .ToArray();
    }

    private LspFoldingRange[] GetFoldingRanges(
        WorkspaceDocumentSnapshot snapshot,
        CancellationToken cancellationToken) => service.GetFoldingRanges(snapshot.Document, cancellationToken)
        .Select(range =>
        {
            LinePosition start = snapshot.Document.Text.GetLinePosition(range.Span.Start);
            LinePosition end = snapshot.Document.Text.GetLinePosition(range.Span.End);
            return new LspFoldingRange
            {
                StartLine = start.Line,
                StartCharacter = start.Character,
                EndLine = end.Line,
                EndCharacter = end.Character,
                Kind = range.Kind
            };
        })
        .ToArray();

    private LspSelectionRange[] GetSelectionRanges(
        WorkspaceDocumentSnapshot snapshot,
        IReadOnlyList<LspPosition> positions,
        CancellationToken cancellationToken)
    {
        CernealaSemanticModel? model = snapshot.GetSemanticModels(cancellationToken).FirstOrDefault();
        return positions.Select(position =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            int offset = snapshot.Document.Text.GetOffset(new LinePosition(position.Line, position.Character));
            return ToSelectionRange(
                snapshot.Document.Text,
                service.GetSelectionRange(snapshot.Document, model, offset));
        }).ToArray();
    }

    private LspSemanticTokens CacheFull(string uri, long version, int[] data)
    {
        lock (cacheGate)
        {
            string resultId = NextResultId(version);
            tokenCache[uri] = new TokenCacheEntry(resultId, data);
            TrimTokenCache(uri);
            return new LspSemanticTokens { ResultId = resultId, Data = data };
        }
    }

    private void TrimTokenCache(string retainedUri)
    {
        while (tokenCache.Count > MaximumTokenCacheEntries)
        {
            string? oldest = tokenCache.Keys.FirstOrDefault(uri =>
                !string.Equals(uri, retainedUri, StringComparison.Ordinal));
            if (oldest is null)
            {
                return;
            }

            tokenCache.Remove(oldest);
        }
    }

    private string NextResultId(long version) => version.ToString(
        System.Globalization.CultureInfo.InvariantCulture) + ":" +
        Interlocked.Increment(ref resultSequence).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static LspSemanticTokensDelta BuildDelta(int[] previous, int[] current, string resultId)
    {
        int previousTokens = previous.Length / 5;
        int currentTokens = current.Length / 5;
        int prefix = 0;
        while (prefix < previousTokens && prefix < currentTokens && TokenEquals(previous, current, prefix, prefix))
        {
            prefix++;
        }

        int suffix = 0;
        while (suffix < previousTokens - prefix && suffix < currentTokens - prefix &&
            TokenEquals(previous, current, previousTokens - suffix - 1, currentTokens - suffix - 1))
        {
            suffix++;
        }

        LspSemanticTokensEdit[] edits = prefix == previousTokens && prefix == currentTokens
            ? []
            :
            [
                new LspSemanticTokensEdit
                {
                    Start = prefix * 5,
                    DeleteCount = (previousTokens - prefix - suffix) * 5,
                    Data = current.Skip(prefix * 5)
                        .Take((currentTokens - prefix - suffix) * 5)
                        .ToArray()
                }
            ];
        return new LspSemanticTokensDelta { ResultId = resultId, Edits = edits };
    }

    private static bool TokenEquals(int[] left, int[] right, int leftToken, int rightToken)
    {
        for (int component = 0; component < 5; component++)
        {
            if (left[leftToken * 5 + component] != right[rightToken * 5 + component])
            {
                return false;
            }
        }

        return true;
    }

    private static void AddSegments(
        SourceText source,
        CernealaSemanticToken token,
        ICollection<EncodedToken> segments)
    {
        int offset = token.Span.Start;
        while (offset < token.Span.End)
        {
            LinePosition position = source.GetLinePosition(offset);
            int nextLine = position.Line + 1 < source.LineCount
                ? source.GetOffset(new LinePosition(position.Line + 1, 0))
                : source.Length;
            int contentEnd = nextLine;
            while (contentEnd > offset && source[contentEnd - 1] is '\r' or '\n')
            {
                contentEnd--;
            }

            int segmentEnd = Math.Min(token.Span.End, contentEnd);
            if (segmentEnd > offset)
            {
                segments.Add(new EncodedToken(
                    position.Line,
                    position.Character,
                    segmentEnd - offset,
                    token.Kind,
                    token.Modifiers));
            }

            if (nextLine <= offset || nextLine >= token.Span.End)
            {
                break;
            }

            offset = nextLine;
        }
    }

    private static LspDocumentSymbol ToDocumentSymbol(SourceText source, CernealaOutlineSymbol symbol) => new()
    {
        Name = symbol.Name,
        Detail = symbol.Detail,
        Kind = ToSymbolKind(symbol.Kind),
        Range = ToRange(source, symbol.Range),
        SelectionRange = ToRange(source, symbol.SelectionRange),
        Children = symbol.Children.Select(child => ToDocumentSymbol(source, child)).ToArray()
    };

    private static LspSelectionRange ToSelectionRange(SourceText source, CernealaSelectionRange selection) => new()
    {
        Range = ToRange(source, selection.Span),
        Parent = selection.Parent is null ? null : ToSelectionRange(source, selection.Parent)
    };

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

    private static int ToSymbolKind(CernealaOutlineSymbolKind kind) => kind switch
    {
        CernealaOutlineSymbolKind.Root => 5,
        CernealaOutlineSymbolKind.ResourceGroup => 3,
        CernealaOutlineSymbolKind.Resource => 14,
        CernealaOutlineSymbolKind.Template => 12,
        CernealaOutlineSymbolKind.Aspect => 5,
        CernealaOutlineSymbolKind.Motion => 24,
        CernealaOutlineSymbolKind.Prism => 19,
        _ => 19
    };

    private sealed record TokenCacheEntry(string ResultId, int[] Data);

    private sealed record EncodedToken(
        int Line,
        int Character,
        int Length,
        CernealaSemanticTokenKind Kind,
        CernealaSemanticTokenModifiers Modifiers);
}
