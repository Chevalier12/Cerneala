using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Input;

public sealed class TouchInputBridgeTests
{
    [Fact]
    public void TouchDownRoutesTypedPreviewAndBubbleEventsToHitTarget()
    {
        (UIRoot root, UIElement target) = CreateRootWithTarget();
        List<string> calls = [];
        target.Handlers.AddHandler(InputEvents.PreviewTouchDownEvent, (_, args) =>
        {
            TouchEventArgs touch = Assert.IsType<TouchEventArgs>(args);
            calls.Add($"preview:{touch.TouchId}:{touch.X}:{touch.Y}:{touch.Action}");
        });
        target.Handlers.AddHandler(InputEvents.TouchDownEvent, (_, args) =>
        {
            TouchEventArgs touch = Assert.IsType<TouchEventArgs>(args);
            calls.Add($"bubble:{touch.TouchId}:{touch.X}:{touch.Y}:{touch.Action}");
        });

        new TouchInputBridge().Dispatch(root, new TouchInputFrame(new TouchInputPoint(7, 10, 12, TouchInputAction.Down)));

        Assert.Equal(["preview:7:10:12:Down", "bubble:7:10:12:Down"], calls);
    }

    [Fact]
    public void CapturedTouchMoveRoutesToCapturedTarget()
    {
        UIRoot root = new(100, 100);
        UIElement captured = Arranged(0, 0, 20, 20);
        UIElement hit = Arranged(40, 0, 20, 20);
        root.VisualChildren.Add(captured);
        root.VisualChildren.Add(hit);
        List<object> sources = [];
        captured.Handlers.AddHandler(InputEvents.TouchMoveEvent, (_, args) => sources.Add(args.OriginalSource));
        TouchInputBridge bridge = new();
        ElementInputRouteMap map = root.InputCache.EnsureCurrent(root);

        bridge.Capture(3, captured, map);
        bridge.Dispatch(root, new TouchInputFrame(new TouchInputPoint(3, 45, 5, TouchInputAction.Move)));

        UiElementId capturedId = Assert.Single(sources).ShouldBeId();
        Assert.True(map.TryGetId(captured, out UiElementId expected));
        Assert.Equal(expected, capturedId);
    }

    [Fact]
    public void CaptureChangeRaisesLostAndGotTouchCapture()
    {
        UIRoot root = new(100, 100);
        UIElement first = Arranged(0, 0, 20, 20);
        UIElement second = Arranged(40, 0, 20, 20);
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        List<string> calls = [];
        first.Handlers.AddHandler(InputEvents.LostTouchCaptureEvent, (_, _) => calls.Add("lost-first"));
        second.Handlers.AddHandler(InputEvents.GotTouchCaptureEvent, (_, _) => calls.Add("got-second"));
        ElementInputRouteMap map = root.InputCache.EnsureCurrent(root);
        TouchInputBridge bridge = new();

        bridge.Capture(3, first, map);
        bridge.Capture(3, second, map);

        Assert.Contains("lost-first", calls);
        Assert.Contains("got-second", calls);
    }

    private static (UIRoot Root, UIElement Target) CreateRootWithTarget()
    {
        UIRoot root = new(100, 100);
        UIElement target = Arranged(0, 0, 50, 50);
        root.VisualChildren.Add(target);
        return (root, target);
    }

    private static UIElement Arranged(float x, float y, float width, float height)
    {
        UIElement element = new();
        element.Arrange(new ArrangeContext(new LayoutRect(x, y, width, height)));
        return element;
    }
}

internal static class TouchInputBridgeTestExtensions
{
    public static UiElementId ShouldBeId(this object source)
    {
        return Assert.IsType<UiElementId>(source);
    }
}
