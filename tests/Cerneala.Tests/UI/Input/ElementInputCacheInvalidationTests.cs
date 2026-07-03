using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Input;

public sealed class ElementInputCacheInvalidationTests
{
    [Fact]
    public void InputDispatchReusesRouteMapWhenNothingChanged()
    {
        UIRoot root = RootWithChild(out UIElement child);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10));
        int rebuildsAfterFirstDispatch = root.InputCache.RebuildCount;
        ElementInputRouteMap firstMap = root.InputCache.RouteMap;

        Assert.Equal(1, rebuildsAfterFirstDispatch);

        bridge.Dispatch(root, PointerFrame(11, 10));

        Assert.Same(firstMap, root.InputCache.RouteMap);
        Assert.Equal(rebuildsAfterFirstDispatch, root.InputCache.RebuildCount);
        Assert.True(child.IsPointerOver);
    }

    [Fact]
    public void HitTestInvalidationRebuildsRouteMapOnce()
    {
        UIRoot root = RootWithChild(out UIElement child);
        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(10, 10));
        int rebuildsAfterFirstDispatch = root.InputCache.RebuildCount;

        child.Visibility = Visibility.Collapsed;
        bridge.Dispatch(root, PointerFrame(10, 10));

        Assert.Equal(rebuildsAfterFirstDispatch + 1, root.InputCache.RebuildCount);
        Assert.False(root.InputCache.RouteMap.TryGetId(child, out _));
    }

    [Fact]
    public void HandlerAddedAfterCacheBuildInvalidatesRouteMap()
    {
        UIRoot root = RootWithChild(out UIElement child);
        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(10, 10));
        int rebuildsAfterFirstDispatch = root.InputCache.RebuildCount;
        bool called = false;

        child.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => called = true);
        bridge.Dispatch(root, PointerFrame(10, 10, pressed: true));

        Assert.True(called);
        Assert.Equal(rebuildsAfterFirstDispatch + 1, root.InputCache.RebuildCount);
    }

    [Fact]
    public void HitTestPhaseRebuildsDirtyInputCacheBeforeNoWorkFrames()
    {
        UIRoot root = RootWithChildAfterFrame(out UIElement child);
        root.InputCache.EnsureCurrent(root);
        int rebuildsAfterInitialBuild = root.InputCache.RebuildCount;

        child.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.HitTest, "route changed");
        Cerneala.UI.Invalidation.FrameStats stats = root.ProcessFrame();

        Assert.Equal(rebuildsAfterInitialBuild + 1, root.InputCache.RebuildCount);
        Assert.True(stats.HitTestElements > 0);
        Assert.False(root.Scheduler.HasWork);
    }

    private static UIRoot RootWithChild(out UIElement child)
    {
        UIRoot root = new(100, 100);
        child = Arranged(0, 0, 40, 40);
        root.VisualChildren.Add(child);
        return root;
    }

    private static UIRoot RootWithChildAfterFrame(out UIElement child)
    {
        UIRoot root = RootWithChild(out child);
        root.ProcessFrame();
        return root;
    }

    private static UIElement Arranged(float x, float y, float width, float height)
    {
        UIElement element = new();
        element.Arrange(new ArrangeContext(new LayoutRect(x, y, width, height)));
        return element;
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
}
