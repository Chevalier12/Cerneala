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

    // The input bridge resolves a click to the nearest command source on the route of the clicked element and runs
    // only that source's command. A click that a command source in the owner's content owns, such as a Button or a
    // ComboBox's PART_DropDownToggle, bubbles its MouseUp to the owner but is not the owner's click (#421, #935). The
    // decision uses the route map the bridge routed the MouseUp with, so it stays the same when an earlier handler,
    // such as the inner button's Click, removes or collapses the clicked element or detaches the owner (#501).
    public bool OwnsClick(MouseButtonEventArgs args)
    {
        ElementInputRouteMap? routeMap = args.RouteMap;
        if (routeMap is null)
        {
            if (owner.Root is not UIRoot root)
            {
                return true;
            }

            routeMap = root.InputCache.EnsureCurrent(root);
        }

        UIElement? source = args.OriginalSource as UIElement;
        if (source is null)
        {
            return true;
        }

        foreach (UIElement current in routeMap.GetRouteToRoot(source))
        {
            if (current is IInputCommandSource)
            {
                return ReferenceEquals(current, owner);
            }
        }

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
        ICommand? command = resolveCommand();
        Subscribe(command);

        // Refresh does nothing for a source without a command, and setting one queues a refresh (OnCommandChanged).
        // So a ScrollBar's RepeatButtons, built in the bar's first measure, leave no work for the next frame (#714).
        if (command is not null)
        {
            owner.QueueCommandStateRefresh();
        }
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
