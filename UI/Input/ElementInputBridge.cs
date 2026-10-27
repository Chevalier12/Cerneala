using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class ElementInputBridge
{
    private readonly HitTestService hitTestService;
    private readonly PointerCaptureManager pointerCaptureManager;
    private readonly HoverTracker hoverTracker;
    private readonly PressedStateTracker pressedStateTracker;
    private readonly ClickTracker clickTracker;
    private readonly CommandRouter commandRouter;
    private readonly FocusManager focusManager;
    private readonly RetainedInputBindingProcessor retainedInputBindingProcessor;
    private readonly KeyboardNavigationController keyboardNavigationController;
    private readonly KeyboardActivationController keyboardActivationController;
    private readonly RepeatButtonController repeatButtonController;
    private readonly TextInputBridge textInputBridge;
    private bool hasLastPointerPosition;
    private readonly object?[] pressOriginalSources = new object?[(int)InputMouseButton.XButton2 + 1];
    private float lastPointerX;
    private float lastPointerY;
    private bool isDispatching;
    private bool pointerModeCancelPending;
    private TimeSpan inputClock;
    private bool wasInputHeld;
    private bool hasActiveInputHold;

    public ElementInputBridge(
        HitTestService? hitTestService = null,
        PointerCaptureManager? pointerCaptureManager = null,
        HoverTracker? hoverTracker = null,
        PressedStateTracker? pressedStateTracker = null,
        ClickTracker? clickTracker = null,
        CommandRouter? commandRouter = null,
        FocusManager? focusManager = null,
        TextInputBridge? textInputBridge = null)
    {
        this.hitTestService = hitTestService ?? new HitTestService();
        this.pointerCaptureManager = pointerCaptureManager ?? new PointerCaptureManager();
        this.hoverTracker = hoverTracker ?? new HoverTracker();
        this.pressedStateTracker = pressedStateTracker ?? new PressedStateTracker();
        this.clickTracker = clickTracker ?? new ClickTracker();
        this.commandRouter = commandRouter ?? new CommandRouter();
        this.focusManager = focusManager ?? new FocusManager();
        retainedInputBindingProcessor = new RetainedInputBindingProcessor();
        keyboardNavigationController = new KeyboardNavigationController();
        keyboardActivationController = new KeyboardActivationController(this.pressedStateTracker);
        repeatButtonController = new RepeatButtonController();
        this.textInputBridge = textInputBridge ?? new TextInputBridge();
        this.focusManager.AddFocusChangeListener(keyboardActivationController);
    }

    public PointerCaptureManager PointerCaptureManager => pointerCaptureManager;

    public HoverTracker HoverTracker => hoverTracker;

    public PressedStateTracker PressedStateTracker => pressedStateTracker;

    public FocusManager FocusManager => focusManager;

    public CommandRouter CommandRouter => commandRouter;

    internal bool HasActivePointerRepeat => repeatButtonController.IsActive;

    // Input was held in the last dispatch and the route tree has a handler for PreviewInputHeld or InputHeld, so the next
    // frame must run to raise it again even if nothing else asks for one.
    internal bool HasActiveInputHold => hasActiveInputHold;

    public void Dispatch(UIRoot root, InputFrame inputFrame)
    {
        Dispatch(root, inputFrame, TimeSpan.Zero);
    }

    public void Dispatch(UIRoot root, InputFrame inputFrame, TimeSpan frameTime)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(inputFrame);
        if (frameTime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(frameTime));
        }

        // The clock that dates presses for the double-click time advances by the frame time of each dispatched frame.
        inputClock += frameTime;
        root.ActiveFocusManager = focusManager;
        (ElementInputRouteMap routeMap, HitTestResult? hitTarget, HitTestResult? pointerTarget) = ResolvePointerTargets(root, inputFrame);

        isDispatching = true;
        try
        {
            DispatchPointer(root, inputFrame, frameTime, routeMap, hitTarget, pointerTarget);
            IReadOnlyList<KeyboardDispatchResult> keyboardResults = focusManager.DispatchKeyboardWithResults(inputFrame, routeMap);
            IReadOnlyList<KeyboardDispatchResult> activationResults = retainedInputBindingProcessor.Process(keyboardResults, inputFrame, commandRouter, routeMap);
            keyboardNavigationController.Process(activationResults, inputFrame, root, focusManager, routeMap);
            keyboardActivationController.Process(activationResults, focusManager, commandRouter, routeMap);
            textInputBridge.Dispatch(inputFrame.TextInputEvents, focusManager, routeMap);
            DispatchInputHeld(root, inputFrame, frameTime);
        }
        finally
        {
            isDispatching = false;
        }

        if (pointerModeCancelPending)
        {
            pointerModeCancelPending = false;
            CancelPointerModeCore(root.InputCache.EnsureCurrent(root));
        }
    }

    // The time of a pump that rendered no frame, so dispatched no input. Without it the double-click time would count only
    // the time of rendered frames: a click, 2 s with nothing to render, and a click at the same point would count 1, 2.
    internal void AdvanceInputClock(TimeSpan elapsedTime)
    {
        if (elapsedTime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedTime));
        }

        inputClock += elapsedTime;
    }

    // The WM_CANCELMODE that Win32 sends to a window when EnableWindow disables it: the pointer capture is released, so
    // the captured element gets LostMouseCapture and ends its drag, and the pressed, click and repeat state of the
    // interrupted press is dropped. A cancel requested by an input handler, for example a PreviewMouseDown handler that
    // shows a modal dialog, runs when the dispatch ends, after the press has begun its drag and taken the capture.
    internal void CancelPointerMode(UIRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        if (isDispatching)
        {
            pointerModeCancelPending = true;
            return;
        }

        CancelPointerModeCore(root.InputCache.EnsureCurrent(root));
    }

    private void CancelPointerModeCore(ElementInputRouteMap routeMap)
    {
        pointerCaptureManager.Release(routeMap);
        repeatButtonController.Cancel(pressedStateTracker);
        pressedStateTracker.Cancel();
        clickTracker.Cancel();
    }

    // One InputHeld pair per update in which a key or a mouse button is down, after the rest of the input of the frame, so
    // KeyDown and MouseDown of a press frame come first. It goes to the focused element. When nothing can take keyboard
    // input it goes to the first visual child of the root that the route map contains, which is the hosted Window, so a
    // Window handler works with nothing focused, and to the root itself when it has no such child. The first update of a
    // hold has no time since a previous one, so its Elapsed is zero.
    private void DispatchInputHeld(UIRoot root, InputFrame inputFrame, TimeSpan frameTime)
    {
        KeyboardSnapshot keyboard = inputFrame.Keyboard.Current;
        PointerSnapshot pointer = inputFrame.Pointer.Current;
        if (!keyboard.HasAnyDown && !pointer.HasAnyDown)
        {
            wasInputHeld = false;
            hasActiveInputHold = false;
            return;
        }

        TimeSpan elapsed = wasInputHeld ? frameTime : TimeSpan.Zero;
        wasInputHeld = true;

        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        if (HasInputHeldHandlers(routeMap))
        {
            UIElement? target = null;
            ElementInputRouteMap targetMap = routeMap;
            if (focusManager.TryGetKeyboardTarget(routeMap, out UIElement? focused, out ElementInputRouteMap focusedMap))
            {
                target = focused;
                targetMap = focusedMap;
            }
            else
            {
                foreach (UIElement child in root.VisualChildren)
                {
                    if (routeMap.Contains(child))
                    {
                        target = child;
                        break;
                    }
                }

                if (target is null && routeMap.Contains(root))
                {
                    target = root;
                }
            }

            if (target is not null)
            {
                RoutedEventRouter.RaisePair(
                    targetMap.InputTree,
                    target,
                    new InputStateEventArgs(InputEvents.PreviewInputHeldEvent, target, keyboard, pointer, elapsed),
                    new InputStateEventArgs(InputEvents.InputHeldEvent, target, keyboard, pointer, elapsed));
            }
        }

        // Handlers can add or remove handlers, so the answer comes from the route tree as the raise left it.
        hasActiveInputHold = HasInputHeldHandlers(root.InputCache.EnsureCurrent(root));
    }

    private static bool HasInputHeldHandlers(ElementInputRouteMap routeMap)
    {
        return routeMap.InputTree.HasHandlers(InputEvents.PreviewInputHeldEvent) ||
            routeMap.InputTree.HasHandlers(InputEvents.InputHeldEvent);
    }

    private void DispatchPointer(
        UIRoot root,
        InputFrame inputFrame,
        TimeSpan frameTime,
        ElementInputRouteMap routeMap,
        HitTestResult? hitTarget,
        HitTestResult? pointerTarget)
    {
        bool hadCapture = pointerCaptureManager.HasCapture;
        bool moved = !hasLastPointerPosition ||
            inputFrame.Pointer.X != lastPointerX ||
            inputFrame.Pointer.Y != lastPointerY;

        // Handlers of an earlier pointer event of this frame can remove or move elements, so each later event is raised
        // on the element under the pointer at that moment, as WPF raises each mouse message on the element under the
        // mouse when it processes that message.
        if (moved || !hoverTracker.IsCurrentRouteValid(routeMap))
        {
            if (hoverTracker.Update(pointerTarget, routeMap, inputFrame.Pointer.X, inputFrame.Pointer.Y))
            {
                (routeMap, hitTarget, pointerTarget) = ResolvePointerTargets(root, inputFrame);
            }

            if (hasLastPointerPosition)
            {
                RaiseMouseMovePair(routeMap, pointerTarget);
                UpdatePointerDrag(routeMap, pointerTarget);
            }
        }

        // One MouseWheel pair per platform wheel event, as WPF raises one per WM_MOUSEWHEEL.
        foreach (int wheelDelta in inputFrame.Pointer.WheelDeltas)
        {
            (routeMap, hitTarget, pointerTarget) = ResolvePointerTargets(root, inputFrame);
            RaiseWheelPair(routeMap, pointerTarget, wheelDelta);
        }

        KeyModifiers modifiers = GetModifiers(inputFrame.Keyboard);
        foreach (InputMouseButton button in Enum.GetValues<InputMouseButton>())
        {
            if (button is InputMouseButton.None)
            {
                continue;
            }

            if (inputFrame.Pointer.IsPressed(button))
            {
                (routeMap, hitTarget, pointerTarget) = ResolvePointerTargets(root, inputFrame);
                // Each button keeps its own press, and only the left button presses a button visually, as a WPF
                // ButtonBase sets IsPressed on MouseLeftButtonDown only: a right click while the left button is held
                // leaves the left press, its IsPressed and its click intact (#593).
                int pressClickCount = clickTracker.Press(
                    button,
                    ResolveClickTarget(hitTarget?.Element, routeMap),
                    inputFrame.Pointer.X,
                    inputFrame.Pointer.Y,
                    inputClock,
                    inputFrame.PressClickSequence);
                pressOriginalSources[(int)button] = pointerTarget?.Element;
                UIElement? focusTarget = null;
                if (button == InputMouseButton.Left)
                {
                    pressedStateTracker.Press(pointerTarget?.Element, routeMap);
                    focusTarget = pointerTarget is null ? null : ResolveFocusTarget(pointerTarget.Element, routeMap);
                }

                UIElement? pressOwner = button == InputMouseButton.Left ? pressedStateTracker.PressedElement as UIElement : null;
                UIElement? routedPressOwner = pressOwner is not null && routeMap.Contains(pressOwner)
                    ? pressOwner
                    : null;
                UIElement? focusBeforePress = focusManager.FocusedElement;
                HashSet<UIElement> previewHandledOnArrival = new(ReferenceEqualityComparer.Instance);
                HashSet<UIElement> handledOnArrival = new(ReferenceEqualityComparer.Instance);
                RaiseMousePair(
                    routeMap,
                    pointerTarget,
                    InputEvents.PreviewMouseDownEvent,
                    InputEvents.MouseDownEvent,
                    button,
                    pressClickCount,
                    modifiers,
                    out bool previewHandled,
                    previewHandledOnArrival,
                    handledOnArrival);
                // The button-specific bubble reaches each element with the Handled value of MouseDown there, so a press
                // handled before the press owner is a MouseDown or a MouseLeftButtonDown handled below it.
                RaiseMouseButtonSpecificPair(
                    routeMap,
                    pointerTarget,
                    button,
                    isDown: true,
                    clickCount: pressClickCount,
                    modifiers,
                    previewHandledOnArrival,
                    handledOnArrival,
                    out bool buttonPreviewHandled,
                    routedPressOwner,
                    out bool handledBeforePressOwner);
                // As WPF's Control, which raises the double click from its MouseLeftButtonDown and MouseRightButtonDown
                // class handlers when the press counts 2, the double click belongs to the second press: a release outside
                // the element after it does not cancel it, and the release raises no double click (#957). The middle and
                // extra buttons raise none, as WPF registers no such handler for them (#868).
                if (button is InputMouseButton.Left or InputMouseButton.Right && pressClickCount == 2)
                {
                    RaiseDirectMousePairAlongRoute(routeMap, pointerTarget, InputEvents.PreviewMouseDoubleClickEvent, InputEvents.MouseDoubleClickEvent, button, pressClickCount, modifiers, genericPreviewHandledOnArrival: null, genericHandledOnArrival: null, out _, null, out _);
                }

                // As in WPF, where a control focuses itself when MouseLeftButtonDown reaches its class handler, the press
                // moves the focus after its events and before the drag begins. A handled PreviewMouseDown or
                // PreviewMouseLeftButtonDown keeps the focus where it was, and so does a MouseDown or MouseLeftButtonDown
                // that an element in a button's content handled, which also keeps the press from that button (#644). A
                // press handler that moved the focus itself, such as a menu that light-dismisses and restores the focus to
                // its root item, keeps its choice.
                if (focusTarget is not null &&
                    ReferenceEquals(focusManager.FocusedElement, focusBeforePress) &&
                    !previewHandled &&
                    !buttonPreviewHandled &&
                    !(handledBeforePressOwner && ReferenceEquals(focusTarget, pressOwner)))
                {
                    focusManager.Focus(focusTarget, root.InputCache.EnsureCurrent(root));
                }

                if (!previewHandled && !buttonPreviewHandled)
                {
                    BeginPointerDrag(routeMap, pointerTarget, button, pressClickCount);
                }
                else if (button == InputMouseButton.Left)
                {
                    // As in WPF, a handled preview keeps the press from ButtonBase.OnMouseLeftButtonDown: the button is
                    // not pressed, and the release completes no click, so it raises no Click and runs no Command (#620).
                    pressedStateTracker.Cancel();
                    clickTracker.CancelPress(button);
                }

                // As in WPF, where ButtonBase.OnMouseLeftButtonDown presses the button when the bubbling press reaches
                // it, a MouseDown or MouseLeftButtonDown that an element in the button's content handled keeps the press
                // from the button. Handlers on the button and its ancestors run after the press and do not undo it (#645).
                if (handledBeforePressOwner &&
                    pressOwner is not null &&
                    ReferenceEquals(pressedStateTracker.PressedElement, pressOwner))
                {
                    pressedStateTracker.Cancel();
                    if (ReferenceEquals(clickTracker.PressedTarget(button), pressOwner))
                    {
                        clickTracker.CancelPress(button);
                    }
                }

                // As WPF RepeatButton.OnMouseLeftButtonDown, which clicks and starts the repeat timer when the bubbling
                // press reaches the button: the same presses that keep the press from a button keep it from a
                // RepeatButton, and handlers on the button and its ancestors that run after the press do not undo it.
                if (button == InputMouseButton.Left)
                {
                    if (!previewHandled && !buttonPreviewHandled && !handledBeforePressOwner)
                    {
                        repeatButtonController.Begin(
                            root,
                            routeMap,
                            hitTarget?.Element,
                            pointerTarget?.Element,
                            commandRouter,
                            pressedStateTracker);
                    }
                    else
                    {
                        repeatButtonController.Clear();
                    }
                }
            }

            if (inputFrame.Pointer.IsReleased(button))
            {
                (routeMap, hitTarget, pointerTarget) = ResolvePointerTargets(root, inputFrame);
                if (button == InputMouseButton.Left)
                {
                    repeatButtonController.Cancel(pressedStateTracker);

                    // As WPF ButtonBase.OnMouseLeftButtonUp, where shouldClick is !IsSpaceKeyDown && IsPressed: the
                    // release of a press on the element that Space holds pressed completes no click, so it raises no
                    // Click and runs no Command, and the element stays pressed until Space is released (#661).
                    if (keyboardActivationController.IsSpacePressed(clickTracker.PressedTarget(button)))
                    {
                        clickTracker.CancelPress(button);
                    }
                }

                int clickCount = clickTracker.Release(
                    button,
                    ResolveReleaseClickTarget(hitTarget?.Element, clickTracker.PressedTarget(button), routeMap));
                object? releasedPressSource = pressOriginalSources[(int)button];
                pressOriginalSources[(int)button] = null;
                HashSet<UIElement> previewHandledOnArrival = new(ReferenceEqualityComparer.Instance);
                HashSet<UIElement> handledOnArrival = new(ReferenceEqualityComparer.Instance);
                bool handled = RaiseMousePair(routeMap, pointerTarget, InputEvents.PreviewMouseUpEvent, InputEvents.MouseUpEvent, button, clickCount, modifiers, out _, previewHandledOnArrival, handledOnArrival, releasedPressSource);
                handled |= RaiseMouseButtonSpecificPair(routeMap, pointerTarget, button, isDown: false, clickCount, modifiers, previewHandledOnArrival, handledOnArrival, out _, null, out _, releasedPressSource);
                CompletePointerDrag(routeMap, pointerTarget, button, clickCount);
                if (!handled)
                {
                    ExecuteButtonCommandOnClick(routeMap, pointerTarget, hitTarget, button, clickCount);
                }

                if (button == InputMouseButton.Left)
                {
                    pressedStateTracker.Release();
                }
            }
        }

        repeatButtonController.Update(
            root,
            routeMap,
            hitTarget?.Element,
            inputFrame.Pointer.IsDown(InputMouseButton.Left),
            frameTime,
            commandRouter,
            pressedStateTracker);

        if (hadCapture && !pointerCaptureManager.HasCapture)
        {
            hoverTracker.Update(hitTarget, routeMap, inputFrame.Pointer.X, inputFrame.Pointer.Y);
        }

        hasLastPointerPosition = true;
        lastPointerX = inputFrame.Pointer.X;
        lastPointerY = inputFrame.Pointer.Y;
    }

    private (ElementInputRouteMap RouteMap, HitTestResult? HitTarget, HitTestResult? PointerTarget) ResolvePointerTargets(
        UIRoot root,
        InputFrame inputFrame)
    {
        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        HitTestResult? hitTarget = hitTestService.HitTest(root, routeMap, inputFrame.Pointer.X, inputFrame.Pointer.Y);
        UIElement? capturedBefore = pointerCaptureManager.CapturedElement;
        HitTestResult? pointerTarget = pointerCaptureManager.OverrideTarget(hitTarget, routeMap, inputFrame.Pointer.X, inputFrame.Pointer.Y);
        if (capturedBefore is not null && !ReferenceEquals(capturedBefore, pointerCaptureManager.CapturedElement))
        {
            // OverrideTarget released a capture that can no longer take input, and the LostMouseCapture handlers it ran,
            // such as a Thumb's DragCompleted handlers, can change the tree: the targets come from the tree they left.
            routeMap = root.InputCache.EnsureCurrent(root);
            hitTarget = hitTestService.HitTest(root, routeMap, inputFrame.Pointer.X, inputFrame.Pointer.Y);
            pointerTarget = pointerCaptureManager.OverrideTarget(hitTarget, routeMap, inputFrame.Pointer.X, inputFrame.Pointer.Y);
        }

        return (routeMap, hitTarget, pointerTarget);
    }

    private void ExecuteButtonCommandOnClick(
        ElementInputRouteMap routeMap,
        HitTestResult? routedTarget,
        HitTestResult? clickTarget,
        InputMouseButton button,
        int clickCount)
    {
        if (button != InputMouseButton.Left ||
            clickCount <= 0 ||
            routedTarget is null ||
            clickTarget is null)
        {
            return;
        }

        UIElement? commandElement = FindAncestor<IInputCommandSource>(clickTarget.Element, routeMap);
        if (commandElement is not IInputCommandSource commandSource ||
            !commandElement.ActivatesOnPointerRelease ||
            (!ReferenceEquals(routedTarget.Element, clickTarget.Element) &&
            !ReferenceEquals(routedTarget.Element, commandElement)))
        {
            return;
        }

        commandSource.ExecuteCommand(commandRouter, routeMap);
    }

    private void BeginPointerDrag(ElementInputRouteMap routeMap, HitTestResult? target, InputMouseButton button, int clickCount)
    {
        if (button != InputMouseButton.Left || target is null)
        {
            return;
        }

        if (FindAncestor<IPointerDragSource>(target.Element, routeMap) is not IPointerDragSource dragSource)
        {
            return;
        }

        dragSource.BeginPointerDrag(pointerCaptureManager, routeMap, new MouseButtonEventArgs(InputEvents.MouseDownEvent, target.Element, button, target.X, target.Y, clickCount));
    }

    private static void UpdatePointerDrag(
        ElementInputRouteMap routeMap,
        HitTestResult? target)
    {
        if (target is null)
        {
            return;
        }

        if (FindAncestor<IPointerDragSource>(target.Element, routeMap) is not IPointerDragSource dragSource)
        {
            return;
        }

        dragSource.UpdatePointerDrag(new MouseEventArgs(InputEvents.MouseMoveEvent, target.Element, target.X, target.Y));
    }

    private void CompletePointerDrag(ElementInputRouteMap routeMap, HitTestResult? target, InputMouseButton button, int clickCount)
    {
        if (button != InputMouseButton.Left || target is null)
        {
            return;
        }

        if (FindAncestor<IPointerDragSource>(target.Element, routeMap) is not IPointerDragSource dragSource)
        {
            return;
        }

        dragSource.CompletePointerDrag(pointerCaptureManager, routeMap, new MouseButtonEventArgs(InputEvents.MouseUpEvent, target.Element, button, target.X, target.Y, clickCount));
    }

    // Click, drag and focus owners are visual ancestors: content projected by an Overlay does not make the overlay's
    // owner the target of a click or of the focus.
    private static UIElement? FindAncestor<TContract>(
        UIElement element,
        ElementInputRouteMap routeMap)
    {
        foreach (UIElement current in routeMap.GetVisualRouteToRoot(element))
        {
            if (current is TContract)
            {
                return current;
            }
        }

        return null;
    }

    // A press belongs to the nearest command source or element that owns its clicks, such as a non-editable
    // ComboBox field, as in WPF the nearest button handles MouseLeftButtonDown. An enclosing command source then
    // does not get the click.
    private static UIElement? ResolveClickTarget(
        UIElement? element,
        ElementInputRouteMap routeMap)
    {
        if (element is null)
        {
            return null;
        }

        foreach (UIElement current in routeMap.GetVisualRouteToRoot(element))
        {
            if (current is IInputCommandSource || current.OwnsPointerClick)
            {
                return current;
            }
        }

        return element;
    }

    // An element that owns its clicks without being a command source completes only a click whose press it took. A
    // release on it after a press on an enclosing command source belongs to that command source, as a WPF Button
    // that took the press clicks for a release over its content.
    private static UIElement? ResolveReleaseClickTarget(
        UIElement? element,
        UIElement? pressedTarget,
        ElementInputRouteMap routeMap)
    {
        if (element is null)
        {
            return null;
        }

        foreach (UIElement current in routeMap.GetVisualRouteToRoot(element))
        {
            if (current is IInputCommandSource ||
                (current.OwnsPointerClick && ReferenceEquals(current, pressedTarget)))
            {
                return current;
            }
        }

        return element;
    }

    // A press focuses the nearest focusable element. An element that owns its clicks takes the press for itself, as a
    // WPF Thumb marks MouseLeftButtonDown handled before an enclosing ButtonBase focuses itself, so when it cannot take
    // focus, the focus stays where it was (#640).
    private static UIElement? ResolveFocusTarget(UIElement element, ElementInputRouteMap routeMap)
    {
        foreach (UIElement current in routeMap.GetVisualRouteToRoot(element))
        {
            if (FocusPolicy.CanFocus(current, routeMap))
            {
                return current;
            }

            if (current.OwnsPointerClick)
            {
                return null;
            }
        }

        return null;
    }

    private static bool RaiseMousePair(
        ElementInputRouteMap routeMap,
        HitTestResult? target,
        RoutedEvent previewEvent,
        RoutedEvent bubbleEvent,
        InputMouseButton button,
        int clickCount,
        KeyModifiers modifiers,
        out bool previewHandled,
        ISet<UIElement> previewHandledOnArrival,
        ISet<UIElement> bubbleHandledOnArrival,
        object? pressOriginalSource = null)
    {
        if (target is null)
        {
            previewHandled = false;
            return false;
        }

        MouseButtonEventArgs previewArgs = new(previewEvent, target.Element, button, target.X, target.Y, clickCount)
        {
            RouteMap = routeMap,
            PressOriginalSource = pressOriginalSource,
            IsControlDown = modifiers.HasFlag(KeyModifiers.Control),
            IsShiftDown = modifiers.HasFlag(KeyModifiers.Shift),
            IsAltDown = modifiers.HasFlag(KeyModifiers.Alt)
        };
        MouseButtonEventArgs bubbleArgs = new(bubbleEvent, target.Element, button, target.X, target.Y, clickCount)
        {
            RouteMap = routeMap,
            PressOriginalSource = pressOriginalSource,
            IsControlDown = modifiers.HasFlag(KeyModifiers.Control),
            IsShiftDown = modifiers.HasFlag(KeyModifiers.Shift),
            IsAltDown = modifiers.HasFlag(KeyModifiers.Alt)
        };
        RoutedEventRouter.RaisePair(
            routeMap.InputTree,
            target.Element,
            previewArgs,
            bubbleArgs,
            previewHandledOnArrival,
            bubbleHandledOnArrival);
        previewHandled = previewArgs.Handled;
        return previewArgs.Handled || bubbleArgs.Handled;
    }

    // As in WPF, which raises the button-specific event on each element when the generic MouseDown or MouseUp reaches
    // that element, each element sees the Handled value that the matching generic phase had when it reached that
    // element: a generic event handled on an element stops ordinary button-specific handlers only on the elements after
    // it on the route, and the handlers on that element and before it run.
    private static bool RaiseMouseButtonSpecificPair(
        ElementInputRouteMap routeMap,
        HitTestResult? target,
        InputMouseButton button,
        bool isDown,
        int clickCount,
        KeyModifiers modifiers,
        IReadOnlySet<UIElement> genericPreviewHandledOnArrival,
        IReadOnlySet<UIElement> genericHandledOnArrival,
        out bool previewHandled,
        UIElement? observedBubble,
        out bool bubbleHandledBeforeObserved,
        object? pressOriginalSource = null)
    {
        (RoutedEvent Preview, RoutedEvent Bubble)? pair = (button, isDown) switch
        {
            (InputMouseButton.Left, true) => (InputEvents.PreviewMouseLeftButtonDownEvent, InputEvents.MouseLeftButtonDownEvent),
            (InputMouseButton.Left, false) => (InputEvents.PreviewMouseLeftButtonUpEvent, InputEvents.MouseLeftButtonUpEvent),
            (InputMouseButton.Right, true) => (InputEvents.PreviewMouseRightButtonDownEvent, InputEvents.MouseRightButtonDownEvent),
            (InputMouseButton.Right, false) => (InputEvents.PreviewMouseRightButtonUpEvent, InputEvents.MouseRightButtonUpEvent),
            _ => null
        };

        if (pair is not { } events)
        {
            previewHandled = false;
            bubbleHandledBeforeObserved = false;
            return false;
        }

        return RaiseDirectMousePairAlongRoute(routeMap, target, events.Preview, events.Bubble, button, clickCount, modifiers, genericPreviewHandledOnArrival, genericHandledOnArrival, out previewHandled, observedBubble, out bubbleHandledBeforeObserved, pressOriginalSource);
    }

    private static bool RaiseDirectMousePairAlongRoute(
        ElementInputRouteMap routeMap,
        HitTestResult? target,
        RoutedEvent previewEvent,
        RoutedEvent bubbleEvent,
        InputMouseButton button,
        int clickCount,
        KeyModifiers modifiers,
        IReadOnlySet<UIElement>? genericPreviewHandledOnArrival,
        IReadOnlySet<UIElement>? genericHandledOnArrival,
        out bool previewHandled,
        UIElement? observedBubble,
        out bool bubbleHandledBeforeObserved,
        object? pressOriginalSource = null)
    {
        if (target is null)
        {
            previewHandled = false;
            bubbleHandledBeforeObserved = false;
            return false;
        }

        IReadOnlyList<UIElement> route = routeMap.InputTree.GetRouteToRoot(target.Element);
        bool handled = false;

        foreach (UIElement element in route.Reverse())
        {
            MouseButtonEventArgs args = new(previewEvent, target.Element, button, target.X, target.Y, clickCount)
            {
                Handled = handled || genericPreviewHandledOnArrival?.Contains(element) == true,
                PressOriginalSource = pressOriginalSource,
                IsControlDown = modifiers.HasFlag(KeyModifiers.Control),
                IsShiftDown = modifiers.HasFlag(KeyModifiers.Shift),
                IsAltDown = modifiers.HasFlag(KeyModifiers.Alt)
            };
            RoutedEventRouter.Raise(routeMap.InputTree, element, args);
            handled |= args.Handled;
        }

        previewHandled = handled;
        bubbleHandledBeforeObserved = false;
        foreach (UIElement element in route)
        {
            bool handledOnArrival = handled || genericHandledOnArrival?.Contains(element) == true;
            if (ReferenceEquals(element, observedBubble))
            {
                bubbleHandledBeforeObserved = handledOnArrival;
            }

            MouseButtonEventArgs args = new(bubbleEvent, target.Element, button, target.X, target.Y, clickCount)
            {
                Handled = handledOnArrival,
                PressOriginalSource = pressOriginalSource,
                IsControlDown = modifiers.HasFlag(KeyModifiers.Control),
                IsShiftDown = modifiers.HasFlag(KeyModifiers.Shift),
                IsAltDown = modifiers.HasFlag(KeyModifiers.Alt)
            };
            RoutedEventRouter.Raise(routeMap.InputTree, element, args);
            handled |= args.Handled;
        }

        return handled;
    }

    // The modifier keys that the frame's keyboard holds down, the left or the right key, as FocusManager gives them to
    // the key events of the same frame.
    private static KeyModifiers GetModifiers(InputFrame.KeyboardFrame keyboard)
    {
        KeyModifiers modifiers = KeyModifiers.None;
        if (keyboard.IsDown(InputKey.LeftCtrl) || keyboard.IsDown(InputKey.RightCtrl))
        {
            modifiers |= KeyModifiers.Control;
        }

        if (keyboard.IsDown(InputKey.LeftShift) || keyboard.IsDown(InputKey.RightShift))
        {
            modifiers |= KeyModifiers.Shift;
        }

        if (keyboard.IsDown(InputKey.LeftAlt) || keyboard.IsDown(InputKey.RightAlt))
        {
            modifiers |= KeyModifiers.Alt;
        }

        return modifiers;
    }

    private static void RaiseMouseMovePair(ElementInputRouteMap routeMap, HitTestResult? target)
    {
        if (target is null)
        {
            return;
        }

        RoutedEventRouter.RaisePair(
            routeMap.InputTree,
            target.Element,
            new MouseEventArgs(InputEvents.PreviewMouseMoveEvent, target.Element, target.X, target.Y),
            new MouseEventArgs(InputEvents.MouseMoveEvent, target.Element, target.X, target.Y));
    }

    private static void RaiseWheelPair(ElementInputRouteMap routeMap, HitTestResult? target, int delta)
    {
        if (target is null)
        {
            return;
        }

        RoutedEventRouter.RaisePair(
            routeMap.InputTree,
            target.Element,
            new MouseWheelEventArgs(InputEvents.PreviewMouseWheelEvent, target.Element, target.X, target.Y, delta),
            new MouseWheelEventArgs(InputEvents.MouseWheelEvent, target.Element, target.X, target.Y, delta));
    }
}
