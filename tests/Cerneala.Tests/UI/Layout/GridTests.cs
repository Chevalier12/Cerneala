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
