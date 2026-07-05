using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Resources;

public sealed class HostResourceInvalidationIntegrationTests
{
    [Fact]
    public void FontResourceChangeInvalidatesDependentTextBlockThroughRoot()
    {
        ResourceId<FontResource> id = new("Body");
        ResourceStore store = new();
        store.SetResource(id, new FontResource(new TestFont("Default", 16)));
        UIRoot root = new(100, 100);
        root.SetResourceProvider(store);
        TextBlock text = new()
        {
            Text = "Hello",
            FontResourceId = id
        };
        root.VisualChildren.Add(text);
        root.ProcessFrame();
        text.DirtyState.Clear(InvalidationFlags.Measure | InvalidationFlags.Render);

        store.SetResource(id, new FontResource(new TestFont("Updated", 16)));
        FrameStats stats = root.ProcessFrame();

        Assert.True(stats.MeasuredElements > 0);
        Assert.True(stats.RenderedElements > 0);
    }

    [Fact]
    public void IntrinsicImageResourceChangeInvalidatesMeasureAndRenderThroughRoot()
    {
        ResourceId<ImageResource> id = new("Logo");
        ResourceStore store = new();
        store.SetResource(id, new ImageResource(new TestImage(10, 20)));
        UIRoot root = new(100, 100);
        root.SetResourceProvider(store);
        Image image = new()
        {
            SourceResourceId = id,
            UseIntrinsicSize = true
        };
        root.VisualChildren.Add(image);
        root.ProcessFrame();
        image.DirtyState.Clear(InvalidationFlags.Measure | InvalidationFlags.Render);

        store.SetResource(id, new ImageResource(new TestImage(30, 40)));
        FrameStats stats = root.ProcessFrame();

        Assert.True(stats.MeasuredElements > 0);
        Assert.True(stats.RenderedElements > 0);
    }

    [Fact]
    public void FixedSizeImageResourceChangeInvalidatesRenderOnlyThroughRoot()
    {
        ResourceId<ImageResource> id = new("Logo");
        ResourceStore store = new();
        store.SetResource(id, new ImageResource(new TestImage(10, 20)));
        UIRoot root = new(100, 100);
        root.SetResourceProvider(store);
        Image image = new()
        {
            SourceResourceId = id,
            UseIntrinsicSize = false
        };
        root.VisualChildren.Add(image);
        root.ProcessFrame();
        image.DirtyState.Clear(InvalidationFlags.Measure | InvalidationFlags.Render);

        store.SetResource(id, new ImageResource(new TestImage(30, 40)));
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(0, stats.MeasuredElements);
        Assert.True(stats.RenderedElements > 0);
    }

    [Fact]
    public void ObservableCustomProviderInvalidatesDependentTextBlockThroughRoot()
    {
        ResourceId<FontResource> id = new("Body");
        ObservableProvider provider = new();
        provider.SetResource(id, new FontResource(new TestFont("Default", 16)));
        UIRoot root = new(100, 100);
        root.SetResourceProvider(provider);
        TextBlock text = new()
        {
            Text = "Hello",
            FontResourceId = id
        };
        root.VisualChildren.Add(text);
        root.ProcessFrame();
        text.DirtyState.Clear(InvalidationFlags.Measure | InvalidationFlags.Render);

        provider.SetResource(id, new FontResource(new TestFont("Updated", 16)));
        FrameStats stats = root.ProcessFrame();

        Assert.True(stats.MeasuredElements > 0);
        Assert.True(stats.RenderedElements > 0);
    }

    [Fact]
    public void NonObservableProviderResolvesResourcesWithoutSubscription()
    {
        ResourceId<FontResource> id = new("Body");
        StaticProvider provider = new();
        provider.SetResource(id, new FontResource(new TestFont("Default", 16)));
        UIRoot root = new(100, 100);
        root.SetResourceProvider(provider);
        TextBlock text = new()
        {
            Text = "Hello",
            FontResourceId = id
        };
        root.VisualChildren.Add(text);

        FrameStats stats = root.ProcessFrame();

        Assert.True(stats.MeasuredElements > 0);
        Assert.Same(provider, root.ResourceProvider);
    }

    private sealed record TestFont(string FamilyName, float Size) : IDrawFont;

    private sealed class TestImage(int width, int height) : IDrawImage
    {
        public int Width { get; } = width;

        public int Height { get; } = height;
    }

    private sealed class ObservableProvider : StaticProvider, IObservableResourceProvider
    {
        public event EventHandler<ResourceChangedEventArgs>? ResourceChanged;

        public override void SetResource<T>(ResourceId<T> id, T resource)
        {
            object? oldValue = TryGetResource(id, out T? existing) ? existing : null;
            base.SetResource(id, resource);
            ResourceChanged?.Invoke(this, new ResourceChangedEventArgs(typeof(T), id.Key, oldValue, resource, 1));
        }
    }

    private class StaticProvider : IResourceProvider
    {
        private readonly Dictionary<(Type Type, string Key), object?> resources = new();

        public virtual void SetResource<T>(ResourceId<T> id, T resource)
        {
            resources[(typeof(T), id.Key)] = resource;
        }

        public bool TryGetResource<T>(ResourceId<T> id, out T resource)
        {
            if (resources.TryGetValue((typeof(T), id.Key), out object? value) && value is T typed)
            {
                resource = typed;
                return true;
            }

            resource = default!;
            return false;
        }
    }
}
