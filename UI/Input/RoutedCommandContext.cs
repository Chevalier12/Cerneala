using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class RoutedCommandContext
{
    public RoutedCommandContext(ICommand command, UIElement? target, ElementInputRouteMap routeMap, object? parameter = null)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Target = target;
        RouteMap = routeMap ?? throw new ArgumentNullException(nameof(routeMap));
        Parameter = parameter;
    }

    public ICommand Command { get; }

    public UIElement? Target { get; }

    public ElementInputRouteMap RouteMap { get; }

    public object? Parameter { get; }
}
