namespace RoslynRepoIndexer.Core;

public interface IRoslynIndexerApplicationService
{
    Task<CommandResponse<DoctorSummary>> DoctorAsync(PathCommandRequest request, CancellationToken cancellationToken = default);
    Task<CommandResponse<IndexSummary>> IndexAsync(IndexCommandRequest request, CancellationToken cancellationToken = default);
    CommandResponse<StatusSummary> Status(PathCommandRequest request);
    CommandResponse<IReadOnlyList<SearchResult>> Search(SearchCommandRequest request);
    CommandResponse<RepositoryFileReadResult> Read(FileReadCommandRequest request);
    CommandResponse<RepositoryPartialFileReadResult> PartialRead(PartialFileReadCommandRequest request);
    CommandResponse<IReadOnlyList<SearchResult>> Goto(SymbolQueryCommandRequest request);
    Task<CommandResponse<object>> RefsAsync(RefsCommandRequest request, CancellationToken cancellationToken = default);
    CommandResponse<object> Suggest(SuggestCommandRequest request);
}

public sealed record PathCommandRequest(string? Path = null, string? ConfigPath = null, bool Deep = false);

public sealed record IndexCommandRequest(
    string? Path = null,
    bool Force = false,
    bool IncludeGenerated = false,
    bool? IncludeNonCSharpText = null,
    long? MaxTextFileBytes = null,
    int? MaxDegreeOfParallelism = null,
    string? ConfigPath = null);

public sealed record SearchCommandRequest(
    string? Query,
    SearchMode Mode = SearchMode.All,
    int Limit = 50,
    string? Kind = null,
    string? Path = null,
    string? Project = null,
    bool? IncludeTests = null,
    string? FromFile = null,
    string? FromProject = null,
    int? TimeoutMs = null);

public sealed record FileReadCommandRequest(
    string? FilePath,
    string? ConfigPath = null,
    long? MaxTextFileBytes = null);

public sealed record PartialFileReadCommandRequest(
    string? FilePath,
    int? StartLine = null,
    int? EndLine = null,
    int? AroundLine = null,
    int Context = 40,
    string? ConfigPath = null,
    long? MaxTextFileBytes = null);

public sealed record SymbolQueryCommandRequest(
    string? Query,
    string? Kind = null,
    int Limit = 20);

public sealed record RefsCommandRequest(
    string? Query,
    string? SymbolId = null,
    bool Exact = false,
    int? TimeoutSeconds = null,
    int Limit = 50);

public sealed record SuggestCommandRequest(
    string? Question,
    int Limit = 5,
    int ExecuteTop = 0);

public sealed record SuggestExecutionResponse(
    IReadOnlyList<QuerySuggestion> Suggestions,
    IReadOnlyList<SuggestExecutedResult> ExecutedResults);

public sealed record SuggestExecutedResult(
    QuerySuggestion Suggestion,
    IReadOnlyList<SearchResult> Results);

public sealed class RoslynIndexerApplicationService : IRoslynIndexerApplicationService
{
    private readonly string workingDirectory;
    private readonly Func<string, QueryIndex>? queryIndexLoader;
    private readonly IndexBuilder indexBuilder;

    public RoslynIndexerApplicationService(string? workingDirectory = null, Func<string, QueryIndex>? queryIndexLoader = null, IndexBuilder? indexBuilder = null)
    {
        this.workingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? Directory.GetCurrentDirectory() : workingDirectory;
        this.queryIndexLoader = queryIndexLoader;
        this.indexBuilder = indexBuilder ?? new IndexBuilder();
    }

    public async Task<CommandResponse<IndexSummary>> IndexAsync(IndexCommandRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        const string command = "index";
        try
        {
            var path = ResolveInputPath(request.Path);
            var root = RepositoryDiscovery.FindRoot(path);
            var configLoad = ConfigLoader.Load(root.RootPath, request.ConfigPath);
            var config = ApplyIndexOptions(configLoad.Config, request);
            var summary = await indexBuilder.BuildAsync(path, request.Force, config, cancellationToken).ConfigureAwait(false);
            return CommandResponse.Success(summary, configLoad.Warnings, command, null, summary.RepoRoot, stopwatch.ElapsedMilliseconds, DateTimeOffset.UtcNow, includeResultsAlias: false);
        }
        catch (Exception ex) when (IsKnownFailure(ex, out var exitCode))
        {
            return Failure<IndexSummary>(exitCode, ErrorMessage(ex), command, null, null, stopwatch);
        }
    }

    public async Task<CommandResponse<DoctorSummary>> DoctorAsync(PathCommandRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        const string command = "doctor";
        try
        {
            var path = ResolveInputPath(request.Path);
            var root = RepositoryDiscovery.FindRoot(path);
            var config = ConfigLoader.Load(root.RootPath, request.ConfigPath).Config;
            var summary = await new DoctorService().RunAsync(path, config, request.Deep, cancellationToken).ConfigureAwait(false);
            return CommandResponse.Success(summary, null, command, null, root.RootPath, stopwatch.ElapsedMilliseconds, TryIndexUpdatedUtc(root.RootPath), includeResultsAlias: false);
        }
        catch (Exception ex) when (IsKnownFailure(ex, out var exitCode))
        {
            return Failure<DoctorSummary>(exitCode, ErrorMessage(ex), command, null, null, stopwatch);
        }
    }

    public CommandResponse<StatusSummary> Status(PathCommandRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        const string command = "status";
        try
        {
            var path = ResolveInputPath(request.Path);
            var root = RepositoryDiscovery.FindRoot(path);
            var status = IndexStore.GetStatus(root.RootPath);
            return CommandResponse.Success(status, null, command, null, root.RootPath, stopwatch.ElapsedMilliseconds, TryIndexUpdatedUtc(root.RootPath), includeResultsAlias: false);
        }
        catch (Exception ex) when (IsKnownFailure(ex, out var exitCode))
        {
            return Failure<StatusSummary>(exitCode, ErrorMessage(ex), command, null, null, stopwatch);
        }
    }

    public CommandResponse<IReadOnlyList<SearchResult>> Search(SearchCommandRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        const string command = "search";
        var query = request.Query?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return Failure<IReadOnlyList<SearchResult>>(2, "Missing search query.", command, null, null, stopwatch);
        }

        try
        {
            var root = RepositoryDiscovery.FindRoot(workingDirectory);
            if (!IndexStore.Exists(root.RootPath))
            {
                return Failure<IReadOnlyList<SearchResult>>(3, "Index is missing. Run 'ri index' first.", command, query, root.RootPath, stopwatch);
            }

            var searchLoadStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var queryIndex = LoadQueryIndex(root.RootPath);
            var snapshot = queryIndex.Snapshot;
            searchLoadStopwatch.Stop();
            if (request.TimeoutMs is < 0)
            {
                return Failure<IReadOnlyList<SearchResult>>(2, "--timeout must be a non-negative number of milliseconds.", command, query, root.RootPath, stopwatch);
            }

            var snippets = new SnippetReader(root.RootPath);
            var execution = new SearchService(queryIndex, (path, line) => snippets.ReadSnippet(path, line)).SearchDetailed(
                new SearchRequest(query, request.Mode, PositiveOrDefault(request.Limit, 50), request.Kind, request.Path, request.Project, request.IncludeTests, request.FromFile, request.FromProject, request.TimeoutMs),
                searchLoadStopwatch.ElapsedMilliseconds);
            var warnings = execution.TimedOut
                ? new[] { $"Search timeout reached after {request.TimeoutMs} ms; returned {execution.Results.Count} partial results. searchLoadMs={execution.SearchLoadMs}; searchScoreMs={execution.SearchScoreMs}." }
                : Array.Empty<string>();
            if (execution.TimedOut && execution.Results.Count == 0)
            {
                return new CommandResponse<IReadOnlyList<SearchResult>>(false, 5, execution.Results, warnings, Array.Empty<string>(), command, query, root.RootPath, stopwatch.ElapsedMilliseconds, IndexUpdatedUtc(snapshot), execution.Results);
            }

            return CommandResponse.Success<IReadOnlyList<SearchResult>>(execution.Results, warnings, command, query, root.RootPath, stopwatch.ElapsedMilliseconds, IndexUpdatedUtc(snapshot), includeResultsAlias: true);
        }
        catch (Exception ex) when (IsKnownFailure(ex, out var exitCode))
        {
            return Failure<IReadOnlyList<SearchResult>>(exitCode, ErrorMessage(ex), command, query, null, stopwatch);
        }
    }

    public CommandResponse<RepositoryFileReadResult> Read(FileReadCommandRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        const string command = "read";
        try
        {
            var root = RepositoryDiscovery.FindRoot(workingDirectory);
            var configLoad = LoadReadConfig(root.RootPath, request.ConfigPath, request.MaxTextFileBytes);
            var result = new RepositoryFileReader().Read(root.RootPath, request.FilePath ?? string.Empty, configLoad.Config);
            return CommandResponse.Success(result, configLoad.Warnings, command, null, root.RootPath, stopwatch.ElapsedMilliseconds, TryIndexUpdatedUtc(root.RootPath), includeResultsAlias: false);
        }
        catch (Exception ex) when (IsKnownFailure(ex, out var exitCode))
        {
            return Failure<RepositoryFileReadResult>(exitCode, ErrorMessage(ex), command, null, TryRepoRoot(), stopwatch);
        }
    }

    public CommandResponse<RepositoryPartialFileReadResult> PartialRead(PartialFileReadCommandRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        const string command = "pread";
        try
        {
            if (string.IsNullOrWhiteSpace(request.FilePath))
            {
                return Failure<RepositoryPartialFileReadResult>(2, "Usage: ri pread <filePath> (--range <startLine>:<endLine> | --around <line> [--context <lineCount>]) [--json]", command, null, TryRepoRoot(), stopwatch);
            }

            var hasRange = request.StartLine is not null || request.EndLine is not null;
            var hasAround = request.AroundLine is not null;
            if (!hasRange && !hasAround)
            {
                return Failure<RepositoryPartialFileReadResult>(2, "Provide either --range or --around.", command, null, TryRepoRoot(), stopwatch);
            }

            if (hasRange && hasAround)
            {
                return Failure<RepositoryPartialFileReadResult>(2, "Use --range or --around, not both.", command, null, TryRepoRoot(), stopwatch);
            }

            var root = RepositoryDiscovery.FindRoot(workingDirectory);
            var configLoad = LoadReadConfig(root.RootPath, request.ConfigPath, request.MaxTextFileBytes);
            var reader = new RepositoryFileReader();
            RepositoryPartialFileReadResult result;
            if (hasRange)
            {
                if (request.StartLine is null || request.EndLine is null)
                {
                    return Failure<RepositoryPartialFileReadResult>(2, "--range must use <startLine>:<endLine> with 1-based line numbers.", command, null, root.RootPath, stopwatch);
                }

                result = reader.ReadRange(root.RootPath, request.FilePath, configLoad.Config, request.StartLine.Value, request.EndLine.Value);
            }
            else
            {
                result = reader.ReadAround(root.RootPath, request.FilePath, configLoad.Config, request.AroundLine!.Value, request.Context);
            }

            return CommandResponse.Success(result, configLoad.Warnings, command, null, root.RootPath, stopwatch.ElapsedMilliseconds, TryIndexUpdatedUtc(root.RootPath), includeResultsAlias: false);
        }
        catch (Exception ex) when (IsKnownFailure(ex, out var exitCode))
        {
            return Failure<RepositoryPartialFileReadResult>(exitCode, ErrorMessage(ex), command, null, TryRepoRoot(), stopwatch);
        }
    }

    public CommandResponse<IReadOnlyList<SearchResult>> Goto(SymbolQueryCommandRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        const string command = "goto";
        var query = request.Query?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return Failure<IReadOnlyList<SearchResult>>(2, "Missing symbol query.", command, null, null, stopwatch);
        }

        try
        {
            var root = RepositoryDiscovery.FindRoot(workingDirectory);
            if (!IndexStore.Exists(root.RootPath))
            {
                return Failure<IReadOnlyList<SearchResult>>(3, "Index is missing. Run 'ri index' first.", command, query, root.RootPath, stopwatch);
            }

            var queryIndex = LoadQueryIndex(root.RootPath);
            var snapshot = queryIndex.Snapshot;
            var snippets = new SnippetReader(root.RootPath);
            var results = new SearchService(queryIndex, (path, line) => snippets.ReadSnippet(path, line)).Search(new SearchRequest(query, SearchMode.Symbol, PositiveOrDefault(request.Limit, 20), request.Kind));
            return CommandResponse.Success<IReadOnlyList<SearchResult>>(results, null, command, query, root.RootPath, stopwatch.ElapsedMilliseconds, IndexUpdatedUtc(snapshot), includeResultsAlias: true);
        }
        catch (Exception ex) when (IsKnownFailure(ex, out var exitCode))
        {
            return Failure<IReadOnlyList<SearchResult>>(exitCode, ErrorMessage(ex), command, query, null, stopwatch);
        }
    }

    public async Task<CommandResponse<object>> RefsAsync(RefsCommandRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        const string command = "refs";
        var query = request.SymbolId ?? request.Query?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return Failure<object>(2, "Missing symbol query.", command, null, null, stopwatch);
        }

        try
        {
            var root = RepositoryDiscovery.FindRoot(workingDirectory);
            if (!IndexStore.Exists(root.RootPath))
            {
                return Failure<object>(3, "Index is missing. Run 'ri index' first.", command, query, root.RootPath, stopwatch);
            }

            var queryIndex = LoadQueryIndex(root.RootPath);
            var snapshot = queryIndex.Snapshot;
            var candidates = FindSymbolCandidates(snapshot, query);
            if (candidates.Length > 1 && request.SymbolId is null)
            {
                var candidateData = candidates.Select(c => new { c.SymbolId, c.Kind, c.FullyQualifiedName, c.Path, c.Line }).ToArray();
                return new CommandResponse<object>(false, 1, candidateData, Array.Empty<string>(), new[] { "Multiple candidates. Re-run with --symbol-id." }, command, query, root.RootPath, stopwatch.ElapsedMilliseconds, IndexUpdatedUtc(snapshot), candidateData);
            }

            IReadOnlyList<SearchResult> results;
            if (request.Exact)
            {
                var config = ConfigLoader.Load(root.RootPath, explicitPath: null).Config;
                var timeoutSeconds = request.TimeoutSeconds ?? config.ExactRefsTimeoutSeconds;
                if (timeoutSeconds < 0)
                {
                    return Failure<object>(2, "--timeout must be a non-negative number of seconds.", command, query, root.RootPath, stopwatch);
                }

                results = await new ExactReferenceService().FindExactAsync(root.RootPath, query, timeoutSeconds, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var symbol = candidates.FirstOrDefault();
                var requestQuery = symbol?.Name ?? query;
                var snippets = new SnippetReader(root.RootPath);
                results = new SearchService(queryIndex, (path, line) => snippets.ReadSnippet(path, line)).Search(new SearchRequest(requestQuery, SearchMode.Reference, PositiveOrDefault(request.Limit, 50)));
            }

            return CommandResponse.Success<object>(results, null, command, query, root.RootPath, stopwatch.ElapsedMilliseconds, TryIndexUpdatedUtc(root.RootPath), includeResultsAlias: true);
        }
        catch (Exception ex) when (IsKnownFailure(ex, out var exitCode))
        {
            return Failure<object>(exitCode, ErrorMessage(ex), command, query, null, stopwatch);
        }
    }

    public CommandResponse<object> Suggest(SuggestCommandRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        const string command = "suggest";
        var question = request.Question?.Trim();
        if (string.IsNullOrWhiteSpace(question))
        {
            return Failure<object>(1, "Missing question.", command, null, null, stopwatch);
        }

        try
        {
            var root = RepositoryDiscovery.FindRoot(workingDirectory);
            if (!IndexStore.Exists(root.RootPath))
            {
                return Failure<object>(3, "Index is missing. Run 'ri index' first.", command, question, root.RootPath, stopwatch);
            }

            var queryIndex = LoadQueryIndex(root.RootPath);
            var snapshot = queryIndex.Snapshot;
            var suggestions = new SuggestionService(snapshot).Suggest(question, PositiveOrDefault(request.Limit, 5));
            object data = suggestions;
            if (request.ExecuteTop > 0)
            {
                var snippets = new SnippetReader(root.RootPath);
                var search = new SearchService(queryIndex, (path, line) => snippets.ReadSnippet(path, line));
                data = new SuggestExecutionResponse(
                    suggestions,
                    suggestions.Take(request.ExecuteTop)
                        .Select(s => new SuggestExecutedResult(s, search.Search(new SearchRequest(s.Query, ParseMode(s.Mode), 10))))
                        .ToArray());
            }

            return CommandResponse.Success(data, null, command, question, root.RootPath, stopwatch.ElapsedMilliseconds, IndexUpdatedUtc(snapshot), includeResultsAlias: true);
        }
        catch (Exception ex) when (IsKnownFailure(ex, out var exitCode))
        {
            return Failure<object>(exitCode, ErrorMessage(ex), command, question, null, stopwatch);
        }
    }

    private string ResolveInputPath(string? path)
        => string.IsNullOrWhiteSpace(path) ? workingDirectory : Path.GetFullPath(Path.IsPathFullyQualified(path) ? path : Path.Combine(workingDirectory, path));

    private static IndexerConfig ApplyIndexOptions(IndexerConfig config, IndexCommandRequest request)
    {
        if (request.IncludeGenerated)
        {
            config = config with { IncludeGenerated = true };
        }

        if (request.IncludeNonCSharpText is { } includeText)
        {
            config = config with { IncludeNonCSharpText = includeText };
        }

        if (request.MaxTextFileBytes is { } maxBytes)
        {
            config = config with { MaxTextFileBytes = maxBytes };
        }

        if (request.MaxDegreeOfParallelism is { } parallelism)
        {
            config = config with { MaxDegreeOfParallelism = parallelism };
        }

        return config;
    }

    private static ConfigLoadResult LoadReadConfig(string repoRoot, string? configPath, long? maxTextFileBytes)
    {
        var configLoad = ConfigLoader.Load(repoRoot, configPath);
        if (maxTextFileBytes is null)
        {
            return configLoad;
        }

        if (maxTextFileBytes <= 0)
        {
            throw new RepositoryFileReadException("--max-text-file-bytes must be a positive number of bytes.");
        }

        return configLoad with { Config = configLoad.Config with { MaxTextFileBytes = maxTextFileBytes.Value } };
    }

    private string? TryRepoRoot()
    {
        try
        {
            return RepositoryDiscovery.FindRoot(workingDirectory).RootPath;
        }
        catch
        {
            return null;
        }
    }

    private static CommandResponse<T> Failure<T>(int exitCode, string message, string command, string? query, string? repoRoot, System.Diagnostics.Stopwatch stopwatch)
        => CommandResponse<T>.Failure(exitCode, new[] { message }, command: command, query: query, repoRoot: repoRoot, elapsedMs: stopwatch.ElapsedMilliseconds);

    private static string ErrorMessage(Exception exception)
        => exception is OperationCanceledException ? "Operation cancelled or timed out." : exception.Message;

    private static bool IsKnownFailure(Exception exception, out int exitCode)
    {
        exitCode = exception switch
        {
            OperationCanceledException => 5,
            WorkspaceLoadingException => 2,
            NoCSharpDocumentsException => 4,
            IndexUnavailableException => 3,
            RepositoryFileReadException readException => readException.ExitCode,
            FileNotFoundException when exception.Message.Contains("Index is missing", StringComparison.OrdinalIgnoreCase) => 3,
            _ => 4
        };
        return true;
    }

    private static int PositiveOrDefault(int value, int fallback)
        => value > 0 ? value : fallback;

    private static SearchMode ParseMode(string mode)
        => Enum.TryParse<SearchMode>(mode, ignoreCase: true, out var parsed) ? parsed : SearchMode.All;

    private static DateTimeOffset? IndexUpdatedUtc(IndexSnapshot snapshot)
        => snapshot.Manifest.UpdatedUtc;

    private QueryIndex LoadQueryIndex(string repoRoot)
        => queryIndexLoader?.Invoke(repoRoot) ?? new QueryIndex(IndexStore.Read(repoRoot));

    private static DateTimeOffset? TryIndexUpdatedUtc(string repoRoot)
    {
        try
        {
            return IndexStore.Read(repoRoot).Manifest.UpdatedUtc;
        }
        catch
        {
            return null;
        }
    }

    private static SymbolEntry[] FindSymbolCandidates(IndexSnapshot snapshot, string query)
    {
        var exact = snapshot.Symbols
            .Where(s => string.Equals(s.SymbolId, query, StringComparison.Ordinal)
                        || string.Equals(s.Name, query, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(s.FullyQualifiedName, query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.FullyQualifiedName, StringComparer.Ordinal)
            .ThenBy(s => s.Path, StringComparer.Ordinal)
            .ThenBy(s => s.Line)
            .Take(10)
            .ToArray();
        if (exact.Length > 0)
        {
            return exact;
        }

        return snapshot.Symbols
            .Where(s => s.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || s.FullyQualifiedName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.FullyQualifiedName, StringComparer.Ordinal)
            .ThenBy(s => s.Path, StringComparer.Ordinal)
            .ThenBy(s => s.Line)
            .Take(10)
            .ToArray();
    }
}
