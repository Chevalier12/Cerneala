using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.Tests.UI.Layout;

public sealed class GridTests
{
    [Fact]
    public void GridMeasuresFixedAutoAndStarDefinitions()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(20)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(10)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        FixedElement autoChild = new(new LayoutSize(30, 12));
        Grid.SetColumn(autoChild, 1);
        Grid.SetRow(autoChild, 1);
        grid.VisualChildren.Add(autoChild);

        LayoutSize desired = grid.Measure(new MeasureContext(new LayoutSize(100, 50)));

        Assert.Equal(new LayoutSize(100, 22), desired);
        Assert.Equal(new LayoutSize(30, 12), autoChild.DesiredSize);
    }

    [Fact]
    public void GridArrangesChildrenIntoCells()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(20)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(3)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(10)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
        FixedElement child = new(new LayoutSize(5, 5));
        Grid.SetColumn(child, 2);
        Grid.SetRow(child, 1);
        grid.VisualChildren.Add(child);
        grid.Measure(new MeasureContext(new LayoutSize(100, 50)));

        grid.Arrange(new ArrangeContext(new LayoutRect(3, 4, 100, 50)));

        Assert.Equal(new LayoutRect(43, 14, 60, 40), child.ArrangedBounds);
    }

    [Fact]
    public void GridSupportsColumnAndRowSpans()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(20)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(30)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(10)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(15)));
        FixedElement child = new(new LayoutSize(5, 5));
        Grid.SetColumnSpan(child, 2);
        Grid.SetRowSpan(child, 2);
        grid.VisualChildren.Add(child);
        grid.Measure(new MeasureContext(new LayoutSize(100, 100)));

        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.Equal(new LayoutRect(0, 0, 50, 25), child.ArrangedBounds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SameSlotArrangeAfterUnboundedMeasureUsesTheNewStarRowSizes(bool measureShorterFirst)
    {
        // Measured at (100, 80), the two * rows share the height by weight (40 and 40). Measured unbounded, each keeps
        // its content height (60 and 20). Both measures end at a desired size of 100 x 80, so only the newer measure
        // state tells the same-slot arrange where row 1 starts: Y 60, as for a Grid that was only measured unbounded.
        Grid grid = new();
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
        UIElement first = new() { Height = 60 };
        UIElement second = new() { Height = 20 };
        Grid.SetRow(second, 1);
        grid.VisualChildren.Add(first);
        grid.VisualChildren.Add(second);
        LayoutRect slot = new(0, 0, 100, 80);
        grid.Measure(new MeasureContext(new LayoutSize(100, 80)));
        grid.Arrange(new ArrangeContext(slot));
        if (measureShorterFirst)
        {
            grid.Measure(new MeasureContext(new LayoutSize(100, 50)));
        }

        grid.Measure(new MeasureContext(new LayoutSize(100, float.PositiveInfinity)));
        grid.Arrange(new ArrangeContext(slot));

        Assert.Equal(new LayoutSize(100, 80), grid.DesiredSize);
        Assert.Equal(60, second.ArrangedBounds.Y);
    }

    [Fact]
    public void LayoutKeepsStoredColumnPlacementWhenGridHasFewerColumns()
    {
        Grid grid = new();
        FixedElement child = new(new LayoutSize(5, 5));
        Grid.SetColumn(child, 2);
        Grid.SetColumnSpan(child, 2);
        grid.VisualChildren.Add(child);

        grid.Measure(new MeasureContext(new LayoutSize(100, 100)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.Equal(new LayoutRect(0, 0, 100, 100), child.ArrangedBounds);
        Assert.Equal(2, Grid.GetColumn(child));
        Assert.Equal(2, Grid.GetColumnSpan(child));

        for (int i = 0; i < 4; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(10)));
        }

        grid.Measure(new MeasureContext(new LayoutSize(100, 100)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.Equal(new LayoutRect(20, 0, 20, 100), child.ArrangedBounds);
        Assert.Equal(2, Grid.GetColumn(child));
        Assert.Equal(2, Grid.GetColumnSpan(child));
    }

    [Fact]
    public void LayoutKeepsStoredRowPlacementWhenGridHasFewerRows()
    {
        Grid grid = new();
        FixedElement child = new(new LayoutSize(5, 5));
        Grid.SetRow(child, 2);
        Grid.SetRowSpan(child, 2);
        grid.VisualChildren.Add(child);

        grid.Measure(new MeasureContext(new LayoutSize(100, 100)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.Equal(new LayoutRect(0, 0, 100, 100), child.ArrangedBounds);
        Assert.Equal(2, Grid.GetRow(child));
        Assert.Equal(2, Grid.GetRowSpan(child));

        for (int i = 0; i < 4; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(10)));
        }

        grid.Measure(new MeasureContext(new LayoutSize(100, 100)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.Equal(new LayoutRect(0, 20, 100, 20), child.ArrangedBounds);
        Assert.Equal(2, Grid.GetRow(child));
        Assert.Equal(2, Grid.GetRowSpan(child));
    }

    [Fact]
    public void GridRecomputesInfiniteStarRowAfterStarColumnsConstrainChildWidth()
    {
        StackPanel stack = new();
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        WidthSensitiveElement wrappingChild = new();
        grid.VisualChildren.Add(wrappingChild);
        stack.VisualChildren.Add(grid);

        stack.Measure(new MeasureContext(new LayoutSize(200, 100)));
        stack.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, stack.DesiredSize.Height)));

        Assert.Equal(60, grid.DesiredSize.Height);
        Assert.Equal(60, wrappingChild.DesiredSize.Height);
        Assert.Equal(wrappingChild.DesiredSize.Height, wrappingChild.ArrangedBounds.Height);
    }

    [Fact]
    public void InfiniteStarRowsInStackPanelKeepContentSizesAtArrange()
    {
        StackPanel stack = new();
        Grid grid = CreateTwoStarRowGrid(out FixedElement small, out FixedElement large);
        stack.VisualChildren.Add(grid);

        stack.Measure(new MeasureContext(new LayoutSize(200, 100)));
        stack.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 100)));

        Assert.Equal(40, grid.DesiredSize.Height);
        Assert.Equal(new LayoutRect(0, 0, 200, 10), small.ArrangedBounds);
        Assert.Equal(new LayoutRect(0, 10, 200, 30), large.ArrangedBounds);
    }

    [Fact]
    public void InfiniteStarRowsInAutoRowKeepContentSizesAtArrange()
    {
        Grid outer = new();
        outer.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        outer.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
        Grid inner = CreateTwoStarRowGrid(out FixedElement small, out FixedElement large);
        outer.VisualChildren.Add(inner);

        outer.Measure(new MeasureContext(new LayoutSize(200, 300)));
        outer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 300)));

        Assert.Equal(40, inner.DesiredSize.Height);
        Assert.Equal(new LayoutRect(0, 0, 200, 40), inner.ArrangedBounds);
        Assert.Equal(new LayoutRect(0, 0, 200, 10), small.ArrangedBounds);
        Assert.Equal(new LayoutRect(0, 10, 200, 30), large.ArrangedBounds);
    }

    [Theory]
    [InlineData(40, 10, 30)]
    [InlineData(50, 20, 30)]
    [InlineData(100, 50, 50)]
    [InlineData(30, 10, 30)]
    public void InfiniteStarRowsShareArrangeHeightByWeightWithoutShrinkingBelowContent(
        float finalHeight,
        float smallRowHeight,
        float largeRowHeight)
    {
        Grid grid = CreateTwoStarRowGrid(out FixedElement small, out FixedElement large);

        grid.Measure(new MeasureContext(new LayoutSize(200, float.PositiveInfinity)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, finalHeight)));

        Assert.Equal(new LayoutRect(0, 0, 200, smallRowHeight), small.ArrangedBounds);
        Assert.Equal(new LayoutRect(0, smallRowHeight, 200, largeRowHeight), large.ArrangedBounds);
    }

    [Fact]
    public void FiniteStarRowsShareArrangeHeightByWeightOnly()
    {
        Grid grid = CreateTwoStarRowGrid(out FixedElement small, out FixedElement large);

        grid.Measure(new MeasureContext(new LayoutSize(200, 40)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 40)));

        Assert.Equal(new LayoutRect(0, 0, 200, 20), small.ArrangedBounds);
        Assert.Equal(new LayoutRect(0, 20, 200, 20), large.ArrangedBounds);
    }

    [Fact]
    public void NestedStarGridsMeasureLeafOncePerConstraint()
    {
        Grid outer = new();
        Grid current = outer;
        for (int depth = 0; depth < 4; depth++)
        {
            current.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(10)));
            current.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
            Grid child = new();
            Grid.SetColumn(child, 1);
            current.VisualChildren.Add(child);
            current = child;
        }

        CountingElement leaf = new(new LayoutSize(40, 20));
        current.VisualChildren.Add(leaf);

        outer.Measure(new MeasureContext(new LayoutSize(320, 180)));

        Assert.Equal(1, leaf.MeasureCount);
        Assert.Equal(new LayoutSize(280, 180), leaf.LastAvailableSize);
    }

    [Fact]
    public void AutoRowSizesToBorderedStarGridContent()
    {
        Grid outer = new();
        outer.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        outer.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));

        Grid header = new();
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        FixedElement title = new(new LayoutSize(50, 16));
        FixedElement button = new(new LayoutSize(40, 20));
        Grid.SetColumn(button, 1);
        header.VisualChildren.Add(title);
        header.VisualChildren.Add(button);
        Cerneala.UI.Controls.Border headerBorder = new() { Padding = new Thickness(24, 18, 24, 18), Child = header };

        FixedElement body = new(new LayoutSize(10, 10));
        Grid.SetRow(body, 1);
        outer.VisualChildren.Add(headerBorder);
        outer.VisualChildren.Add(body);

        outer.Measure(new MeasureContext(new LayoutSize(200, 300)));
        outer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 300)));

        Assert.Equal(56, headerBorder.DesiredSize.Height);
        Assert.Equal(new LayoutRect(0, 0, 200, 56), headerBorder.ArrangedBounds);
        Assert.Equal(new LayoutRect(136, 18, 40, 20), button.ArrangedBounds);
        Assert.Equal(new LayoutRect(0, 56, 200, 244), body.ArrangedBounds);
    }

    [Fact]
    public void AutoColumnSizesToStarGridContent()
    {
        Grid outer = new();
        outer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        outer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));

        Grid inner = new();
        inner.VisualChildren.Add(new FixedElement(new LayoutSize(30, 20)));
        FixedElement filler = new(new LayoutSize(10, 10));
        Grid.SetColumn(filler, 1);
        outer.VisualChildren.Add(inner);
        outer.VisualChildren.Add(filler);

        outer.Measure(new MeasureContext(new LayoutSize(200, 300)));
        outer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 300)));

        Assert.Equal(30, inner.DesiredSize.Width);
        Assert.Equal(new LayoutRect(0, 0, 30, 300), inner.ArrangedBounds);
        Assert.Equal(new LayoutRect(30, 0, 170, 300), filler.ArrangedBounds);
    }

    [Fact]
    public void AutoRowChildInPixelColumnDoesNotOverflowStarRows()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(100)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
        WrappingElement wrapping = new(vertical: false);
        CountingElement starChild = new(new LayoutSize(10, 10));
        Grid.SetRow(starChild, 1);
        grid.VisualChildren.Add(wrapping);
        grid.VisualChildren.Add(starChild);

        LayoutSize desired = grid.Measure(new MeasureContext(new LayoutSize(800, 300)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 800, 300)));

        Assert.Equal(new LayoutSize(800, 300), desired);
        Assert.Equal(new LayoutRect(0, 0, 100, 80), wrapping.ArrangedBounds);
        Assert.Equal(new LayoutSize(100, 220), starChild.LastAvailableSize);
        Assert.Equal(new LayoutRect(0, 80, 100, 220), starChild.ArrangedBounds);
    }

    [Fact]
    public void AutoColumnChildInPixelRowDoesNotOverflowStarColumns()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(100)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
        WrappingElement wrapping = new(vertical: true);
        CountingElement starChild = new(new LayoutSize(10, 10));
        Grid.SetColumn(starChild, 1);
        grid.VisualChildren.Add(wrapping);
        grid.VisualChildren.Add(starChild);

        LayoutSize desired = grid.Measure(new MeasureContext(new LayoutSize(300, 800)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 300, 800)));

        Assert.Equal(new LayoutSize(300, 800), desired);
        Assert.Equal(new LayoutRect(0, 0, 80, 100), wrapping.ArrangedBounds);
        Assert.Equal(new LayoutSize(220, 100), starChild.LastAvailableSize);
        Assert.Equal(new LayoutRect(80, 0, 220, 100), starChild.ArrangedBounds);
    }

    [Fact]
    public void AutoRowChildInStarColumnDoesNotOverflowStarRows()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(700)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
        WrappingElement wrapping = new(vertical: false);
        CountingElement starChild = new(new LayoutSize(10, 10));
        Grid.SetRow(starChild, 1);
        grid.VisualChildren.Add(wrapping);
        grid.VisualChildren.Add(starChild);

        LayoutSize desired = grid.Measure(new MeasureContext(new LayoutSize(800, 300)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 800, 300)));

        Assert.Equal(new LayoutSize(800, 300), desired);
        Assert.Equal(new LayoutRect(0, 0, 100, 80), wrapping.ArrangedBounds);
        Assert.Equal(new LayoutSize(100, 220), starChild.LastAvailableSize);
        Assert.Equal(new LayoutRect(0, 80, 100, 220), starChild.ArrangedBounds);
    }

    [Fact]
    public void AutoColumnChildInStarRowDoesNotOverflowStarColumns()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(700)));
        WrappingElement wrapping = new(vertical: true);
        CountingElement starChild = new(new LayoutSize(10, 10));
        Grid.SetColumn(starChild, 1);
        grid.VisualChildren.Add(wrapping);
        grid.VisualChildren.Add(starChild);

        LayoutSize desired = grid.Measure(new MeasureContext(new LayoutSize(300, 800)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 300, 800)));

        Assert.Equal(new LayoutSize(300, 800), desired);
        Assert.Equal(new LayoutRect(0, 0, 80, 100), wrapping.ArrangedBounds);
        Assert.Equal(new LayoutSize(220, 100), starChild.LastAvailableSize);
        Assert.Equal(new LayoutRect(80, 0, 220, 100), starChild.ArrangedBounds);
    }

    [Fact]
    public void AutoRowChildSpanningAutoAndPixelColumnsDoesNotOverflowStarRows()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(100)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
        ItemWrappingElement items = new(vertical: true);
        Grid.SetRow(items, 1);
        WrappingElement wrapping = new(vertical: false);
        Grid.SetColumnSpan(wrapping, 2);
        grid.VisualChildren.Add(items);
        grid.VisualChildren.Add(wrapping);

        LayoutSize desired = grid.Measure(new MeasureContext(new LayoutSize(800, 300)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 800, 300)));

        Assert.Equal(new LayoutSize(800, 300), desired);
        Assert.Equal(new LayoutRect(0, 0, 140, 60), wrapping.ArrangedBounds);
        Assert.Equal(240, items.LastAvailableSize.Height);
        Assert.Equal(new LayoutRect(0, 60, 40, 240), items.ArrangedBounds);
    }

    [Fact]
    public void AutoColumnChildSpanningAutoAndPixelRowsDoesNotOverflowStarColumns()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(100)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
        ItemWrappingElement items = new(vertical: false);
        Grid.SetColumn(items, 1);
        WrappingElement wrapping = new(vertical: true);
        Grid.SetRowSpan(wrapping, 2);
        grid.VisualChildren.Add(items);
        grid.VisualChildren.Add(wrapping);

        LayoutSize desired = grid.Measure(new MeasureContext(new LayoutSize(300, 800)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 300, 800)));

        Assert.Equal(new LayoutSize(300, 800), desired);
        Assert.Equal(new LayoutRect(0, 0, 60, 140), wrapping.ArrangedBounds);
        Assert.Equal(240, items.LastAvailableSize.Width);
        Assert.Equal(new LayoutRect(60, 0, 240, 40), items.ArrangedBounds);
    }

    // At these heights the Auto column's child is wider at the re-sized star row than at the provisional one.
    [Theory]
    [InlineData(220)]
    [InlineData(230)]
    public void AutoRowChildSpanningAutoAndPixelColumnsDoesNotOverflowStarColumnsWhenAutoColumnGrows(float height)
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(100)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
        ItemWrappingElement items = new(vertical: true);
        Grid.SetRow(items, 1);
        WrappingElement wrapping = new(vertical: false);
        Grid.SetColumnSpan(wrapping, 2);
        grid.VisualChildren.Add(items);
        grid.VisualChildren.Add(wrapping);

        LayoutSize desired = grid.Measure(new MeasureContext(new LayoutSize(800, height)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 800, height)));

        Assert.Equal(new LayoutSize(800, height), desired);
        Assert.Equal(new LayoutRect(0, 0, 160, 60), wrapping.ArrangedBounds);
        Assert.Equal(160, wrapping.LastAvailableSize.Width);
        Assert.Equal(height - 60, items.LastAvailableSize.Height);
        Assert.Equal(new LayoutRect(0, 60, 60, height - 60), items.ArrangedBounds);
    }

    // At these widths the Auto row's child is taller at the re-sized star column than at the provisional one.
    [Theory]
    [InlineData(220)]
    [InlineData(230)]
    public void AutoColumnChildSpanningAutoAndPixelRowsDoesNotOverflowStarRowsWhenAutoRowGrows(float width)
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(100)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
        ItemWrappingElement items = new(vertical: false);
        Grid.SetColumn(items, 1);
        WrappingElement wrapping = new(vertical: true);
        Grid.SetRowSpan(wrapping, 2);
        grid.VisualChildren.Add(items);
        grid.VisualChildren.Add(wrapping);

        LayoutSize desired = grid.Measure(new MeasureContext(new LayoutSize(width, 800)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, width, 800)));

        Assert.Equal(new LayoutSize(width, 800), desired);
        Assert.Equal(new LayoutRect(0, 0, 60, 160), wrapping.ArrangedBounds);
        Assert.Equal(160, wrapping.LastAvailableSize.Height);
        Assert.Equal(width - 60, items.LastAvailableSize.Width);
        Assert.Equal(new LayoutRect(60, 0, width - 60, 60), items.ArrangedBounds);
    }

    [Fact]
    public void AutoColumnChildThatNarrowsAtItsResolvedHeightDoesNotOverflowStarRows()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(50)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(20)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
        AspectElement aspect = new();
        Grid.SetRowSpan(aspect, 2);
        WrappingElement wrapping = new(vertical: false);
        Grid.SetColumnSpan(wrapping, 2);
        grid.VisualChildren.Add(aspect);
        grid.VisualChildren.Add(wrapping);

        LayoutSize desired = grid.Measure(new MeasureContext(new LayoutSize(300, 300)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 300, 300)));

        Assert.Equal(new LayoutSize(300, 300), desired);
        Assert.Equal(new LayoutRect(0, 0, 125, 80), wrapping.ArrangedBounds);
        Assert.Equal(new LayoutRect(0, 0, 75, 100), aspect.ArrangedBounds);
    }

    [Fact]
    public void CyclicAutoColumnChildInStarRowDoesNotOverflowStarColumns()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
        WrappingElement wrapping = new(vertical: true);
        Grid.SetRow(wrapping, 1);
        CountingElement autoRowChild = new(new LayoutSize(50, 150));
        Grid.SetColumn(autoRowChild, 1);
        grid.VisualChildren.Add(wrapping);
        grid.VisualChildren.Add(autoRowChild);

        LayoutSize desired = grid.Measure(new MeasureContext(new LayoutSize(300, 300)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 300, 300)));

        Assert.Equal(new LayoutSize(300, 300), desired);
        Assert.Equal(new LayoutRect(0, 150, 60, 150), wrapping.ArrangedBounds);
        Assert.Equal(240, autoRowChild.LastAvailableSize.Width);
        Assert.Equal(new LayoutRect(60, 0, 240, 150), autoRowChild.ArrangedBounds);
    }

    [Fact]
    public void CyclicAutoRowChildInStarColumnFollowsGrownAutoColumn()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
        WrappingElement columnWrapping = new(vertical: true);
        Grid.SetRow(columnWrapping, 1);
        WrappingElement rowWrapping = new(vertical: false);
        Grid.SetColumn(rowWrapping, 1);
        grid.VisualChildren.Add(columnWrapping);
        grid.VisualChildren.Add(rowWrapping);

        LayoutSize desired = grid.Measure(new MeasureContext(new LayoutSize(250, 220)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 250, 220)));

        Assert.Equal(new LayoutSize(250, 220), desired);
        Assert.Equal(new LayoutRect(0, 60, 60, 160), columnWrapping.ArrangedBounds);
        Assert.Equal(new LayoutSize(190, 60), rowWrapping.DesiredSize);
        Assert.Equal(new LayoutRect(60, 0, 190, 60), rowWrapping.ArrangedBounds);
    }

    [Fact]
    public void ChildSpanningTwoAutoColumnsSizesThemEqually()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        FixedElement spanning = new(new LayoutSize(100, 20));
        Grid.SetColumnSpan(spanning, 2);
        FixedElement marker = new(LayoutSize.Zero);
        Grid.SetColumn(marker, 1);
        grid.VisualChildren.Add(spanning);
        grid.VisualChildren.Add(marker);

        LayoutSize desired = grid.Measure(new MeasureContext(new LayoutSize(300, 100)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 300, 100)));

        Assert.Equal(new LayoutSize(100, 100), desired);
        Assert.Equal(new LayoutRect(0, 0, 100, 100), spanning.ArrangedBounds);
        Assert.Equal(new LayoutRect(50, 0, 50, 100), marker.ArrangedBounds);
    }

    [Fact]
    public void ChildSpanningTwoAutoRowsSizesThemEqually()
    {
        Grid grid = new();
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        FixedElement spanning = new(new LayoutSize(20, 100));
        Grid.SetRowSpan(spanning, 2);
        FixedElement marker = new(LayoutSize.Zero);
        Grid.SetRow(marker, 1);
        grid.VisualChildren.Add(spanning);
        grid.VisualChildren.Add(marker);

        LayoutSize desired = grid.Measure(new MeasureContext(new LayoutSize(100, 300)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 300)));

        Assert.Equal(new LayoutSize(100, 100), desired);
        Assert.Equal(new LayoutRect(0, 0, 100, 100), spanning.ArrangedBounds);
        Assert.Equal(new LayoutRect(0, 50, 100, 50), marker.ArrangedBounds);
    }

    [Fact]
    public void ChildSpanningAutoColumnsAddsOnlyWhatSingleColumnChildrenLeave()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        FixedElement spanning = new(new LayoutSize(100, 20));
        Grid.SetColumnSpan(spanning, 2);
        FixedElement single = new(new LayoutSize(80, 20));
        FixedElement marker = new(LayoutSize.Zero);
        Grid.SetColumn(marker, 1);
        grid.VisualChildren.Add(spanning);
        grid.VisualChildren.Add(single);
        grid.VisualChildren.Add(marker);

        LayoutSize desired = grid.Measure(new MeasureContext(new LayoutSize(300, 100)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 300, 100)));

        Assert.Equal(100, desired.Width);
        Assert.Equal(new LayoutRect(0, 0, 90, 100), single.ArrangedBounds);
        Assert.Equal(new LayoutRect(90, 0, 10, 100), marker.ArrangedBounds);
    }

    [Fact]
    public void NarrowerAutoColumnSpanSizesItsColumnsBeforeWiderSpan()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        FixedElement wide = new(new LayoutSize(90, 20));
        Grid.SetColumnSpan(wide, 3);
        FixedElement narrow = new(new LayoutSize(100, 20));
        Grid.SetColumnSpan(narrow, 2);
        FixedElement marker = new(LayoutSize.Zero);
        Grid.SetColumn(marker, 2);
        grid.VisualChildren.Add(wide);
        grid.VisualChildren.Add(narrow);
        grid.VisualChildren.Add(marker);

        LayoutSize desired = grid.Measure(new MeasureContext(new LayoutSize(300, 100)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 300, 100)));

        Assert.Equal(100, desired.Width);
        Assert.Equal(new LayoutRect(0, 0, 100, 100), narrow.ArrangedBounds);
        Assert.Equal(new LayoutRect(100, 0, 0, 100), marker.ArrangedBounds);
    }

    [Fact]
    public void ChildSpanningAutoColumnsIsSharedAgainAfterSingleColumnChildGrowsAtResolvedHeight()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(50)));
        WrappingElement wrapping = new(vertical: true);
        Grid.SetRowSpan(wrapping, 2);
        FixedElement spanning = new(new LayoutSize(100, 20));
        Grid.SetColumnSpan(spanning, 2);
        FixedElement marker = new(LayoutSize.Zero);
        Grid.SetColumn(marker, 1);
        grid.VisualChildren.Add(wrapping);
        grid.VisualChildren.Add(spanning);
        grid.VisualChildren.Add(marker);

        LayoutSize desired = grid.Measure(new MeasureContext(new LayoutSize(300, 300)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 300, 300)));

        Assert.Equal(new LayoutSize(120, 70), desired);
        Assert.Equal(new LayoutRect(0, 0, 120, 70), wrapping.ArrangedBounds);
        Assert.Equal(new LayoutRect(0, 0, 120, 20), spanning.ArrangedBounds);
        Assert.Equal(new LayoutRect(120, 0, 0, 20), marker.ArrangedBounds);
    }

    [Fact]
    public void ChildSpanningAutoAndStarColumnsOnInfiniteWidthSizesOnlyTheStarColumn()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        FixedElement spanning = new(new LayoutSize(100, 20));
        Grid.SetColumnSpan(spanning, 2);
        FixedElement marker = new(LayoutSize.Zero);
        Grid.SetColumn(marker, 1);
        grid.VisualChildren.Add(spanning);
        grid.VisualChildren.Add(marker);

        LayoutSize desired = grid.Measure(new MeasureContext(new LayoutSize(float.PositiveInfinity, 100)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.Equal(100, desired.Width);
        Assert.Equal(new LayoutRect(0, 0, 100, 100), spanning.ArrangedBounds);
        Assert.Equal(new LayoutRect(0, 0, 100, 100), marker.ArrangedBounds);
    }

    [Fact]
    public void ChildSpanningAutoAndPixelColumnsKeepsAutoColumnUnsized()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(100)));
        CountingElement spanning = new(new LayoutSize(400, 20));
        Grid.SetColumnSpan(spanning, 2);
        FixedElement marker = new(LayoutSize.Zero);
        Grid.SetColumn(marker, 1);
        grid.VisualChildren.Add(spanning);
        grid.VisualChildren.Add(marker);

        LayoutSize desired = grid.Measure(new MeasureContext(new LayoutSize(800, 100)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 800, 100)));

        Assert.Equal(100, desired.Width);
        Assert.Equal(100, spanning.LastAvailableSize.Width);
        Assert.Equal(new LayoutRect(0, 0, 100, 100), marker.ArrangedBounds);
    }

    [Fact]
    public void ChangingAttachedGridPlacementQueuesGridLayout()
    {
        UIRoot root = new(100, 100);
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(20)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(20)));
        FixedElement child = new(new LayoutSize(5, 5));
        root.VisualChildren.Add(grid);
        grid.VisualChildren.Add(child);
        root.ProcessFrame();

        Grid.SetColumn(child, 1);

        Assert.Contains(grid, root.LayoutQueue.SnapshotMeasure());
        Assert.Contains(grid, root.LayoutQueue.SnapshotArrange());
    }

    [Fact]
    public void GridLengthRejectsInvalidValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GridLength(-1).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => GridLength.Stars(float.PositiveInfinity).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new GridLength(float.NaN, GridUnitType.Auto).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => Grid.SetRow(new UIElement(), -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Grid.SetColumnSpan(new UIElement(), 0));
    }

    // Edges are in device pixels: 100 px is 100, 125 and 150 device pixels at scales 1, 1.25 and 1.5.
    [Theory]
    [InlineData(1f, "0..34, 34..67, 67..100")]
    [InlineData(1.25f, "0..42, 42..84, 84..125")]
    [InlineData(1.5f, "0..50, 50..100, 100..150")]
    public void LayoutRoundingKeepsEqualStarColumnsContiguousInsideTheGrid(float scale, string expectedEdges)
    {
        Grid grid = new();
        FixedElement[] cells = new FixedElement[3];
        for (int i = 0; i < cells.Length; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
            cells[i] = new FixedElement(new LayoutSize(1, 1));
            Grid.SetColumn(cells[i], i);
            grid.VisualChildren.Add(cells[i]);
        }

        LayoutRounding rounding = LayoutRounding.ForScale(scale);
        grid.Measure(new MeasureContext(new LayoutSize(100, 20), rounding));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 20), rounding));

        Assert.Equal(expectedEdges, GetDeviceEdges(cells, scale, columns: true));
    }

    [Theory]
    [InlineData(1f, "0..34, 34..67, 67..100")]
    [InlineData(1.25f, "0..42, 42..84, 84..125")]
    [InlineData(1.5f, "0..50, 50..100, 100..150")]
    public void LayoutRoundingKeepsEqualStarRowsContiguousInsideTheGrid(float scale, string expectedEdges)
    {
        Grid grid = new();
        FixedElement[] cells = new FixedElement[3];
        for (int i = 0; i < cells.Length; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
            cells[i] = new FixedElement(new LayoutSize(1, 1));
            Grid.SetRow(cells[i], i);
            grid.VisualChildren.Add(cells[i]);
        }

        LayoutRounding rounding = LayoutRounding.ForScale(scale);
        grid.Measure(new MeasureContext(new LayoutSize(20, 100), rounding));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 100), rounding));

        Assert.Equal(expectedEdges, GetDeviceEdges(cells, scale, columns: false));
    }

    [Fact]
    public void LayoutRoundingKeepsStarColumnsContiguousWhenAColumnIsNotAWholeLogicalPixel()
    {
        // 113 device pixels at scale 1.75 split into 57 and 56; 57 device pixels are 32.571428 px.
        const float scale = 1.75f;
        Grid grid = new();
        FixedElement[] cells = new FixedElement[2];
        for (int i = 0; i < cells.Length; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
            cells[i] = new FixedElement(new LayoutSize(1, 1));
            Grid.SetColumn(cells[i], i);
            grid.VisualChildren.Add(cells[i]);
        }

        LayoutRounding rounding = LayoutRounding.ForScale(scale);
        float width = 113 / scale;
        grid.Measure(new MeasureContext(new LayoutSize(width, 20), rounding));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, width, 20), rounding));

        Assert.Equal("0..57, 57..113", GetDeviceEdges(cells, scale, columns: true));
    }

    [Fact]
    public void LayoutRoundingKeepsPixelAndAutoDefinitionsAndSpansOnTheRoundedEdges()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(20)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(2)));
        FixedElement pixel = new(new LayoutSize(1, 1));
        FixedElement auto = new(new LayoutSize(13, 1));
        Grid.SetColumn(auto, 1);
        FixedElement oneStar = new(new LayoutSize(1, 1));
        Grid.SetColumn(oneStar, 2);
        FixedElement twoStar = new(new LayoutSize(1, 1));
        Grid.SetColumn(twoStar, 3);
        FixedElement spanning = new(new LayoutSize(1, 1));
        Grid.SetColumn(spanning, 1);
        Grid.SetColumnSpan(spanning, 2);
        grid.VisualChildren.Add(pixel);
        grid.VisualChildren.Add(auto);
        grid.VisualChildren.Add(oneStar);
        grid.VisualChildren.Add(twoStar);
        grid.VisualChildren.Add(spanning);

        // 100 - 20 - 13 = 67 px of stars: 22.33 and 44.67 round to 22 and 45.
        LayoutRounding rounding = LayoutRounding.Enabled;
        grid.Measure(new MeasureContext(new LayoutSize(100, 20), rounding));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 20), rounding));

        Assert.Equal(
            "0..20, 20..33, 33..55, 55..100, 20..55",
            GetDeviceEdges([pixel, auto, oneStar, twoStar, spanning], 1, columns: true));
    }

    [Fact]
    public void LayoutRoundingKeepsStarCellsContiguousWhenTheGridOriginIsOnAHalfPixel()
    {
        // A 100 px Grid centered in a 101 px slot starts at 0.5 px; its own bounds round to 0..100.
        Grid grid = new()
        {
            Width = 100,
            Height = 100,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        FixedElement[] cells = new FixedElement[3];
        for (int i = 0; i < cells.Length; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
            cells[i] = new FixedElement(new LayoutSize(1, 1));
            Grid.SetColumn(cells[i], i);
            Grid.SetRow(cells[i], i);
            grid.VisualChildren.Add(cells[i]);
        }

        LayoutRounding rounding = LayoutRounding.Enabled;
        grid.Measure(new MeasureContext(new LayoutSize(101, 101), rounding));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 101, 101), rounding));

        Assert.Equal("0..100", GetDeviceEdges([grid], 1, columns: true));
        Assert.Equal("0..100", GetDeviceEdges([grid], 1, columns: false));
        Assert.Equal("0..34, 34..67, 67..100", GetDeviceEdges(cells, 1, columns: true));
        Assert.Equal("0..34, 34..67, 67..100", GetDeviceEdges(cells, 1, columns: false));
    }

    // A cell child is measured at the rounded size it is then arranged at: three * definitions in 100 px are
    // 34, 33 and 33 device pixels at scale 1, and 42, 42 and 41 device pixels at scale 1.25.
    [Theory]
    [InlineData(1f, true, "34, 33, 33")]
    [InlineData(1f, false, "34, 33, 33")]
    [InlineData(1.25f, true, "42, 42, 41")]
    [InlineData(1.25f, false, "42, 42, 41")]
    public void LayoutRoundingMeasuresStarCellChildrenAtTheirArrangedSize(float scale, bool columns, string expectedDeviceSizes)
    {
        Grid grid = new();
        SizeRecordingElement[] cells = new SizeRecordingElement[3];
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = new SizeRecordingElement();
            if (columns)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
                Grid.SetColumn(cells[i], i);
            }
            else
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
                Grid.SetRow(cells[i], i);
            }

            grid.VisualChildren.Add(cells[i]);
        }

        LayoutRounding rounding = LayoutRounding.ForScale(scale);
        LayoutSize size = columns ? new LayoutSize(100, 20) : new LayoutSize(20, 100);
        grid.Measure(new MeasureContext(size, rounding));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, size.Width, size.Height), rounding));

        Assert.Equal(cells.Select(cell => cell.ArrangeSize), cells.Select(cell => cell.MeasureSize));
        Assert.Equal(
            expectedDeviceSizes,
            string.Join(", ", cells.Select(cell => MathF.Round((columns ? cell.MeasureSize.Width : cell.MeasureSize.Height) * scale))));
    }

    [Fact]
    public void LayoutRoundingMeasuresAutoRowChildrenAtTheRoundedWidthOfTheirStarColumn()
    {
        // The first of three * columns in 100 px is 34 px wide once rounded. Measured at the unrounded 33.33 px,
        // which the child receives as 33, the child would ask for 60 px and leave the Auto row 60 px high.
        Grid grid = new();
        for (int i = 0; i < 3; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        }

        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        WidthThresholdElement child = new();
        grid.VisualChildren.Add(child);

        LayoutRounding rounding = LayoutRounding.ForScale(1);
        LayoutSize desired = grid.Measure(new MeasureContext(new LayoutSize(100, 100), rounding));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100), rounding));

        Assert.Equal(20, desired.Height);
        Assert.Equal(new LayoutRect(0, 0, 34, 20), child.ArrangedBounds);
    }

    // Three 33 px pixel rows at scale 1.25 are 41.25 device pixels each and are rounded to 42, 41 and 41. A child of
    // the Auto column in the first row is measured at 42 device pixels in every pass, not at the 41 that its unrounded
    // 41.25 device pixels round to on their own.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LayoutRoundingMeasuresAutoChildrenInPixelSpansAtTheirArrangedSize(bool columns)
    {
        Grid grid = new();
        AddDefinition(grid, columns, GridLength.Auto);
        AddDefinition(grid, columns, GridLength.Stars(1));
        for (int i = 0; i < 3; i++)
        {
            AddDefinition(grid, !columns, GridLength.Pixels(33));
        }

        MeasureRecordingElement child = new();
        grid.VisualChildren.Add(child);

        LayoutRounding rounding = LayoutRounding.ForScale(1.25f);
        LayoutSize size = columns ? new LayoutSize(200, 99) : new LayoutSize(99, 200);
        grid.Measure(new MeasureContext(size, rounding));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, size.Width, size.Height), rounding));

        float arranged = columns ? child.ArrangeSize.Height : child.ArrangeSize.Width;
        Assert.Equal(42, MathF.Round(arranged * 1.25f));
        Assert.All(child.MeasureSizes, measured => Assert.Equal(arranged, columns ? measured.Height : measured.Width));
    }

    // Two 33.6 px pixel rows are rounded to 34 and 33 px, and with a third, Auto row holding a 10.8 px child to 34, 33
    // and 11 px. The Auto-column child in the second row asks for 70 px at 33 px but for 50 px at 34 px. Measured at
    // 34 px first, it would leave the * column measured at 150 px and then arranged at 130 px.
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void LayoutRoundingMeasuresTheStarSiblingOfAnAutoChildInAPixelSpanAtItsArrangedSize(
        bool columns,
        bool autoCrossDefinition)
    {
        Grid grid = new();
        AddDefinition(grid, columns, GridLength.Auto);
        AddDefinition(grid, columns, GridLength.Stars(1));
        AddDefinition(grid, !columns, GridLength.Pixels(33.6f));
        AddDefinition(grid, !columns, GridLength.Pixels(33.6f));
        CrossThresholdElement autoChild = new(columns);
        SetCell(autoChild, columns, index: 0, crossIndex: 1);
        MeasureRecordingElement starChild = new();
        SetCell(starChild, columns, index: 1, crossIndex: 1);
        grid.VisualChildren.Add(autoChild);
        grid.VisualChildren.Add(starChild);
        if (autoCrossDefinition)
        {
            AddDefinition(grid, !columns, GridLength.Auto);
            FixedElement filler = new(columns ? new LayoutSize(1, 10.8f) : new LayoutSize(10.8f, 1));
            SetCell(filler, columns, index: 1, crossIndex: 2);
            grid.VisualChildren.Add(filler);
        }

        LayoutRounding rounding = LayoutRounding.ForScale(1);
        LayoutSize size = columns ? new LayoutSize(200, 100) : new LayoutSize(100, 200);
        LayoutSize desired = grid.Measure(new MeasureContext(size, rounding));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, size.Width, size.Height), rounding));

        Assert.Equal(starChild.ArrangeSize, starChild.MeasureSizes[^1]);
        Assert.Equal(130, columns ? starChild.ArrangeSize.Width : starChild.ArrangeSize.Height);
        Assert.Equal(70, columns ? autoChild.ArrangedBounds.Width : autoChild.ArrangedBounds.Height);
        Assert.Equal(200, columns ? desired.Width : desired.Height);
    }

    // Two 33.6 px pixel rows and two Auto rows that share a 21 px child (10.5 px each) are rounded to 34, 34, 10 and
    // 10 px. Without the Auto rows' sizes, the two pixel rows alone round to 34 and 33 px. The Auto-column child in the
    // second row asks for 70 px below 33.5 px and for 50 px at 33.5 px or more, so it is arranged in a 50 px column
    // next to a 150 px * column, as when it is measured at the unrounded 33.6 px, which it receives as 34 px. A measure
    // at 33 px must not leave 70 px as a minimum.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LayoutRoundingKeepsNoAutoSizeFromAPixelSpanMeasuredBelowItsArrangedSize(bool columns)
    {
        Grid grid = new();
        AddDefinition(grid, columns, GridLength.Auto);
        AddDefinition(grid, columns, GridLength.Stars(1));
        AddDefinition(grid, !columns, GridLength.Pixels(33.6f));
        AddDefinition(grid, !columns, GridLength.Pixels(33.6f));
        AddDefinition(grid, !columns, GridLength.Auto);
        AddDefinition(grid, !columns, GridLength.Auto);
        CrossThresholdElement autoChild = new(columns, threshold: 33.5f);
        SetCell(autoChild, columns, index: 0, crossIndex: 1);
        MeasureRecordingElement starChild = new();
        SetCell(starChild, columns, index: 1, crossIndex: 1);
        FixedElement filler = new(columns ? new LayoutSize(1, 21) : new LayoutSize(21, 1));
        SetCell(filler, columns, index: 1, crossIndex: 2);
        if (columns)
        {
            Grid.SetRowSpan(filler, 2);
        }
        else
        {
            Grid.SetColumnSpan(filler, 2);
        }

        grid.VisualChildren.Add(autoChild);
        grid.VisualChildren.Add(starChild);
        grid.VisualChildren.Add(filler);

        LayoutRounding rounding = LayoutRounding.ForScale(1);
        LayoutSize size = columns ? new LayoutSize(200, 100) : new LayoutSize(100, 200);
        LayoutSize desired = grid.Measure(new MeasureContext(size, rounding));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, size.Width, size.Height), rounding));

        Assert.Equal(34, columns ? autoChild.ArrangedBounds.Height : autoChild.ArrangedBounds.Width);
        Assert.Equal(50, columns ? autoChild.ArrangedBounds.Width : autoChild.ArrangedBounds.Height);
        Assert.Equal(150, columns ? starChild.ArrangeSize.Width : starChild.ArrangeSize.Height);
        Assert.Equal(starChild.ArrangeSize, starChild.MeasureSizes[^1]);
        Assert.Equal(200, columns ? desired.Width : desired.Height);
    }

    // Two 33.6 px pixel rows are rounded to 34 and 33 px, with or without a third, empty Auto row (0 px). The Auto-column
    // child in the second row asks for 70 px at 33.5 px or more and for 50 px below that, so it is arranged in a 50 px
    // column at its 33 px height in both grids. Without the Auto row, the rows are rounded before they are resolved and
    // the child is measured at 33 px only. With it, the child is first measured at the unrounded 33.6 px, which it
    // receives as 34 px. A measure at 34 px must not leave 70 px as a minimum.
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void LayoutRoundingKeepsNoAutoSizeFromAPixelSpanMeasuredAboveItsArrangedSize(
        bool columns,
        bool emptyAutoCrossDefinition)
    {
        Grid grid = new();
        AddDefinition(grid, columns, GridLength.Auto);
        AddDefinition(grid, columns, GridLength.Stars(1));
        AddDefinition(grid, !columns, GridLength.Pixels(33.6f));
        AddDefinition(grid, !columns, GridLength.Pixels(33.6f));
        if (emptyAutoCrossDefinition)
        {
            AddDefinition(grid, !columns, GridLength.Auto);
        }

        CrossThresholdElement autoChild = new(columns, threshold: 33.5f, atThreshold: 70, belowThreshold: 50);
        SetCell(autoChild, columns, index: 0, crossIndex: 1);
        MeasureRecordingElement starChild = new();
        SetCell(starChild, columns, index: 1, crossIndex: 1);
        grid.VisualChildren.Add(autoChild);
        grid.VisualChildren.Add(starChild);

        LayoutRounding rounding = LayoutRounding.ForScale(1);
        LayoutSize size = columns ? new LayoutSize(200, 100) : new LayoutSize(100, 200);
        LayoutSize desired = grid.Measure(new MeasureContext(size, rounding));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, size.Width, size.Height), rounding));

        Assert.Equal(33, columns ? autoChild.ArrangedBounds.Height : autoChild.ArrangedBounds.Width);
        Assert.Equal(50, columns ? autoChild.ArrangedBounds.Width : autoChild.ArrangedBounds.Height);
        Assert.Equal(150, columns ? starChild.ArrangeSize.Width : starChild.ArrangeSize.Height);
        Assert.Equal(starChild.ArrangeSize, starChild.MeasureSizes[^1]);
        Assert.Equal(200, columns ? desired.Width : desired.Height);
    }

    // Rows 33.6, 33.6 and * in a 100 px high grid are rounded to 34, 33 and 33 px. The Auto-column child in the second
    // row is twice as wide as its available height, so at its 33 px height it asks for 66 px: the Auto column is 66 px
    // and the * column 134 px, not 68 and 132 px from the first measure at 34 px.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LayoutRoundingSizesAnAutoDefinitionFromAPixelSpanChildAtItsArrangedSize(bool columns)
    {
        Grid grid = new();
        AddDefinition(grid, columns, GridLength.Auto);
        AddDefinition(grid, columns, GridLength.Stars(1));
        AddDefinition(grid, !columns, GridLength.Pixels(33.6f));
        AddDefinition(grid, !columns, GridLength.Pixels(33.6f));
        AddDefinition(grid, !columns, GridLength.Stars(1));
        CrossRatioElement autoChild = new(columns);
        SetCell(autoChild, columns, index: 0, crossIndex: 1);
        MeasureRecordingElement starChild = new();
        SetCell(starChild, columns, index: 1, crossIndex: 1);
        grid.VisualChildren.Add(autoChild);
        grid.VisualChildren.Add(starChild);

        LayoutRounding rounding = LayoutRounding.ForScale(1);
        LayoutSize size = columns ? new LayoutSize(200, 100) : new LayoutSize(100, 200);
        grid.Measure(new MeasureContext(size, rounding));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, size.Width, size.Height), rounding));

        Assert.Equal(
            (33f, 66f, 66f, 134f),
            columns
                ? (autoChild.ArrangedBounds.Height, autoChild.ArrangedBounds.Width, autoChild.DesiredSize.Width, starChild.ArrangeSize.Width)
                : (autoChild.ArrangedBounds.Width, autoChild.ArrangedBounds.Height, autoChild.DesiredSize.Height, starChild.ArrangeSize.Height));
    }

    // Columns 33.6, 33.6 and Auto are rounded to 34, 33 and 0 px. The 0 x 1 child of the Auto column in the * row makes
    // the grid content-size its rows first, before its columns are resolved. The Auto-row child in the second column
    // asks for 70 px at a width of 33.5 px or more and for 50 px below that, so at its 33 px width the row is 50 px high.
    [Fact]
    public void LayoutRoundingKeepsNoAutoRowSizeFromAPixelSpanMeasuredAboveItsArrangedWidthWhenRowsAreSizedFirst()
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(33.6f)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(33.6f)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
        CrossThresholdElement autoChild = new(columns: false, threshold: 33.5f, atThreshold: 70, belowThreshold: 50);
        Grid.SetColumn(autoChild, 1);
        FixedElement marker = new(new LayoutSize(0, 1));
        Grid.SetColumn(marker, 2);
        Grid.SetRow(marker, 1);
        grid.VisualChildren.Add(autoChild);
        grid.VisualChildren.Add(marker);

        LayoutRounding rounding = LayoutRounding.ForScale(1);
        grid.Measure(new MeasureContext(new LayoutSize(200, 100), rounding));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 100), rounding));

        Assert.Equal(new LayoutRect(34, 0, 33, 50), autoChild.ArrangedBounds);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LayoutRoundingDesiresTheSumOfAutoDefinitionDevicePixelsWhenTheFloatSumIsOneStepAboveIt(bool columns)
    {
        // At scale 1.75 the Auto definitions are 57 / 1.75f and 1 / 1.75f. Their float sum is one step above 58 / 1.75f,
        // and rounding that sum up must not add a 59th device pixel that leaves a gap before the next sibling.
        UIRoot root = new(200, 200, 1.75f);
        StackPanel outer = new()
        {
            Orientation = columns ? Orientation.Horizontal : Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        Grid grid = new();
        AddDefinition(grid, columns, GridLength.Auto);
        AddDefinition(grid, columns, GridLength.Auto);
        UIElement first = columns ? new UIElement { Width = 32.5f, Height = 10 } : new UIElement { Width = 10, Height = 32.5f };
        UIElement second = columns ? new UIElement { Width = 0.5f, Height = 10 } : new UIElement { Width = 10, Height = 0.5f };
        SetCell(second, columns, 1, 0);
        UIElement next = new() { Width = 10, Height = 10 };
        grid.VisualChildren.Add(first);
        grid.VisualChildren.Add(second);
        outer.VisualChildren.Add(grid);
        outer.VisualChildren.Add(next);
        root.VisualChildren.Add(outer);

        root.ProcessFrame();

        float desired = columns ? grid.DesiredSize.Width : grid.DesiredSize.Height;
        float nextStart = columns ? next.ArrangedBounds.X : next.ArrangedBounds.Y;
        Assert.Equal(
            ("0..57, 57..58", 58f, 58f),
            (GetDeviceEdges([first, second], 1.75f, columns), MathF.Round(desired * 1.75f), MathF.Round(nextStart * 1.75f)));
    }

    private static void AddDefinition(Grid grid, bool columns, GridLength length)
    {
        if (columns)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(length));
        }
        else
        {
            grid.RowDefinitions.Add(new RowDefinition(length));
        }
    }

    private static void SetCell(UIElement element, bool columns, int index, int crossIndex)
    {
        Grid.SetColumn(element, columns ? index : crossIndex);
        Grid.SetRow(element, columns ? crossIndex : index);
    }

    private static string GetDeviceEdges(IEnumerable<UIElement> cells, float scale, bool columns)
    {
        return string.Join(", ", cells.Select(cell =>
        {
            LayoutRect bounds = cell.ArrangedBounds;
            float start = columns ? bounds.X : bounds.Y;
            float end = start + (columns ? bounds.Width : bounds.Height);
            return $"{MathF.Round(start * scale)}..{MathF.Round(end * scale)}";
        }));
    }

    private static Grid CreateTwoStarRowGrid(out FixedElement small, out FixedElement large)
    {
        Grid grid = new();
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));
        small = new FixedElement(new LayoutSize(50, 10));
        large = new FixedElement(new LayoutSize(50, 30));
        Grid.SetRow(large, 1);
        grid.VisualChildren.Add(small);
        grid.VisualChildren.Add(large);
        return grid;
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }

    private sealed class SizeRecordingElement : UIElement
    {
        public LayoutSize MeasureSize { get; private set; }

        public LayoutSize ArrangeSize { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureSize = context.AvailableSize;
            return context.AvailableSize;
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            ArrangeSize = context.FinalRect.Size;
            return context.FinalRect;
        }
    }

    private sealed class MeasureRecordingElement : UIElement
    {
        public List<LayoutSize> MeasureSizes { get; } = [];

        public LayoutSize ArrangeSize { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureSizes.Add(context.AvailableSize);
            return new LayoutSize(10, 10);
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            ArrangeSize = context.FinalRect.Size;
            return context.FinalRect;
        }
    }

    // Along the axis, atThreshold (50 px by default) when the cross size is at least the threshold (34 px by default),
    // belowThreshold (70 px by default) below that; 10 px across.
    private sealed class CrossThresholdElement(
        bool columns,
        float threshold = 34,
        float atThreshold = 50,
        float belowThreshold = 70) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            float size = (columns ? context.AvailableSize.Height : context.AvailableSize.Width) >= threshold
                ? atThreshold
                : belowThreshold;
            return columns ? new LayoutSize(size, 10) : new LayoutSize(10, size);
        }
    }

    // Along the axis twice the available cross size; across, the available cross size.
    private sealed class CrossRatioElement(bool columns) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            float cross = columns ? context.AvailableSize.Height : context.AvailableSize.Width;
            return columns ? new LayoutSize(cross * 2, cross) : new LayoutSize(cross, cross * 2);
        }
    }

    // 20 px high when at least 34 px wide, 60 px high when narrower.
    private sealed class WidthThresholdElement : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(MathF.Min(context.AvailableSize.Width, 34), context.AvailableSize.Width >= 34 ? 20 : 60);
        }
    }

    private sealed class WidthSensitiveElement : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            float height = context.AvailableSize.Width <= 100 ? 60 : 20;
            return new LayoutSize(context.AvailableSize.Width, height);
        }
    }

    // 400 px of content in 20 px lines, wrapped to the available width, or to the available height when vertical.
    private sealed class WrappingElement(bool vertical) : UIElement
    {
        public LayoutSize LastAvailableSize { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            LastAvailableSize = context.AvailableSize;
            float length = MathF.Min(vertical ? context.AvailableSize.Height : context.AvailableSize.Width, 400);
            float thickness = MathF.Ceiling(400 / length) * 20;
            return vertical ? new LayoutSize(thickness, length) : new LayoutSize(length, thickness);
        }
    }

    // 8 items of 50 x 20, laid left to right and wrapped into new 20 px rows, or when vertical,
    // 8 items of 20 x 50 stacked top to bottom and wrapped into new 20 px columns.
    private sealed class ItemWrappingElement(bool vertical) : UIElement
    {
        public LayoutSize LastAvailableSize { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            LastAvailableSize = context.AvailableSize;
            float length = vertical ? context.AvailableSize.Height : context.AvailableSize.Width;
            int perLine = float.IsPositiveInfinity(length) ? 8 : Math.Clamp((int)(length / 50), 1, 8);
            int lineCount = (8 + perLine - 1) / perLine;
            return vertical
                ? new LayoutSize(lineCount * 20, perLine * 50)
                : new LayoutSize(perLine * 50, lineCount * 20);
        }
    }

    // A quarter as wide as the available height, or 100 x 400 when the height is unbounded.
    private sealed class AspectElement : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            float height = float.IsPositiveInfinity(context.AvailableSize.Height) ? 400 : context.AvailableSize.Height;
            return new LayoutSize(height / 4, height);
        }
    }

    private sealed class CountingElement(LayoutSize size) : UIElement
    {
        public int MeasureCount { get; private set; }

        public LayoutSize LastAvailableSize { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            LastAvailableSize = context.AvailableSize;
            return size;
        }
    }
}
