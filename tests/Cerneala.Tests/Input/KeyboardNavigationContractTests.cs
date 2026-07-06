using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Input;

public sealed class KeyboardNavigationContractTests
{
    [Fact]
    public void TabWithNoFocusFocusesFirstTabStopInVisualOrder()
    {
        UIRoot root = RootWithTabStops(out TextBox first, out Button _, out TextBox _);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, KeyPressFrame(InputKey.Tab));

        Assert.Same(first, bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void TabMovesToNextTabStopInVisualOrder()
    {
        UIRoot root = RootWithTabStops(out TextBox first, out Button second, out TextBox _);
        ElementInputBridge bridge = FocusedBridge(root, first);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Tab));

        Assert.Same(second, bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void ShiftTabMovesToPreviousTabStopInVisualOrder()
    {
        UIRoot root = RootWithTabStops(out TextBox first, out Button second, out TextBox _);
        ElementInputBridge bridge = FocusedBridge(root, second);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Tab, InputKey.LeftShift));

        Assert.Same(first, bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void TabNavigationWrapsFromLastToFirst()
    {
        UIRoot root = RootWithTabStops(out TextBox first, out Button _, out TextBox last);
        ElementInputBridge bridge = FocusedBridge(root, last);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Tab));

        Assert.Same(first, bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void ShiftTabNavigationWrapsFromFirstToLast()
    {
        UIRoot root = RootWithTabStops(out TextBox first, out Button _, out TextBox last);
        ElementInputBridge bridge = FocusedBridge(root, first);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Tab, InputKey.LeftShift));

        Assert.Same(last, bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void TabNavigationSkipsDisabledHiddenCollapsedDetachedAndNonTabStopElements()
    {
        UIRoot root = new();
        Button disabled = Arranged(new Button { IsEnabled = false });
        Button hidden = Arranged(new Button { Visibility = Visibility.Hidden });
        Button collapsed = Arranged(new Button { Visibility = Visibility.Collapsed });
        UIElement nonTabStop = Arranged(new UIElement { Focusable = true, IsTabStop = false });
        Button detached = Arranged(new Button());
        Button valid = Arranged(new Button());
        root.VisualChildren.Add(disabled);
        root.VisualChildren.Add(hidden);
        root.VisualChildren.Add(collapsed);
        root.VisualChildren.Add(nonTabStop);
        root.VisualChildren.Add(valid);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, KeyPressFrame(InputKey.Tab));

        Assert.False(detached.IsAttached);
        Assert.Same(valid, bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void TabFromFocusedNonTabStopMovesToNextValidTargetInVisualOrder()
    {
        UIRoot root = new();
        Button first = Arranged(new Button());
        UIElement focusedNonTabStop = Arranged(new UIElement { Focusable = true, IsTabStop = false });
        Button next = Arranged(new Button());
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(focusedNonTabStop);
        root.VisualChildren.Add(next);
        ElementInputBridge bridge = FocusedBridge(root, focusedNonTabStop);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Tab));

        Assert.Same(next, bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void ShiftTabFromFocusedNonTabStopMovesToPreviousValidTargetInVisualOrder()
    {
        UIRoot root = new();
        Button previous = Arranged(new Button());
        UIElement focusedNonTabStop = Arranged(new UIElement { Focusable = true, IsTabStop = false });
        Button last = Arranged(new Button());
        root.VisualChildren.Add(previous);
        root.VisualChildren.Add(focusedNonTabStop);
        root.VisualChildren.Add(last);
        ElementInputBridge bridge = FocusedBridge(root, focusedNonTabStop);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Tab, InputKey.LeftShift));

        Assert.Same(previous, bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void TabFromFocusedNonTabStopIgnoresItsTabIndexAndUsesVisualPosition()
    {
        UIRoot root = new();
        Button previous = Arranged(new Button());
        UIElement focusedNonTabStop = Arranged(new UIElement { Focusable = true, IsTabStop = false });
        Button next = Arranged(new Button());
        SetTabIndex(focusedNonTabStop, 10);
        root.VisualChildren.Add(previous);
        root.VisualChildren.Add(focusedNonTabStop);
        root.VisualChildren.Add(next);
        ElementInputBridge bridge = FocusedBridge(root, focusedNonTabStop);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Tab));

        Assert.Same(next, bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void TabNavigationRespectsTabIndexThenVisualOrderWhenTabIndexExists()
    {
        UIRoot root = new();
        Button visualFirst = Arranged(new Button());
        Button indexedFirst = Arranged(new Button());
        Button indexedSecond = Arranged(new Button());
        SetTabIndex(visualFirst, 2);
        SetTabIndex(indexedFirst, 1);
        SetTabIndex(indexedSecond, 1);
        root.VisualChildren.Add(visualFirst);
        root.VisualChildren.Add(indexedFirst);
        root.VisualChildren.Add(indexedSecond);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, KeyPressFrame(InputKey.Tab));
        Assert.Same(indexedFirst, bridge.FocusManager.FocusedElement);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Tab));
        Assert.Same(indexedSecond, bridge.FocusManager.FocusedElement);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Tab));
        Assert.Same(visualFirst, bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void TabNavigationDoesNothingWhenNoValidTargetsExist()
    {
        UIRoot root = new();
        root.VisualChildren.Add(Arranged(new UIElement()));
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, KeyPressFrame(InputKey.Tab));

        Assert.Null(bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void HandledPreviewKeyDownSuppressesDefaultTabNavigation()
    {
        UIRoot root = RootWithTabStops(out TextBox first, out Button second, out TextBox _);
        ElementInputBridge bridge = FocusedBridge(root, first);
        first.Handlers.AddHandler(InputEvents.PreviewKeyDownEvent, (_, args) => args.Handled = true);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Tab));

        Assert.NotSame(second, bridge.FocusManager.FocusedElement);
        Assert.Same(first, bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void HandledKeyDownSuppressesDefaultTabNavigation()
    {
        UIRoot root = RootWithTabStops(out TextBox first, out Button second, out TextBox _);
        ElementInputBridge bridge = FocusedBridge(root, first);
        first.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, args) => args.Handled = true);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Tab));

        Assert.NotSame(second, bridge.FocusManager.FocusedElement);
        Assert.Same(first, bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void TabNavigationUsesPreInputRouteOrderWhenKeyHandlerReordersVisualChildren()
    {
        UIRoot root = new();
        Button first = Arranged(new Button());
        Button second = Arranged(new Button());
        Button third = Arranged(new Button());
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        root.VisualChildren.Add(third);
        first.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, _) =>
        {
            Assert.True(root.VisualChildren.Remove(second));
            root.VisualChildren.Add(second);
        });
        ElementInputBridge bridge = FocusedBridge(root, first);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Tab));

        Assert.Same(second, bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void NonTabKeysDoNotInvokeKeyboardNavigation()
    {
        UIRoot root = RootWithTabStops(out TextBox first, out Button _, out TextBox _);
        ElementInputBridge bridge = FocusedBridge(root, first);

        bridge.Dispatch(root, KeyPressFrame(InputKey.A));

        Assert.Same(first, bridge.FocusManager.FocusedElement);
    }

    private static UIRoot RootWithTabStops(out TextBox first, out Button second, out TextBox third)
    {
        UIRoot root = new(100, 100);
        UIElement container = Arranged(new UIElement());
        first = Arranged(new TextBox());
        UIElement plainElement = Arranged(new UIElement { Focusable = true });
        second = Arranged(new Button());
        third = Arranged(new TextBox());

        container.VisualChildren.Add(first);
        container.VisualChildren.Add(plainElement);
        container.VisualChildren.Add(second);
        container.VisualChildren.Add(third);
        root.VisualChildren.Add(container);
        return root;
    }

    private static ElementInputBridge FocusedBridge(UIRoot root, UIElement focusedElement)
    {
        ElementInputBridge bridge = new();
        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        Assert.True(bridge.FocusManager.Focus(focusedElement, routeMap));
        return bridge;
    }

    private static TElement Arranged<TElement>(TElement element)
        where TElement : UIElement
    {
        element.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 20)));
        return element;
    }

    private static InputFrame KeyPressFrame(params InputKey[] currentKeys)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys(currentKeys),
            []);
    }

    private static void SetTabIndex(UIElement element, int value)
    {
        element.TabIndex = value;
    }
}
