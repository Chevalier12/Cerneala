using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class ScrollViewerTests
{
    [Fact]
    public void PresenterComputesExtentViewportAndClampsOffset()
    {
        ScrollContentPresenter presenter = new()
        {
            Content = new FixedElement(new LayoutSize(200, 150))
        };

        presenter.Measure(new MeasureContext(new LayoutSize(80, 50)));
        presenter.SetHorizontalOffset(500);
        presenter.SetVerticalOffset(500);

        Assert.Equal(200, presenter.ExtentWidth);
        Assert.Equal(150, presenter.ExtentHeight);
        Assert.Equal(80, presenter.ViewportWidth);
        Assert.Equal(50, presenter.ViewportHeight);
        Assert.Equal(120, presenter.HorizontalOffset);
        Assert.Equal(100, presenter.VerticalOffset);
    }

    [Fact]
    public void PresenterArrangesContentWithNegativeOffsetAndClip()
    {
        ScrollContentPresenter presenter = new()
        {
            Content = new FixedElement(new LayoutSize(200, 150))
        };
        presenter.Measure(new MeasureContext(new LayoutSize(80, 50)));
        presenter.SetHorizontalOffset(20);
        presenter.SetVerticalOffset(10);

        presenter.Arrange(new ArrangeContext(new LayoutRect(5, 6, 80, 50)));

        UIElement child = Assert.IsType<FixedElement>(presenter.Content);
        Assert.Equal(new LayoutRect(-15, -4, 200, 150), child.ArrangedBounds);
    }

    [Fact]
    public void ScrollViewerWheelScrollsVerticalOffset()
    {
        UIRoot root = new(100, 100);
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(80, 300)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        viewer.Measure(new MeasureContext(new LayoutSize(80, 80)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 80, 80)));
        root.VisualChildren.Add(viewer);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerWheelFrame(10, 10, -120));

        Assert.Equal(48, viewer.Presenter.VerticalOffset);
    }

    [Fact]
    public void ScrollBarVisibilityPolicyControlsVisibleAndReservedBars()
    {
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(20, 20)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden
        };

        viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.True(viewer.IsVerticalScrollBarVisible);
        Assert.False(viewer.IsHorizontalScrollBarVisible);
        Assert.Equal(Visibility.Hidden, viewer.HorizontalScrollBar.Visibility);
    }

    [Fact]
    public void AutoScrollbarsReevaluateWhenOneScrollbarConsumesViewportSpace()
    {
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(100, 110)),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.True(viewer.IsVerticalScrollBarVisible);
        Assert.True(viewer.IsHorizontalScrollBarVisible);
    }

    [Fact]
    public void ScrollInfoOffsetUpdatesScrollBarValues()
    {
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(200, 300)),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        viewer.ScrollInfo.SetHorizontalOffset(40);
        viewer.ScrollInfo.SetVerticalOffset(60);

        Assert.Equal(40, viewer.HorizontalScrollBar.Value);
        Assert.Equal(60, viewer.VerticalScrollBar.Value);
    }

    [Fact]
    public void OffsetChangeInvalidatesArrangeRenderAndHitTestWithoutMeasure()
    {
        UIRoot root = new(100, 100);
        ScrollContentPresenter presenter = new()
        {
            Content = new FixedElement(new LayoutSize(200, 200))
        };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        presenter.Measure(new MeasureContext(new LayoutSize(50, 50)));
        presenter.Arrange(new ArrangeContext(new LayoutRect(0, 0, 50, 50)));

        presenter.SetVerticalOffset(10);

        Assert.DoesNotContain(presenter, root.LayoutQueue.SnapshotMeasure());
        Assert.Contains(presenter, root.LayoutQueue.SnapshotArrange());
        Assert.Contains(presenter, root.RenderQueue.Snapshot());
        Assert.Contains(presenter, root.HitTestQueue.Snapshot());
    }

    private static InputFrame PointerWheelFrame(float x, float y, int delta)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y).WithWheelValue(delta);
        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }
}
