using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Hosting;

public sealed class UiHostFrameStatsIntegrityTests
{
    [Fact]
    public void UpdateCommitsRootCommandsAfterCountingRenderCacheWork()
    {
        UiHost host = HostWithRenderableRoot(out UIRoot root, out RenderCountingElement child);

        UiFrame frame = host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        DrawCommandList committed = root.RetainedRenderer.Render(root);

        Assert.True(frame.Stats.RenderedElements > 0);
        Assert.True(child.RenderCount > 0);
        Assert.NotEmpty(committed);
    }

    [Fact]
    public void RenderInvalidationAfterUpdateIsNotProcessedUntilNextUpdate()
    {
        UiHost host = HostWithRenderableRoot(out UIRoot root, out RenderCountingElement child);
        host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        int renderCountAfterFirstUpdate = child.RenderCount;

        child.Invalidate(InvalidationFlags.Render, "after update");

        Assert.Throws<InvalidOperationException>(() => root.RetainedRenderer.Render(root));

        UiFrame next = host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        DrawCommandList committed = root.RetainedRenderer.Render(root);

        Assert.True(next.Stats.RenderedElements > 0);
        Assert.Equal(renderCountAfterFirstUpdate + 1, child.RenderCount);
        Assert.NotEmpty(committed);
    }

    private static UiHost HostWithRenderableRoot(out UIRoot root, out RenderCountingElement child)
    {
        root = new UIRoot();
        child = new RenderCountingElement();
        root.VisualChildren.Add(child);
        return new UiHost(new UiHostOptions { Root = root });
    }

    private sealed class RenderCountingElement : UIElement
    {
        public int RenderCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(10, 10);
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            return new LayoutRect(context.FinalRect.X, context.FinalRect.Y, DesiredSize.Width, DesiredSize.Height);
        }

        protected override void OnRender(RenderContext context)
        {
            RenderCount++;
            context.DrawingContext.FillRectangle(new DrawRect(context.Bounds.X, context.Bounds.Y, 1, 1), Color.White);
        }
    }
}
