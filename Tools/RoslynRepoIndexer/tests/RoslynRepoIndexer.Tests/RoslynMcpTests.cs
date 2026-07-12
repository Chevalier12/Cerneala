using System.Text.Json;
using Ri.Mcp;
using RoslynRepoIndexer.Core;

namespace RoslynRepoIndexer.Tests;

public sealed class RoslynMcpTests
{
    [Fact]
    public async Task Profile_reports_generation_session_files_and_top_terms()
    {
        using var repo = TestRepo.Create();
        var document = new DocumentEntry("doc", null, "Widget.cs", null, "C#", true, false, false, 1, DateTimeOffset.UtcNow, "hash", "decl", 1);
        var token = new TokenPosting("widget", "Widget.cs", 1, 1, "text", "text", null, "doc");
        var snapshot = new IndexSnapshot(IndexManifest.CreateNew(repo.Root, "config", "workspace") with { DocumentCount = 1, TokenCount = 1 }, new[] { document }, Array.Empty<SymbolEntry>(), Array.Empty<ReferenceEntry>(), new[] { token });
        IndexStore.Write(repo.Root, snapshot);
        var tools = new RoslynMcpTools(new RepositorySessionRegistry(), new RepositoryBinding(repo.Root), new ContinuationTokenCodec());

        var result = await tools.ProfileAsync(new RoslynProfileRequest(repo.Root));

        Assert.True(result.Success);
        Assert.Equal(snapshot.Manifest.GenerationId, result.Data?.GenerationId);
        Assert.Contains(result.Data!.Files, file => file.Name == "segments.json");
        Assert.Equal("widget", result.Data.TopTerms.Single().Term);
        Assert.Equal(1, result.Data.Session.ReloadCount);
    }

    [Fact]
    public void Tool_catalog_is_deterministic_and_documents_reading_guidance()
    {
        var tools = RoslynMcpToolCatalog.Tools;

        Assert.Equal(new[]
        {
            "roslyn_doctor",
            "roslyn_index",
            "roslyn_status",
            "roslyn_search",
            "roslyn_read",
            "roslyn_pread",
            "roslyn_goto",
            "roslyn_refs",
            "roslyn_outline",
            "roslyn_inspect",
            "roslyn_context",
            "roslyn_callgraph",
            "roslyn_impact",
            "roslyn_tests_for",
            "roslyn_batch",
            "roslyn_changes",
            "roslyn_profile",
            "roslyn_suggest",
            "roslyn_capabilities"
        }, tools.Select(tool => tool.Name));
        Assert.Contains("prefer", tools.Single(tool => tool.Name == "roslyn_read").Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("before editing", tools.Single(tool => tool.Name == "roslyn_read").Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("partial", tools.Single(tool => tool.Name == "roslyn_pread").Description, StringComparison.OrdinalIgnoreCase);
        Assert.All(tools, tool => Assert.False(string.IsNullOrWhiteSpace(tool.InputSchemaJson)));
    }

    [Fact]
    public async Task Read_rejects_path_traversal_and_returns_mcp_contract()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(repo.Root)!, "outside.txt"), "outside");
        var tools = new RoslynMcpTools();

        var result = await tools.ReadAsync(new RoslynReadRequest(repo.Root, "../outside.txt"));

        Assert.False(result.Success);
        Assert.Equal("roslyn_read", result.Tool);
        Assert.Equal(repo.Root, result.RepoRoot);
        Assert.NotNull(result.Warnings);
        Assert.NotNull(result.Errors);
        Assert.Contains("outside", result.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task All_tool_results_include_required_json_compatible_metadata()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        File.WriteAllText(Path.Combine(repo.Root, "README.md"), "hello\n");
        var tools = new RoslynMcpTools();

        var results = new object[]
        {
            await tools.StatusAsync(new RoslynRepoRequest(repo.Root)),
            await tools.ReadAsync(new RoslynReadRequest(repo.Root, "README.md")),
            await tools.PReadAsync(new RoslynPReadRequest(repo.Root, "README.md", StartLine: 1, EndLine: 1)),
            await tools.SearchAsync(new RoslynSearchRequest(repo.Root, "anything")),
            await tools.GotoAsync(new RoslynGotoRequest(repo.Root, "anything")),
            await tools.RefsAsync(new RoslynRefsRequest(repo.Root, "anything")),
            await tools.SuggestAsync(new RoslynSuggestRequest(repo.Root, "where is anything?")),
            await tools.DoctorAsync(new RoslynRepoRequest(repo.Root))
        };

        foreach (var result in results)
        {
            using var json = JsonDocument.Parse(JsonSerializer.Serialize(result, JsonOptions.Default));
            Assert.True(json.RootElement.GetProperty("success").ValueKind is JsonValueKind.True or JsonValueKind.False);
            Assert.StartsWith("roslyn_", json.RootElement.GetProperty("tool").GetString(), StringComparison.Ordinal);
            Assert.Equal(repo.Root, json.RootElement.GetProperty("repoRoot").GetString());
            Assert.Equal(JsonValueKind.Number, json.RootElement.GetProperty("elapsedMs").ValueKind);
            Assert.Equal(JsonValueKind.Array, json.RootElement.GetProperty("warnings").ValueKind);
            Assert.Equal(JsonValueKind.Array, json.RootElement.GetProperty("errors").ValueKind);
        }
    }

    [Fact]
    public async Task Adapter_methods_call_application_service_for_every_tool()
    {
        var app = new RecordingApplicationService();
        var tools = new RoslynMcpTools(app);

        await tools.DoctorAsync(new RoslynRepoRequest("C:/repo"));
        await tools.IndexAsync(new RoslynIndexRequest("C:/repo", Force: true));
        await tools.StatusAsync(new RoslynRepoRequest("C:/repo"));
        await tools.SearchAsync(new RoslynSearchRequest("C:/repo", "Customer"));
        await tools.ReadAsync(new RoslynReadRequest("C:/repo", "src/Foo.cs"));
        await tools.PReadAsync(new RoslynPReadRequest("C:/repo", "src/Foo.cs", StartLine: 1, EndLine: 2));
        await tools.GotoAsync(new RoslynGotoRequest("C:/repo", "Customer"));
        await tools.RefsAsync(new RoslynRefsRequest("C:/repo", "Customer"));
        await tools.SuggestAsync(new RoslynSuggestRequest("C:/repo", "where is Customer?"));

        Assert.Equal(new[]
        {
            "doctor",
            "index",
            "status",
            "search",
            "read",
            "pread",
            "goto",
            "refs",
            "suggest"
        }, app.Calls);
    }

    [Fact]
    public async Task Suggest_returns_structured_operations_without_cli_command_strings()
    {
        var result = await new RoslynMcpTools(new RecordingApplicationService())
            .SuggestAsync(new RoslynSuggestRequest("C:/repo", "where is Customer?"));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result, JsonOptions.Default));
        var suggestion = json.RootElement.GetProperty("data")[0];

        Assert.Equal("search", suggestion.GetProperty("operation").GetString());
        Assert.Equal("Customer", suggestion.GetProperty("input").GetProperty("query").GetString());
        Assert.False(suggestion.TryGetProperty("command", out _));
    }

    [Fact]
    public async Task Index_defaults_to_csharp_only_for_mcp_when_include_non_csharp_text_is_omitted()
    {
        var app = new RecordingApplicationService();
        var tools = new RoslynMcpTools(app);

        await tools.IndexAsync(new RoslynIndexRequest("C:/repo"));

        Assert.False(app.LastIndexRequest?.IncludeNonCSharpText);
    }

    [Fact]
    public async Task Index_preserves_explicit_include_non_csharp_text_for_mcp()
    {
        var app = new RecordingApplicationService();
        var tools = new RoslynMcpTools(app);

        await tools.IndexAsync(new RoslynIndexRequest("C:/repo", IncludeNonCSharpText: true));

        Assert.True(app.LastIndexRequest?.IncludeNonCSharpText);
    }

    private sealed class RecordingApplicationService : IRoslynIndexerApplicationService
    {
        private readonly List<string> calls = new();

        public IReadOnlyList<string> Calls => calls;
        public IndexCommandRequest? LastIndexRequest { get; private set; }

        public Task<CommandResponse<DoctorSummary>> DoctorAsync(PathCommandRequest request, CancellationToken cancellationToken = default)
        {
            calls.Add("doctor");
            return Task.FromResult(CommandResponse.Success(new DoctorSummary("C:/repo", Array.Empty<DoctorCheck>()), null, "doctor", null, "C:/repo", 1, null, false));
        }

        public Task<CommandResponse<IndexSummary>> IndexAsync(IndexCommandRequest request, CancellationToken cancellationToken = default)
        {
            calls.Add("index");
            LastIndexRequest = request;
            return Task.FromResult(CommandResponse.Success(new IndexSummary("C:/repo", 0, 0, 0, 0, 0, TimeSpan.Zero, false, false, 0, 0, 0), null, "index", null, "C:/repo", 1, null, false));
        }

        public CommandResponse<StatusSummary> Status(PathCommandRequest request)
        {
            calls.Add("status");
            return CommandResponse.Success(new StatusSummary(IndexStatus.Missing, "C:/repo", 0, 0, 0, 0, 0, 0, Array.Empty<string>()), null, "status", null, "C:/repo", 1, null, false);
        }

        public CommandResponse<IReadOnlyList<SearchResult>> Search(SearchCommandRequest request)
        {
            calls.Add("search");
            return EmptySearch("search", request.Query);
        }

        public CommandResponse<RepositoryFileReadResult> Read(FileReadCommandRequest request)
        {
            calls.Add("read");
            return CommandResponse.Success(new RepositoryFileReadResult("src/Foo.cs", "C:/repo", "csharp", 1, 1, "sha256:x", DateTimeOffset.UtcNow, false, "x"), null, "read", null, "C:/repo", 1, null, false);
        }

        public CommandResponse<RepositoryPartialFileReadResult> PartialRead(PartialFileReadCommandRequest request)
        {
            calls.Add("pread");
            return CommandResponse.Success(new RepositoryPartialFileReadResult("src/Foo.cs", "C:/repo", "csharp", 1, 1, "sha256:x", DateTimeOffset.UtcNow, false, "range", 1, 1, 1, "x"), null, "pread", null, "C:/repo", 1, null, false);
        }

        public CommandResponse<IReadOnlyList<SearchResult>> Goto(SymbolQueryCommandRequest request)
        {
            calls.Add("goto");
            return EmptySearch("goto", request.Query);
        }

        public Task<CommandResponse<object>> RefsAsync(RefsCommandRequest request, CancellationToken cancellationToken = default)
        {
            calls.Add("refs");
            return Task.FromResult(CommandResponse.Success<object>(Array.Empty<SearchResult>(), null, "refs", request.Query, "C:/repo", 1, null, true));
        }

        public CommandResponse<object> Suggest(SuggestCommandRequest request)
        {
            calls.Add("suggest");
            return CommandResponse.Success<object>(new[] { new QuerySuggestion("ri search Customer", "Customer", "all", 0.9, "symbol", "mixed") }, null, "suggest", request.Question, "C:/repo", 1, null, true);
        }

        private static CommandResponse<IReadOnlyList<SearchResult>> EmptySearch(string command, string? query)
            => CommandResponse.Success<IReadOnlyList<SearchResult>>(Array.Empty<SearchResult>(), null, command, query, "C:/repo", 1, null, true);
    }
}
