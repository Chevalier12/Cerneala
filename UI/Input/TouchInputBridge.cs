using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class TouchInputBridge
{
    private readonly HitTestService hitTestService;
    private readonly Dictionary<int, UIElement> capturedElementsByTouchId = [];

    public TouchInputBridge(HitTestService? hitTestService = null)
    {
        this.hitTestService = hitTestService ?? new HitTestService();
    }

    public void Capture(int touchId, UIElement element, ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(routeMap);
        if (capturedElementsByTouchId.TryGetValue(touchId, out UIElement? oldCapture) &&
            ReferenceEquals(oldCapture, element))
        {
            return;
        }

        capturedElementsByTouchId[touchId] = element;
        RaiseCaptureChanged(routeMap, oldCapture, InputEvents.LostTouchCaptureEvent, touchId);
        RaiseCaptureChanged(routeMap, element, InputEvents.GotTouchCaptureEvent, touchId);
    }

    private static void RaiseCaptureChanged(ElementInputRouteMap routeMap, UIElement? element, RoutedEvent routedEvent, int touchId)
    {
        if (element is null || !routeMap.TryGetId(element, out UiElementId id))
        {
            return;
        }

        RoutedEventRouter.Raise(routeMap.InputTree, id, new TouchEventArgs(routedEvent, id, touchId, 0, 0, TouchInputAction.Move));
    }

    public void Release(int touchId, ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(routeMap);
        if (!capturedElementsByTouchId.Remove(touchId, out UIElement? element))
        {
            return;
        }

        RaiseCaptureChanged(routeMap, element, InputEvents.LostTouchCaptureEvent, touchId);
    }

    public void Dispatch(UIRoot root, TouchInputFrame frame)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(frame);

        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        foreach (TouchInputPoint point in frame.Points)
        {
            DispatchPoint(root, routeMap, point);
        }
    }

    private void DispatchPoint(UIRoot root, ElementInputRouteMap routeMap, TouchInputPoint point)
    {
        HitTestResult? hit = hitTestService.HitTest(root, routeMap, point.X, point.Y);
        HitTestResult? target = ResolveTarget(routeMap, point, hit);
        if (target is null)
        {
            return;
        }

        (RoutedEvent preview, RoutedEvent bubble) = point.Action switch
        {
            TouchInputAction.Down => (InputEvents.PreviewTouchDownEvent, InputEvents.TouchDownEvent),
            TouchInputAction.Move => (InputEvents.PreviewTouchMoveEvent, InputEvents.TouchMoveEvent),
            TouchInputAction.Up => (InputEvents.PreviewTouchUpEvent, InputEvents.TouchUpEvent),
            _ => throw new InvalidOperationException($"Unsupported touch action '{point.Action}'.")
        };

        RoutedEventRouter.RaisePair(
            routeMap.InputTree,
            target.ElementId,
            new TouchEventArgs(preview, target.ElementId, point.Id, point.X, point.Y, point.Action),
            new TouchEventArgs(bubble, target.ElementId, point.Id, point.X, point.Y, point.Action));
    }

    private HitTestResult? ResolveTarget(ElementInputRouteMap routeMap, TouchInputPoint point, HitTestResult? hit)
    {
        if (!capturedElementsByTouchId.TryGetValue(point.Id, out UIElement? captured))
        {
            return hit;
        }

        if (routeMap.TryGetId(captured, out UiElementId id))
        {
            return new HitTestResult(captured, id, point.X, point.Y);
        }

        capturedElementsByTouchId.Remove(point.Id);
        return hit;
    }
}

public sealed record TouchInputFrame(IReadOnlyList<TouchInputPoint> Points)
{
    public TouchInputFrame(params TouchInputPoint[] points)
        : this((IReadOnlyList<TouchInputPoint>)points)
    {
    }
}

public sealed record TouchInputPoint(int Id, float X, float Y, TouchInputAction Action);

public enum TouchInputAction
{
    Down,
    Move,
    Up
}

public sealed class TouchEventArgs : RoutedEventArgs
{
    public TouchEventArgs(RoutedEvent routedEvent, object originalSource, int touchId, float x, float y, TouchInputAction action)
        : base(routedEvent, originalSource)
    {
        TouchId = touchId;
        X = x;
        Y = y;
        Action = action;
    }

    public int TouchId { get; }

    public float X { get; }

    public float Y { get; }

    public TouchInputAction Action { get; }
}
