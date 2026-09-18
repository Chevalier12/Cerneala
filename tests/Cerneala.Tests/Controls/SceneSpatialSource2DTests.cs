using Cerneala.Drawing;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Controls;

public sealed class SceneSpatialSource2DTests
{
    [Fact]
    public void CatalogDoesNotLoadPayloadsAndDoesNotRetainTheCallersList()
    {
        int loads = 0;
        List<SceneSpatialEntry2D> entries = [Entry("a"), Entry("b")];
        SceneSpatialSource2D<object> source = new(entries, (entry, _) =>
        {
            loads++;
            return ValueTask.FromResult(new SceneSpatialLease2D<object>(new object()));
        });

        entries.Clear();

        Assert.Equal(["a", "b"], source.Entries.Select(entry => entry.Id));
        Assert.Equal(0, loads);
        Assert.True(((ICollection<SceneSpatialEntry2D>)source.Entries).IsReadOnly);
    }

    [Fact]
    public async Task AcquisitionLoadsOnlyTheRequestedEntryAndReleasesExactlyOnce()
    {
        List<string> loaded = [];
        object value = new();
        int releases = 0;
        SceneSpatialSource2D<object> source = new([Entry("a"), Entry("b")], (entry, _) =>
        {
            loaded.Add(entry.Id);
            return ValueTask.FromResult(new SceneSpatialLease2D<object>(value, released =>
            {
                Assert.Same(value, released);
                releases++;
            }));
        });

        SceneSpatialLease2D<object> lease = await source.LoadAsync(source.Entries[1]);
        Assert.Same(value, lease.Value);
        Assert.Equal(["b"], loaded);
        Assert.Equal(0, releases);
        lease.Dispose();
        lease.Dispose();

        Assert.Equal(1, releases);
        Assert.Throws<ObjectDisposedException>(() => lease.Value);
    }

    [Fact]
    public void ConcurrentDisposalCannotLoseOrDuplicateTheReleaseCallback()
    {
        int releases = 0;
        SceneSpatialLease2D<object> lease = new(new(), _ => Interlocked.Increment(ref releases));

        Parallel.For(0, 64, _ => lease.Dispose());

        Assert.Equal(1, releases);
    }

    [Fact]
    public void ThrowingReleaseDoesNotPermitASecondRelease()
    {
        int releases = 0;
        SceneSpatialLease2D<object> lease = new(new(), _ =>
        {
            releases++;
            throw new InvalidOperationException("release failed");
        });
        Assert.Throws<InvalidOperationException>(() => lease.Dispose());
        lease.Dispose();
        Assert.Equal(1, releases);
        Assert.Throws<ObjectDisposedException>(() => lease.Value);
    }

    [Fact]
    public void InvalidCatalogPublicationIsAtomic()
    {
        SceneSpatialSource2D<object> source = Source([Entry("a")]);
        IReadOnlyList<SceneSpatialEntry2D> previous = source.Entries;
        int changes = 0;
        source.Changed += (_, _) => changes++;

        Assert.Throws<ArgumentException>(() => source.SetEntries([Entry("b"), Entry("b")]));

        Assert.Same(previous, source.Entries);
        Assert.Equal(0, changes);
        source.SetEntries([Entry("b")]);
        Assert.Equal(["a"], previous.Select(entry => entry.Id));
        Assert.Equal(["b"], source.Entries.Select(entry => entry.Id));
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task SourceReplacementDoesNotRevokeAnAlreadyAcquiredPayload()
    {
        object value = new();
        SceneSpatialSource2D<object> source = new([Entry("a")], (_, _) =>
            ValueTask.FromResult(new SceneSpatialLease2D<object>(value)));
        using SceneSpatialLease2D<object> acquired = await source.LoadAsync(source.Entries[0]);
        source.SetEntries([]);
        Assert.Same(value, acquired.Value);
    }

    [Fact]
    public void ObsoleteRevisionAndCancelledRequestsDoNotInvokeTheLoader()
    {
        int calls = 0;
        SceneSpatialEntry2D old = Entry("a");
        SceneSpatialSource2D<object> source = new([old], (_, _) =>
        {
            calls++;
            return ValueTask.FromResult(new SceneSpatialLease2D<object>(new()));
        });
        source.SetEntries([Entry("a", version: 2)]);
        Assert.Throws<InvalidOperationException>(() => source.LoadAsync(old));
        Assert.Throws<InvalidOperationException>(() => source.LoadAsync(Entry("missing")));
        Assert.Throws<OperationCanceledException>(() => source.LoadAsync(source.Entries[0], new CancellationToken(true)));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task AsyncCompletionAndFailureAreNotConvertedIntoEmptyPayloads()
    {
        TaskCompletionSource<SceneSpatialLease2D<object>> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        SceneSpatialSource2D<object> source = new([Entry("a")], (_, _) => new(completion.Task));
        Task<SceneSpatialLease2D<object>> load = source.LoadAsync(source.Entries[0]).AsTask();
        Assert.False(load.IsCompleted);
        completion.SetException(new IOException("read failed"));
        IOException error = await Assert.ThrowsAsync<IOException>(() => load);
        Assert.Equal("read failed", error.Message);
    }

    [Theory]
    [InlineData(float.NaN, 0, 1, 1)]
    [InlineData(0, float.PositiveInfinity, 1, 1)]
    [InlineData(0, 0, -1, 1)]
    [InlineData(0, 0, 1, -1)]
    [InlineData(float.MaxValue, 0, float.MaxValue, 1)]
    public void SpatialBoundsRejectUnknownOrOverflowingExtents(float x, float y, float width, float height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SceneSpatialEntry2D("a", new DrawRect(x, y, width, height)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SceneSpatialEntry2D("a", default,
            new DrawRect(x, y, width, height)));
    }

    [Fact]
    public void CollisionMetadataDefaultsToVisualBoundsButCanBeIndependentOrAbsent()
    {
        DrawRect visual = new(-100, 20, 16, 16);
        DrawRect collision = new(500, 40, 0, 0);
        Assert.Equal(visual, new SceneSpatialEntry2D("a", visual).CollisionBounds);
        Assert.Equal(collision, new SceneSpatialEntry2D("a", visual, collision).CollisionBounds);
        Assert.Null(new SceneSpatialEntry2D("a", visual, collisionBounds: null).CollisionBounds);
    }

    [Fact]
    public void EntryPreservesIdentitySimulationAndVersionWithoutRequiringAnImage()
    {
        SceneSpatialEntry2D entry = new("npc", new DrawRect(-100, 20, 0, 0), isSimulated: true, version: 3);
        Assert.Equal("npc", entry.Id);
        Assert.Equal(new DrawRect(-100, 20, 0, 0), entry.Bounds);
        Assert.True(entry.IsSimulated);
        Assert.Equal(3, entry.Version);
        Assert.Throws<ArgumentException>(() => new SceneSpatialEntry2D(" ", default));
        Assert.Throws<ArgumentOutOfRangeException>(() => Entry("a", 0));
    }

    private static SceneSpatialEntry2D Entry(string id, long version = 1) =>
        new(id, new DrawRect(0, 0, 16, 16), version: version);

    private static SceneSpatialSource2D<object> Source(IEnumerable<SceneSpatialEntry2D> entries) =>
        new(entries, (_, _) => ValueTask.FromResult(new SceneSpatialLease2D<object>(new())));
}
