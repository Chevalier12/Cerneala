namespace Cerneala.UI.Input;

public static class RoutedEventRouter
{
    public static void RaisePair(UiInputTree tree, UiElementId targetId, RoutedEventArgs previewArgs, RoutedEventArgs bubbleArgs)
    {
        ArgumentNullException.ThrowIfNull(previewArgs);
        ArgumentNullException.ThrowIfNull(bubbleArgs);

        Raise(tree, targetId, previewArgs);
        bubbleArgs.Handled |= previewArgs.Handled;
        Raise(tree, targetId, bubbleArgs);
    }

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
            args.Source = elementId;
            foreach (InputRoutedEventHandlerRegistration registration in tree.GetHandlerRegistrations(elementId, args.RoutedEvent))
            {
                if (!args.Handled || registration.HandledEventsToo)
                {
                    registration.Handler(elementId, args);
                }
            }
        }
    }
}
