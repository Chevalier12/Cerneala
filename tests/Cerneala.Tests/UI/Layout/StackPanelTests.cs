using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.Tests.UI.Layout;

public sealed class StackPanelTests
{
    [Fact]
    public void VerticalStackAccumulatesHeight()
    {
        StackPanel panel = new();
        panel.VisualChildren.Add(new FixedElement(new LayoutSize(20, 10)));
        panel.VisualChildren.Add(new FixedElement(new LayoutSize(30, 5)));

        LayoutSize desired = panel.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(new LayoutSize(30, 15), desired);
    }

    [Fact]
    public void HorizontalStackAccumulatesWidth()
    {
        StackPanel panel = new()
        {
            Orientation = Orientation.Horizontal
        };
        panel.VisualChildren.Add(new FixedElement(new LayoutSize(20, 10)));
        panel.VisualChildren.Add(new FixedElement(new LayoutSize(30, 5)));

        LayoutSize desired = panel.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(new LayoutSize(50, 10), desired);
    }

    [Fact]
    public void StackPanelArrangesChildrenInVisualOrder()
    {
        StackPanel panel = new();
        FixedElement first = new(new LayoutSize(20, 10));
        FixedElement second = new(new LayoutSize(30, 5));
        panel.VisualChildren.Add(first);
        panel.VisualChildren.Add(second);
        panel.Measure(new MeasureContext(new LayoutSize(100, 100)));

        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.Equal(new LayoutRect(0, 0, 100, 10), first.ArrangedBounds);
        Assert.Equal(new LayoutRect(0, 10, 100, 5), second.ArrangedBounds);
    }

    [Fact]
    public void VerticalStackUsesChildMarginsForMeasureAndSlots()
    {
        StackPanel panel = new();
        FixedElement first = new(new LayoutSize(20, 10))
        {
            Margin = new Thickness(1, 2, 3, 4)
        };
        FixedElement second = new(new LayoutSize(30, 5))
        {
            Margin = new Thickness(0, 1, 0, 2)
        };
        panel.VisualChildren.Add(first);
        panel.VisualChildren.Add(second);

        LayoutSize desired = panel.Measure(new MeasureContext(new LayoutSize(100, 100)));
        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.Equal(new LayoutSize(30, 24), desired);
        Assert.Equal(new LayoutRect(1, 2, 96, 10), first.ArrangedBounds);
        Assert.Equal(new LayoutRect(0, 17, 100, 5), second.ArrangedBounds);
    }

    [Fact]
    public void ParentMarginRemeasureUpdatesChildMeasureWhenConstraintsChange()
    {
        StackPanel panel = new();
        FixedElement child = new(new LayoutSize(20, 10));
        panel.VisualChildren.Add(child);

        panel.Measure(new MeasureContext(new LayoutSize(100, 100)));
        panel.Margin = new Thickness(1);
        panel.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(2, child.MeasureCount);
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        public int MeasureCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            return size;
        }
    }
}
