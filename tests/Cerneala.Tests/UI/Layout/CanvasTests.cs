using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.Tests.UI.Layout;

public sealed class CanvasTests
{
    [Fact]
    public void CanvasMeasuresChildrenWithoutChildOffsetForcingDesiredSize()
    {
        Canvas canvas = new();
        FixedElement child = new(new LayoutSize(20, 10));
        Canvas.SetLeft(child, 50);
        Canvas.SetTop(child, 40);
        canvas.VisualChildren.Add(child);

        LayoutSize desired = canvas.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(LayoutSize.Zero, desired);
        Assert.Equal(new LayoutSize(20, 10), child.DesiredSize);
    }

    [Fact]
    public void CanvasArrangesChildAtCoordinates()
    {
        Canvas canvas = new();
        FixedElement child = new(new LayoutSize(20, 10));
        Canvas.SetLeft(child, 5);
        Canvas.SetTop(child, 7);
        canvas.VisualChildren.Add(child);
        canvas.Measure(new MeasureContext(new LayoutSize(100, 100)));

        canvas.Arrange(new ArrangeContext(new LayoutRect(10, 20, 100, 100)));

        Assert.Equal(new LayoutRect(15, 27, 20, 10), child.ArrangedBounds);
    }

    [Fact]
    public void ChangingAttachedChildCoordinatesQueuesCanvasArrange()
    {
        UIRoot root = new(100, 100);
        Canvas canvas = new();
        FixedElement child = new(new LayoutSize(20, 10));
        root.VisualChildren.Add(canvas);
        canvas.VisualChildren.Add(child);
        canvas.Measure(new MeasureContext(new LayoutSize(100, 100)));
        canvas.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Canvas.SetLeft(child, 12);
        Canvas.SetTop(child, 8);

        Assert.Contains(canvas, root.LayoutQueue.SnapshotArrange());

        root.ProcessFrame(new FramePhaseProcessors
        {
            Arrange = element =>
            {
                if (ReferenceEquals(element, canvas))
                {
                    canvas.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));
                }
            }
        });

        Assert.Equal(new LayoutRect(12, 8, 20, 10), child.ArrangedBounds);
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }
}
