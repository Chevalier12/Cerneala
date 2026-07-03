using Cerneala.UI.Core;
using Cerneala.UI.Controls;
using Cerneala.UI.Input;

namespace Cerneala.UI.Controls.Primitives;

public class ButtonBase : Control
{
    public static readonly UiProperty<bool> IsPressedProperty = UiProperty<bool>.Register(
        nameof(IsPressed),
        typeof(ButtonBase),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual));

    public static readonly UiProperty<ICommand?> CommandProperty = UiProperty<ICommand?>.Register(
        nameof(Command),
        typeof(ButtonBase),
        new UiPropertyMetadata<ICommand?>(null, UiPropertyOptions.AffectsInputVisual));

    public static readonly UiProperty<object?> CommandParameterProperty = UiProperty<object?>.Register(
        nameof(CommandParameter),
        typeof(ButtonBase),
        new UiPropertyMetadata<object?>(null, UiPropertyOptions.None));

    public bool IsPressed
    {
        get => GetValue(IsPressedProperty);
        set => SetValue(IsPressedProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public bool CanExecuteCommand(CommandRouter router, ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(routeMap);

        return Command switch
        {
            null => false,
            RoutedCommand => router.CanExecute(new RoutedCommandContext(Command, this, routeMap, CommandParameter)),
            _ => Command.CanExecute(CommandParameter)
        };
    }

    public bool ExecuteCommand(CommandRouter router, ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(routeMap);

        if (!IsEnabled)
        {
            return false;
        }

        if (Command is null)
        {
            return false;
        }

        if (Command is RoutedCommand)
        {
            return router.Execute(new RoutedCommandContext(Command, this, routeMap, CommandParameter));
        }

        if (!Command.CanExecute(CommandParameter))
        {
            return false;
        }

        Command.Execute(CommandParameter);
        return true;
    }

    public bool RefreshCommandState(CommandRouter router, ElementInputRouteMap routeMap)
    {
        bool canExecute = Command is null || CanExecuteCommand(router, routeMap);
        if (IsEnabled == canExecute)
        {
            return false;
        }

        IsEnabled = canExecute;
        return true;
    }
}
