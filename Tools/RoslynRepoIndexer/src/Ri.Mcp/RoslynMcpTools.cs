using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using RoslynRepoIndexer.Core;

namespace Ri.Mcp;

[McpServerToolType]
public sealed class RoslynMcpTools
{
    private readonly IRoslynIndexerApplicationService? applicationService;
    private readonly RepositorySessionRegistry? sessionRegistry;
    private readonly RepositoryBinding repositoryBinding;
    private readonly ContinuationTokenCodec continuationTokens;

    public RoslynMcpTools()
    {
        repositoryBinding = new RepositoryBinding(Directory.GetCurrentDirectory());
        continuationTokens = new ContinuationTokenCodec();
    }

    public RoslynMcpTools(IRoslynIndexerApplicationService applicationService)
    {
        this.applicationService = applicationService;
        repositoryBinding = new RepositoryBinding(Directory.GetCurrentDirectory());
        continuationTokens = new ContinuationTokenCodec();
    }

    public RoslynMcpTools(RepositorySessionRegistry sessionRegistry, RepositoryBinding repositoryBinding, ContinuationTokenCodec continuationTokens)
    {
        this.sessionRegistry = sessionRegistry;
        this.repositoryBinding = repositoryBinding;
        this.continuationTokens = continuationTokens;
    }

    [McpServerTool(Name = "roslyn_doctor", ReadOnly = true, UseStructuredContent = true)]
    [Description("Inspect local repository/index prerequisites and diagnostics.")]
    public async Task<RoslynMcpToolResult<DoctorSummary>> DoctorAsync(RoslynRepoRequest request, CancellationToken cancellationToken = default)
    {
        var repoRoot = ResolveRepoRoot(request.RepoRoot);
        var app = CreateService(repoRoot);
        return ToToolResult("roslyn_doctor", await app.DoctorAsync(new PathCommandRequest(repoRoot, request.ConfigPath, request.Deep), cancellationToken).ConfigureAwait(false));
    }

    [McpServerTool(Name = "roslyn_index", ReadOnly = false, UseStructuredContent = true)]
    [Description("Build or update the local Roslyn repository index. Writes only under .roslyn-index in the repo root.")]
    public async Task<RoslynMcpToolResult<IndexSummary>> IndexAsync(RoslynIndexRequest request, CancellationToken cancellationToken = default)
    {
        var repoRoot = ResolveRepoRoot(request.RepoRoot);
        var app = CreateService(repoRoot);
        var response = await app.IndexAsync(
            new IndexCommandRequest(repoRoot, request.Force, request.IncludeGenerated, request.IncludeNonCSharpText ?? false, request.MaxTextFileBytes, request.MaxDegreeOfParallelism, request.ConfigPath),
            cancellationToken).ConfigureAwait(false);
        return ToToolResult("roslyn_index", response);
    }

    [McpServerTool(Name = "roslyn_status", ReadOnly = true, UseStructuredContent = true)]
    [Description("Return deterministic local index status for the repository.")]
    public Task<RoslynMcpToolResult<StatusSummary>> StatusAsync(RoslynRepoRequest request)
    {
        var repoRoot = ResolveRepoRoot(request.RepoRoot);
        var app = CreateService(repoRoot);
        var response = app.Status(new PathCommandRequest(repoRoot, request.ConfigPath));
        if (response.Success && response.Data is not null && sessionRegistry is not null)
        {
            var session = sessionRegistry.Get(repoRoot);
            response = response with { Data = response.Data with { SessionState = session.SessionState, WorkspaceState = session.WorkspaceState } };
        }
        return Task.FromResult(ToToolResult("roslyn_status", response));
    }

    [McpServerTool(Name = "roslyn_search", ReadOnly = true, UseStructuredContent = true)]
    [Description("Search the existing local index for symbols, text, files, or references.")]
    public Task<RoslynMcpToolResult<IReadOnlyList<SearchResult>>> SearchAsync(RoslynSearchRequest request)
    {
        var repoRoot = ResolveRepoRoot(request.RepoRoot);
        var app = CreateService(repoRoot);
        if (!TryPreparePage("roslyn_search", repoRoot, request.ContinuationToken, request.Limit, out var page, out var pagingError))
        {
            return Task.FromResult(PagingFailure<IReadOnlyList<SearchResult>>("roslyn_search", repoRoot, pagingError!));
        }

        var response = app.Search(new SearchCommandRequest(
            request.Query,
            ParseMode(request.Mode),
            page.FetchCount,
            request.Kind,
            request.Path,
            request.Project,
            request.IncludeTests,
            request.FromFile,
            request.FromProject,
            request.TimeoutMs));
        RecordSessionQuery(repoRoot, response.ElapsedMs);
        return Task.FromResult(ToPagedToolResult("roslyn_search", response, page));
    }

    [McpServerTool(Name = "roslyn_read", ReadOnly = true, UseStructuredContent = true)]
    [Description("Return a full local repository file. Prefer roslyn_read before editing so the complete file context is available.")]
    public Task<RoslynMcpToolResult<RepositoryFileReadResult>> ReadAsync(RoslynReadRequest request)
    {
        var repoRoot = ResolveRepoRoot(request.RepoRoot);
        var app = CreateService(repoRoot);
        return Task.FromResult(ToToolResult("roslyn_read", app.Read(new FileReadCommandRequest(request.FilePath, request.ConfigPath, request.MaxTextFileBytes))));
    }

    [McpServerTool(Name = "roslyn_pread", ReadOnly = true, UseStructuredContent = true)]
    [Description("Return a targeted partial read of a local repository file for focused inspection after full context is known.")]
    public Task<RoslynMcpToolResult<RepositoryPartialFileReadResult>> PReadAsync(RoslynPReadRequest request)
    {
        var repoRoot = ResolveRepoRoot(request.RepoRoot);
        var app = CreateService(repoRoot);
        return Task.FromResult(ToToolResult("roslyn_pread", app.PartialRead(new PartialFileReadCommandRequest(
            request.FilePath,
            request.StartLine,
            request.EndLine,
            request.AroundLine,
            request.Context,
            request.ConfigPath,
            request.MaxTextFileBytes))));
    }

    [McpServerTool(Name = "roslyn_goto", ReadOnly = true, UseStructuredContent = true)]
    [Description("Find symbol declarations in the local index.")]
    public Task<RoslynMcpToolResult<IReadOnlyList<SearchResult>>> GotoAsync(RoslynGotoRequest request)
    {
        var repoRoot = ResolveRepoRoot(request.RepoRoot);
        var app = CreateService(repoRoot);
        if (!TryPreparePage("roslyn_goto", repoRoot, request.ContinuationToken, request.Limit, out var page, out var pagingError))
        {
            return Task.FromResult(PagingFailure<IReadOnlyList<SearchResult>>("roslyn_goto", repoRoot, pagingError!));
        }

        var response = app.Goto(new SymbolQueryCommandRequest(request.SymbolId ?? request.Query, request.Kind, page.FetchCount));
        RecordSessionQuery(repoRoot, response.ElapsedMs);
        return Task.FromResult(ToPagedToolResult("roslyn_goto", response, page));
    }

    [McpServerTool(Name = "roslyn_refs", ReadOnly = true, UseStructuredContent = true)]
    [Description("Find indexed or exact Roslyn references for a symbol without shell execution or network calls.")]
    public async Task<RoslynMcpToolResult<object>> RefsAsync(RoslynRefsRequest request, CancellationToken cancellationToken = default)
    {
        var repoRoot = ResolveRepoRoot(request.RepoRoot);
        var app = CreateService(repoRoot);
        if (!TryPreparePage("roslyn_refs", repoRoot, request.ContinuationToken, request.Limit, out var page, out var pagingError))
        {
            return PagingFailure<object>("roslyn_refs", repoRoot, pagingError!);
        }

        var response = await app.RefsAsync(new RefsCommandRequest(request.Query, request.SymbolId, request.Exact, request.TimeoutSeconds, page.FetchCount), cancellationToken).ConfigureAwait(false);
        RecordSessionQuery(repoRoot, response.ElapsedMs);
        if (response.Data is IReadOnlyList<SearchResult> results)
        {
            var paged = Page(results, page, out var truncated, out var nextToken);
            response = response with { Data = paged, Results = default };
            return ToToolResult("roslyn_refs", response, truncated, nextToken);
        }

        return ToToolResult("roslyn_refs", response);
    }

    [McpServerTool(Name = "roslyn_outline", ReadOnly = true, UseStructuredContent = true)]
    [Description("Return a bounded semantic outline for a file, type, or namespace.")]
    public Task<RoslynMcpToolResult<object>> OutlineAsync(RoslynOutlineRequest request)
    {
        var repoRoot = ResolveRepoRoot(request.RepoRoot);
        return Task.FromResult(ExecuteSemantic("roslyn_outline", repoRoot, service => service.Outline(request.Target, request.Depth, request.MaxResults, request.MaxChars, request.IncludePrivate, request.IncludeGenerated)));
    }

    [McpServerTool(Name = "roslyn_inspect", ReadOnly = true, UseStructuredContent = true)]
    [Description("Resolve one symbol strictly and return selected source, relationship, reference, and test context.")]
    public Task<RoslynMcpToolResult<object>> InspectAsync(RoslynInspectRequest request)
    {
        var repoRoot = ResolveRepoRoot(request.RepoRoot);
        return Task.FromResult(ExecuteSemantic("roslyn_inspect", repoRoot, service => service.Inspect(request.Symbol, request.Include ?? Array.Empty<InspectInclude>(), request.Depth, request.MaxResults, request.MaxChars)));
    }

    [McpServerTool(Name = "roslyn_context", ReadOnly = true, UseStructuredContent = true)]
    [Description("Build a compact relevance-ranked context package for one symbol.")]
    public Task<RoslynMcpToolResult<object>> ContextAsync(RoslynContextRequest request)
    {
        var repoRoot = ResolveRepoRoot(request.RepoRoot);
        return Task.FromResult(ExecuteSemantic("roslyn_context", repoRoot, service => service.Context(request.Symbol, request.MaxChars, request.MaxResults)));
    }

    [McpServerTool(Name = "roslyn_callgraph", ReadOnly = true, UseStructuredContent = true)]
    [Description("Traverse the indexed invocation graph with deterministic depth and node bounds.")]
    public Task<RoslynMcpToolResult<object>> CallGraphAsync(RoslynCallGraphRequest request)
    {
        var repoRoot = ResolveRepoRoot(request.RepoRoot);
        return Task.FromResult(ExecuteSemantic("roslyn_callgraph", repoRoot, service => service.CallGraph(request.Symbol, request.Direction, request.Depth, request.MaxNodes, request.IncludeTests, request.IncludeExternal)));
    }

    [McpServerTool(Name = "roslyn_impact", ReadOnly = true, UseStructuredContent = true)]
    [Description("Return demonstrable semantic and structural impact relationships for one symbol.")]
    public Task<RoslynMcpToolResult<object>> ImpactAsync(RoslynImpactRequest request)
    {
        var repoRoot = ResolveRepoRoot(request.RepoRoot);
        return Task.FromResult(ExecuteSemantic("roslyn_impact", repoRoot, service => service.Impact(request.Symbol, request.MaxResults)));
    }

    [McpServerTool(Name = "roslyn_tests_for", ReadOnly = true, UseStructuredContent = true)]
    [Description("Rank candidate tests using semantic references, naming, project, and path evidence.")]
    public Task<RoslynMcpToolResult<object>> TestsForAsync(RoslynTestsForRequest request)
    {
        var repoRoot = ResolveRepoRoot(request.RepoRoot);
        return Task.FromResult(ExecuteSemantic("roslyn_tests_for", repoRoot, service => service.TestsFor(request.Symbol, request.MaxResults)));
    }

    [McpServerTool(Name = "roslyn_batch", ReadOnly = true, UseStructuredContent = true)]
    [Description("Execute a validated bounded operation graph against one immutable index generation.")]
    public Task<RoslynMcpToolResult<object>> BatchAsync(RoslynBatchRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var repoRoot = ResolveRepoRoot(request.RepoRoot);
        var operations = request.Operations ?? Array.Empty<RoslynBatchOperation>();
        if (operations.Count is < 1 or > 20 || operations.Select(operation => operation.Id).Distinct(StringComparer.Ordinal).Count() != operations.Count)
        {
            return Task.FromResult(PagingFailure<object>("roslyn_batch", repoRoot, new RoslynMcpError("invalid-batch", "Batch requires 1-20 operations with unique non-empty IDs.", false, "Correct the operation list and retry.")));
        }

        try
        {
            var queryIndex = sessionRegistry is null ? new QueryIndex(IndexStore.Read(repoRoot)) : sessionRegistry.Get(repoRoot).GetQueryIndex();
            var generationId = queryIndex.Snapshot.Manifest.GenerationId;
            var service = new SemanticQueryService(queryIndex, repoRoot);
            var search = new SearchService(queryIndex, static (_, _) => string.Empty);
            var completed = new List<RoslynBatchOperationResult>();
            var timedOut = false;
            var truncated = false;
            foreach (var operation in operations)
            {
                if (stopwatch.ElapsedMilliseconds >= Math.Max(0, request.TimeoutMs))
                {
                    timedOut = true;
                    break;
                }

                RoslynBatchOperationResult result;
                try
                {
                    var query = ResolveBatchInput(operation, completed);
                    object data = operation.Operation.ToLowerInvariant() switch
                    {
                        "goto" => search.Search(new SearchRequest(query, SearchMode.Symbol, Math.Clamp(operation.Limit, 1, 1000))),
                        "refs" => search.Search(new SearchRequest(query, SearchMode.Reference, Math.Clamp(operation.Limit, 1, 1000))),
                        "outline" => service.Outline(query, operation.Depth, operation.Limit, request.MaxChars),
                        "inspect" => service.Inspect(query, operation.Include ?? Array.Empty<InspectInclude>(), operation.Depth, operation.Limit, request.MaxChars),
                        "context" => service.Context(query, request.MaxChars, operation.Limit),
                        "callgraph" => service.CallGraph(query, CallGraphDirection.Both, operation.Depth, operation.Limit),
                        "impact" => service.Impact(query, operation.Limit),
                        "tests_for" => service.TestsFor(query, operation.Limit),
                        _ => throw new InvalidOperationException($"Unsupported batch operation '{operation.Operation}'.")
                    };
                    result = new RoslynBatchOperationResult(operation.Id, operation.Operation, true, data);
                }
                catch (Exception ex) when (ex is SymbolQueryException or InvalidOperationException or FormatException)
                {
                    var code = ex is SymbolQueryException symbolError ? symbolError.Code : "invalid-batch-operation";
                    result = new RoslynBatchOperationResult(operation.Id, operation.Operation, false, null, new RoslynMcpError(code, ex.Message, false, "Correct this operation or its dependency reference."));
                }

                completed.Add(result);
                var candidate = new RoslynBatchResult(generationId, completed, false, false);
                if (JsonSerializer.Serialize(candidate, JsonOptions.Compact).Length > Math.Max(1, request.MaxChars))
                {
                    completed.RemoveAt(completed.Count - 1);
                    truncated = true;
                    break;
                }
                if (!result.Success && request.FailureMode == RoslynBatchFailureMode.Stop) break;
            }

            stopwatch.Stop();
            var batch = new RoslynBatchResult(generationId, completed, truncated, timedOut);
            return Task.FromResult(new RoslynMcpToolResult<object>(true, "roslyn_batch", repoRoot, stopwatch.ElapsedMilliseconds, Array.Empty<string>(), Array.Empty<RoslynMcpError>(), batch, Truncated: truncated));
        }
        catch (Exception ex) when (ex is IndexUnavailableException or IOException or InvalidDataException)
        {
            stopwatch.Stop();
            return Task.FromResult(new RoslynMcpToolResult<object>(false, "roslyn_batch", repoRoot, stopwatch.ElapsedMilliseconds, Array.Empty<string>(), new[] { new RoslynMcpError("index-unavailable", ex.Message, true, "Run roslyn_index and retry.") }, null, 3));
        }
    }

    [McpServerTool(Name = "roslyn_changes", ReadOnly = true, UseStructuredContent = true)]
    [Description("Return bounded semantic changes between generations or structural Git changes.")]
    public Task<RoslynMcpToolResult<object>> ChangesAsync(RoslynChangesRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var repoRoot = ResolveRepoRoot(request.RepoRoot);
        try
        {
            var result = new SemanticChangeService(repoRoot).Compare(request.Comparison, request.BaseId, request.TargetId, request.MaxResults);
            stopwatch.Stop();
            return Task.FromResult(new RoslynMcpToolResult<object>(true, "roslyn_changes", repoRoot, stopwatch.ElapsedMilliseconds, Array.Empty<string>(), Array.Empty<RoslynMcpError>(), result, Truncated: result.Truncated));
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or IndexUnavailableException or ArgumentException or InvalidOperationException)
        {
            stopwatch.Stop();
            return Task.FromResult(new RoslynMcpToolResult<object>(false, "roslyn_changes", repoRoot, stopwatch.ElapsedMilliseconds, Array.Empty<string>(), new[] { new RoslynMcpError("changes-unavailable", ex.Message, false, "Verify generation IDs or Git revisions and retry.") }, null, 2));
        }
    }

    [McpServerTool(Name = "roslyn_profile", ReadOnly = true, UseStructuredContent = true)]
    [Description("Return local generation, session, timing, table-size, and posting diagnostics.")]
    public Task<RoslynMcpToolResult<RoslynProfileResult>> ProfileAsync(RoslynProfileRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var repoRoot = ResolveRepoRoot(request.RepoRoot);
        try
        {
            var session = sessionRegistry?.Get(repoRoot) ?? new RepositoryIndexSession(repoRoot);
            var queryIndex = session.GetQueryIndex();
            var directory = IndexStore.GetVersionDirectory(repoRoot);
            var files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                .Select(path => new RoslynProfileFileSize(RepositoryDiscovery.NormalizeRelative(Path.GetRelativePath(directory, path)), new FileInfo(path).Length))
                .OrderBy(file => file.Name, StringComparer.Ordinal).ToArray();
            var topTerms = queryIndex.Snapshot.Tokens.GroupBy(token => token.Token, StringComparer.Ordinal)
                .Select(group => new RoslynProfileTerm(group.Key, group.Count()))
                .OrderByDescending(term => term.PostingCount).ThenBy(term => term.Term, StringComparer.Ordinal)
                .Take(Math.Clamp(request.TopTerms, 1, 100)).ToArray();
            var result = new RoslynProfileResult(queryIndex.Snapshot.Manifest.GenerationId, session.Metrics, queryIndex.Snapshot.Manifest.Timings, files, topTerms, files.Sum(file => file.Bytes));
            stopwatch.Stop();
            return Task.FromResult(new RoslynMcpToolResult<RoslynProfileResult>(true, "roslyn_profile", repoRoot, stopwatch.ElapsedMilliseconds, Array.Empty<string>(), Array.Empty<RoslynMcpError>(), result));
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or IndexUnavailableException)
        {
            stopwatch.Stop();
            return Task.FromResult(new RoslynMcpToolResult<RoslynProfileResult>(false, "roslyn_profile", repoRoot, stopwatch.ElapsedMilliseconds, Array.Empty<string>(), new[] { new RoslynMcpError("index-unavailable", ex.Message, true, "Run roslyn_index and retry.") }, null, 3));
        }
    }

    [McpServerTool(Name = "roslyn_capabilities", ReadOnly = true, UseStructuredContent = true)]
    [Description("Return server, contract, repository binding, command, and limit capabilities.")]
    public Task<RoslynMcpToolResult<RoslynCapabilities>> CapabilitiesAsync(RoslynRepoRequest request)
    {
        var repoRoot = ResolveRepoRoot(request.RepoRoot);
        var capabilities = new RoslynCapabilities(
            "0.2.0",
            IndexManifest.CurrentSchemaVersion,
            "2",
            repositoryBinding.RepoRoot,
            RoslynMcpToolCatalog.Tools.Select(tool => tool.Name).ToArray(),
            Enum.GetNames<RoslynResponseProfile>().Select(name => name.ToLowerInvariant()).ToArray(),
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["maxResults"] = 1000,
                ["maxChars"] = 1_000_000,
                ["maxDepth"] = 8,
                ["maxBatchOperations"] = 20
            });
        return Task.FromResult(new RoslynMcpToolResult<RoslynCapabilities>(true, "roslyn_capabilities", repoRoot, 0, Array.Empty<string>(), Array.Empty<RoslynMcpError>(), capabilities));
    }

    private string ResolveRepoRoot(string? repoRoot) => repositoryBinding.Resolve(repoRoot);

    private static string ResolveBatchInput(RoslynBatchOperation operation, IReadOnlyList<RoslynBatchOperationResult> completed)
    {
        if (!string.IsNullOrWhiteSpace(operation.Query)) return operation.Query;
        var dependency = operation.SymbolFrom ?? operation.FileFrom;
        if (string.IsNullOrWhiteSpace(dependency)) throw new InvalidOperationException($"Operation '{operation.Id}' requires query, symbolFrom, or fileFrom.");
        var parts = dependency.Split(':', 2);
        var source = completed.FirstOrDefault(result => string.Equals(result.Id, parts[0], StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Dependency '{parts[0]}' must reference an earlier successful operation.");
        if (!source.Success || source.Data is null) throw new InvalidOperationException($"Dependency '{parts[0]}' did not produce data.");
        var index = parts.Length == 2 && int.TryParse(parts[1], out var parsed) ? parsed : 0;
        if (operation.FileFrom is not null) return ExtractFile(source.Data, index);
        return ExtractSymbol(source.Data, index);
    }

    private static string ExtractSymbol(object data, int index)
        => data switch
        {
            IReadOnlyList<SearchResult> results when index >= 0 && index < results.Count => results[index].SymbolId ?? throw new InvalidOperationException("Selected search result has no symbolId."),
            InspectResult inspect => inspect.Symbol.SymbolId,
            ContextResult context => context.Symbol.SymbolId,
            ImpactResult impact => impact.Target.SymbolId,
            OutlineResult outline when index >= 0 && index < outline.Items.Count => outline.Items[index].Symbol.SymbolId,
            _ => throw new InvalidOperationException("Dependency result cannot provide a symbolId.")
        };

    private static string ExtractFile(object data, int index)
        => data switch
        {
            IReadOnlyList<SearchResult> results when index >= 0 && index < results.Count => results[index].Path,
            InspectResult inspect => inspect.Symbol.Span.Path,
            ContextResult context => context.Symbol.Span.Path,
            OutlineResult outline when index >= 0 && index < outline.Items.Count => outline.Items[index].Symbol.Span.Path,
            _ => throw new InvalidOperationException("Dependency result cannot provide a file path.")
        };

    private RoslynMcpToolResult<object> ExecuteSemantic(string tool, string repoRoot, Func<SemanticQueryService, object> operation)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            QueryIndex queryIndex;
            RoslynMcpCacheInfo? cache = null;
            if (sessionRegistry is not null)
            {
                var session = sessionRegistry.Get(repoRoot);
                var before = session.Metrics;
                queryIndex = session.GetQueryIndex();
                var after = session.Metrics;
                cache = new RoslynMcpCacheInfo(after.SessionHits > before.SessionHits, after.ReloadCount > before.ReloadCount, after.ReloadCount, after.LoadMs);
            }
            else
            {
                queryIndex = new QueryIndex(IndexStore.Read(repoRoot));
            }

            var data = operation(new SemanticQueryService(queryIndex, repoRoot));
            stopwatch.Stop();
            sessionRegistry?.Get(repoRoot).RecordQuery(stopwatch.ElapsedMilliseconds);
            return new RoslynMcpToolResult<object>(true, tool, repoRoot, stopwatch.ElapsedMilliseconds, Array.Empty<string>(), Array.Empty<RoslynMcpError>(), data, Cache: cache);
        }
        catch (SymbolQueryException ex)
        {
            stopwatch.Stop();
            return new RoslynMcpToolResult<object>(false, tool, repoRoot, stopwatch.ElapsedMilliseconds, Array.Empty<string>(), new[] { new RoslynMcpError(ex.Code, ex.Message, false, "Use a returned symbolId to disambiguate the next request.") }, ex.Candidates, 2);
        }
        catch (Exception ex) when (ex is IndexUnavailableException or FileNotFoundException or InvalidDataException)
        {
            stopwatch.Stop();
            return new RoslynMcpToolResult<object>(false, tool, repoRoot, stopwatch.ElapsedMilliseconds, Array.Empty<string>(), new[] { new RoslynMcpError("index-unavailable", ex.Message, true, "Run roslyn_index with force=true, then retry.") }, null, 3);
        }
    }

    private IRoslynIndexerApplicationService CreateService(string repoRoot)
        => applicationService ?? new RoslynIndexerApplicationService(
            repoRoot,
            sessionRegistry is null ? null : root => sessionRegistry.Get(root).GetQueryIndex(),
            sessionRegistry?.Get(repoRoot).CreateIndexBuilder());

    private RoslynMcpToolResult<IReadOnlyList<SearchResult>> ToPagedToolResult(
        string tool,
        CommandResponse<IReadOnlyList<SearchResult>> response,
        PageRequest page)
    {
        if (!response.Success || response.Data is null)
        {
            return ToToolResult(tool, response);
        }

        var data = Page(response.Data, page, out var truncated, out var nextToken);
        return ToToolResult(tool, response with { Data = data, Results = default }, truncated, nextToken);
    }

    private IReadOnlyList<T> Page<T>(IReadOnlyList<T> source, PageRequest page, out bool truncated, out string? nextToken)
    {
        var data = source.Skip(page.Offset).Take(page.Limit).ToArray();
        truncated = source.Count > page.Offset + data.Length;
        nextToken = truncated
            ? continuationTokens.Encode(page.Tool, page.GenerationId, page.Offset + data.Length)
            : null;
        return data;
    }

    private bool TryPreparePage(string tool, string repoRoot, string? token, int requestedLimit, out PageRequest page, out RoslynMcpError? error)
    {
        var limit = Math.Clamp(requestedLimit, 1, 1000);
        try
        {
            var generationId = IndexStore.Exists(repoRoot) ? IndexStore.ReadManifest(repoRoot).GenerationId : string.Empty;
            var offset = string.IsNullOrWhiteSpace(token) ? 0 : continuationTokens.Decode(token, tool, generationId);
            page = new PageRequest(tool, generationId, offset, limit, checked(Math.Min(1001, offset + limit + 1)));
            error = null;
            return true;
        }
        catch (Exception ex) when (ex is ContinuationTokenException or OverflowException)
        {
            page = default;
            error = new RoslynMcpError("invalid-continuation-token", ex.Message, false, "Restart paging without a continuation token.");
            return false;
        }
    }

    private static RoslynMcpToolResult<T> PagingFailure<T>(string tool, string repoRoot, RoslynMcpError error)
        => new(false, tool, repoRoot, 0, Array.Empty<string>(), new[] { error }, default, 2);

    private static RoslynMcpToolResult<T> ToToolResult<T>(string tool, CommandResponse<T> response, bool truncated = false, string? continuationToken = null)
        => new(
            response.Success,
            tool,
            response.RepoRoot,
            response.ElapsedMs ?? 0,
            response.Warnings,
            response.Errors.Select(message => ToStructuredError(response.ExitCode, message)).ToArray(),
            response.Data,
            response.ExitCode,
            response.IndexUpdatedUtc,
            Cache: null,
            Truncated: truncated,
            ContinuationToken: continuationToken);

    private static RoslynMcpError ToStructuredError(int exitCode, string message)
        => exitCode switch
        {
            2 => new("invalid-request", message, false, "Correct the request arguments and retry."),
            3 => new("index-unavailable", message, true, "Run roslyn_index with force=true, then retry."),
            5 => new("timeout", message, true, "Retry with a larger timeout or smaller result budget."),
            _ => new("operation-failed", message, false, null)
        };

    private static SearchMode ParseMode(string mode)
        => Enum.TryParse<SearchMode>(mode, ignoreCase: true, out var parsed) ? parsed : SearchMode.All;

    private void RecordSessionQuery(string repoRoot, long? elapsedMs)
    {
        if (sessionRegistry is not null)
        {
            sessionRegistry.Get(repoRoot).RecordQuery(elapsedMs ?? 0);
        }
    }

    private readonly record struct PageRequest(string Tool, string GenerationId, int Offset, int Limit, int FetchCount);
}

public sealed class RepositoryBinding
{
    public RepositoryBinding(string repoRoot)
        => RepoRoot = Path.GetFullPath(repoRoot);

    public string RepoRoot { get; }

    public string Resolve(string? repoRoot)
        => Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? RepoRoot : repoRoot);
}
