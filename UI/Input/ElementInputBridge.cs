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
                clickTracker.Press(button, ResolveClickTarget(hitTarget?.Element, routeMap));
                pressOriginalSources[(int)button] = pointerTarget?.ElementId;
                UIElement? focusTarget = null;
                if (button == InputMouseButton.Left)
                {
                    pressedStateTracker.Press(pointerTarget?.Element, routeMap);
                    focusTarget = pointerTarget is null ? null : ResolveFocusTarget(pointerTarget.Element, routeMap);
                }

                UIElement? pressOwner = button == InputMouseButton.Left ? pressedStateTracker.PressedElement as UIElement : null;
                UiElementId? pressOwnerId = pressOwner is not null && routeMap.TryGetId(pressOwner, out UiElementId ownerId)
                    ? ownerId
                    : null;
                UIElement? focusBeforePress = focusManager.FocusedElement;
                bool handled = RaiseMousePair(
                    routeMap,
                    pointerTarget,
                    InputEvents.PreviewMouseDownEvent,
                    InputEvents.MouseDownEvent,
                    button,
                    1,
                    out bool previewHandled,
                    pressOwnerId,
                    out bool handledBeforePressOwner);
                handled |= RaiseMouseButtonSpecificPair(
                    routeMap,
                    pointerTarget,
                    button,
                    isDown: true,
                    clickCount: 1,
                    previewHandled,
                    handled,
                    out bool buttonPreviewHandled,
                    pressOwnerId,
                    out bool buttonHandledBeforePressOwner);

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
                    !((handledBeforePressOwner || buttonHandledBeforePressOwner) && ReferenceEquals(focusTarget, pressOwner)))
                {
                    focusManager.Focus(focusTarget, root.InputCache.EnsureCurrent(root));
                }

                if (!previewHandled && !buttonPreviewHandled)
                {
                    BeginPointerDrag(routeMap, pointerTarget, button);
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
                if ((handledBeforePressOwner || buttonHandledBeforePressOwner) &&
                    pressOwner is not null &&
                    ReferenceEquals(pressedStateTracker.PressedElement, pressOwner))
                {
                    pressedStateTracker.Cancel();
                    if (ReferenceEquals(clickTracker.PressedTarget(button), pressOwner))
                    {
                        clickTracker.CancelPress(button);
                    }
                }

                if (button == InputMouseButton.Left)
                {
                    if (!handled)
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
                }

                int clickCount = clickTracker.Release(
                    button,
                    ResolveReleaseClickTarget(hitTarget?.Element, clickTracker.PressedTarget(button), routeMap));
                object? releasedPressSource = pressOriginalSources[(int)button];
                pressOriginalSources[(int)button] = null;
                bool handled = RaiseMousePair(routeMap, pointerTarget, InputEvents.PreviewMouseUpEvent, InputEvents.MouseUpEvent, button, clickCount, out bool previewHandled, null, out _, releasedPressSource);
                handled |= RaiseMouseButtonSpecificPair(routeMap, pointerTarget, button, isDown: false, clickCount, previewHandled, handled, out _, null, out _, releasedPressSource);
                if (button == InputMouseButton.Left && clickCount == 2)
                {
                    handled |= RaiseDirectMousePairAlongRoute(routeMap, pointerTarget, InputEvents.PreviewMouseDoubleClickEvent, InputEvents.MouseDoubleClickEvent, button, clickCount, previewInitiallyHandled: false, bubbleInitiallyHandled: false, out _, null, out _, releasedPressSource);
                }
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
        HitTestResult? pointerTarget = pointerCaptureManager.OverrideTarget(hitTarget, routeMap, inputFrame.Pointer.X, inputFrame.Pointer.Y);
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

    private void BeginPointerDrag(ElementInputRouteMap routeMap, HitTestResult? target, InputMouseButton button)
    {
        if (button != InputMouseButton.Left || target is null)
        {
            return;
        }

        if (FindAncestor<IPointerDragSource>(target.Element, routeMap) is not IPointerDragSource dragSource)
        {
            return;
        }

        dragSource.BeginPointerDrag(pointerCaptureManager, routeMap, new MouseButtonEventArgs(InputEvents.MouseDownEvent, target.ElementId, button, target.X, target.Y, 1));
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

        dragSource.UpdatePointerDrag(new MouseEventArgs(InputEvents.MouseMoveEvent, target.ElementId, target.X, target.Y));
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

        dragSource.CompletePointerDrag(pointerCaptureManager, routeMap, new MouseButtonEventArgs(InputEvents.MouseUpEvent, target.ElementId, button, target.X, target.Y, clickCount));
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
        out bool previewHandled,
        UiElementId? observedBubbleId,
        out bool bubbleHandledBeforeObserved,
        object? pressOriginalSource = null)
    {
        if (target is null)
        {
            previewHandled = false;
            bubbleHandledBeforeObserved = false;
            return false;
        }

        MouseButtonEventArgs previewArgs = new(previewEvent, target.ElementId, button, target.X, target.Y, clickCount)
        {
            RouteMap = routeMap,
            PressOriginalSource = pressOriginalSource
        };
        MouseButtonEventArgs bubbleArgs = new(bubbleEvent, target.ElementId, button, target.X, target.Y, clickCount)
        {
            RouteMap = routeMap,
            PressOriginalSource = pressOriginalSource
        };
        RoutedEventRouter.RaisePair(
            routeMap.InputTree,
            target.ElementId,
            previewArgs,
            bubbleArgs,
            observedBubbleId,
            out bubbleHandledBeforeObserved);
        previewHandled = previewArgs.Handled;
        return previewArgs.Handled || bubbleArgs.Handled;
    }

    // As in WPF, the button-specific events report the generic MouseDown or MouseUp, so each phase starts with the
    // Handled value of the matching generic phase: ordinary handlers do not run after the generic event was handled.
    private static bool RaiseMouseButtonSpecificPair(
        ElementInputRouteMap routeMap,
        HitTestResult? target,
        InputMouseButton button,
        bool isDown,
        int clickCount,
        bool genericPreviewHandled,
        bool genericHandled,
        out bool previewHandled,
        UiElementId? observedBubbleId,
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

        return RaiseDirectMousePairAlongRoute(routeMap, target, events.Preview, events.Bubble, button, clickCount, genericPreviewHandled, genericHandled, out previewHandled, observedBubbleId, out bubbleHandledBeforeObserved, pressOriginalSource);
    }

    private static bool RaiseDirectMousePairAlongRoute(
        ElementInputRouteMap routeMap,
        HitTestResult? target,
        RoutedEvent previewEvent,
        RoutedEvent bubbleEvent,
        InputMouseButton button,
        int clickCount,
        bool previewInitiallyHandled,
        bool bubbleInitiallyHandled,
        out bool previewHandled,
        UiElementId? observedBubbleId,
        out bool bubbleHandledBeforeObserved,
        object? pressOriginalSource = null)
    {
        if (target is null)
        {
            previewHandled = false;
            bubbleHandledBeforeObserved = false;
            return false;
        }

        IReadOnlyList<UiElementId> route = routeMap.InputTree.GetRouteToRoot(target.ElementId);
        bool handled = previewInitiallyHandled;

        foreach (UiElementId elementId in route.Reverse())
        {
            MouseButtonEventArgs args = new(previewEvent, target.ElementId, button, target.X, target.Y, clickCount)
            {
                Handled = handled,
                PressOriginalSource = pressOriginalSource
            };
            RoutedEventRouter.Raise(routeMap.InputTree, elementId, args);
            handled |= args.Handled;
        }

        previewHandled = handled;
        handled |= bubbleInitiallyHandled;
        // Only a handler on an element before observed counts: a bubble that starts handled, because the generic event
        // was handled further up the route, was not handled before observed, as WPF raises the button-specific event on
        // each element when the generic event reaches it.
        bool bubbleStartsHandled = handled;
        bubbleHandledBeforeObserved = false;
        foreach (UiElementId elementId in route)
        {
            if (elementId == observedBubbleId)
            {
                bubbleHandledBeforeObserved = handled && !bubbleStartsHandled;
            }

            MouseButtonEventArgs args = new(bubbleEvent, target.ElementId, button, target.X, target.Y, clickCount)
            {
                Handled = handled,
                PressOriginalSource = pressOriginalSource
            };
            RoutedEventRouter.Raise(routeMap.InputTree, elementId, args);
            handled |= args.Handled;
        }

        return handled;
    }

    private static void RaiseMouseMovePair(ElementInputRouteMap routeMap, HitTestResult? target)
    {
        if (target is null)
        {
            return;
        }

        RoutedEventRouter.RaisePair(
            routeMap.InputTree,
            target.ElementId,
            new MouseEventArgs(InputEvents.PreviewMouseMoveEvent, target.ElementId, target.X, target.Y),
            new MouseEventArgs(InputEvents.MouseMoveEvent, target.ElementId, target.X, target.Y));
    }

    private static void RaiseWheelPair(ElementInputRouteMap routeMap, HitTestResult? target, int delta)
    {
        if (target is null)
        {
            return;
        }

        RoutedEventRouter.RaisePair(
            routeMap.InputTree,
            target.ElementId,
            new MouseWheelEventArgs(InputEvents.PreviewMouseWheelEvent, target.ElementId, target.X, target.Y, delta),
            new MouseWheelEventArgs(InputEvents.MouseWheelEvent, target.ElementId, target.X, target.Y, delta));
    }
}
