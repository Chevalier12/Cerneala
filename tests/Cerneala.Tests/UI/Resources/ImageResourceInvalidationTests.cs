using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Resources;

public sealed class ImageResourceInvalidationTests
{
    [Fact]
    public void ImageResourceResolvesDrawImage()
    {
        TestImage image = new(10, 20);
        ImageResource resource = new(image);

        Assert.Same(image, resource.Resolve());
    }

    [Fact]
    public void ImageResourceUsesExplicitLoader()
    {
        RecordingImageLoader loader = new(new TestImage(10, 20));
        ImageResource resource = new("asset.png");

        IDrawImage image = resource.Resolve(loader);

        Assert.Same(loader.Image, image);
        Assert.Equal("asset.png", loader.Path);
    }

    [Fact]
    public void ImageResourceRejectsNullLoaderResult()
    {
        ImageResource resource = new("asset.png");

        Assert.Throws<InvalidOperationException>(() => resource.Resolve(new NullImageLoader()));
    }

    [Fact]
    public void ReplacingIntrinsicImageResourceInvalidatesMeasureAndRender()
    {
        ResourceStore store = new();
        ResourceDependencyTracker tracker = new();
        tracker.Track(store);
        ResourceId<ImageResource> id = new("Logo");
        store.SetResource(id, new ImageResource(new TestImage(10, 20)));
        Image image = new()
        {
            SourceResourceId = id,
            ResourceProvider = store,
            ResourceDependencyTracker = tracker
        };
        image.Measure(new MeasureContext(new LayoutSize(100, 100)));
        image.DirtyState.ClearAll();

        store.SetResource(id, new ImageResource(new TestImage(30, 40)));

        Assert.True(image.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(image.DirtyState.Has(InvalidationFlags.Render));
        Assert.Equal(new LayoutSize(30, 40), image.Measure(new MeasureContext(new LayoutSize(100, 100))));
    }

    [Fact]
    public void ReplacingFixedSizeImageResourceInvalidatesRenderOnly()
    {
        ResourceStore store = new();
        ResourceDependencyTracker tracker = new();
        tracker.Track(store);
        ResourceId<ImageResource> id = new("Logo");
        store.SetResource(id, new ImageResource(new TestImage(10, 20)));
        Image image = new()
        {
            SourceResourceId = id,
            ResourceProvider = store,
            ResourceDependencyTracker = tracker,
            UseIntrinsicSize = false
        };
        image.Measure(new MeasureContext(new LayoutSize(100, 100)));
        image.DirtyState.ClearAll();

        store.SetResource(id, new ImageResource(new TestImage(30, 40)));

        Assert.False(image.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(image.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void ChangingImageResourceIdInvalidatesIntrinsicMeasurementAndRender()
    {
        ResourceStore store = new();
        ResourceId<ImageResource> logo = new("Logo");
        ResourceId<ImageResource> avatar = new("Avatar");
        store.SetResource(logo, new ImageResource(new TestImage(10, 20)));
        store.SetResource(avatar, new ImageResource(new TestImage(30, 40)));
        Image image = new()
        {
            SourceResourceId = logo,
            ResourceProvider = store
        };
        image.Measure(new MeasureContext(new LayoutSize(100, 100)));
        image.DirtyState.ClearAll();

        image.SourceResourceId = avatar;

        Assert.True(image.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(image.DirtyState.Has(InvalidationFlags.Render));
        Assert.Equal(new LayoutSize(30, 40), image.Measure(new MeasureContext(new LayoutSize(100, 100))));
    }

    [Fact]
    public void ChangingImageResourceProviderInvalidatesCachedIntrinsicMeasurement()
    {
        ResourceId<ImageResource> id = new("Logo");
        ResourceStore firstStore = new();
        ResourceStore secondStore = new();
        firstStore.SetResource(id, new ImageResource(new TestImage(10, 20)));
        secondStore.SetResource(id, new ImageResource(new TestImage(30, 40)));
        Image image = new()
        {
            SourceResourceId = id,
            ResourceProvider = firstStore
        };
        image.Measure(new MeasureContext(new LayoutSize(100, 100)));
        image.DirtyState.ClearAll();

        image.ResourceProvider = secondStore;

        Assert.True(image.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(image.DirtyState.Has(InvalidationFlags.Render));
        Assert.Equal(new LayoutSize(30, 40), image.Measure(new MeasureContext(new LayoutSize(100, 100))));
    }

    private sealed class RecordingImageLoader(IDrawImage image) : IImageLoader
    {
        public IDrawImage Image { get; } = image;

        public string? Path { get; private set; }

        public IDrawImage Load(string path)
        {
            Path = path;
            return Image;
        }
    }

    private sealed class NullImageLoader : IImageLoader
    {
        public IDrawImage Load(string path)
        {
            return null!;
        }
    }

    private sealed class TestImage(int width, int height) : IDrawImage
    {
        public int Width { get; } = width;

        public int Height { get; } = height;
    }
}
