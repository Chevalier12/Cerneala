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
    private float lastPointerX;
    private float lastPointerY;

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
        keyboardActivationController = new KeyboardActivationController();
        repeatButtonController = new RepeatButtonController();
        this.textInputBridge = textInputBridge ?? new TextInputBridge();
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
        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        HitTestResult? hitTarget = hitTestService.HitTest(root, routeMap, inputFrame.Pointer.X, inputFrame.Pointer.Y);
        HitTestResult? pointerTarget = pointerCaptureManager.OverrideTarget(hitTarget, routeMap, inputFrame.Pointer.X, inputFrame.Pointer.Y);

        DispatchPointer(root, inputFrame, frameTime, routeMap, hitTarget, pointerTarget);
        IReadOnlyList<KeyboardDispatchResult> keyboardResults = focusManager.DispatchKeyboardWithResults(inputFrame, routeMap);
        IReadOnlyList<KeyboardDispatchResult> activationResults = retainedInputBindingProcessor.Process(keyboardResults, inputFrame, commandRouter, routeMap);
        keyboardNavigationController.Process(activationResults, inputFrame, root, focusManager, routeMap);
        keyboardActivationController.Process(activationResults, focusManager, commandRouter, routeMap);
        textInputBridge.Dispatch(inputFrame.TextInputEvents, focusManager, routeMap);
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

        if (moved || !hoverTracker.IsCurrentRouteValid(routeMap))
        {
            hoverTracker.Update(pointerTarget, routeMap, inputFrame.Pointer.X, inputFrame.Pointer.Y);
            if (hasLastPointerPosition)
            {
                RaiseMouseMovePair(routeMap, pointerTarget);
                UpdatePointerDrag(routeMap, pointerTarget);
            }
        }

        if (inputFrame.Pointer.WheelDelta != 0)
        {
            RaiseWheelPair(routeMap, pointerTarget, inputFrame.Pointer.WheelDelta);
        }

        foreach (InputMouseButton button in Enum.GetValues<InputMouseButton>())
        {
            if (button is InputMouseButton.None)
            {
                continue;
            }

            if (inputFrame.Pointer.IsPressed(button))
            {
                clickTracker.Press(ResolveClickTarget(hitTarget?.Element, routeMap));
                pressedStateTracker.Press(pointerTarget?.Element, routeMap);
                if (button == InputMouseButton.Left &&
                    pointerTarget is not null &&
                    ResolveFocusTarget(pointerTarget.Element, routeMap) is UIElement focusTarget)
                {
                    focusManager.Focus(focusTarget, routeMap);
                }

                bool handled = RaiseMousePair(
                    routeMap,
                    pointerTarget,
                    InputEvents.PreviewMouseDownEvent,
                    InputEvents.MouseDownEvent,
                    button,
                    1,
                    out bool previewHandled);
                handled |= RaiseMouseButtonSpecificPair(
                    routeMap,
                    pointerTarget,
                    button,
                    isDown: true,
                    clickCount: 1,
                    out bool buttonPreviewHandled);
                if (!previewHandled && !buttonPreviewHandled)
                {
                    BeginPointerDrag(routeMap, pointerTarget, button);
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
                if (button == InputMouseButton.Left)
                {
                    repeatButtonController.Cancel(pressedStateTracker);
                }

                int clickCount = clickTracker.Release(ResolveClickTarget(hitTarget?.Element, routeMap));
                bool handled = RaiseMousePair(routeMap, pointerTarget, InputEvents.PreviewMouseUpEvent, InputEvents.MouseUpEvent, button, clickCount);
                handled |= RaiseMouseButtonSpecificPair(routeMap, pointerTarget, button, isDown: false, clickCount);
                if (button == InputMouseButton.Left && clickCount == 2)
                {
                    handled |= RaiseDirectMousePairAlongRoute(routeMap, pointerTarget, InputEvents.PreviewMouseDoubleClickEvent, InputEvents.MouseDoubleClickEvent, button, clickCount);
                }
                CompletePointerDrag(routeMap, pointerTarget, button, clickCount);
                if (!handled)
                {
                    ExecuteButtonCommandOnClick(routeMap, pointerTarget, hitTarget, button, clickCount);
                }

                pressedStateTracker.Release();
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

    private static UIElement? FindAncestor<TContract>(
        UIElement element,
        ElementInputRouteMap routeMap)
    {
        foreach (UIElement current in routeMap.GetRouteToRoot(element))
        {
            if (current is TContract)
            {
                return current;
            }
        }

        return null;
    }

    private static UIElement? ResolveClickTarget(
        UIElement? element,
        ElementInputRouteMap routeMap)
    {
        if (element is null)
        {
            return null;
        }

        return FindAncestor<IInputCommandSource>(element, routeMap) ?? element;
    }

    private static UIElement? ResolveFocusTarget(UIElement element, ElementInputRouteMap routeMap)
    {
        foreach (UIElement current in routeMap.GetRouteToRoot(element))
        {
            if (FocusPolicy.CanFocus(current, routeMap))
            {
                return current;
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
        int clickCount)
    {
        return RaiseMousePair(routeMap, target, previewEvent, bubbleEvent, button, clickCount, out _);
    }

    private static bool RaiseMousePair(
        ElementInputRouteMap routeMap,
        HitTestResult? target,
        RoutedEvent previewEvent,
        RoutedEvent bubbleEvent,
        InputMouseButton button,
        int clickCount,
        out bool previewHandled)
    {
        if (target is null)
        {
            previewHandled = false;
            return false;
        }

        MouseButtonEventArgs previewArgs = new(previewEvent, target.ElementId, button, target.X, target.Y, clickCount);
        MouseButtonEventArgs bubbleArgs = new(bubbleEvent, target.ElementId, button, target.X, target.Y, clickCount);
        RoutedEventRouter.RaisePair(
            routeMap.InputTree,
            target.ElementId,
            previewArgs,
            bubbleArgs);
        previewHandled = previewArgs.Handled;
        return previewArgs.Handled || bubbleArgs.Handled;
    }

    private static bool RaiseMouseButtonSpecificPair(ElementInputRouteMap routeMap, HitTestResult? target, InputMouseButton button, bool isDown, int clickCount)
    {
        return RaiseMouseButtonSpecificPair(routeMap, target, button, isDown, clickCount, out _);
    }

    private static bool RaiseMouseButtonSpecificPair(
        ElementInputRouteMap routeMap,
        HitTestResult? target,
        InputMouseButton button,
        bool isDown,
        int clickCount,
        out bool previewHandled)
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
            return false;
        }

        return RaiseDirectMousePairAlongRoute(routeMap, target, events.Preview, events.Bubble, button, clickCount, out previewHandled);
    }

    private static bool RaiseDirectMousePairAlongRoute(
        ElementInputRouteMap routeMap,
        HitTestResult? target,
        RoutedEvent previewEvent,
        RoutedEvent bubbleEvent,
        InputMouseButton button,
        int clickCount)
    {
        return RaiseDirectMousePairAlongRoute(routeMap, target, previewEvent, bubbleEvent, button, clickCount, out _);
    }

    private static bool RaiseDirectMousePairAlongRoute(
        ElementInputRouteMap routeMap,
        HitTestResult? target,
        RoutedEvent previewEvent,
        RoutedEvent bubbleEvent,
        InputMouseButton button,
        int clickCount,
        out bool previewHandled)
    {
        if (target is null)
        {
            previewHandled = false;
            return false;
        }

        IReadOnlyList<UiElementId> route = routeMap.InputTree.GetRouteToRoot(target.ElementId);
        bool handled = false;

        foreach (UiElementId elementId in route.Reverse())
        {
            MouseButtonEventArgs args = new(previewEvent, target.ElementId, button, target.X, target.Y, clickCount) { Handled = handled };
            RoutedEventRouter.Raise(routeMap.InputTree, elementId, args);
            handled |= args.Handled;
        }

        previewHandled = handled;
        foreach (UiElementId elementId in route)
        {
            MouseButtonEventArgs args = new(bubbleEvent, target.ElementId, button, target.X, target.Y, clickCount) { Handled = handled };
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
