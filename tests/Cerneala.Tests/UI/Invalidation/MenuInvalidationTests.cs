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

        Assert.True(firstContent.ArrangeCount > 0);
        Assert.True(secondContent.MeasureCount > 0);
        Assert.Equal(0, secondContent.ArrangeCount);

        secondContent.ResetCounts();
        firstOverlay.IsOpen = false;
        root.ProcessFrame();

        Assert.False(firstOverlay.IsOpen);
        Assert.True(secondOverlay.IsOpen);
        Assert.Equal(0, secondContent.MeasureCount);
        Assert.Equal(0, secondContent.ArrangeCount);
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
