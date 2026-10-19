using System.Runtime.CompilerServices;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Input;

public sealed class ElementInputBridgeTests
{
    [Fact]
    public void MouseDownRaisesPreviewBeforeBubbleOnHitElement()
    {
        UIRoot root = RootWithChild(out UIElement child);
        List<string> calls = [];
        root.Handlers.AddHandler(InputEvents.PreviewMouseDownEvent, (_, _) => calls.Add("preview-root"));
        child.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => calls.Add("bubble-child"));

        new ElementInputBridge().Dispatch(root, PointerFrame(10, 10, pressed: true));

        Assert.Equal(["preview-root", "bubble-child"], calls);
    }

    // As in WPF, the button-specific events carry the Handled value of the MouseDown or MouseUp they report:
    // ordinary handlers on the ancestors do not run, handlers added with handledEventsToo still run.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HandledMouseDownOrUpStopsOrdinaryLeftButtonHandlersOnAncestors(bool isDown)
    {
        UIRoot root = RootWithChild(out UIElement child);
        child.Handlers.AddHandler(isDown ? InputEvents.MouseDownEvent : InputEvents.MouseUpEvent, (_, args) => args.Handled = true);
        RoutedEvent buttonEvent = isDown ? InputEvents.MouseLeftButtonDownEvent : InputEvents.MouseLeftButtonUpEvent;
        int ordinaryCalls = 0;
        int handledTooCalls = 0;
        root.Handlers.AddHandler(buttonEvent, (_, _) => ordinaryCalls++);
        root.Handlers.AddHandler(buttonEvent, (_, _) => handledTooCalls++, handledEventsToo: true);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, pressed: true));
        bridge.Dispatch(root, PointerFrame(10, 10, 10, 10, previousDown: true));

        Assert.Equal(0, ordinaryCalls);
        Assert.Equal(1, handledTooCalls);
    }

    [Fact]
    public void HandledPreviewMouseDownStopsOrdinaryPreviewLeftButtonDownHandlers()
    {
        UIRoot root = RootWithChild(out UIElement child);
        root.Handlers.AddHandler(InputEvents.PreviewMouseDownEvent, (_, args) => args.Handled = true);
        int ordinaryCalls = 0;
        int handledTooCalls = 0;
        child.Handlers.AddHandler(InputEvents.PreviewMouseLeftButtonDownEvent, (_, _) => ordinaryCalls++);
        child.Handlers.AddHandler(InputEvents.PreviewMouseLeftButtonDownEvent, (_, _) => handledTooCalls++, handledEventsToo: true);

        new ElementInputBridge().Dispatch(root, PointerFrame(10, 10, pressed: true));

        Assert.Equal(0, ordinaryCalls);
        Assert.Equal(1, handledTooCalls);
    }

    [Fact]
    public void HandledRightMouseUpStopsOrdinaryRightButtonUpHandlersOnAncestors()
    {
        UIRoot root = RootWithChild(out UIElement child);
        child.Handlers.AddHandler(InputEvents.MouseUpEvent, (_, args) => args.Handled = true);
        int ordinaryCalls = 0;
        int handledTooCalls = 0;
        root.Handlers.AddHandler(InputEvents.MouseRightButtonUpEvent, (_, _) => ordinaryCalls++);
        root.Handlers.AddHandler(InputEvents.MouseRightButtonUpEvent, (_, _) => handledTooCalls++, handledEventsToo: true);
        PointerSnapshot released = PointerSnapshot.Empty.WithPosition(10, 10);
        PointerSnapshot pressed = released.WithButton(InputMouseButton.Right, true);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, new InputFrame(released, pressed, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        bridge.Dispatch(root, new InputFrame(pressed, released, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));

        Assert.Equal(0, ordinaryCalls);
        Assert.Equal(1, handledTooCalls);
    }

    [Fact]
    public void MouseMoveUpdatesHoverAndInvalidatesBeforeScheduler()
    {
        UIRoot root = RootWithChild(out UIElement child);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10));
        FrameStats stats = root.ProcessFrame();

        Assert.True(child.IsPointerOver);
        Assert.True(stats.RenderedElements > 0);
    }

    [Fact]
    public void StationaryPointerFrameDoesNotRaiseMouseMove()
    {
        UIRoot root = RootWithChild(out UIElement child);
        int moveCount = 0;
        child.Handlers.AddHandler(InputEvents.MouseMoveEvent, (_, _) => moveCount++);

        new ElementInputBridge().Dispatch(root, PointerFrame(10, 10));

        Assert.Equal(0, moveCount);
    }

    [Fact]
    public void PointerMoveRaisesMouseEventArgsWithoutButtonState()
    {
        UIRoot root = RootWithChild(out UIElement child);
        ElementInputBridge bridge = new();
        RoutedEventArgs? receivedArgs = null;
        child.Handlers.AddHandler(InputEvents.MouseMoveEvent, (_, args) => receivedArgs = args);

        bridge.Dispatch(root, PointerFrame(9, 10));
        bridge.Dispatch(root, PointerFrame(9, 10, 10, 10));

        MouseEventArgs mouseArgs = Assert.IsType<MouseEventArgs>(receivedArgs);
        Assert.Equal(10, mouseArgs.X);
        Assert.Equal(10, mouseArgs.Y);
    }

    [Fact]
    public void DisabledElementDoesNotReceiveHandlers()
    {
        UIRoot root = RootWithChild(out UIElement child);
        child.IsEnabled = false;
        bool called = false;
        child.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => called = true);

        new ElementInputBridge().Dispatch(root, PointerFrame(10, 10, pressed: true));

        Assert.False(called);
    }

    [Fact]
    public void PointerPressOnPlainUiElementDoesNotFocusIt()
    {
        UIRoot root = RootWithChild(out UIElement child);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, pressed: true));

        Assert.Null(bridge.FocusManager.FocusedElement);
        Assert.False(child.IsKeyboardFocused);
    }

    [Fact]
    public void PointerPressOnButtonFocusesButton()
    {
        UIRoot root = new(100, 100);
        Button button = new();
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 40)));
        root.VisualChildren.Add(button);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, pressed: true));

        Assert.Same(button, bridge.FocusManager.FocusedElement);
        Assert.True(button.IsKeyboardFocused);
    }

    [Fact]
    public void PointerPressOnButtonElementContentFocusesOwningButton()
    {
        UIRoot root = new(100, 100);
        UIElement content = new();
        Button button = new()
        {
            Content = content
        };
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 40)));
        root.VisualChildren.Add(button);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, pressed: true));

        Assert.Same(button, bridge.FocusManager.FocusedElement);
        Assert.True(button.IsKeyboardFocused);
        Assert.False(content.IsKeyboardFocused);
    }

    [Fact]
    public void PointerHoverOverButtonElementContentMarksOwningButtonPointerOver()
    {
        UIRoot root = new(100, 100);
        UIElement content = new();
        Button button = new()
        {
            Content = content
        };
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 40)));
        root.VisualChildren.Add(button);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10));

        Assert.True(content.IsPointerOver);
        Assert.True(button.IsPointerOver);
    }

    [Fact]
    public void PointerPressOnNonFocusableButtonRoutesMouseWithoutFocusing()
    {
        UIRoot root = new(100, 100);
        Button button = new()
        {
            Focusable = false
        };
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 40)));
        root.VisualChildren.Add(button);
        bool mouseDownCalled = false;
        button.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => mouseDownCalled = true);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, pressed: true));

        Assert.True(mouseDownCalled);
        Assert.Null(bridge.FocusManager.FocusedElement);
        Assert.False(button.IsKeyboardFocused);
    }

    [Fact]
    public void CapturedButtonReleaseOutsideDoesNotExecuteCommand()
    {
        UIRoot root = new(100, 100);
        ButtonBase button = ArrangedButton(0, 0, 40, 40);
        UIElement other = HitTestServiceTests.Arranged(50, 0, 40, 40);
        root.VisualChildren.Add(button);
        root.VisualChildren.Add(other);
        bool executed = false;
        button.Command = new ActionCommand(_ => executed = true);
        ElementInputBridge bridge = new();
        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        bridge.PointerCaptureManager.Capture(button, routeMap);

        bridge.Dispatch(root, PointerFrame(10, 10, 10, 10, currentDown: true));
        bridge.Dispatch(root, PointerFrame(10, 10, 60, 10, previousDown: true));

        Assert.False(executed);
    }

    [Fact]
    public void ButtonCommandExecutesWhenPressAndReleaseHitDifferentChildrenInsideSameButton()
    {
        UIRoot root = new(100, 100);
        ButtonBase button = ArrangedButton(0, 0, 40, 40);
        UIElement left = HitTestServiceTests.Arranged(0, 0, 20, 40);
        UIElement right = HitTestServiceTests.Arranged(20, 0, 20, 40);
        button.VisualChildren.Add(left);
        button.VisualChildren.Add(right);
        root.VisualChildren.Add(button);
        bool executed = false;
        button.Command = new ActionCommand(_ => executed = true);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, pressed: true));
        bridge.Dispatch(root, PointerFrame(10, 10, 30, 10, previousDown: true));

        Assert.True(executed);
    }

    [Fact]
    public void ButtonRaisesClickOnlyOnValidLeftButtonRelease()
    {
        UIRoot root = new(100, 100);
        ButtonBase button = ArrangedButton(0, 0, 40, 40);
        root.VisualChildren.Add(button);
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, pressed: true));

        Assert.Equal(0, clickCount);
        Assert.True(button.IsPressed);

        bridge.Dispatch(root, PointerFrame(10, 10, 10, 10, previousDown: true));

        Assert.Equal(1, clickCount);
        Assert.False(button.IsPressed);
    }

    [Fact]
    public void ButtonCommandExecutesExactlyOnceForOneClick()
    {
        UIRoot root = new(100, 100);
        ButtonBase button = ArrangedButton(0, 0, 40, 40);
        root.VisualChildren.Add(button);
        int executionCount = 0;
        button.Command = new ActionCommand(_ => executionCount++);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, pressed: true));
        bridge.Dispatch(root, PointerFrame(10, 10, 10, 10, previousDown: true));

        Assert.Equal(1, executionCount);
    }

    [Fact]
    public void ReleaseAfterDetachClearsPressedStateWithoutClickOrCommand()
    {
        UIRoot root = new(100, 100);
        ButtonBase button = ArrangedButton(0, 0, 40, 40);
        root.VisualChildren.Add(button);
        int clickCount = 0;
        int executionCount = 0;
        button.Click += (_, _) => clickCount++;
        button.Command = new ActionCommand(_ => executionCount++);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, pressed: true));
        root.VisualChildren.Remove(button);
        bridge.Dispatch(root, PointerFrame(10, 10, 10, 10, previousDown: true));

        Assert.False(button.IsPressed);
        Assert.Null(bridge.PressedStateTracker.PressedElement);
        Assert.Equal(0, clickCount);
        Assert.Equal(0, executionCount);
    }

    [Fact]
    public void WheelEventsAfterAWheelHandlerRemovedTheTargetGoToTheElementNowUnderThePointer()
    {
        UIRoot root = RootWithFrontAndBack(out UIElement front, out UIElement back);
        List<string> calls = [];
        front.Handlers.AddHandler(InputEvents.MouseWheelEvent, (_, _) =>
        {
            calls.Add("front");
            root.VisualChildren.Remove(front);
        });
        back.Handlers.AddHandler(InputEvents.MouseWheelEvent, (_, _) => calls.Add("back"));
        PointerSnapshot pointer = PointerSnapshot.Empty.WithPosition(10, 10);

        new ElementInputBridge().Dispatch(root, new InputFrame(
            pointer,
            pointer.WithWheelValue(-360),
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            [],
            [],
            [-120, -120, -120]));

        Assert.Equal(["front", "back", "back"], calls);
    }

    [Fact]
    public void WheelAfterAMouseMoveHandlerRemovedTheTargetGoesToTheElementNowUnderThePointer()
    {
        UIRoot root = RootWithFrontAndBack(out UIElement front, out UIElement back);
        List<string> calls = [];
        front.Handlers.AddHandler(InputEvents.MouseMoveEvent, (_, _) =>
        {
            calls.Add("front move");
            root.VisualChildren.Remove(front);
        });
        front.Handlers.AddHandler(InputEvents.MouseWheelEvent, (_, _) => calls.Add("front wheel"));
        back.Handlers.AddHandler(InputEvents.MouseWheelEvent, (_, _) => calls.Add("back wheel"));
        ElementInputBridge bridge = new();
        PointerSnapshot first = PointerSnapshot.Empty.WithPosition(10, 10);

        bridge.Dispatch(root, new InputFrame(first, first, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        bridge.Dispatch(root, new InputFrame(
            first,
            first.WithPosition(20, 20).WithWheelValue(-120),
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            []));

        Assert.Equal(["front move", "back wheel"], calls);
    }

    [Fact]
    public void ButtonPressAfterAWheelHandlerRemovedTheTargetGoesToTheElementNowUnderThePointer()
    {
        UIRoot root = RootWithFrontAndBack(out UIElement front, out UIElement back);
        List<string> calls = [];
        front.Handlers.AddHandler(InputEvents.MouseWheelEvent, (_, _) =>
        {
            calls.Add("front wheel");
            root.VisualChildren.Remove(front);
        });
        front.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => calls.Add("front down"));
        back.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => calls.Add("back down"));
        PointerSnapshot pointer = PointerSnapshot.Empty.WithPosition(10, 10);

        new ElementInputBridge().Dispatch(root, new InputFrame(
            pointer,
            pointer.WithWheelValue(-120).WithButton(InputMouseButton.Left, true),
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            []));

        Assert.Equal(["front wheel", "back down"], calls);
    }

    [Fact]
    public void ButtonReleaseAfterAMouseMoveHandlerRemovedTheTargetGoesToTheElementNowUnderThePointer()
    {
        UIRoot root = RootWithFrontAndBack(out UIElement front, out UIElement back);
        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(10, 10, pressed: true));
        List<string> calls = [];
        front.Handlers.AddHandler(InputEvents.MouseMoveEvent, (_, _) =>
        {
            calls.Add("front move");
            root.VisualChildren.Remove(front);
        });
        front.Handlers.AddHandler(InputEvents.MouseUpEvent, (_, _) => calls.Add("front up"));
        back.Handlers.AddHandler(InputEvents.MouseUpEvent, (_, _) => calls.Add("back up"));

        bridge.Dispatch(root, PointerFrame(10, 10, 20, 20, previousDown: true));

        Assert.Equal(["front move", "back up"], calls);
    }

    [Fact]
    public void MouseMoveAfterAMouseEnterHandlerRemovedTheTargetGoesToTheElementNowUnderThePointer()
    {
        UIRoot root = RootWithFrontAndBack(out UIElement front, out UIElement back);
        List<string> calls = [];
        front.Handlers.AddHandler(InputEvents.MouseEnterEvent, (_, _) =>
        {
            calls.Add("front enter");
            root.VisualChildren.Remove(front);
        });
        front.Handlers.AddHandler(InputEvents.MouseMoveEvent, (_, _) => calls.Add("front move"));
        back.Handlers.AddHandler(InputEvents.MouseMoveEvent, (_, _) => calls.Add("back move"));
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(60, 60));
        bridge.Dispatch(root, PointerFrame(60, 60, 10, 10));

        Assert.Equal(["front enter", "back move"], calls);
    }

    [Fact]
    public void HostUpdateDispatchesInputBeforeScheduler()
    {
        UIRoot root = RootWithChild(out UIElement child);
        UiHost host = new(new UiHostOptions { Root = root });

        UiFrame frame = host.Update(PointerFrame(10, 10), new UiViewport(100, 100), TimeSpan.Zero);

        Assert.True(child.IsPointerOver);
        Assert.True(frame.Stats.RenderedElements > 0);
    }

    [Fact]
    public void DroppedBridgeIsCollectableWhileItsSharedFocusManagerLives()
    {
        FocusManager sharedFocusManager = new();
        WeakReference droppedBridge = CreateDroppedBridge(sharedFocusManager);

        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False(droppedBridge.IsAlive);
        GC.KeepAlive(sharedFocusManager);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateDroppedBridge(FocusManager sharedFocusManager)
    {
        return new WeakReference(new ElementInputBridge(focusManager: sharedFocusManager));
    }

    private static UIRoot RootWithChild(out UIElement child)
    {
        UIRoot root = new(100, 100);
        child = HitTestServiceTests.Arranged(0, 0, 40, 40);
        root.VisualChildren.Add(child);
        return root;
    }

    private static UIRoot RootWithFrontAndBack(out UIElement front, out UIElement back)
    {
        UIRoot root = new(100, 100);
        back = HitTestServiceTests.Arranged(0, 0, 40, 40);
        front = HitTestServiceTests.Arranged(0, 0, 40, 40);
        root.VisualChildren.Add(back);
        root.VisualChildren.Add(front);
        return root;
    }

    private static ButtonBase ArrangedButton(float x, float y, float width, float height)
    {
        ButtonBase button = new();
        button.Arrange(new ArrangeContext(new LayoutRect(x, y, width, height)));
        return button;
    }

    private static InputFrame PointerFrame(float x, float y, bool pressed = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y);
        if (pressed)
        {
            current = current.WithButton(InputMouseButton.Left, true);
        }

        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static InputFrame PointerFrame(
        float previousX,
        float previousY,
        float currentX,
        float currentY,
        bool previousDown = false,
        bool currentDown = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(previousX, previousY);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(currentX, currentY);
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
}
