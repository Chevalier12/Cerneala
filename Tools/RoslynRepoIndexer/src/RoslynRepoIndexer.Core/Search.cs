namespace RoslynRepoIndexer.Core;

public static class SearchScorer
{
    public static double ScoreSymbol(string fullyQualifiedName, string name, string query)
        => ScoreSymbolMatch(fullyQualifiedName, name, query).Score;

    public static SearchScore ScoreSymbolMatch(string fullyQualifiedName, string name, string query)
    {
        if (string.Equals(fullyQualifiedName, query, StringComparison.Ordinal))
        {
            return new SearchScore(1000, "exact-fqn");
        }

        if (string.Equals(name, query, StringComparison.OrdinalIgnoreCase))
        {
            return new SearchScore(800, "exact-symbol");
        }

        if (name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return new SearchScore(600, "prefix-symbol");
        }

        if (IsCamelCaseAcronymMatch(name, query))
        {
            return new SearchScore(500, "acronym-symbol");
        }

        if (name.Contains(query, StringComparison.OrdinalIgnoreCase) || fullyQualifiedName.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return new SearchScore(350, "contains-symbol");
        }

        var queryTokens = Tokenizer.NormalizeTerms(query);
        var nameTokens = Tokenizer.NormalizeTerms(name + " " + fullyQualifiedName);
        var hits = queryTokens.Count(q => nameTokens.Contains(q, StringComparer.Ordinal));
        return hits == 0 ? SearchScore.None : new SearchScore(hits * 250, "token-overlap");
    }

    private static bool IsCamelCaseAcronymMatch(string name, string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Any(char.IsWhiteSpace))
        {
            return false;
        }

        var acronym = new string(name.Where(char.IsUpper).ToArray());
        return acronym.Length > 0 && string.Equals(acronym, query, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record SearchScore(double Score, string Reason)
{
    public static SearchScore None { get; } = new(0, string.Empty);
}

public sealed class SearchService
{
    private readonly IndexSnapshot snapshot;
    private readonly Func<string, int, string> readSnippet;
    private readonly IReadOnlyDictionary<string, SymbolEntry> symbolsById;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<SymbolEntry>> symbolsByLowerName;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<SymbolEntry>> symbolsByLowerFullyQualifiedName;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<TokenPosting>> tokenToPostings;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<ReferenceEntry>> referencesBySymbolId;
    private readonly IReadOnlyDictionary<string, DocumentEntry> documentsById;
    private readonly IReadOnlyDictionary<string, DocumentEntry> documentsByPath;

    public SearchService(IndexSnapshot snapshot, SnippetReader snippets)
        : this(snapshot, (path, line) => snippets.ReadSnippet(path, line))
    {
    }

    public SearchService(IndexSnapshot snapshot, Func<string, int, string> readSnippet)
    {
        this.snapshot = snapshot;
        this.readSnippet = readSnippet;
        symbolsById = snapshot.Symbols
            .GroupBy(symbol => symbol.SymbolId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        symbolsByLowerName = snapshot.Symbols
            .GroupBy(symbol => symbol.Name.ToLowerInvariant(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<SymbolEntry>)group.ToArray(), StringComparer.Ordinal);
        symbolsByLowerFullyQualifiedName = snapshot.Symbols
            .GroupBy(symbol => symbol.FullyQualifiedName.ToLowerInvariant(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<SymbolEntry>)group.ToArray(), StringComparer.Ordinal);
        tokenToPostings = snapshot.Tokens
            .GroupBy(token => token.Token, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<TokenPosting>)group.ToArray(), StringComparer.Ordinal);
        referencesBySymbolId = snapshot.References
            .GroupBy(reference => reference.SymbolId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<ReferenceEntry>)group.ToArray(), StringComparer.Ordinal);
        documentsById = snapshot.Documents
            .GroupBy(document => document.DocumentId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        documentsByPath = snapshot.Documents
            .GroupBy(document => document.RelativePath, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
    }

    public IReadOnlyList<SearchResult> Search(SearchRequest request)
        => SearchDetailed(request).Results;

    public SearchExecution SearchDetailed(SearchRequest request, long searchLoadMs = 0)
    {
        var scoreStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var parsed = QueryParser.Parse(request.Query);
        var query = string.Join(' ', parsed.Terms.Concat(parsed.Phrases)).Trim();
        var contextProjects = ResolveContextProjects(request);
        var relatedContextProjects = ResolveRelatedContextProjects(contextProjects);
        var results = new List<SearchResult>();
        var mode = request.Mode;

        if (mode is SearchMode.All or SearchMode.Symbol)
        {
            results.AddRange(SearchSymbols(query, request));
            if (IsTimedOut(request, scoreStopwatch))
            {
                return Finish(results, request, parsed, contextProjects, relatedContextProjects, searchLoadMs, scoreStopwatch, timedOut: true);
            }
        }

        if (mode is SearchMode.All or SearchMode.Text)
        {
            results.AddRange(SearchText(parsed, request));
            if (IsTimedOut(request, scoreStopwatch))
            {
                return Finish(results, request, parsed, contextProjects, relatedContextProjects, searchLoadMs, scoreStopwatch, timedOut: true);
            }
        }

        if (mode is SearchMode.All or SearchMode.File)
        {
            results.AddRange(SearchFiles(query, request));
            if (IsTimedOut(request, scoreStopwatch))
            {
                return Finish(results, request, parsed, contextProjects, relatedContextProjects, searchLoadMs, scoreStopwatch, timedOut: true);
            }
        }

        if (mode is SearchMode.All or SearchMode.Reference)
        {
            results.AddRange(SearchReferences(query, request));
        }

        return Finish(results, request, parsed, contextProjects, relatedContextProjects, searchLoadMs, scoreStopwatch, IsTimedOut(request, scoreStopwatch));
    }

    private SearchExecution Finish(
        List<SearchResult> results,
        SearchRequest request,
        ParsedQuery parsed,
        HashSet<string> contextProjects,
        HashSet<string> relatedContextProjects,
        long searchLoadMs,
        System.Diagnostics.Stopwatch scoreStopwatch,
        bool timedOut)
    {
        var topResults = results
            .Where(result => PassesPath(result.Path, request.Path))
            .GroupBy(r => $"{r.Path}|{r.Line}|{r.Column}|{r.Kind}|{r.SymbolId}", StringComparer.Ordinal)
            .Select(g => g.OrderByDescending(r => r.Score).First())
            .GroupBy(r => $"{r.Path}|{r.Line}|{r.Column}", StringComparer.Ordinal)
            .Select(g => ApplyResultAdjustments(g.OrderByDescending(r => r.Score).First(), request, parsed, contextProjects, relatedContextProjects))
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Path, StringComparer.Ordinal)
            .ThenBy(r => r.Line)
            .ThenBy(r => r.Column)
            .Take(request.Limit)
            .ToArray();

        scoreStopwatch.Stop();
        return new SearchExecution(topResults.Select(HydrateSnippet).ToArray(), timedOut, searchLoadMs, scoreStopwatch.ElapsedMilliseconds);
    }

    private static bool IsTimedOut(SearchRequest request, System.Diagnostics.Stopwatch stopwatch)
        => request.TimeoutMs is { } timeoutMs && stopwatch.ElapsedMilliseconds >= Math.Max(0, timeoutMs);

    private HashSet<string> ResolveContextProjects(SearchRequest request)
    {
        var projects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(request.FromProject))
        {
            foreach (var document in snapshot.Documents.Where(d => d.ProjectName?.Contains(request.FromProject, StringComparison.OrdinalIgnoreCase) == true))
            {
                projects.Add(document.ProjectName!);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.FromFile))
        {
            var fromFile = RepositoryDiscovery.NormalizeRelative(request.FromFile);
            foreach (var document in snapshot.Documents.Where(d => string.Equals(d.RelativePath, fromFile, StringComparison.OrdinalIgnoreCase) && d.ProjectName is not null))
            {
                projects.Add(document.ProjectName!);
            }
        }

        return projects;
    }

    private HashSet<string> ResolveRelatedContextProjects(HashSet<string> contextProjects)
    {
        var related = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (contextProjects.Count == 0)
        {
            return related;
        }

        foreach (var reference in snapshot.References)
        {
            if (!symbolsById.TryGetValue(reference.SymbolId, out var symbol))
            {
                continue;
            }

            if (reference.ProjectName is not null
                && contextProjects.Contains(reference.ProjectName)
                && symbol.ProjectName is not null
                && !contextProjects.Contains(symbol.ProjectName))
            {
                related.Add(symbol.ProjectName);
            }

            if (symbol.ProjectName is not null
                && contextProjects.Contains(symbol.ProjectName)
                && reference.ProjectName is not null
                && !contextProjects.Contains(reference.ProjectName))
            {
                related.Add(reference.ProjectName);
            }
        }

        return related;
    }

    private SearchResult ApplyResultAdjustments(SearchResult result, SearchRequest request, ParsedQuery parsed, HashSet<string> contextProjects, HashSet<string> relatedContextProjects)
    {
        var adjusted = result;
        if (result.ProjectName is not null && contextProjects.Contains(result.ProjectName))
        {
            adjusted = adjusted with
            {
                Score = adjusted.Score + 120,
                MatchReason = AppendReason(adjusted.MatchReason, "context-boost")
            };
        }
        else if (result.ProjectName is not null && relatedContextProjects.Contains(result.ProjectName))
        {
            adjusted = adjusted with
            {
                Score = adjusted.Score + 60,
                MatchReason = AppendReason(adjusted.MatchReason, "related-context-boost")
            };
        }

        documentsByPath.TryGetValue(result.Path, out var document);
        if (IsTestResult(result, document) && request.IncludeTests != true && !QueryExplicitlyTargetsTests(parsed))
        {
            adjusted = adjusted with
            {
                Score = adjusted.Score - 80,
                MatchReason = AppendReason(adjusted.MatchReason, "test-penalty")
            };
        }

        if (document?.IsGenerated == true)
        {
            adjusted = adjusted with
            {
                Score = adjusted.Score - 100,
                MatchReason = AppendReason(adjusted.MatchReason, "generated-penalty")
            };
        }

        if (IsVendorLikeOrDeepPath(result.Path))
        {
            adjusted = adjusted with
            {
                Score = adjusted.Score - 20,
                MatchReason = AppendReason(adjusted.MatchReason, "path-penalty")
            };
        }

        return adjusted;
    }

    private SearchResult HydrateSnippet(SearchResult result)
        => string.IsNullOrEmpty(result.Snippet)
            ? result with { Snippet = readSnippet(result.Path, result.Line) }
            : result;

    private IEnumerable<SearchResult> SearchSymbols(string query, SearchRequest request)
    {
        var kinds = SplitKinds(request.Kind);
        foreach (var symbol in CandidateSymbols(query))
        {
            if (kinds.Count > 0 && !kinds.Contains(symbol.Kind))
            {
                continue;
            }

            if (!PassesProject(symbol.ProjectName, request.Project) || !PassesTests(symbol.Path, request.IncludeTests))
            {
                continue;
            }

            var match = SearchScorer.ScoreSymbolMatch(symbol.FullyQualifiedName, symbol.Name, query);
            if (match.Score <= 0)
            {
                continue;
            }

            yield return new SearchResult(
                symbol.Path,
                symbol.Line,
                symbol.Column,
                symbol.EndLine,
                symbol.EndColumn,
                symbol.Kind,
                match.Score,
                match.Reason,
                string.Empty,
                symbol.SymbolId,
                symbol.Name,
                symbol.ContainerName,
                symbol.FullyQualifiedName,
                null,
                symbol.ProjectName);
        }
    }

    private IEnumerable<SearchResult> SearchText(ParsedQuery parsed, SearchRequest request)
    {
        var terms = parsed.Terms.SelectMany(Tokenizer.NormalizeTerms).Distinct(StringComparer.Ordinal).ToArray();
        var phrases = parsed.Phrases.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
        var phraseTerms = phrases.SelectMany(Tokenizer.NormalizeTerms).Distinct(StringComparer.Ordinal).ToArray();
        if (terms.Length == 0 && phrases.Length == 0)
        {
            yield break;
        }

        var candidateTerms = terms.Length > 0 ? terms : phraseTerms;
        if (candidateTerms.Length == 0)
        {
            yield break;
        }

        var postingGroups = candidateTerms
            .Select(term => tokenToPostings.TryGetValue(term, out var postings)
                ? postings
                : Array.Empty<TokenPosting>())
            .ToArray();
        var nonEmptyPostingGroups = postingGroups.Where(group => group.Count > 0).ToArray();
        if (nonEmptyPostingGroups.Length == 0)
        {
            if (terms.Length > 0)
            {
                yield break;
            }

            nonEmptyPostingGroups = new[] { snapshot.Tokens };
        }

        var lineSets = nonEmptyPostingGroups
            .Select(group => group.Select(LineKey).ToHashSet(StringComparer.Ordinal))
            .ToArray();
        var intersectedLineKeys = new HashSet<string>(lineSets[0], StringComparer.Ordinal);
        foreach (var lineSet in lineSets.Skip(1))
        {
            intersectedLineKeys.IntersectWith(lineSet);
        }

        var candidateLineKeys = lineSets.SelectMany(set => set).ToHashSet(StringComparer.Ordinal);

        var candidatePostings = nonEmptyPostingGroups
            .SelectMany(group => group)
            .Where(token => candidateLineKeys.Contains(LineKey(token)))
            .GroupBy(t => new { t.Path, t.Line });

        foreach (var group in candidatePostings)
        {
            if (!PassesProject(group.First().ProjectName, request.Project) || !PassesTests(group.Key.Path, request.IncludeTests))
            {
                continue;
            }

            var matchedTokens = group
                .Where(t => terms.Contains(t.Token, StringComparer.Ordinal))
                .GroupBy(t => t.Token + "|" + t.Weight, StringComparer.Ordinal)
                .Select(g => g.First())
                .ToArray();
            var snippet = phrases.Length == 0 ? string.Empty : readSnippet(group.Key.Path, group.Key.Line);
            var phraseMatches = phrases.Count(phrase => snippet.Contains(phrase, StringComparison.OrdinalIgnoreCase));

            if (matchedTokens.Length == 0 && phraseMatches == 0)
            {
                continue;
            }

            var column = group.Min(t => t.Column);
            var score = (double)(matchedTokens.Sum(t => ScoreTokenWeight(t.Weight)) + phraseMatches * 300);
            var isUnionFallback = terms.Length > 0
                                  && candidateTerms.Length > 1
                                  && !intersectedLineKeys.Contains($"{group.Key.Path}|{group.Key.Line}");
            if (isUnionFallback)
            {
                score *= 0.5;
            }

            var reason = string.Join("; ", matchedTokens
                .GroupBy(t => ReasonForTokenWeight(t.Weight), StringComparer.Ordinal)
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .OrderBy(g => g.Min(t => TokenWeightReasonOrder(t.Weight)))
                .Select(g => g.Key));
            if (phraseMatches > 0)
            {
                reason = AppendReason(reason, "phrase-match");
            }
            if (isUnionFallback)
            {
                reason = AppendReason(reason, "union-fallback");
            }

            yield return new SearchResult(group.Key.Path, group.Key.Line, column, group.Key.Line, column, "text", score, reason, string.Empty, null, null, null, null, null, group.First().ProjectName);
        }
    }

    private IEnumerable<SearchResult> SearchFiles(string query, SearchRequest request)
    {
        foreach (var document in snapshot.Documents)
        {
            if (!PassesProject(document.ProjectName, request.Project) || !PassesTests(document.RelativePath, request.IncludeTests))
            {
                continue;
            }

            var fileName = Path.GetFileName(document.RelativePath);
            if (fileName.Contains(query, StringComparison.OrdinalIgnoreCase) || document.RelativePath.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                yield return new SearchResult(document.RelativePath, 1, 1, 1, 1, "file", fileName.Equals(query, StringComparison.OrdinalIgnoreCase) ? 400 : 200, "path-match", string.Empty, null, null, null, null, null, document.ProjectName);
            }
        }
    }

    private IEnumerable<SearchResult> SearchReferences(string query, SearchRequest request)
    {
        var emitted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var symbol in CandidateSymbols(query))
        {
            if (!referencesBySymbolId.TryGetValue(symbol.SymbolId, out var references))
            {
                continue;
            }

            foreach (var reference in references)
            {
                if (!PassesProject(reference.ProjectName, request.Project) || !PassesTests(reference.Path, request.IncludeTests))
                {
                    continue;
                }

                yield return new SearchResult(
                    reference.Path,
                    reference.Line,
                    reference.Column,
                    reference.EndLine,
                    reference.EndColumn,
                    "reference",
                    300,
                    "reference-match",
                    string.Empty,
                    reference.SymbolId,
                    symbol.Name,
                    symbol.ContainerName,
                    symbol.FullyQualifiedName,
                    reference.ReferenceKind,
                    reference.ProjectName);
                emitted.Add(reference.ReferenceId);
            }
        }

        foreach (var reference in snapshot.References)
        {
            if (emitted.Contains(reference.ReferenceId)
                || !reference.ReferencedName.Contains(query, StringComparison.OrdinalIgnoreCase)
                || !PassesProject(reference.ProjectName, request.Project)
                || !PassesTests(reference.Path, request.IncludeTests))
            {
                continue;
            }

            symbolsById.TryGetValue(reference.SymbolId, out var symbol);
            yield return new SearchResult(
                reference.Path,
                reference.Line,
                reference.Column,
                reference.EndLine,
                reference.EndColumn,
                "reference",
                300,
                "reference-match",
                string.Empty,
                reference.SymbolId,
                symbol?.Name ?? reference.ReferencedName,
                symbol?.ContainerName,
                symbol?.FullyQualifiedName,
                reference.ReferenceKind,
                reference.ProjectName);
        }
    }

    private IEnumerable<SymbolEntry> CandidateSymbols(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<SymbolEntry>();
        }

        var lowerQuery = query.ToLowerInvariant();
        var candidates = new Dictionary<string, SymbolEntry>(StringComparer.Ordinal);
        if (symbolsByLowerName.TryGetValue(lowerQuery, out var byName))
        {
            foreach (var symbol in byName)
            {
                candidates.TryAdd(symbol.SymbolId, symbol);
            }
        }

        if (symbolsByLowerFullyQualifiedName.TryGetValue(lowerQuery, out var byFullyQualifiedName))
        {
            foreach (var symbol in byFullyQualifiedName)
            {
                candidates.TryAdd(symbol.SymbolId, symbol);
            }
        }

        IEnumerable<SymbolEntry> source = candidates.Count > 0 ? candidates.Values : snapshot.Symbols;
        return source
            .Select(symbol => new { Symbol = symbol, Match = SearchScorer.ScoreSymbolMatch(symbol.FullyQualifiedName, symbol.Name, query) })
            .Where(candidate => candidate.Match.Score > 0)
            .Select(candidate => candidate.Symbol)
            .ToArray();
    }

    private static HashSet<string> SplitKinds(string? kinds)
        => string.IsNullOrWhiteSpace(kinds)
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : kinds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static bool PassesPath(string path, string? filter)
        => string.IsNullOrWhiteSpace(filter) || path.Contains(filter, StringComparison.OrdinalIgnoreCase);

    private static bool PassesProject(string? projectName, string? filter)
        => string.IsNullOrWhiteSpace(filter) || (projectName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false);

    private static bool PassesTests(string path, bool? includeTests)
    {
        if (includeTests is null or true)
        {
            return true;
        }

        return !path.Contains("test", StringComparison.OrdinalIgnoreCase);
    }

    private static int ScoreTokenWeight(string weight)
        => weight switch
        {
            "path" => 120,
            "identifier" or "symbol-name" => 100,
            "keyword" => 60,
            "string" or "comment" or "text" => 40,
            _ => 40
        };

    private static string ReasonForTokenWeight(string weight)
        => weight switch
        {
            "path" => "path-match",
            "identifier" or "symbol-name" => "identifier-match",
            "keyword" => "keyword-match",
            "string" or "comment" or "text" => "text-match",
            _ => "text-match"
        };

    private static int TokenWeightReasonOrder(string weight)
        => weight switch
        {
            "path" => 0,
            "identifier" or "symbol-name" => 1,
            "keyword" => 2,
            "string" or "comment" or "text" => 3,
            _ => 4
        };

    private static string AppendReason(string current, string reason)
        => string.IsNullOrWhiteSpace(current) ? reason : current.Contains(reason, StringComparison.Ordinal) ? current : current + "; " + reason;

    private static string LineKey(TokenPosting token) => $"{token.Path}|{token.Line}";

    private static bool QueryExplicitlyTargetsTests(ParsedQuery parsed)
        => parsed.Terms.Concat(parsed.Phrases)
            .SelectMany(Tokenizer.NormalizeTerms)
            .Any(term => term is "test" or "tests" or "spec" or "fixture");

    private static bool IsTestResult(SearchResult result, DocumentEntry? document)
        => result.Path.Contains("test", StringComparison.OrdinalIgnoreCase)
           || result.ProjectName?.Contains("test", StringComparison.OrdinalIgnoreCase) == true
           || document?.ProjectName?.Contains("test", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsVendorLikeOrDeepPath(string path)
    {
        var normalized = RepositoryDiscovery.NormalizeRelative(path);
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment.Equals("vendor", StringComparison.OrdinalIgnoreCase)
                                       || segment.Equals("node_modules", StringComparison.OrdinalIgnoreCase)
                                       || segment.Equals("packages", StringComparison.OrdinalIgnoreCase))
               || segments.Length >= 6;
    }
}

public sealed class SuggestionService
{
    private readonly IndexSnapshot snapshot;

    public SuggestionService(IndexSnapshot snapshot) => this.snapshot = snapshot;

    public IReadOnlyList<QuerySuggestion> Suggest(string question, int limit)
    {
        var parsed = ParseNaturalQuestion(question);
        var normalized = NormalizeForIntent(question);
        var primary = PickPrimaryTerm(parsed.CodeLikeIdentifiers.Concat(parsed.Terms).ToArray())
                      ?? parsed.CodeLikeIdentifiers.FirstOrDefault()
                      ?? parsed.Terms.FirstOrDefault()
                      ?? parsed.Phrases.FirstOrDefault()
                      ?? question.Trim();
        if (string.IsNullOrWhiteSpace(primary))
        {
            primary = question.Trim();
        }

        var suggestions = new List<QuerySuggestion>();
        var expanded = BuildExpandedTerms(parsed).ToArray();
        var searchQuery = BuildSearchQuery(parsed, expanded, primary);
        var hasCodeLikePrimary = parsed.CodeLikeIdentifiers.Count > 0 || snapshot.Symbols.Any(s => SymbolMatches(s, primary));
        var isReferenceIntent = ContainsAny(normalized, "cine foloseste", "where is used", " used", "used", "apelat", "refs");
        var isDefinitionIntent = ContainsAny(normalized, "unde este definit", "where is defined", "defined", "definit") && !isReferenceIntent;
        var isBroadIntent = ContainsAny(normalized, "unde se face", "how is", "how are", "done", "implemented", "handled");

        if (isDefinitionIntent)
        {
            suggestions.Add(new QuerySuggestion($"ri goto {primary}", primary, "symbol", 0.95, "definition intent", "definition"));
        }

        if (isReferenceIntent)
        {
            suggestions.Add(new QuerySuggestion($"ri refs {primary}", primary, "reference", 0.93, "reference intent", "reference"));
        }

        if (expanded.Any(t => t is "config" or "settings" or "options"))
        {
            suggestions.Add(new QuerySuggestion($"ri search \"{searchQuery}\" --path config", searchQuery, "all", 0.86, "configuration terms detected", "configuration"));
        }

        if (expanded.Any(t => t is "controller" or "endpoint" or "route" or "api"))
        {
            suggestions.Add(new QuerySuggestion($"ri search \"{searchQuery}\" --path Controllers", searchQuery, "all", 0.85, "endpoint terms detected", "endpoint"));
        }

        if (expanded.Any(t => t is "test" or "spec" or "fixture"))
        {
            suggestions.Add(new QuerySuggestion($"ri search \"{searchQuery}\" --include-tests", searchQuery, "all", 0.96, "test terms detected", "test"));
        }

        if (expanded.Any(t => t is "validate" or "validation" or "validator"))
        {
            suggestions.Add(new QuerySuggestion($"ri search \"{searchQuery}\" --path Validators", searchQuery, "all", 0.87, "validation terms detected", "validation"));
        }

        if (expanded.Any(t => t is "serialize" or "json" or "deserialize"))
        {
            suggestions.Add(new QuerySuggestion($"ri search \"{searchQuery}\" --mode text", searchQuery, "text", 0.87, "serialization terms detected", "serialization"));
        }

        if (expanded.Any(t => t is "save" or "persist" or "store" or "insert" or "update" or "repository" or "database" or "dbcontext"))
        {
            suggestions.Add(new QuerySuggestion($"ri search \"{searchQuery}\" --path Repositories", searchQuery, "all", 0.87, "persistence terms detected", "persistence"));
        }

        suggestions.Add(new QuerySuggestion($"ri search \"{searchQuery}\"", searchQuery, "all", 0.70 + Math.Min(snapshot.Symbols.Count, 10) / 100.0, isBroadIntent ? "broad implementation search" : "deterministic token search", "mixed"));

        if (hasCodeLikePrimary)
        {
            suggestions.Add(new QuerySuggestion($"ri search {primary} --mode symbol", primary, "symbol", 0.66, "code-like identifier symbol search", "definition"));
            suggestions.Add(new QuerySuggestion($"ri search {primary} --mode reference", primary, "reference", 0.65, "code-like identifier reference search", "reference"));
        }

        return suggestions
            .GroupBy(s => s.Command, StringComparer.Ordinal)
            .Select(g => g.OrderByDescending(s => s.Confidence).First())
            .OrderByDescending(s => s.Confidence)
            .ThenBy(s => s.Command, StringComparer.Ordinal)
            .Take(Math.Clamp(limit, 1, 5))
            .ToArray();
    }

    private static ParsedNaturalQuestion ParseNaturalQuestion(string question)
    {
        var parsed = QueryParser.Parse(question);
        var withoutPhrases = question;
        foreach (var phrase in parsed.Phrases)
        {
            withoutPhrases = withoutPhrases.Replace("\"" + phrase + "\"", " ", StringComparison.Ordinal);
        }

        var codeLike = ExtractCodeLikeIdentifiers(withoutPhrases)
            .Where(term => !IsStopWord(term))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var codeLikeNormalized = codeLike.SelectMany(Tokenizer.NormalizeTerms).ToHashSet(StringComparer.Ordinal);
        var terms = Tokenizer.NormalizeTerms(withoutPhrases)
            .Where(term => !IsStopWord(term))
            .Where(term => !codeLikeNormalized.Contains(term))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new ParsedNaturalQuestion(parsed.Phrases.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.Ordinal).ToArray(), codeLike, terms);
    }

    private static IEnumerable<string> ExtractCodeLikeIdentifiers(string question)
    {
        foreach (var part in question.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var value = part.Trim().Trim('?', '!', ',', ';', ':', '"', '\'', '(', ')', '[', ']', '{', '}');
            if (value.Length == 0)
            {
                continue;
            }

            if (IsCodeLikeIdentifier(value))
            {
                yield return value;
            }
        }
    }

    private static bool IsCodeLikeIdentifier(string value)
        => value.Contains('.', StringComparison.Ordinal)
           || value.Contains('_', StringComparison.Ordinal)
           || value.Contains('-', StringComparison.Ordinal)
           || value.Any(char.IsUpper)
           || value.Any(char.IsDigit);

    private static IEnumerable<string> BuildExpandedTerms(ParsedNaturalQuestion parsed)
        => ExpandSynonyms(parsed.CodeLikeIdentifiers.SelectMany(Tokenizer.NormalizeTerms).Concat(parsed.Terms).Concat(parsed.Phrases.SelectMany(Tokenizer.NormalizeTerms)))
            .Where(term => !IsStopWord(term))
            .Distinct(StringComparer.Ordinal);

    private static string BuildSearchQuery(ParsedNaturalQuestion parsed, IReadOnlyList<string> expanded, string primary)
    {
        var parts = new List<string>();
        parts.AddRange(parsed.Phrases);
        parts.AddRange(parsed.CodeLikeIdentifiers);
        parts.AddRange(expanded.Where(term => !parts.Any(existing => !IsAllCapsIdentifier(existing) && Tokenizer.NormalizeTerms(existing).Contains(term, StringComparer.Ordinal))).Take(12));
        return parts.Count == 0 ? primary : string.Join(' ', parts.Take(12));
    }

    private static bool IsAllCapsIdentifier(string value)
        => value.Any(char.IsLetter) && value.Where(char.IsLetter).All(char.IsUpper);

    private string? PickPrimaryTerm(IReadOnlyList<string> terms)
    {
        foreach (var term in terms.OrderByDescending(IsCodeLikeIdentifier).ThenByDescending(t => t.Length))
        {
            if (snapshot.Symbols.Any(s => SymbolMatches(s, term)))
            {
                return snapshot.Symbols.First(s => SymbolMatches(s, term)).Name;
            }
        }

        return terms.FirstOrDefault();
    }

    private static bool SymbolMatches(SymbolEntry symbol, string term)
        => string.Equals(symbol.Name, term, StringComparison.OrdinalIgnoreCase)
           || string.Equals(symbol.FullyQualifiedName, term, StringComparison.OrdinalIgnoreCase)
           || symbol.FullyQualifiedName.EndsWith("." + term, StringComparison.OrdinalIgnoreCase)
           || term.EndsWith("." + symbol.Name, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> ExpandSynonyms(IEnumerable<string> terms)
    {
        var groups = new[]
        {
            new[] { "login", "auth", "authentication", "authorize", "jwt", "token" },
            new[] { "config", "settings", "options" },
            new[] { "db", "database", "repository", "context", "dbcontext" },
            new[] { "endpoint", "controller", "route", "api" },
            new[] { "validate", "validation", "validator", "valideaza", "validează", "validare", "validarea", "validat" },
            new[] { "serialize", "json", "deserialize" },
            new[] { "save", "saved", "persist", "persistence", "store", "insert", "update" }
        };

        foreach (var term in terms)
        {
            yield return term;
            foreach (var group in groups.Where(g => g.Contains(term, StringComparer.OrdinalIgnoreCase)))
            {
                foreach (var item in group)
                {
                    yield return item;
                }
            }
        }
    }

    private static bool ContainsAny(string value, params string[] parts)
        => parts.Any(part => value.Contains(part, StringComparison.OrdinalIgnoreCase));

    private static string NormalizeForIntent(string value)
        => RemoveDiacritics(value).ToLowerInvariant();

    private static string RemoveDiacritics(string value)
        => value.Replace('ă', 'a').Replace('â', 'a').Replace('î', 'i').Replace('ș', 's').Replace('ş', 's').Replace('ț', 't').Replace('ţ', 't');

    private static bool IsStopWord(string value)
        => StopWords.Contains(NormalizeForIntent(value));

    private sealed record ParsedNaturalQuestion(
        IReadOnlyList<string> Phrases,
        IReadOnlyList<string> CodeLikeIdentifiers,
        IReadOnlyList<string> Terms);

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "unde", "care", "cum", "cine", "este", "sunt", "se", "face", "gaseste", "tokenul",
        "find", "where", "how", "what", "who", "is", "are", "the", "a", "an", "to", "of", "e"
    };
}
