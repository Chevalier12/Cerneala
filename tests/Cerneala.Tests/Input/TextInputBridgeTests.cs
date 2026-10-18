using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class TextInputBridgeTests
{
    [Fact]
    public void FocusedElementReceivesTextInputPayload()
    {
        UIRoot root = new();
        UIElement target = new() { Focusable = true };
        root.VisualChildren.Add(target);
        List<string> text = [];
        target.Handlers.AddHandler(InputEvents.TextInputEvent, (_, args) => text.Add(((TextCompositionEventArgs)args).Text));
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        FocusManager focusManager = new();
        focusManager.Focus(target, map);

        new TextInputBridge().Dispatch([new TextInputSnapshotEvent("a")], focusManager, map);

        Assert.Equal(["a"], text);
    }

    [Fact]
    public void HandledPreviewTextInputSuppressesBubbleTextInput()
    {
        UIRoot root = new();
        UIElement target = new() { Focusable = true };
        root.VisualChildren.Add(target);
        bool bubbleCalled = false;
        target.Handlers.AddHandler(InputEvents.PreviewTextInputEvent, (_, args) => args.Handled = true);
        target.Handlers.AddHandler(InputEvents.TextInputEvent, (_, _) => bubbleCalled = true);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        FocusManager focusManager = new();
        focusManager.Focus(target, map);

        new TextInputBridge().Dispatch([new TextInputSnapshotEvent("a")], focusManager, map);

        Assert.False(bubbleCalled);
    }

    [Fact]
    public void EachTextInputEventGoesToElementFocusedWhenItIsRaised()
    {
        UIRoot root = new();
        UIElement a = new() { Focusable = true };
        UIElement b = new() { Focusable = true };
        root.VisualChildren.Add(a);
        root.VisualChildren.Add(b);
        FocusManager manager = new();
        ElementInputRouteMap map = null!;
        List<string> receivers = [];
        a.Handlers.AddHandler(InputEvents.TextInputEvent, (_, args) =>
        {
            receivers.Add("A:" + ((TextCompositionEventArgs)args).Text);
            manager.Focus(b, map);
        });
        b.Handlers.AddHandler(InputEvents.TextInputEvent, (_, args) => receivers.Add("B:" + ((TextCompositionEventArgs)args).Text));
        map = new ElementInputRouteBuilder().Build(root);
        manager.Focus(a, map);

        new TextInputBridge().Dispatch([new TextInputSnapshotEvent("x"), new TextInputSnapshotEvent("y")], manager, map);

        Assert.Equal("A:x,B:y", string.Join(",", receivers));
    }

    [Fact]
    public void TextInputIsNotRaisedOnFocusedElementDetachedByPreviousTextInput()
    {
        UIRoot root = new();
        UIElement target = new() { Focusable = true };
        root.VisualChildren.Add(target);
        List<string> text = [];
        target.Handlers.AddHandler(InputEvents.TextInputEvent, (_, args) =>
        {
            text.Add(((TextCompositionEventArgs)args).Text);
            root.VisualChildren.Remove(target);
        });
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        FocusManager focusManager = new();
        focusManager.Focus(target, map);

        new TextInputBridge().Dispatch([new TextInputSnapshotEvent("x"), new TextInputSnapshotEvent("y")], focusManager, map);

        Assert.Equal(["x"], text);
    }

    [Fact]
    public void TextInputIsNotRaisedOnFocusedElementDetachedByKeyHandlerOfSameFrame()
    {
        UIRoot root = new(360, 240);
        UIElement target = new() { Focusable = true };
        root.VisualChildren.Add(target);
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(target, root.InputCache.EnsureCurrent(root));
        List<string> text = [];
        target.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, args) =>
        {
            if (((KeyEventArgs)args).Key == InputKey.Enter)
            {
                root.VisualChildren.Remove(target);
            }
        });
        target.Handlers.AddHandler(InputEvents.TextInputEvent, (_, args) => text.Add(((TextCompositionEventArgs)args).Text));
        KeyboardSnapshot none = KeyboardSnapshot.FromDownKeys([]);
        KeyboardSnapshot enter = KeyboardSnapshot.FromDownKeys([InputKey.Enter]);

        bridge.Dispatch(root, new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, none, enter, [new TextInputSnapshotEvent("x")]));

        Assert.False(target.IsAttached);
        Assert.Empty(text);
    }

    [Fact]
    public void NoFocusedElementIgnoresTextInput()
    {
        UIRoot root = new();
        UIElement target = new();
        root.VisualChildren.Add(target);
        bool called = false;
        target.Handlers.AddHandler(InputEvents.TextInputEvent, (_, _) => called = true);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);

        new TextInputBridge().Dispatch([new TextInputSnapshotEvent("a")], new FocusManager(), map);

        Assert.False(called);
    }
}
