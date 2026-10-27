using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Input;

public sealed class PointerCaptureManagerTests
{
    [Fact]
    public void CapturedElementOverridesHitTarget()
    {
        UIRoot root = new();
        UIElement captured = new();
        UIElement hit = new();
        root.VisualChildren.Add(captured);
        root.VisualChildren.Add(hit);
        ElementInputRouteMap map = root.InputCache.EnsureCurrent(root);
        Assert.True(map.Contains(hit));
        HitTestResult hitTarget = new(hit, 5, 6);
        PointerCaptureManager manager = new();

        manager.Capture(captured, map);
        HitTestResult? result = manager.OverrideTarget(hitTarget, map, 5, 6);

        Assert.Same(captured, result!.Element);
    }

    [Fact]
    public void OverrideTargetReleasesCaptureWhenCapturedElementIsNotRoutable()
    {
        UIRoot root = new();
        UIElement captured = new();
        UIElement hit = new();
        root.VisualChildren.Add(captured);
        root.VisualChildren.Add(hit);
        ElementInputRouteMap firstMap = root.InputCache.EnsureCurrent(root);
        PointerCaptureManager manager = new();
        manager.Capture(captured, firstMap);
        root.VisualChildren.Remove(captured);
        ElementInputRouteMap secondMap = root.InputCache.EnsureCurrent(root);
        Assert.True(secondMap.Contains(hit));
        HitTestResult hitTarget = new(hit, 5, 6);

        HitTestResult? result = manager.OverrideTarget(hitTarget, secondMap, 5, 6);

        Assert.Same(hit, result!.Element);
        Assert.False(manager.HasCapture);
    }

    // Clearing the capture because the captured element can no longer take input notifies it like Release does (#952).
    [Theory]
    [InlineData("detach")]
    [InlineData("hidden")]
    [InlineData("disabled")]
    public void OverrideTargetRaisesLostCaptureWhenItReleasesAnUnroutableCapture(string change)
    {
        UIRoot root = new();
        UIElement captured = new();
        UIElement hit = new();
        root.VisualChildren.Add(captured);
        root.VisualChildren.Add(hit);
        int lostCount = 0;
        captured.Handlers.AddHandler(InputEvents.LostMouseCaptureEvent, (_, _) => lostCount++);
        PointerCaptureManager manager = new();
        manager.Capture(captured, root.InputCache.EnsureCurrent(root));
        switch (change)
        {
            case "detach": root.VisualChildren.Remove(captured); break;
            case "hidden": captured.Visibility = Visibility.Hidden; break;
            case "disabled": captured.IsEnabled = false; break;
        }

        root.ProcessFrame();
        ElementInputRouteMap map = root.InputCache.EnsureCurrent(root);
        Assert.False(map.Contains(captured));

        manager.OverrideTarget(new HitTestResult(hit, 5, 6), map, 5, 6);
        manager.OverrideTarget(new HitTestResult(hit, 5, 6), map, 5, 6);

        Assert.False(manager.HasCapture);
        Assert.Equal(1, lostCount);
    }

    // A LostMouseCapture handler of the released element can capture another element; that element then takes the
    // pointer input, as a captured element overrides the hit target.
    [Fact]
    public void OverrideTargetReturnsTheElementThatALostCaptureHandlerCaptured()
    {
        UIRoot root = new();
        UIElement released = new();
        UIElement next = new();
        UIElement hit = new();
        root.VisualChildren.Add(released);
        root.VisualChildren.Add(next);
        root.VisualChildren.Add(hit);
        PointerCaptureManager manager = new();
        manager.Capture(released, root.InputCache.EnsureCurrent(root));
        released.Handlers.AddHandler(
            InputEvents.LostMouseCaptureEvent,
            (_, _) => manager.Capture(next, root.InputCache.EnsureCurrent(root)));
        released.Visibility = Visibility.Hidden;
        root.ProcessFrame();
        ElementInputRouteMap map = root.InputCache.EnsureCurrent(root);

        HitTestResult? result = manager.OverrideTarget(new HitTestResult(hit, 5, 6), map, 5, 6);

        Assert.True(
            ReferenceEquals(result?.Element, next) && ReferenceEquals(manager.CapturedElement, next),
            $"returned next: {ReferenceEquals(result?.Element, next)}, returned hit: {ReferenceEquals(result?.Element, hit)}, " +
            $"captured next: {ReferenceEquals(manager.CapturedElement, next)}");
    }

    [Fact]
    public void CaptureChangeRaisesLostAndGotCapture()
    {
        UIRoot root = new();
        UIElement first = new();
        UIElement second = new();
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        List<string> calls = [];
        first.Handlers.AddHandler(InputEvents.LostMouseCaptureEvent, (_, _) => calls.Add("lost-first"));
        second.Handlers.AddHandler(InputEvents.GotMouseCaptureEvent, (_, _) => calls.Add("got-second"));
        ElementInputRouteMap map = root.InputCache.EnsureCurrent(root);
        PointerCaptureManager manager = new();

        manager.Capture(first, map);
        manager.Capture(second, map);

        Assert.Contains("lost-first", calls);
        Assert.Contains("got-second", calls);
    }
}
