using Cerneala.UI.Detective;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.Input;

public sealed class HoverTrackerTests
{
    [Fact]
    public void HoverEnterAndLeaveUpdatePointerOverAndRaiseEvents()
    {
        UIRoot root = new();
        UIElement first = new();
        UIElement second = new();
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        List<string> calls = [];
        first.Handlers.AddHandler(InputEvents.MouseEnterEvent, (_, _) => calls.Add("enter-first"));
        first.Handlers.AddHandler(InputEvents.MouseLeaveEvent, (_, _) => calls.Add("leave-first"));
        second.Handlers.AddHandler(InputEvents.MouseEnterEvent, (_, _) => calls.Add("enter-second"));
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        Assert.True(map.Contains(first));
        Assert.True(map.Contains(second));
        HoverTracker tracker = new();

        tracker.Update(new HitTestResult(first, 1, 1), map);
        tracker.Update(new HitTestResult(second, 2, 2), map);

        Assert.False(first.IsPointerOver);
        Assert.True(second.IsPointerOver);
        Assert.Equal(["enter-first", "leave-first", "enter-second"], calls);
    }

    [Fact]
    public void SameHoverTargetDoesNotReapplyState()
    {
        UIRoot root = new();
        UIElement target = new();
        root.VisualChildren.Add(target);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        Assert.True(map.Contains(target));
        HoverTracker tracker = new();
        HitTestResult result = new(target, 1, 1);

        Assert.True(tracker.Update(result, map));
        Assert.False(tracker.Update(result, map));
    }

    [Fact]
    public void HoverLeaveToNoTargetUsesCurrentPointerCoordinates()
    {
        UIRoot root = new();
        UIElement target = new();
        root.VisualChildren.Add(target);
        MouseEventArgs? leaveArgs = null;
        target.Handlers.AddHandler(InputEvents.MouseLeaveEvent, (_, args) => leaveArgs = (MouseEventArgs)args);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        Assert.True(map.Contains(target));
        HoverTracker tracker = new();

        tracker.Update(new HitTestResult(target, 1, 1), map);
        tracker.Update(null, map, 9, 10);

        Assert.NotNull(leaveArgs);
        Assert.Equal(9, leaveArgs.X);
        Assert.Equal(10, leaveArgs.Y);
    }

    [Fact]
    public void HoverStateChangesInvalidateInputVisuals()
    {
        UIRoot root = new(invalidationTrace: new InvalidationTrace());
        UIElement target = new();
        root.VisualChildren.Add(target);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        Assert.True(map.Contains(target));
        HoverTracker tracker = new();

        tracker.Update(new HitTestResult(target, 1, 1), map);

        Assert.Contains(root.Detective.Invalidation.Entries, entry =>
            ReferenceEquals(entry.Element, target) &&
            ReferenceEquals(entry.SourceProperty, UIElement.IsPointerOverProperty) &&
            entry.Flags.HasFlag(InvalidationFlags.InputVisual));
    }
}
