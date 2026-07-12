using RoslynRepoIndexer.Core;

namespace RoslynRepoIndexer.Tests;

public sealed class SemanticChangesTests
{
    [Fact]
    public void Previous_generation_diff_reports_files_signatures_public_api_and_projects()
    {
        using var repo = TestRepo.Create();
        var before = Snapshot(repo.Root, "old-content", new[]
        {
            Symbol("shared", "void Run()", "public"),
            Symbol("removed", "void Removed()", "internal")
        });
        var after = Snapshot(repo.Root, "new-content", new[]
        {
            Symbol("shared", "int Run()", "public"),
            Symbol("added", "void Added()", "public")
        });
        IndexStore.Write(repo.Root, before);
        IndexStore.Write(repo.Root, after);

        var result = new SemanticChangeService(repo.Root).Compare(ChangeComparison.PreviousGeneration);

        Assert.Equal(before.Manifest.GenerationId, result.BaseId);
        Assert.Equal(after.Manifest.GenerationId, result.TargetId);
        Assert.Contains(result.Files, file => file.Path == "Service.cs" && file.Status == "modified");
        Assert.Contains(result.Symbols, change => change.SymbolId == "shared" && change.Kind == SemanticChangeKind.Modified && change.SignatureChanged && change.PublicApiChange);
        Assert.Contains(result.Symbols, change => change.SymbolId == "removed" && change.Kind == SemanticChangeKind.Removed);
        Assert.Contains(result.Symbols, change => change.SymbolId == "added" && change.Kind == SemanticChangeKind.Added);
        Assert.Equal(new[] { "App" }, result.AffectedProjects);
    }

    private static IndexSnapshot Snapshot(string root, string contentHash, SymbolEntry[] symbols)
    {
        var document = new DocumentEntry("doc", "project", "Service.cs", "App", "C#", true, false, false, 10, DateTimeOffset.UtcNow, contentHash, "decl", 2);
        var manifest = IndexManifest.CreateNew(root, "config", "workspace") with { DocumentCount = 1, SymbolCount = symbols.Length };
        return new IndexSnapshot(manifest, new[] { document }, symbols, Array.Empty<ReferenceEntry>(), Array.Empty<TokenPosting>());
    }

    private static SymbolEntry Symbol(string id, string signature, string accessibility)
        => new(id, "doc", "project", "method", id, id, "App.Service." + id, "App.Service", signature, accessibility, Array.Empty<string>(), "Service.cs", 1, 1, 1, 2, 0, 1, true, false, Array.Empty<string>(), signature.StartsWith("int", StringComparison.Ordinal) ? "int" : "void", "App", null);
}
