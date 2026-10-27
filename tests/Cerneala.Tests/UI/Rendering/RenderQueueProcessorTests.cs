using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Rendering;

public sealed class RenderQueueProcessorTests
{
    [Fact]
    public void QueuedRenderWorkRebuildsElementCacheDuringFrame()
    {
        UIRoot root = new();
        RenderingTestElement child = new(Color.White);
        root.VisualChildren.Add(child);
        root.ProcessFrame();
        int renderCountAfterInitialFrame = child.RenderCount;

        child.Invalidate(InvalidationFlags.Render, "test");
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(1, stats.RenderedElements);
        Assert.Equal(renderCountAfterInitialFrame + 1, child.RenderCount);
        Assert.True(root.RetainedRenderCache.GetElementCache(child).IsValid);
    }

    [Fact]
    public void FailedRenderProcessingKeepsDirtyFlagsAndQueuedWork()
    {
        UIRoot root = new();
        RenderingTestElement child = new(Color.White, throwOnRender: true);
        root.VisualChildren.Add(child);
        child.Invalidate(InvalidationFlags.Render, "test");

        Assert.Throws<InvalidOperationException>(() => root.ProcessFrame());

        Assert.True(child.DirtyState.Has(InvalidationFlags.Render));
        Assert.Contains(child, root.RenderQueue.Snapshot());
    }

    [Fact]
    public void RenderOnlyInvalidationDoesNotRunMeasureOrArrange()
    {
        UIRoot root = new();
        RenderingTestElement child = new(Color.White);
        root.VisualChildren.Add(child);
        root.ProcessFrame();

        child.Invalidate(InvalidationFlags.Render, "test");
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(0, stats.MeasuredElements);
        Assert.Equal(0, stats.ArrangedElements);
        Assert.Equal(1, stats.RenderedElements);
    }

    [Fact]
    public void ScopeOnlyInvalidationDoesNotSkipStaleLayoutCache()
    {
        UIRoot root = new();
        RenderingTestElement child = new(Color.White);
        root.VisualChildren.Add(child);
        root.ProcessFrame();
        int renderCountAfterInitialFrame = child.RenderCount;

        child.Arrange(new ArrangeContext(new LayoutRect(0, 20, 10, 10)));
        child.SetLayoutCorrectionTransform(new Transform(Matrix3x2.CreateTranslation(0, -20)));
        FrameStats stats = root.ProcessFrame();
        ElementRenderCache cache = root.RetainedRenderCache.GetElementCache(child);

        Assert.Equal(1, stats.RenderedElements);
        Assert.Equal(renderCountAfterInitialFrame + 1, child.RenderCount);
        Assert.True(cache.IsValid);
        Assert.Equal(child.RenderVersion, cache.RenderVersion);
    }

    // #954: a scope change (Opacity) must not hide an explicit Render request for the same element in the same frame.
    [Theory]
    [InlineData("invalidateOnly", 1)]
    [InlineData("opacityOnly", 0)]
    [InlineData("opacityThenInvalidate", 1)]
    [InlineData("invalidateThenOpacity", 1)]
    [InlineData("opacityThenParentSubtreeInvalidate", 1)]
    [InlineData("parentSubtreeInvalidateThenOpacity", 1)]
    public void ExplicitRenderInvalidationStillRebuildsWhenAScopePropertyChangedInTheSameFrame(string row, int expectedRebuilds)
    {
        UIRoot root = new();
        RenderingTestElement parent = new(Color.White);
        RenderingTestElement child = new(Color.White);
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        root.ProcessFrame();
        int renderCountAfterInitialFrame = child.RenderCount;

        switch (row)
        {
            case "invalidateOnly":
                child.Invalidate(InvalidationFlags.Render, "test");
                break;
            case "opacityOnly":
                child.Opacity = 0.5f;
                break;
            case "opacityThenInvalidate":
                child.Opacity = 0.5f;
                child.Invalidate(InvalidationFlags.Render, "test");
                break;
            case "invalidateThenOpacity":
                child.Invalidate(InvalidationFlags.Render, "test");
                child.Opacity = 0.5f;
                break;
            case "opacityThenParentSubtreeInvalidate":
                child.Opacity = 0.5f;
                parent.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
                break;
            default:
                parent.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
                child.Opacity = 0.5f;
                break;
        }

        root.ProcessFrame();

        Assert.True(
            child.RenderCount == renderCountAfterInitialFrame + expectedRebuilds,
            $"{row}: OnRender ran {child.RenderCount - renderCountAfterInitialFrame} time(s) after the initial frame, expected {expectedRebuilds}.");
    }
}
