using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class FocusManagerTests
{
    [Fact]
    public void FocusChangeUpdatesStateAndRaisesEvents()
    {
        UIRoot root = new();
        UIElement parent = new();
        UIElement first = new() { Focusable = true };
        UIElement second = new() { Focusable = true };
        parent.VisualChildren.Add(first);
        parent.VisualChildren.Add(second);
        root.VisualChildren.Add(parent);
        List<string> calls = [];
        first.Handlers.AddHandler(InputEvents.LostKeyboardFocusEvent, (_, _) => calls.Add("lost-first"));
        second.Handlers.AddHandler(InputEvents.GotKeyboardFocusEvent, (_, _) => calls.Add("got-second"));
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        FocusManager manager = new();

        manager.Focus(first, map);
        manager.Focus(second, map);

        Assert.False(first.IsKeyboardFocused);
        Assert.True(second.IsKeyboardFocused);
        Assert.True(parent.IsKeyboardFocusWithin);
        Assert.Equal(["lost-first", "got-second"], calls);
    }

    [Fact]
    public void FocusChangeRaisesPreviewEventsBeforeBubbleEvents()
    {
        UIRoot root = new();
        UIElement first = new() { Focusable = true };
        UIElement second = new() { Focusable = true };
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        List<string> calls = [];
        first.Handlers.AddHandler(InputEvents.PreviewLostKeyboardFocusEvent, (_, _) => calls.Add("preview-lost-first"));
        second.Handlers.AddHandler(InputEvents.PreviewGotKeyboardFocusEvent, (_, _) => calls.Add("preview-got-second"));
        first.Handlers.AddHandler(InputEvents.LostKeyboardFocusEvent, (_, _) => calls.Add("lost-first"));
        second.Handlers.AddHandler(InputEvents.GotKeyboardFocusEvent, (_, _) => calls.Add("got-second"));
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        FocusManager manager = new();

        manager.Focus(first, map);
        calls.Clear();
        manager.Focus(second, map);

        Assert.Equal(["preview-lost-first", "preview-got-second", "lost-first", "got-second"], calls);
    }

    [Fact]
    public void FocusWithinClearsOldBranchWhenFocusMovesToNewBranch()
    {
        UIRoot root = new();
        UIElement oldParent = new();
        UIElement oldChild = new() { Focusable = true };
        UIElement newParent = new();
        UIElement newChild = new() { Focusable = true };
        oldParent.VisualChildren.Add(oldChild);
        newParent.VisualChildren.Add(newChild);
        root.VisualChildren.Add(oldParent);
        root.VisualChildren.Add(newParent);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        FocusManager manager = new();

        manager.Focus(oldChild, map);
        manager.Focus(newChild, map);

        Assert.False(oldChild.IsKeyboardFocusWithin);
        Assert.False(oldParent.IsKeyboardFocusWithin);
        Assert.True(newChild.IsKeyboardFocusWithin);
        Assert.True(newParent.IsKeyboardFocusWithin);
        Assert.True(root.IsKeyboardFocusWithin);
    }

    [Fact]
    public void FocusWithinPreservesSharedAncestorWhenFocusMovesToAncestor()
    {
        UIRoot root = new();
        UIElement parent = new() { Focusable = true };
        UIElement child = new() { Focusable = true };
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        FocusManager manager = new();

        manager.Focus(child, map);
        manager.Focus(parent, map);

        Assert.False(child.IsKeyboardFocusWithin);
        Assert.True(parent.IsKeyboardFocusWithin);
        Assert.True(root.IsKeyboardFocusWithin);
    }

    [Fact]
    public void FocusedElementReceivesKeyboardEvents()
    {
        UIRoot root = new();
        UIElement target = new() { Focusable = true };
        root.VisualChildren.Add(target);
        List<InputKey> keys = [];
        target.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, args) => keys.Add(((KeyEventArgs)args).Key));
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        FocusManager manager = new();
        manager.Focus(target, map);

        manager.DispatchKeyboard(FrameWithKey(InputKey.A), map);

        Assert.Equal([InputKey.A], keys);
    }

    [Fact]
    public void HandledPreviewKeyDownSuppressesBubbleKeyDown()
    {
        UIRoot root = new();
        UIElement target = new() { Focusable = true };
        root.VisualChildren.Add(target);
        bool bubbleCalled = false;
        target.Handlers.AddHandler(InputEvents.PreviewKeyDownEvent, (_, args) => args.Handled = true);
        target.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, _) => bubbleCalled = true);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        FocusManager manager = new();
        manager.Focus(target, map);

        manager.DispatchKeyboard(FrameWithKey(InputKey.A), map);

        Assert.False(bubbleCalled);
    }

    [Fact]
    public void NoFocusedElementIgnoresKeyboardInput()
    {
        UIRoot root = new();
        UIElement target = new();
        root.VisualChildren.Add(target);
        bool called = false;
        target.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, _) => called = true);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);

        new FocusManager().DispatchKeyboard(FrameWithKey(InputKey.A), map);

        Assert.False(called);
    }

    [Fact]
    public void KeyRepeatsRaiseRepeatedKeyDownPairsBetweenPressAndRelease()
    {
        UIRoot root = new();
        UIElement target = new() { Focusable = true };
        root.VisualChildren.Add(target);
        List<string> calls = [];
        target.Handlers.AddHandler(InputEvents.PreviewKeyDownEvent, (_, args) => calls.Add($"preview-down:{((KeyEventArgs)args).IsRepeat}"));
        target.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, args) => calls.Add($"down:{((KeyEventArgs)args).IsRepeat}"));
        target.Handlers.AddHandler(InputEvents.PreviewKeyUpEvent, (_, args) => calls.Add($"preview-up:{((KeyEventArgs)args).IsRepeat}"));
        target.Handlers.AddHandler(InputEvents.KeyUpEvent, (_, args) => calls.Add($"up:{((KeyEventArgs)args).IsRepeat}"));
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        FocusManager manager = new();
        manager.Focus(target, map);
        KeyboardSnapshot up = KeyboardSnapshot.Empty;
        KeyboardSnapshot down = KeyboardSnapshot.FromDownKeys([InputKey.Back]);

        manager.DispatchKeyboard(new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, up, down, [], [InputKey.Back]), map);
        manager.DispatchKeyboard(new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, down, down, [], [InputKey.Back, InputKey.Back]), map);
        manager.DispatchKeyboard(new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, down, up, [], [InputKey.Back]), map);

        Assert.Equal(
        [
            "preview-down:False", "down:False", "preview-down:True", "down:True",
            "preview-down:True", "down:True", "preview-down:True", "down:True",
            "preview-down:True", "down:True", "preview-up:False", "up:False"
        ], calls);
    }

    [Fact]
    public void KeyRepeatForKeyThatIsNeitherDownNorReleasedRaisesNothing()
    {
        UIRoot root = new();
        UIElement target = new() { Focusable = true };
        root.VisualChildren.Add(target);
        bool called = false;
        target.Handlers.AddHandler(InputEvents.PreviewKeyDownEvent, (_, _) => called = true);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        FocusManager manager = new();
        manager.Focus(target, map);

        manager.DispatchKeyboard(
            new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, [], [InputKey.Back]),
            map);

        Assert.False(called);
    }

    internal static InputFrame FrameWithKey(InputKey key)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys([key]),
            []);
    }
}
