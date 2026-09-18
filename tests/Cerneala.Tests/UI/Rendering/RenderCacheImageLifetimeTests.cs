using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Rendering;

public sealed class RenderCacheImageLifetimeTests
{
    [Fact]
    public void StandaloneElementCacheOwnsImagesUntilRebuiltOrDisposed()
    {
        using ImageResourceCache images = new(new Loader());
        UIRoot root = new(20, 20);
        root.SetImageResourceCache(null, images);
        Image element = AddImage(root, "atlas.png");
        ElementRenderCache cache = new();
        cache.Ensure(element, new RenderCounters());
        TestImage image = Assert.IsType<TestImage>(cache.Commands[0].Image);
        root.VisualChildren.Remove(element);
        cache.Invalidate();
        Assert.Equal(0, image.DisposeCount);
        Assert.Single(cache.Commands);

        IDisposable lifetime = Assert.IsAssignableFrom<IDisposable>(cache);
        lifetime.Dispose();
        lifetime.Dispose();
        Assert.Equal(1, image.DisposeCount);
        Assert.Empty(cache.Commands);
        Assert.True(cache.IsStale(element));
        Assert.Throws<ObjectDisposedException>(() => cache.Ensure(element, new RenderCounters()));
    }

    [Fact]
    public void RetainedCacheDisposalReleasesLocalAndRootCommands()
    {
        using ImageResourceCache images = new(new Loader());
        UIRoot root = new(20, 20);
        root.SetImageResourceCache(null, images);
        Image element = AddImage(root, "atlas.png");
        RetainedRenderCache cache = new();
        ElementRenderCache local = cache.GetElementCache(element);
        local.Ensure(element, new RenderCounters());
        TestImage image = Assert.IsType<TestImage>(local.Commands[0].Image);
        cache.RootCommands.Add(local.Commands[0]);
        cache.RetainRootResources(images);
        cache.MarkRootBuilt();
        root.VisualChildren.Remove(element);
        Assert.Equal(0, image.DisposeCount);

        IDisposable lifetime = Assert.IsAssignableFrom<IDisposable>(cache);
        lifetime.Dispose();
        lifetime.Dispose();
        Assert.Equal(1, image.DisposeCount);
        Assert.Empty(local.Commands);
        Assert.Empty(cache.RootCommands);
        Assert.False(cache.IsRootValid);
        Assert.Throws<ObjectDisposedException>(() => cache.GetElementCache(element));
        Assert.Throws<ObjectDisposedException>(() => cache.MarkRootBuilt());
        Assert.Throws<ObjectDisposedException>(() => local.Ensure(element, new RenderCounters()));
    }

    [Fact]
    public void RetainedCacheDoesNotLoseItsReleaseResponsibilityWhenAnElementIsAbandoned()
    {
        using ImageResourceCache images = new(new Loader());
        RetainedRenderCache cache = new();
        TestImage image = PopulateAbandonedElement(cache, images);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.Equal(0, image.DisposeCount);
        Assert.IsAssignableFrom<IDisposable>(cache).Dispose();
        Assert.Equal(1, image.DisposeCount);
        Assert.Equal(0, images.ResidentCount);
    }

    [Fact]
    public void RetainedCacheReleasesEveryImageEvenWhenOneReleaseThrows()
    {
        using ImageResourceCache images = new(new Loader(throwOnDispose: true));
        UIRoot root = new(20, 20);
        root.SetImageResourceCache(null, images);
        RetainedRenderCache cache = new();
        List<TestImage> loaded = new();
        foreach (string path in new[] { "first.png", "second.png" })
        {
            Image element = AddImage(root, path);
            ElementRenderCache local = cache.GetElementCache(element);
            local.Ensure(element, new RenderCounters());
            loaded.Add(Assert.IsType<TestImage>(local.Commands[0].Image));
            root.VisualChildren.Remove(element);
        }
        IDisposable lifetime = Assert.IsAssignableFrom<IDisposable>(cache);
        AggregateException error = Assert.Throws<AggregateException>(() => lifetime.Dispose());
        Assert.Equal(2, error.Flatten().InnerExceptions.Count);
        Assert.All(loaded, image => Assert.Equal(1, image.DisposeCount));
        Assert.Equal(0, images.ResidentCount);
        lifetime.Dispose();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static TestImage PopulateAbandonedElement(RetainedRenderCache cache, ImageResourceCache images)
    {
        UIRoot root = new(20, 20);
        root.SetImageResourceCache(null, images);
        Image element = AddImage(root, "atlas.png");
        ElementRenderCache local = cache.GetElementCache(element);
        local.Ensure(element, new RenderCounters());
        TestImage image = Assert.IsType<TestImage>(local.Commands[0].Image);
        root.VisualChildren.Remove(element);
        return image;
    }

    private static Image AddImage(UIRoot root, string path)
    {
        ResourceId<ImageResource> id = new(path);
        root.Resources.SetResource(id, new ImageResource(path));
        Image element = new() { SourceResourceId = id };
        root.VisualChildren.Add(element);
        root.ProcessFrame();
        return element;
    }

    private sealed class Loader(bool throwOnDispose = false) : IAsyncImageLoader
    {
        public IDrawImage Load(string path) => new TestImage(throwOnDispose);
        public ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new(Load(path));
        }
    }

    private sealed class TestImage(bool throwOnDispose) : IDrawImage, IDisposable
    {
        public int Width => 16;
        public int Height => 8;
        public int DisposeCount { get; private set; }
        public void Dispose()
        {
            DisposeCount++;
            if (throwOnDispose) { throw new IOException("release failed"); }
        }
    }
}
