using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.UI.Diagnostics;

public static class RoutedEventTrace
{
    public static RoutedEventTraceSnapshot Trace(UIElement target, RoutedEvent routedEvent, ElementChildRole role = ElementChildRole.Visual)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(routedEvent);

        UIElement[] routeToRoot = [target, .. ElementTreeWalker.Ancestors(target, role).Where(ancestor => ancestor.IsEnabled)];
        IReadOnlyList<UIElement> route = routedEvent.RoutingStrategy switch
        {
            RoutingStrategy.Direct => [target],
            RoutingStrategy.Bubble => routeToRoot,
            RoutingStrategy.Tunnel => routeToRoot.Reverse().ToArray(),
            _ => throw new InvalidOperationException($"Unsupported routing strategy '{routedEvent.RoutingStrategy}'.")
        };

        return new RoutedEventTraceSnapshot(
            routedEvent.Name,
            routedEvent.RoutingStrategy,
            target.ElementId?.ToString(),
            route.Select(element => new RoutedEventTraceStep(element.ElementId?.ToString(), element.GetType().Name)).ToArray());
    }
}

public sealed record RoutedEventTraceSnapshot(
    string EventName,
    RoutingStrategy RoutingStrategy,
    string? TargetId,
    IReadOnlyList<RoutedEventTraceStep> Steps)
{
    public override string ToString()
    {
        return $"{EventName} {RoutingStrategy}: " + string.Join(" -> ", Steps.Select(step => step.ToString()));
    }
}

public sealed record RoutedEventTraceStep(string? ElementId, string ElementType)
{
    public override string ToString()
    {
        return $"{ElementType}#{ElementId ?? "unattached"}";
    }
}
