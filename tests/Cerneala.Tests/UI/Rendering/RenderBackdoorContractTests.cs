using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Rendering;

public sealed class RenderBackdoorContractTests
{
    [Fact]
    public void BuilderThrowsWhenVisibleLocalCacheWasNeverBuilt()
    {
        UIRoot root = new();
        RenderingTestElement child = new(Color.White);
        root.VisualChildren.Add(child);
        RetainedRenderCache cache = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new DrawCommandListBuilder().Build(root, cache, new RenderCounters()));

        Assert.Contains("valid local render cache", exception.Message);
        Assert.Equal(0, child.RenderCount);
    }

    [Fact]
    public void BuilderThrowsWhenVisibleLocalCacheIsStale()
    {
        UIRoot root = new();
        RenderingTestElement child = new(Color.White);
        root.VisualChildren.Add(child);
        RetainedRenderCache cache = new();
        RenderCounters counters = new();
        cache.GetElementCache(root).Ensure(root, counters, forceRebuild: true);
        cache.GetElementCache(child).Ensure(child, counters, forceRebuild: true);
        int renderCountAfterPrepare = child.RenderCount;

        child.ChangeDependencies(RenderDependency.None.WithTextLayoutIdentity("changed"));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new DrawCommandListBuilder().Build(root, cache, counters));

        Assert.Contains("valid local render cache", exception.Message);
        Assert.Equal(renderCountAfterPrepare, child.RenderCount);
    }

    [Fact]
    public void BuilderComposesPreparedLocalCachesWithoutRenderingElements()
    {
        UIRoot root = new();
        RenderingTestElement child = new(Color.White);
        root.VisualChildren.Add(child);
        RetainedRenderCache cache = new();
        RenderCounters counters = new();
        cache.GetElementCache(root).Ensure(root, counters, forceRebuild: true);
        cache.GetElementCache(child).Ensure(child, counters, forceRebuild: true);
        int renderCountAfterPrepare = child.RenderCount;

        new DrawCommandListBuilder().Build(root, cache, counters);

        Assert.True(cache.IsRootValid);
        Assert.Single(cache.RootCommands);
        Assert.Equal(renderCountAfterPrepare, child.RenderCount);
    }
}
