using RoslynRepoIndexer.Core;
using Ri.Mcp;

namespace RoslynRepoIndexer.Tests;

public sealed class McpBehaviorTests
{
    [Fact]
    public void Tool_registry_lists_expected_tools_in_deterministic_order()
    {
        var names = RoslynMcpToolCatalog.Tools.Select(tool => tool.Name).ToArray();

        Assert.Equal(
            new[]
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
            },
            names);
    }

    [Fact]
    public void Read_description_tells_agents_to_prefer_full_file_before_editing()
    {
        var read = RoslynMcpToolCatalog.Tools.Single(tool => tool.Name == "roslyn_read");
        var pread = RoslynMcpToolCatalog.Tools.Single(tool => tool.Name == "roslyn_pread");

        Assert.Contains("full", read.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("before editing", read.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("targeted partial", pread.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Search_validation_returns_actionable_structured_error()
    {
        var tools = new RoslynMcpTools();
        var result = await tools.SearchAsync(new RoslynSearchRequest(Directory.GetCurrentDirectory(), "   "));

        Assert.False(result.Success);
        Assert.Equal("roslyn_search", result.Tool);
        Assert.Contains("query", result.Errors.Single(), StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(result.Warnings);
    }

    [Fact]
    public async Task Read_rejects_paths_outside_repository_root()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        var outside = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        await File.WriteAllTextAsync(outside, "outside");

        try
        {
            var tools = new RoslynMcpTools();
            var result = await tools.ReadAsync(new RoslynReadRequest(repo.Root, outside));

            Assert.False(result.Success);
            Assert.Equal("roslyn_read", result.Tool);
            Assert.Equal(repo.Root, result.RepoRoot);
            Assert.Contains("outside the repository root", result.Errors.Single(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(outside);
        }
    }

    [Fact]
    public async Task Read_output_contract_contains_full_file_content_and_common_fields()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        File.WriteAllText(Path.Combine(repo.Root, "Sample.cs"), "line 1\nline 2\n");
        var tools = new RoslynMcpTools();

        var result = await tools.ReadAsync(new RoslynReadRequest(repo.Root, "Sample.cs"));

        Assert.True(result.Success);
        Assert.Equal("roslyn_read", result.Tool);
        Assert.Equal(repo.Root, result.RepoRoot);
        Assert.Empty(result.Warnings);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Data);
        Assert.Equal("line 1\nline 2\n", result.Data.Content);
    }

    [Fact]
    public async Task Mcp_read_uses_same_application_service_contract_as_cli_read()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        File.WriteAllText(Path.Combine(repo.Root, "Sample.cs"), "class Sample {}\n");
        var service = new RoslynIndexerApplicationService(repo.Root);
        var tools = new RoslynMcpTools(service);

        var serviceResult = service.Read(new FileReadCommandRequest("Sample.cs"));
        var mcpResult = await tools.ReadAsync(new RoslynReadRequest(repo.Root, "Sample.cs"));

        Assert.Equal(serviceResult.Success, mcpResult.Success);
        Assert.Equal(serviceResult.RepoRoot, mcpResult.RepoRoot);
        Assert.Equal(serviceResult.Data!.Content, mcpResult.Data!.Content);
        Assert.Equal(serviceResult.Data.FilePath, mcpResult.Data.FilePath);
    }

    [Theory]
    [InlineData("roslyn_doctor")]
    [InlineData("roslyn_index")]
    [InlineData("roslyn_status")]
    [InlineData("roslyn_search")]
    [InlineData("roslyn_read")]
    [InlineData("roslyn_pread")]
    [InlineData("roslyn_goto")]
    [InlineData("roslyn_refs")]
    [InlineData("roslyn_suggest")]
    public void Every_mcp_tool_declares_input_schema(string toolName)
    {
        var tool = RoslynMcpToolCatalog.Tools.Single(tool => tool.Name == toolName);

        Assert.Contains("\"type\":\"object\"", tool.InputSchemaJson, StringComparison.Ordinal);
        Assert.Contains("\"properties\"", tool.InputSchemaJson, StringComparison.Ordinal);
    }
}
