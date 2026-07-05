using System.Text.Json;
using RoslynRepoIndexer.Core;

namespace RoslynRepoIndexer.Tests;

public sealed class ApplicationServiceTests
{
    [Fact]
    public void Status_returns_common_command_response_contract()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        var service = new RoslynIndexerApplicationService(repo.Root);

        var response = service.Status(new PathCommandRequest());

        Assert.True(response.Success);
        Assert.Equal(0, response.ExitCode);
        Assert.Equal("status", response.Command);
        Assert.Equal(repo.Root, response.RepoRoot);
        Assert.NotNull(response.ElapsedMs);
        Assert.NotNull(response.Warnings);
        Assert.NotNull(response.Errors);
        Assert.NotNull(response.Data);
        Assert.Equal("missing", response.Data.IndexState);
    }

    [Fact]
    public void Read_uses_repository_file_reader_and_preserves_full_file_contract()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        Directory.CreateDirectory(Path.Combine(repo.Root, "src"));
        File.WriteAllText(Path.Combine(repo.Root, "src", "Foo.cs"), "namespace Demo;\r\npublic sealed class Foo {}\r\n");
        var service = new RoslynIndexerApplicationService(repo.Root);

        var response = service.Read(new FileReadCommandRequest("src/Foo.cs"));
        var direct = new RepositoryFileReader().Read(repo.Root, "src/Foo.cs", IndexerConfig.Default);

        Assert.True(response.Success);
        Assert.Equal("read", response.Command);
        Assert.Equal(repo.Root, response.RepoRoot);
        Assert.Equal(direct.FilePath, response.Data?.FilePath);
        Assert.Equal(direct.Content, response.Data?.Content);
        Assert.Equal(direct.ContentHash, response.Data?.ContentHash);
    }

    [Theory]
    [InlineData("../outside.txt", "outside")]
    [InlineData("", "missing")]
    public void Read_returns_useful_failure_for_invalid_input(string filePath, string expectedMessage)
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(repo.Root)!, "outside.txt"), "outside");
        var service = new RoslynIndexerApplicationService(repo.Root);

        var response = service.Read(new FileReadCommandRequest(filePath));

        Assert.False(response.Success);
        Assert.Equal(2, response.ExitCode);
        Assert.Equal("read", response.Command);
        Assert.Equal(repo.Root, response.RepoRoot);
        Assert.Contains(expectedMessage, response.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Search_uses_same_search_service_path_as_core()
    {
        using var repo = TestRepo.Create();
        await CreateMinimalProjectAsync(repo.Root);
        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);
        var service = new RoslynIndexerApplicationService(repo.Root);

        var response = service.Search(new SearchCommandRequest("CustomerService", Mode: SearchMode.Symbol, Limit: 10));
        var snapshot = IndexStore.Read(repo.Root);
        var direct = new SearchService(snapshot, new SnippetReader(repo.Root))
            .Search(new SearchRequest("CustomerService", SearchMode.Symbol, 10));

        Assert.True(response.Success);
        Assert.Equal("search", response.Command);
        Assert.Equal("CustomerService", response.Query);
        Assert.Equal(direct.Select(result => result.Path), response.Data!.Select(result => result.Path));
        Assert.NotNull(response.Results);
    }

    [Fact]
    public void Json_serialization_keeps_output_contract_shape()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        var service = new RoslynIndexerApplicationService(repo.Root);

        var response = service.Status(new PathCommandRequest());
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(response, JsonOptions.Default));

        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("status", json.RootElement.GetProperty("command").GetString());
        Assert.Equal(repo.Root, json.RootElement.GetProperty("repoRoot").GetString());
        Assert.Equal(JsonValueKind.Array, json.RootElement.GetProperty("warnings").ValueKind);
        Assert.Equal(JsonValueKind.Array, json.RootElement.GetProperty("errors").ValueKind);
        Assert.Equal(JsonValueKind.Number, json.RootElement.GetProperty("elapsedMs").ValueKind);
    }

    private static async Task CreateMinimalProjectAsync(string root)
    {
        await File.WriteAllTextAsync(Path.Combine(root, "Sample.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "CustomerService.cs"), """
            namespace Sample;

            public sealed class CustomerService
            {
                public string GetCustomerName(string id) => id;
            }
            """);
    }
}
