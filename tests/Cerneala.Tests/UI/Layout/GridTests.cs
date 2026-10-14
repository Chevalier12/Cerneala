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
