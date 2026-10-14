using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Layout;

public sealed class ParentRequeuedAfterAncestorMeasuredItTests
{
    [Theory]
    [InlineData(false)] // grandparent.Invalidate(Measure): the grandparent measures the parent in pass 0
    [InlineData(true)]  // a sibling's size change requeues the grandparent, which measures the parent in pass 0
    public void ParentIsMeasuredAgainWhenChildSizeChangesAfterAnAncestorMeasuredIt(bool viaSibling)
    {
        UIRoot root = new(100, 100);
        MaxPanel grandparent = new();
        MaxPanel parent = new();
        Box child = new() { BoxWidth = 20 };
        Box sibling = new() { BoxWidth = 5 };
        parent.VisualChildren.Add(child);
        grandparent.VisualChildren.Add(parent);
        grandparent.VisualChildren.Add(sibling);
        root.VisualChildren.Add(grandparent);
        Settle(root);
        Assert.Equal(20, parent.DesiredSize.Width);

        child.BoxWidth = 30;
        if (viaSibling)
        {
            sibling.BoxWidth = 6;
            sibling.Invalidate(InvalidationFlags.Measure, "sibling");
        }
        else
        {
            grandparent.Invalidate(InvalidationFlags.Measure, "grandparent");
        }

        child.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Subtree, "child");
        root.ProcessFrame();

        Assert.Equal(30, child.DesiredSize.Width);
        Assert.Equal(30, parent.DesiredSize.Width);
        Assert.Equal(30, grandparent.DesiredSize.Width);
    }

    private static void Settle(UIRoot root)
    {
        root.ProcessFrame();
        for (int frame = 0; frame < 20 && root.Scheduler.HasWork; frame++)
        {
            root.ProcessFrame();
        }
    }

    // Measures every child with 100 x 100 and reports the widest child.
    private sealed class MaxPanel : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            float width = 0;
            foreach (UIElement child in VisualChildren)
            {
                width = Math.Max(width, child.Measure(new MeasureContext(new LayoutSize(100, 100), context.Rounding)).Width);
            }

            return new LayoutSize(width, 10);
        }
    }

    private sealed class Box : UIElement
    {
        // Plain property: raises no invalidation.
        public float BoxWidth { get; set; }

        protected override LayoutSize MeasureCore(MeasureContext context) => new(BoxWidth, 10);
    }
}
