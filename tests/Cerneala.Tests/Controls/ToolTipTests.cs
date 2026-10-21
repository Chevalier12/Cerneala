using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using LayoutCanvas = Cerneala.UI.Layout.Panels.Canvas;

namespace Cerneala.Tests.Controls;

public sealed class ToolTipTests
{
    [Fact]
    public void ToolTipHostsContentThroughPopupRootWhenOpen()
    {
        UIRoot root = new(100, 100);
        ToolTip toolTip = new()
        {
            Content = new FixedElement(new LayoutSize(30, 10)),
            IsOpen = true
        };
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(toolTip);
        root.VisualChildren.Add(canvas);

        root.ProcessFrame();

        Assert.Equal(LayoutSize.Zero, toolTip.DesiredSize);
        Assert.DoesNotContain(toolTip.PopupRoot, toolTip.VisualChildren);
        UIElement content = Assert.IsType<FixedElement>(toolTip.Content);
        Assert.Same(toolTip.PopupRoot, content.VisualParent);
        Assert.Same(root.VisualChildren[^1], toolTip.PopupRoot.VisualParent?.VisualParent);
    }

    [Fact]
    public void PopupRootOverlayParticipatesInHitTestingAndInputRouting()
    {
        UIRoot root = new(100, 100);
        ToolTip toolTip = new()
        {
            Content = new FixedElement(new LayoutSize(30, 10)),
            IsOpen = true
        };
        bool routed = false;
        toolTip.PopupRoot.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => routed = true);
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(toolTip);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();

        LayoutRect bounds = toolTip.PopupRoot.ArrangedBounds;
        float x = bounds.X + 1;
        float y = bounds.Y + 1;
        HitTestResult? hit = new HitTestService().HitTest(root, x, y);
        new ElementInputBridge().Dispatch(root, PointerFrame(x, y, currentDown: true));

        Assert.NotNull(hit);
        Assert.True(routed);
    }

    [Fact]
    public void RejectedContentChangeRollsBackWithoutDisturbingOverlayHost()
    {
        UIElement existingParent = new();
        UIElement content = new();
        existingParent.VisualChildren.Add(content);
        ToolTip toolTip = new();

        Assert.Throws<InvalidOperationException>(() => toolTip.Content = content);

        Assert.DoesNotContain(toolTip.PopupRoot, toolTip.VisualChildren);
        Assert.NotNull(toolTip.PopupRoot.LogicalParent);
        Assert.NotNull(toolTip.PopupRoot.VisualParent);
        Assert.Null(toolTip.Content);
        Assert.Null(content.LogicalParent);
        Assert.Same(existingParent, content.VisualParent);
    }

    // As a WPF ToolTip with Placement Custom: the placement gets the target's bounds and the ToolTip's size and returns
    // its corner, here 5 pixels right of the target's right edge; a corner outside the viewport moves into it.
    [Fact]
    public void CustomPlacementPutsToolTipWhereThePlacementSaysInsideTheViewport()
    {
        UIRoot root = new(100, 100);
        FixedElement target = new(new LayoutSize(20, 10));
        LayoutCanvas.SetLeft(target, 30);
        LayoutCanvas.SetTop(target, 40);
        ToolTip toolTip = new() { Content = new FixedElement(new LayoutSize(30, 10)) };
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(target);
        canvas.VisualChildren.Add(toolTip);
        root.VisualChildren.Add(canvas);
        toolTip.SetCustomPlacement(target, (bounds, size) => new LayoutPoint(bounds.X + bounds.Width + 5, bounds.Y));
        toolTip.IsOpen = true;
        root.ProcessFrame();

        Assert.Equal(new LayoutRect(55, 40, 30, 10), toolTip.PopupRoot.ArrangedBounds);

        toolTip.SetCustomPlacement(target, (bounds, size) => new LayoutPoint(bounds.X + 80, bounds.Y - 90));
        root.ProcessFrame();

        Assert.Equal(new LayoutRect(70, 0, 30, 10), toolTip.PopupRoot.ArrangedBounds);
    }

    private static InputFrame PointerFrame(float x, float y, bool currentDown = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y);
        if (currentDown)
        {
            current = current.WithButton(InputMouseButton.Left, true);
        }

        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }
}
