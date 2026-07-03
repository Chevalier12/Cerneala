using Cerneala.Drawing;
using Cerneala.UI.Resources;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Rendering;

public sealed class ResourceRenderDependencyTests
{
    [Fact]
    public void ResourceIdentityParticipatesInCacheStaleness()
    {
        RenderingTestElement element = new(DrawColor.White);
        ElementRenderCache cache = new();
        cache.Ensure(element, new RenderCounters(), forceRebuild: true);

        element.ChangeDependencies(RenderDependency.None.WithResourceIdentity("resource:logo").WithResourceVersion(1));

        Assert.True(cache.IsStale(element));
    }

    [Fact]
    public void UnchangedResourceDependencyAllowsCacheReuse()
    {
        RenderingTestElement element = new(DrawColor.White);
        element.ChangeDependencies(RenderDependency.None.WithResourceIdentity("resource:logo").WithResourceVersion(1));
        ElementRenderCache cache = new();
        RenderCounters counters = new();
        cache.Ensure(element, counters, forceRebuild: true);

        bool rebuilt = cache.Ensure(element, counters);

        Assert.False(rebuilt);
        Assert.Equal(1, counters.CacheHits);
    }

    [Fact]
    public void ResourceVersionPreservesTrackerLongVersion()
    {
        long version = (long)int.MaxValue + 1;

        RenderDependency dependency = RenderDependency.None.WithResourceVersion(version);

        Assert.Equal(version, dependency.ResourceVersion);
    }

    [Fact]
    public void DifferentResourceReplacementInvalidatesCacheEvenWhenResourceVersionsMatch()
    {
        ResourceStore store = new();
        ResourceDependencyTracker tracker = new();
        tracker.Track(store);
        ResourceId<string> first = new("First");
        ResourceId<string> second = new("Second");
        RenderingTestElement element = new(DrawColor.White);
        tracker.RecordDependency(element, first);
        tracker.RecordDependency(element, second);

        store.SetResource(first, "A");
        element.ChangeDependencies(RenderDependency.None
            .WithResourceIdentity("resource:first,resource:second")
            .WithResourceVersion(tracker.GetDependencyVersion(element)));
        ElementRenderCache cache = new();
        cache.Ensure(element, new RenderCounters(), forceRebuild: true);

        store.SetResource(second, "B");
        element.ChangeDependencies(element.RenderDependencies
            .WithResourceVersion(tracker.GetDependencyVersion(element)));

        Assert.True(cache.IsStale(element));
    }
}
