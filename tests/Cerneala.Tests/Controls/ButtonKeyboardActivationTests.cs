using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class ButtonKeyboardActivationTests
{
    [Fact]
    public void FocusedButtonEnterExecutesCommand()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        ElementInputBridge bridge = FocusedBridge(root, button);
        int executions = 0;
        button.Command = new ActionCommand(_ => executions++);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Enter));

        Assert.Equal(1, executions);
    }

    [Fact]
    public void FocusedButtonSpacePressSetsPressedState()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        ElementInputBridge bridge = FocusedBridge(root, button);
        int executions = 0;
        button.Command = new ActionCommand(_ => executions++);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Space));

        Assert.True(button.IsPressed);
        Assert.Equal(0, executions);
    }

    [Fact]
    public void FocusedButtonSpaceReleaseClearsPressedStateAndExecutesCommand()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        ElementInputBridge bridge = FocusedBridge(root, button);
        int executions = 0;
        button.Command = new ActionCommand(_ => executions++);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Space));
        bridge.Dispatch(root, KeyReleaseFrame(InputKey.Space));

        Assert.False(button.IsPressed);
        Assert.Equal(1, executions);
    }

    [Fact]
    public void SpacePressedButtonIsReleasedWithoutClickWhenFocusMovesAway()
    {
        UIRoot root = RootWithTwoButtons(out ButtonBase first, out ButtonBase second);
        ElementInputBridge bridge = FocusedBridge(root, first);
        int firstClicks = 0;
        int secondClicks = 0;
        first.Click += (_, _) => firstClicks++;
        second.Click += (_, _) => secondClicks++;

        bridge.Dispatch(root, KeyPressFrame(InputKey.Space));
        Assert.True(first.IsPressed);

        Assert.True(bridge.FocusManager.Focus(second, root.InputCache.EnsureCurrent(root)));
        Assert.False(first.IsPressed);

        bridge.Dispatch(root, KeyHeldFrame(InputKey.Space));
        Assert.False(first.IsPressed);
        Assert.False(second.IsPressed);

        bridge.Dispatch(root, KeyReleaseFrame(InputKey.Space));
        Assert.False(first.IsPressed);
        Assert.False(second.IsPressed);
        Assert.Equal(0, firstClicks);
        Assert.Equal(0, secondClicks);
    }

    [Fact]
    public void SpacePressedButtonIsNotClickedWhenFocusLeavesAndReturnsBeforeRelease()
    {
        UIRoot root = RootWithTwoButtons(out ButtonBase first, out ButtonBase second);
        ElementInputBridge bridge = FocusedBridge(root, first);
        int firstClicks = 0;
        first.Click += (_, _) => firstClicks++;

        bridge.Dispatch(root, KeyPressFrame(InputKey.Space));
        Assert.True(bridge.FocusManager.Focus(second, root.InputCache.EnsureCurrent(root)));
        Assert.True(bridge.FocusManager.Focus(first, root.InputCache.EnsureCurrent(root)));
        bridge.Dispatch(root, KeyHeldFrame(InputKey.Space));
        Assert.False(first.IsPressed);

        bridge.Dispatch(root, KeyReleaseFrame(InputKey.Space));
        Assert.False(first.IsPressed);
        Assert.Equal(0, firstClicks);
    }

    [Fact]
    public void SpaceAndTabInOneFrameDoNotLeaveTheOldButtonPressed()
    {
        UIRoot root = RootWithTwoButtons(out ButtonBase first, out ButtonBase second);
        ElementInputBridge bridge = FocusedBridge(root, first);
        int firstClicks = 0;
        int secondClicks = 0;
        first.Click += (_, _) => firstClicks++;
        second.Click += (_, _) => secondClicks++;

        bridge.Dispatch(root, KeyboardFrame(previousKeys: [], currentKeys: [InputKey.Space, InputKey.Tab]));
        Assert.Same(second, bridge.FocusManager.FocusedElement);
        Assert.False(first.IsPressed);
        Assert.False(second.IsPressed);

        bridge.Dispatch(root, KeyboardFrame(previousKeys: [InputKey.Space, InputKey.Tab], currentKeys: [InputKey.Space]));
        Assert.False(first.IsPressed);
        Assert.False(second.IsPressed);

        bridge.Dispatch(root, KeyReleaseFrame(InputKey.Space));
        Assert.False(first.IsPressed);
        Assert.False(second.IsPressed);
        Assert.Equal(0, firstClicks);
        Assert.Equal(0, secondClicks);
    }

    [Fact]
    public void SpaceKeyDownHandlerThatMovesFocusDoesNotLeaveTheButtonPressed()
    {
        UIRoot root = RootWithTwoButtons(out ButtonBase first, out ButtonBase second);
        ElementInputBridge bridge = FocusedBridge(root, first);
        int firstClicks = 0;
        int secondClicks = 0;
        first.Click += (_, _) => firstClicks++;
        second.Click += (_, _) => secondClicks++;
        first.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, _) =>
            bridge.FocusManager.Focus(second, root.InputCache.EnsureCurrent(root)));

        bridge.Dispatch(root, KeyPressFrame(InputKey.Space));
        Assert.Same(second, bridge.FocusManager.FocusedElement);
        Assert.False(first.IsPressed);
        Assert.False(second.IsPressed);

        bridge.Dispatch(root, KeyHeldFrame(InputKey.Space));
        Assert.False(first.IsPressed);

        bridge.Dispatch(root, KeyReleaseFrame(InputKey.Space));
        Assert.False(first.IsPressed);
        Assert.False(second.IsPressed);
        Assert.Equal(0, firstClicks);
        Assert.Equal(0, secondClicks);
    }

    [Fact]
    public void SpacePressedButtonStaysPressedWhileThePointerHoldsItAfterFocusMovesAway()
    {
        UIRoot root = RootWithTwoButtons(out ButtonBase first, out ButtonBase second);
        ElementInputBridge bridge = FocusedBridge(root, first);
        int firstClicks = 0;
        first.Click += (_, _) => firstClicks++;
        PointerSnapshot pointerUp = PointerSnapshot.Empty.WithPosition(20, 20);
        PointerSnapshot pointerDown = pointerUp.WithButton(InputMouseButton.Left, isDown: true);
        KeyboardSnapshot noKeys = KeyboardSnapshot.FromDownKeys([]);
        KeyboardSnapshot space = KeyboardSnapshot.FromDownKeys([InputKey.Space]);

        bridge.Dispatch(root, new InputFrame(pointerUp, pointerUp, noKeys, space, []));
        bridge.Dispatch(root, new InputFrame(pointerUp, pointerDown, space, space, []));
        Assert.True(bridge.FocusManager.Focus(second, root.InputCache.EnsureCurrent(root)));
        Assert.True(first.IsPressed);

        bridge.Dispatch(root, new InputFrame(pointerDown, pointerUp, space, space, []));
        Assert.False(first.IsPressed);
        Assert.Equal(1, firstClicks);

        bridge.Dispatch(root, new InputFrame(pointerUp, pointerUp, space, noKeys, []));
        Assert.Equal(1, firstClicks);
    }

    [Fact]
    public void DisabledFocusedButtonDoesNotExecuteKeyboardCommand()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        ElementInputBridge bridge = FocusedBridge(root, button);
        int executions = 0;
        button.Command = new ActionCommand(_ => executions++);
        button.IsEnabled = false;

        bridge.Dispatch(root, KeyPressFrame(InputKey.Enter));

        Assert.Equal(0, executions);
        Assert.Null(bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void HiddenOrDetachedFocusedButtonIsClearedBeforeKeyboardActivation()
    {
        UIRoot hiddenRoot = RootWithButton(out ButtonBase hiddenButton);
        ElementInputBridge hiddenBridge = FocusedBridge(hiddenRoot, hiddenButton);
        int hiddenExecutions = 0;
        hiddenButton.Command = new ActionCommand(_ => hiddenExecutions++);
        hiddenButton.Visibility = Visibility.Hidden;

        hiddenBridge.Dispatch(hiddenRoot, KeyPressFrame(InputKey.Enter));

        Assert.Equal(0, hiddenExecutions);
        Assert.Null(hiddenBridge.FocusManager.FocusedElement);

        UIRoot detachedRoot = RootWithButton(out ButtonBase detachedButton);
        ElementInputBridge detachedBridge = FocusedBridge(detachedRoot, detachedButton);
        int detachedExecutions = 0;
        detachedButton.Command = new ActionCommand(_ => detachedExecutions++);
        detachedRoot.VisualChildren.Remove(detachedButton);

        detachedBridge.Dispatch(detachedRoot, KeyPressFrame(InputKey.Enter));

        Assert.Equal(0, detachedExecutions);
        Assert.Null(detachedBridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void HandledPreviewKeyDownSuppressesButtonDefaultActivation()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        ElementInputBridge bridge = FocusedBridge(root, button);
        int executions = 0;
        button.Command = new ActionCommand(_ => executions++);
        button.Handlers.AddHandler(InputEvents.PreviewKeyDownEvent, (_, args) => args.Handled = true);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Enter));

        Assert.Equal(0, executions);
    }

    [Fact]
    public void HandledKeyDownSuppressesButtonDefaultActivation()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        ElementInputBridge bridge = FocusedBridge(root, button);
        int executions = 0;
        button.Command = new ActionCommand(_ => executions++);
        button.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, args) => args.Handled = true);

        bridge.Dispatch(root, KeyPressFrame(InputKey.Enter));

        Assert.Equal(0, executions);
    }

    private static ElementInputBridge FocusedBridge(UIRoot root, ButtonBase button)
    {
        ElementInputBridge bridge = new();
        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);

        Assert.True(bridge.FocusManager.Focus(button, routeMap));

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

    private static UIRoot RootWithTwoButtons(out ButtonBase first, out ButtonBase second)
    {
        UIRoot root = RootWithButton(out first);
        second = new ButtonBase();
        second.Arrange(new ArrangeContext(new LayoutRect(50, 0, 40, 40)));
        root.VisualChildren.Add(second);
        return root;
    }

    private static InputFrame KeyPressFrame(InputKey key)
    {
        return KeyboardFrame(previousKeys: [], currentKeys: [key]);
    }

    private static InputFrame KeyHeldFrame(InputKey key)
    {
        return KeyboardFrame(previousKeys: [key], currentKeys: [key]);
    }

    private static InputFrame KeyReleaseFrame(InputKey key)
    {
        return KeyboardFrame(previousKeys: [key], currentKeys: []);
    }

    private static InputFrame KeyboardFrame(IEnumerable<InputKey> previousKeys, IEnumerable<InputKey> currentKeys)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys(previousKeys),
            KeyboardSnapshot.FromDownKeys(currentKeys),
            []);
    }
}
