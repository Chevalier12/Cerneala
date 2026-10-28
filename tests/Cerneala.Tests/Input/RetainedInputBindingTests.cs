using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Input;

public sealed class RetainedInputBindingTests
{
    [Fact]
    public void FocusedElementKeyBindingExecutesDirectCommand()
    {
        UIRoot root = RootWithFocusableChild(out UIElement child);
        ElementInputBridge bridge = FocusedBridge(root, child);
        int executions = 0;
        child.InputBindings.Add(new KeyBinding(new ActionCommand(_ => executions++), InputKey.S, KeyModifiers.Control));

        bridge.Dispatch(root, KeyPressFrame(InputKey.S, InputKey.LeftCtrl));

        Assert.Equal(1, executions);
    }

    [Fact]
    public void FocusedElementKeyBindingExecutesRoutedCommandThroughCommandRouter()
    {
        UIRoot root = RootWithFocusableChild(out UIElement child);
        ElementInputBridge bridge = FocusedBridge(root, child);
        RoutedCommand command = new("Save", typeof(RetainedInputBindingTests));
        object? receivedParameter = null;
        child.InputBindings.Add(new KeyBinding(command, InputKey.S, KeyModifiers.Control, "file"));
        child.CommandBindings.Add(new CommandBinding(command, (_, args) => receivedParameter = args.Parameter, (_, args) =>
        {
            args.CanExecute = true;
            args.Handled = true;
        }));

        bridge.Dispatch(root, KeyPressFrame(InputKey.S, InputKey.LeftCtrl));

        Assert.Equal("file", receivedParameter);
    }

    [Fact]
    public void AncestorKeyBindingCanHandleFocusedChildGesture()
    {
        UIRoot root = new();
        UIElement parent = new();
        UIElement child = new() { Focusable = true };
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        ElementInputBridge bridge = FocusedBridge(root, child);
        int executions = 0;
        parent.InputBindings.Add(new KeyBinding(new ActionCommand(_ => executions++), InputKey.Enter));

        bridge.Dispatch(root, KeyPressFrame(InputKey.Enter));

        Assert.Equal(1, executions);
    }

    [Fact]
    public void HandledPreviewKeyDownSuppressesInputBindingExecution()
    {
        UIRoot root = RootWithFocusableChild(out UIElement child);
        ElementInputBridge bridge = FocusedBridge(root, child);
        int executions = 0;
        child.InputBindings.Add(new KeyBinding(new ActionCommand(_ => executions++), InputKey.Enter));
        child.Handlers.AddHandler(InputEvents.PreviewKeyDownEvent, (_, args) => args.Handled = true);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Enter));

        Assert.Equal(0, executions);
    }

    [Fact]
    public void HandledKeyDownSuppressesInputBindingExecution()
    {
        UIRoot root = RootWithFocusableChild(out UIElement child);
        ElementInputBridge bridge = FocusedBridge(root, child);
        int executions = 0;
        child.InputBindings.Add(new KeyBinding(new ActionCommand(_ => executions++), InputKey.Enter));
        child.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, args) => args.Handled = true);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Enter));

        Assert.Equal(0, executions);
    }

    [Fact]
    public void NonMatchingGestureDoesNotExecute()
    {
        UIRoot root = RootWithFocusableChild(out UIElement child);
        ElementInputBridge bridge = FocusedBridge(root, child);
        int executions = 0;
        child.InputBindings.Add(new KeyBinding(new ActionCommand(_ => executions++), InputKey.S, KeyModifiers.Control));

        bridge.Dispatch(root, KeyPressFrame(InputKey.S));

        Assert.Equal(0, executions);
    }

    [Fact]
    public void InputBindingExecutionSuppressesButtonDefaultActivation()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        ElementInputBridge bridge = FocusedBridge(root, button);
        int executions = 0;
        button.InputBindings.Add(new KeyBinding(new ActionCommand(_ => executions += 10), InputKey.Enter));
        button.Command = new ActionCommand(_ => executions++);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Enter));

        Assert.Equal(10, executions);
    }

    // As WPF's KeyGesture.Matches, which checks the key of the KeyEventArgs: a key binding runs for the press of its own
    // key, once, however many other keys go down in the same frame (#771).
    [Theory]
    [InlineData(new InputKey[] { }, new[] { InputKey.LeftCtrl, InputKey.S }, 1)]
    [InlineData(new[] { InputKey.LeftCtrl }, new[] { InputKey.LeftCtrl, InputKey.S, InputKey.A }, 1)]
    [InlineData(new[] { InputKey.LeftCtrl }, new[] { InputKey.LeftCtrl, InputKey.S }, 1)]
    public void KeyBindingExecutesOncePerFrameWhenSeveralKeysGoDownTogether(
        InputKey[] previousKeys, InputKey[] currentKeys, int expectedExecutions)
    {
        UIRoot root = RootWithFocusableChild(out UIElement child);
        ElementInputBridge bridge = FocusedBridge(root, child);
        int executions = 0;
        child.InputBindings.Add(new KeyBinding(new ActionCommand(_ => executions++), InputKey.S, KeyModifiers.Control));
        InputFrame frame = new(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys(previousKeys),
            KeyboardSnapshot.FromDownKeys(currentKeys),
            []);

        bridge.Dispatch(root, frame);

        Assert.Equal(expectedExecutions, executions);
    }

    [Fact]
    public void HandledKeyDownOfTheGestureKeyKeepsTheBindingFromRunningWhenAModifierGoesDownInTheSameFrame()
    {
        UIRoot root = RootWithFocusableChild(out UIElement child);
        ElementInputBridge bridge = FocusedBridge(root, child);
        int executions = 0;
        child.InputBindings.Add(new KeyBinding(new ActionCommand(_ => executions++), InputKey.S, KeyModifiers.Control));
        child.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, args) =>
        {
            if (((KeyEventArgs)args).Key == InputKey.S)
            {
                args.Handled = true;
            }
        });

        bridge.Dispatch(root, new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys([InputKey.LeftCtrl, InputKey.S]),
            []));

        Assert.Equal(0, executions);
    }

    private static ElementInputBridge FocusedBridge(UIRoot root, UIElement element)
    {
        ElementInputBridge bridge = new();
        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);

        Assert.True(bridge.FocusManager.Focus(element, routeMap));

        return bridge;
    }

    private static UIRoot RootWithFocusableChild(out UIElement child)
    {
        UIRoot root = new();
        child = new UIElement { Focusable = true };
        root.VisualChildren.Add(child);
        return root;
    }

    private static UIRoot RootWithButton(out ButtonBase button)
    {
        UIRoot root = new(100, 100);
        button = new ButtonBase();
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 40)));
        root.VisualChildren.Add(button);
        return root;
    }

    private static InputFrame KeyPressFrame(InputKey key, params InputKey[] modifiers)
    {
        InputKey[] currentKeys = [.. modifiers, key];
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys(modifiers),
            KeyboardSnapshot.FromDownKeys(currentKeys),
            []);
    }
}
