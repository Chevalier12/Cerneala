using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using StackPanel = Cerneala.UI.Layout.Panels.StackPanel;

namespace Cerneala.Tests.UI.Layout;

public sealed class MeasureInvalidationAfterMeasureSameFrameRegressionTests
{
    [Fact]
    public void MeasureInvalidationRaisedAfterTheElementWasMeasuredInTheSamePhaseRuns()
    {
        UIRoot root = new(100, 100);
        StackPanel parent = new();
        WidthElement target = new();
        TriggerElement trigger = new();
        // The measure snapshot is walked in reverse preorder, so target is measured before trigger.
        parent.VisualChildren.Add(trigger);
        parent.VisualChildren.Add(target);
        root.VisualChildren.Add(parent);
        Settle(root);
        Assert.Equal(1, target.MeasureCount);
        Assert.Equal(20, target.DesiredSize.Width);

        trigger.MeasureCallback = () =>
        {
            target.Width = 25;
            target.Invalidate(InvalidationFlags.Measure, "sibling measured");
        };
        target.Invalidate(InvalidationFlags.Measure, "target first");
        trigger.Invalidate(InvalidationFlags.Measure, "trigger");
        Settle(root);

        Assert.Equal(25, target.DesiredSize.Width);
        Assert.False(target.DirtyState.Has(InvalidationFlags.Measure));
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

    private sealed class WidthElement : UIElement
    {
        public float Width { get; set; } = 20;

        public int MeasureCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            return new(Width, 10);
        }
    }

    private sealed class TriggerElement : UIElement
    {
        public Action? MeasureCallback { get; set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            Action? callback = MeasureCallback;
            MeasureCallback = null;
            callback?.Invoke();
            return new(20, 10);
        }
    }
}
