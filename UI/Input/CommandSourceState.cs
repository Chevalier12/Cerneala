using Cerneala.UI.Elements;
using Cerneala.UI.Relay;

namespace Cerneala.UI.Input;

internal sealed class CommandSourceState
{
    private readonly UIElement owner;
    private readonly Func<ICommand?> resolveCommand;
    private readonly Func<object?> resolveParameter;
    private readonly UiRelayRefreshDispatcher refreshDispatcher;
    private IObservableCommand? observableCommand;
    private Func<bool>? callbackGuard;
    private bool isAttached;

    public CommandSourceState(
        UIElement owner,
        Func<ICommand?> resolveCommand,
        Func<object?> resolveParameter)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.resolveCommand = resolveCommand ?? throw new ArgumentNullException(nameof(resolveCommand));
        this.resolveParameter = resolveParameter ?? throw new ArgumentNullException(nameof(resolveParameter));
        refreshDispatcher = new UiRelayRefreshDispatcher(
            () => owner.Root?.Relay,
            owner.QueueCommandStateRefresh,
            "command state");
    }

    public bool CanExecute(CommandRouter router, ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(routeMap);

        ICommand? command = resolveCommand();
        object? parameter = resolveParameter();
        return command switch
        {
            null => false,
            RoutedCommand => router.CanExecute(new RoutedCommandContext(command, owner, routeMap, parameter)),
            _ => command.CanExecute(parameter)
        };
    }

    public bool Execute(CommandRouter router, ElementInputRouteMap routeMap)
    {
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(routeMap);

        if (!owner.IsEnabled || resolveCommand() is not ICommand command)
        {
            return false;
        }

        object? parameter = resolveParameter();
        if (command is RoutedCommand)
        {
            return router.Execute(new RoutedCommandContext(command, owner, routeMap, parameter));
        }

        if (!command.CanExecute(parameter))
        {
            return false;
        }

        command.Execute(parameter);
        return true;
    }

    public bool Refresh(CommandRouter router, ElementInputRouteMap routeMap)
    {
        if (resolveCommand() is null)
        {
            return false;
        }

        bool canExecute = CanExecute(router, routeMap);
        if (owner.IsEnabled == canExecute)
        {
            return false;
        }

        owner.IsEnabled = canExecute;
        return true;
    }

    public void Attach()
    {
        if (isAttached)
        {
            return;
        }

        isAttached = true;
        callbackGuard = refreshDispatcher.Activate();
        Subscribe(resolveCommand());
        owner.QueueCommandStateRefresh();
    }

    public void Detach()
    {
        if (!isAttached)
        {
            return;
        }

        isAttached = false;
        Unsubscribe();
        refreshDispatcher.Deactivate();
        callbackGuard = null;
    }

    public void OnCommandChanged()
    {
        if (isAttached)
        {
            Subscribe(resolveCommand());
        }
        else
        {
            Unsubscribe();
        }

        owner.QueueCommandStateRefresh();
    }

    public void OnParameterChanged()
    {
        owner.QueueCommandStateRefresh();
    }

    private void Subscribe(ICommand? command)
    {
        if (!isAttached || ReferenceEquals(observableCommand, command))
        {
            return;
        }

        Unsubscribe();
        observableCommand = command as IObservableCommand;
        if (observableCommand is not null)
        {
            observableCommand.CanExecuteChanged += OnCanExecuteChanged;
        }
    }

    private void Unsubscribe()
    {
        if (observableCommand is null)
        {
            return;
        }

        observableCommand.CanExecuteChanged -= OnCanExecuteChanged;
        observableCommand = null;
    }

    private void OnCanExecuteChanged(object? sender, EventArgs args)
    {
        if (ReferenceEquals(sender, observableCommand) && callbackGuard?.Invoke() == true)
        {
            owner.QueueCommandStateRefresh();
        }
    }
}
