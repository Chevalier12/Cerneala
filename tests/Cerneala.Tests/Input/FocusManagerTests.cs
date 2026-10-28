using Cerneala.UI.Controls.Primitives;
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
    public void KeyRepeatGoesToElementFocusedByPreviousRepeatOfSameFrame()
    {
        UIRoot root = new();
        UIElement[] chain = [new() { Focusable = true }, new() { Focusable = true }, new() { Focusable = true }];
        foreach (UIElement element in chain)
        {
            root.VisualChildren.Add(element);
        }

        FocusManager manager = new();
        ElementInputRouteMap map = null!;
        List<string> receivers = [];
        for (int i = 0; i < chain.Length; i++)
        {
            UIElement next = chain[(i + 1) % chain.Length];
            string name = ((char)('A' + i)).ToString();
            chain[i].Handlers.AddHandler(InputEvents.KeyDownEvent, (_, _) =>
            {
                receivers.Add(name);
                manager.Focus(next, map);
            });
        }

        map = new ElementInputRouteBuilder().Build(root);
        manager.Focus(chain[0], map);
        KeyboardSnapshot down = KeyboardSnapshot.FromDownKeys([InputKey.Down]);

        manager.DispatchKeyboard(new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, down, down, [], [InputKey.Down, InputKey.Down]), map);

        Assert.Equal(["A", "B"], receivers);
        Assert.True(chain[2].IsKeyboardFocused);
    }

    [Fact]
    public void KeyRepeatIsNotRaisedOnFocusedElementDetachedByPreviousRepeat()
    {
        UIRoot root = new();
        UIElement target = new() { Focusable = true };
        root.VisualChildren.Add(target);
        int keyDownCount = 0;
        target.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, _) =>
        {
            keyDownCount++;
            root.VisualChildren.Remove(target);
        });
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        FocusManager manager = new();
        manager.Focus(target, map);
        KeyboardSnapshot down = KeyboardSnapshot.FromDownKeys([InputKey.Down]);

        manager.DispatchKeyboard(new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, down, down, [], [InputKey.Down, InputKey.Down]), map);

        Assert.False(target.IsAttached);
        Assert.Equal(1, keyDownCount);
    }

    // #953: hiding an ancestor removes the focused element from the input route, as hiding the element itself does.
    [Theory]
    [InlineData("fieldCollapsed")]
    [InlineData("panelCollapsed")]
    [InlineData("panelHidden")]
    public void RemainingKeysOfTheFrameAreNotRaisedAfterAHandlerHidesTheFocusedElementOrItsAncestor(string change)
    {
        UIRoot root = new();
        UIElement panel = new();
        UIElement field = new() { Focusable = true };
        panel.VisualChildren.Add(field);
        root.VisualChildren.Add(panel);
        int keyDownCount = 0;
        field.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, _) =>
        {
            keyDownCount++;
            Hide(change, panel, field);
        });
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        FocusManager manager = new();
        manager.Focus(field, map);
        KeyboardSnapshot down = KeyboardSnapshot.FromDownKeys([InputKey.Down]);

        manager.DispatchKeyboard(new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, down, down, [], [InputKey.Down, InputKey.Down]), map);

        Assert.True(keyDownCount == 1, $"row={change}: KeyDown raised {keyDownCount} times (expected 1).");
    }

    // #953: text input of the same frame uses the same focus-target check as the key events.
    [Theory]
    [InlineData("fieldCollapsed")]
    [InlineData("panelCollapsed")]
    [InlineData("panelHidden")]
    public void TextInputOfTheFrameIsNotRaisedAfterAKeyHandlerHidesTheFocusedElementOrItsAncestor(string change)
    {
        UIRoot root = new();
        UIElement panel = new();
        UIElement field = new() { Focusable = true };
        panel.VisualChildren.Add(field);
        root.VisualChildren.Add(panel);
        int textInputCount = 0;
        field.Handlers.AddHandler(InputEvents.TextInputEvent, (_, _) => textInputCount++);
        field.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, _) => Hide(change, panel, field));
        ElementInputBridge bridge = new();
        Assert.True(bridge.FocusManager.Focus(field, root.InputCache.EnsureCurrent(root)));
        KeyboardSnapshot down = KeyboardSnapshot.FromDownKeys([InputKey.Down]);

        bridge.Dispatch(root, new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, down, [new TextInputSnapshotEvent("x")]));

        Assert.True(textInputCount == 0, $"row={change}: TextInput raised {textInputCount} time(s) after the KeyDown handler hid the element (expected 0).");
    }

    private static void Hide(string change, UIElement panel, UIElement field)
    {
        switch (change)
        {
            case "fieldCollapsed": field.Visibility = Cerneala.UI.Layout.Visibility.Collapsed; break;
            case "panelCollapsed": panel.Visibility = Cerneala.UI.Layout.Visibility.Collapsed; break;
            case "panelHidden": panel.Visibility = Cerneala.UI.Layout.Visibility.Hidden; break;
        }
    }

    [Fact]
    public void LaterKeyOfSameFrameGoesToElementCreatedAndFocusedByEarlierKeyHandler()
    {
        UIRoot root = new();
        UIElement first = new() { Focusable = true };
        root.VisualChildren.Add(first);
        FocusManager manager = new();
        UIElement? created = null;
        List<string> receivers = [];
        first.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, args) =>
        {
            KeyEventArgs key = (KeyEventArgs)args;
            receivers.Add($"first {key.Key}");
            if (key.Key == InputKey.Back)
            {
                created = new UIElement { Focusable = true };
                created.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, e) => receivers.Add($"created {((KeyEventArgs)e).Key}"));
                root.VisualChildren.Add(created);
                Assert.True(manager.Focus(created, root.InputCache.EnsureCurrent(root)));
            }
        });
        ElementInputRouteMap frameMap = root.InputCache.EnsureCurrent(root);
        manager.Focus(first, frameMap);

        manager.DispatchKeyboard(
            new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.FromDownKeys([InputKey.Back, InputKey.Tab]), []),
            frameMap);

        Assert.Equal(["first Back", "created Tab"], receivers);
        Assert.Same(created, manager.FocusedElement);
    }

    // As WPF's Keyboard.Focus, which fails for an element whose IsVisible is false: an element whose ancestor was
    // collapsed or hidden after the supplied route map was built cannot take focus, although that map contains it (#1176).
    [Theory]
    [InlineData("panelCollapsed")]
    [InlineData("panelHidden")]
    public void FocusReturnsFalseForAnElementWhoseAncestorWasHiddenAfterTheRouteMapWasBuilt(string change)
    {
        UIRoot root = new();
        UIElement panel = new();
        UIElement field = new() { Focusable = true };
        panel.VisualChildren.Add(field);
        root.VisualChildren.Add(panel);
        ElementInputRouteMap frameMap = root.InputCache.EnsureCurrent(root);
        FocusManager manager = new();
        Hide(change, panel, field);

        bool focused = manager.Focus(field, frameMap);

        Assert.True(frameMap.Contains(field));
        Assert.True(
            !focused && manager.FocusedElement is null && !field.IsKeyboardFocused,
            $"row={change}: Focus returned {focused}, FocusedElement is {(manager.FocusedElement is null ? "null" : "the field")}, IsKeyboardFocused {field.IsKeyboardFocused}.");
    }

    // As WPF's KeyboardNavigation, which moves Tab focus only to visible elements: a tab stop whose ancestor a KeyDown
    // handler of the same frame hid is skipped, and the focus moves to the next visible tab stop (#1176).
    [Theory]
    [InlineData("none")]
    [InlineData("panelCollapsed")]
    [InlineData("panelHidden")]
    public void TabSkipsATabStopWhoseAncestorAKeyDownHandlerOfTheSameFrameHid(string change)
    {
        UIRoot root = new(200, 200);
        ButtonBase x = new();
        UIElement panel = new();
        ButtonBase y = new();
        ButtonBase z = new();
        panel.VisualChildren.Add(y);
        root.VisualChildren.Add(x);
        root.VisualChildren.Add(panel);
        root.VisualChildren.Add(z);
        x.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, _) => Hide(change, panel, y));
        ElementInputBridge bridge = new();
        Assert.True(bridge.FocusManager.Focus(x, root.InputCache.EnsureCurrent(root)));

        bridge.Dispatch(root, FrameWithKey(InputKey.Tab));

        UIElement expected = change == "none" ? y : z;
        string Name(UIElement? element) => element is null ? "null" : ReferenceEquals(element, x) ? "x" : ReferenceEquals(element, y) ? "y" : ReferenceEquals(element, z) ? "z" : "other";
        Assert.True(
            ReferenceEquals(bridge.FocusManager.FocusedElement, expected) && y.IsKeyboardFocused == (change == "none"),
            $"row={change}: focused after Tab = {Name(bridge.FocusManager.FocusedElement)} (expected {Name(expected)}), y.IsKeyboardFocused {y.IsKeyboardFocused}.");
    }

    // As WPF, where every RaiseEvent builds its route from the handlers registered at that moment: a handler that a
    // KeyDown handler removes or adds changes the next key event of the same frame (#1177).
    [Theory]
    [InlineData("remove", 1)]
    [InlineData("add", 1)]
    public void HandlerChangedByAKeyDownHandlerAppliesToTheNextKeyEventOfTheSameFrame(string change, int expectedCalls)
    {
        UIRoot root = new();
        UIElement panel = new();
        UIElement field = new() { Focusable = true };
        panel.VisualChildren.Add(field);
        root.VisualChildren.Add(panel);
        int calls = 0;
        RoutedEventHandler handler = (_, _) => calls++;
        if (change == "remove")
        {
            panel.Handlers.AddHandler(InputEvents.KeyDownEvent, handler);
        }

        bool changed = false;
        field.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, _) =>
        {
            if (changed)
            {
                return;
            }

            changed = true;
            if (change == "remove")
            {
                panel.Handlers.RemoveHandler(InputEvents.KeyDownEvent, handler);
            }
            else
            {
                panel.Handlers.AddHandler(InputEvents.KeyDownEvent, handler);
            }
        });
        ElementInputBridge bridge = new();
        Assert.True(bridge.FocusManager.Focus(field, root.InputCache.EnsureCurrent(root)));
        KeyboardSnapshot down = KeyboardSnapshot.FromDownKeys([InputKey.Down]);

        bridge.Dispatch(root, new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, down, down, [], [InputKey.Down, InputKey.Down]));

        Assert.True(calls == expectedCalls, $"row={change}: the panel handler ran {calls} time(s) (expected {expectedCalls}).");
    }

    // The TextInput of a frame comes after its key events, so a TextInput handler that a KeyDown handler removes or adds
    // applies to it (#1177).
    [Theory]
    [InlineData("remove", 0)]
    [InlineData("add", 1)]
    public void TextInputHandlerChangedByAKeyDownHandlerAppliesToTheTextInputOfTheSameFrame(string change, int expectedCalls)
    {
        UIRoot root = new();
        UIElement panel = new();
        UIElement field = new() { Focusable = true };
        panel.VisualChildren.Add(field);
        root.VisualChildren.Add(panel);
        int calls = 0;
        RoutedEventHandler handler = (_, _) => calls++;
        if (change == "remove")
        {
            panel.Handlers.AddHandler(InputEvents.TextInputEvent, handler);
        }

        field.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, _) =>
        {
            if (change == "remove")
            {
                panel.Handlers.RemoveHandler(InputEvents.TextInputEvent, handler);
            }
            else
            {
                panel.Handlers.AddHandler(InputEvents.TextInputEvent, handler);
            }
        });
        ElementInputBridge bridge = new();
        Assert.True(bridge.FocusManager.Focus(field, root.InputCache.EnsureCurrent(root)));

        bridge.Dispatch(root, new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.FromDownKeys([InputKey.A]), [new TextInputSnapshotEvent("a")]));

        Assert.True(calls == expectedCalls, $"row={change}: the panel TextInput handler ran {calls} time(s) (expected {expectedCalls}).");
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
