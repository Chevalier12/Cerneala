namespace Cerneala.UI.Input;

public static class RoutedEventRouter
{
    public static void RaisePair(UiInputTree tree, UiElementId targetId, RoutedEventArgs previewArgs, RoutedEventArgs bubbleArgs)
    {
        RaisePair(tree, targetId, previewArgs, bubbleArgs, observedId: null, out _);
    }

    // Also reports whether bubbleArgs was already handled when its route reached observedId.
    internal static void RaisePair(
        UiInputTree tree,
        UiElementId targetId,
        RoutedEventArgs previewArgs,
        RoutedEventArgs bubbleArgs,
        UiElementId? observedId,
        out bool bubbleHandledOnArrival)
    {
        ArgumentNullException.ThrowIfNull(previewArgs);
        ArgumentNullException.ThrowIfNull(bubbleArgs);

        Raise(tree, targetId, previewArgs);
        bubbleArgs.Handled |= previewArgs.Handled;
        Raise(tree, targetId, bubbleArgs, observedId, out bubbleHandledOnArrival);
    }

    public static void Raise(UiInputTree tree, UiElementId targetId, RoutedEventArgs args)
    {
        Raise(tree, targetId, args, observedId: null, out _);
    }

    // Also reports whether args was already handled when its route reached observedId, before that element's handlers
    // ran. handledOnArrival is false when the route does not pass observedId.
    internal static void Raise(
        UiInputTree tree,
        UiElementId targetId,
        RoutedEventArgs args,
        UiElementId? observedId,
        out bool handledOnArrival)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(args);
        handledOnArrival = false;

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
            if (elementId == observedId)
            {
                handledOnArrival = args.Handled;
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
