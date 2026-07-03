using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using ControlsStackPanel = Cerneala.UI.Controls.StackPanel;

namespace Cerneala.Tests.Controls;

public sealed class StackPanelTests
{
    [Fact]
    public void StackPanelWrapperIsSealed()
    {
        Assert.True(typeof(ControlsStackPanel).IsSealed);
    }

    [Fact]
    public void StackPanelMeasuresAndArrangesLikeLayoutStackPanel()
    {
        ControlsStackPanel panel = new();
        FixedElement first = new(new LayoutSize(20, 10));
        FixedElement second = new(new LayoutSize(30, 5));
        panel.VisualChildren.Add(first);
        panel.VisualChildren.Add(second);

        LayoutSize desired = panel.Measure(new MeasureContext(new LayoutSize(100, 100)));
        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.Equal(new LayoutSize(30, 15), desired);
        Assert.Equal(new LayoutRect(0, 0, 100, 10), first.ArrangedBounds);
        Assert.Equal(new LayoutRect(0, 10, 100, 5), second.ArrangedBounds);
    }

    [Fact]
    public void HorizontalStackPanelUsesOrientation()
    {
        ControlsStackPanel panel = new()
        {
            Orientation = Orientation.Horizontal
        };
        panel.VisualChildren.Add(new FixedElement(new LayoutSize(20, 10)));
        panel.VisualChildren.Add(new FixedElement(new LayoutSize(30, 5)));

        LayoutSize desired = panel.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(new LayoutSize(50, 10), desired);
    }

    [Fact]
    public void ParentRemeasureReusesUnchangedChildMeasureWhenConstraintsMatch()
    {
        ControlsStackPanel panel = new();
        CountingElement child = new(new LayoutSize(20, 10));
        panel.VisualChildren.Add(child);

        panel.Measure(new MeasureContext(new LayoutSize(100, 100)));
        panel.Margin = new Thickness(1);
        panel.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(1, child.MeasureCount);
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }

    private sealed class CountingElement(LayoutSize size) : UIElement
    {
        public int MeasureCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            return size;
        }
    }
}
