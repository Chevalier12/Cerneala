using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Rendering;

public sealed class RenderQueueProcessorTests
{
    [Fact]
    public void QueuedRenderWorkRebuildsElementCacheDuringFrame()
    {
        UIRoot root = new();
        RenderingTestElement child = new(DrawColor.White);
        root.VisualChildren.Add(child);

        child.Invalidate(InvalidationFlags.Render, "test");
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(1, stats.RenderedElements);
        Assert.Equal(1, child.RenderCount);
        Assert.True(root.RetainedRenderCache.GetElementCache(child).IsValid);
    }

    [Fact]
    public void FailedRenderProcessingKeepsDirtyFlagsAndQueuedWork()
    {
        UIRoot root = new();
        RenderingTestElement child = new(DrawColor.White, throwOnRender: true);
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
        RenderingTestElement child = new(DrawColor.White);
        root.VisualChildren.Add(child);

        child.Invalidate(InvalidationFlags.Render, "test");
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(0, stats.MeasuredElements);
        Assert.Equal(0, stats.ArrangedElements);
        Assert.Equal(1, stats.RenderedElements);
    }
}
