using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using LayoutCanvas = Cerneala.UI.Layout.Panels.Canvas;

namespace Cerneala.Tests.UI.Invalidation;

public sealed class MenuInvalidationTests
{
    [Fact]
    public void NestedSubmenuOpenAndCloseSettlesToAnIdleFollowingFrame()
    {
        MenuItem leaf = Item("Leaf");
        MenuItem nested = Item("Nested", leaf);
        MenuItem file = Item("File", nested);
        MenuBar menuBar = new();
        menuBar.Items.Add(file);
        UIRoot root = Attach(menuBar);

        file.IsSubmenuOpen = true;
        root.ProcessFrame();
        nested.IsSubmenuOpen = true;
        root.ProcessFrame();

        file.IsSubmenuOpen = false;
        FrameStats closingFrame = root.ProcessFrame();
        FrameStats idleFrame = root.ProcessFrame();

        Assert.True(closingFrame.HasWork);
        Assert.False(file.IsSubmenuOpen);
        Assert.False(nested.IsSubmenuOpen);
        Assert.Equal(0, idleFrame.MeasuredElements);
        Assert.Equal(0, idleFrame.ArrangedElements);
        Assert.Equal(0, idleFrame.RenderedElements);
        Assert.Equal(0, idleFrame.HitTestElements);
        Assert.Equal(1, idleFrame.NoWorkFrames);
        Assert.False(idleFrame.HasWork);
    }

    [Fact]
    public void PlacementInvalidationIsScopedAcrossTargetMoveViewportResizeAndBranchClose()
    {
        UIRoot root = new(300, 200);
        FixedElement firstTarget = new(new LayoutSize(20, 20));
        FixedElement secondTarget = new(new LayoutSize(20, 20));
        RecordingElement firstContent = new(new LayoutSize(50, 30));
        RecordingElement secondContent = new(new LayoutSize(40, 25));
        Overlay firstOverlay = CreateOverlay(firstTarget, firstContent);
        Overlay secondOverlay = CreateOverlay(secondTarget, secondContent);
        LayoutCanvas canvas = new();
        LayoutCanvas.SetLeft(firstTarget, 230);
        LayoutCanvas.SetTop(firstTarget, 20);
        LayoutCanvas.SetLeft(secondTarget, 20);
        LayoutCanvas.SetTop(secondTarget, 110);
        canvas.VisualChildren.Add(firstTarget);
        canvas.VisualChildren.Add(secondTarget);
        canvas.VisualChildren.Add(firstOverlay);
        canvas.VisualChildren.Add(secondOverlay);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        root.ProcessFrame();

        firstContent.ResetCounts();
        secondContent.ResetCounts();
        LayoutCanvas.SetLeft(firstTarget, 260);
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.True(firstContent.ArrangeCount > 0);
        Assert.Equal(0, secondContent.MeasureCount);
        Assert.Equal(0, secondContent.ArrangeCount);

        firstContent.ResetCounts();
        secondContent.ResetCounts();
        root.SetViewport(250, 180, 1);
        root.ProcessFrame();
        root.ProcessFrame();

        // The second content is measured at its natural size, which the smaller viewport still holds beside its target.
        Assert.True(firstContent.ArrangeCount > 0);
        Assert.Equal(0, secondContent.MeasureCount);
        Assert.Equal(0, secondContent.ArrangeCount);

        secondContent.ResetCounts();
        firstOverlay.IsOpen = false;
        root.ProcessFrame();

        Assert.False(firstOverlay.IsOpen);
        Assert.True(secondOverlay.IsOpen);
        Assert.Equal(0, secondContent.MeasureCount);
        Assert.Equal(0, secondContent.ArrangeCount);
    }

    [Theory]
    [InlineData(OverlayPlacement.Bottom, 120, 80)]
    [InlineData(OverlayPlacement.Auto, 0, 100)]
    public void PlacementThatKeepsTheSlotDoesNotArrangeAgainOverlayContentClampedBesideItsTarget(
        OverlayPlacement placement,
        float expectedY,
        float expectedHeight)
    {
        // The content wants 50 x 300 and gets the 80 px below the target (Bottom) or the 100 px above it (Auto). The
        // layer measures it unbounded (300 high) before it measures it clamped, in its measure and in its arrange, so
        // its desired height goes 80 -> 300 -> 80 (Auto: 100 -> 300 -> 100) although its slot and final desired size
        // never change.
        UIRoot root = new(300, 200);
        UIElement target = new() { Width = 20, Height = 20 };
        ConstraintFollowingElement content = new(new LayoutSize(50, 300));
        Overlay overlay = new()
        {
            Placement = placement,
            PlacementTarget = target,
            Content = content,
            IsOpen = true
        };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetLeft(target, 20);
        LayoutCanvas.SetTop(target, 100);
        canvas.VisualChildren.Add(target);
        canvas.VisualChildren.Add(overlay);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        root.ProcessFrame();

        content.ArrangeCount = 0;
        root.SetViewport(300, 200, 1);
        FrameStats unchangedViewport = root.ProcessFrame();
        int arrangesForUnchangedViewport = content.ArrangeCount;
        content.ArrangeCount = 0;
        root.SetViewport(290, 200, 1);
        root.ProcessFrame();
        int arrangesForNarrowerViewport = content.ArrangeCount;
        // A wider target keeps the overlay's left edge, its side and the space on that side. The target's arrange
        // invalidates the placement, which the next frame computes.
        content.ArrangeCount = 0;
        target.Width = 40;
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Equal(new LayoutRect(20, expectedY, 50, expectedHeight), content.ArrangedBounds);
        // The unchanged-viewport frame arranges the layer and the content's ContentPresenter: the layer's arrange measures
        // the presenter unbounded and then clamped, and a measure that runs makes the presenter's own arrange cache
        // invalid, as WPF's Measure calls InvalidateArrange (#516). The content's measures return cached sizes, so it
        // keeps its arrange.
        Assert.Equal(
            (0, 2, 0, 0),
            (arrangesForUnchangedViewport, unchangedViewport.ArrangeCalls, arrangesForNarrowerViewport, content.ArrangeCount));
    }

    [Fact]
    public void OpenOverlayWhoseScrollViewerIsTallerThanTheWindowSettlesAfterTheSecondFrame()
    {
        // The layer measures the ScrollViewer unbounded (500 high, no scroll bar) and then clamped to the 80 px below
        // the target (scroll bar shown), in its measure and in its arrange. Each measure toggles the scroll bar and
        // queues the ScrollViewer's measure again, so a layer arrange in the second frame leaves work for a third.
        UIRoot root = new(200, 100, 1);
        Border target = new() { Height = 20, VerticalAlignment = VerticalAlignment.Top };
        root.VisualChildren.Add(target);
        ScrollViewer viewer = new() { Content = new UIElement { Height = 500 } };
        Overlay overlay = new() { PlacementTarget = target, Content = viewer };
        root.VisualChildren.Add(overlay);
        overlay.IsOpen = true;

        root.ProcessFrame();
        root.ProcessFrame();
        bool hasWorkAfterSecondFrame = root.Scheduler.HasWork;
        FrameStats laterFrame = root.ProcessFrame();

        Assert.Equal((20f, 80f), (viewer.ArrangedBounds.Y, viewer.ArrangedBounds.Height));
        Assert.Equal(
            (false, 0, 0),
            (hasWorkAfterSecondFrame, laterFrame.MeasuredElements, laterFrame.ArrangedElements));
    }

    [Fact]
    public void OpenComboBoxDropDownWithFiftyItemsSettlesAfterTheSecondFrame()
    {
        UIRoot root = new(300, 200, 1);
        ComboBox combo = new() { Width = 120, VerticalAlignment = VerticalAlignment.Top };
        for (int index = 0; index < 50; index++)
        {
            combo.Items.Add($"Item {index}");
        }

        root.VisualChildren.Add(combo);
        root.ProcessFrame();
        combo.IsDropDownOpen = true;

        root.ProcessFrame();
        root.ProcessFrame();
        bool hasWorkAfterSecondFrame = root.Scheduler.HasWork;
        FrameStats laterFrame = root.ProcessFrame();

        Assert.True(combo.IsDropDownOpen);
        Assert.Equal(
            (false, 0, 0),
            (hasWorkAfterSecondFrame, laterFrame.MeasuredElements, laterFrame.ArrangedElements));
    }

    private static Overlay CreateOverlay(UIElement target, UIElement content)
    {
        return new Overlay
        {
            Placement = OverlayPlacement.AutoHorizontal,
            PlacementTarget = target,
            Content = content,
            IsOpen = true
        };
    }

    private static MenuItem Item(string header, params MenuItem[] children)
    {
        MenuItem item = new() { Header = header, Width = 90, Height = 28 };
        foreach (MenuItem child in children)
        {
            item.Items.Add(child);
        }

        return item;
    }

    private static UIRoot Attach(UIElement element)
    {
        UIRoot root = new(360, 240);
        root.VisualChildren.Add(element);
        root.ProcessFrame();
        return root;
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }

    // Desires its natural size, or less when the constraint is smaller, as a ScrollViewer or wrapping text does.
    private sealed class ConstraintFollowingElement(LayoutSize size) : UIElement
    {
        public int ArrangeCount { get; set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(
                MathF.Min(size.Width, context.AvailableSize.Width),
                MathF.Min(size.Height, context.AvailableSize.Height));
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            ArrangeCount++;
            return context.FinalRect;
        }
    }

    private sealed class RecordingElement(LayoutSize size) : UIElement
    {
        public int MeasureCount { get; private set; }

        public int ArrangeCount { get; private set; }

        public void ResetCounts()
        {
            MeasureCount = 0;
            ArrangeCount = 0;
        }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            return size;
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            ArrangeCount++;
            return context.FinalRect;
        }
    }
}
