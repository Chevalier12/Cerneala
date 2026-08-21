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
            canExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
