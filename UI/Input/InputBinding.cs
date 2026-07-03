using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public class InputBinding
{
    public InputBinding(ICommand command, InputGesture gesture, object? commandParameter = null)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Gesture = gesture ?? throw new ArgumentNullException(nameof(gesture));
        CommandParameter = commandParameter;
    }

    public ICommand Command { get; }

    public InputGesture Gesture { get; }

    public object? CommandParameter { get; }

    public bool Matches(InputFrame frame)
    {
        return Gesture.Matches(frame);
    }

    public bool TryExecute(InputFrame frame)
    {
        if (!Matches(frame) || !Command.CanExecute(CommandParameter))
        {
            return false;
        }

        Command.Execute(CommandParameter);
        return true;
    }

    public bool TryExecute(InputFrame frame, CommandRouter router, ElementInputRouteMap routeMap, UIElement target)
    {
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(routeMap);
        ArgumentNullException.ThrowIfNull(target);

        if (!Matches(frame))
        {
            return false;
        }

        if (Command is RoutedCommand)
        {
            return router.Execute(new RoutedCommandContext(Command, target, routeMap, CommandParameter));
        }

        return TryExecute(frame);
    }
}
