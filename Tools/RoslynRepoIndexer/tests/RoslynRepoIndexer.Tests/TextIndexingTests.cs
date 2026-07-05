using RoslynRepoIndexer.Core;

namespace RoslynRepoIndexer.Tests;

public sealed class TextIndexingTests
{
    [Fact]
    public void Tokenizer_splits_on_whitespace_punctuation_operators_and_normalizes_lowercase()
    {
        var tokens = Tokenizer.Tokenize("SaveCustomer + HTTPResponse, route/api=>OK").Select(t => t.Value).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("savecustomer", tokens);
        Assert.Contains("save", tokens);
        Assert.Contains("customer", tokens);
        Assert.Contains("http", tokens);
        Assert.Contains("response", tokens);
        Assert.Contains("route", tokens);
        Assert.Contains("api", tokens);
        Assert.Contains("ok", tokens);
        Assert.DoesNotContain("+", tokens);
        Assert.DoesNotContain("=>", tokens);
    }

    [Fact]
    public void Tokenizer_expands_interface_prefix_and_filters_plain_text_single_character_tokens()
    {
        var plainTokens = Tokenizer.Tokenize("IHttpClientFactory i x y T z").Select(t => t.Value).ToHashSet(StringComparer.Ordinal);
        var codeTokens = Tokenizer.Tokenize("IHttpClientFactory i x y T z", includeCodeSingleCharacterTokens: true).Select(t => t.Value).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("ihttpclientfactory", plainTokens);
        Assert.Contains("ihttp", plainTokens);
        Assert.Contains("http", plainTokens);
        Assert.Contains("client", plainTokens);
        Assert.Contains("factory", plainTokens);
        Assert.DoesNotContain("i", plainTokens);
        Assert.DoesNotContain("x", plainTokens);
        Assert.DoesNotContain("y", plainTokens);
        Assert.DoesNotContain("t", plainTokens);
        Assert.Contains("i", codeTokens);
        Assert.Contains("x", codeTokens);
        Assert.Contains("y", codeTokens);
        Assert.Contains("t", codeTokens);
        Assert.DoesNotContain("z", codeTokens);
    }

    [Fact]
    public async Task IndexBuilder_marks_csharp_token_weights_from_roslyn_tokens_and_trivia()
    {
        using var repo = TestRepo.Create();
        CreateMinimalProject(repo.Root);
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "Program.cs"), """
            namespace App;

            // customer comment
            public sealed class CustomerService
            {
                public string Name => "customer string";
            }
            """);

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);

        var snapshot = IndexStore.Read(repo.Root);
        var tokens = snapshot.Tokens
            .Where(t => t.Path == "Program.cs")
            .Select(t => (t.Token, t.Weight))
            .ToHashSet();

        Assert.Contains(("customerservice", "identifier"), tokens);
        Assert.Contains(("public", "keyword"), tokens);
        Assert.Contains(("customer", "comment"), tokens);
        Assert.Contains(("string", "string"), tokens);
    }

    [Fact]
    public async Task IndexBuilder_indexes_non_csharp_text_tokens_line_by_line()
    {
        using var repo = TestRepo.Create();
        CreateMinimalProject(repo.Root);
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "notes.md"), "alpha\nbeta customer");

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);

        var snapshot = IndexStore.Read(repo.Root);
        Assert.Contains(snapshot.Tokens, t => t.Path == "notes.md" && t.Token == "alpha" && t.Line == 1);
        Assert.Contains(snapshot.Tokens, t => t.Path == "notes.md" && t.Token == "beta" && t.Line == 2);
        Assert.Contains(snapshot.Tokens, t => t.Path == "notes.md" && t.Token == "customer" && t.Line == 2);
    }

    [Fact]
    public async Task IndexBuilder_adds_path_weight_tokens_for_path_segments_and_file_name()
    {
        using var repo = TestRepo.Create();
        CreateMinimalProject(repo.Root);
        Directory.CreateDirectory(Path.Combine(repo.Root, "docs", "ApiRoutes"));
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "docs", "ApiRoutes", "Customer-Config.md"), "plain content");

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);

        var snapshot = IndexStore.Read(repo.Root);
        var pathTokens = snapshot.Tokens
            .Where(t => t.Path == "docs/ApiRoutes/Customer-Config.md" && t.Weight == "path")
            .Select(t => t.Token)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("docs", pathTokens);
        Assert.Contains("apiroutes", pathTokens);
        Assert.Contains("api", pathTokens);
        Assert.Contains("routes", pathTokens);
        Assert.Contains("customer-config", pathTokens);
        Assert.Contains("customer", pathTokens);
        Assert.Contains("config", pathTokens);
    }

    [Fact]
    public async Task IndexBuilder_skips_binary_files_with_nul_in_first_8kb()
    {
        using var repo = TestRepo.Create();
        CreateMinimalProject(repo.Root);
        var bytes = Enumerable.Repeat((byte)'A', 7 * 1024).ToArray();
        bytes[5 * 1024] = 0;
        await File.WriteAllBytesAsync(Path.Combine(repo.Root, "binary.txt"), bytes);

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);

        var snapshot = IndexStore.Read(repo.Root);
        Assert.DoesNotContain(snapshot.Documents, d => d.RelativePath == "binary.txt");
        Assert.DoesNotContain(snapshot.Tokens, t => t.Path == "binary.txt");
    }

    [Fact]
    public async Task IndexBuilder_skips_text_files_over_maxTextFileBytes_with_warning()
    {
        using var repo = TestRepo.Create();
        CreateMinimalProject(repo.Root);
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "large.txt"), "0123456789");

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default with { MaxTextFileBytes = 4 });

        var snapshot = IndexStore.Read(repo.Root);
        Assert.DoesNotContain(snapshot.Documents, d => d.RelativePath == "large.txt");
        Assert.DoesNotContain(snapshot.Tokens, t => t.Path == "large.txt");
        Assert.True(snapshot.Manifest.WarningCount >= 1);
        Assert.Contains(snapshot.Manifest.RecentWarnings, warning => warning.Contains("large.txt", StringComparison.Ordinal) && warning.Contains("maxTextFileBytes", StringComparison.Ordinal));
    }

    private static void CreateMinimalProject(string root)
    {
        File.WriteAllText(Path.Combine(root, "App.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(root, "Program.cs"), "namespace App; public sealed class Program { }");
    }
}
