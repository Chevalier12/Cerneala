using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.Tests.UI.Layout;

public sealed class LayoutManagerTests
{
    [Fact]
    public void MeasureQueueUpdatesDesiredSize()
    {
        UIRoot root = new(100, 100);
        CountingElement child = new(new LayoutSize(20, 10));
        root.VisualChildren.Add(child);
        child.Invalidate(InvalidationFlags.Measure, "measure");

        FrameStats stats = root.ProcessFrame();

        Assert.Equal(new LayoutSize(20, 10), child.DesiredSize);
        Assert.True(stats.MeasuredElements > 0);
    }

    [Fact]
    public void ArrangeQueueUpdatesArrangedBounds()
    {
        UIRoot root = new(100, 100);
        CountingElement child = new(new LayoutSize(20, 10));
        root.VisualChildren.Add(child);
        root.LayoutManager.Measure(child, new LayoutSize(100, 100));
        child.Invalidate(InvalidationFlags.Arrange, "arrange");

        root.ProcessFrame();

        Assert.Equal(new LayoutRect(0, 0, 20, 10), child.ArrangedBounds);
    }

    [Fact]
    public void MeasureCacheIsReusedForSameConstraintAndVersion()
    {
        UIRoot root = new(100, 100);
        CountingElement child = new(new LayoutSize(20, 10));

        root.LayoutManager.Measure(child, new LayoutSize(100, 100));
        LayoutResult result = root.LayoutManager.Measure(child, new LayoutSize(100, 100));

        Assert.True(result.UsedMeasureCache);
        Assert.Equal(1, child.MeasureCount);
    }

    [Fact]
    public void DirtyMeasureBypassesParentMeasureCacheWhenChildLayoutChanges()
    {
        UIRoot root = new(100, 100);
        MeasuringParent parent = new();
        ResizableElement child = new(new LayoutSize(10, 5));
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        parent.Invalidate(InvalidationFlags.Measure, "initial measure");
        root.ProcessFrame();

        child.Size = new LayoutSize(25, 5);
        child.Margin = new Thickness(1);
        root.ProcessFrame();

        Assert.Equal(new LayoutSize(25, 5), parent.DesiredSize);
        Assert.Equal(2, parent.MeasureCount);
        Assert.True(child.MeasureCount >= 2);
    }

    [Fact]
    public void ArrangeCacheIsReusedForSameRectAndVersion()
    {
        UIRoot root = new(100, 100);
        CountingElement child = new(new LayoutSize(20, 10));

        root.LayoutManager.Measure(child, new LayoutSize(100, 100));
        root.LayoutManager.Arrange(child, new LayoutRect(0, 0, 20, 10));
        LayoutResult result = root.LayoutManager.Arrange(child, new LayoutRect(0, 0, 20, 10));

        Assert.True(result.UsedArrangeCache);
        Assert.Equal(1, child.ArrangeCount);
    }

    [Fact]
    public void DirtyArrangeBypassesArrangeCache()
    {
        UIRoot root = new(100, 100);
        CountingElement child = new(new LayoutSize(20, 10));
        root.VisualChildren.Add(child);
        root.LayoutManager.Measure(child, new LayoutSize(100, 100));
        root.LayoutManager.Arrange(child, new LayoutRect(0, 0, 20, 10));

        child.Invalidate(InvalidationFlags.Arrange, "arrange again");
        root.ProcessFrame();

        Assert.Equal(2, child.ArrangeCount);
    }

    [Fact]
    public void SubtreeArrangeDoesNotOverwritePanelChildSlots()
    {
        UIRoot root = new(100, 100);
        StackPanel panel = new();
        StretchingElement first = new(new LayoutSize(20, 10));
        StretchingElement second = new(new LayoutSize(30, 5));
        panel.VisualChildren.Add(first);
        panel.VisualChildren.Add(second);
        root.VisualChildren.Add(panel);

        panel.Invalidate(
            InvalidationFlags.Measure |
            InvalidationFlags.Arrange |
            InvalidationFlags.Render |
            InvalidationFlags.HitTest |
            InvalidationFlags.Subtree,
            "subtree layout");
        root.ProcessFrame();

        Assert.Equal(new LayoutRect(0, 0, 100, 10), first.ArrangedBounds);
        Assert.Equal(new LayoutRect(0, 10, 100, 5), second.ArrangedBounds);
    }

    [Fact]
    public void FailedMeasureKeepsDirtyFlagsAndQueue()
    {
        UIRoot root = new(100, 100);
        ThrowingElement child = new();
        root.VisualChildren.Add(child);
        child.Invalidate(InvalidationFlags.Measure, "measure");

        Assert.Throws<InvalidOperationException>(() => root.ProcessFrame());

        Assert.True(child.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(root.LayoutQueue.MeasureCount > 0);
    }

    private sealed class CountingElement(LayoutSize size) : UIElement
    {
        public int MeasureCount { get; private set; }

        public int ArrangeCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            return size;
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            ArrangeCount++;
            return new LayoutRect(context.FinalRect.X, context.FinalRect.Y, DesiredSize.Width, DesiredSize.Height);
        }
    }

    private sealed class StretchingElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }

    private sealed class MeasuringParent : UIElement
    {
        public int MeasureCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            LayoutSize desired = LayoutSize.Zero;
            foreach (UIElement child in VisualChildren)
            {
                LayoutSize childSize = child.Measure(context);
                desired = new LayoutSize(MathF.Max(desired.Width, childSize.Width), MathF.Max(desired.Height, childSize.Height));
            }

            return desired;
        }
    }

    private sealed class ResizableElement(LayoutSize size) : UIElement
    {
        public LayoutSize Size { get; set; } = size;

        public int MeasureCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            return Size;
        }
    }

    private sealed class ThrowingElement : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            throw new InvalidOperationException("boom");
        }
    }
}
