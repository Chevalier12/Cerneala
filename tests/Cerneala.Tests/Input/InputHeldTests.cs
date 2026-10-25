using System.Numerics;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class InputHeldTests
{
    private static readonly KeyboardSnapshot NoKeys = KeyboardSnapshot.Empty;
    private static readonly KeyboardSnapshot HeldD = KeyboardSnapshot.FromDownKeys([InputKey.D]);

    [Fact]
    public void HeldKeyRaisesPreviewThenBubbleOnTheFocusedElementEveryUpdateUntilReleased()
    {
        (UIRoot root, UIElement target, ElementInputBridge bridge) = FocusedTarget();
        List<string> calls = [];
        target.PreviewInputHeld += (sender, args) => calls.Add(Describe("preview", sender, target, args));
        target.InputHeld += (sender, args) => calls.Add(Describe("bubble", sender, target, args));

        bridge.Dispatch(root, KeyFrame(NoKeys, HeldD));
        bridge.Dispatch(root, KeyFrame(HeldD, HeldD));
        bridge.Dispatch(root, KeyFrame(HeldD, HeldD));

        string[] expected = ["preview:self:D=True:A=False", "bubble:self:D=True:A=False"];
        Assert.Equal([.. expected, .. expected, .. expected], calls);

        calls.Clear();
        bridge.Dispatch(root, KeyFrame(HeldD, NoKeys));
        bridge.Dispatch(root, KeyFrame(NoKeys, NoKeys));

        Assert.Empty(calls);
    }

    [Fact]
    public void FirstEventOfAHoldHasZeroElapsedAndLaterEventsCarryTheFrameElapsed()
    {
        (UIRoot root, UIElement target, ElementInputBridge bridge) = FocusedTarget();
        List<TimeSpan> elapsed = [];
        target.InputHeld += (_, args) => elapsed.Add(args.Elapsed);

        bridge.Dispatch(root, KeyFrame(NoKeys, HeldD), TimeSpan.FromMilliseconds(16));
        bridge.Dispatch(root, KeyFrame(HeldD, HeldD), TimeSpan.FromMilliseconds(17));
        bridge.Dispatch(root, KeyFrame(HeldD, HeldD), TimeSpan.FromMilliseconds(33));
        bridge.Dispatch(root, KeyFrame(HeldD, NoKeys), TimeSpan.FromMilliseconds(16));
        bridge.Dispatch(root, KeyFrame(NoKeys, HeldD), TimeSpan.FromMilliseconds(20));
        bridge.Dispatch(root, KeyFrame(HeldD, HeldD), TimeSpan.FromMilliseconds(21));

        Assert.Equal(
            [
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(17),
                TimeSpan.FromMilliseconds(33),
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(21)
            ],
            elapsed);
    }

    [Fact]
    public void KeyDownIsRaisedBeforeInputHeldOnThePressFrame()
    {
        (UIRoot root, UIElement target, ElementInputBridge bridge) = FocusedTarget();
        List<string> calls = [];
        target.PreviewKeyDown += (_, _) => calls.Add("preview-key-down");
        target.KeyDown += (_, _) => calls.Add("key-down");
        target.PreviewInputHeld += (_, _) => calls.Add("preview-input-held");
        target.InputHeld += (_, _) => calls.Add("input-held");

        bridge.Dispatch(root, KeyFrame(NoKeys, HeldD));

        Assert.Equal(["preview-key-down", "key-down", "preview-input-held", "input-held"], calls);
    }

    [Fact]
    public void WithoutAFocusedElementTheRootContentReceivesInputHeld()
    {
        UIRoot root = new(100, 100);
        UIElement child = new() { Focusable = true };
        UIElement secondChild = new();
        root.VisualChildren.Add(child);
        root.VisualChildren.Add(secondChild);
        List<string> calls = [];
        child.InputHeld += (sender, args) =>
            calls.Add(ReferenceEquals(sender, child) && ReferenceEquals(args.OriginalSource, child) ? "child" : "wrong-child");
        secondChild.InputHeld += (_, _) => Assert.Fail("Only the first visual child of the root is the fallback target.");
        root.InputHeld += (sender, args) =>
            calls.Add(ReferenceEquals(sender, root) && ReferenceEquals(args.OriginalSource, child) ? "root" : "wrong-root");

        new ElementInputBridge().Dispatch(root, KeyFrame(NoKeys, HeldD));

        Assert.Equal(["child", "root"], calls);
    }

    [Fact]
    public void WithoutAFocusedElementAndNoRootContentTheRootReceivesInputHeld()
    {
        UIRoot root = new(100, 100);
        List<object> senders = [];
        List<object?> originalSources = [];
        root.InputHeld += (sender, args) =>
        {
            senders.Add(sender);
            originalSources.Add(args.OriginalSource);
        };

        new ElementInputBridge().Dispatch(root, KeyFrame(NoKeys, HeldD));

        Assert.Same(root, Assert.Single(senders));
        Assert.Same(root, Assert.Single(originalSources));
    }

    [Fact]
    public void HoldingOnlyTheLeftMouseButtonRaisesInputHeldWithThePointerPosition()
    {
        UIRoot root = new(100, 100);
        UIElement child = HitTestServiceTests.Arranged(0, 0, 40, 40);
        root.VisualChildren.Add(child);
        List<InputStateEventArgs> received = [];
        root.InputHeld += (_, args) => received.Add(args);
        PointerSnapshot up = PointerSnapshot.Empty.WithPosition(10, 20);
        PointerSnapshot down = up.WithButton(InputMouseButton.Left, true);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, new InputFrame(up, down, NoKeys, NoKeys, []));
        bridge.Dispatch(root, new InputFrame(down, down, NoKeys, NoKeys, []));

        Assert.Equal(2, received.Count);
        Assert.All(received, args =>
        {
            Assert.True(args.IsDown(InputMouseButton.Left));
            Assert.False(args.IsDown(InputMouseButton.Right));
            Assert.False(args.IsDown(InputKey.D));
            Assert.Equal(new Vector2(10, 20), args.PointerPosition);
        });
    }

    [Fact]
    public void HandledPreviewInputHeldSuppressesInputHeld()
    {
        (UIRoot root, UIElement target, ElementInputBridge bridge) = FocusedTarget();
        int bubbleCalls = 0;
        int handledTooCalls = 0;
        target.PreviewInputHeld += (_, args) => args.Handled = true;
        target.InputHeld += (_, _) => bubbleCalls++;
        target.AddHandler(InputEvents.InputHeldEvent, (_, _) => handledTooCalls++, handledEventsToo: true);

        bridge.Dispatch(root, KeyFrame(NoKeys, HeldD));

        // Like KeyDown: a handler registered with handledEventsToo still sees the already handled bubble.
        Assert.Equal(0, bubbleCalls);
        Assert.Equal(1, handledTooCalls);
    }

    [Fact]
    public void InputHeldBubblesFromTheFocusedElementToItsAncestors()
    {
        UIRoot root = new(100, 100);
        UIElement parent = new();
        UIElement target = new() { Focusable = true };
        parent.VisualChildren.Add(target);
        root.VisualChildren.Add(parent);
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(target, root.InputCache.EnsureCurrent(root));
        List<string> calls = [];
        root.PreviewInputHeld += (_, _) => calls.Add("preview-root");
        parent.InputHeld += (sender, args) => calls.Add(ReferenceEquals(args.OriginalSource, target) && ReferenceEquals(sender, parent) ? "bubble-parent" : "wrong");

        bridge.Dispatch(root, KeyFrame(NoKeys, HeldD));

        Assert.Equal(["preview-root", "bubble-parent"], calls);
    }

    [Fact]
    public void NothingHeldRaisesNoInputHeld()
    {
        (UIRoot root, UIElement target, ElementInputBridge bridge) = FocusedTarget();
        int calls = 0;
        target.PreviewInputHeld += (_, _) => calls++;
        target.InputHeld += (_, _) => calls++;

        bridge.Dispatch(root, KeyFrame(NoKeys, NoKeys));

        Assert.Equal(0, calls);
    }

    [Fact]
    public void HasActiveInputHoldIsTrueOnlyWhileInputIsHeldAndAHandlerExists()
    {
        (UIRoot root, UIElement target, ElementInputBridge bridge) = FocusedTarget();

        bridge.Dispatch(root, KeyFrame(NoKeys, HeldD));
        Assert.False(bridge.HasActiveInputHold);

        RoutedEventHandler<InputStateEventArgs> handler = (_, _) => { };
        target.InputHeld += handler;
        bridge.Dispatch(root, KeyFrame(HeldD, HeldD));
        Assert.True(bridge.HasActiveInputHold);

        bridge.Dispatch(root, KeyFrame(HeldD, NoKeys));
        Assert.False(bridge.HasActiveInputHold);

        bridge.Dispatch(root, KeyFrame(NoKeys, HeldD));
        Assert.True(bridge.HasActiveInputHold);

        target.InputHeld -= handler;
        RoutedEventHandler<InputStateEventArgs> previewHandler = (_, _) => { };
        target.PreviewInputHeld += previewHandler;
        bridge.Dispatch(root, KeyFrame(HeldD, HeldD));
        Assert.True(bridge.HasActiveInputHold);

        target.PreviewInputHeld -= previewHandler;
        bridge.Dispatch(root, KeyFrame(HeldD, HeldD));
        Assert.False(bridge.HasActiveInputHold);
    }

    [Fact]
    public void HasActiveInputHoldIsFalseAfterTheLastHandlerIsRemovedWhileHeld()
    {
        (UIRoot root, UIElement target, ElementInputBridge bridge) = FocusedTarget();
        RoutedEventHandler<InputStateEventArgs> handler = (_, _) => { };
        target.InputHeld += handler;
        bridge.Dispatch(root, KeyFrame(NoKeys, HeldD));
        Assert.True(bridge.HasActiveInputHold);

        target.InputHeld -= handler;
        bridge.Dispatch(root, KeyFrame(HeldD, HeldD));

        Assert.False(bridge.HasActiveInputHold);
    }

    private static (UIRoot Root, UIElement Target, ElementInputBridge Bridge) FocusedTarget()
    {
        UIRoot root = new(100, 100);
        UIElement target = new() { Focusable = true };
        root.VisualChildren.Add(target);
        ElementInputBridge bridge = new();
        bridge.FocusManager.Focus(target, root.InputCache.EnsureCurrent(root));
        Assert.Same(target, bridge.FocusManager.FocusedElement);
        return (root, target, bridge);
    }

    private static InputFrame KeyFrame(KeyboardSnapshot previous, KeyboardSnapshot current) =>
        new(PointerSnapshot.Empty, PointerSnapshot.Empty, previous, current, []);

    private static string Describe(string phase, object sender, UIElement expectedSender, InputStateEventArgs args) =>
        $"{phase}:{(ReferenceEquals(sender, expectedSender) ? "self" : "other")}:D={args.IsDown(InputKey.D)}:A={args.IsDown(InputKey.A)}";
}
