using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using StackPanel = Cerneala.UI.Layout.Panels.StackPanel;

namespace Cerneala.Tests.UI.Layout;

public sealed class DirectArrangeInvalidationSameFrameRegressionTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DirtyChildArrangeRunsWhenParentArrangesItWithTheSameSlotInTheSameFrame(bool invalidateChildFirst)
    {
        UIRoot root = new(100, 100);
        StackPanel parent = new();
        OffsetElement child = new();
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);
        Settle(root);
        Assert.Equal(1, child.ArrangeCount);
        Assert.Equal(0, child.ArrangedBounds.X);

        child.Offset = 15;
        if (invalidateChildFirst)
        {
            child.Invalidate(InvalidationFlags.Arrange, "child offset changed");
            parent.Invalidate(InvalidationFlags.Arrange, "parent arrange");
        }
        else
        {
            parent.Invalidate(InvalidationFlags.Arrange, "parent arrange");
            child.Invalidate(InvalidationFlags.Arrange, "child offset changed");
        }

        root.ProcessFrame();

        Assert.Equal(2, child.ArrangeCount);
        Assert.Equal(15, child.ArrangedBounds.X);
        Assert.False(root.Scheduler.HasWork);
    }

    private static void Settle(UIRoot root)
    {
        root.ProcessFrame();
        for (int frame = 0; frame < 20 && root.Scheduler.HasWork; frame++)
        {
            root.ProcessFrame();
        }

        Assert.False(root.Scheduler.HasWork);
    }

    private sealed class OffsetElement : UIElement
    {
        public float Offset { get; set; }

        public int ArrangeCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context) => new(20, 10);

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            ArrangeCount++;
            return new LayoutRect(context.FinalRect.X + Offset, context.FinalRect.Y, 20, 10);
        }
    }
}
