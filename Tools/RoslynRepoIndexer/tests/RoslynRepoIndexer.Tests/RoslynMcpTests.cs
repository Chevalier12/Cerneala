using System.Text.Json;
using Ri.Mcp;
using RoslynRepoIndexer.Core;

namespace RoslynRepoIndexer.Tests;

public sealed class RoslynMcpTests
{
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
            "roslyn_suggest"
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
        Assert.Contains("outside", result.Errors[0], StringComparison.OrdinalIgnoreCase);
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
            return CommandResponse.Success<object>(Array.Empty<QuerySuggestion>(), null, "suggest", request.Question, "C:/repo", 1, null, true);
        }

        private static CommandResponse<IReadOnlyList<SearchResult>> EmptySearch(string command, string? query)
            => CommandResponse.Success<IReadOnlyList<SearchResult>>(Array.Empty<SearchResult>(), null, command, query, "C:/repo", 1, null, true);
    }
}
