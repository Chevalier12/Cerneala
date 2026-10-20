namespace Cerneala.UI.Input;

public static class RoutedEventRouter
{
    public static void RaisePair(UiInputTree tree, UiElementId targetId, RoutedEventArgs previewArgs, RoutedEventArgs bubbleArgs)
    {
        RaisePair(tree, targetId, previewArgs, bubbleArgs, previewHandledOnArrival: null, bubbleHandledOnArrival: null);
    }

    // Also collects, for each of the two routes, the elements that its args reached already handled.
    internal static void RaisePair(
        UiInputTree tree,
        UiElementId targetId,
        RoutedEventArgs previewArgs,
        RoutedEventArgs bubbleArgs,
        ISet<UiElementId>? previewHandledOnArrival,
        ISet<UiElementId>? bubbleHandledOnArrival)
    {
        ArgumentNullException.ThrowIfNull(previewArgs);
        ArgumentNullException.ThrowIfNull(bubbleArgs);

        Raise(tree, targetId, previewArgs, previewHandledOnArrival);
        bubbleArgs.Handled |= previewArgs.Handled;
        Raise(tree, targetId, bubbleArgs, bubbleHandledOnArrival);
    }

    public static void Raise(UiInputTree tree, UiElementId targetId, RoutedEventArgs args)
    {
        Raise(tree, targetId, args, handledOnArrival: null);
    }

    // Also adds to handledOnArrival each element of the route that args reached already handled, before that element's
    // handlers ran.
    internal static void Raise(
        UiInputTree tree,
        UiElementId targetId,
        RoutedEventArgs args,
        ISet<UiElementId>? handledOnArrival)
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
            if (args.Handled)
            {
                handledOnArrival?.Add(elementId);
            }

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
