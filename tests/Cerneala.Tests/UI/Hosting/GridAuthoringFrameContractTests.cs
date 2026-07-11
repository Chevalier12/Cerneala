using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Hosting;

public sealed class GridAuthoringFrameContractTests
{
    [Fact]
    public void GridDefinitionMutationChangesChildBoundsOnNextUpdate()
    {
        UiHost host = HostWithGrid(out Grid grid, out FixedElement child, out _);
        host.Update(EmptyFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        Assert.Equal(new LayoutRect(20, 0, 80, 100), child.ArrangedBounds);

        grid.ColumnDefinitions[0].Width = GridLength.Pixels(40);
        host.Update(EmptyFrame(), new UiViewport(100, 100), TimeSpan.Zero);

        Assert.Equal(new LayoutRect(40, 0, 60, 100), child.ArrangedBounds);
    }

    [Fact]
    public void SecondUnchangedFrameAfterGridDefinitionMutationDoesNoRetainedWork()
    {
        UiHost host = HostWithGrid(out Grid grid, out _, out _);
        host.Update(EmptyFrame(), new UiViewport(100, 100), TimeSpan.Zero);

        grid.ColumnDefinitions[0].Width = GridLength.Pixels(40);
        host.Update(EmptyFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        UiFrame unchanged = host.Update(EmptyFrame(), new UiViewport(100, 100), TimeSpan.Zero);

        Assert.False(unchanged.Stats.HasWork);
        Assert.Equal(1, unchanged.Stats.NoWorkFrames);
    }

    [Fact]
    public void GridDefinitionMutationDoesNotRebuildUnrelatedSiblingRenderCache()
    {
        UiHost host = HostWithGrid(out Grid grid, out _, out RenderCountingElement sibling);
        host.Update(EmptyFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        int siblingRenderCount = sibling.RenderCount;

        grid.ColumnDefinitions[0].Width = GridLength.Pixels(40);
        host.Update(EmptyFrame(), new UiViewport(100, 100), TimeSpan.Zero);

        Assert.Equal(siblingRenderCount, sibling.RenderCount);
    }

    private static UiHost HostWithGrid(out Grid grid, out FixedElement child, out RenderCountingElement sibling)
    {
        UIRoot root = new(100, 100);
        grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(20)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        child = new FixedElement(new LayoutSize(10, 10));
        Grid.SetColumn(child, 1);
        grid.VisualChildren.Add(child);
        sibling = new RenderCountingElement();
        root.VisualChildren.Add(grid);
        root.VisualChildren.Add(sibling);
        return new UiHost(new UiHostOptions { Root = root });
    }

    private static InputFrame EmptyFrame()
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            []);
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

    private sealed class RenderCountingElement : UIElement
    {
        public int RenderCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(10, 10);
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            return new LayoutRect(0, 0, DesiredSize.Width, DesiredSize.Height);
        }

        protected override void OnRender(RenderContext context)
        {
            RenderCount++;
            context.DrawingContext.FillRectangle(new DrawRect(context.Bounds.X, context.Bounds.Y, 1, 1), Color.White);
        }
    }
}
