using System.Text.Json;
using RoslynRepoIndexer.Core;

return await CliApp.RunAsync(args).ConfigureAwait(false);

internal static class CliApp
{
    internal static RepositorySessionRegistry? ServerSessions { get; set; }

    public static async Task<int> RunAsync(string[] args)
    {
        if (await CliQueryDaemon.TryHandleServerModeAsync(args).ConfigureAwait(false) is { } serverExitCode)
        {
            return serverExitCode;
        }

        if (await CliQueryDaemon.TryProxyAsync(args).ConfigureAwait(false) is { } proxy)
        {
            Console.Out.Write(proxy.StandardOutput);
            Console.Error.Write(proxy.StandardError);
            return proxy.ExitCode;
        }

        return await RunLocalAsync(args).ConfigureAwait(false);
    }

    internal static async Task<int> RunLocalAsync(string[] args)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            if (args.Length == 0 || Has(args, "--help") || Has(args, "-h"))
            {
                Console.WriteLine(Help.Global);
                return 0;
            }

            if (Has(args, "--version"))
            {
                Console.WriteLine("ri 0.1.0");
                return 0;
            }

            var command = args[0].ToLowerInvariant();
            var rest = args.Skip(1).ToArray();
            if (Has(rest, "--help"))
            {
                Console.WriteLine(Help.For(command));
                return 0;
            }

            return command switch
            {
                "index" => await Index(rest, command, stopwatch).ConfigureAwait(false),
                "read" => Read(rest, command, stopwatch),
                "pread" => PRead(rest, command, stopwatch),
                "search" => Search(rest, command, stopwatch),
                "refs" => await Refs(rest, command, stopwatch).ConfigureAwait(false),
                "goto" => Goto(rest, command, stopwatch),
                "symbols" => Symbols(rest, command, stopwatch),
                "doctor" => await Doctor(rest, command, stopwatch).ConfigureAwait(false),
                "status" => Status(rest, command, stopwatch),
                "clean" => Clean(rest, command, stopwatch),
                _ => Error(2, $"Unknown command '{command}'.", Has(rest, "--json"), command, null, null, stopwatch)
            };
        }
        catch (OperationCanceledException)
        {
            return Error(5, "Operation cancelled or timed out.", Has(args, "--json"), args.FirstOrDefault(), null, null, stopwatch);
        }
        catch (WorkspaceLoadingException ex)
        {
            return Error(2, ex.Message, Has(args, "--json"), args.FirstOrDefault(), null, null, stopwatch);
        }
        catch (NoCSharpDocumentsException ex)
        {
            return Error(4, ex.Message, Has(args, "--json"), args.FirstOrDefault(), null, null, stopwatch);
        }
        catch (IndexUnavailableException ex)
        {
            return Error(3, ex.Message, Has(args, "--json"), args.FirstOrDefault(), null, null, stopwatch);
        }
        catch (Exception ex)
        {
            return Error(4, ex.Message, Has(args, "--json"), args.FirstOrDefault(), null, null, stopwatch);
        }
    }

    private static async Task<int> Index(string[] args, string command, System.Diagnostics.Stopwatch stopwatch)
    {
        var options = Args.Parse(args);
        if (RejectUnknownOptions(options, command, stopwatch, "force", "json", "include-generated", "include-non-csharp-text", "max-text-file-bytes", "max-degree-of-parallelism", "config") is { } error)
        {
            return error;
        }

        var response = await CreateService().IndexAsync(new IndexCommandRequest(
            options.Positionals.FirstOrDefault() ?? Directory.GetCurrentDirectory(),
            options.Flag("force"),
            options.Value("include-generated") is not null || options.Flag("include-generated"),
            TryParseBool(options.Value("include-non-csharp-text")),
            TryParseLong(options.Value("max-text-file-bytes")),
            TryParseInt(options.Value("max-degree-of-parallelism")),
            options.Value("config"))).ConfigureAwait(false);

        if (options.Json)
        {
            WriteJson(response);
        }
        else
        {
            WriteWarnings(response.Warnings);

            if (response.Success && response.Data is { } summary)
            {
                Console.WriteLine($"Indexed {summary.RepoRoot}");
                Console.WriteLine($"documents: {summary.Documents}, symbols: {summary.Symbols}, references: {summary.References}, tokens: {summary.Tokens}, warnings: {summary.Warnings}, duration: {summary.Duration.TotalSeconds:0.00}s");
            }
            else
            {
                WriteErrors(response.Errors);
            }
        }

        return response.ExitCode;
    }

    private static int Search(string[] args, string command, System.Diagnostics.Stopwatch stopwatch)
    {
        var options = Args.Parse(args);
        if (RejectUnknownOptions(options, command, stopwatch, "mode", "kind", "path", "project", "from-file", "from-project", "include-tests", "exclude-tests", "include-generated", "limit", "timeout", "json") is { } error)
        {
            return error;
        }

        var query = string.Join(' ', options.Positionals);
        if (string.IsNullOrWhiteSpace(query))
        {
            return Error(2, "Missing search query.", options.Json, command, null, null, stopwatch);
        }

        if (!TryParseNonNegativeInt(options.Value("timeout"), out var timeoutMs))
        {
            return Error(2, "--timeout must be a non-negative number of milliseconds.", options.Json, command, query, null, stopwatch);
        }

        var response = CreateService().Search(new SearchCommandRequest(
            query,
            ParseMode(options.Value("mode") ?? "all"),
            ParseInt(options.Value("limit"), 50),
            options.Value("kind"),
            options.Value("path"),
            options.Value("project"),
            options.Flag("exclude-tests") ? false : options.Flag("include-tests") ? true : null,
            options.Value("from-file"),
            options.Value("from-project"),
            timeoutMs));
        return OutputResponse(response, options.Json, "No results.");
    }

    private static async Task<int> Refs(string[] args, string command, System.Diagnostics.Stopwatch stopwatch)
    {
        var options = Args.Parse(args);
        if (RejectUnknownOptions(options, command, stopwatch, "symbol-id", "exact", "timeout", "limit", "json") is { } error)
        {
            return error;
        }

        var query = options.Value("symbol-id") ?? string.Join(' ', options.Positionals);
        if (string.IsNullOrWhiteSpace(query))
        {
            return Error(2, "Missing symbol query.", options.Json, command, null, null, stopwatch);
        }

        if (!TryParseNullableInt(options.Value("timeout"), out var timeoutSeconds))
        {
            return Error(2, "--timeout must be a non-negative number of seconds.", options.Json, command, query, null, stopwatch);
        }

        var response = await CreateService().RefsAsync(new RefsCommandRequest(
            string.Join(' ', options.Positionals),
            options.Value("symbol-id"),
            options.Flag("exact"),
            timeoutSeconds,
            ParseInt(options.Value("limit"), 50))).ConfigureAwait(false);
        return OutputResponse(response, options.Json, "No references.");
    }

    private static int Goto(string[] args, string command, System.Diagnostics.Stopwatch stopwatch)
    {
        var options = Args.Parse(args);
        if (RejectUnknownOptions(options, command, stopwatch, "kind", "limit", "json") is { } error)
        {
            return error;
        }

        var query = string.Join(' ', options.Positionals);
        if (string.IsNullOrWhiteSpace(query))
        {
            return Error(2, "Missing symbol query.", options.Json, command, null, null, stopwatch);
        }

        var response = CreateService().Goto(new SymbolQueryCommandRequest(query, options.Value("kind"), ParseInt(options.Value("limit"), 20)));
        return OutputResponse(response, options.Json, "No declarations.");
    }

    private static int Symbols(string[] args, string command, System.Diagnostics.Stopwatch stopwatch)
    {
        var options = Args.Parse(args);
        if (RejectUnknownOptions(options, command, stopwatch, "prefix", "contains", "kind", "limit", "json") is { } error)
        {
            return error;
        }

        var root = RepositoryDiscovery.FindRoot(Directory.GetCurrentDirectory());
        if (!IndexStore.Exists(root.RootPath))
        {
            return Error(3, "Index is missing. Run 'ri index' first.", options.Json, command, null, root.RootPath, stopwatch);
        }

        var snapshot = IndexStore.Read(root.RootPath);
        var kinds = options.Value("kind")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var symbols = snapshot.Symbols.AsEnumerable();
        if (options.Value("prefix") is { } prefix)
        {
            symbols = symbols.Where(s => s.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        if (options.Value("contains") is { } contains)
        {
            symbols = symbols.Where(s => s.Name.Contains(contains, StringComparison.OrdinalIgnoreCase) || s.FullyQualifiedName.Contains(contains, StringComparison.OrdinalIgnoreCase));
        }

        if (kinds is not null)
        {
            symbols = symbols.Where(s => kinds.Contains(s.Kind));
        }

        Output(symbols.OrderBy(s => s.FullyQualifiedName, StringComparer.Ordinal).Take(ParseInt(options.Value("limit"), 100)).ToArray(), options.Json, "No symbols.", command, options.Value("prefix") ?? options.Value("contains"), root.RootPath, stopwatch, IndexUpdatedUtc(snapshot), includeResultsAlias: true);
        return 0;
    }

    private static async Task<int> Doctor(string[] args, string command, System.Diagnostics.Stopwatch stopwatch)
    {
        var options = Args.Parse(args);
        if (RejectUnknownOptions(options, command, stopwatch, "config", "deep", "json") is { } error)
        {
            return error;
        }

        var response = await CreateService().DoctorAsync(new PathCommandRequest(options.Positionals.FirstOrDefault() ?? Directory.GetCurrentDirectory(), options.Value("config"), options.Flag("deep"))).ConfigureAwait(false);
        return OutputResponse(response, options.Json, string.Empty);
    }

    private static int Status(string[] args, string command, System.Diagnostics.Stopwatch stopwatch)
    {
        var options = Args.Parse(args);
        if (RejectUnknownOptions(options, command, stopwatch, "json") is { } error)
        {
            return error;
        }

        var response = CreateService().Status(new PathCommandRequest(options.Positionals.FirstOrDefault() ?? Directory.GetCurrentDirectory()));
        return OutputResponse(response, options.Json, string.Empty);
    }

    private static int Clean(string[] args, string command, System.Diagnostics.Stopwatch stopwatch)
    {
        var options = Args.Parse(args);
        if (RejectUnknownOptions(options, command, stopwatch, "yes", "json") is { } error)
        {
            return error;
        }

        var path = options.Positionals.FirstOrDefault() ?? Directory.GetCurrentDirectory();
        if (!options.Flag("yes"))
        {
            return Error(1, "Refusing to delete index without --yes.", options.Json, command, null, null, stopwatch);
        }

        var root = RepositoryDiscovery.FindRoot(path);
        IndexStore.Clean(root.RootPath);
        Output(new { cleaned = true, repoRoot = root.RootPath }, options.Json, "Index cleaned.", command, null, root.RootPath, stopwatch, null, includeResultsAlias: false);
        return 0;
    }

    private static int Read(string[] args, string command, System.Diagnostics.Stopwatch stopwatch)
    {
        var options = Args.Parse(args);
        if (RejectUnknownOptions(options, command, stopwatch, "json", "config", "max-text-file-bytes") is { } error)
        {
            return error;
        }

        if (options.Positionals.Count != 1)
        {
            return Error(2, "Usage: ri read <filePath> [--json]", options.Json, command, null, null, stopwatch);
        }

        if (!TryParsePositiveLong(options.Value("max-text-file-bytes"), out var maxTextFileBytes))
        {
            return Error(2, "--max-text-file-bytes must be a positive number of bytes.", options.Json, command, null, null, stopwatch);
        }

        var response = CreateService().Read(new FileReadCommandRequest(options.Positionals[0], options.Value("config"), maxTextFileBytes));
        if (response.Success && response.Data is { } result)
        {
            if (options.Json)
            {
                WriteFileReadJson(result, response.Warnings, command, response.RepoRoot ?? result.RepoRoot, stopwatch);
            }
            else
            {
                WriteWarnings(response.Warnings);
                Console.Write(result.Content);
            }

            return 0;
        }

        return OutputResponse(response, options.Json, string.Empty);
    }

    private static int PRead(string[] args, string command, System.Diagnostics.Stopwatch stopwatch)
    {
        var options = Args.Parse(args);
        if (RejectUnknownOptions(options, command, stopwatch, "range", "around", "context", "json", "config", "max-text-file-bytes") is { } error)
        {
            return error;
        }

        if (options.Positionals.Count != 1)
        {
            return Error(2, "Usage: ri pread <filePath> (--range <startLine>:<endLine> | --around <line> [--context <lineCount>]) [--json]", options.Json, command, null, null, stopwatch);
        }

        var hasRange = options.Value("range") is not null;
        var hasAround = options.Value("around") is not null;
        if (!hasRange && !hasAround)
        {
            return Error(2, "Provide either --range or --around.", options.Json, command, null, null, stopwatch);
        }

        if (hasRange && hasAround)
        {
            return Error(2, "Use --range or --around, not both.", options.Json, command, null, null, stopwatch);
        }

        if (!TryParsePositiveLong(options.Value("max-text-file-bytes"), out var maxTextFileBytes))
        {
            return Error(2, "--max-text-file-bytes must be a positive number of bytes.", options.Json, command, null, null, stopwatch);
        }

        PartialFileReadCommandRequest request;
        if (hasRange)
        {
            if (!TryParseLineRange(options.Value("range"), out var startLine, out var endLine))
            {
                return Error(2, "--range must use <startLine>:<endLine> with 1-based line numbers.", options.Json, command, null, null, stopwatch);
            }

            request = new PartialFileReadCommandRequest(options.Positionals[0], startLine, endLine, null, 40, options.Value("config"), maxTextFileBytes);
        }
        else
        {
            if (!int.TryParse(options.Value("around"), out var targetLine))
            {
                return Error(2, "--around must be a 1-based line number.", options.Json, command, null, null, stopwatch);
            }

            if (!int.TryParse(options.Value("context") ?? "40", out var context))
            {
                return Error(2, "--context must be a non-negative line count.", options.Json, command, null, null, stopwatch);
            }

            request = new PartialFileReadCommandRequest(options.Positionals[0], null, null, targetLine, context, options.Value("config"), maxTextFileBytes);
        }

        var response = CreateService().PartialRead(request);
        if (response.Success && response.Data is { } result)
        {
            if (options.Json)
            {
                WritePartialFileReadJson(result, response.Warnings, command, response.RepoRoot ?? result.RepoRoot, stopwatch);
            }
            else
            {
                WriteWarnings(response.Warnings);
                Console.Write(result.Content);
            }

            return 0;
        }

        return OutputResponse(response, options.Json, string.Empty);
    }

    private static IndexerConfig ApplyIndexOptions(IndexerConfig config, Args options)
    {
        if (options.Value("include-generated") is not null || options.Flag("include-generated"))
        {
            config = config with { IncludeGenerated = true };
        }

        if (options.Value("include-non-csharp-text") is { } includeText && bool.TryParse(includeText, out var include))
        {
            config = config with { IncludeNonCSharpText = include };
        }

        if (long.TryParse(options.Value("max-text-file-bytes"), out var maxBytes))
        {
            config = config with { MaxTextFileBytes = maxBytes };
        }

        if (int.TryParse(options.Value("max-degree-of-parallelism"), out var parallelism))
        {
            config = config with { MaxDegreeOfParallelism = parallelism };
        }

        return config;
    }

    private static bool TryLoadReadConfig(
        string repoRoot,
        Args options,
        string command,
        System.Diagnostics.Stopwatch stopwatch,
        out IndexerConfig config,
        out IReadOnlyList<string> warnings,
        out int error)
    {
        var configLoad = ConfigLoader.Load(repoRoot, options.Value("config"));
        config = configLoad.Config;
        warnings = configLoad.Warnings;
        error = 0;
        if (options.Value("max-text-file-bytes") is not { } rawMaxBytes)
        {
            return true;
        }

        if (!long.TryParse(rawMaxBytes, out var maxBytes) || maxBytes <= 0)
        {
            error = Error(2, "--max-text-file-bytes must be a positive number of bytes.", options.Json, command, null, repoRoot, stopwatch);
            return false;
        }

        config = config with { MaxTextFileBytes = maxBytes };
        return true;
    }

    private static bool TryParseLineRange(string? rawRange, out int startLine, out int endLine)
    {
        startLine = 0;
        endLine = 0;
        if (string.IsNullOrWhiteSpace(rawRange))
        {
            return false;
        }

        var parts = rawRange.Split(':', 2);
        return parts.Length == 2
               && int.TryParse(parts[0], out startLine)
               && int.TryParse(parts[1], out endLine);
    }

    private static void WriteWarnings(IReadOnlyList<string> warnings)
    {
        foreach (var warning in warnings)
        {
            Console.Error.WriteLine("warning: " + warning);
        }
    }

    private static void WriteErrors(IReadOnlyList<string> errors)
    {
        foreach (var error in errors)
        {
            Console.Error.WriteLine(error);
        }
    }

    private static int OutputResponse<T>(CommandResponse<T> response, bool json, string emptyMessage)
    {
        if (json)
        {
            WriteJson(response);
            return response.ExitCode;
        }

        WriteWarnings(response.Warnings);
        if (!response.Success)
        {
            WriteErrors(response.Errors);
            return response.ExitCode;
        }

        Output(response.Data, json: false, emptyMessage: emptyMessage, response.Command, response.Query, response.RepoRoot, System.Diagnostics.Stopwatch.StartNew(), response.IndexUpdatedUtc, response.Results is not null, response.Warnings);
        return response.ExitCode;
    }

    private static void WriteFileReadJson(
        RepositoryFileReadResult result,
        IReadOnlyList<string> warnings,
        string command,
        string repoRoot,
        System.Diagnostics.Stopwatch stopwatch)
    {
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            success = true,
            exitCode = 0,
            result.FilePath,
            result.RepoRoot,
            result.Language,
            result.LineCount,
            result.SizeBytes,
            result.ContentHash,
            result.LastModifiedUtc,
            result.IsIndexed,
            result.Content,
            warnings,
            errors = Array.Empty<string>(),
            command,
            elapsedMs = stopwatch.ElapsedMilliseconds,
            indexUpdatedUtc = TryIndexUpdatedUtc(repoRoot),
            data = result
        }, JsonOptions.Default));
    }

    private static void WritePartialFileReadJson(
        RepositoryPartialFileReadResult result,
        IReadOnlyList<string> warnings,
        string command,
        string repoRoot,
        System.Diagnostics.Stopwatch stopwatch)
    {
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            success = true,
            exitCode = 0,
            result.FilePath,
            result.RepoRoot,
            result.Language,
            result.LineCount,
            result.SizeBytes,
            result.ContentHash,
            result.LastModifiedUtc,
            result.IsIndexed,
            result.SelectionMode,
            result.TargetLine,
            result.Context,
            result.StartLine,
            result.EndLine,
            result.SelectedLineCount,
            result.Content,
            warnings,
            errors = Array.Empty<string>(),
            command,
            elapsedMs = stopwatch.ElapsedMilliseconds,
            indexUpdatedUtc = TryIndexUpdatedUtc(repoRoot),
            data = result
        }, JsonOptions.Default));
    }

    private static SearchMode ParseMode(string mode)
        => Enum.TryParse<SearchMode>(mode, ignoreCase: true, out var parsed) ? parsed : SearchMode.All;

    private static int ParseInt(string? value, int fallback)
        => int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;

    private static int? TryParseInt(string? value)
        => int.TryParse(value, out var parsed) ? parsed : null;

    private static long? TryParseLong(string? value)
        => long.TryParse(value, out var parsed) ? parsed : null;

    private static bool? TryParseBool(string? value)
        => bool.TryParse(value, out var parsed) ? parsed : null;

    private static bool TryParsePositiveLong(string? value, out long? parsed)
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!long.TryParse(value, out var number) || number <= 0)
        {
            return false;
        }

        parsed = number;
        return true;
    }

    private static bool TryParseNullableInt(string? value, out int? parsed)
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!int.TryParse(value, out var number) || number < 0)
        {
            return false;
        }

        parsed = number;
        return true;
    }

    private static bool TryParseNonNegativeInt(string? value, out int? parsed)
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!int.TryParse(value, out var number) || number < 0)
        {
            return false;
        }

        parsed = number;
        return true;
    }

    private static bool TryParseExactRefsTimeout(string? value, int configuredSeconds, out int timeoutSeconds)
    {
        timeoutSeconds = configuredSeconds;
        if (value is null)
        {
            return configuredSeconds >= 0;
        }

        if (!int.TryParse(value, out var parsed) || parsed < 0)
        {
            return false;
        }

        timeoutSeconds = parsed;
        return true;
    }

    private static int? RejectUnknownOptions(Args options, string command, System.Diagnostics.Stopwatch stopwatch, params string[] allowed)
    {
        var allowedSet = allowed.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = options.OptionNames.FirstOrDefault(name => !allowedSet.Contains(name));
        return unknown is null
            ? null
            : Error(2, $"Unknown option '--{unknown}' for command '{command}'.", options.Json, command, null, null, stopwatch);
    }

    private static bool Has(IEnumerable<string> args, string name)
        => args.Contains(name, StringComparer.OrdinalIgnoreCase);

    private static RoslynIndexerApplicationService CreateService()
        => ServerSessions is null
            ? new RoslynIndexerApplicationService()
            : new RoslynIndexerApplicationService(queryIndexLoader: repoRoot => ServerSessions.Get(repoRoot).GetQueryIndex());

    private static void Output<T>(
        T data,
        bool json,
        string emptyMessage,
        string? command,
        string? query,
        string? repoRoot,
        System.Diagnostics.Stopwatch stopwatch,
        DateTimeOffset? indexUpdatedUtc,
        bool includeResultsAlias,
        IReadOnlyList<string>? warnings = null)
    {
        if (json)
        {
            WriteJson(CommandResponse.Success(data, warnings, command, query, repoRoot, stopwatch.ElapsedMilliseconds, indexUpdatedUtc, includeResultsAlias));
            return;
        }

        if (data is System.Collections.IEnumerable enumerable and not string)
        {
            var items = enumerable.Cast<object>().ToArray();
            if (items.Length == 0)
            {
                if (!string.IsNullOrWhiteSpace(emptyMessage))
                {
                    Console.WriteLine(emptyMessage);
                }

                return;
            }

            if (items.All(item => item is SearchResult))
            {
                foreach (var result in items.Cast<SearchResult>())
                {
                    WriteHumanSearchResult(result);
                }

                if (items.Length > 1)
                {
                    Console.WriteLine($"showing {items.Length} of {items.Length}");
                }

                return;
            }

            foreach (var item in enumerable)
            {
                Console.WriteLine(JsonSerializer.Serialize(item, JsonOptions.Default));
            }

            return;
        }

        Console.WriteLine(JsonSerializer.Serialize(data, JsonOptions.Default));
    }

    private static void WriteHumanSearchResult(SearchResult result)
    {
        var title = result.FullyQualifiedName
                    ?? result.SymbolName
                    ?? result.Path;
        var location = $"{result.Path}:{result.Line}:{result.Column}";
        var score = result.Score.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        var suffix = result.ReferenceKind is null ? string.Empty : $"  ref-kind={result.ReferenceKind}";
        Console.WriteLine($"[{result.Kind}] {title}  {location}  score={score}{suffix}");
        if (!string.IsNullOrWhiteSpace(result.Snippet))
        {
            Console.WriteLine("    " + result.Snippet.Trim());
        }
    }

    private static void WriteJson<T>(CommandResponse<T> response)
        => Console.WriteLine(JsonSerializer.Serialize(response, JsonOptions.Default));

    private static int Error(int exitCode, string message, bool json, string? command, string? query, string? repoRoot, System.Diagnostics.Stopwatch stopwatch)
    {
        if (json)
        {
            WriteJson(CommandResponse<object>.Failure(exitCode, new[] { message }, command: command, query: query, repoRoot: repoRoot, elapsedMs: stopwatch.ElapsedMilliseconds));
        }
        else
        {
            Console.Error.WriteLine(message);
        }

        return exitCode;
    }

    private static int ErrorWithData<T>(
        int exitCode,
        T data,
        string message,
        bool json,
        string? command,
        string? query,
        string? repoRoot,
        System.Diagnostics.Stopwatch stopwatch,
        DateTimeOffset? indexUpdatedUtc)
    {
        if (json)
        {
            var response = new CommandResponse<T>(
                false,
                exitCode,
                data,
                Array.Empty<string>(),
                new[] { message },
                command,
                query,
                repoRoot,
                stopwatch.ElapsedMilliseconds,
                indexUpdatedUtc,
                data);
            Console.WriteLine(JsonSerializer.Serialize(response, JsonOptions.Default));
        }
        else
        {
            Console.Error.WriteLine(message);
            if (data is System.Collections.IEnumerable enumerable and not string)
            {
                foreach (var item in enumerable)
                {
                    Console.WriteLine(JsonSerializer.Serialize(item, JsonOptions.Default));
                }
            }
        }

        return exitCode;
    }

    private static DateTimeOffset? IndexUpdatedUtc(IndexSnapshot snapshot)
        => snapshot.Manifest.UpdatedUtc;

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

}

internal sealed class Args
{
    private readonly Dictionary<string, string?> options;

    private Args(List<string> positionals, Dictionary<string, string?> options)
    {
        Positionals = positionals;
        this.options = options;
    }

    public IReadOnlyList<string> Positionals { get; }
    public IReadOnlyCollection<string> OptionNames => options.Keys;
    public bool Json => Flag("json");
    public bool Flag(string name) => options.ContainsKey(name);
    public string? Value(string name) => options.TryGetValue(name, out var value) ? value : null;

    public static Args Parse(string[] args)
    {
        var positionals = new List<string>();
        var options = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                positionals.Add(arg);
                continue;
            }

            var name = arg[2..];
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                options[name] = args[++i];
            }
            else
            {
                options[name] = null;
            }
        }

        return new Args(positionals, options);
    }
}

internal static class Help
{
    public const string Global = """
        ri - local Roslyn repository indexer

        Usage:
          ri index [path] [--force] [--json]
          ri read <filePath> [--json]
          ri pread <filePath> (--range <startLine>:<endLine> | --around <line> [--context <lineCount>]) [--json]
          ri search <query> [--mode all|symbol|text|file|reference] [--json]
          ri refs <symbol> [--exact] [--json]
          ri goto <symbol> [--json]
          ri symbols [--prefix text] [--contains text] [--json]
          ri doctor [path] [--json]
          ri status [path] [--json]
          ri clean [path] --yes
          ri --version
        """;

    public static string For(string command)
        => command switch
        {
            "index" => "ri index [path] --force --json --include-generated --include-non-csharp-text true|false --max-text-file-bytes <bytes>",
            "read" => "ri read <filePath> --json --max-text-file-bytes <bytes>",
            "pread" => "ri pread <filePath> --range <startLine>:<endLine> --json\nri pread <filePath> --around <line> --context <lineCount> --json",
            "search" => "ri search <query> --mode all|symbol|text|file|reference --kind <kinds> --path <text> --from-file <path> --from-project <name> --limit <n> --json",
            "refs" => "ri refs <symbol> --symbol-id <id> --exact --timeout <seconds> --json",
            "goto" => "ri goto <symbol> --limit <n> --json",
            "symbols" => "ri symbols --prefix <prefix> --contains <text> --kind <kinds> --limit <n> --json",
            "doctor" => "ri doctor [path] --json",
            "status" => "ri status [path] --json",
            "clean" => "ri clean [path] --yes",
            _ => Global
        };
}
