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
        RaiseLostCapture(routeMap, oldCapture);
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
        RaiseLostCapture(routeMap, oldCapture);
    }

    public HitTestResult? OverrideTarget(HitTestResult? hitTarget, ElementInputRouteMap routeMap, float x, float y)
    {
        ArgumentNullException.ThrowIfNull(routeMap);

        if (CapturedElement is null)
        {
            return hitTarget;
        }

        if (!routeMap.Contains(CapturedElement))
        {
            ReleaseUnroutableCapture(routeMap);
        }

        // A LostMouseCapture handler of the released element can capture another element, which then takes the input.
        return CapturedElement is not null && routeMap.Contains(CapturedElement)
            ? new HitTestResult(CapturedElement, x, y)
            : hitTarget;
    }

    // The captured element was detached, hidden, collapsed or disabled, itself or through an ancestor, so it can no longer
    // take input: as WPF re-evaluates the capture then, the capture ends with LostMouseCapture, and a dragging Thumb or a
    // TextBox selecting with the mouse ends that drag (#952).
    private void ReleaseUnroutableCapture(ElementInputRouteMap routeMap)
    {
        UIElement? oldCapture = CapturedElement;
        CapturedElement = null;
        RaiseLostCapture(routeMap, oldCapture);
    }

    // The old captured element always gets LostMouseCapture. An element outside the route map has no input route, so its
    // own handlers get the event, as UIElement.RaiseEvent raises it for such an element.
    private static void RaiseLostCapture(ElementInputRouteMap routeMap, UIElement? element)
    {
        if (element is null)
        {
            return;
        }

        if (routeMap.Contains(element))
        {
            RaiseCaptureChanged(routeMap, element, InputEvents.LostMouseCaptureEvent);
            return;
        }

        element.RaiseEvent(new MouseEventArgs(InputEvents.LostMouseCaptureEvent, element, 0, 0));
    }

    private static void RaiseCaptureChanged(ElementInputRouteMap routeMap, UIElement? element, RoutedEvent routedEvent)
    {
        if (element is null || !routeMap.Contains(element))
        {
            return;
        }

        RoutedEventRouter.Raise(routeMap.InputTree, element, new MouseEventArgs(routedEvent, element, 0, 0));
    }
}
