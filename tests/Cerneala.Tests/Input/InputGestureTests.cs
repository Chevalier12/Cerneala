using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class InputGestureTests
{
    [Fact]
    public void KeyGestureMatchesPressedKeyAndExactModifiers()
    {
        KeyGesture gesture = new(InputKey.S, KeyModifiers.Control | KeyModifiers.Shift);

        Assert.True(gesture.Matches(Frame(previous: [InputKey.LeftCtrl, InputKey.LeftShift], current: [InputKey.LeftCtrl, InputKey.LeftShift, InputKey.S])));
        Assert.False(gesture.Matches(Frame(previous: [InputKey.LeftCtrl], current: [InputKey.LeftCtrl, InputKey.S])));
        Assert.False(gesture.Matches(Frame(previous: [InputKey.LeftCtrl, InputKey.LeftShift, InputKey.LeftAlt], current: [InputKey.LeftCtrl, InputKey.LeftShift, InputKey.LeftAlt, InputKey.S])));
    }

    [Fact]
    public void KeyBindingExecutesActionCommandWhenGestureMatches()
    {
        bool executed = false;
        KeyBinding binding = new(new ActionCommand(_ => executed = true), InputKey.Enter);

        bool result = binding.TryExecute(Frame(current: [InputKey.Enter]));

        Assert.True(result);
        Assert.True(executed);
    }

    [Fact]
    public void KeyBindingDoesNotExecuteWhenGestureDoesNotMatch()
    {
        bool executed = false;
        KeyBinding binding = new(new ActionCommand(_ => executed = true), InputKey.Enter);

        bool result = binding.TryExecute(Frame(current: [InputKey.Escape]));

        Assert.False(result);
        Assert.False(executed);
    }

    [Fact]
    public void KeyBindingRoutesRoutedCommandThroughExplicitRouter()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        RoutedCommand command = new("Save", typeof(InputGestureTests));
        bool executed = false;
        child.CommandBindings.Add(new CommandBinding(command, (_, _) => executed = true, (_, args) =>
        {
            args.CanExecute = true;
            args.Handled = true;
        }));
        KeyBinding binding = new(command, InputKey.S, KeyModifiers.Control, "file");

        bool result = binding.TryExecute(Frame(previous: [InputKey.LeftCtrl], current: [InputKey.LeftCtrl, InputKey.S]), new CommandRouter(), map, child);

        Assert.True(result);
        Assert.True(executed);
    }

    private static InputFrame Frame(InputKey[]? previous = null, InputKey[]? current = null)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys(previous ?? []),
            KeyboardSnapshot.FromDownKeys(current ?? []),
            []);
    }
}
