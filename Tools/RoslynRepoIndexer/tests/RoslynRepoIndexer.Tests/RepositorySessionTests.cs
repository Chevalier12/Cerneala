using RoslynRepoIndexer.Core;
using Ri.Mcp;

namespace RoslynRepoIndexer.Tests;

public sealed class RepositorySessionTests
{
    [Fact]
    public void Registry_evicts_the_least_recently_used_repository_without_a_timer()
    {
        using var first = TestRepo.Create();
        using var second = TestRepo.Create();
        using var third = TestRepo.Create();
        var registry = new RepositorySessionRegistry(maxSessions: 2);

        registry.Get(first.Root);
        registry.Get(second.Root);
        registry.Get(first.Root);
        registry.Get(third.Root);

        Assert.Equal(2, registry.Count);
        Assert.True(registry.Contains(first.Root));
        Assert.True(registry.Contains(third.Root));
        Assert.False(registry.Contains(second.Root));
    }

    [Fact]
    public async Task Concurrent_reads_load_a_generation_once_and_reuse_the_same_query_index()
    {
        using var repo = TestRepo.Create();
        IndexStore.Write(repo.Root, Snapshot(repo.Root, "first"));
        var session = new RepositoryIndexSession(repo.Root);

        var reads = await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(_ => session.GetQueryIndexAsync().AsTask()));

        Assert.All(reads, item => Assert.Same(reads[0], item));
        Assert.Equal(1, session.Metrics.ReloadCount);
        Assert.Equal(99, session.Metrics.SessionHits);
    }

    [Fact]
    public async Task Generation_change_is_atomically_reloaded_for_concurrent_readers()
    {
        using var repo = TestRepo.Create();
        IndexStore.Write(repo.Root, Snapshot(repo.Root, "first"));
        var session = new RepositoryIndexSession(repo.Root);
        var first = await session.GetQueryIndexAsync();

        IndexStore.Write(repo.Root, Snapshot(repo.Root, "second"));
        var reads = await Task.WhenAll(Enumerable.Range(0, 50)
            .Select(_ => session.GetQueryIndexAsync().AsTask()));

        Assert.All(reads, item =>
        {
            Assert.Same(reads[0], item);
            Assert.Equal("second", item.Snapshot.Documents.Single().DocumentId);
        });
        Assert.NotSame(first, reads[0]);
        Assert.Equal(2, session.Metrics.ReloadCount);
    }

    [Fact]
    [Trait("Category", "Stress")]
    public async Task Ten_thousand_persistent_mcp_queries_do_not_retain_per_query_memory()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        IndexStore.Write(repo.Root, SearchableSnapshot(repo.Root));
        var registry = new RepositorySessionRegistry();
        var tools = new RoslynMcpTools(registry, new RepositoryBinding(repo.Root), new ContinuationTokenCodec());
        var request = new RoslynSearchRequest(RepoRoot: repo.Root, Query: "Target", Limit: 10);

        for (var index = 0; index < 100; index++) _ = await tools.SearchAsync(request);
        ForceFullCollection();
        var baseline = GC.GetTotalMemory(forceFullCollection: true);

        for (var index = 0; index < 10_000; index++)
        {
            var response = await tools.SearchAsync(request);
            Assert.True(response.Success, string.Join("; ", response.Errors.Select(error => error.Code + ": " + error.Message)));
        }

        ForceFullCollection();
        var retained = GC.GetTotalMemory(forceFullCollection: true) - baseline;
        Assert.True(retained < 4 * 1024 * 1024, $"Persistent query loop retained {retained:N0} bytes.");
    }

    private static IndexSnapshot Snapshot(string root, string id)
        => new(
            IndexManifest.CreateNew(root, "config", "workspace") with
            {
                DocumentCount = 1,
                UpdatedUtc = DateTimeOffset.UtcNow
            },
            new[]
            {
                new DocumentEntry(id, null, id + ".cs", null, "C#", true, false, false, 1, DateTimeOffset.UtcNow, "hash", "decl", 1)
            },
            Array.Empty<SymbolEntry>(),
            Array.Empty<ReferenceEntry>(),
            Array.Empty<TokenPosting>());

    private static IndexSnapshot SearchableSnapshot(string root)
    {
        var document = new DocumentEntry("doc", null, "Target.cs", "Tests", "C#", true, false, false, 1, DateTimeOffset.UtcNow, "hash", "decl", 1);
        var symbol = new SymbolEntry("symbol", "doc", null, "class", "Target", "Target", "Tests.Target", "Tests", "class Target", "public", Array.Empty<string>(), "Target.cs", 1, 1, 1, 7, 0, 6, true, false, Array.Empty<string>(), null, "Tests", null);
        return new IndexSnapshot(
            IndexManifest.CreateNew(root, "config", "workspace") with { DocumentCount = 1, SymbolCount = 1 },
            new[] { document },
            new[] { symbol },
            Array.Empty<ReferenceEntry>(),
            new[] { new TokenPosting("target", "Target.cs", 1, 1, "symbol", "symbol-name", "Tests", "doc") });
    }

    private static void ForceFullCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
