using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class PointerCaptureManager
{
    public UIElement? CapturedElement { get; private set; }

    public bool HasCapture => CapturedElement is not null;

    public void Capture(UIElement element, ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(routeMap);

        if (ReferenceEquals(CapturedElement, element))
        {
            return;
        }

        UIElement? oldCapture = CapturedElement;
        CapturedElement = element;
        RaiseCaptureChanged(routeMap, oldCapture, InputEvents.LostMouseCaptureEvent);
        RaiseCaptureChanged(routeMap, CapturedElement, InputEvents.GotMouseCaptureEvent);
    }

    public void Release(ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(routeMap);

        UIElement? oldCapture = CapturedElement;
        if (oldCapture is null)
        {
            return;
        }

        CapturedElement = null;
        RaiseCaptureChanged(routeMap, oldCapture, InputEvents.LostMouseCaptureEvent);
    }

    public HitTestResult? OverrideTarget(HitTestResult? hitTarget, ElementInputRouteMap routeMap, float x, float y)
    {
        ArgumentNullException.ThrowIfNull(routeMap);

        if (CapturedElement is null)
        {
            return hitTarget;
        }

        return routeMap.TryGetId(CapturedElement, out UiElementId capturedId)
            ? new HitTestResult(CapturedElement, capturedId, x, y)
            : ReleaseUnroutableCapture(hitTarget);
    }

    private HitTestResult? ReleaseUnroutableCapture(HitTestResult? hitTarget)
    {
        CapturedElement = null;
        return hitTarget;
    }

    private static void RaiseCaptureChanged(ElementInputRouteMap routeMap, UIElement? element, RoutedEvent routedEvent)
    {
        if (element is null || !routeMap.TryGetId(element, out UiElementId id))
        {
            return;
        }

        RoutedEventRouter.Raise(routeMap.InputTree, id, new MouseEventArgs(routedEvent, id, 0, 0));
    }
}
