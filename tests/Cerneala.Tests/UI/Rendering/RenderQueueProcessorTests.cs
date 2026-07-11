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
}
