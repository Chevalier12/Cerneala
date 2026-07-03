using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class DragDropController
{
    private readonly ElementInputRouteBuilder routeBuilder;
    private readonly HitTestService hitTestService;
    private DragSession? session;

    public DragDropController(ElementInputRouteBuilder? routeBuilder = null, HitTestService? hitTestService = null)
    {
        this.routeBuilder = routeBuilder ?? new ElementInputRouteBuilder();
        this.hitTestService = hitTestService ?? new HitTestService();
    }

    public bool IsDragging => session is not null;

    public void Begin(UIElement source, DataTransfer data)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(data);
        session = new DragSession(source, data);
    }

    public void Move(UIRoot root, float x, float y)
    {
        if (session is null)
        {
            return;
        }

        ElementInputRouteMap routeMap = routeBuilder.Build(root);
        HitTestResult? target = hitTestService.HitTest(root, routeMap, x, y);
        if (session.CurrentTarget is not null && !ReferenceEquals(session.CurrentTarget.Element, target?.Element))
        {
            Raise(routeMap, session.CurrentTarget, InputEvents.PreviewDragLeaveEvent, InputEvents.DragLeaveEvent, x, y);
        }

        if (target is not null && !ReferenceEquals(session.CurrentTarget?.Element, target.Element))
        {
            Raise(routeMap, target, InputEvents.PreviewDragEnterEvent, InputEvents.DragEnterEvent, x, y);
        }

        if (target is not null)
        {
            Raise(routeMap, target, InputEvents.PreviewDragOverEvent, InputEvents.DragOverEvent, x, y);
        }

        session.CurrentTarget = target;
    }

    public void Drop(UIRoot root, float x, float y)
    {
        if (session is null)
        {
            return;
        }

        ElementInputRouteMap routeMap = routeBuilder.Build(root);
        HitTestResult? target = hitTestService.HitTest(root, routeMap, x, y);
        if (target is not null)
        {
            Raise(routeMap, target, InputEvents.PreviewDropEvent, InputEvents.DropEvent, x, y);
        }

        session = null;
    }

    private void Raise(ElementInputRouteMap routeMap, HitTestResult target, RoutedEvent preview, RoutedEvent bubble, float x, float y)
    {
        RoutedEventRouter.RaisePair(
            routeMap.InputTree,
            target.ElementId,
            new DragEventArgs(preview, target.ElementId, session!.Data, x, y),
            new DragEventArgs(bubble, target.ElementId, session.Data, x, y));
    }

    private sealed class DragSession(UIElement source, DataTransfer data)
    {
        public UIElement Source { get; } = source;

        public DataTransfer Data { get; } = data;

        public HitTestResult? CurrentTarget { get; set; }
    }
}

public sealed class DragEventArgs : RoutedEventArgs
{
    public DragEventArgs(RoutedEvent routedEvent, object originalSource, DataTransfer data, float x, float y)
        : base(routedEvent, originalSource)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        X = x;
        Y = y;
    }

    public DataTransfer Data { get; }

    public float X { get; }

    public float Y { get; }
}
