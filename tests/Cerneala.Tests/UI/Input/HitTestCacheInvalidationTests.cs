using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Input;

public sealed class HitTestCacheInvalidationTests
{
    [Fact]
    public void VisualTreeMutationInvalidatesInputCache()
    {
        UIRoot root = new(100, 100);
        UIElement first = Arranged(0, 0, 40, 40);
        root.VisualChildren.Add(first);
        root.ProcessFrame();
        root.InputCache.EnsureCurrent(root);
        int rebuildsAfterInitialBuild = root.InputCache.RebuildCount;

        UIElement second = Arranged(40, 0, 40, 40);
        root.VisualChildren.Add(second);
        root.InputCache.EnsureCurrent(root);

        Assert.Equal(rebuildsAfterInitialBuild + 1, root.InputCache.RebuildCount);
        Assert.True(root.InputCache.RouteMap.TryGetId(second, out _));
    }

    [Fact]
    public void LayoutBoundsChangeInvalidatesHitTestResult()
    {
        UIRoot root = new(100, 100);
        UIElement child = Arranged(0, 0, 20, 20);
        root.VisualChildren.Add(child);
        root.ProcessFrame();
        root.InputCache.EnsureCurrent(root);

        child.Arrange(new ArrangeContext(new LayoutRect(50, 0, 20, 20)));
        child.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.HitTest, "manual bounds change");

        HitTestResult? oldPoint = root.InputCache.HitTest(root, 10, 10);
        HitTestResult? newPoint = root.InputCache.HitTest(root, 55, 10);

        Assert.NotSame(child, oldPoint?.Element);
        Assert.Same(child, newPoint!.Element);
    }

    [Fact]
    public void DisabledAndInvisibleElementsAreRemovedFromRetainedRouteMap()
    {
        UIRoot root = new(100, 100);
        UIElement disabled = Arranged(0, 0, 40, 40);
        UIElement invisible = Arranged(40, 0, 40, 40);
        root.VisualChildren.Add(disabled);
        root.VisualChildren.Add(invisible);
        root.ProcessFrame();
        root.InputCache.EnsureCurrent(root);

        disabled.IsEnabled = false;
        invisible.IsVisible = false;
        root.InputCache.EnsureCurrent(root);

        Assert.False(root.InputCache.RouteMap.TryGetId(disabled, out _));
        Assert.False(root.InputCache.RouteMap.TryGetId(invisible, out _));
    }

    [Fact]
    public void RemovedCapturedElementIsReleasedWhenRouteMapRebuilds()
    {
        UIRoot root = new(100, 100);
        UIElement captured = Arranged(0, 0, 40, 40);
        UIElement fallback = Arranged(50, 0, 40, 40);
        root.VisualChildren.Add(captured);
        root.VisualChildren.Add(fallback);
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        root.InputCache.EnsureCurrent(root);
        bridge.PointerCaptureManager.Capture(captured, root.InputCache.RouteMap);

        root.VisualChildren.Remove(captured);
        bridge.Dispatch(root, PointerFrame(60, 10));

        Assert.False(bridge.PointerCaptureManager.HasCapture);
    }

    private static UIElement Arranged(float x, float y, float width, float height)
    {
        UIElement element = new();
        element.Arrange(new ArrangeContext(new LayoutRect(x, y, width, height)));
        return element;
    }

    private static InputFrame PointerFrame(float x, float y)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y);
        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }
}
