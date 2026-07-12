using RoslynRepoIndexer.Core;

namespace RoslynRepoIndexer.Tests;

public sealed class SemanticQueryTests
{
    [Fact]
    public async Task Roslyn_indexing_persists_invocation_hierarchy_and_override_edges()
    {
        using var repo = TestRepo.Create();
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "Graph.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "Graph.cs"), """
            namespace Graph;
            public interface IWorker { void Execute(); }
            public class BaseWorker { public virtual void Execute() { } }
            public class Worker : BaseWorker, IWorker
            {
                public override void Execute() => Helper();
                private void Helper() { }
            }
            public class Caller { public void Run() => new Worker().Execute(); }
            """);

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default with { IncludeNonCSharpText = false });
        var snapshot = IndexStore.Read(repo.Root);
        var execute = snapshot.Symbols.Single(symbol => symbol.Name == "Execute" && symbol.ContainerName == "Graph.Worker");
        var helper = snapshot.Symbols.Single(symbol => symbol.Name == "Helper" && symbol.ContainerName == "Graph.Worker");
        var worker = snapshot.Symbols.Single(symbol => symbol.FullyQualifiedName.EndsWith("Graph.Worker", StringComparison.Ordinal));
        var baseWorker = snapshot.Symbols.Single(symbol => symbol.FullyQualifiedName.EndsWith("Graph.BaseWorker", StringComparison.Ordinal));
        var service = new SemanticQueryService(new QueryIndex(snapshot), repo.Root);
        var graph = service.CallGraph(execute.SymbolId, CallGraphDirection.Both, depth: 1, maxNodes: 20);
        var impact = service.Impact(baseWorker.SymbolId);

        Assert.Contains(snapshot.References, reference => reference.ContainingSymbolId == execute.SymbolId && reference.SymbolId == helper.SymbolId && reference.IsInvocation);
        Assert.Contains(graph.Edges, edge => edge.From == execute.SymbolId && edge.To == helper.SymbolId);
        Assert.Contains(worker.BaseTypeIds, id => id == baseWorker.SymbolId);
        Assert.Contains(impact.Links, link => link.SymbolId == worker.SymbolId && link.Relationship == "derived-type");
        Assert.NotNull(execute.OverriddenSymbolId);
    }

    [Fact]
    public void Outline_inspect_context_graph_impact_and_test_selection_are_bounded_and_structured()
    {
        using var repo = TestRepo.Create();
        File.WriteAllText(Path.Combine(repo.Root, "Widget.cs"), "namespace App;\n/// <summary>A widget.</summary>\npublic class Widget\n{\n    public void Run() => Helper();\n    private void Helper() { }\n}\n");
        File.WriteAllText(Path.Combine(repo.Root, "WidgetTests.cs"), "namespace App.Tests;\npublic class WidgetTests { public void TestRun() { } }\n");
        var service = new SemanticQueryService(new QueryIndex(Snapshot(repo.Root)), repo.Root);

        var outline = service.Outline("Widget.cs", depth: 3, maxResults: 20, maxChars: 10_000, includePrivate: true);
        var inspect = service.Inspect("widget", Enum.GetValues<InspectInclude>(), maxResults: 20, maxChars: 20_000);
        var context = service.Context("run", maxChars: 5_000);
        var graph = service.CallGraph("run", CallGraphDirection.Both, depth: 1, maxNodes: 10);
        var impact = service.Impact("widget");
        var tests = service.TestsFor("widget");

        Assert.Equal(new[] { "Widget", "Run", "Helper" }, outline.Items.Select(item => item.Symbol.Name));
        Assert.Equal(new[] { "Run", "Helper" }, inspect.Members.Select(member => member.Name));
        Assert.Contains(inspect.Implementations, item => item.Name == "DerivedWidget");
        Assert.Contains("public void Run", context.Source, StringComparison.Ordinal);
        Assert.Contains(graph.Edges, edge => edge.From == "run" && edge.To == "helper");
        Assert.Contains(graph.Edges, edge => edge.From == "test" && edge.To == "run");
        Assert.Contains(impact.Links, link => link.SymbolId == "derived" && link.Relationship == "derived-type");
        Assert.Contains(tests, test => test.Path == "WidgetTests.cs");
    }

    [Fact]
    public void Ambiguous_queries_return_candidates_and_outline_obeys_character_budget()
    {
        using var repo = TestRepo.Create();
        var snapshot = Snapshot(repo.Root) with
        {
            Symbols = Snapshot(repo.Root).Symbols.Concat(new[] { Symbol("other-widget", "other", "Widget", "Other.Widget", "Other.cs", 1, null) }).ToArray()
        };
        var service = new SemanticQueryService(new QueryIndex(snapshot), repo.Root);

        var error = Assert.Throws<SymbolQueryException>(() => service.Inspect("Widget", Array.Empty<InspectInclude>()));
        var outline = service.Outline("Widget.cs", depth: 3, maxResults: 20, maxChars: 500, includePrivate: true);

        Assert.Equal("ambiguous-symbol", error.Code);
        Assert.Equal(2, error.Candidates.Count);
        Assert.True(outline.Truncated);
    }

    private static IndexSnapshot Snapshot(string root)
    {
        var documents = new[]
        {
            Document("prod", "Widget.cs", "App"),
            Document("tests", "WidgetTests.cs", "App.Tests")
        };
        var symbols = new[]
        {
            Symbol("widget", "prod", "Widget", "App.Widget", "Widget.cs", 3, "App", baseIds: new[] { "base" }, interfaceIds: new[] { "iface" }),
            Symbol("run", "prod", "Run", "App.Widget.Run", "Widget.cs", 5, "App.Widget"),
            Symbol("helper", "prod", "Helper", "App.Widget.Helper", "Widget.cs", 6, "App.Widget", accessibility: "private"),
            Symbol("base", "prod", "BaseWidget", "App.BaseWidget", "BaseWidget.cs", 1, "App"),
            Symbol("iface", "prod", "IWidget", "App.IWidget", "IWidget.cs", 2, "App"),
            Symbol("derived", "prod", "DerivedWidget", "App.DerivedWidget", "DerivedWidget.cs", 8, "App", baseIds: new[] { "widget" }),
            Symbol("test", "tests", "TestRun", "App.Tests.WidgetTests.TestRun", "WidgetTests.cs", 2, "App.Tests.WidgetTests")
        };
        var references = new[]
        {
            Reference("r1", "helper", "prod", "Widget.cs", 5, "run"),
            Reference("r2", "run", "tests", "WidgetTests.cs", 2, "test")
        };
        var manifest = IndexManifest.CreateNew(root, "config", "workspace") with { DocumentCount = documents.Length, SymbolCount = symbols.Length, ReferenceCount = references.Length };
        return new IndexSnapshot(manifest, documents, symbols, references, Array.Empty<TokenPosting>());
    }

    private static DocumentEntry Document(string id, string path, string project)
        => new(id, project, path, project, "C#", true, false, false, 10, DateTimeOffset.UtcNow, "hash", "decl", 10);

    private static SymbolEntry Symbol(string id, string documentId, string name, string fqn, string path, int line, string? container, string accessibility = "public", string[]? baseIds = null, string[]? interfaceIds = null)
        => new(id, documentId, documentId == "tests" ? "App.Tests" : "App", name is "Widget" or "BaseWidget" or "IWidget" or "DerivedWidget" ? "class" : "method", name, name, fqn, container, $"public void {name}()", accessibility, Array.Empty<string>(), path, line, 1, line, name.Length + 1, line * 10, name.Length, true, false, Array.Empty<string>(), "void", documentId == "tests" ? "App.Tests" : "App", null, baseIds, interfaceIds);

    private static ReferenceEntry Reference(string id, string symbolId, string documentId, string path, int line, string containing)
        => new(id, symbolId, documentId, documentId == "tests" ? "App.Tests" : "App", symbolId, path, line, 1, line, 4, line * 10, 3, documentId == "tests" ? "App.Tests" : "App", "invocation", containing, true);
}
