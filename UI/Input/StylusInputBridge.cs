using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class StylusInputBridge
{
    private readonly HitTestService hitTestService;

    public StylusInputBridge(HitTestService? hitTestService = null)
    {
        this.hitTestService = hitTestService ?? new HitTestService();
    }

    public void Dispatch(UIRoot root, StylusInputFrame frame)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(frame);

        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        foreach (StylusInputPoint point in frame.Points)
        {
            HitTestResult? target = hitTestService.HitTest(root, routeMap, point.X, point.Y);
            if (target is null)
            {
                continue;
            }

            DispatchPoint(routeMap, target, point);
        }
    }

    private static void DispatchPoint(ElementInputRouteMap routeMap, HitTestResult target, StylusInputPoint point)
    {
        (RoutedEvent preview, RoutedEvent bubble) = point.Action switch
        {
            StylusInputAction.Down => (InputEvents.PreviewStylusDownEvent, InputEvents.StylusDownEvent),
            StylusInputAction.Move => (InputEvents.PreviewStylusMoveEvent, InputEvents.StylusMoveEvent),
            StylusInputAction.Up => (InputEvents.PreviewStylusUpEvent, InputEvents.StylusUpEvent),
            StylusInputAction.InRange => (InputEvents.PreviewStylusInRangeEvent, InputEvents.StylusInRangeEvent),
            StylusInputAction.OutOfRange => (InputEvents.PreviewStylusOutOfRangeEvent, InputEvents.StylusOutOfRangeEvent),
            StylusInputAction.ButtonDown => (InputEvents.PreviewStylusButtonDownEvent, InputEvents.StylusButtonDownEvent),
            StylusInputAction.ButtonUp => (InputEvents.PreviewStylusButtonUpEvent, InputEvents.StylusButtonUpEvent),
            _ => throw new InvalidOperationException($"Unsupported stylus action '{point.Action}'.")
        };

        RoutedEventRouter.RaisePair(
            routeMap.InputTree,
            target.ElementId,
            new StylusEventArgs(preview, target.ElementId, point),
            new StylusEventArgs(bubble, target.ElementId, point));
    }
}
