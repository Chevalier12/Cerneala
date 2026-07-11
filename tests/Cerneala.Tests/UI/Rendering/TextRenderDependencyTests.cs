using Cerneala.Drawing;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Rendering;

public sealed class TextRenderDependencyTests
{
    [Fact]
    public void TextLayoutIdentityParticipatesInCacheStaleness()
    {
        RenderingTestElement element = new(Color.White);
        ElementRenderCache cache = new();
        cache.Ensure(element, new RenderCounters(), forceRebuild: true);

        element.ChangeDependencies(RenderDependency.None.WithTextLayoutIdentity("text:changed"));

        Assert.True(cache.IsStale(element));
    }

    [Fact]
    public void UnchangedTextLayoutIdentityAllowsCacheReuse()
    {
        RenderingTestElement element = new(Color.White);
        element.ChangeDependencies(RenderDependency.None.WithTextLayoutIdentity("text:same"));
        ElementRenderCache cache = new();
        RenderCounters counters = new();
        cache.Ensure(element, counters, forceRebuild: true);

        bool rebuilt = cache.Ensure(element, counters);

        Assert.False(rebuilt);
        Assert.Equal(1, counters.CacheHits);
    }

    [Fact]
    public void ForegroundOnlyRenderVersionCanInvalidateWithoutChangingTextIdentity()
    {
        RenderingTestElement element = new(Color.White);
        element.ChangeDependencies(RenderDependency.None.WithTextLayoutIdentity("text:same"));
        ElementRenderCache cache = new();
        cache.Ensure(element, new RenderCounters(), forceRebuild: true);
        string identity = element.RenderDependencies.TextLayoutIdentity;

        element.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "Foreground changed");

        Assert.Equal(identity, element.RenderDependencies.TextLayoutIdentity);
    }
}
