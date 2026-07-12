using RoslynRepoIndexer.Core;

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

}
