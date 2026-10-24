using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.UI.Detective;

public static class InputDiagnostics
{
    public static InputDiagnosticsSnapshot Capture(UIElement? hitTarget, RoutedEvent? routedEvent = null)
    {
        return new InputDiagnosticsSnapshot(
            hitTarget?.GetType().Name,
            routedEvent?.Name,
            routedEvent?.RoutingStrategy);
    }
}

public sealed record InputDiagnosticsSnapshot(
    string? HitTargetType,
    string? RoutedEventName,
    RoutingStrategy? RoutingStrategy)
{
    public override string ToString()
    {
        return $"input target={HitTargetType ?? "none"}, routedEvent={RoutedEventName ?? "none"}, strategy={RoutingStrategy?.ToString() ?? "none"}";
    }
}
