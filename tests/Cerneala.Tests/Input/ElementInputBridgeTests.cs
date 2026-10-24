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

    // The MouseDown event passes MouseButtonEventArgs to its handlers, so a handler reads ChangedButton without a cast.
    // -= with the same handler removes it.
    [Fact]
    public void MouseDownEventHandlerReadsTheTypedArgsAndMinusEqualsRemovesIt()
    {
        UIRoot root = RootWithChild(out UIElement child);
        List<InputMouseButton> buttons = [];
        RoutedEventHandler<MouseButtonEventArgs> handler = (_, args) => buttons.Add(args.ChangedButton);
        child.MouseDown += handler;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, ButtonsFrame(10, 10, previousLeft: false, previousRight: false, currentLeft: false, currentRight: true));
        bridge.Dispatch(root, ButtonsFrame(10, 10, previousLeft: false, previousRight: true, currentLeft: false, currentRight: false));
        child.MouseDown -= handler;
        bridge.Dispatch(root, ButtonsFrame(10, 10, previousLeft: false, previousRight: false, currentLeft: true, currentRight: false));
        bridge.Dispatch(root, ButtonsFrame(10, 10, previousLeft: true, previousRight: false, currentLeft: false, currentRight: false));

        Assert.Equal([InputMouseButton.Right], buttons);
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

    // As in WPF, which raises the button-specific event on each element when the generic event reaches that element, a
    // MouseDown or MouseUp handled on an element stops ordinary button-specific handlers only on its ancestors: the
    // handlers on that element and on the elements below it run (#660).
    [Theory]
    [InlineData(InputMouseButton.Left, true)]
    [InlineData(InputMouseButton.Left, false)]
    [InlineData(InputMouseButton.Right, true)]
    [InlineData(InputMouseButton.Right, false)]
    public void HandledMouseDownOrUpStillRunsButtonHandlersOnTheHandlingElementAndBelowIt(InputMouseButton button, bool isDown)
    {
        UIRoot root = RootWithParentAndChild(out UIElement parent, out UIElement child);
        parent.Handlers.AddHandler(isDown ? InputEvents.MouseDownEvent : InputEvents.MouseUpEvent, (_, args) => args.Handled = true);
        RoutedEvent buttonEvent = (button, isDown) switch
        {
            (InputMouseButton.Left, true) => InputEvents.MouseLeftButtonDownEvent,
            (InputMouseButton.Left, false) => InputEvents.MouseLeftButtonUpEvent,
            (_, true) => InputEvents.MouseRightButtonDownEvent,
            _ => InputEvents.MouseRightButtonUpEvent
        };
        List<string> calls = [];
        child.Handlers.AddHandler(buttonEvent, (_, _) => calls.Add("child"));
        parent.Handlers.AddHandler(buttonEvent, (_, _) => calls.Add("parent"));
        root.Handlers.AddHandler(buttonEvent, (_, _) => calls.Add("root"));
        PointerSnapshot released = PointerSnapshot.Empty.WithPosition(10, 10);
        PointerSnapshot pressed = released.WithButton(button, true);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, new InputFrame(released, pressed, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        bridge.Dispatch(root, new InputFrame(pressed, released, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));

        Assert.Equal(["child", "parent"], calls);
    }

    // The tunnelling preview pairs follow the same rule: a PreviewMouseDown or PreviewMouseUp handled on an element stops
    // ordinary preview button-specific handlers only on the elements below it (#660).
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HandledPreviewMouseDownOrUpStillRunsPreviewButtonHandlersOnTheHandlingElementAndAboveIt(bool isDown)
    {
        UIRoot root = RootWithParentAndChild(out UIElement parent, out UIElement child);
        parent.Handlers.AddHandler(isDown ? InputEvents.PreviewMouseDownEvent : InputEvents.PreviewMouseUpEvent, (_, args) => args.Handled = true);
        RoutedEvent buttonEvent = isDown ? InputEvents.PreviewMouseLeftButtonDownEvent : InputEvents.PreviewMouseLeftButtonUpEvent;
        List<string> calls = [];
        root.Handlers.AddHandler(buttonEvent, (_, _) => calls.Add("root"));
        parent.Handlers.AddHandler(buttonEvent, (_, _) => calls.Add("parent"));
        child.Handlers.AddHandler(buttonEvent, (_, _) => calls.Add("child"));
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, pressed: true));
        bridge.Dispatch(root, PointerFrame(10, 10, 10, 10, previousDown: true));

        Assert.Equal(["root", "parent"], calls);
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
            Content = content,
            // The content has no size of its own; stretched, it fills the Button, so the pointer is over it.
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch
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

    // A MouseDown handled in a focusable element's content keeps the focus off that element only when the element is
    // the button whose press it cancels (#644). A press on the track of a focusable Slider beside its thumb presses the
    // track's decrease RepeatButton, which steps the value down by LargeChange; the Slider then focuses itself and marks
    // the MouseDown handled, as a WPF Slider does with handledEventsToo (#662), and neither undoes the other: the Slider
    // takes the focus and the value goes from 50 to 40.
    [Fact]
    public void PointerPressOnTrackOfFocusableSliderFocusesTheSliderAndStepsByLargeChange()
    {
        Button other = new() { Content = "other", Width = 100, Height = 30 };
        Slider slider = new() { Minimum = 0, Maximum = 100, Value = 50, LargeChange = 10, Width = 200, Height = 30 };
        StackPanel panel = new();
        panel.Children.Add(other);
        panel.Children.Add(slider);
        UIRoot root = new(400, 400);
        root.VisualChildren.Add(new Window { Content = panel, Width = 400, Height = 400 });
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        LayoutRect otherBounds = other.ArrangedBounds;
        float otherX = otherBounds.X + otherBounds.Width / 2;
        float otherY = otherBounds.Y + otherBounds.Height / 2;
        bridge.Dispatch(root, PointerFrame(otherX, otherY, pressed: true));
        bridge.Dispatch(root, PointerFrame(otherX, otherY, otherX, otherY, previousDown: true));
        root.ProcessFrame();
        Assert.True(other.IsKeyboardFocused);
        LayoutRect track = slider.Track.ArrangedBounds;
        float x = track.X + 4;
        float y = track.Y + track.Height / 2;
        UIElement? hit = new HitTestService().HitTest(root, x, y)?.Element;
        Assert.NotNull(hit);
        Assert.NotSame(slider.Track.Thumb, hit);

        bridge.Dispatch(root, PointerFrame(x, y, pressed: true));
        root.ProcessFrame();
        bool sliderFocusedWhileHeld = slider.IsKeyboardFocused;
        bridge.Dispatch(root, PointerFrame(x, y, x, y, previousDown: true));
        root.ProcessFrame();

        Assert.Equal(40, slider.Value);
        Assert.True(sliderFocusedWhileHeld);
        Assert.True(slider.IsKeyboardFocused);
        Assert.False(other.IsKeyboardFocused);
    }

    // The press moves the focus after its events (#644). A press handler that moves the focus itself, without marking
    // the press handled, keeps its choice: the pressed button does not take the focus back.
    [Theory]
    [InlineData("PreviewMouseDown")]
    [InlineData("MouseDown")]
    public void PointerPressHandlerThatMovesFocusKeepsItsChoice(string handlerEvent)
    {
        Button first = new() { Content = "first", Width = 100, Height = 30 };
        Button second = new() { Content = "second", Width = 100, Height = 30 };
        Button third = new() { Content = "third", Width = 100, Height = 30 };
        StackPanel panel = new();
        panel.Children.Add(first);
        panel.Children.Add(second);
        panel.Children.Add(third);
        UIRoot root = new(400, 400);
        root.VisualChildren.Add(new Window { Content = panel, Width = 400, Height = 400 });
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        RoutedEvent routedEvent = handlerEvent == "PreviewMouseDown" ? InputEvents.PreviewMouseDownEvent : InputEvents.MouseDownEvent;
        second.Handlers.AddHandler(routedEvent, (_, _) => bridge.FocusManager.Focus(third, root.InputCache.EnsureCurrent(root)));
        LayoutRect firstBounds = first.ArrangedBounds;
        float firstX = firstBounds.X + firstBounds.Width / 2;
        float firstY = firstBounds.Y + firstBounds.Height / 2;
        bridge.Dispatch(root, PointerFrame(firstX, firstY, pressed: true));
        bridge.Dispatch(root, PointerFrame(firstX, firstY, firstX, firstY, previousDown: true));
        root.ProcessFrame();
        Assert.True(first.IsKeyboardFocused);
        LayoutRect bounds = second.ArrangedBounds;
        float x = bounds.X + bounds.Width / 2;
        float y = bounds.Y + bounds.Height / 2;

        bridge.Dispatch(root, PointerFrame(x, y, pressed: true));
        root.ProcessFrame();
        bool thirdFocusedWhileHeld = third.IsKeyboardFocused;
        bridge.Dispatch(root, PointerFrame(x, y, x, y, previousDown: true));
        root.ProcessFrame();

        Assert.True(thirdFocusedWhileHeld);
        Assert.True(third.IsKeyboardFocused);
        Assert.False(second.IsKeyboardFocused);
        Assert.False(first.IsKeyboardFocused);
    }

    // As in WPF, where RepeatButton.OnMouseLeftButtonDown clicks and starts its timer when the bubbling press reaches
    // it, a MouseDown that the RepeatButton's parent marks handled after the button does not undo the press: the press
    // clicks once, and a press held past Delay clicks again (#645 rule for the press owner).
    [Fact]
    public void MouseDownHandledByParentAfterRepeatButtonStillClicksAndRepeats()
    {
        UIRoot root = new(100, 100);
        UIElement parent = HitTestServiceTests.Arranged(0, 0, 60, 60);
        RepeatButton button = new() { Delay = 100, Interval = 50 };
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 40)));
        parent.VisualChildren.Add(button);
        root.VisualChildren.Add(parent);
        parent.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, args) => args.Handled = true, handledEventsToo: true);
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, pressed: true));
        int clicksAfterPress = clickCount;
        bridge.Dispatch(root, PointerFrame(10, 10, 10, 10, previousDown: true, currentDown: true), TimeSpan.FromMilliseconds(150));

        Assert.Equal(1, clicksAfterPress);
        Assert.Equal(2, clickCount);
        Assert.True(button.IsPressed);
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

    // As in WPF, where ButtonBase clicks on MouseLeftButtonUp and only the left button sets IsPressed, a right click
    // while the left button is held does not cancel the left press (#593).
    [Fact]
    public void RightClickWhileLeftButtonIsHeldDoesNotCancelLeftClickOfButton()
    {
        UIRoot root = new(100, 100);
        ButtonBase button = ArrangedButton(0, 0, 40, 40);
        root.VisualChildren.Add(button);
        int clickCount = 0;
        int executionCount = 0;
        button.Click += (_, _) => clickCount++;
        button.Command = new ActionCommand(_ => executionCount++);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, ButtonsFrame(10, 10, previousLeft: false, previousRight: false, currentLeft: true, currentRight: false));
        bridge.Dispatch(root, ButtonsFrame(10, 10, previousLeft: true, previousRight: false, currentLeft: true, currentRight: true));
        bridge.Dispatch(root, ButtonsFrame(10, 10, previousLeft: true, previousRight: true, currentLeft: true, currentRight: false));

        Assert.True(button.IsPressed);
        Assert.Equal(0, clickCount);

        bridge.Dispatch(root, ButtonsFrame(10, 10, previousLeft: true, previousRight: false, currentLeft: false, currentRight: false));

        Assert.False(button.IsPressed);
        Assert.Equal(1, clickCount);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public void RightClickOnButtonNeitherPressesNorClicksIt()
    {
        UIRoot root = new(100, 100);
        ButtonBase button = ArrangedButton(0, 0, 40, 40);
        root.VisualChildren.Add(button);
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, ButtonsFrame(10, 10, previousLeft: false, previousRight: false, currentLeft: false, currentRight: true));

        Assert.False(button.IsPressed);

        bridge.Dispatch(root, ButtonsFrame(10, 10, previousLeft: false, previousRight: true, currentLeft: false, currentRight: false));

        Assert.False(button.IsPressed);
        Assert.Equal(0, clickCount);
    }

    // As in WPF, where MouseDevice starts the click count at 1 when the pressed button differs from the previously
    // pressed button, a press of another button ends the double-click sequence of the left button.
    [Fact]
    public void PressOfAnotherButtonRestartsTheLeftClickCount()
    {
        UIRoot root = RootWithChild(out UIElement child);
        List<int> leftClickCounts = [];
        child.Handlers.AddHandler(InputEvents.MouseLeftButtonUpEvent, (_, args) => leftClickCounts.Add(((MouseButtonEventArgs)args).ClickCount));
        ElementInputBridge bridge = new();

        void LeftClick()
        {
            bridge.Dispatch(root, ButtonsFrame(10, 10, previousLeft: false, previousRight: false, currentLeft: true, currentRight: false));
            bridge.Dispatch(root, ButtonsFrame(10, 10, previousLeft: true, previousRight: false, currentLeft: false, currentRight: false));
        }

        LeftClick();
        LeftClick();
        bridge.Dispatch(root, ButtonsFrame(10, 10, previousLeft: false, previousRight: false, currentLeft: false, currentRight: true));
        bridge.Dispatch(root, ButtonsFrame(10, 10, previousLeft: false, previousRight: true, currentLeft: false, currentRight: false));
        LeftClick();
        bridge.Dispatch(root, ButtonsFrame(10, 10, previousLeft: false, previousRight: false, currentLeft: true, currentRight: false));
        bridge.Dispatch(root, ButtonsFrame(10, 10, previousLeft: true, previousRight: false, currentLeft: true, currentRight: true));
        bridge.Dispatch(root, ButtonsFrame(10, 10, previousLeft: true, previousRight: true, currentLeft: true, currentRight: false));
        bridge.Dispatch(root, ButtonsFrame(10, 10, previousLeft: true, previousRight: false, currentLeft: false, currentRight: false));
        LeftClick();

        Assert.Equal([1, 2, 1, 1, 1], leftClickCounts);
    }

    // As WPF's MouseDevice, which decides the click count on the press: a press continues the sequence when it is of the
    // same button, on the same target, within the double-click time (500 ms) and within the double-click size (4 px).
    [Fact]
    public void SecondQuickPressCarriesClickCountTwoOnMouseDown()
    {
        UIRoot root = RootWithChild(out UIElement child);
        List<int> downCounts = [];
        List<int> buttonDownCounts = [];
        List<int> upCounts = [];
        int doubleClicks = 0;
        child.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, args) => downCounts.Add(((MouseButtonEventArgs)args).ClickCount));
        child.Handlers.AddHandler(InputEvents.MouseLeftButtonDownEvent, (_, args) => buttonDownCounts.Add(((MouseButtonEventArgs)args).ClickCount));
        child.Handlers.AddHandler(InputEvents.MouseUpEvent, (_, args) => upCounts.Add(((MouseButtonEventArgs)args).ClickCount));
        child.Handlers.AddHandler(InputEvents.MouseDoubleClickEvent, (_, _) => doubleClicks++);
        ElementInputBridge bridge = new();

        LeftClick(bridge, root, 10, 10, TimeSpan.Zero);
        LeftClick(bridge, root, 10, 10, TimeSpan.Zero);

        Assert.Equal([1, 2], downCounts);
        Assert.Equal([1, 2], buttonDownCounts);
        Assert.Equal([1, 2], upCounts);
        Assert.Equal(1, doubleClicks);
    }

    [Fact]
    public void SecondPressAfterMoreThanTheDoubleClickTimeRestartsTheClickCount()
    {
        UIRoot root = RootWithChild(out UIElement child);
        List<int> downCounts = [];
        List<int> upCounts = [];
        int doubleClicks = 0;
        child.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, args) => downCounts.Add(((MouseButtonEventArgs)args).ClickCount));
        child.Handlers.AddHandler(InputEvents.MouseUpEvent, (_, args) => upCounts.Add(((MouseButtonEventArgs)args).ClickCount));
        child.Handlers.AddHandler(InputEvents.MouseDoubleClickEvent, (_, _) => doubleClicks++);
        ElementInputBridge bridge = new();

        LeftClick(bridge, root, 10, 10, TimeSpan.Zero);
        LeftClick(bridge, root, 10, 10, TimeSpan.FromMilliseconds(600));

        Assert.Equal([1, 1], downCounts);
        Assert.Equal([1, 1], upCounts);
        Assert.Equal(0, doubleClicks);
    }

    [Fact]
    public void SecondPressAtTheDoubleClickTimeLimitStillContinuesTheClickCount()
    {
        UIRoot root = RootWithChild(out UIElement child);
        List<int> downCounts = [];
        child.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, args) => downCounts.Add(((MouseButtonEventArgs)args).ClickCount));
        ElementInputBridge bridge = new();

        LeftClick(bridge, root, 10, 10, TimeSpan.Zero);
        LeftClick(bridge, root, 10, 10, TimeSpan.FromMilliseconds(500));

        Assert.Equal([1, 2], downCounts);
    }

    [Fact]
    public void SecondPressMoreThanTheDoubleClickSizeAwayRestartsTheClickCount()
    {
        UIRoot root = RootWithChild(out UIElement child);
        List<int> downCounts = [];
        List<int> upCounts = [];
        child.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, args) => downCounts.Add(((MouseButtonEventArgs)args).ClickCount));
        child.Handlers.AddHandler(InputEvents.MouseUpEvent, (_, args) => upCounts.Add(((MouseButtonEventArgs)args).ClickCount));
        ElementInputBridge bridge = new();

        LeftClick(bridge, root, 10, 10, TimeSpan.Zero);
        LeftClick(bridge, root, 20, 10, TimeSpan.Zero);

        Assert.Equal([1, 1], downCounts);
        Assert.Equal([1, 1], upCounts);
    }

    [Fact]
    public void SecondPressWithinTheDoubleClickSizeOnBothAxesContinuesTheClickCount()
    {
        UIRoot root = RootWithChild(out UIElement child);
        List<int> downCounts = [];
        child.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, args) => downCounts.Add(((MouseButtonEventArgs)args).ClickCount));
        ElementInputBridge bridge = new();

        LeftClick(bridge, root, 10, 10, TimeSpan.Zero);
        LeftClick(bridge, root, 14, 6, TimeSpan.Zero);

        Assert.Equal([1, 2], downCounts);
    }

    [Fact]
    public void ThreeQuickPressesCarryClickCountThreeOnTheThirdPress()
    {
        UIRoot root = RootWithChild(out UIElement child);
        List<int> downCounts = [];
        List<int> upCounts = [];
        child.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, args) => downCounts.Add(((MouseButtonEventArgs)args).ClickCount));
        child.Handlers.AddHandler(InputEvents.MouseUpEvent, (_, args) => upCounts.Add(((MouseButtonEventArgs)args).ClickCount));
        ElementInputBridge bridge = new();

        LeftClick(bridge, root, 10, 10, TimeSpan.Zero);
        LeftClick(bridge, root, 10, 10, TimeSpan.FromMilliseconds(100));
        LeftClick(bridge, root, 10, 10, TimeSpan.FromMilliseconds(100));

        Assert.Equal([1, 2, 3], downCounts);
        Assert.Equal([1, 2, 3], upCounts);
    }

    [Fact]
    public void PointerDragSourceReceivesTheClickCountOfThePress()
    {
        UIRoot root = new(100, 100);
        DragSourceElement source = new();
        source.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 40)));
        root.VisualChildren.Add(source);
        ElementInputBridge bridge = new();

        LeftClick(bridge, root, 10, 10, TimeSpan.Zero);
        LeftClick(bridge, root, 10, 10, TimeSpan.Zero);

        Assert.Equal([1, 2], source.BeginClickCounts);
        Assert.Equal([1, 2], source.CompleteClickCounts);
    }

    private static void LeftClick(ElementInputBridge bridge, UIRoot root, float x, float y, TimeSpan timeBeforePress)
    {
        bridge.Dispatch(root, ButtonsFrame(x, y, previousLeft: false, previousRight: false, currentLeft: true, currentRight: false), timeBeforePress);
        bridge.Dispatch(root, ButtonsFrame(x, y, previousLeft: true, previousRight: false, currentLeft: false, currentRight: false), TimeSpan.Zero);
    }

    private sealed class DragSourceElement : UIElement, IPointerDragSource
    {
        internal List<int> BeginClickCounts { get; } = [];

        internal List<int> CompleteClickCounts { get; } = [];

        bool IPointerDragSource.BeginPointerDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args)
        {
            BeginClickCounts.Add(args.ClickCount);
            return true;
        }

        bool IPointerDragSource.UpdatePointerDrag(MouseEventArgs args) => false;

        bool IPointerDragSource.CompletePointerDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args)
        {
            CompleteClickCounts.Add(args.ClickCount);
            return true;
        }
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

    private static UIRoot RootWithParentAndChild(out UIElement parent, out UIElement child)
    {
        UIRoot root = new(100, 100);
        parent = HitTestServiceTests.Arranged(0, 0, 60, 60);
        child = HitTestServiceTests.Arranged(0, 0, 40, 40);
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);
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

    private static InputFrame ButtonsFrame(
        float x,
        float y,
        bool previousLeft,
        bool previousRight,
        bool currentLeft,
        bool currentRight)
    {
        return new InputFrame(
            PointerSnapshot.Empty.WithPosition(x, y).WithButton(InputMouseButton.Left, previousLeft).WithButton(InputMouseButton.Right, previousRight),
            PointerSnapshot.Empty.WithPosition(x, y).WithButton(InputMouseButton.Left, currentLeft).WithButton(InputMouseButton.Right, currentRight),
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            []);
    }
}
