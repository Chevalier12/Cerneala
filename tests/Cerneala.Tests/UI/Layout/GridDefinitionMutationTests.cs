using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Layout;

public sealed class GridDefinitionMutationTests
{
    [Fact]
    public void AddingColumnDefinitionAfterFirstFrameInvalidatesGridMeasureArrangeRenderAndHitTest()
    {
        UIRoot root = AttachedGrid(out Grid grid);

        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(30)));

        AssertDefinitionMutationQueued(root, grid);
    }

    [Fact]
    public void AddingRowDefinitionAfterFirstFrameInvalidatesGridMeasureArrangeRenderAndHitTest()
    {
        UIRoot root = AttachedGrid(out Grid grid);

        grid.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(30)));

        AssertDefinitionMutationQueued(root, grid);
    }

    [Fact]
    public void RemovingDefinitionInvalidatesGridAndClampsExistingPlacementsSafely()
    {
        UIRoot root = AttachedGrid(out Grid grid, columnCount: 2);
        FixedElement child = new(new LayoutSize(10, 10));
        Grid.SetColumn(child, 1);
        grid.VisualChildren.Add(child);
        root.ProcessFrame();

        grid.ColumnDefinitions.RemoveAt(1);
        AssertDefinitionMutationQueued(root, grid);
        Exception? exception = Record.Exception(() => root.ProcessFrame());

        Assert.Null(exception);
        Assert.Equal(new LayoutRect(0, 0, 20, 100), child.ArrangedBounds);
    }

    [Fact]
    public void ClearingDefinitionsReturnsGridToSingleStarCellAndInvalidates()
    {
        UIRoot root = AttachedGrid(out Grid grid, columnCount: 2, rowCount: 2);
        FixedElement child = new(new LayoutSize(10, 10));
        Grid.SetColumn(child, 1);
        Grid.SetRow(child, 1);
        grid.VisualChildren.Add(child);
        root.ProcessFrame();

        grid.ColumnDefinitions.Clear();
        grid.RowDefinitions.Clear();
        root.ProcessFrame();

        Assert.Equal(new LayoutRect(0, 0, 100, 100), child.ArrangedBounds);
    }

    [Fact]
    public void ReplacingDefinitionInvalidatesExactlyOnce()
    {
        UIRoot root = AttachedGrid(out Grid grid, columnCount: 1);
        int layoutVersion = grid.LayoutVersion;

        grid.ColumnDefinitions[0] = new ColumnDefinition(GridLength.Pixels(40));

        Assert.Equal(layoutVersion + 1, grid.LayoutVersion);
        AssertDefinitionMutationQueued(root, grid);
    }

    [Fact]
    public void ChangingColumnWidthInvalidatesOwningGridLayout()
    {
        UIRoot root = AttachedGrid(out Grid grid, columnCount: 1);

        grid.ColumnDefinitions[0].Width = GridLength.Pixels(40);

        AssertDefinitionMutationQueued(root, grid);
    }

    [Fact]
    public void ChangingRowHeightInvalidatesOwningGridLayout()
    {
        UIRoot root = AttachedGrid(out Grid grid, rowCount: 1);

        grid.RowDefinitions[0].Height = GridLength.Pixels(40);

        AssertDefinitionMutationQueued(root, grid);
    }

    [Fact]
    public void SettingSameColumnWidthDoesNotInvalidate()
    {
        UIRoot root = AttachedGrid(out Grid grid, columnCount: 1);
        GridLength width = grid.ColumnDefinitions[0].Width;
        int layoutVersion = grid.LayoutVersion;

        grid.ColumnDefinitions[0].Width = width;

        Assert.Equal(layoutVersion, grid.LayoutVersion);
        Assert.DoesNotContain(grid, root.LayoutQueue.SnapshotMeasure());
        Assert.DoesNotContain(grid, root.LayoutQueue.SnapshotArrange());
        Assert.DoesNotContain(grid, root.RenderQueue.Snapshot());
        Assert.DoesNotContain(grid, root.HitTestQueue.Snapshot());
    }

    [Fact]
    public void SettingSameRowHeightDoesNotInvalidate()
    {
        UIRoot root = AttachedGrid(out Grid grid, rowCount: 1);
        GridLength height = grid.RowDefinitions[0].Height;
        int layoutVersion = grid.LayoutVersion;

        grid.RowDefinitions[0].Height = height;

        Assert.Equal(layoutVersion, grid.LayoutVersion);
        Assert.DoesNotContain(grid, root.LayoutQueue.SnapshotMeasure());
        Assert.DoesNotContain(grid, root.LayoutQueue.SnapshotArrange());
        Assert.DoesNotContain(grid, root.RenderQueue.Snapshot());
        Assert.DoesNotContain(grid, root.HitTestQueue.Snapshot());
    }

    [Fact]
    public void DetachedDefinitionChangeMarksGridDirtyAndProcessesOnAttach()
    {
        UIRoot root = new(100, 100);
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(20)));

        grid.ColumnDefinitions[0].Width = GridLength.Pixels(40);
        Assert.True(grid.DirtyState.Has(InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render | InvalidationFlags.HitTest));

        root.VisualChildren.Add(grid);
        FrameStats stats = root.ProcessFrame();

        Assert.True(stats.MeasuredElements > 0);
        Assert.True(stats.ArrangedElements > 0);
    }

    [Fact]
    public void SameDefinitionCannotBeAddedTwiceToSameGrid()
    {
        Grid grid = new();
        ColumnDefinition definition = new();
        grid.ColumnDefinitions.Add(definition);

        Assert.Throws<InvalidOperationException>(() => grid.ColumnDefinitions.Add(definition));
    }

    [Fact]
    public void FailedInsertDoesNotAttachDefinitionToGrid()
    {
        Grid firstGrid = new();
        Grid secondGrid = new();
        ColumnDefinition definition = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => firstGrid.ColumnDefinitions.Insert(1, definition));
        Exception? exception = Record.Exception(() => secondGrid.ColumnDefinitions.Add(definition));

        Assert.Null(exception);
    }

    private static UIRoot AttachedGrid(out Grid grid, int columnCount = 0, int rowCount = 0)
    {
        UIRoot root = new(100, 100);
        grid = new Grid();
        for (int i = 0; i < columnCount; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(20)));
        }

        for (int i = 0; i < rowCount; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(20)));
        }

        root.VisualChildren.Add(grid);
        root.ProcessFrame();
        return root;
    }

    private static void AssertDefinitionMutationQueued(UIRoot root, Grid grid)
    {
        Assert.Contains(grid, root.LayoutQueue.SnapshotMeasure());
        Assert.Contains(grid, root.LayoutQueue.SnapshotArrange());
        Assert.Contains(grid, root.RenderQueue.Snapshot());
        Assert.Contains(grid, root.HitTestQueue.Snapshot());
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }

        protected override void OnRender(RenderContext context)
        {
            context.DrawingContext.FillRectangle(new DrawRect(context.Bounds.X, context.Bounds.Y, 1, 1), Color.White);
        }
    }
}
