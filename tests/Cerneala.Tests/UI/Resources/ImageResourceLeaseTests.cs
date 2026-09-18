using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Resources;

public sealed class ImageResourceLeaseTests
{
    [Fact]
    public void LastReleaseUnloadsWithoutClearingTheCache()
    {
        Loader loader = new();
        using ImageResourceCache cache = new(loader);
        ImageResource resource = new("atlas.png");
        ImageResourceLease first = cache.Acquire(resource);
        ImageResourceLease second = cache.Acquire(resource);
        ImageResourceLease command = first.Retain();
        TestImage image = Assert.IsType<TestImage>(first.Image);
        Assert.Same(image, second.Image);
        Assert.Equal(1, cache.LoadCount);
        first.Dispose();
        second.Dispose();
        Assert.Equal(0, image.DisposeCount);
        command.Dispose();
        command.Dispose();
        Assert.Equal(1, image.DisposeCount);
        Assert.Equal(0, cache.ResidentCount);
        Assert.Throws<ObjectDisposedException>(() => command.Image);
        Assert.Throws<ObjectDisposedException>(() => command.Retain());
        using ImageResourceLease reloaded = cache.Acquire(resource);
        Assert.NotSame(image, reloaded.Image);
        Assert.Equal(2, cache.LoadCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ForgettingAnIdentityDoesNotInvalidateOldOrReplacementAcquisitions(bool clearAll)
    {
        using ImageResourceCache cache = new(new Loader());
        ImageResource resource = new("atlas.png");
        ImageResourceLease old = cache.Acquire(resource);
        TestImage oldImage = Assert.IsType<TestImage>(old.Image);
        if (clearAll) { cache.Clear(); } else { cache.Remove(resource); }
        using ImageResourceLease replacement = cache.Acquire(resource);
        using ImageResourceLease retainedOld = old.Retain();
        old.Dispose();
        Assert.Equal(0, oldImage.DisposeCount);
        retainedOld.Dispose();
        Assert.Equal(1, oldImage.DisposeCount);
        using ImageResourceLease replacementAgain = cache.Acquire(resource);
        Assert.Same(replacement.Image, replacementAgain.Image);
        Assert.Equal(2, cache.LoadCount);
        Assert.Equal(1, cache.ResidentCount);
    }

    [Fact]
    public void CacheDisposalClosesAcquisitionButDoesNotRevokeExistingLeases()
    {
        ImageResourceCache cache = new(new Loader());
        ImageResourceLease lease = cache.Acquire(new ImageResource("atlas.png"));
        TestImage image = Assert.IsType<TestImage>(lease.Image);
        cache.Dispose();
        cache.Dispose();
        Assert.Throws<ObjectDisposedException>(() => cache.Acquire(new ImageResource("atlas.png")));
        Assert.Throws<ObjectDisposedException>(() => cache.Acquire(new ImageResource(image)));
        using ImageResourceLease retained = lease.Retain();
        lease.Dispose();
        Assert.Equal(0, image.DisposeCount);
        retained.Dispose();
        Assert.Equal(1, image.DisposeCount);
        Assert.Equal(0, cache.ResidentCount);
    }

    [Fact]
    public void EmbeddedImageAcquisitionsNeverOwnTheSuppliedImage()
    {
        TestImage image = new();
        using ImageResourceCache cache = new(null);
        using ImageResourceLease lease = cache.Acquire(new ImageResource(image));
        using ImageResourceLease retained = lease.Retain();
        lease.Dispose();
        retained.Dispose();
        cache.Dispose();
        Assert.Equal(0, image.DisposeCount);
        Assert.Equal(0, cache.LoadCount);
    }

    [Fact]
    public void LoadFailureDoesNotPoisonAnIdentity()
    {
        int attempts = 0;
        using ImageResourceCache cache = new(new Loader(() => ++attempts == 1
            ? throw new IOException("decode failed") : new TestImage()));
        ImageResource resource = new("atlas.png");
        Assert.Throws<IOException>(() => cache.Acquire(resource));
        Assert.Equal(0, cache.ResidentCount);
        using ImageResourceLease lease = cache.Acquire(resource);
        Assert.Equal(2, attempts);
        Assert.Equal(1, cache.LoadCount);
    }

    [Fact]
    public void ThrowingDisposalStillRemovesTheEntryAndCannotBeRepeated()
    {
        using ImageResourceCache cache = new(new Loader(() => new TestImage(throwOnDispose: true)));
        ImageResourceLease lease = cache.Acquire(new ImageResource("atlas.png"));
        TestImage image = Assert.IsType<TestImage>(lease.Image);
        Assert.Throws<IOException>(() => lease.Dispose());
        lease.Dispose();
        Assert.Equal(1, image.DisposeCount);
        Assert.Equal(0, cache.ResidentCount);
        Assert.Throws<ObjectDisposedException>(() => lease.Image);
    }

    [Fact]
    public async Task ConcurrentAcquisitionSharesOneLoadAndLastDisposalRunsOnce()
    {
        using ManualResetEventSlim entered = new();
        using ManualResetEventSlim finish = new();
        using ImageResourceCache cache = new(new Loader(() =>
        {
            entered.Set();
            if (!finish.Wait(TimeSpan.FromSeconds(10))) { throw new TimeoutException(); }
            return new TestImage();
        }));
        ImageResource resource = new("atlas.png");
        Task<ImageResourceLease> first = Task.Run(() => cache.Acquire(resource));
        Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
        Task<ImageResourceLease>[] others = Enumerable.Range(0, 15)
            .Select(_ => Task.Run(() => cache.Acquire(resource))).ToArray();
        finish.Set();
        ImageResourceLease[] leases = await Task.WhenAll(others.Prepend(first)).WaitAsync(TimeSpan.FromSeconds(10));
        TestImage image = Assert.IsType<TestImage>(leases[0].Image);
        Assert.All(leases, lease => Assert.Same(image, lease.Image));
        Assert.Equal(1, cache.LoadCount);
        await Task.WhenAll(leases.Select(lease => Task.Run(() => { lease.Dispose(); lease.Dispose(); })));
        Assert.Equal(1, image.DisposeCount);
        Assert.Equal(0, cache.ResidentCount);
    }

    [Fact]
    public void ReplacingOneRootsCacheDoesNotEvictAnotherRootsSharedAtlas()
    {
        Loader loader = new();
        using ImageResourceCache cache = new(loader);
        ResourceId<ImageResource> id = new("Atlas");
        ResourceStore resources = new();
        resources.SetResource(id, new ImageResource("atlas.png"));
        UIRoot first = CreateRoot();
        UIRoot second = CreateRoot();
        Commit(first);
        Commit(second);
        TestImage image = Assert.IsType<TestImage>(first.RetainedRenderCache.RootCommands[0].Image);
        Assert.Same(image, second.RetainedRenderCache.RootCommands[0].Image);
        first.SetImageLoader(null);
        Assert.Equal(0, image.DisposeCount);
        using (ImageResourceLease current = cache.Acquire(new ImageResource("atlas.png")))
        {
            Assert.Same(image, current.Image);
        }
        Assert.Equal(1, cache.LoadCount);
        second.SetImageLoader(null);
        Assert.Equal(1, image.DisposeCount);
        Assert.Equal(0, cache.ResidentCount);

        UIRoot CreateRoot()
        {
            UIRoot root = new(20, 20);
            root.SetImageResourceCache(loader, cache);
            root.SetResourceProvider(resources);
            root.VisualChildren.Add(new Image { SourceResourceId = id });
            return root;
        }
    }

    [Fact]
    public void DetachingAnImageKeepsCommittedCommandsAliveUntilTheNextCommit()
    {
        using ImageResourceCache cache = new(new Loader());
        UIRoot root = new(20, 20);
        root.SetImageResourceCache(null, cache);
        ResourceId<ImageResource> id = new("Atlas");
        root.Resources.SetResource(id, new ImageResource("atlas.png"));
        Image control = new() { SourceResourceId = id };
        root.VisualChildren.Add(control);
        Commit(root);
        TestImage image = Assert.IsType<TestImage>(root.RetainedRenderCache.RootCommands[0].Image);
        root.VisualChildren.Remove(control);
        Assert.Equal(0, image.DisposeCount);
        Commit(root);
        Assert.Equal(1, image.DisposeCount);
        Assert.Empty(root.RetainedRenderCache.RootCommands);
    }

    private static void Commit(UIRoot root)
    {
        root.ProcessFrame();
        root.RetainedRenderer.Commit(root);
    }

    private sealed class Loader(Func<IDrawImage>? load = null) : IAsyncImageLoader
    {
        public IDrawImage Load(string path) => load?.Invoke() ?? new TestImage();
        public ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new(Load(path));
        }
    }

    private sealed class TestImage(bool throwOnDispose = false) : IDrawImage, IDisposable
    {
        private int disposeCount;
        public int Width => 16;
        public int Height => 8;
        public int DisposeCount => Volatile.Read(ref disposeCount);
        public void Dispose()
        {
            Interlocked.Increment(ref disposeCount);
            if (throwOnDispose) { throw new IOException("release failed"); }
        }
    }
}
