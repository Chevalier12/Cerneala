using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using StackPanel = Cerneala.UI.Layout.Panels.StackPanel;

namespace Cerneala.Tests.UI.Layout;

public sealed class ArrangeInvalidationAfterArrangeSameFrameRegressionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ArrangeInvalidationRaisedAfterTheElementWasArrangedInTheSamePhaseRuns(bool raiseFromArrangeCore)
    {
        UIRoot root = new(100, 100);
        StackPanel parent = new();
        OffsetElement target = new();
        TriggerElement trigger = new();
        parent.VisualChildren.Add(target);
        parent.VisualChildren.Add(trigger);
        root.VisualChildren.Add(parent);
        Settle(root);
        Assert.Equal(1, target.ArrangeCount);
        Assert.Equal(0, target.ArrangedBounds.X);

        void MoveTarget()
        {
            target.Offset = 15;
            target.Invalidate(InvalidationFlags.Arrange, "sibling size changed");
        }

        if (raiseFromArrangeCore)
        {
            trigger.ArrangeCallback = MoveTarget;
        }
        else
        {
            int sizeChangedCount = 0;
            trigger.SizeChanged += (_, _) =>
            {
                if (sizeChangedCount++ == 0)
                {
                    MoveTarget();
                }
            };
        }

        target.Invalidate(InvalidationFlags.Arrange, "target first");
        trigger.Width = 30;
        trigger.Invalidate(InvalidationFlags.Arrange, "trigger width changed");
        Settle(root);

        Assert.Equal(15, target.ArrangedBounds.X);
        Assert.False(target.DirtyState.Has(InvalidationFlags.Arrange));
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

    private sealed class TriggerElement : UIElement
    {
        public float Width { get; set; } = 20;

        public Action? ArrangeCallback { get; set; }

        protected override LayoutSize MeasureCore(MeasureContext context) => new(20, 10);

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            Action? callback = ArrangeCallback;
            ArrangeCallback = null;
            callback?.Invoke();
            return new LayoutRect(context.FinalRect.X, context.FinalRect.Y, Width, 10);
        }
    }
}
