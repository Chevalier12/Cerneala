using System.Diagnostics.CodeAnalysis;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class CommandRouter
{
    public bool CanExecute(RoutedCommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!TryGetTargetRoute(context, out UIElement? target, out IReadOnlyList<UIElement> routeToRoot))
        {
            return false;
        }

        CanExecuteRoutedEventArgs previewArgs = new(CommandEvents.PreviewCanExecuteEvent, target, context.Command, context.Parameter);
        RaiseCommandBindings(context.RouteMap, routeToRoot.Reverse(), previewArgs);
        if (previewArgs.Handled)
        {
            return previewArgs.CanExecute;
        }

        CanExecuteRoutedEventArgs bubbleArgs = new(CommandEvents.CanExecuteEvent, target, context.Command, context.Parameter)
        {
            CanExecute = previewArgs.CanExecute
        };
        RaiseCommandBindings(context.RouteMap, routeToRoot, bubbleArgs);
        return bubbleArgs.CanExecute;
    }

    public bool Execute(RoutedCommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!CanExecute(context) || !TryGetTargetRoute(context, out UIElement? target, out IReadOnlyList<UIElement> routeToRoot))
        {
            return false;
        }

        ExecutedRoutedEventArgs previewArgs = new(CommandEvents.PreviewExecutedEvent, target, context.Command, context.Parameter);
        RaiseCommandBindings(context.RouteMap, routeToRoot.Reverse(), previewArgs);
        if (!previewArgs.Handled)
        {
            ExecutedRoutedEventArgs bubbleArgs = new(CommandEvents.ExecutedEvent, target, context.Command, context.Parameter);
            RaiseCommandBindings(context.RouteMap, routeToRoot, bubbleArgs);
        }

        return true;
    }

    private static bool TryGetTargetRoute(
        RoutedCommandContext context,
        [NotNullWhen(true)] out UIElement? target,
        out IReadOnlyList<UIElement> routeToRoot)
    {
        target = null;
        routeToRoot = [];
        if (context.Target is null || !context.RouteMap.Contains(context.Target))
        {
            return false;
        }

        target = context.Target;
        routeToRoot = context.RouteMap.InputTree.GetRouteToRoot(target);
        return true;
    }

    private static void RaiseCommandBindings(
        ElementInputRouteMap routeMap,
        IEnumerable<UIElement> route,
        RoutedEventArgs args)
    {
        foreach (UIElement element in route)
        {
            if (args.Handled)
            {
                return;
            }

            if (!routeMap.Contains(element))
            {
                continue;
            }

            args.Source = element;
            if (args is CanExecuteRoutedEventArgs canExecuteArgs)
            {
                element.CommandBindings.InvokeCanExecute(element, canExecuteArgs);
            }
            else if (args is ExecutedRoutedEventArgs executedArgs)
            {
                element.CommandBindings.InvokeExecuted(element, executedArgs);
            }
        }
    }
}
