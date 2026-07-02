namespace Cerneala.UI.Input;

public static class CommandEvents
{
    public static readonly RoutedEvent PreviewCanExecuteEvent = Register(
        "PreviewCanExecute",
        RoutingStrategy.Tunnel,
        typeof(RoutedEventArgs));

    public static readonly RoutedEvent CanExecuteEvent = Register(
        "CanExecute",
        RoutingStrategy.Bubble,
        typeof(RoutedEventArgs));

    public static readonly RoutedEvent PreviewExecutedEvent = Register(
        "PreviewExecuted",
        RoutingStrategy.Tunnel,
        typeof(RoutedEventArgs));

    public static readonly RoutedEvent ExecutedEvent = Register(
        "Executed",
        RoutingStrategy.Bubble,
        typeof(RoutedEventArgs));

    public static IReadOnlyList<RoutedEvent> All { get; } =
    [
        PreviewCanExecuteEvent,
        CanExecuteEvent,
        PreviewExecutedEvent,
        ExecutedEvent
    ];

    private static RoutedEvent Register(string name, RoutingStrategy routingStrategy, Type argsType)
    {
        return RoutedEventRegistry.Register(name, typeof(CommandEvents), routingStrategy, argsType);
    }
}
