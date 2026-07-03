using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Input;

public sealed class ElementInputBridgeTests
{
    [Fact]
    public void MouseDownRaisesPreviewBeforeBubbleOnHitElement()
    {
        UIRoot root = RootWithChild(out UIElement child);
        List<string> calls = [];
        root.Handlers.AddHandler(InputEvents.PreviewMouseDownEvent, (_, _) => calls.Add("preview-root"));
        child.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => calls.Add("bubble-child"));

        new ElementInputBridge().Dispatch(root, PointerFrame(10, 10, pressed: true));

        Assert.Equal(["preview-root", "bubble-child"], calls);
    }

    [Fact]
    public void MouseMoveUpdatesHoverAndInvalidatesBeforeScheduler()
    {
        UIRoot root = RootWithChild(out UIElement child);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10));
        FrameStats stats = root.ProcessFrame();

        Assert.True(child.IsPointerOver);
        Assert.True(stats.RenderedElements > 0);
    }

    [Fact]
    public void StationaryPointerFrameDoesNotRaiseMouseMove()
    {
        UIRoot root = RootWithChild(out UIElement child);
        int moveCount = 0;
        child.Handlers.AddHandler(InputEvents.MouseMoveEvent, (_, _) => moveCount++);

        new ElementInputBridge().Dispatch(root, PointerFrame(10, 10));

        Assert.Equal(0, moveCount);
    }

    [Fact]
    public void PointerMoveRaisesMouseEventArgsWithoutButtonState()
    {
        UIRoot root = RootWithChild(out UIElement child);
        ElementInputBridge bridge = new();
        RoutedEventArgs? receivedArgs = null;
        child.Handlers.AddHandler(InputEvents.MouseMoveEvent, (_, args) => receivedArgs = args);

        bridge.Dispatch(root, PointerFrame(9, 10));
        bridge.Dispatch(root, PointerFrame(9, 10, 10, 10));

        MouseEventArgs mouseArgs = Assert.IsType<MouseEventArgs>(receivedArgs);
        Assert.Equal(10, mouseArgs.X);
        Assert.Equal(10, mouseArgs.Y);
    }

    [Fact]
    public void DisabledElementDoesNotReceiveHandlers()
    {
        UIRoot root = RootWithChild(out UIElement child);
        child.IsEnabled = false;
        bool called = false;
        child.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => called = true);

        new ElementInputBridge().Dispatch(root, PointerFrame(10, 10, pressed: true));

        Assert.False(called);
    }

    [Fact]
    public void CapturedButtonReleaseOutsideDoesNotExecuteCommand()
    {
        UIRoot root = new(100, 100);
        ButtonBase button = ArrangedButton(0, 0, 40, 40);
        UIElement other = HitTestServiceTests.Arranged(50, 0, 40, 40);
        root.VisualChildren.Add(button);
        root.VisualChildren.Add(other);
        bool executed = false;
        button.Command = new ActionCommand(_ => executed = true);
        ElementInputBridge bridge = new();
        ElementInputRouteMap routeMap = new ElementInputRouteBuilder().Build(root);
        bridge.PointerCaptureManager.Capture(button, routeMap);

        bridge.Dispatch(root, PointerFrame(10, 10, 10, 10, currentDown: true));
        bridge.Dispatch(root, PointerFrame(10, 10, 60, 10, previousDown: true));

        Assert.False(executed);
    }

    [Fact]
    public void HostUpdateDispatchesInputBeforeScheduler()
    {
        UIRoot root = RootWithChild(out UIElement child);
        UiHost host = new(new UiHostOptions { Root = root });

        UiFrame frame = host.Update(PointerFrame(10, 10), new UiViewport(100, 100), TimeSpan.Zero);

        Assert.True(child.IsPointerOver);
        Assert.True(frame.Stats.RenderedElements > 0);
    }

    private static UIRoot RootWithChild(out UIElement child)
    {
        UIRoot root = new(100, 100);
        child = HitTestServiceTests.Arranged(0, 0, 40, 40);
        root.VisualChildren.Add(child);
        return root;
    }

    private static ButtonBase ArrangedButton(float x, float y, float width, float height)
    {
        ButtonBase button = new();
        button.Arrange(new ArrangeContext(new LayoutRect(x, y, width, height)));
        return button;
    }

    private static InputFrame PointerFrame(float x, float y, bool pressed = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y);
        if (pressed)
        {
            current = current.WithButton(InputMouseButton.Left, true);
        }

        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
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
