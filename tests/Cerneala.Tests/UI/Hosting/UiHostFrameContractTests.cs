using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Hosting;

public sealed class UiHostFrameContractTests
{
    [Fact]
    public void FirstFrameProcessesLayoutAndRenderWork()
    {
        UiHost host = HostWithRenderableRoot(out _, out _);

        UiFrame frame = host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);

        Assert.True(frame.Stats.MeasuredElements > 0);
        Assert.True(frame.Stats.ArrangedElements > 0);
        Assert.True(frame.Stats.RenderedElements > 0);
    }

    [Fact]
    public void SecondUnchangedFrameReportsNoRetainedWork()
    {
        UiHost host = HostWithRenderableRoot(out _, out _);
        host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);

        UiFrame second = host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);

        Assert.Equal(0, second.Stats.MeasuredElements);
        Assert.Equal(0, second.Stats.ArrangedElements);
        Assert.Equal(0, second.Stats.RenderedElements);
        Assert.Equal(0, second.Stats.HitTestElements);
        Assert.Equal(1, second.Stats.NoWorkFrames);
    }

    [Fact]
    public void ViewportChangeSchedulesArrangeAndRenderWork()
    {
        UiHost host = HostWithRenderableRoot(out UIRoot root, out _);
        host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);

        UiFrame resized = host.Update(FakeInputSource.CreateFrame(), new UiViewport(200, 120), TimeSpan.Zero);

        Assert.Equal(200, root.ViewportWidth);
        Assert.Equal(120, root.ViewportHeight);
        Assert.True(resized.Stats.ArrangedElements > 0);
        Assert.True(resized.Stats.RenderedElements > 0);
    }

    [Fact]
    public void SameViewportDoesNotRequeueViewportWork()
    {
        UiHost host = HostWithRenderableRoot(out _, out _);
        host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);

        UiFrame sameViewport = host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);

        Assert.Equal(1, sameViewport.Stats.NoWorkFrames);
    }

    [Fact]
    public void InputVisualInvalidationIsProcessedInSameUpdateFrame()
    {
        UiHost host = HostWithRenderableRoot(out _, out RenderCountingElement child);
        host.Update(PointerFrame(50, 50), new UiViewport(100, 100), TimeSpan.Zero);
        int renderCountAfterInitialFrame = child.RenderCount;

        UiFrame hoverFrame = host.Update(PointerFrame(50, 50, 5, 5), new UiViewport(100, 100), TimeSpan.Zero);

        Assert.True(child.IsPointerOver);
        Assert.True(hoverFrame.Stats.RenderedElements > 0);
        Assert.True(child.RenderCount > renderCountAfterInitialFrame);
    }

    [Fact]
    public void DrawSubmitsCachedCommandsWithoutReRendering()
    {
        UiHost host = HostWithRenderableRoot(out _, out RenderCountingElement child);
        FakeDrawingBackend backend = new();
        host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        int renderCountAfterUpdate = child.RenderCount;

        host.Draw(backend);
        host.Draw(backend);

        Assert.Equal(2, backend.RenderCalls);
        Assert.Equal(renderCountAfterUpdate, child.RenderCount);
        Assert.NotNull(backend.LastCommands);
        Assert.NotEmpty(backend.LastCommands);
    }

    [Fact]
    public void DrawDoesNotRegenerateRenderCacheAfterPostUpdateInvalidation()
    {
        UiHost host = HostWithRenderableRoot(out _, out RenderCountingElement child);
        FakeDrawingBackend backend = new();
        host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        int renderCountAfterUpdate = child.RenderCount;

        child.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "after update");
        host.Draw(backend);

        Assert.Equal(renderCountAfterUpdate, child.RenderCount);
    }

    private static UiHost HostWithRenderableRoot(out UIRoot root, out RenderCountingElement child)
    {
        root = new UIRoot();
        child = new RenderCountingElement();
        root.VisualChildren.Add(child);
        return new UiHost(new UiHostOptions { Root = root });
    }

    private static InputFrame PointerFrame(float previousX, float previousY, float currentX, float currentY)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(previousX, previousY);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(currentX, currentY);
        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static InputFrame PointerFrame(float x, float y)
    {
        return PointerFrame(x, y, x, y);
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
            context.DrawingContext.FillRectangle(new DrawRect(context.Bounds.X, context.Bounds.Y, 1, 1), DrawColor.White);
        }
    }
}
