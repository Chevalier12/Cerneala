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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LayoutRoundingDesiresTheSumOfChildDevicePixelsWhenTheFloatSumIsOneStepAboveIt(bool vertical)
    {
        // At scale 1.75 the children desire 57 / 1.75f and 1 / 1.75f. Their float sum is one step above 58 / 1.75f,
        // and rounding that sum up must not add a 59th device pixel that leaves a gap before the next sibling.
        Orientation orientation = vertical ? Orientation.Vertical : Orientation.Horizontal;
        UIRoot root = new(200, 200, 1.75f);
        StackPanel outer = new()
        {
            Orientation = orientation,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        StackPanel inner = new() { Orientation = orientation };
        UIElement first = CreateSizedElement(32.5f, vertical);
        UIElement second = CreateSizedElement(0.5f, vertical);
        UIElement next = CreateSizedElement(10, vertical);
        inner.VisualChildren.Add(first);
        inner.VisualChildren.Add(second);
        outer.VisualChildren.Add(inner);
        outer.VisualChildren.Add(next);
        root.VisualChildren.Add(outer);

        root.ProcessFrame();

        float desiredInner = vertical ? inner.DesiredSize.Height : inner.DesiredSize.Width;
        LayoutRect nextBounds = next.ArrangedBounds;
        Assert.Equal(
            (57f, 58f, 58f, 58f),
            (ToDevicePixels(first.ArrangedBounds, vertical, end: true),
                ToDevicePixels(second.ArrangedBounds, vertical, end: true),
                MathF.Round(desiredInner * 1.75f),
                ToDevicePixels(nextBounds, vertical, end: false)));
    }

    [Fact]
    public void LayoutRoundingArrangesAChildAtLeastAsWideAsItsMeasuredContent()
    {
        // A child whose content measures 100.003 pixels (for example a TextBlock) gets a slot of its desired width.
        // That slot must hold the whole content, or the text renders into 100 pixels and wraps or trims.
        UIRoot root = new(200, 200, 1);
        StackPanel panel = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        FixedElement child = new(new LayoutSize(100.003f, 10));
        panel.VisualChildren.Add(child);
        root.VisualChildren.Add(panel);

        root.ProcessFrame();

        Assert.Equal((101f, 101f), (child.DesiredSize.Width, child.ArrangedBounds.Width));
    }

    private static float ToDevicePixels(LayoutRect bounds, bool vertical, bool end)
    {
        float start = vertical ? bounds.Y : bounds.X;
        float edge = end ? start + (vertical ? bounds.Height : bounds.Width) : start;
        return MathF.Round(edge * 1.75f);
    }

    private static UIElement CreateSizedElement(float length, bool vertical)
    {
        return vertical
            ? new UIElement { Width = 10, Height = length }
            : new UIElement { Width = length, Height = 10 };
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
