using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Hosting;

public sealed class UiHostLateTreeMutationTests
{
    [Fact]
    public void VisualChildAddedAfterFirstFrameIsProcessedDuringNextUpdate()
    {
        UIRoot root = new();
        UiHost host = new(new UiHostOptions { Root = root });
        host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        RenderCountingElement child = new();

        root.VisualChildren.Add(child);

        Assert.Throws<InvalidOperationException>(() => root.RetainedRenderer.Render(root));

        UiFrame frame = host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        DrawCommandList committed = root.RetainedRenderer.Render(root);

        Assert.True(frame.Stats.MeasuredElements > 0);
        Assert.True(frame.Stats.ArrangedElements > 0);
        Assert.True(frame.Stats.RenderedElements > 0);
        Assert.True(frame.Stats.HitTestElements > 0);
        Assert.Equal(1, child.MeasureCount);
        Assert.Equal(1, child.ArrangeCount);
        Assert.Equal(1, child.RenderCount);
        Assert.Single(committed);
    }

    [Fact]
    public void VisualChildAddedAfterFirstFrameDoesNotRenderDuringDraw()
    {
        UIRoot root = new();
        UiHost host = new(new UiHostOptions { Root = root });
        FakeDrawingBackend backend = new();
        host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        RenderCountingElement child = new();
        root.VisualChildren.Add(child);
        host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        int renderCountAfterUpdate = child.RenderCount;

        host.Draw(backend);
        host.Draw(backend);

        Assert.Equal(2, backend.RenderCalls);
        Assert.Equal(renderCountAfterUpdate, child.RenderCount);
        Assert.NotNull(backend.LastCommands);
        Assert.Single(backend.LastCommands);
    }

    [Fact]
    public void RootVisualChildRemovedAfterFirstFrameIsProcessedDuringNextUpdate()
    {
        UIRoot root = new();
        RenderCountingElement child = new();
        root.VisualChildren.Add(child);
        UiHost host = new(new UiHostOptions { Root = root });
        host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        int renderCountAfterFirstFrame = child.RenderCount;

        root.VisualChildren.Remove(child);

        Assert.Throws<InvalidOperationException>(() => root.RetainedRenderer.Render(root));

        UiFrame frame = host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        DrawCommandList committed = root.RetainedRenderer.Render(root);

        Assert.True(frame.Stats.MeasuredElements > 0);
        Assert.True(frame.Stats.ArrangedElements > 0);
        Assert.True(frame.Stats.RenderedElements > 0);
        Assert.True(frame.Stats.HitTestElements > 0);
        Assert.Equal(renderCountAfterFirstFrame, child.RenderCount);
        Assert.Empty(committed);
    }

    private sealed class RenderCountingElement : UIElement
    {
        public int MeasureCount { get; private set; }

        public int ArrangeCount { get; private set; }

        public int RenderCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            return new LayoutSize(10, 10);
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            ArrangeCount++;
            return new LayoutRect(context.FinalRect.X, context.FinalRect.Y, DesiredSize.Width, DesiredSize.Height);
        }

        protected override void OnRender(RenderContext context)
        {
            RenderCount++;
            context.DrawingContext.FillRectangle(new DrawRect(context.Bounds.X, context.Bounds.Y, 1, 1), Color.White);
        }
    }
}
