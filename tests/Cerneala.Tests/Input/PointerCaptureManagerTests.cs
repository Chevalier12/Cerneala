using Cerneala.UI.Elements;
using Cerneala.UI.Input;

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
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        Assert.True(map.TryGetId(hit, out UiElementId hitId));
        HitTestResult hitTarget = new(hit, hitId, 5, 6);
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
        ElementInputRouteMap firstMap = new ElementInputRouteBuilder().Build(root);
        PointerCaptureManager manager = new();
        manager.Capture(captured, firstMap);
        root.VisualChildren.Remove(captured);
        ElementInputRouteMap secondMap = new ElementInputRouteBuilder().Build(root);
        Assert.True(secondMap.TryGetId(hit, out UiElementId hitId));
        HitTestResult hitTarget = new(hit, hitId, 5, 6);

        HitTestResult? result = manager.OverrideTarget(hitTarget, secondMap, 5, 6);

        Assert.Same(hit, result!.Element);
        Assert.False(manager.HasCapture);
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
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        PointerCaptureManager manager = new();

        manager.Capture(first, map);
        manager.Capture(second, map);

        Assert.Contains("lost-first", calls);
        Assert.Contains("got-second", calls);
    }
}
