using Cerneala.Drawing;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Controls;

public sealed class SceneSpatialResidency2DTests
{
    [Fact]
    public async Task CameraInterestDoesNotLoadRemoteDecorationButSimulationRemainsPinned()
    {
        List<string> loaded = [], released = [];
        SceneSpatialSource2D<object> source = new(
            [Entry("near", 0), Entry("far", 1000), Entry("npc", 2000, simulated: true)],
            (entry, _) =>
            {
                loaded.Add(entry.Id);
                return ValueTask.FromResult(new SceneSpatialLease2D<object>(new(), _ => released.Add(entry.Id)));
            });
        using SceneSpatialResidency2D<object> residency = new(source);
        using SceneSpatialRegion2D<object> first = await residency.AcquireAsync(new DrawRect(-1, -1, 32, 32), includeSimulated: true);
        Assert.Equal(new[] { "near", "npc" }, loaded);
        object npc = first.GetValue("npc");
        using SceneSpatialRegion2D<object> next = await residency.AcquireAsync(new DrawRect(999, -1, 32, 32), includeSimulated: true);
        first.Dispose();
        Assert.Same(npc, next.GetValue("npc"));
        Assert.Equal(new[] { "near" }, released);
        Assert.Equal(2, residency.ResidentCount);
        next.Dispose();
        Assert.Equal(0, residency.ResidentCount);
        Assert.Equal(new[] { "far", "near", "npc" }, released.Order());
    }

    [Fact]
    public async Task OverlappingRequestsShareAnAcquisitionAndCancellationDoesNotCancelTheOtherWaiter()
    {
        TaskCompletionSource<SceneSpatialLease2D<object>> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int loads = 0, releases = 0;
        CancellationToken loaderToken = default;
        SceneSpatialSource2D<object> source = new([Entry("a", 0)], (_, token) =>
        {
            loaderToken = token;
            loads++;
            return new(completion.Task);
        });
        using SceneSpatialResidency2D<object> residency = new(source);
        using CancellationTokenSource cancellation = new();
        Task<SceneSpatialRegion2D<object>> first = residency.AcquireAsync(Bounds, cancellationToken: cancellation.Token).AsTask();
        Task<SceneSpatialRegion2D<object>> second = residency.AcquireAsync(Bounds).AsTask();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        Assert.False(loaderToken.IsCancellationRequested);
        completion.SetResult(new(new(), _ => releases++));
        using SceneSpatialRegion2D<object> remaining = await second;
        Assert.Equal(1, loads);
        Assert.Equal(0, releases);
        remaining.Dispose();
        Assert.Equal(1, releases);
    }

    [Fact]
    public async Task IgnoredCancellationCannotPublishItsLatePayloadIntoANewRequest()
    {
        TaskCompletionSource<SceneSpatialLease2D<object>> oldLoad = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        object currentValue = new();
        SceneSpatialSource2D<object> source = new([Entry("a", 0)], (_, _) =>
            ++calls == 1 ? new(oldLoad.Task) : ValueTask.FromResult(new SceneSpatialLease2D<object>(currentValue)));
        using SceneSpatialResidency2D<object> residency = new(source);
        using CancellationTokenSource cancellation = new();
        Task<SceneSpatialRegion2D<object>> cancelled = residency.AcquireAsync(Bounds, cancellationToken: cancellation.Token).AsTask();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
        using SceneSpatialRegion2D<object> current = await residency.AcquireAsync(Bounds);
        oldLoad.SetResult(new(new(), _ => released.SetResult()));
        await released.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Same(currentValue, current.GetValue("a"));
        Assert.Equal(1, residency.ResidentCount);
    }

    [Fact]
    public async Task FailedRegionReleasesSuccessfulPartsAndCanBeRetried()
    {
        int releases = 0, failures = 0;
        SceneSpatialSource2D<object> source = new([Entry("a", 0), Entry("b", 1)], (entry, _) =>
        {
            if (entry.Id == "b" && failures++ == 0) { throw new IOException("missing chunk"); }
            return ValueTask.FromResult(new SceneSpatialLease2D<object>(new(), _ => releases++));
        });
        using SceneSpatialResidency2D<object> residency = new(source);
        await Assert.ThrowsAsync<IOException>(() => residency.AcquireAsync(Bounds).AsTask());
        Assert.Equal(1, releases);
        Assert.Equal(0, residency.ResidentCount);
        using SceneSpatialRegion2D<object> retry = await residency.AcquireAsync(Bounds);
        Assert.Equal(2, retry.Entries.Count);
    }

    [Fact]
    public async Task RevisionChangesProduceANewSnapshotWithoutRevokingTheOldPin()
    {
        SceneSpatialSource2D<object> source = new([Entry("a", 0)], (_, _) =>
            ValueTask.FromResult(new SceneSpatialLease2D<object>(new())));
        using SceneSpatialResidency2D<object> residency = new(source);
        using SceneSpatialRegion2D<object> first = await residency.AcquireAsync(Bounds);
        object firstValue = first.GetValue("a");
        source.SetEntries([Entry("a", 0, version: 2)]);
        using SceneSpatialRegion2D<object> second = await residency.AcquireAsync(Bounds);
        Assert.False(first.IsCurrent);
        Assert.True(second.IsCurrent);
        Assert.Same(firstValue, first.GetValue("a"));
        Assert.NotSame(firstValue, second.GetValue("a"));
        first.Dispose();
        Assert.Throws<ObjectDisposedException>(() => first.GetValue("a"));
        Assert.Equal(1, residency.ResidentCount);
    }

    [Fact]
    public async Task RegionWithoutCameraCanAcquireOnlyTheSimulationSet()
    {
        using SceneSpatialResidency2D<object> residency = new(new SceneSpatialSource2D<object>(
            [Entry("decoration", 0), Entry("npc", 1000, simulated: true)],
            (_, _) => ValueTask.FromResult(new SceneSpatialLease2D<object>(new()))));
        using SceneSpatialRegion2D<object> empty = await residency.AcquireAsync(null);
        using SceneSpatialRegion2D<object> simulation = await residency.AcquireAsync(null, includeSimulated: true);
        Assert.Empty(empty.Entries);
        Assert.Equal("npc", Assert.Single(simulation.Entries).Id);
    }

    [Fact]
    public async Task DisposalDoesNotWaitForUncooperativeIoButReleasesItsEventualPayload()
    {
        TaskCompletionSource<SceneSpatialLease2D<object>> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        SceneSpatialResidency2D<object> residency = new(new SceneSpatialSource2D<object>(
            [Entry("a", 0)], (_, _) => new(completion.Task)));
        Task<SceneSpatialRegion2D<object>> request = residency.AcquireAsync(Bounds).AsTask();
        residency.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => request);
        completion.SetResult(new(new(), _ => released.SetResult()));
        await released.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(0, residency.ResidentCount);
    }

    [Fact]
    public async Task DisposingTheOwnerInvalidatesValuesInOutstandingRegions()
    {
        SceneSpatialResidency2D<object> residency = new(new SceneSpatialSource2D<object>(
            [Entry("a", 0)], (_, _) => ValueTask.FromResult(new SceneSpatialLease2D<object>(new()))));
        using SceneSpatialRegion2D<object> region = await residency.AcquireAsync(Bounds);
        residency.Dispose();
        Assert.Throws<ObjectDisposedException>(() => region.GetValue("a"));
        Assert.False(region.IsCurrent);
    }

    [Fact]
    public async Task AsyncDisposalObservesFailuresFromLatePayloadRelease()
    {
        TaskCompletionSource<SceneSpatialLease2D<object>> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        SceneSpatialResidency2D<object> residency = new(new SceneSpatialSource2D<object>(
            [Entry("a", 0)], (_, _) => new(completion.Task)));
        Task<SceneSpatialRegion2D<object>> request = residency.AcquireAsync(Bounds).AsTask();
        Task disposal = residency.DisposeAsync().AsTask();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => request);
        Assert.False(disposal.IsCompleted);
        completion.SetResult(new(new(), _ => throw new IOException("late release")));
        AggregateException failure = await Assert.ThrowsAsync<AggregateException>(() => disposal);
        Assert.Equal("late release", Assert.IsType<IOException>(Assert.Single(failure.InnerExceptions)).Message);
        Assert.Equal(0, residency.PendingLoadCount);
    }

    [Fact]
    public async Task CancellationCompletionAndDisposalStressHasNoLostOrDuplicatedAcquisitions()
    {
        const int iterations = 256;
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            TaskCompletionSource<SceneSpatialLease2D<object>> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            int releases = 0;
            await using SceneSpatialResidency2D<object> residency = new(new SceneSpatialSource2D<object>(
                [Entry("a", 0)], (_, _) => new(completion.Task)));
            using CancellationTokenSource cancellation = new();
            Task<SceneSpatialRegion2D<object>> request = residency.AcquireAsync(Bounds, cancellationToken: cancellation.Token).AsTask();
            await Task.WhenAll(
                Task.Run(cancellation.Cancel),
                Task.Run(() => completion.SetResult(new(new(), _ => Interlocked.Increment(ref releases)))),
                Task.Run(residency.Dispose));
            try { (await request).Dispose(); }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            await residency.DisposeAsync();
            Assert.Equal(1, releases);
            Assert.Equal(0, residency.ResidentCount);
            Assert.Equal(0, residency.PendingLoadCount);
        }
    }

    [Fact]
    public async Task ThrowingReleaseDoesNotLeakTheOtherEntries()
    {
        int releases = 0;
        using SceneSpatialResidency2D<object> residency = new(new SceneSpatialSource2D<object>(
            [Entry("a", 0), Entry("b", 1)], (_, _) => ValueTask.FromResult(new SceneSpatialLease2D<object>(new(), _ =>
            {
                releases++;
                throw new IOException("release");
            }))));
        SceneSpatialRegion2D<object> region = await residency.AcquireAsync(Bounds);
        AggregateException failure = Assert.Throws<AggregateException>(region.Dispose);
        Assert.Equal(2, failure.InnerExceptions.Count);
        Assert.Equal(2, releases);
        Assert.Equal(0, residency.ResidentCount);
        region.Dispose();
    }

    [Fact]
    public async Task ConcurrentIoIsBoundedWithoutSerializingIndependentRequests()
    {
        TaskCompletionSource unblock = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource full = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int active = 0, maximum = 0, loads = 0;
        using SceneSpatialResidency2D<object> residency = new(new SceneSpatialSource2D<object>(
            Enumerable.Range(0, 20).Select(index => Entry(index.ToString(), index)), async (_, token) =>
            {
                int count = Interlocked.Increment(ref active);
                Interlocked.Increment(ref loads);
                maximum = Math.Max(maximum, count);
                if (count == 2) { full.TrySetResult(); }
                await unblock.Task.WaitAsync(token);
                Interlocked.Decrement(ref active);
                return new(new());
            }), maximumConcurrentLoads: 2);
        Task<SceneSpatialRegion2D<object>> request = residency.AcquireAsync(Bounds).AsTask();
        await full.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, loads);
        unblock.SetResult();
        using SceneSpatialRegion2D<object> region = await request;
        Assert.Equal(20, region.Entries.Count);
        Assert.Equal(2, maximum);
    }

    private static readonly DrawRect Bounds = new(-1, -1, 64, 64);

    [Fact]
    public async Task ExactPreparationDoesNotExpandToOverlappingChunksOrSimulatedEntries()
    {
        List<string> loaded = [], released = [];
        SceneSpatialSource2D<object> source = new(
            [Entry("a", 0), Entry("b", 0), Entry("npc", 0, simulated: true)], (entry, _) =>
            {
                loaded.Add(entry.Id);
                return ValueTask.FromResult(new SceneSpatialLease2D<object>(new(), _ => released.Add(entry.Id)));
            });
        await using SceneSpatialResidency2D<object> residency = new(source);
        IReadOnlyList<SceneSpatialEntry2D> catalog = source.Entries;
        using SceneSpatialRegion2D<object> empty = await residency.AcquireEntriesAsync(catalog, []);
        Assert.Empty(empty.Entries);
        Assert.Empty(loaded);
        using SceneSpatialRegion2D<object> selected = await residency.AcquireEntriesAsync(catalog, [catalog[1]]);
        Assert.Equal("b", Assert.Single(selected.Entries).Id);
        Assert.Equal(["b"], loaded);
        Assert.Equal(1, residency.ResidentCount);
        selected.Dispose();
        Assert.Equal(["b"], released);
        Assert.Equal(0, residency.ResidentCount);
    }

    [Fact]
    public async Task ExactAndRequiredRegionsShareTheExistingResidentAndPreserveCatalogOrder()
    {
        int loads = 0, releases = 0;
        // The loader is synchronous here so sharing is independent of scheduling.
        SceneSpatialSource2D<object> source = new([Entry("a", 0), Entry("b", 1)], (_, _) =>
        {
            loads++;
            return ValueTask.FromResult(new SceneSpatialLease2D<object>(new(), _ => releases++));
        });
        await using SceneSpatialResidency2D<object> residency = new(source);
        IReadOnlyList<SceneSpatialEntry2D> catalog = source.Entries;
        using SceneSpatialRegion2D<object> selected = await residency.AcquireEntriesAsync(catalog, [catalog[1], catalog[0]]);
        using SceneSpatialRegion2D<object> required = await residency.AcquireAsync(Bounds);
        Assert.Equal(["a", "b"], selected.Entries.Select(static entry => entry.Id));
        Assert.Same(selected.GetValue("a"), required.GetValue("a"));
        Assert.Same(selected.GetValue("b"), required.GetValue("b"));
        Assert.Equal(2, loads);
        selected.Dispose();
        Assert.Equal(0, releases);
        required.Dispose();
        Assert.Equal(2, releases);
        Assert.Equal(0, residency.ResidentCount);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("foreign")]
    [InlineData("stale-catalog")]
    [InlineData("null-entry")]
    public async Task InvalidExactSelectionStartsNoPartialAcquisitions(string scenario)
    {
        int loads = 0;
        SceneSpatialSource2D<object> source = new([Entry("a", 0), Entry("b", 1)], (_, _) =>
        {
            loads++;
            return ValueTask.FromResult(new SceneSpatialLease2D<object>(new()));
        });
        await using SceneSpatialResidency2D<object> residency = new(source);
        IReadOnlyList<SceneSpatialEntry2D> catalog = source.Entries;
        IReadOnlyList<SceneSpatialEntry2D> selection = scenario switch
        {
            "duplicate" => [catalog[0], catalog[0]],
            "foreign" => [catalog[0], Entry("b", 1)],
            "null-entry" => [catalog[0], null!],
            _ => [catalog[0]]
        };
        if (scenario == "stale-catalog") { source.SetEntries([Entry("a", 0), Entry("b", 1)]); }
        Exception? failure = await Record.ExceptionAsync(() => residency.AcquireEntriesAsync(catalog, selection).AsTask());
        if (scenario == "stale-catalog") { Assert.IsType<InvalidOperationException>(failure); }
        else { Assert.IsAssignableFrom<ArgumentException>(failure); }
        Assert.Equal(0, loads);
        Assert.Equal(0, residency.ResidentCount);
        Assert.Equal(0, residency.PendingLoadCount);
    }

    [Fact]
    public async Task CancelledExactPreparationCannotCancelTheRequiredRegionSharingItsLoad()
    {
        TaskCompletionSource<SceneSpatialLease2D<object>> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int loads = 0, releases = 0;
        CancellationToken loaderToken = default;
        SceneSpatialSource2D<object> source = new([Entry("a", 0)], (_, token) =>
        {
            loaderToken = token;
            loads++;
            return new(completion.Task.WaitAsync(token));
        });
        await using SceneSpatialResidency2D<object> residency = new(source);
        using CancellationTokenSource cancellation = new();
        Task<SceneSpatialRegion2D<object>> optional = residency.AcquireEntriesAsync(
            source.Entries, source.Entries, cancellation.Token).AsTask();
        Task<SceneSpatialRegion2D<object>> required = residency.AcquireAsync(Bounds).AsTask();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => optional.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.False(loaderToken.IsCancellationRequested);
        completion.SetResult(new(new(), _ => releases++));
        using SceneSpatialRegion2D<object> region = await required.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, loads);
        Assert.Equal(0, releases);
        region.Dispose();
        Assert.Equal(1, releases);
    }

    private static SceneSpatialEntry2D Entry(string id, float x, bool simulated = false, long version = 1) =>
        new(id, new DrawRect(x, 0, 16, 16), simulated, version);
}
