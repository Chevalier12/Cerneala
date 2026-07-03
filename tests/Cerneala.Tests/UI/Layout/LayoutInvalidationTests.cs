using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Layout;

public sealed class LayoutInvalidationTests
{
    [Fact]
    public void ChangedLayoutPropertyQueuesMeasureWork()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);

        child.Margin = new Thickness(8);

        Assert.True(child.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(root.LayoutQueue.MeasureCount >= 1);
        Assert.Contains(child, root.LayoutQueue.SnapshotMeasure());
    }

    [Fact]
    public void NoOpLayoutPropertySetDoesNotQueueMeasureWork()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        child.Margin = new Thickness(8);
        root.ProcessFrame();

        child.Margin = new Thickness(8);

        Assert.Equal(0, root.LayoutQueue.MeasureCount);
    }

    [Fact]
    public void LayoutBoundaryStopsMeasurePropagation()
    {
        UIRoot root = new();
        UIElement boundary = new();
        UIElement child = new();
        root.VisualChildren.Add(boundary);
        boundary.VisualChildren.Add(child);
        LayoutBoundary.SetIsBoundary(boundary, true);

        child.Invalidate(InvalidationFlags.Measure, "measure");

        Assert.True(boundary.DirtyState.Has(InvalidationFlags.Measure));
        Assert.False(root.DirtyState.Has(InvalidationFlags.Measure));
    }

    [Fact]
    public void ChangedArrangeBoundsSchedulesRenderAndHitTest()
    {
        UIRoot root = new(100, 100);
        FixedElement child = new(new LayoutSize(20, 10));
        root.VisualChildren.Add(child);
        root.LayoutManager.Measure(child, new LayoutSize(100, 100));

        LayoutResult result = root.LayoutManager.Arrange(child, new LayoutRect(0, 0, 20, 10));

        Assert.True(result.BoundsChanged);
        Assert.Equal(1, root.RenderQueue.Count);
        Assert.Equal(1, root.HitTestQueue.Count);
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            return new LayoutRect(context.FinalRect.X, context.FinalRect.Y, DesiredSize.Width, DesiredSize.Height);
        }
    }
}
