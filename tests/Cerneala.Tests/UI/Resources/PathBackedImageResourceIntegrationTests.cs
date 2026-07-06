using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Resources;

public sealed class PathBackedImageResourceIntegrationTests
{
    [Fact]
    public void ImageControlResolvesPathBackedResourceThroughRootLoader()
    {
        TestImage loaded = new(32, 16);
        RecordingImageLoader loader = new();
        loader.SetImage("logo.png", loaded);
        ResourceId<ImageResource> id = new("Logo");
        ResourceStore store = new();
        store.SetResource(id, new ImageResource("logo.png"));
        UIRoot root = RootWithImageLoader(loader, store);
        Image image = new()
        {
            SourceResourceId = id
        };
        root.VisualChildren.Add(image);

        root.ProcessFrame();
        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Single(commands);
        Assert.Same(loaded, commands[0].Image);
        Assert.Equal(1, loader.GetLoadCount("logo.png"));
    }

    [Fact]
    public void ImageControlPathResourceLoadInvalidatesMeasureWhenIntrinsicSizeIsUsed()
    {
        RecordingImageLoader loader = new();
        loader.SetImage("logo.png", new TestImage(32, 16));
        ResourceId<ImageResource> id = new("Logo");
        ResourceStore store = new();
        store.SetResource(id, new ImageResource("logo.png"));
        UIRoot root = RootWithImageLoader(loader, store);
        Image image = new()
        {
            SourceResourceId = id,
            UseIntrinsicSize = true
        };
        root.VisualChildren.Add(image);

        FrameStats stats = root.ProcessFrame();

        Assert.True(stats.MeasuredElements > 0);
        Assert.Equal(32, image.DesiredSize.Width);
        Assert.Equal(16, image.DesiredSize.Height);
        Assert.Equal(1, loader.GetLoadCount("logo.png"));
    }

    [Fact]
    public void ReplacingPathBackedImageResourceInvalidatesDependentImageRender()
    {
        RecordingImageLoader loader = new();
        TestImage first = new(32, 16);
        TestImage second = new(48, 24);
        loader.SetImage("first.png", first);
        loader.SetImage("second.png", second);
        ResourceId<ImageResource> id = new("Logo");
        ResourceStore store = new();
        store.SetResource(id, new ImageResource("first.png"));
        UIRoot root = RootWithImageLoader(loader, store);
        Image image = new()
        {
            SourceResourceId = id,
            UseIntrinsicSize = false
        };
        root.VisualChildren.Add(image);
        root.ProcessFrame();
        root.RetainedRenderer.Commit(root);
        image.DirtyState.Clear(InvalidationFlags.Measure | InvalidationFlags.Render);

        store.SetResource(id, new ImageResource("second.png"));
        FrameStats stats = root.ProcessFrame();
        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Equal(0, stats.MeasuredElements);
        Assert.True(stats.RenderedElements > 0);
        Assert.Single(commands);
        Assert.Same(second, commands[0].Image);
        Assert.Equal(1, loader.GetLoadCount("first.png"));
        Assert.Equal(1, loader.GetLoadCount("second.png"));
    }

    [Fact]
    public void SecondUnchangedFrameDoesNotReloadPathBackedImage()
    {
        RecordingImageLoader loader = new();
        TestImage loaded = new(32, 16);
        loader.SetImage("logo.png", loaded);
        ResourceId<ImageResource> id = new("Logo");
        ResourceStore store = new();
        store.SetResource(id, new ImageResource("logo.png"));
        UIRoot root = RootWithImageLoader(loader, store);
        Image image = new()
        {
            SourceResourceId = id
        };
        root.VisualChildren.Add(image);

        root.ProcessFrame();
        root.RetainedRenderer.Commit(root);
        root.ProcessFrame();
        root.RetainedRenderer.Commit(root);

        Assert.Equal(1, loader.GetLoadCount("logo.png"));
    }

    [Fact]
    public void DetachedImageIsRemovedFromResourceDependencyTrackingAfterResourceChange()
    {
        RecordingImageLoader loader = new();
        loader.SetImage("first.png", new TestImage(32, 16));
        loader.SetImage("second.png", new TestImage(48, 24));
        ResourceId<ImageResource> id = new("Logo");
        ResourceStore store = new();
        store.SetResource(id, new ImageResource("first.png"));
        UIRoot root = RootWithImageLoader(loader, store);
        Image image = new()
        {
            SourceResourceId = id
        };
        root.VisualChildren.Add(image);
        root.ProcessFrame();

        Assert.Contains(image, root.ResourceDependencyTracker.GetDependents(id));
        root.VisualChildren.Remove(image);
        store.SetResource(id, new ImageResource("second.png"));
        root.ProcessFrame();

        Assert.Empty(root.ResourceDependencyTracker.GetDependents(id));
        Assert.Equal(1, loader.GetLoadCount("first.png"));
        Assert.Equal(0, loader.GetLoadCount("second.png"));
    }

    private static UIRoot RootWithImageLoader(RecordingImageLoader loader, ResourceStore store)
    {
        UIRoot root = new(100, 100);
        root.SetResourceProvider(store);
        root.SetImageLoader(loader);
        return root;
    }

    private sealed class RecordingImageLoader : IImageLoader
    {
        private readonly Dictionary<string, IDrawImage> images = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> loadCounts = new(StringComparer.Ordinal);

        public void SetImage(string path, IDrawImage image)
        {
            images[path] = image;
        }

        public int GetLoadCount(string path)
        {
            return loadCounts.GetValueOrDefault(path);
        }

        public IDrawImage Load(string path)
        {
            loadCounts[path] = GetLoadCount(path) + 1;
            return images.TryGetValue(path, out IDrawImage? image)
                ? image
                : throw new InvalidOperationException($"No fake image registered for '{path}'.");
        }
    }

    private sealed class TestImage(int width, int height) : IDrawImage
    {
        public int Width { get; } = width;

        public int Height { get; } = height;
    }
}
