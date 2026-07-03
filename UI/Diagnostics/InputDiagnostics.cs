using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.UI.Diagnostics;

public static class InputDiagnostics
{
    public static InputDiagnosticsSnapshot Capture(UIElement? hitTarget, RoutedEvent? routedEvent = null)
    {
        return new InputDiagnosticsSnapshot(
            hitTarget?.ElementId?.ToString(),
            hitTarget?.GetType().Name,
            routedEvent?.Name,
            routedEvent?.RoutingStrategy);
    }
}

public sealed record InputDiagnosticsSnapshot(
    string? HitTargetId,
    string? HitTargetType,
    string? RoutedEventName,
    RoutingStrategy? RoutingStrategy)
{
    public override string ToString()
    {
        return $"input target={HitTargetType ?? "none"}#{HitTargetId ?? "none"}, routedEvent={RoutedEventName ?? "none"}, strategy={RoutingStrategy?.ToString() ?? "none"}";
    }
}
