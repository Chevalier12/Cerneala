using RoslynRepoIndexer.Core;

namespace RoslynRepoIndexer.Tests;

public sealed class SearchScorerTests
{
    [Theory]
    [InlineData("My.App.CustomerService", "CustomerService", "My.App.CustomerService", 1000, "exact-fqn")]
    [InlineData("My.App.CustomerService", "CustomerService", "CustomerService", 800, "exact-symbol")]
    [InlineData("My.App.CustomerService", "CustomerService", "Customer", 600, "prefix-symbol")]
    [InlineData("My.App.CustomerService", "CustomerService", "CS", 500, "acronym-symbol")]
    [InlineData("My.App.CustomerService", "CustomerService", "Service", 350, "contains-symbol")]
    public void ScoreSymbolMatch_returns_plan_scores_and_short_reasons(
        string fullyQualifiedName,
        string name,
        string query,
        double expectedScore,
        string expectedReason)
    {
        var match = SearchScorer.ScoreSymbolMatch(fullyQualifiedName, name, query);

        Assert.Equal(expectedScore, match.Score);
        Assert.Equal(expectedReason, match.Reason);
    }

    [Fact]
    public void ScoreSymbolMatch_uses_token_overlap_reason_for_symbol_tokens()
    {
        var match = SearchScorer.ScoreSymbolMatch("My.App.CustomerOrderService", "CustomerOrderService", "customer order");

        Assert.True(match.Score >= 500);
        Assert.Equal("token-overlap", match.Reason);
    }

    [Fact]
    public void SearchService_uses_short_symbol_match_reasons_from_scorer()
    {
        var snapshot = new IndexSnapshot(
            IndexManifest.CreateNew("C:/repo", "cfg", "workspace"),
            new[] { Doc("d1", "src/CustomerService.cs", "P") },
            new[] { Sym("s1", "class", "CustomerService", "My.App.CustomerService", "src/CustomerService.cs", "P") },
            Array.Empty<ReferenceEntry>(),
            Array.Empty<TokenPosting>());

        var results = new SearchService(snapshot, new SnippetReader("C:/repo"))
            .Search(new SearchRequest("CustomerService", SearchMode.Symbol, 10));

        Assert.Single(results);
        Assert.Equal("exact-symbol", results[0].MatchReason);
    }

    [Fact]
    public void SearchService_uses_short_match_reasons_for_file_text_reference_and_context()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, "src"));
        File.WriteAllText(Path.Combine(repo.Root, "src", "CustomerService.cs"), "public class CustomerService { }");

        var snapshot = new IndexSnapshot(
            IndexManifest.CreateNew(repo.Root, "cfg", "workspace"),
            new[] { Doc("d1", "src/CustomerService.cs", "P") },
            new[] { Sym("s1", "class", "CustomerService", "My.App.CustomerService", "src/CustomerService.cs", "P") },
            new[] { new ReferenceEntry("r1", "s1", "d1", "pid-P", "CustomerService", "src/CustomerService.cs", 1, 14, 1, 29, 13, 15, "P", "read") },
            new[] { new TokenPosting("customer", "src/CustomerService.cs", 1, 14, "text", "text", "P", "d1") });
        var service = new SearchService(snapshot, new SnippetReader(repo.Root));

        var file = service.Search(new SearchRequest("CustomerService.cs", SearchMode.File, 10)).Single();
        var phrase = service.Search(new SearchRequest("\"public class\"", SearchMode.Text, 10)).Single();
        var reference = service.Search(new SearchRequest("CustomerService", SearchMode.Reference, 10)).Single();
        var context = service.Search(new SearchRequest("CustomerService", SearchMode.Symbol, 10, FromProject: "P")).Single();

        Assert.Equal("path-match", file.MatchReason);
        Assert.Equal("phrase-match", phrase.MatchReason);
        Assert.Equal("reference-match", reference.MatchReason);
        Assert.Equal("exact-symbol; context-boost", context.MatchReason);
    }

    [Fact]
    public void SearchService_scores_token_weights_phrase_and_context_boost_with_short_reasons()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, "src", "Features"));
        File.WriteAllText(
            Path.Combine(repo.Root, "src", "Features", "CustomerService.cs"),
            "public class CustomerService { public string Name => \"customer\"; }");

        var snapshot = new IndexSnapshot(
            IndexManifest.CreateNew(repo.Root, "cfg", "workspace"),
            new[] { Doc("d1", "src/Features/CustomerService.cs", "App") },
            Array.Empty<SymbolEntry>(),
            Array.Empty<ReferenceEntry>(),
            new[]
            {
                Tok("customer", "src/Features/CustomerService.cs", 1, 14, "path", "App", "d1"),
                Tok("customer", "src/Features/CustomerService.cs", 1, 14, "identifier", "App", "d1"),
                Tok("public", "src/Features/CustomerService.cs", 1, 1, "keyword", "App", "d1"),
                Tok("customer", "src/Features/CustomerService.cs", 1, 60, "string", "App", "d1")
            });

        var service = new SearchService(snapshot, new SnippetReader(repo.Root));

        var tokenResult = service.Search(new SearchRequest("customer public", SearchMode.Text, 10)).Single();
        var phraseResult = service.Search(new SearchRequest("\"public class\"", SearchMode.Text, 10)).Single();
        var contextResult = service.Search(new SearchRequest("customer", SearchMode.Text, 10, FromProject: "App")).Single();

        Assert.Equal(320, tokenResult.Score);
        Assert.Equal("path-match; identifier-match; keyword-match; text-match", tokenResult.MatchReason);
        Assert.Equal(300, phraseResult.Score);
        Assert.Equal("phrase-match", phraseResult.MatchReason);
        Assert.Equal(380, contextResult.Score);
        Assert.Equal("path-match; identifier-match; text-match; context-boost", contextResult.MatchReason);
    }

    [Fact]
    public void SearchService_intersects_text_postings_and_marks_lower_scored_union_fallback()
    {
        var snapshot = new IndexSnapshot(
            IndexManifest.CreateNew("C:/repo", "cfg", "workspace"),
            new[]
            {
                Doc("d1", "src/Both.cs", "App"),
                Doc("d2", "src/One.cs", "App")
            },
            Array.Empty<SymbolEntry>(),
            Array.Empty<ReferenceEntry>(),
            new[]
            {
                Tok("customer", "src/Both.cs", 1, 1, "identifier", "App", "d1"),
                Tok("service", "src/Both.cs", 1, 10, "identifier", "App", "d1"),
                Tok("customer", "src/One.cs", 1, 1, "identifier", "App", "d2")
            });

        var results = new SearchService(snapshot, new SnippetReader("C:/repo"))
            .Search(new SearchRequest("customer service", SearchMode.Text, 10));

        Assert.Equal(new[] { "src/Both.cs", "src/One.cs" }, results.Select(r => r.Path).ToArray());
        Assert.Equal(200, results[0].Score);
        Assert.Equal(50, results[1].Score);
        Assert.Equal("identifier-match; union-fallback", results[1].MatchReason);
    }

    [Fact]
    public void SearchService_reference_search_finds_symbols_by_fqn_then_reads_references_by_symbol_id()
    {
        var snapshot = new IndexSnapshot(
            IndexManifest.CreateNew("C:/repo", "cfg", "workspace"),
            new[] { Doc("d1", "src/CustomerService.cs", "App"), Doc("d2", "src/Usage.cs", "App") },
            new[] { Sym("s1", "class", "CustomerService", "My.App.CustomerService", "src/CustomerService.cs", "App") },
            new[] { new ReferenceEntry("r1", "s1", "d2", "pid-App", "ServiceAlias", "src/Usage.cs", 3, 20, 3, 32, 19, 12, "App", "read") },
            Array.Empty<TokenPosting>());

        var results = new SearchService(snapshot, new SnippetReader("C:/repo"))
            .Search(new SearchRequest("My.App.CustomerService", SearchMode.Reference, 10));

        var result = Assert.Single(results);
        Assert.Equal("src/Usage.cs", result.Path);
        Assert.Equal("CustomerService", result.SymbolName);
        Assert.Equal("My.App.CustomerService", result.FullyQualifiedName);
    }

    [Fact]
    public void SearchService_boosts_directly_referenced_or_referencing_context_projects()
    {
        var snapshot = new IndexSnapshot(
            IndexManifest.CreateNew("C:/repo", "cfg", "workspace"),
            new[]
            {
                Doc("app-doc", "src/App/Usage.cs", "App"),
                Doc("core-doc", "src/Core/DomainThing.cs", "Core"),
                Doc("other-doc", "src/Other/DomainThing.cs", "Other")
            },
            new[]
            {
                new SymbolEntry("core-symbol", "core-doc", "pid-Core", "class", "DomainThing", "DomainThing", "Core.DomainThing", null, "DomainThing", "public", Array.Empty<string>(), "src/Core/DomainThing.cs", 1, 1, 1, 12, 0, 11, true, false, Array.Empty<string>(), null, "Core", null),
                new SymbolEntry("other-symbol", "other-doc", "pid-Other", "class", "DomainThing", "DomainThing", "Other.DomainThing", null, "DomainThing", "public", Array.Empty<string>(), "src/Other/DomainThing.cs", 1, 1, 1, 12, 0, 11, true, false, Array.Empty<string>(), null, "Other", null)
            },
            new[] { new ReferenceEntry("r1", "core-symbol", "app-doc", "pid-App", "DomainThing", "src/App/Usage.cs", 2, 14, 2, 25, 13, 11, "App", "read") },
            Array.Empty<TokenPosting>());

        var results = new SearchService(snapshot, new SnippetReader("C:/repo"))
            .Search(new SearchRequest("DomainThing", SearchMode.Symbol, 10, FromProject: "App"));

        Assert.Equal("Core", results[0].ProjectName);
        Assert.Equal(860, results[0].Score);
        Assert.Equal(800, results.Single(r => r.ProjectName == "Other").Score);
        Assert.Contains("related-context-boost", results[0].MatchReason, StringComparison.Ordinal);
    }

    [Fact]
    public void SearchService_reads_snippets_only_for_limited_top_results()
    {
        var snapshot = new IndexSnapshot(
            IndexManifest.CreateNew("C:/repo", "cfg", "workspace"),
            new[]
            {
                Doc("d1", "src/Top.cs", "App"),
                Doc("d2", "src/Bottom.cs", "App")
            },
            new[]
            {
                Sym("s1", "class", "CustomerService", "App.CustomerService", "src/Top.cs", "App"),
                Sym("s2", "class", "CustomerService", "App.CustomerService", "src/Bottom.cs", "App")
            },
            Array.Empty<ReferenceEntry>(),
            Array.Empty<TokenPosting>());
        var reads = new List<string>();

        var results = new SearchService(snapshot, (path, line) =>
            {
                reads.Add($"{path}:{line}");
                return path;
            })
            .Search(new SearchRequest("CustomerService", SearchMode.Symbol, 1));

        Assert.Single(results);
        Assert.Equal(new[] { "src/Bottom.cs:1" }, reads);
        Assert.Equal("src/Bottom.cs", results[0].Snippet);
    }

    [Fact]
    public void SearchService_applies_test_generated_and_vendor_penalties_with_short_reasons()
    {
        var snapshot = new IndexSnapshot(
            IndexManifest.CreateNew("C:/repo", "cfg", "workspace"),
            new[]
            {
                Doc("d1", "tests/CustomerServiceTests.cs", "App.Tests"),
                Doc("d2", "src/Generated/CustomerService.g.cs", "App", isGenerated: true),
                Doc("d3", "vendor/pkg/deep/path/CustomerService.cs", "Vendor")
            },
            Array.Empty<SymbolEntry>(),
            Array.Empty<ReferenceEntry>(),
            new[]
            {
                Tok("customer", "tests/CustomerServiceTests.cs", 1, 14, "identifier", "App.Tests", "d1"),
                Tok("customer", "src/Generated/CustomerService.g.cs", 1, 14, "identifier", "App", "d2"),
                Tok("customer", "vendor/pkg/deep/path/CustomerService.cs", 1, 14, "identifier", "Vendor", "d3")
            });
        var service = new SearchService(snapshot, new SnippetReader("C:/repo"));

        var results = service.Search(new SearchRequest("customer", SearchMode.Text, 10));
        var testResult = Assert.Single(results.Where(r => r.Path == "tests/CustomerServiceTests.cs"));
        var generatedResult = Assert.Single(results.Where(r => r.Path == "src/Generated/CustomerService.g.cs"));
        var vendorResult = Assert.Single(results.Where(r => r.Path == "vendor/pkg/deep/path/CustomerService.cs"));

        Assert.Equal(20, testResult.Score);
        Assert.Equal("identifier-match; test-penalty", testResult.MatchReason);
        Assert.Equal(0, generatedResult.Score);
        Assert.Equal("identifier-match; generated-penalty", generatedResult.MatchReason);
        Assert.Equal(80, vendorResult.Score);
        Assert.Equal("identifier-match; path-penalty", vendorResult.MatchReason);

        var explicitTestQuery = service.Search(new SearchRequest("customer test", SearchMode.Text, 10)).Single(r => r.Path == "tests/CustomerServiceTests.cs");
        Assert.Equal(100, explicitTestQuery.Score);
        Assert.Equal("identifier-match", explicitTestQuery.MatchReason);
    }

    private static DocumentEntry Doc(string id, string path, string? project)
        => Doc(id, path, project, isGenerated: false);

    private static DocumentEntry Doc(string id, string path, string? project, bool isGenerated)
        => new(id, project is null ? null : "pid-" + project, path, project, "C#", true, isGenerated, false, 10, DateTimeOffset.UtcNow, "h", "dh", 1);

    private static SymbolEntry Sym(string id, string kind, string name, string fqn, string path, string? project)
        => new(id, "d1", project is null ? null : "pid-" + project, kind, name, name, fqn, null, name, "public", Array.Empty<string>(), path, 1, 1, 1, 1 + name.Length, 0, name.Length, true, false, Array.Empty<string>(), null, project, null);

    private static TokenPosting Tok(string token, string path, int line, int column, string weight, string? project, string? documentId)
        => new(token, path, line, column, "text", weight, project, documentId);
}
