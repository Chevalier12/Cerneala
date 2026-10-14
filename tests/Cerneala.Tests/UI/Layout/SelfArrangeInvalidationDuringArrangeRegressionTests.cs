using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Layout;

public sealed class SelfArrangeInvalidationDuringArrangeRegressionTests
{
    public enum Trigger
    {
        SizeChanged,
        ArrangeCore
    }

    [Theory]
    [InlineData(Trigger.SizeChanged)]
    [InlineData(Trigger.ArrangeCore)]
    public void ArrangeRequestFromOwnArrangeSurvivesSameSlotParentArrange(Trigger trigger)
    {
        UIRoot root = new(100, 100);
        SlotPanel parent = new();
        OffsetElement child = new();
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);
        Settle(root);
        Assert.Equal(0, child.ArrangedBounds.X);

        ArmSelfInvalidation(child, trigger);
        parent.ChildX = 5;
        parent.Invalidate(InvalidationFlags.Arrange, "slot moved");
        root.ProcessFrame();
        Assert.Equal(5, child.ArrangedBounds.X);
        Assert.True(child.DirtyState.Has(InvalidationFlags.Arrange));

        parent.Invalidate(InvalidationFlags.Arrange, "parent arranged again, same slot");
        root.ProcessFrame();
        Settle(root);

        Assert.Equal(20, child.ArrangedBounds.X);
    }

    [Theory]
    [InlineData(Trigger.SizeChanged)]
    [InlineData(Trigger.ArrangeCore)]
    public void ArrangeRequestFromOwnArrangeSurvivesLayoutManagerArrange(Trigger trigger)
    {
        UIRoot root = new(100, 100);
        OffsetElement child = new();
        root.VisualChildren.Add(child);
        LayoutRect slot = new(5, 0, 20, 10);
        root.LayoutManager.Measure(child, new LayoutSize(100, 100));
        ArmSelfInvalidation(child, trigger);

        root.LayoutManager.Arrange(child, slot);
        Assert.Equal(5, child.ArrangedBounds.X);
        Assert.Equal(1, child.ArrangeCount);

        child.Arrange(new ArrangeContext(slot, LayoutRounding.ForScale(root.Scale)));

        Assert.Equal(2, child.ArrangeCount);
        Assert.Equal(20, child.ArrangedBounds.X);
    }

    private static void ArmSelfInvalidation(OffsetElement child, Trigger trigger)
    {
        bool armed = true;
        void Request()
        {
            if (!armed)
            {
                return;
            }

            armed = false;
            child.Offset = 15;
            child.Invalidate(InvalidationFlags.Arrange, "self during own arrange");
        }

        if (trigger == Trigger.SizeChanged)
        {
            child.SizeChanged += (_, _) => Request();
        }
        else
        {
            child.Arranged = Request;
        }
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

    private sealed class SlotPanel : UIElement
    {
        public float ChildX { get; set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            foreach (UIElement child in VisualChildren)
            {
                child.Measure(context);
            }

            return new LayoutSize(100, 100);
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            foreach (UIElement child in VisualChildren)
            {
                child.Arrange(new ArrangeContext(
                    new LayoutRect(context.FinalRect.X + ChildX, context.FinalRect.Y, 20, 10),
                    context.Rounding));
            }

            return context.FinalRect;
        }
    }

    private sealed class OffsetElement : UIElement
    {
        public float Offset { get; set; }

        public int ArrangeCount { get; private set; }

        public Action? Arranged { get; set; }

        protected override LayoutSize MeasureCore(MeasureContext context) => new(20, 10);

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            ArrangeCount++;
            LayoutRect arranged = new(context.FinalRect.X + Offset, context.FinalRect.Y, 20, 10);
            Arranged?.Invoke();
            return arranged;
        }
    }
}
