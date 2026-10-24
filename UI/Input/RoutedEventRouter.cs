using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public static class RoutedEventRouter
{
    public static void RaisePair(UiInputTree tree, UIElement target, RoutedEventArgs previewArgs, RoutedEventArgs bubbleArgs)
    {
        RaisePair(tree, target, previewArgs, bubbleArgs, previewHandledOnArrival: null, bubbleHandledOnArrival: null);
    }

    // Also collects, for each of the two routes, the elements that its args reached already handled.
    internal static void RaisePair(
        UiInputTree tree,
        UIElement target,
        RoutedEventArgs previewArgs,
        RoutedEventArgs bubbleArgs,
        ISet<UIElement>? previewHandledOnArrival,
        ISet<UIElement>? bubbleHandledOnArrival)
    {
        ArgumentNullException.ThrowIfNull(previewArgs);
        ArgumentNullException.ThrowIfNull(bubbleArgs);

        Raise(tree, target, previewArgs, previewHandledOnArrival);
        bubbleArgs.Handled |= previewArgs.Handled;
        Raise(tree, target, bubbleArgs, bubbleHandledOnArrival);
    }

    public static void Raise(UiInputTree tree, UIElement target, RoutedEventArgs args)
    {
        Raise(tree, target, args, handledOnArrival: null);
    }

    // Also adds to handledOnArrival each element of the route that args reached already handled, before that element's
    // handlers ran.
    internal static void Raise(
        UiInputTree tree,
        UIElement target,
        RoutedEventArgs args,
        ISet<UIElement>? handledOnArrival)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(args);

        IReadOnlyList<UIElement> routeToRoot = tree.GetRouteToRoot(target);
        IEnumerable<UIElement> route = args.RoutedEvent.RoutingStrategy switch
        {
            RoutingStrategy.Direct => [target],
            RoutingStrategy.Bubble => routeToRoot,
            RoutingStrategy.Tunnel => routeToRoot.Reverse(),
            _ => throw new InvalidOperationException($"Unsupported routing strategy '{args.RoutedEvent.RoutingStrategy}'.")
        };

        foreach (UIElement element in route)
        {
            args.Source = element;
            if (args.Handled)
            {
                handledOnArrival?.Add(element);
            }

            foreach (InputRoutedEventHandlerRegistration registration in tree.GetHandlerRegistrations(element, args.RoutedEvent))
            {
                if (!args.Handled || registration.HandledEventsToo)
                {
                    registration.Handler(element, args);
                }
            }
        }
    }
}
