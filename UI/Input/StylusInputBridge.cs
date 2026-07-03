using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class StylusInputBridge
{
    private readonly ElementInputRouteBuilder routeBuilder;
    private readonly HitTestService hitTestService;

    public StylusInputBridge(ElementInputRouteBuilder? routeBuilder = null, HitTestService? hitTestService = null)
    {
        this.routeBuilder = routeBuilder ?? new ElementInputRouteBuilder();
        this.hitTestService = hitTestService ?? new HitTestService();
    }

    public void Dispatch(UIRoot root, StylusInputFrame frame)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(frame);

        ElementInputRouteMap routeMap = routeBuilder.Build(root);
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

public sealed record StylusInputFrame(IReadOnlyList<StylusInputPoint> Points)
{
    public StylusInputFrame(params StylusInputPoint[] points)
        : this((IReadOnlyList<StylusInputPoint>)points)
    {
    }
}

public sealed record StylusInputPoint(
    int Id,
    float X,
    float Y,
    StylusInputAction Action,
    float Pressure = 0.5f,
    bool IsInRange = true,
    string? Button = null);

public enum StylusInputAction
{
    Down,
    Move,
    Up,
    InRange,
    OutOfRange,
    ButtonDown,
    ButtonUp
}

public sealed class StylusEventArgs : RoutedEventArgs
{
    public StylusEventArgs(RoutedEvent routedEvent, object originalSource, StylusInputPoint point)
        : base(routedEvent, originalSource)
    {
        Point = point ?? throw new ArgumentNullException(nameof(point));
    }

    public StylusInputPoint Point { get; }

    public int StylusId => Point.Id;

    public float X => Point.X;

    public float Y => Point.Y;

    public float Pressure => Point.Pressure;

    public bool IsInRange => Point.IsInRange;

    public string? Button => Point.Button;
}
