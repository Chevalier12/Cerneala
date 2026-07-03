using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class CommandRouter
{
    public bool CanExecute(RoutedCommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!TryGetTargetRoute(context, out UiElementId targetId, out IReadOnlyList<UiElementId> routeToRoot))
        {
            return false;
        }

        CanExecuteRoutedEventArgs previewArgs = new(CommandEvents.PreviewCanExecuteEvent, targetId, context.Command, context.Parameter);
        RaiseCommandBindings(context.RouteMap, routeToRoot.Reverse(), previewArgs);
        if (previewArgs.Handled)
        {
            return previewArgs.CanExecute;
        }

        CanExecuteRoutedEventArgs bubbleArgs = new(CommandEvents.CanExecuteEvent, targetId, context.Command, context.Parameter)
        {
            CanExecute = previewArgs.CanExecute
        };
        RaiseCommandBindings(context.RouteMap, routeToRoot, bubbleArgs);
        return bubbleArgs.CanExecute;
    }

    public bool Execute(RoutedCommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!CanExecute(context) || !TryGetTargetRoute(context, out UiElementId targetId, out IReadOnlyList<UiElementId> routeToRoot))
        {
            return false;
        }

        ExecutedRoutedEventArgs previewArgs = new(CommandEvents.PreviewExecutedEvent, targetId, context.Command, context.Parameter);
        RaiseCommandBindings(context.RouteMap, routeToRoot.Reverse(), previewArgs);
        if (!previewArgs.Handled)
        {
            ExecutedRoutedEventArgs bubbleArgs = new(CommandEvents.ExecutedEvent, targetId, context.Command, context.Parameter);
            RaiseCommandBindings(context.RouteMap, routeToRoot, bubbleArgs);
        }

        return true;
    }

    private static bool TryGetTargetRoute(
        RoutedCommandContext context,
        out UiElementId targetId,
        out IReadOnlyList<UiElementId> routeToRoot)
    {
        targetId = default;
        routeToRoot = [];
        if (context.Target is null || !context.RouteMap.TryGetId(context.Target, out targetId))
        {
            return false;
        }

        routeToRoot = context.RouteMap.InputTree.GetRouteToRoot(targetId);
        return true;
    }

    private static void RaiseCommandBindings(
        ElementInputRouteMap routeMap,
        IEnumerable<UiElementId> route,
        RoutedEventArgs args)
    {
        foreach (UiElementId elementId in route)
        {
            if (args.Handled)
            {
                return;
            }

            if (!routeMap.TryGetElement(elementId, out UIElement? element) || element is null)
            {
                continue;
            }

            args.Source = elementId;
            if (args is CanExecuteRoutedEventArgs canExecuteArgs)
            {
                element.CommandBindings.InvokeCanExecute(elementId, canExecuteArgs);
            }
            else if (args is ExecutedRoutedEventArgs executedArgs)
            {
                element.CommandBindings.InvokeExecuted(elementId, executedArgs);
            }
        }
    }
}
