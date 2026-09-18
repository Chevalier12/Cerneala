using Cerneala.Drawing;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Resources;

public sealed class ImageResourceAsyncLeaseTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task CancellingOneWaiterDoesNotCancelTheSharedLoad()
    {
        TaskCompletionSource<IDrawImage> completion = NewCompletion();
        CancellationToken shared = default;
        AsyncLoader loader = new((path, token) =>
        {
            Assert.Equal("atlas.png", path);
            shared = token;
            return new(completion.Task);
        });
        await using ImageResourceCache cache = new(loader);
        using CancellationTokenSource cancellation = new();
        ImageResource resource = new("atlas.png");
        Task<ImageResourceLease> first = cache.AcquireAsync(resource, cancellation.Token).AsTask();
        Task<ImageResourceLease> second = cache.AcquireAsync(resource).AsTask();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first.WaitAsync(Timeout));
        Assert.False(shared.IsCancellationRequested);
        TestImage image = new();
        completion.SetResult(image);
        using ImageResourceLease lease = await second.WaitAsync(Timeout);
        Assert.Same(image, lease.Image);
        Assert.Equal(1, loader.Calls);
        Assert.Equal(1, cache.LoadCount);
        lease.Dispose();
        Assert.Equal(1, image.DisposeCount);
    }

    [Fact]
    public async Task AbandonedCompletionDoesNotReplaceANewAcquisitionOfTheSameIdentity()
    {
        TaskCompletionSource<IDrawImage> oldCompletion = NewCompletion();
        TaskCompletionSource<IDrawImage> newCompletion = NewCompletion();
        CancellationToken oldToken = default;
        int attempt = 0;
        AsyncLoader loader = new((_, token) =>
        {
            if (++attempt == 1) { oldToken = token; return new(oldCompletion.Task); }
            return new(newCompletion.Task);
        });
        await using ImageResourceCache cache = new(loader);
        ImageResource resource = new("atlas.png");
        using CancellationTokenSource cancellation = new();
        Task<ImageResourceLease> abandoned = cache.AcquireAsync(resource, cancellation.Token).AsTask();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => abandoned.WaitAsync(Timeout));
        Assert.True(oldToken.IsCancellationRequested);
        Task<ImageResourceLease> replacement = cache.AcquireAsync(resource).AsTask();
        TestImage oldImage = new();
        TestImage newImage = new();
        newCompletion.SetResult(newImage);
        using ImageResourceLease current = await replacement.WaitAsync(Timeout);
        oldCompletion.SetResult(oldImage);
        await oldImage.Disposed.Task.WaitAsync(Timeout);
        using ImageResourceLease same = await cache.AcquireAsync(resource);
        Assert.Same(newImage, same.Image);
        Assert.Equal(1, oldImage.DisposeCount);
        Assert.Equal(0, newImage.DisposeCount);
        Assert.Equal(1, cache.ResidentCount);
        Assert.Equal(2, loader.Calls);
    }

    [Fact]
    public async Task AsyncConcurrencyIsBoundedAndCancelledQueuedRequestsDoNotLoad()
    {
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int active = 0;
        int maximum = 0;
        object gate = new();
        AsyncLoader loader = new(async (_, _) =>
        {
            lock (gate) { maximum = Math.Max(maximum, ++active); }
            try { await release.Task; return new TestImage(); }
            finally { lock (gate) { active--; } }
        });
        await using ImageResourceCache cache = new(loader, maximumConcurrentLoads: 2);
        Task<ImageResourceLease> first = cache.AcquireAsync(new("first.png")).AsTask();
        Task<ImageResourceLease> second = cache.AcquireAsync(new("second.png")).AsTask();
        using CancellationTokenSource cancellation = new();
        Task<ImageResourceLease> cancelled = cache.AcquireAsync(new("cancelled.png"), cancellation.Token).AsTask();
        Task<ImageResourceLease> fourth = cache.AcquireAsync(new("fourth.png")).AsTask();
        Task<ImageResourceLease> fifth = cache.AcquireAsync(new("fifth.png")).AsTask();
        Assert.Equal(2, loader.Calls);
        Assert.Equal(5, cache.PendingLoadCount);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled.WaitAsync(Timeout));
        release.SetResult();
        ImageResourceLease[] leases = await Task.WhenAll(first, second, fourth, fifth).WaitAsync(Timeout);
        Assert.Equal(2, maximum);
        Assert.Equal(4, loader.Calls);
        foreach (ImageResourceLease lease in leases) { lease.Dispose(); }
        await cache.DisposeAsync();
        Assert.Equal(0, cache.PendingLoadCount);
        Assert.Equal(0, cache.ResidentCount);
    }

    [Fact]
    public async Task AsyncDisposalDrainsLoadingWithoutRevokingItsAcquisition()
    {
        TaskCompletionSource<IDrawImage> completion = NewCompletion();
        ImageResourceCache cache = new(new AsyncLoader((_, _) => new(completion.Task)));
        Task<ImageResourceLease> acquisition = cache.AcquireAsync(new("atlas.png")).AsTask();
        Task disposal = cache.DisposeAsync().AsTask();
        Assert.False(disposal.IsCompleted);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => cache.AcquireAsync(new("other.png")).AsTask());
        TestImage image = new();
        completion.SetResult(image);
        using ImageResourceLease lease = await acquisition.WaitAsync(Timeout);
        await disposal.WaitAsync(Timeout);
        using ImageResourceLease retained = lease.Retain();
        lease.Dispose();
        Assert.Equal(0, image.DisposeCount);
        retained.Dispose();
        Assert.Equal(1, image.DisposeCount);
        await cache.DisposeAsync();
    }

    [Fact]
    public async Task AsyncDisposalReportsAnAbandonedResultsReleaseFailure()
    {
        TaskCompletionSource<IDrawImage> completion = NewCompletion();
        ImageResourceCache cache = new(new AsyncLoader((_, _) => new(completion.Task)));
        using CancellationTokenSource cancellation = new();
        Task<ImageResourceLease> acquisition = cache.AcquireAsync(new("atlas.png"), cancellation.Token).AsTask();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => acquisition.WaitAsync(Timeout));
        Task disposal = cache.DisposeAsync().AsTask();
        TestImage image = new(throwOnDispose: true);
        completion.SetResult(image);
        AggregateException error = await Assert.ThrowsAsync<AggregateException>(() => disposal.WaitAsync(Timeout));
        Assert.IsType<IOException>(Assert.Single(error.InnerExceptions));
        Assert.Equal(1, image.DisposeCount);
        Assert.Equal(0, cache.PendingLoadCount);
        Assert.Equal(0, cache.ResidentCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedOrNullAsyncResultsCanBeRetried(bool nullResult)
    {
        int attempt = 0;
        AsyncLoader loader = new((_, _) => ++attempt == 1
            ? nullResult ? new((IDrawImage)null!) : ValueTask.FromException<IDrawImage>(new IOException("decode failed"))
            : new(new TestImage()));
        await using ImageResourceCache cache = new(loader);
        if (nullResult)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => cache.AcquireAsync(new("atlas.png")).AsTask());
        }
        else
        {
            await Assert.ThrowsAsync<IOException>(() => cache.AcquireAsync(new("atlas.png")).AsTask());
        }
        Assert.Equal(0, cache.ResidentCount);
        using ImageResourceLease lease = await cache.AcquireAsync(new("atlas.png"));
        Assert.Equal(2, loader.Calls);
        Assert.Equal(1, cache.LoadCount);
    }

    [Fact]
    public async Task SynchronousAcquisitionCanJoinAnAsyncLoad()
    {
        TaskCompletionSource<IDrawImage> completion = NewCompletion();
        AsyncLoader loader = new((_, _) => new(completion.Task));
        await using ImageResourceCache cache = new(loader);
        ImageResource resource = new("atlas.png");
        Task<ImageResourceLease> asynchronous = cache.AcquireAsync(resource).AsTask();
        Task<ImageResourceLease> synchronous = Task.Run(() => cache.Acquire(resource));
        completion.SetResult(new TestImage());
        using ImageResourceLease first = await asynchronous.WaitAsync(Timeout);
        using ImageResourceLease second = await synchronous.WaitAsync(Timeout);
        Assert.Same(first.Image, second.Image);
        Assert.Equal(1, loader.Calls);
    }

    [Fact]
    public async Task PathOnlyLoaderIsNotSilentlyMovedToAWorker()
    {
        SyncLoader loader = new();
        await using ImageResourceCache cache = new(loader);
        ImageResource resource = new("atlas.png");
        await Assert.ThrowsAsync<NotSupportedException>(() => cache.AcquireAsync(resource).AsTask());
        Assert.Equal(0, loader.Calls);
        using ImageResourceLease synchronous = cache.Acquire(resource);
        using ImageResourceLease asynchronous = await cache.AcquireAsync(resource);
        Assert.Same(synchronous.Image, asynchronous.Image);
        Assert.Equal(1, loader.Calls);
    }

    [Fact]
    public async Task EmbeddedAsyncAcquisitionIsBorrowedAndHonoursPreCancellation()
    {
        TestImage image = new();
        await using ImageResourceCache cache = new(null);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.AcquireAsync(new(image), cancellation.Token).AsTask());
        using ImageResourceLease lease = await cache.AcquireAsync(new(image));
        lease.Dispose();
        Assert.Equal(0, image.DisposeCount);
        Assert.Equal(0, cache.LoadCount);
    }

    [Fact]
    public async Task RecursiveAsyncLoadingFailsInsteadOfAwaitingItself()
    {
        ImageResourceCache? cache = null;
        AsyncLoader loader = new(async (path, _) =>
        {
            await Task.Yield();
            using ImageResourceLease lease = await cache!.AcquireAsync(new(path));
            return lease.Image;
        });
        await using (cache = new(loader))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => cache.AcquireAsync(new("atlas.png")).AsTask().WaitAsync(Timeout));
            Assert.Equal(0, cache.ResidentCount);
        }
    }

    [Fact]
    public async Task CancellationAndCompletionRaceReleasesExactlyOnceFor256Iterations()
    {
        for (int iteration = 0; iteration < 256; iteration++)
        {
            TaskCompletionSource<IDrawImage> completion = NewCompletion();
            await using ImageResourceCache cache = new(new AsyncLoader((_, _) => new(completion.Task)));
            using CancellationTokenSource cancellation = new();
            Task<ImageResourceLease> acquisition = cache.AcquireAsync(new("atlas.png"), cancellation.Token).AsTask();
            TestImage image = new();
            await Task.WhenAll(Task.Run(() => cancellation.Cancel()), Task.Run(() => completion.SetResult(image))).WaitAsync(Timeout);
            try { (await acquisition.WaitAsync(Timeout)).Dispose(); }
            catch (OperationCanceledException) { }
            await cache.DisposeAsync();
            Assert.Equal(1, image.DisposeCount);
            Assert.Equal(0, cache.ResidentCount);
            Assert.Equal(0, cache.PendingLoadCount);
        }
    }

    private static TaskCompletionSource<IDrawImage> NewCompletion() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class AsyncLoader(Func<string, CancellationToken, ValueTask<IDrawImage>> load) : IAsyncImageLoader
    {
        private int calls;
        public int Calls => Volatile.Read(ref calls);
        public IDrawImage Load(string path) => throw new InvalidOperationException("Unexpected synchronous decode.");
        public ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref calls);
            return load(path, cancellationToken);
        }
    }

    private sealed class SyncLoader : IImageLoader
    {
        public int Calls { get; private set; }
        public IDrawImage Load(string path) { Calls++; return new TestImage(); }
    }

    private sealed class TestImage(bool throwOnDispose = false) : IDrawImage, IDisposable
    {
        private int disposeCount;
        public int Width => 16;
        public int Height => 8;
        public int DisposeCount => Volatile.Read(ref disposeCount);
        public TaskCompletionSource Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Dispose()
        {
            Interlocked.Increment(ref disposeCount);
            Disposed.TrySetResult();
            if (throwOnDispose) { throw new IOException("release failed"); }
        }
    }
}
