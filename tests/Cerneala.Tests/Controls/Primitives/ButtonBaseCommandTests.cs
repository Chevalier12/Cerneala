using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls.Primitives;

public sealed class ButtonBaseCommandTests
{
    [Fact]
    public void ButtonClickExecutesDirectCommandWithParameter()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        object? received = null;
        button.Command = new ActionCommand(parameter => received = parameter);
        button.CommandParameter = "file";
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, currentDown: true));
        bridge.Dispatch(root, PointerFrame(10, 10, previousDown: true));

        Assert.Equal("file", received);
    }

    [Fact]
    public void ButtonClickExecutesRoutedCommandThroughRouter()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        RoutedCommand command = new("Save", typeof(ButtonBaseCommandTests));
        button.Command = command;
        button.CommandParameter = "file";
        object? received = null;
        root.CommandBindings.Add(new CommandBinding(command, (_, args) => received = ((ExecutedRoutedEventArgs)args).Parameter, (_, args) =>
        {
            args.CanExecute = true;
            args.Handled = true;
        }));
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, currentDown: true));
        bridge.Dispatch(root, PointerFrame(10, 10, previousDown: true));

        Assert.Equal("file", received);
    }

    [Fact]
    public void ButtonClickDoesNotExecuteCannotExecuteCommand()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        bool executed = false;
        button.Command = new ActionCommand(_ => executed = true, _ => false);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, currentDown: true));
        bridge.Dispatch(root, PointerFrame(10, 10, previousDown: true));

        Assert.False(executed);
    }

    [Fact]
    public void HandledMouseUpDoesNotExecuteCommand()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        bool executed = false;
        button.Command = new ActionCommand(_ => executed = true);
        root.Handlers.AddHandler(InputEvents.PreviewMouseUpEvent, (_, args) => args.Handled = true);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, currentDown: true));
        bridge.Dispatch(root, PointerFrame(10, 10, previousDown: true));

        Assert.False(executed);
    }

    [Fact]
    public void CanceledClickDoesNotExecuteCommand()
    {
        UIRoot root = new(100, 100);
        ButtonBase button = ArrangedButton(0, 0, 40, 40);
        UIElement other = ArrangedElement(50, 0, 40, 40);
        root.VisualChildren.Add(button);
        root.VisualChildren.Add(other);
        bool executed = false;
        button.Command = new ActionCommand(_ => executed = true);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, currentDown: true));
        bridge.Dispatch(root, PointerFrame(10, 10, 60, 10, previousDown: true));

        Assert.False(executed);
    }

    [Fact]
    public void DisabledButtonDoesNotExecuteCommand()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        bool executed = false;
        button.Command = new ActionCommand(_ => executed = true);
        button.IsEnabled = false;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, currentDown: true));
        bridge.Dispatch(root, PointerFrame(10, 10, previousDown: true));

        Assert.False(executed);
    }

    [Fact]
    public void ExecuteCommandDoesNotRunDirectCommandWhenButtonIsDisabled()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        bool executed = false;
        button.Command = new ActionCommand(_ => executed = true);
        button.IsEnabled = false;
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);

        bool result = button.ExecuteCommand(new CommandRouter(), map);

        Assert.False(result);
        Assert.False(executed);
    }

    [Fact]
    public void RefreshCommandStateUpdatesEnabledState()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        button.Command = new ActionCommand(_ => { }, _ => false);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);

        bool changed = button.RefreshCommandState(new CommandRouter(), map);

        Assert.True(changed);
        Assert.False(button.IsEnabled);
    }

    private static UIRoot RootWithButton(out ButtonBase button)
    {
        UIRoot root = new(100, 100);
        button = ArrangedButton(0, 0, 40, 40);
        root.VisualChildren.Add(button);
        return root;
    }

    private static ButtonBase ArrangedButton(float x, float y, float width, float height)
    {
        ButtonBase button = new();
        button.Arrange(new ArrangeContext(new LayoutRect(x, y, width, height)));
        return button;
    }

    private static UIElement ArrangedElement(float x, float y, float width, float height)
    {
        UIElement element = new();
        element.Arrange(new ArrangeContext(new LayoutRect(x, y, width, height)));
        return element;
    }

    private static InputFrame PointerFrame(float x, float y, bool previousDown = false, bool currentDown = false)
    {
        return PointerFrame(x, y, x, y, previousDown, currentDown);
    }

    private static InputFrame PointerFrame(
        float previousX,
        float previousY,
        float currentX,
        float currentY,
        bool previousDown = false,
        bool currentDown = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(previousX, previousY);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(currentX, currentY);
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
}
