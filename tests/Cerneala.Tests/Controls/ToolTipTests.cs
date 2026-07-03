using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class ToolTipTests
{
    [Fact]
    public void ToolTipHostsContentThroughPopupRootWhenOpen()
    {
        ToolTip toolTip = new()
        {
            Content = new FixedElement(new LayoutSize(30, 10)),
            IsOpen = true
        };

        LayoutSize desired = toolTip.Measure(new MeasureContext(new LayoutSize(100, 100)));
        toolTip.Arrange(new ArrangeContext(new LayoutRect(0, 0, 30, 10)));

        Assert.Equal(new LayoutSize(30, 10), desired);
        Assert.Contains(toolTip.PopupRoot, toolTip.VisualChildren);
        UIElement content = Assert.IsType<FixedElement>(toolTip.Content);
        Assert.Same(toolTip.PopupRoot, content.VisualParent);
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
        root.VisualChildren.Add(toolTip);
        toolTip.Measure(new MeasureContext(new LayoutSize(100, 100)));
        toolTip.Arrange(new ArrangeContext(new LayoutRect(0, 0, 30, 10)));

        HitTestResult? hit = new HitTestService().HitTest(root, 5, 5);
        new ElementInputBridge().Dispatch(root, PointerFrame(5, 5, currentDown: true));

        Assert.NotNull(hit);
        Assert.True(routed);
    }

    [Fact]
    public void RejectedOpenDoesNotLeavePopupRootAttached()
    {
        UIElement existingParent = new();
        UIElement content = new();
        existingParent.VisualChildren.Add(content);
        ToolTip toolTip = new()
        {
            Content = content
        };

        Assert.Throws<InvalidOperationException>(() => toolTip.IsOpen = true);

        Assert.DoesNotContain(toolTip.PopupRoot, toolTip.LogicalChildren);
        Assert.DoesNotContain(toolTip.PopupRoot, toolTip.VisualChildren);
        Assert.Null(toolTip.PopupRoot.LogicalParent);
        Assert.Null(toolTip.PopupRoot.VisualParent);
        Assert.Null(content.LogicalParent);
        Assert.Same(existingParent, content.VisualParent);
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
