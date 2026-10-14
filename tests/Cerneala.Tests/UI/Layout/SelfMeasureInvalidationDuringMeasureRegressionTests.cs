using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Layout;

public sealed class SelfMeasureInvalidationDuringMeasureRegressionTests
{
    [Theory]
    [InlineData(false)] // desired size unchanged in frame 1: the parent does not measure the child
    [InlineData(true)]  // desired size changes in frame 1: the parent re-measures the child with the same size
    public void MeasureRequestFromOwnMeasureCoreRuns(bool desiredSizeChangesFirst)
    {
        UIRoot root = new(100, 100);
        Panel parent = new();
        GrowingElement child = new();
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);
        Settle(root);
        Assert.Equal(10, child.DesiredSize.Height);

        ArmSelfInvalidation(child);
        if (desiredSizeChangesFirst)
        {
            child.BoxHeight = 20;
        }

        child.Invalidate(InvalidationFlags.Measure, "child first");
        Settle(root);

        Assert.Equal(30, child.DesiredSize.Height);
        Assert.False(child.DirtyState.Has(InvalidationFlags.Measure));
    }

    [Fact]
    public void MeasureRequestFromOwnMeasureCoreSurvivesLayoutManagerMeasure()
    {
        UIRoot root = new(100, 100);
        GrowingElement child = new();
        root.VisualChildren.Add(child);
        LayoutSize available = new(100, 100);
        ArmSelfInvalidation(child);

        root.LayoutManager.Measure(child, available);
        Assert.Equal(10, child.DesiredSize.Height);
        Assert.Equal(1, child.MeasureCount);

        child.Measure(new MeasureContext(available, LayoutRounding.ForScale(root.Scale)));

        Assert.Equal(2, child.MeasureCount);
        Assert.Equal(30, child.DesiredSize.Height);
    }

    private static void ArmSelfInvalidation(GrowingElement child)
    {
        child.MeasureCallback = () =>
        {
            child.BoxHeight = 30;
            child.Invalidate(InvalidationFlags.Measure, "self during own MeasureCore");
        };
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

    private sealed class Panel : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            foreach (UIElement child in VisualChildren)
            {
                child.Measure(new MeasureContext(new LayoutSize(100, 100), context.Rounding));
            }

            return new LayoutSize(100, 100);
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            foreach (UIElement child in VisualChildren)
            {
                child.Arrange(new ArrangeContext(
                    new LayoutRect(context.FinalRect.X, context.FinalRect.Y, 20, child.DesiredSize.Height),
                    context.Rounding));
            }

            return context.FinalRect;
        }
    }

    private sealed class GrowingElement : UIElement
    {
        public float BoxHeight { get; set; } = 10;

        public Action? MeasureCallback { get; set; }

        public int MeasureCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            LayoutSize result = new(20, BoxHeight);
            Action? callback = MeasureCallback;
            MeasureCallback = null;
            callback?.Invoke();
            return result;
        }
    }
}
