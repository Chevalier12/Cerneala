using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class BringIntoViewTests
{
    // As WPF's FrameworkElement.BringIntoView, a Button below the viewport of a ScrollViewer 100 pixels high scrolls up
    // until its bottom is at the bottom of the viewport: the Button ends at 320, so the offset becomes 220. A Button above
    // the viewport scrolls down until its top is at the top: the first Button starts at 10, so the offset becomes 10 (#702).
    [Fact]
    public void BringIntoViewScrollsTheScrollViewerByTheLeastAmount()
    {
        Button top = new() { Height = 20 };
        Button bottom = new() { Height = 20 };
        StackPanel stack = new();
        stack.VisualChildren.Add(new Border { Height = 10 });
        stack.VisualChildren.Add(top);
        stack.VisualChildren.Add(new Border { Height = 270 });
        stack.VisualChildren.Add(bottom);
        stack.VisualChildren.Add(new Border { Height = 200 });
        (UIRoot root, ScrollViewer viewer) = CreateViewer(stack, width: 200, height: 100);

        bottom.BringIntoView();
        root.ProcessFrame();

        Assert.Equal(220, viewer.Presenter.VerticalOffset);
        AssertInsideViewport(viewer, bottom);

        top.BringIntoView();
        root.ProcessFrame();

        Assert.Equal(10, viewer.Presenter.VerticalOffset);
        AssertInsideViewport(viewer, top);
    }

    // BringIntoView with a rectangle brings only that part of the element into view: the top 5 pixels of a Button that
    // starts at 300 end at 305, so the offset of the viewport 100 pixels high becomes 205 (#702).
    [Fact]
    public void BringIntoViewOfARectangleScrollsOnlyUntilThatPartIsVisible()
    {
        Button button = new() { Height = 20 };
        StackPanel stack = new();
        stack.VisualChildren.Add(new Border { Height = 300 });
        stack.VisualChildren.Add(button);
        (UIRoot root, ScrollViewer viewer) = CreateViewer(stack, width: 200, height: 100);

        button.BringIntoView(new LayoutRect(0, 0, 10, 5));
        root.ProcessFrame();

        Assert.Equal(205, viewer.Presenter.VerticalOffset);
    }

    // As WPF's ScrollViewer, which brings its own visible part into view after it scrolls, nested ScrollViewers both
    // scroll: the inner one 100 pixels high scrolls the Button at 300 to its bottom (offset 220), and the outer one,
    // where the inner viewer starts at 200, scrolls until the Button at 280 to 300 is visible (offset 200) (#702).
    [Fact]
    public void BringIntoViewScrollsNestedScrollViewers()
    {
        Button button = new() { Height = 20 };
        StackPanel innerStack = new();
        innerStack.VisualChildren.Add(new Border { Height = 300 });
        innerStack.VisualChildren.Add(button);
        ScrollViewer inner = new() { Content = innerStack, Height = 100, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        StackPanel outerStack = new();
        outerStack.VisualChildren.Add(new Border { Height = 200 });
        outerStack.VisualChildren.Add(inner);
        outerStack.VisualChildren.Add(new Border { Height = 200 });
        (UIRoot root, ScrollViewer outer) = CreateViewer(outerStack, width: 200, height: 100);

        button.BringIntoView();
        root.ProcessFrame();

        Assert.Equal(220, inner.Presenter.VerticalOffset);
        Assert.Equal(200, outer.Presenter.VerticalOffset);
        AssertInsideViewport(inner, button);
        AssertInsideViewport(outer, button);
    }

    // As in WPF, a RequestBringIntoView handler between the element and the ScrollViewer that marks the event handled
    // stops it: the ScrollViewer does not scroll. The handler gets the element as TargetObject, and LayoutRect.Empty
    // (the whole element) or the requested rectangle as TargetRect (#702).
    [Fact]
    public void HandledRequestBringIntoViewDoesNotScrollAndCarriesTheTarget()
    {
        Button button = new() { Height = 20 };
        StackPanel stack = new();
        stack.VisualChildren.Add(new Border { Height = 300 });
        stack.VisualChildren.Add(button);
        (UIRoot root, ScrollViewer viewer) = CreateViewer(stack, width: 200, height: 100);
        List<RequestBringIntoViewEventArgs> requests = [];
        stack.RequestBringIntoView += (_, args) =>
        {
            requests.Add(args);
            args.Handled = true;
        };

        button.BringIntoView();
        button.BringIntoView(new LayoutRect(1, 2, 3, 4));
        root.ProcessFrame();

        Assert.Equal(0, viewer.Presenter.VerticalOffset);
        Assert.Equal(2, requests.Count);
        Assert.All(requests, request => Assert.Same(button, request.TargetObject));
        Assert.All(requests, request => Assert.Same(UIElement.RequestBringIntoViewEvent, request.RoutedEvent));
        Assert.Equal(LayoutRect.Empty, requests[0].TargetRect);
        Assert.Equal(new LayoutRect(1, 2, 3, 4), requests[1].TargetRect);
    }

    // A ScrollViewer 100 pixels wide that scrolls horizontally brings a Button 40 pixels wide that starts at 300 into view
    // by scrolling until its right edge at 340 is at the right of the viewport: the offset becomes 240 (#702).
    [Fact]
    public void BringIntoViewScrollsHorizontally()
    {
        Button button = new() { Width = 40 };
        StackPanel stack = new() { Orientation = Orientation.Horizontal };
        stack.VisualChildren.Add(new Border { Width = 300 });
        stack.VisualChildren.Add(button);
        stack.VisualChildren.Add(new Border { Width = 200 });
        (UIRoot root, ScrollViewer viewer) = CreateViewer(stack, width: 100, height: 50);
        viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        root.ProcessFrame();

        button.BringIntoView();
        root.ProcessFrame();

        Assert.Equal(240, viewer.Presenter.HorizontalOffset);
        Assert.Equal(0, viewer.Presenter.VerticalOffset);
    }

    // As WPF's FrameworkElement.OnGotFocus, which brings an element that gets the keyboard focus into view, Tab scrolls
    // a ScrollViewer 90 pixels high by the least amount that shows the focused element. Tab from "before" focuses
    // "first" at 0 to 30, which is already visible, so nothing scrolls; the next Tab focuses "deep" at 300 to 330, so the
    // offset becomes 330 - 90 = 240; Shift+Tab focuses "first" again, so the offset goes back to 0 (#1158).
    [Fact]
    public void TabAndShiftTabScrollTheFocusedElementIntoView()
    {
        Button before = new() { Content = "before", Width = 100, Height = 30 };
        Button first = new() { Content = "first", Width = 100, Height = 30 };
        Button deep = new() { Content = "deep", Width = 100, Height = 30 };
        StackPanel inner = new();
        inner.VisualChildren.Add(first);
        for (int i = 0; i < 9; i++)
        {
            inner.VisualChildren.Add(new Border { Width = 50, Height = 30 });
        }

        inner.VisualChildren.Add(deep);
        ScrollViewer viewer = new() { Content = inner, Width = 150, Height = 90 };
        (UIRoot root, ElementInputBridge bridge) = CreateTabRoot(before, viewer);
        ClickCenter(root, bridge, before);

        PressKey(root, bridge, InputKey.Tab);

        Assert.True(first.IsKeyboardFocused);
        Assert.Equal(0, viewer.Presenter.VerticalOffset);

        PressKey(root, bridge, InputKey.Tab);

        Assert.True(deep.IsKeyboardFocused);
        Assert.Equal(240, viewer.Presenter.VerticalOffset);
        AssertInsideViewport(viewer, deep);

        PressKey(root, bridge, InputKey.Tab, InputKey.LeftShift);

        Assert.True(first.IsKeyboardFocused);
        Assert.Equal(0, viewer.Presenter.VerticalOffset);
        AssertInsideViewport(viewer, first);
    }

    // With PR #1039, Tab enters a ListBox on its selected row. With 100 rows in a list 100 pixels high and row 80
    // selected, that row is below the viewport, so the list scrolls until the row is visible (#1158).
    [Fact]
    public void TabIntoAListBoxScrollsTheSelectedRowIntoView()
    {
        Button before = new() { Content = "before", Width = 100, Height = 30 };
        ListBox listBox = new() { Width = 150, Height = 100 };
        listBox.SetItems(Enumerable.Range(0, 100).Select(index => $"R{index}").ToArray());
        listBox.SelectedIndex = 80;
        (UIRoot root, ElementInputBridge bridge) = CreateTabRoot(before, listBox);
        ScrollViewer viewer = FindDescendant<ScrollViewer>(listBox)!;
        ClickCenter(root, bridge, before);
        Assert.Equal(0, viewer.Presenter.VerticalOffset);

        PressKey(root, bridge, InputKey.Tab);

        UIElement row = listBox.ItemContainerGenerator.RealizedContainers[80];
        Assert.Same(row, bridge.FocusManager.FocusedElement);
        Assert.True(viewer.Presenter.VerticalOffset > 0, $"VerticalOffset stayed {viewer.Presenter.VerticalOffset}.");
        AssertInsideViewport(viewer, row);
    }

    // The button and then the target in a StackPanel in a window, after the first layout.
    private static (UIRoot Root, ElementInputBridge Bridge) CreateTabRoot(UIElement before, UIElement target)
    {
        StackPanel panel = new();
        panel.VisualChildren.Add(before);
        panel.VisualChildren.Add(target);
        UIRoot root = new(360, 400);
        root.VisualChildren.Add(new Window { Content = panel, Width = 360, Height = 400 });
        root.ProcessFrame();
        return (root, new ElementInputBridge());
    }

    private static void ClickCenter(UIRoot root, ElementInputBridge bridge, UIElement element)
    {
        LayoutRect bounds = element.ArrangedBounds;
        float x = bounds.X + bounds.Width / 2;
        float y = bounds.Y + bounds.Height / 2;
        bridge.Dispatch(root, new InputFrame(
            PointerSnapshot.Empty.WithPosition(x, y),
            PointerSnapshot.Empty.WithPosition(x, y).WithButton(InputMouseButton.Left, true),
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            []));
        root.ProcessFrame();
        bridge.Dispatch(root, new InputFrame(
            PointerSnapshot.Empty.WithPosition(x, y).WithButton(InputMouseButton.Left, true),
            PointerSnapshot.Empty.WithPosition(x, y),
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            []));
        root.ProcessFrame();
    }

    // A press and release of key, with the modifier keys held down around it.
    private static void PressKey(UIRoot root, ElementInputBridge bridge, InputKey key, params InputKey[] modifiers)
    {
        InputKey[] withKey = [.. modifiers, key];
        bridge.Dispatch(root, KeyFrame([], modifiers));
        bridge.Dispatch(root, KeyFrame(modifiers, withKey));
        bridge.Dispatch(root, KeyFrame(withKey, modifiers));
        bridge.Dispatch(root, KeyFrame(modifiers, []));
        root.ProcessFrame();
    }

    private static InputFrame KeyFrame(InputKey[] previous, InputKey[] current)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys(previous),
            KeyboardSnapshot.FromDownKeys(current),
            []);
    }

    private static T? FindDescendant<T>(UIElement element)
        where T : UIElement
    {
        if (element is T match)
        {
            return match;
        }

        foreach (UIElement child in element.VisualChildren)
        {
            if (FindDescendant<T>(child) is T found)
            {
                return found;
            }
        }

        return null;
    }

    // The viewer as the only child of a root of the given size, after the first layout.
    private static (UIRoot Root, ScrollViewer Viewer) CreateViewer(UIElement content, float width, float height)
    {
        ScrollViewer viewer = new() { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        UIRoot root = new(width, height);
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();
        return (root, viewer);
    }

    private static void AssertInsideViewport(ScrollViewer viewer, UIElement element)
    {
        LayoutRect viewport = viewer.Presenter.ArrangedBounds;
        LayoutRect bounds = element.ArrangedBounds;
        Assert.InRange(bounds.Y, viewport.Y, viewport.Y + viewport.Height - bounds.Height);
    }
}
