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
    public void AutoHorizontalPlacesContentToTheRightWhenItFits()
    {
        UIRoot root = new(120, 80);
        FixedElement target = new(new LayoutSize(20, 10));
        FixedElement content = new(new LayoutSize(30, 15));
        Overlay overlay = new()
        {
            Placement = OverlayPlacement.AutoHorizontal,
            PlacementTarget = target,
            Content = content,
            IsOpen = true
        };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetLeft(target, 15);
        LayoutCanvas.SetTop(target, 20);
        canvas.VisualChildren.Add(target);
        canvas.VisualChildren.Add(overlay);
        root.VisualChildren.Add(canvas);

        root.ProcessFrame();

        Assert.Equal(new LayoutRect(35, 20, 30, 15), content.VisualParent!.ArrangedBounds);
    }

    [Fact]
    public void AutoHorizontalFallsBackToTheLeftWhenTheRightSideIsTooSmall()
    {
        UIRoot root = new(120, 80);
        FixedElement target = new(new LayoutSize(10, 10));
        FixedElement content = new(new LayoutSize(40, 15));
        Overlay overlay = new()
        {
            Placement = OverlayPlacement.AutoHorizontal,
            PlacementTarget = target,
            Content = content,
            IsOpen = true
        };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetLeft(target, 95);
        LayoutCanvas.SetTop(target, 20);
        canvas.VisualChildren.Add(target);
        canvas.VisualChildren.Add(overlay);
        root.VisualChildren.Add(canvas);

        root.ProcessFrame();

        Assert.Equal(new LayoutRect(55, 20, 40, 15), content.VisualParent!.ArrangedBounds);
    }

    [Fact]
    public void AutoHorizontalClampsToViewportWhenNeitherSideCanContainContent()
    {
        UIRoot root = new(70, 40);
        FixedElement target = new(new LayoutSize(20, 10));
        FixedElement content = new(new LayoutSize(90, 60));
        Overlay overlay = new()
        {
            Placement = OverlayPlacement.AutoHorizontal,
            PlacementTarget = target,
            Content = content,
            IsOpen = true
        };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetLeft(target, 25);
        LayoutCanvas.SetTop(target, 10);
        canvas.VisualChildren.Add(target);
        canvas.VisualChildren.Add(overlay);
        root.VisualChildren.Add(canvas);

        root.ProcessFrame();

        Assert.Equal(new LayoutRect(0, 0, 70, 40), content.VisualParent!.ArrangedBounds);
    }

    [Fact]
    public void AutoHorizontalHandlesViewportSmallerThanTargetAndContent()
    {
        UIRoot root = new(12, 8);
        FixedElement target = new(new LayoutSize(20, 10));
        FixedElement content = new(new LayoutSize(30, 20));
        Overlay overlay = new()
        {
            Placement = OverlayPlacement.AutoHorizontal,
            PlacementTarget = target,
            Content = content,
            IsOpen = true
        };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetLeft(target, 5);
        canvas.VisualChildren.Add(target);
        canvas.VisualChildren.Add(overlay);
        root.VisualChildren.Add(canvas);

        root.ProcessFrame();

        Assert.Equal(new LayoutRect(0, 0, 12, 8), content.VisualParent!.ArrangedBounds);
    }

    [Fact]
    public void AutoHorizontalTracksTargetMovementWithoutRedundantContentMeasurement()
    {
        UIRoot root = new(120, 80);
        FixedElement target = new(new LayoutSize(20, 10));
        RecordingElement content = new(new LayoutSize(30, 15));
        Overlay overlay = new()
        {
            Placement = OverlayPlacement.AutoHorizontal,
            PlacementTarget = target,
            Content = content,
            IsOpen = true
        };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetLeft(target, 10);
        LayoutCanvas.SetTop(target, 20);
        canvas.VisualChildren.Add(target);
        canvas.VisualChildren.Add(overlay);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();

        Assert.Equal(30, content.VisualParent!.ArrangedBounds.X);
        Assert.Equal(2, content.MeasureConstraints.Count);

        LayoutCanvas.SetLeft(target, 90);
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Equal(60, content.VisualParent.ArrangedBounds.X);
        Assert.Equal(2, content.MeasureConstraints.Count);
    }

    // The content fills any finite width it gets (as a ScrollViewer does), so only a natural-size measure
    // lets it open beside the target instead of across the whole viewport.
    [Theory]
    [InlineData(15, 20, 30, 35)]
    [InlineData(95, 10, 40, 55)]
    public void AutoHorizontalMeasuresContentThatFillsAFiniteWidthAtItsNaturalWidth(
        float targetX,
        float targetWidth,
        float naturalWidth,
        float expectedX)
    {
        UIRoot root = new(120, 80);
        FixedElement target = new(new LayoutSize(targetWidth, 10));
        FillingElement content = new(new LayoutSize(naturalWidth, 15));
        Overlay overlay = new()
        {
            Placement = OverlayPlacement.AutoHorizontal,
            PlacementTarget = target,
            Content = content,
            IsOpen = true
        };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetLeft(target, targetX);
        LayoutCanvas.SetTop(target, 20);
        canvas.VisualChildren.Add(target);
        canvas.VisualChildren.Add(overlay);
        root.VisualChildren.Add(canvas);

        root.ProcessFrame();

        Assert.Equal(new LayoutRect(expectedX, 20, naturalWidth, 15), content.VisualParent!.ArrangedBounds);
    }

    // Pins the measure sequence that OverlayPlacement.md documents for AutoHorizontal: the natural-width
    // measure, then a second measure at the viewport width only for content wider than the viewport.
    // The first constraint is the root's ordinary measure of the canvas, before overlay placement.
    [Theory]
    [InlineData(30, false)]
    [InlineData(200, true)]
    public void AutoHorizontalMeasuresAtNaturalWidthAndAgainAtViewportWidthOnlyForWiderContent(
        float contentWidth,
        bool expectsViewportWidthMeasure)
    {
        UIRoot root = new(120, 80);
        FixedElement target = new(new LayoutSize(20, 10));
        RecordingElement content = new(new LayoutSize(contentWidth, 15));
        Overlay overlay = new()
        {
            Placement = OverlayPlacement.AutoHorizontal,
            PlacementTarget = target,
            Content = content,
            IsOpen = true
        };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetLeft(target, 15);
        LayoutCanvas.SetTop(target, 20);
        canvas.VisualChildren.Add(target);
        canvas.VisualChildren.Add(overlay);
        root.VisualChildren.Add(canvas);

        root.ProcessFrame();

        List<LayoutSize> expected =
        [
            new LayoutSize(120, 80),
            new LayoutSize(float.PositiveInfinity, float.PositiveInfinity)
        ];
        if (expectsViewportWidthMeasure)
        {
            expected.Add(new LayoutSize(120, 15));
        }

        Assert.Equal(expected, content.MeasureConstraints);
    }

    [Fact]
    public void NestedMenuItemSubmenuOpensBesideItsParentItemAtItsNaturalWidth()
    {
        UIRoot root = new(1100, 820);
        MenuItem recent = new() { Header = "Recent", Width = 140, Height = 34 };
        recent.Items.Add(new MenuItem { Header = "a.txt", Width = 140, Height = 34 });
        recent.Items.Add(new MenuItem { Header = "b.txt", Width = 140, Height = 34 });
        MenuItem file = new() { Header = "File", Width = 140, Height = 34 };
        file.Items.Add(new MenuItem { Header = "Open", Width = 140, Height = 34 });
        file.Items.Add(new MenuItem { Header = "Save", Width = 140, Height = 34 });
        file.Items.Add(recent);
        MenuBar menuBar = new() { HorizontalAlignment = HorizontalAlignment.Left };
        menuBar.Items.Add(file);
        root.VisualChildren.Add(menuBar);
        root.ProcessFrame();
        file.IsSubmenuOpen = true;
        root.ProcessFrame();
        recent.IsSubmenuOpen = true;
        root.ProcessFrame();

        LayoutRect fileSubmenu = file.SubmenuOverlay!.ProjectedPresenter.ArrangedBounds;
        LayoutRect recentBounds = recent.ArrangedBounds;
        LayoutRect recentSubmenu = recent.SubmenuOverlay!.ProjectedPresenter.ArrangedBounds;
        Assert.Equal(file.ArrangedBounds.X, fileSubmenu.X);
        Assert.Equal(file.ArrangedBounds.Y + file.ArrangedBounds.Height, fileSubmenu.Y);
        Assert.Equal(140, fileSubmenu.Width);
        Assert.Equal(
            new LayoutRect(recentBounds.X + recentBounds.Width, recentBounds.Y, 140, recentSubmenu.Height),
            recentSubmenu);
    }

    // A submenu taller than its side of the viewport scrolls vertically; as in WPF, its width makes room for the
    // vertical scrollbar instead of taking the scrollbar column out of the items (#510).
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SubmenuTallerThanTheViewportWidensForItsVerticalScrollBar(bool nested)
    {
        UIRoot root = new(1100, 820);
        MenuItem file = new() { Header = "File", Width = 140, Height = 34 };
        MenuItem owner = file;
        if (nested)
        {
            owner = new MenuItem { Header = "Recent", Width = 140, Height = 34 };
            file.Items.Add(new MenuItem { Header = "Open", Width = 140, Height = 34 });
            file.Items.Add(owner);
        }

        MenuItem first = null!;
        for (int i = 0; i < 30; i++)
        {
            MenuItem item = new() { Header = $"item{i}", Width = 140, Height = 34 };
            first ??= item;
            owner.Items.Add(item);
        }

        MenuBar menuBar = new() { HorizontalAlignment = HorizontalAlignment.Left };
        menuBar.Items.Add(file);
        root.VisualChildren.Add(menuBar);
        root.ProcessFrame();
        file.IsSubmenuOpen = true;
        root.ProcessFrame();
        if (nested)
        {
            owner.IsSubmenuOpen = true;
            root.ProcessFrame();
        }

        root.ProcessFrame();

        ScrollViewer viewer = FindAncestor<ScrollViewer>(first);
        ScrollContentPresenter presenter = FindAncestor<ScrollContentPresenter>(first);
        LayoutRect submenu = owner.SubmenuOverlay!.ProjectedPresenter.ArrangedBounds;
        Assert.True(viewer.IsVerticalScrollBarVisible);
        Assert.Equal(140, first.ArrangedBounds.Width);
        Assert.True(
            presenter.ViewportWidth >= presenter.ExtentWidth,
            $"ViewportWidth={presenter.ViewportWidth} ExtentWidth={presenter.ExtentWidth}");
        Assert.Equal(140 + viewer.VerticalScrollBar.ArrangedBounds.Width, submenu.Width);
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

    // UIElementCollection.Move raises no Changed event, but the layer must still stay above the page (#932).
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OpenOverlayStaysHitTestedFirstAfterARootChildIsMoved(bool moveRootChild)
    {
        UIRoot root = new(100, 100);
        FixedElement target = new(new LayoutSize(20, 10));
        FixedElement blocker = new(new LayoutSize(30, 15));
        FixedElement content = new(new LayoutSize(30, 15));
        Overlay overlay = new()
        {
            PlacementTarget = target,
            Content = content,
            IsOpen = true
        };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetTop(blocker, 10);
        canvas.VisualChildren.Add(target);
        canvas.VisualChildren.Add(blocker);
        canvas.VisualChildren.Add(overlay);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        HitTestResult? before = new HitTestService().HitTest(root, 5, 15);
        Assert.Same(content, before?.Element);
        Assert.Equal(2, root.VisualChildren.Count);
        Assert.Same(canvas, root.VisualChildren[0]);

        if (moveRootChild)
        {
            root.VisualChildren.Move(1, 0);
        }

        root.ProcessFrame();

        HitTestResult? after = new HitTestService().HitTest(root, 5, 15);
        Assert.True(
            ReferenceEquals(content, after?.Element),
            $"Move={moveRootChild}: the hit element is {after?.Element.GetType().Name ?? "null"}, not the overlay content.");
    }

    // The overlay content's fill must be recorded after the page's fill; the run without the Move is the control (#932).
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OpenOverlayIsDrawnLastAfterARootChildIsMoved(bool moveRootChild)
    {
        UIRoot root = new(100, 100);
        FixedElement target = new(new LayoutSize(20, 10));
        Cerneala.UI.Media.SolidColorBrush red = new(Cerneala.Drawing.Color.Red);
        Cerneala.UI.Media.SolidColorBrush blue = new(Cerneala.Drawing.Color.Blue);
        Border blocker = new() { Width = 30, Height = 15, Background = red };
        Border content = new() { Width = 30, Height = 15, Background = blue };
        Overlay overlay = new() { PlacementTarget = target, Content = content, IsOpen = true };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetTop(blocker, 10);
        canvas.VisualChildren.Add(target);
        canvas.VisualChildren.Add(blocker);
        canvas.VisualChildren.Add(overlay);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        if (moveRootChild)
        {
            root.VisualChildren.Move(1, 0);
        }

        root.ProcessFrame();
        Cerneala.Drawing.DrawCommandList commands = root.RetainedRenderer.Commit(root);

        int redIndex = IndexOfFill(commands, red);
        int blueIndex = IndexOfFill(commands, blue);
        Assert.True(redIndex >= 0 && blueIndex >= 0, $"Move={moveRootChild}: red fill at {redIndex}, blue fill at {blueIndex}.");
        Assert.True(blueIndex > redIndex, $"Move={moveRootChild}: the overlay content is drawn at command {blueIndex} but the page at {redIndex}.");
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
    public void DismissScopeKeepsNestedOverlayGroupOpenForPointerTransitionsAndClosesItTogether()
    {
        UIRoot root = new(180, 120);
        Button target = new() { Content = "target" };
        Button firstContent = new() { Content = "first" };
        Button secondContent = new() { Content = "second" };
        Button outside = new() { Content = "outside" };
        Overlay? first = null;
        Overlay? second = null;
        int dismissals = 0;
        OverlayDismissScope scope = new(() =>
        {
            dismissals++;
            first!.IsOpen = false;
            second!.IsOpen = false;
        });
        first = new Overlay
        {
            PlacementTarget = target,
            Content = firstContent,
            IsLightDismissEnabled = true,
            DismissScope = scope,
            IsOpen = true
        };
        second = new Overlay
        {
            PlacementTarget = firstContent,
            Content = secondContent,
            IsLightDismissEnabled = true,
            DismissScope = scope,
            IsOpen = true
        };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetLeft(outside, 100);
        LayoutCanvas.SetTop(outside, 80);
        canvas.VisualChildren.Add(target);
        canvas.VisualChildren.Add(outside);
        canvas.VisualChildren.Add(first);
        canvas.VisualChildren.Add(second);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        ElementInputBridge bridge = new();

        Click(bridge, root, firstContent.VisualParent!.ArrangedBounds.X + 2, firstContent.VisualParent.ArrangedBounds.Y + 2);
        Click(bridge, root, secondContent.VisualParent!.ArrangedBounds.X + 2, secondContent.VisualParent.ArrangedBounds.Y + 2);

        Assert.True(first.IsOpen);
        Assert.True(second.IsOpen);
        Assert.Equal(0, dismissals);

        float outsideX = outside.ArrangedBounds.X + 2;
        float outsideY = outside.ArrangedBounds.Y + outside.ArrangedBounds.Height - 2;
        Assert.Same(outside, new HitTestService().HitTest(root, outsideX, outsideY)?.Element);
        Click(bridge, root, outsideX, outsideY);

        Assert.False(first.IsOpen);
        Assert.False(second.IsOpen);
        Assert.Equal(1, dismissals);
    }

    [Fact]
    public void DismissScopeKeepsNestedOverlayGroupOpenForFocusTransitionsAndClosesItTogether()
    {
        UIRoot root = new(180, 120);
        Button target = new() { Content = "target" };
        Button firstContent = new() { Content = "first" };
        Button secondContent = new() { Content = "second" };
        Button outside = new() { Content = "outside" };
        Overlay? first = null;
        Overlay? second = null;
        OverlayDismissScope scope = new(() =>
        {
            first!.IsOpen = false;
            second!.IsOpen = false;
        });
        first = new Overlay
        {
            PlacementTarget = target,
            Content = firstContent,
            IsLightDismissEnabled = true,
            DismissScope = scope,
            IsOpen = true
        };
        second = new Overlay
        {
            PlacementTarget = firstContent,
            Content = secondContent,
            IsLightDismissEnabled = true,
            DismissScope = scope,
            IsOpen = true
        };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetTop(outside, 80);
        canvas.VisualChildren.Add(target);
        canvas.VisualChildren.Add(outside);
        canvas.VisualChildren.Add(first);
        canvas.VisualChildren.Add(second);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        ElementInputRouteMap routes = root.InputCache.EnsureCurrent(root);

        bridge.FocusManager.Focus(target, routes);
        bridge.FocusManager.Focus(firstContent, routes);
        bridge.FocusManager.Focus(secondContent, routes);

        Assert.True(first.IsOpen);
        Assert.True(second.IsOpen);

        bridge.FocusManager.Focus(outside, routes);

        Assert.False(first.IsOpen);
        Assert.False(second.IsOpen);
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

    // A Closed handler throws while the overlay closes. The exception reaches the caller, and the root-owned
    // layer stays attached only while another overlay is still open.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClosedHandlerFailureOnCloseStillDetachesTheEmptyLayer(bool otherOverlayOpen)
    {
        UIRoot root = new(100, 100);
        LayoutCanvas canvas = new();
        Overlay overlay = new() { Content = new FixedElement(new LayoutSize(30, 15)), IsOpen = true };
        FixedElement otherContent = new(new LayoutSize(20, 10));
        Overlay other = new() { Content = otherContent, IsOpen = otherOverlayOpen };
        canvas.VisualChildren.Add(overlay);
        canvas.VisualChildren.Add(other);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        overlay.Closed += (_, _) => throw new InvalidOperationException("Closed handler failed");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => overlay.IsOpen = false);
        root.ProcessFrame();

        Assert.Equal("Closed handler failed", exception.Message);
        Assert.False(overlay.IsOpen);
        AssertLayer(root, canvas, otherOverlayOpen ? otherContent : null);
    }

    // A Closed handler throws while removing the overlay's parent closes it. The exception reaches the caller
    // after the overlay is closed and detached, and the layer stays only for another open overlay.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClosedHandlerFailureOnDetachStillClosesAndDetachesTheOverlay(bool otherOverlayOpen)
    {
        UIRoot root = new(100, 100);
        LayoutCanvas canvas = new();
        Overlay overlay = new() { Content = new FixedElement(new LayoutSize(30, 15)), IsOpen = true };
        canvas.VisualChildren.Add(overlay);
        LayoutCanvas otherCanvas = new();
        FixedElement otherContent = new(new LayoutSize(20, 10));
        otherCanvas.VisualChildren.Add(new Overlay { Content = otherContent, IsOpen = otherOverlayOpen });
        root.VisualChildren.Add(canvas);
        root.VisualChildren.Add(otherCanvas);
        root.ProcessFrame();
        overlay.Closed += (_, _) => throw new InvalidOperationException("Closed handler failed");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => root.VisualChildren.Remove(canvas));
        root.ProcessFrame();

        Assert.Equal("Closed handler failed", exception.Message);
        Assert.False(overlay.IsOpen);
        Assert.Null(overlay.Root);
        Assert.Null(canvas.Root);
        AssertLayer(root, otherCanvas, otherOverlayOpen ? otherContent : null);
    }

    // An Unloaded handler throws while removing the overlay's parent. The overlay is still closed and its
    // projection withdrawn, then the handler's exception reaches the caller; the layer stays only for another
    // open overlay.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnloadedHandlerFailureOnDetachStillClosesAndDetachesTheOverlay(bool otherOverlayOpen)
    {
        UIRoot root = new(100, 100);
        LayoutCanvas canvas = new();
        Overlay overlay = new() { Content = new FixedElement(new LayoutSize(30, 15)), IsOpen = true };
        canvas.VisualChildren.Add(overlay);
        LayoutCanvas otherCanvas = new();
        FixedElement otherContent = new(new LayoutSize(20, 10));
        otherCanvas.VisualChildren.Add(new Overlay { Content = otherContent, IsOpen = otherOverlayOpen });
        root.VisualChildren.Add(canvas);
        root.VisualChildren.Add(otherCanvas);
        root.ProcessFrame();
        int closed = 0;
        overlay.Closed += (_, _) => closed++;
        overlay.Unloaded += (_, _) => throw new InvalidOperationException("Unloaded handler failed");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => root.VisualChildren.Remove(canvas));
        root.ProcessFrame();

        Assert.Equal("Unloaded handler failed", exception.Message);
        Assert.False(overlay.IsOpen);
        Assert.Equal(1, closed);
        Assert.Null(overlay.Root);
        AssertLayer(root, otherCanvas, otherOverlayOpen ? otherContent : null);
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

    private static void Click(ElementInputBridge bridge, UIRoot root, float x, float y)
    {
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
    }

    // Index of the first FillRectangle command that uses the brush, or -1.
    private static int IndexOfFill(Cerneala.Drawing.DrawCommandList commands, Cerneala.UI.Media.Brush brush)
    {
        for (int index = 0; index < commands.Count; index++)
        {
            if (commands[index].Kind == Cerneala.Drawing.DrawCommandKind.FillRectangle && ReferenceEquals(commands[index].Brush, brush))
            {
                return index;
            }
        }

        return -1;
    }

    private static T FindAncestor<T>(UIElement element)
        where T : UIElement
    {
        for (UIElement? current = element.VisualParent; current is not null; current = current.VisualParent)
        {
            if (current is T match)
            {
                return match;
            }
        }

        throw new InvalidOperationException($"No {typeof(T).Name} ancestor.");
    }

    // The root holds the content element and, only while projected content remains, the overlay layer last.
    private static void AssertLayer(UIRoot root, UIElement content, UIElement? projectedContent)
    {
        if (projectedContent is null)
        {
            Assert.Same(content, Assert.Single(root.VisualChildren));
            return;
        }

        Assert.Equal(2, root.VisualChildren.Count);
        Assert.Same(content, root.VisualChildren[0]);
        Assert.Same(projectedContent.VisualParent, Assert.Single(root.VisualChildren[1].VisualChildren));
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }

    private sealed class FillingElement(LayoutSize naturalSize) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            float width = float.IsFinite(context.AvailableSize.Width) ? context.AvailableSize.Width : naturalSize.Width;
            return new LayoutSize(width, naturalSize.Height);
        }
    }

    private sealed class RecordingElement(LayoutSize size) : UIElement
    {
        public List<LayoutSize> MeasureConstraints { get; } = [];

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureConstraints.Add(context.AvailableSize);
            return size;
        }
    }
}
