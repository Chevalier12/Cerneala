using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using LayoutCanvas = Cerneala.UI.Layout.Panels.Canvas;

namespace Cerneala.Tests.Controls;

public sealed class OverlayTests
{
    [Fact]
    public void OpenOverlayProjectsAboveRootContentAndDoesNotAffectOwnerLayout()
    {
        UIRoot root = new(100, 100);
        FixedElement target = new(new LayoutSize(20, 10));
        FixedElement content = new(new LayoutSize(30, 15));
        Overlay overlay = new()
        {
            PlacementTarget = target,
            Content = content,
            IsOpen = true
        };
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(target);
        canvas.VisualChildren.Add(overlay);
        root.VisualChildren.Add(canvas);

        root.ProcessFrame();

        Assert.Equal(LayoutSize.Zero, overlay.DesiredSize);
        Assert.Equal(new LayoutRect(0, 10, 30, 15), content.VisualParent!.ArrangedBounds);
        Assert.Same(root.VisualChildren[^1], content.VisualParent.VisualParent);
        Assert.NotSame(canvas, root.VisualChildren[^1]);
    }

    [Fact]
    public void AutoPlacementFlipsAndTracksTargetMovementAndViewportResize()
    {
        UIRoot root = new(100, 100);
        FixedElement target = new(new LayoutSize(20, 10));
        FixedElement content = new(new LayoutSize(30, 25));
        Overlay overlay = new()
        {
            PlacementTarget = target,
            Content = content,
            IsOpen = true
        };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetTop(target, 80);
        canvas.VisualChildren.Add(target);
        canvas.VisualChildren.Add(overlay);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();

        Assert.Equal(55, content.VisualParent!.ArrangedBounds.Y);

        LayoutCanvas.SetTop(target, 20);
        root.ProcessFrame();
        root.ProcessFrame();
        Assert.Equal(20, target.ArrangedBounds.Y);
        Assert.Equal(30, content.VisualParent.ArrangedBounds.Y);

        root.SetViewport(25, 40, 1);
        root.ProcessFrame();
        Assert.Equal(25, content.VisualParent.ArrangedBounds.Width);
        Assert.True(
            content.VisualParent.ArrangedBounds.Y + content.VisualParent.ArrangedBounds.Height <= 40);
    }

    [Fact]
    public void MultipleOverlaysKeepLastOpenedContentTopmost()
    {
        UIRoot root = new(100, 100);
        FixedElement target = new(new LayoutSize(10, 10));
        FixedElement firstContent = new(new LayoutSize(20, 10));
        FixedElement secondContent = new(new LayoutSize(20, 10));
        Overlay first = new() { PlacementTarget = target, Content = firstContent, IsOpen = true };
        Overlay second = new() { PlacementTarget = target, Content = secondContent, IsOpen = true };
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(target);
        canvas.VisualChildren.Add(first);
        canvas.VisualChildren.Add(second);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();

        UIElement layer = root.VisualChildren[^1];
        Assert.Same(secondContent.VisualParent, layer.VisualChildren[^1]);

        second.IsOpen = false;
        root.ProcessFrame();
        Assert.Same(firstContent.VisualParent, layer.VisualChildren[^1]);
    }

    [Fact]
    public void OutsideClickDismissesWithoutBlockingUnderlyingButton()
    {
        UIRoot root = new(100, 100);
        Button target = new() { Content = "target" };
        Button outside = new() { Content = "outside" };
        FixedElement content = new(new LayoutSize(30, 10));
        Overlay overlay = new()
        {
            PlacementTarget = target,
            Content = content,
            IsLightDismissEnabled = true,
            IsOpen = true
        };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetTop(outside, 40);
        canvas.VisualChildren.Add(target);
        canvas.VisualChildren.Add(outside);
        canvas.VisualChildren.Add(overlay);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        bool clicked = false;
        outside.Click += (_, _) => clicked = true;
        ElementInputBridge bridge = new();

        float x = outside.ArrangedBounds.X + 2;
        float y = outside.ArrangedBounds.Y + outside.ArrangedBounds.Height - 2;
        Assert.Same(outside, new HitTestService().HitTest(root, x, y)?.Element);
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));

        Assert.False(overlay.IsOpen);
        Assert.True(clicked);
    }

    [Fact]
    public void ClicksInsideContentOrOnTargetDoNotDismiss()
    {
        UIRoot root = new(100, 100);
        Button target = new() { Content = "target" };
        FixedElement content = new(new LayoutSize(30, 10));
        Overlay overlay = new()
        {
            PlacementTarget = target,
            Content = content,
            IsLightDismissEnabled = true,
            IsOpen = true
        };
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(target);
        canvas.VisualChildren.Add(overlay);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(2, 2, currentDown: true));
        bridge.Dispatch(root, PointerFrame(2, 2, previousDown: true));
        Assert.True(overlay.IsOpen);

        float contentY = content.VisualParent!.ArrangedBounds.Y + 2;
        bridge.Dispatch(root, PointerFrame(2, contentY, currentDown: true));
        bridge.Dispatch(root, PointerFrame(2, contentY, previousDown: true));
        Assert.True(overlay.IsOpen);
    }

    [Fact]
    public void CompositeFocusDomainClosesOnlyAfterFocusLeavesOwnerAndContent()
    {
        UIRoot root = new(160, 120);
        Button target = new() { Content = "target" };
        Button content = new() { Content = "content" };
        Button outside = new() { Content = "outside" };
        Overlay overlay = new()
        {
            PlacementTarget = target,
            Content = content,
            IsLightDismissEnabled = true,
            IsOpen = true
        };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetTop(outside, 80);
        canvas.VisualChildren.Add(target);
        canvas.VisualChildren.Add(outside);
        canvas.VisualChildren.Add(overlay);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        ElementInputRouteMap routes = root.InputCache.EnsureCurrent(root);

        bridge.FocusManager.Focus(target, routes);
        bridge.FocusManager.Focus(content, routes);
        Assert.True(overlay.IsOpen);

        bridge.FocusManager.Focus(outside, routes);
        Assert.False(overlay.IsOpen);
    }

    [Fact]
    public void OpeningBeforeAttachIsDeferredAndDetachClosesOnce()
    {
        UIRoot root = new(100, 100);
        Overlay overlay = new()
        {
            Content = new FixedElement(new LayoutSize(20, 10)),
            IsOpen = true
        };
        int opened = 0;
        int closed = 0;
        overlay.Opened += (_, _) => opened++;
        overlay.Closed += (_, _) => closed++;
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(overlay);

        Assert.Equal(0, opened);
        root.VisualChildren.Add(canvas);
        Assert.Equal(1, opened);

        root.VisualChildren.Remove(canvas);
        Assert.False(overlay.IsOpen);
        Assert.Equal(1, closed);
    }

    private static InputFrame PointerFrame(float x, float y, bool previousDown = false, bool currentDown = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y);
        if (previousDown)
        {
            previous = previous.WithButton(InputMouseButton.Left, true);
        }

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
