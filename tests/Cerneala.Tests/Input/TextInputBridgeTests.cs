using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class TextInputBridgeTests
{
    [Fact]
    public void FocusedElementReceivesTextInputPayload()
    {
        UIRoot root = new();
        UIElement target = new();
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
        UIElement target = new();
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
