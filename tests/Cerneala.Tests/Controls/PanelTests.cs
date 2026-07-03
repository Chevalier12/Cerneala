using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class PanelTests
{
    [Fact]
    public void PanelWrapperIsSealed()
    {
        Assert.True(typeof(Panel).IsSealed);
    }

    [Fact]
    public void PanelMeasuresVisualChildren()
    {
        Panel panel = new();
        panel.VisualChildren.Add(new FixedElement(new LayoutSize(20, 10)));
        panel.VisualChildren.Add(new FixedElement(new LayoutSize(30, 5)));

        LayoutSize desired = panel.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(new LayoutSize(30, 10), desired);
    }

    [Fact]
    public void PanelArrangesChildrenToFinalRect()
    {
        Panel panel = new();
        FixedElement child = new(new LayoutSize(20, 10));
        panel.VisualChildren.Add(child);
        panel.Measure(new MeasureContext(new LayoutSize(100, 100)));

        panel.Arrange(new ArrangeContext(new LayoutRect(1, 2, 30, 40)));

        Assert.Equal(new LayoutRect(1, 2, 30, 40), child.ArrangedBounds);
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }
}
