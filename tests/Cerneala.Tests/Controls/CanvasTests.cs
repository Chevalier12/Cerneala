using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class CanvasTests
{
    [Fact]
    public void CanvasWrapperIsSealed()
    {
        Assert.True(typeof(Canvas).IsSealed);
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

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }
}
