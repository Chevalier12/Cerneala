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
    private bool disabledByCommand;
    private bool writingEnabled;

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
        ICommand? command = resolveCommand();
        if (command is null)
        {
            // As WPF's ButtonBase, a source without a command is enabled as far as commands go: undo the disable the
            // removed command wrote, and leave an IsEnabled that the app wrote alone (#1202).
            return RefreshIgnoringCommand();
        }

        // A routed command reaches its CommandBinding only along the input route. A source outside the route (collapsed,
        // hidden, skipped by virtualization) keeps IsEnabled and refreshes again when the route changes (#901).
        if (command is RoutedCommand && !routeMap.Contains(owner))
        {
            owner.Root?.DeferCommandStateRefreshUntilInputRouteChanges(owner);
            return false;
        }

        bool canExecute = CanExecute(router, routeMap);
        if (canExecute)
        {
            disabledByCommand = false;
        }

        if (owner.IsEnabled == canExecute)
        {
            return false;
        }

        writingEnabled = true;
        try
        {
            owner.IsEnabled = canExecute;
        }
        finally
        {
            writingEnabled = false;
        }

        disabledByCommand = !canExecute;
        return true;
    }

    // The owner reports every write of IsEnabled, also one that keeps the value. A write that Refresh did not make is
    // someone else's IsEnabled, so RefreshIgnoringCommand must leave it alone.
    public void OnEnabledWritten()
    {
        if (!writingEnabled)
        {
            disabledByCommand = false;
        }
    }

    // For an owner without a command (#1202) or that ignores its command (a MenuItem with children, #934): undo the
    // disable that Refresh wrote, and leave an IsEnabled that the command did not write alone.
    public bool RefreshIgnoringCommand()
    {
        if (!disabledByCommand)
        {
            return false;
        }

        disabledByCommand = false;
        if (owner.IsEnabled)
        {
            return false;
        }

        owner.IsEnabled = true;
        return true;
    }

    // The input bridge resolves a click to the nearest command source on the route of the clicked element and runs
    // only that source's command. A click that a command source in the owner's content owns, such as a Button or a
    // ComboBox's PART_DropDownToggle, bubbles its MouseUp to the owner but is not the owner's click (#421, #935). The
    // decision uses the route map the bridge routed the MouseUp with, so it stays the same when an earlier handler,
    // such as the inner button's Click, removes or collapses the clicked element or detaches the owner (#501). Like the
    // bridge, it walks the visual ancestors: a MouseUp from content that an Overlay in the owner's content projects
    // bubbles to the owner, but the owner is not a visual ancestor of it, so it is not the owner's click (#1203).
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

        IReadOnlyList<UIElement> visualRoute = routeMap.GetVisualRouteToRoot(source);
        if (visualRoute.Count == 0)
        {
            return true;
        }

        foreach (UIElement current in visualRoute)
        {
            if (current is IInputCommandSource)
            {
                return ReferenceEquals(current, owner);
            }
        }

        return false;
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

    // The event is the request, whatever its sender: commands may raise it with null or forward an inner command's
    // event (#902). Only the bound command's event reaches this handler, because Subscribe removes it from the old one.
    private void OnCanExecuteChanged(object? sender, EventArgs args)
    {
        if (callbackGuard?.Invoke() == true)
        {
            owner.QueueCommandStateRefresh();
        }
    }
}
