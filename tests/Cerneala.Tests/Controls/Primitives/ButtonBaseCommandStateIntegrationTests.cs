using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls.Primitives;

public sealed class ButtonBaseCommandStateIntegrationTests
{
    [Fact]
    public void RoutedCommandBindingAddRefreshesAffectedButtonState()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        RoutedCommand command = new("Save", typeof(ButtonBaseCommandStateIntegrationTests));
        button.Command = command;
        root.ProcessFrame();
        Assert.False(button.IsEnabled);

        root.CommandBindings.Add(new CommandBinding(command, (_, _) => { }, (_, args) =>
        {
            args.CanExecute = true;
            args.Handled = true;
        }));
        FrameStats changed = root.ProcessFrame();

        Assert.True(button.IsEnabled);
        Assert.Equal(1, changed.CommandStateElements);
    }

    [Fact]
    public void RoutedCommandBindingCanExecuteFalseDisablesButtonBeforeKeyboardActivation()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        RoutedCommand command = new("Save", typeof(ButtonBaseCommandStateIntegrationTests));
        button.Command = command;
        bool canExecute = true;
        int executions = 0;
        root.CommandBindings.Add(new CommandBinding(command, (_, _) => executions++, (_, args) =>
        {
            args.CanExecute = canExecute;
            args.Handled = true;
        }));
        root.ProcessFrame();
        ElementInputBridge bridge = FocusedBridge(root, button);

        canExecute = false;
        root.CommandBindings.Clear();
        root.CommandBindings.Add(new CommandBinding(command, (_, _) => executions++, (_, args) =>
        {
            args.CanExecute = false;
            args.Handled = true;
        }));
        root.ProcessFrame();
        bridge.Dispatch(root, KeyPressFrame(InputKey.Enter));

        Assert.False(button.IsEnabled);
        Assert.Equal(0, executions);
    }

    [Fact]
    public void DisabledByCommandStateDoesNotExecuteMouseClick()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        int executions = 0;
        button.Command = new ActionCommand(_ => executions++, _ => false);
        root.ProcessFrame();
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, currentDown: true));
        bridge.Dispatch(root, PointerFrame(10, 10, previousDown: true));

        Assert.False(button.IsEnabled);
        Assert.Equal(0, executions);
    }

    [Fact]
    public void DisabledByCommandStateDoesNotExecuteKeyboardActivation()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        int executions = 0;
        button.Command = new ActionCommand(_ => executions++, _ => false);
        root.ProcessFrame();
        ElementInputBridge bridge = FocusedBridge(root, button);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Enter));

        Assert.False(button.IsEnabled);
        Assert.Equal(0, executions);
    }

    [Fact]
    public void CanExecuteChangedAndParameterChangesRefreshButtonState()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        TrackingCommand command = new(parameter => Equals(parameter, "allowed"));
        button.Command = command;
        root.ProcessFrame();
        Assert.False(button.IsEnabled);

        button.CommandParameter = "allowed";
        root.ProcessFrame();
        Assert.True(button.IsEnabled);

        command.CanExecuteEvaluator = _ => false;
        command.RaiseCanExecuteChanged();
        root.ProcessFrame();
        Assert.False(button.IsEnabled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanExecuteChangedRefreshesTheButtonWhateverTheSender(bool senderIsTheCommand)
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        TrackingCommand command = new(_ => true);
        button.Command = command;
        root.ProcessFrame();
        Assert.True(button.IsEnabled);

        command.CanExecuteEvaluator = _ => false;
        command.RaiseCanExecuteChanged(senderIsTheCommand ? command : null);
        root.ProcessFrame();

        Assert.False(button.IsEnabled);
    }

    [Fact]
    public void CanExecuteChangedOfAReplacedCommandDoesNotRefreshTheButton()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        TrackingCommand first = new(_ => true);
        TrackingCommand second = new(_ => true);
        button.Command = first;
        button.Command = second;
        root.ProcessFrame();

        first.RaiseCanExecuteChanged(null);
        FrameStats stats = root.ProcessFrame();

        Assert.True(button.IsEnabled);
        Assert.Equal(0, stats.CommandStateElements);
    }

    [Fact]
    public void CommandChangesAndDetachReattachMaintainOneObservableSubscription()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        TrackingCommand first = new(_ => true);
        TrackingCommand second = new(_ => true);

        button.Command = first;
        Assert.Equal(1, first.AddCount);
        Assert.Equal(0, first.RemoveCount);

        button.Command = second;
        Assert.Equal(1, first.RemoveCount);
        Assert.Equal(1, second.AddCount);

        root.VisualChildren.Remove(button);
        Assert.Equal(1, second.RemoveCount);

        root.VisualChildren.Add(button);
        Assert.Equal(2, second.AddCount);
        Assert.Equal(1, second.RemoveCount);
    }

    public enum HiddenBy
    {
        None,
        Collapsed,
        Hidden,
        IsVisibleFalse,
        CollapsedParent,
        SkippedByVirtualization
    }

    // #901: a refresh while the button takes no part in input cannot route the command to its binding.
    [Theory]
    [InlineData(HiddenBy.None)]
    [InlineData(HiddenBy.Collapsed)]
    [InlineData(HiddenBy.Hidden)]
    [InlineData(HiddenBy.IsVisibleFalse)]
    [InlineData(HiddenBy.CollapsedParent)]
    [InlineData(HiddenBy.SkippedByVirtualization)]
    public void RoutedCommandButtonIsEnabledOnceItBecomesVisible(HiddenBy hiddenBy)
    {
        UIRoot root = RootWithHideableButton(out ButtonBase button, out Action<bool> setHidden, hiddenBy);
        RoutedCommand command = new("Save", typeof(ButtonBaseCommandStateIntegrationTests));
        root.CommandBindings.Add(new CommandBinding(command, (_, _) => { }, (_, args) =>
        {
            args.CanExecute = true;
            args.Handled = true;
        }));
        setHidden(true);

        button.Command = command;
        root.ProcessFrame();
        setHidden(false);
        root.ProcessFrame();

        Assert.True(button.IsEnabled);
    }

    [Theory]
    [InlineData(HiddenBy.None)]
    [InlineData(HiddenBy.Collapsed)]
    [InlineData(HiddenBy.CollapsedParent)]
    [InlineData(HiddenBy.SkippedByVirtualization)]
    public void RoutedCommandButtonIsDisabledOnceItBecomesVisibleWhenItsCommandCannotExecute(HiddenBy hiddenBy)
    {
        UIRoot root = RootWithHideableButton(out ButtonBase button, out Action<bool> setHidden, hiddenBy);
        RoutedCommand command = new("Save", typeof(ButtonBaseCommandStateIntegrationTests));
        root.CommandBindings.Add(new CommandBinding(command, (_, _) => { }, (_, args) =>
        {
            args.CanExecute = false;
            args.Handled = true;
        }));
        setHidden(true);

        button.Command = command;
        root.ProcessFrame();
        // A hidden button cannot reach the binding, so its refresh leaves IsEnabled unchanged.
        Assert.Equal(hiddenBy != HiddenBy.None, button.IsEnabled);
        setHidden(false);
        root.ProcessFrame();

        Assert.False(button.IsEnabled);
    }

    [Fact]
    public void CollapsedRoutedCommandButtonWaitsForAnInputRouteChangeBeforeRefreshingAgain()
    {
        UIRoot root = RootWithHideableButton(out ButtonBase button, out Action<bool> setHidden, HiddenBy.Collapsed);
        RoutedCommand command = new("Save", typeof(ButtonBaseCommandStateIntegrationTests));
        setHidden(true);
        button.Command = command;
        Assert.Equal(1, root.ProcessFrame().CommandStateElements);

        FrameStats idle = root.ProcessFrame();
        root.CommandBindings.Add(new CommandBinding(command, (_, _) => { }, (_, args) =>
        {
            args.CanExecute = true;
            args.Handled = true;
        }));
        FrameStats bindingAdded = root.ProcessFrame();
        setHidden(false);
        FrameStats shown = root.ProcessFrame();

        Assert.Equal(0, idle.CommandStateElements);
        Assert.Equal(1, bindingAdded.CommandStateElements);
        Assert.Equal(1, shown.CommandStateElements);
        Assert.True(button.IsEnabled);
    }

    // A handler run by an earlier refresh of the same command-state phase shows the waiting button, so its refresh
    // later in that phase must see the new route instead of deferring again with nothing left to wake it.
    [Fact]
    public void RoutedCommandButtonShownDuringTheCommandStatePhaseIsRefreshedAgainstTheNewRoute()
    {
        UIRoot root = new(100, 100);
        ButtonBase edit = new();
        ButtonBase save = new() { Visibility = Visibility.Collapsed };
        root.VisualChildren.Add(edit);
        root.VisualChildren.Add(save);
        RoutedCommand saveCommand = new("Save", typeof(ButtonBaseCommandStateIntegrationTests));
        root.CommandBindings.Add(new CommandBinding(saveCommand, (_, _) => { }, (_, args) =>
        {
            args.CanExecute = false;
            args.Handled = true;
        }));
        TrackingCommand editCommand = new(_ => true);
        edit.Command = editCommand;
        save.Command = saveCommand;
        root.ProcessFrame();
        Assert.True(save.IsEnabled);
        edit.IsEnabledChanged += (_, _) => save.Visibility = Visibility.Visible;

        editCommand.CanExecuteEvaluator = _ => false;
        editCommand.RaiseCanExecuteChanged();
        save.CommandParameter = "document";
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.False(edit.IsEnabled);
        Assert.Equal(Visibility.Visible, save.Visibility);
        Assert.False(save.IsEnabled);
    }

    private static UIRoot RootWithHideableButton(out ButtonBase button, out Action<bool> setHidden, HiddenBy hiddenBy)
    {
        UIRoot root = new(100, 100);
        ButtonBase target = new();
        UIElement hidden = target;
        if (hiddenBy == HiddenBy.CollapsedParent)
        {
            StackPanel parent = new();
            parent.Children.Add(target);
            root.VisualChildren.Add(parent);
            hidden = parent;
        }
        else
        {
            root.VisualChildren.Add(target);
        }

        setHidden = isHidden =>
        {
            switch (hiddenBy)
            {
                case HiddenBy.Collapsed:
                case HiddenBy.CollapsedParent:
                    hidden.Visibility = isHidden ? Visibility.Collapsed : Visibility.Visible;
                    break;
                case HiddenBy.Hidden:
                    hidden.Visibility = isHidden ? Visibility.Hidden : Visibility.Visible;
                    break;
                case HiddenBy.IsVisibleFalse:
                    hidden.IsVisible = !isHidden;
                    break;
                case HiddenBy.SkippedByVirtualization:
                    hidden.SetSkippedByVirtualization(isHidden);
                    break;
            }
        };
        button = target;
        return root;
    }

    private static ElementInputBridge FocusedBridge(UIRoot root, ButtonBase button)
    {
        ElementInputBridge bridge = new();
        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        bridge.FocusManager.Focus(button, routeMap);
        return bridge;
    }

    private static UIRoot RootWithButton(out ButtonBase button)
    {
        UIRoot root = new(100, 100);
        button = new ButtonBase();
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 40)));
        root.VisualChildren.Add(button);
        return root;
    }

    private static InputFrame KeyPressFrame(InputKey key)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys([key]),
            []);
    }

    private static InputFrame PointerFrame(float x, float y, bool previousDown = false, bool currentDown = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y);
        if (previousDown)
        {
            previous = previous.WithButton(InputMouseButton.Left, true);
        }

        if (currentDown)
        {
            current = current.WithButton(InputMouseButton.Left, true);
        }

        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private sealed class TrackingCommand(Func<object?, bool> canExecute) : IObservableCommand
    {
        private EventHandler? canExecuteChanged;

        public Func<object?, bool> CanExecuteEvaluator { get; set; } = canExecute;

        public int AddCount { get; private set; }

        public int RemoveCount { get; private set; }

        public event EventHandler? CanExecuteChanged
        {
            add
            {
                AddCount++;
                canExecuteChanged += value;
            }
            remove
            {
                RemoveCount++;
                canExecuteChanged -= value;
            }
        }

        public bool CanExecute(object? parameter) => CanExecuteEvaluator(parameter);

        public void Execute(object? parameter)
        {
        }

        public void RaiseCanExecuteChanged()
        {
            RaiseCanExecuteChanged(this);
        }

        public void RaiseCanExecuteChanged(object? sender)
        {
            canExecuteChanged?.Invoke(sender, EventArgs.Empty);
        }
    }
}
