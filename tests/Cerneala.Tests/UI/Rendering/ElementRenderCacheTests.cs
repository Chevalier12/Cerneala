using Cerneala.Drawing;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Rendering;

public sealed class ElementRenderCacheTests
{
    [Fact]
    public void DirtyElementCacheRebuildsLocalCommands()
    {
        RenderingTestElement element = new(Color.White);
        ElementRenderCache cache = new();
        RenderCounters counters = new();

        bool rebuilt = cache.Ensure(element, counters, forceRebuild: true);

        Assert.True(rebuilt);
        Assert.True(cache.IsValid);
        Assert.Single(cache.Commands);
        Assert.Equal(1, element.RenderCount);
        Assert.Equal(1, counters.CacheMisses);
        Assert.Equal(1, counters.LocalRebuilds);
    }

    [Fact]
    public void UnchangedElementCacheIsReused()
    {
        RenderingTestElement element = new(Color.White);
        ElementRenderCache cache = new();
        RenderCounters counters = new();
        cache.Ensure(element, counters, forceRebuild: true);

        bool rebuilt = cache.Ensure(element, counters);

        Assert.False(rebuilt);
        Assert.Equal(1, element.RenderCount);
        Assert.Equal(1, counters.CacheHits);
    }

    [Fact]
    public void DependencyChangeMakesCacheStale()
    {
        RenderingTestElement element = new(Color.White);
        ElementRenderCache cache = new();
        RenderCounters counters = new();
        cache.Ensure(element, counters, forceRebuild: true);

        element.ChangeDependencies(RenderDependency.None.WithTextVersion(1));

        Assert.True(cache.IsStale(element));
    }

    [Fact]
    public void CacheBuiltForOneElementIsStaleForAnotherElementWithMatchingVersion()
    {
        RenderingTestElement first = new(Color.White);
        RenderingTestElement second = new(Color.Black);
        ElementRenderCache cache = new();
        cache.Ensure(first, new RenderCounters(), forceRebuild: true);

        Assert.True(cache.IsStale(second));
        Assert.Throws<InvalidOperationException>(() => cache.GetValidCommands(second));
    }

    [Fact]
    public void RebuildStoresContentBounds()
    {
        RenderingTestElement element = new(Color.White);
        element.Arrange(new ArrangeContext(new LayoutRect(2, 3, 20, 10)));
        ElementRenderCache cache = new();

        cache.Ensure(element, new RenderCounters(), forceRebuild: true);

        Assert.Equal(new LayoutRect(2, 3, 20, 10), cache.ContentBounds);
    }

    [Fact]
    public void FailedRebuildInvalidatesPreviouslyValidCache()
    {
        RenderingTestElement first = new(Color.White);
        RenderingTestElement failing = new(Color.White, throwOnRender: true);
        ElementRenderCache cache = new();
        cache.Ensure(first, new RenderCounters(), forceRebuild: true);

        Assert.Throws<InvalidOperationException>(() => cache.Ensure(failing, new RenderCounters(), forceRebuild: true));

        Assert.False(cache.IsValid);
    }
}
