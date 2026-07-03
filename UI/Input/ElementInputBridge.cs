using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class ElementInputBridge
{
    private readonly ElementInputRouteBuilder routeBuilder;
    private readonly HitTestService hitTestService;
    private readonly PointerCaptureManager pointerCaptureManager;
    private readonly HoverTracker hoverTracker;
    private readonly PressedStateTracker pressedStateTracker;
    private readonly ClickTracker clickTracker;
    private readonly FocusManager focusManager;
    private readonly TextInputBridge textInputBridge;
    private bool hasLastPointerPosition;
    private float lastPointerX;
    private float lastPointerY;

    public ElementInputBridge(
        ElementInputRouteBuilder? routeBuilder = null,
        HitTestService? hitTestService = null,
        PointerCaptureManager? pointerCaptureManager = null,
        HoverTracker? hoverTracker = null,
        PressedStateTracker? pressedStateTracker = null,
        ClickTracker? clickTracker = null,
        FocusManager? focusManager = null,
        TextInputBridge? textInputBridge = null)
    {
        this.routeBuilder = routeBuilder ?? new ElementInputRouteBuilder();
        this.hitTestService = hitTestService ?? new HitTestService();
        this.pointerCaptureManager = pointerCaptureManager ?? new PointerCaptureManager();
        this.hoverTracker = hoverTracker ?? new HoverTracker();
        this.pressedStateTracker = pressedStateTracker ?? new PressedStateTracker();
        this.clickTracker = clickTracker ?? new ClickTracker();
        this.focusManager = focusManager ?? new FocusManager();
        this.textInputBridge = textInputBridge ?? new TextInputBridge();
    }

    public PointerCaptureManager PointerCaptureManager => pointerCaptureManager;

    public HoverTracker HoverTracker => hoverTracker;

    public PressedStateTracker PressedStateTracker => pressedStateTracker;

    public FocusManager FocusManager => focusManager;

    public void Dispatch(UIRoot root, InputFrame inputFrame)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(inputFrame);

        ElementInputRouteMap routeMap = routeBuilder.Build(root);
        HitTestResult? hitTarget = hitTestService.HitTest(root, routeMap, inputFrame.Pointer.X, inputFrame.Pointer.Y);
        HitTestResult? pointerTarget = pointerCaptureManager.OverrideTarget(hitTarget, routeMap, inputFrame.Pointer.X, inputFrame.Pointer.Y);

        DispatchPointer(inputFrame, routeMap, hitTarget, pointerTarget);
        focusManager.DispatchKeyboard(inputFrame, routeMap);
        textInputBridge.Dispatch(inputFrame.TextInputEvents, focusManager, routeMap);
    }

    private void DispatchPointer(InputFrame inputFrame, ElementInputRouteMap routeMap, HitTestResult? hitTarget, HitTestResult? pointerTarget)
    {
        bool moved = !hasLastPointerPosition ||
            inputFrame.Pointer.X != lastPointerX ||
            inputFrame.Pointer.Y != lastPointerY;

        if (moved)
        {
            hoverTracker.Update(hitTarget, routeMap, inputFrame.Pointer.X, inputFrame.Pointer.Y);
            if (hasLastPointerPosition)
            {
                RaiseMouseMovePair(routeMap, pointerTarget);
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
                clickTracker.Press(pointerTarget?.Element);
                pressedStateTracker.Press(pointerTarget?.Element);
                if (button == InputMouseButton.Left && pointerTarget is not null)
                {
                    focusManager.Focus(pointerTarget.Element, routeMap);
                }

                RaiseMousePair(routeMap, pointerTarget, InputEvents.PreviewMouseDownEvent, InputEvents.MouseDownEvent, button, 1);
            }

            if (inputFrame.Pointer.IsReleased(button))
            {
                int clickCount = clickTracker.Release(pointerTarget?.Element);
                RaiseMousePair(routeMap, pointerTarget, InputEvents.PreviewMouseUpEvent, InputEvents.MouseUpEvent, button, clickCount);
                pressedStateTracker.Release();
            }
        }

        hasLastPointerPosition = true;
        lastPointerX = inputFrame.Pointer.X;
        lastPointerY = inputFrame.Pointer.Y;
    }

    private static void RaiseMousePair(
        ElementInputRouteMap routeMap,
        HitTestResult? target,
        RoutedEvent previewEvent,
        RoutedEvent bubbleEvent,
        InputMouseButton button,
        int clickCount)
    {
        if (target is null)
        {
            return;
        }

        int x = (int)MathF.Round(target.X);
        int y = (int)MathF.Round(target.Y);
        RoutedEventRouter.RaisePair(
            routeMap.InputTree,
            target.ElementId,
            new MouseButtonEventArgs(previewEvent, target.ElementId, button, x, y, clickCount),
            new MouseButtonEventArgs(bubbleEvent, target.ElementId, button, x, y, clickCount));
    }

    private static void RaiseMouseMovePair(ElementInputRouteMap routeMap, HitTestResult? target)
    {
        if (target is null)
        {
            return;
        }

        int x = (int)MathF.Round(target.X);
        int y = (int)MathF.Round(target.Y);
        RoutedEventRouter.RaisePair(
            routeMap.InputTree,
            target.ElementId,
            new MouseEventArgs(InputEvents.PreviewMouseMoveEvent, target.ElementId, x, y),
            new MouseEventArgs(InputEvents.MouseMoveEvent, target.ElementId, x, y));
    }

    private static void RaiseWheelPair(ElementInputRouteMap routeMap, HitTestResult? target, int delta)
    {
        if (target is null)
        {
            return;
        }

        int x = (int)MathF.Round(target.X);
        int y = (int)MathF.Round(target.Y);
        RoutedEventRouter.RaisePair(
            routeMap.InputTree,
            target.ElementId,
            new MouseWheelEventArgs(InputEvents.PreviewMouseWheelEvent, target.ElementId, x, y, delta),
            new MouseWheelEventArgs(InputEvents.MouseWheelEvent, target.ElementId, x, y, delta));
    }
}
