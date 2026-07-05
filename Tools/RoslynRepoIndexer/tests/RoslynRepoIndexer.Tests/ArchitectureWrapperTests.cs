using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynRepoIndexer.Core;

namespace RoslynRepoIndexer.Tests;

public sealed class ArchitectureWrapperTests
{
    [Fact]
    public async Task WorkspaceLoader_discovers_and_loads_configured_workspace_inputs()
    {
        using var repo = TestRepo.Create();
        File.WriteAllText(Path.Combine(repo.Root, "App.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(repo.Root, "CustomerService.cs"), "namespace Demo; public class CustomerService {}");

        var loader = new WorkspaceLoader();
        var inputs = loader.Discover(repo.Root, IndexerConfig.Default);

        var input = Assert.Single(inputs);
        Assert.Equal("csproj", input.Kind);
        Assert.Equal(Path.Combine(repo.Root, "App.csproj"), input.Path);

        using var loaded = await loader.LoadAsync(repo.Root, input);
        Assert.Contains(loaded.Solution.Projects, p => p.Name == "App");
    }

    [Fact]
    public void SymbolIdProvider_generates_stable_ids_from_symbol_identity_parts()
    {
        var provider = new SymbolIdProvider();

        var id1 = provider.Create("class", "Demo.CustomerService", "CustomerService.Get(int)", "src/CustomerService.cs", 4, 8);
        var id2 = provider.Create("class", "Demo.CustomerService", "CustomerService.Get(int)", "src/CustomerService.cs", 4, 8);
        var renamed = provider.Create("class", "Demo.ClientService", "ClientService.Get(int)", "src/CustomerService.cs", 4, 8);

        Assert.Equal(id1, id2);
        Assert.NotEqual(id1, renamed);
        Assert.Equal(16, id1.Length);
    }

    [Fact]
    public void SymbolCollector_and_ReferenceCollector_collect_real_semantic_entries()
    {
        var (tree, model) = Compile("""
            namespace Demo;
            public class CustomerService
            {
                public string Name { get; set; } = "";
                public void Save() { Name = Name.Trim(); }
            }
            public class Consumer
            {
                public void Run() { new CustomerService().Save(); }
            }
            """);

        var root = tree.GetRoot();
        var symbols = new SymbolCollector().Collect(root, model, "doc1", "project1", "src/CustomerService.cs", "App");
        var references = new ReferenceCollector().Collect(root, model, "doc1", "project1", "src/CustomerService.cs", "App");

        Assert.Contains(symbols, s => s.Kind == "class" && s.Name == "CustomerService" && s.FullyQualifiedName == "Demo.CustomerService");
        Assert.Contains(symbols, s => s.Kind == "method" && s.Name == "Save");
        Assert.Contains(references, r => r.ReferencedName == "CustomerService" && r.ReferenceKind == "object-creation");
        Assert.Contains(references, r => r.ReferencedName == "Save" && r.ReferenceKind == "invocation");
    }

    [Fact]
    public void TextIndexer_indexes_text_tokens_and_path_tokens()
    {
        var postings = new TextIndexer().IndexText("docs/customer-service.md", "CustomerService saves customer data", "Docs", "doc1");

        Assert.Contains(postings, p => p.Token == "customerservice" && p.Field == "text");
        Assert.Contains(postings, p => p.Token == "customer" && p.Field == "path");
        Assert.Contains(postings, p => p.Token == "service" && p.Weight == "path");
    }

    [Fact]
    public void IndexReader_loads_manifest_and_jsonl_from_store()
    {
        using var repo = TestRepo.Create();
        var snapshot = new IndexSnapshot(
            IndexManifest.CreateNew(repo.Root, "cfg", "workspace"),
            new[] { Doc("d1", "src/A.cs", "App") },
            new[] { Sym("s1", "class", "CustomerService", "Demo.CustomerService", "src/A.cs", "App") },
            Array.Empty<ReferenceEntry>(),
            Array.Empty<TokenPosting>());
        IndexStore.Write(repo.Root, snapshot);

        var loaded = new IndexReader().Read(repo.Root);

        Assert.Equal(repo.Root, loaded.Manifest.RepoRoot);
        Assert.Single(loaded.Documents);
        Assert.Single(loaded.Symbols);
    }

    [Fact]
    public void JsonOutputWriter_and_HumanOutputWriter_render_stable_outputs()
    {
        var result = new SearchResult(
            "src/CustomerService.cs",
            10,
            5,
            10,
            20,
            "reference",
            300,
            "reference-match",
            "customer.Save();",
            "sym1",
            "Save",
            "Demo.CustomerService",
            "Demo.CustomerService.Save()",
            "invocation",
            "App");

        var json = new JsonOutputWriter().Serialize(CommandResponse.Success(new[] { result }, warnings: Array.Empty<string>()));
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("src/CustomerService.cs", document.RootElement.GetProperty("data")[0].GetProperty("path").GetString());

        using var writer = new StringWriter();
        new HumanOutputWriter().WriteSearchResults(writer, new[] { result }, "No results.");
        var human = writer.ToString();

        Assert.Contains("[reference] Demo.CustomerService.Save()", human, StringComparison.Ordinal);
        Assert.Contains("ref-kind=invocation", human, StringComparison.Ordinal);
        Assert.Contains("customer.Save();", human, StringComparison.Ordinal);
    }

    private static (SyntaxTree Tree, SemanticModel Model) Compile(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "Tests",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        return (tree, compilation.GetSemanticModel(tree));
    }

    private static DocumentEntry Doc(string id, string path, string? project)
        => new(id, project is null ? null : "pid-" + project, path, project, "C#", true, false, false, 10, DateTimeOffset.UtcNow, "h", "dh", 1);

    private static SymbolEntry Sym(string id, string kind, string name, string fqn, string path, string? project)
        => new(id, "d1", project is null ? null : "pid-" + project, kind, name, name, fqn, null, name, "public", Array.Empty<string>(), path, 1, 1, 1, 1 + name.Length, 0, name.Length, true, false, Array.Empty<string>(), null, project, null);
}
