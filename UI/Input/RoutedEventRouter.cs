namespace Cerneala.UI.Input;

public static class RoutedEventRouter
{
    public static void Raise(UiInputTree tree, UiElementId targetId, RoutedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(args);

        IReadOnlyList<UiElementId> routeToRoot = tree.GetRouteToRoot(targetId);
        IEnumerable<UiElementId> route = args.RoutedEvent.RoutingStrategy switch
        {
            RoutingStrategy.Direct => [targetId],
            RoutingStrategy.Bubble => routeToRoot,
            RoutingStrategy.Tunnel => routeToRoot.Reverse(),
            _ => throw new InvalidOperationException($"Unsupported routing strategy '{args.RoutedEvent.RoutingStrategy}'.")
        };

        foreach (UiElementId elementId in route)
        {
            if (args.Handled)
            {
                return;
            }

            args.Source = elementId;
            foreach (RoutedEventHandler handler in tree.GetHandlers(elementId, args.RoutedEvent))
            {
                handler(elementId, args);
                if (args.Handled)
                {
                    return;
                }
            }
        }
    }
}
