using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using RoslynRepoIndexer.Core;

namespace RoslynRepoIndexer.Tests;

public sealed class CoreBehaviorTests
{
    [Fact]
    public void RepositoryDiscovery_detects_git_root_and_excludes_default_directories()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        Directory.CreateDirectory(Path.Combine(repo.Root, "src"));
        Directory.CreateDirectory(Path.Combine(repo.Root, "bin"));
        File.WriteAllText(Path.Combine(repo.Root, "src", "App.cs"), "class App {}");
        File.WriteAllText(Path.Combine(repo.Root, "bin", "Ignored.cs"), "class Ignored {}");

        var root = RepositoryDiscovery.FindRoot(Path.Combine(repo.Root, "src"));
        var files = RepositoryDiscovery.EnumerateCandidateFiles(root.RootPath, IndexerConfig.Default).ToArray();

        Assert.Equal(repo.Root, root.RootPath);
        Assert.Contains(files, f => f.RelativePath == "src/App.cs");
        Assert.DoesNotContain(files, f => f.RelativePath.Contains("bin", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RepositoryDiscovery_detects_solution_root_without_git()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, "child"));
        File.WriteAllText(Path.Combine(repo.Root, "Repo.sln"), string.Empty);

        var root = RepositoryDiscovery.FindRoot(Path.Combine(repo.Root, "child"));

        Assert.Equal(repo.Root, root.RootPath);
        Assert.Equal(RepositoryRootKind.WorkspaceFile, root.Kind);
    }

    [Fact]
    public void ConfigLoader_uses_defaults_and_warns_for_invalid_values()
    {
        using var repo = TestRepo.Create();
        File.WriteAllText(Path.Combine(repo.Root, ".roslyn-index.json"), """
            {
              "maxTextFileBytes": -1,
              "unknownSetting": true,
              "excludeDirectories": [".git", "bin"]
            }
            """);

        var load = ConfigLoader.Load(repo.Root, null);

        Assert.Equal(IndexerConfig.Default.MaxTextFileBytes, load.Config.MaxTextFileBytes);
        Assert.Contains(load.Warnings, w => w.Contains("unknownSetting", StringComparison.Ordinal));
        Assert.Contains(load.Warnings, w => w.Contains("maxTextFileBytes", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("customerService", "customer", "service", "customerservice")]
    [InlineData("CustomerService", "customer", "service", "customerservice")]
    [InlineData("customer_service", "customer", "service", "customer_service")]
    [InlineData("customer-service", "customer", "service", "customer-service")]
    [InlineData("IHttpClientFactory", "ihttp", "client", "factory", "ihttpclientfactory")]
    public void Tokenizer_keeps_whole_tokens_and_identifier_subtokens(string input, params string[] expected)
    {
        var tokens = Tokenizer.Tokenize(input).Select(t => t.Value).ToHashSet(StringComparer.Ordinal);

        foreach (var token in expected)
        {
            Assert.Contains(token, tokens);
        }
    }

    [Fact]
    public void BinaryFileDetector_detects_nul_bytes()
    {
        Assert.True(BinaryFileDetector.IsBinary(new byte[] { 65, 0, 66 }));
        Assert.False(BinaryFileDetector.IsBinary("hello"u8.ToArray()));
    }

    [Fact]
    public void QueryParser_supports_phrases_and_filters()
    {
        var query = QueryParser.Parse("""kind:method path:Services "Customer Service" Customer""");

        Assert.Equal("method", query.Filters["kind"]);
        Assert.Equal("Services", query.Filters["path"]);
        Assert.Contains("Customer Service", query.Phrases);
        Assert.Contains("Customer", query.Terms);
    }

    [Fact]
    public void SearchScorer_prefers_fqn_then_simple_exact_then_prefix_then_contains()
    {
        var fqn = SearchScorer.ScoreSymbol("My.App.CustomerService", "CustomerService", "My.App.CustomerService");
        var simple = SearchScorer.ScoreSymbol("My.App.CustomerService", "CustomerService", "CustomerService");
        var prefix = SearchScorer.ScoreSymbol("My.App.CustomerService", "CustomerService", "Customer");
        var contains = SearchScorer.ScoreSymbol("My.App.CustomerService", "CustomerService", "Service");

        Assert.True(fqn > simple);
        Assert.True(simple > prefix);
        Assert.True(prefix > contains);
    }

    [Fact]
    public void SearchService_returns_stably_sorted_results_with_match_reason()
    {
        var snapshot = new IndexSnapshot(
            IndexManifest.CreateNew("C:/repo", "cfg", "workspace"),
            new[]
            {
                Doc("d1", "src/B.cs", "P"),
                Doc("d2", "src/A.cs", "P")
            },
            new[]
            {
                Sym("s1", "class", "CustomerService", "My.App.CustomerService", "src/B.cs", "P"),
                Sym("s2", "class", "CustomerService", "My.App.CustomerService", "src/A.cs", "P")
            },
            Array.Empty<ReferenceEntry>(),
            Array.Empty<TokenPosting>());

        var results = new SearchService(snapshot, new SnippetReader("C:/repo")).Search(new SearchRequest("CustomerService", SearchMode.Symbol, 10));

        Assert.Equal(new[] { "src/A.cs", "src/B.cs" }, results.Select(r => r.Path));
        Assert.All(results, r => Assert.False(string.IsNullOrWhiteSpace(r.MatchReason)));
    }

    [Fact]
    public void SearchService_boosts_results_from_context_project_or_file()
    {
        var snapshot = new IndexSnapshot(
            IndexManifest.CreateNew("C:/repo", "cfg", "workspace"),
            new[]
            {
                Doc("d1", "src/App/CustomerService.cs", "App"),
                Doc("d2", "src/Tests/CustomerService.cs", "App.Tests"),
                Doc("d3", "src/Tests/CustomerServiceTests.cs", "App.Tests")
            },
            new[]
            {
                Sym("s1", "class", "CustomerService", "App.CustomerService", "src/App/CustomerService.cs", "App"),
                Sym("s2", "class", "CustomerService", "App.Tests.CustomerService", "src/Tests/CustomerService.cs", "App.Tests")
            },
            Array.Empty<ReferenceEntry>(),
            Array.Empty<TokenPosting>());

        var service = new SearchService(snapshot, new SnippetReader("C:/repo"));

        var fromProject = service.Search(new SearchRequest("CustomerService", SearchMode.Symbol, 10, FromProject: "App.Tests"));
        var fromFile = service.Search(new SearchRequest("CustomerService", SearchMode.Symbol, 10, FromFile: "src/Tests/CustomerServiceTests.cs"));

        Assert.Equal("src/Tests/CustomerService.cs", fromProject[0].Path);
        Assert.Contains("context-boost", fromProject[0].MatchReason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("src/Tests/CustomerService.cs", fromFile[0].Path);
        Assert.Contains("context-boost", fromFile[0].MatchReason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("unde este definit CustomerService?", "ri goto")]
    [InlineData("cine foloseste GetCustomerAsync?", "ri refs")]
    [InlineData("unde se valideaza tokenul JWT?", "ri search")]
    public void SuggestionService_maps_simple_intents_deterministically(string question, string expectedCommand)
    {
        var snapshot = new IndexSnapshot(
            IndexManifest.CreateNew("C:/repo", "cfg", "workspace"),
            Array.Empty<DocumentEntry>(),
            new[] { Sym("s1", "class", "CustomerService", "My.App.CustomerService", "src/CustomerService.cs", "P") },
            Array.Empty<ReferenceEntry>(),
            Array.Empty<TokenPosting>());

        var suggestions = new SuggestionService(snapshot).Suggest(question, 5);

        Assert.StartsWith(expectedCommand, suggestions[0].Command, StringComparison.Ordinal);
        Assert.Equal(suggestions.OrderByDescending(s => s.Confidence).ThenBy(s => s.Command, StringComparer.Ordinal).Select(s => s.Command), suggestions.Select(s => s.Command));
    }

    [Fact]
    public void SuggestionService_prefers_reference_intent_when_used_is_explicit()
    {
        var snapshot = new IndexSnapshot(
            IndexManifest.CreateNew("C:/repo", "cfg", "workspace"),
            Array.Empty<DocumentEntry>(),
            new[] { Sym("s1", "method", "GetCustomerAsync", "My.App.CustomerService.GetCustomerAsync(string)", "src/CustomerService.cs", "P") },
            Array.Empty<ReferenceEntry>(),
            Array.Empty<TokenPosting>());

        var suggestions = new SuggestionService(snapshot).Suggest("where is GetCustomerAsync used?", 5);

        Assert.StartsWith("ri refs", suggestions[0].Command, StringComparison.Ordinal);
        Assert.Equal("GetCustomerAsync", suggestions[0].Query);
    }

    [Fact]
    public void SuggestionService_expands_synonyms_for_broad_code_search()
    {
        var snapshot = new IndexSnapshot(
            IndexManifest.CreateNew("C:/repo", "cfg", "workspace"),
            Array.Empty<DocumentEntry>(),
            Array.Empty<SymbolEntry>(),
            Array.Empty<ReferenceEntry>(),
            Array.Empty<TokenPosting>());

        var suggestions = new SuggestionService(snapshot).Suggest("unde se valideaza tokenul JWT?", 5);
        var search = suggestions.First(s => s.Command.StartsWith("ri search", StringComparison.Ordinal));

        Assert.Contains("auth", search.Query, StringComparison.Ordinal);
        Assert.Contains("jwt", search.Query, StringComparison.Ordinal);
        Assert.Contains("validator", search.Query, StringComparison.Ordinal);
    }

    [Fact]
    public void SuggestionService_preserves_quoted_phrases_as_priority_terms_and_code_like_identifiers()
    {
        var snapshot = new IndexSnapshot(
            IndexManifest.CreateNew("C:/repo", "cfg", "workspace"),
            Array.Empty<DocumentEntry>(),
            new[] { Sym("s1", "method", "GetCustomerAsync", "My.App.CustomerService.GetCustomerAsync(string)", "src/CustomerService.cs", "P") },
            Array.Empty<ReferenceEntry>(),
            Array.Empty<TokenPosting>());

        var suggestions = new SuggestionService(snapshot).Suggest("""where is "Customer Service" handled by My.App.CustomerService.GetCustomerAsync and order_item-id?""", 5);
        var search = suggestions.First(s => s.Command.StartsWith("ri search", StringComparison.Ordinal));

        Assert.StartsWith("Customer Service", search.Query, StringComparison.Ordinal);
        Assert.Contains("My.App.CustomerService.GetCustomerAsync", search.Query, StringComparison.Ordinal);
        Assert.Contains("order_item-id", search.Query, StringComparison.Ordinal);
    }

    [Fact]
    public void SuggestionService_generates_three_to_five_concrete_suggestions()
    {
        var snapshot = new IndexSnapshot(
            IndexManifest.CreateNew("C:/repo", "cfg", "workspace"),
            Array.Empty<DocumentEntry>(),
            new[] { Sym("s1", "class", "CustomerService", "My.App.CustomerService", "src/CustomerService.cs", "P") },
            Array.Empty<ReferenceEntry>(),
            Array.Empty<TokenPosting>());

        var suggestions = new SuggestionService(snapshot).Suggest("how is CustomerService validation persisted?", 5);

        Assert.InRange(suggestions.Count, 3, 5);
        Assert.All(suggestions, suggestion => Assert.StartsWith("ri ", suggestion.Command, StringComparison.Ordinal));
    }

    [Fact]
    public void SuggestionService_removes_romanian_and_english_stopwords()
    {
        var snapshot = new IndexSnapshot(
            IndexManifest.CreateNew("C:/repo", "cfg", "workspace"),
            Array.Empty<DocumentEntry>(),
            new[] { Sym("s1", "class", "CustomerService", "My.App.CustomerService", "src/CustomerService.cs", "P") },
            Array.Empty<ReferenceEntry>(),
            Array.Empty<TokenPosting>());

        var suggestions = new SuggestionService(snapshot).Suggest("unde care cum cine este sunt se face găsește find where how what who is are the a an to of CustomerService", 5);
        var search = suggestions.First(s => s.Command.StartsWith("ri search", StringComparison.Ordinal));
        var queryTerms = Tokenizer.NormalizeTerms(search.Query).ToHashSet(StringComparer.Ordinal);

        foreach (var stopword in new[] { "unde", "care", "cum", "cine", "este", "sunt", "se", "face", "găsește", "find", "where", "how", "what", "who", "is", "are", "the", "a", "an", "to", "of" })
        {
            Assert.DoesNotContain(stopword, queryTerms);
        }

        Assert.Contains("customerservice", queryTerms);
    }

    [Theory]
    [InlineData("settings", new[] { "config", "settings", "options" })]
    [InlineData("DbContext", new[] { "db", "database", "repository", "context", "dbcontext" })]
    [InlineData("controller", new[] { "endpoint", "controller", "route", "api" })]
    [InlineData("json", new[] { "serialize", "json", "deserialize" })]
    [InlineData("persist", new[] { "save", "persist", "store", "insert", "update" })]
    public void SuggestionService_expands_remaining_deterministic_synonym_groups(string question, string[] expectedTerms)
    {
        var snapshot = new IndexSnapshot(
            IndexManifest.CreateNew("C:/repo", "cfg", "workspace"),
            Array.Empty<DocumentEntry>(),
            Array.Empty<SymbolEntry>(),
            Array.Empty<ReferenceEntry>(),
            Array.Empty<TokenPosting>());

        var suggestions = new SuggestionService(snapshot).Suggest(question, 5);
        var search = suggestions.First(s => s.Command.StartsWith("ri search", StringComparison.Ordinal));

        foreach (var term in expectedTerms)
        {
            Assert.Contains(term, search.Query, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("where are app options configured?", "configuration", "--path config")]
    [InlineData("which controller exposes the customer route?", "endpoint", "--path Controllers")]
    [InlineData("where is the auth fixture tested?", "test", "--include-tests")]
    [InlineData("where is customer validation implemented?", "validation", "--path Validators")]
    [InlineData("where is json serialization configured?", "serialization", "--mode text")]
    [InlineData("where is customer persistence saved?", "persistence", "--path Repositories")]
    public void SuggestionService_prioritizes_specialized_boosts_over_generic_search(string question, string expectedKind, string expectedCommandPart)
    {
        var snapshot = new IndexSnapshot(
            IndexManifest.CreateNew("C:/repo", "cfg", "workspace"),
            Array.Empty<DocumentEntry>(),
            Enumerable.Range(0, 20)
                .Select(i => Sym($"s{i}", "class", $"Type{i}", $"My.App.Type{i}", $"src/Type{i}.cs", "P"))
                .ToArray(),
            Array.Empty<ReferenceEntry>(),
            Array.Empty<TokenPosting>());

        var suggestions = new SuggestionService(snapshot).Suggest(question, 5);

        Assert.Equal(expectedKind, suggestions[0].ExpectedResultKind);
        Assert.Contains(expectedCommandPart, suggestions[0].Command, StringComparison.Ordinal);
    }

    private static DocumentEntry Doc(string id, string path, string? project)
        => new(id, project is null ? null : "pid-" + project, path, project, "C#", true, false, false, 10, DateTimeOffset.UtcNow, "h", "dh", 1);

    private static SymbolEntry Sym(string id, string kind, string name, string fqn, string path, string? project)
        => new(id, "d1", project is null ? null : "pid-" + project, kind, name, name, fqn, null, name, "public", Array.Empty<string>(), path, 1, 1, 1, 1 + name.Length, 0, name.Length, true, false, Array.Empty<string>(), null, project, null);

    [Fact]
    public void Json_output_uses_common_command_response_contract()
    {
        var json = JsonSerializer.Serialize(CommandResponse.Success(new[] { "ok" }, warnings: new[] { "warn" }));
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(0, document.RootElement.GetProperty("exitCode").GetInt32());
        Assert.Equal("warn", document.RootElement.GetProperty("warnings")[0].GetString());
        Assert.Equal("ok", document.RootElement.GetProperty("data")[0].GetString());
    }

    [Fact]
    public void IndexStore_reads_backup_version_when_swap_was_interrupted()
    {
        using var repo = TestRepo.Create();
        var snapshot = new IndexSnapshot(
            IndexManifest.CreateNew(repo.Root, "cfg", "workspace"),
            new[] { Doc("d1", "A.cs", "App") },
            Array.Empty<SymbolEntry>(),
            Array.Empty<ReferenceEntry>(),
            Array.Empty<TokenPosting>());
        IndexStore.Write(repo.Root, snapshot);
        var backup = Path.Combine(IndexStore.GetIndexDirectory(repo.Root), "old-test");
        Directory.Move(IndexStore.GetVersionDirectory(repo.Root), backup);

        var status = IndexStore.GetStatus(repo.Root);
        var read = IndexStore.Read(repo.Root);

        Assert.Equal(IndexStatus.Valid, status.Status);
        Assert.Single(read.Documents);
    }

    [Fact]
    public void New_projects_do_not_reference_forbidden_ai_http_or_search_packages()
    {
        var root = TestPaths.RepositoryRoot;
        var projectFiles = Directory.EnumerateFiles(Path.Combine(root, "tools", "RoslynRepoIndexer"), "*.csproj", SearchOption.AllDirectories);
        var forbidden = new[] { "OpenAI", "SemanticKernel", "MLNet", "Pinecone", "Qdrant", "Weaviate", "Elasticsearch", "Lucene", "HttpClientFactory" };

        foreach (var projectFile in projectFiles)
        {
            var text = File.ReadAllText(projectFile);
            foreach (var term in forbidden)
            {
                Assert.DoesNotContain(term, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Roslyn_workspace_packages_align_with_existing_repo_roslyn_version()
    {
        var root = TestPaths.RepositoryRoot;
        var repoRoslynVersion = Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Tools{Path.DirectorySeparatorChar}RoslynRepoIndexer{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .SelectMany(ReadPackageReferences)
            .Where(package => package.Include.Equals("Microsoft.CodeAnalysis.CSharp", StringComparison.Ordinal))
            .Select(package => package.Version)
            .Distinct(StringComparer.Ordinal)
            .Single();

        var indexerPackages = ReadPackageReferences(Path.Combine(root, "tools", "RoslynRepoIndexer", "src", "RoslynRepoIndexer.Core", "RoslynRepoIndexer.Core.csproj"))
            .Where(package => package.Include is "Microsoft.CodeAnalysis.CSharp.Workspaces" or "Microsoft.CodeAnalysis.Workspaces.MSBuild")
            .ToArray();

        Assert.NotEmpty(indexerPackages);
        Assert.All(indexerPackages, package => Assert.Equal(repoRoslynVersion, package.Version));
    }

    private static IEnumerable<(string Include, string Version)> ReadPackageReferences(string projectFile)
    {
        var document = XDocument.Load(projectFile);
        return document
            .Descendants("PackageReference")
            .Select(element => (
                Include: element.Attribute("Include")?.Value ?? string.Empty,
                Version: element.Attribute("Version")?.Value ?? string.Empty))
            .Where(package => !string.IsNullOrWhiteSpace(package.Include));
    }
}

internal sealed class TestRepo : IDisposable
{
    private TestRepo(string root) => Root = root;

    public string Root { get; }

    public static TestRepo Create()
    {
        var root = Path.Combine(Path.GetTempPath(), "ri-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return new TestRepo(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, recursive: true);
        }
    }
}

internal static class TestPaths
{
    public static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.csproj")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
        }
    }
}
