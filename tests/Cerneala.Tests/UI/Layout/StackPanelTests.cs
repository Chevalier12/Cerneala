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

    [Theory]
    [InlineData(1f, 3f, 13f)]
    [InlineData(2f, 1.5f, 6.5f)]
    public void LayoutRoundingKeepsHorizontalChildrenContiguousWhenThePanelOriginIsOnAHalfDevicePixel(float scale, float childWidth, float slotWidth)
    {
        // Four children of 3 device pixels in a panel of 12 device pixels, centered in a slot one device pixel
        // wider: the panel starts half a device pixel in and its own bounds round to 0..12 device pixels.
        StackPanel panel = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        FixedElement[] cells = AddCells(panel, new LayoutSize(childWidth, 10));

        LayoutRounding rounding = LayoutRounding.ForScale(scale);
        panel.Measure(new MeasureContext(new LayoutSize(slotWidth, 10), rounding));
        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, slotWidth, 10), rounding));

        Assert.Equal("0..12", GetDeviceEdges([panel], scale, columns: true));
        Assert.Equal("0..3, 3..6, 6..9, 9..12", GetDeviceEdges(cells, scale, columns: true));
    }

    [Theory]
    [InlineData(1f, 3f, 13f)]
    [InlineData(2f, 1.5f, 6.5f)]
    public void LayoutRoundingKeepsVerticalChildrenContiguousWhenThePanelOriginIsOnAHalfDevicePixel(float scale, float childHeight, float slotHeight)
    {
        StackPanel panel = new()
        {
            VerticalAlignment = VerticalAlignment.Center
        };
        FixedElement[] cells = AddCells(panel, new LayoutSize(10, childHeight));

        LayoutRounding rounding = LayoutRounding.ForScale(scale);
        panel.Measure(new MeasureContext(new LayoutSize(10, slotHeight), rounding));
        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 10, slotHeight), rounding));

        Assert.Equal("0..12", GetDeviceEdges([panel], scale, columns: false));
        Assert.Equal("0..3, 3..6, 6..9, 9..12", GetDeviceEdges(cells, scale, columns: false));
    }

    private static FixedElement[] AddCells(StackPanel panel, LayoutSize size)
    {
        FixedElement[] cells = new FixedElement[4];
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = new FixedElement(size);
            panel.VisualChildren.Add(cells[i]);
        }

        return cells;
    }

    private static string GetDeviceEdges(IEnumerable<UIElement> elements, float scale, bool columns)
    {
        return string.Join(", ", elements.Select(element =>
        {
            LayoutRect bounds = element.ArrangedBounds;
            float start = columns ? bounds.X : bounds.Y;
            float end = start + (columns ? bounds.Width : bounds.Height);
            return $"{start * scale}..{end * scale}";
        }));
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
