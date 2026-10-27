using Cerneala.Drawing;
using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Canvas = Cerneala.UI.Layout.Panels.Canvas;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Presence;
using Cerneala.UI.Motion.Specs;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Invalidation;

public sealed class RenderQueueTests
{
    [Fact]
    public void RenderQueueDeduplicatesByReference()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        ClearInitialMutationWork(root);

        root.RenderQueue.Enqueue(child);
        root.RenderQueue.Enqueue(child);

        Assert.Equal(1, root.RenderQueue.Count);
    }

    [Fact]
    public void RenderOnlyInvalidationDoesNotScheduleMeasure()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        ClearInitialMutationWork(root);

        child.Invalidate(InvalidationFlags.Render, "render");

        Assert.Equal(0, root.LayoutQueue.MeasureCount);
        Assert.Equal(1, root.RenderQueue.Count);
    }

    [Fact]
    public void RenderOrderIsDeterministic()
    {
        UIRoot root = new();
        UIElement first = new();
        UIElement second = new();
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        ClearInitialMutationWork(root);

        root.RenderQueue.Enqueue(second);
        root.RenderQueue.Enqueue(first);

        Assert.Equal([first, second], root.RenderQueue.Snapshot());
    }

    [Fact]
    public void DetachedElementsAreRemovedFromRenderWork()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        ClearInitialMutationWork(root);

        root.RenderQueue.Enqueue(child);
        root.VisualChildren.Remove(child);

        Assert.DoesNotContain(child, root.RenderQueue.Snapshot());
        Assert.Contains(root, root.RenderQueue.Snapshot());
        Assert.Equal(1, root.RenderQueue.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RenderChangeOfAnExitingElementStaysQueuedAndRendersWithoutThrowing(bool excludeInputWhileExiting)
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        Canvas parent = new();
        MotionSpec<float> spec = MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100));
        Rectangle child = new()
        {
            Fill = new SolidColorBrush(Color.White),
            Geometry = new RectangleGeometry(new DrawRect(0, 0, 20, 20)),
            Presence = PresenceOptions.FadeAndScale(spec, spec, excludeInputWhileExiting)
        };
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        RenderFrame(root);
        clock.Advance(TimeSpan.FromMilliseconds(100));
        RenderFrame(root);
        parent.VisualChildren.Remove(child);
        RenderFrame(root);
        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(child));

        child.Fill = new SolidColorBrush(Color.Red);
        bool queued = root.RenderQueue.Snapshot().Contains(child);
        Exception? failure = Record.Exception(() => RenderFrame(root));

        Assert.True(
            queued && failure is null,
            "excludeInputWhileExiting=" + excludeInputWhileExiting + ": queued=" + queued +
            "; next frame exception=" + (failure is null ? "none" : failure.GetType().Name + ": " + failure.Message));
    }

    [Fact]
    public void RenderChangeOfADescendantOfAnExitingElementRendersWithoutThrowing()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        Canvas parent = new();
        MotionSpec<float> spec = MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100));
        Rectangle inner = new()
        {
            Fill = new SolidColorBrush(Color.White),
            Geometry = new RectangleGeometry(new DrawRect(0, 0, 20, 20))
        };
        Border exiting = new() { Presence = PresenceOptions.FadeAndScale(spec, spec), Child = inner };
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(exiting);
        RenderFrame(root);
        clock.Advance(TimeSpan.FromMilliseconds(100));
        RenderFrame(root);
        parent.VisualChildren.Remove(exiting);
        RenderFrame(root);
        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(exiting));

        inner.Fill = new SolidColorBrush(Color.Red);
        bool queued = root.RenderQueue.Snapshot().Contains(inner);
        Exception? failure = Record.Exception(() => RenderFrame(root));

        Assert.True(
            queued && failure is null,
            "descendant queued=" + queued + "; next frame exception=" + (failure is null ? "none" : failure.GetType().Name + ": " + failure.Message));
    }

    [Fact]
    public void DescendantOfAnExitingElementIsRemovedFromRenderWorkAfterTheExitEnds()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        Canvas parent = new();
        MotionSpec<float> spec = MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100));
        Rectangle inner = new()
        {
            Fill = new SolidColorBrush(Color.White),
            Geometry = new RectangleGeometry(new DrawRect(0, 0, 20, 20))
        };
        Border exiting = new() { Presence = PresenceOptions.FadeAndScale(spec, spec), Child = inner };
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(exiting);
        RenderFrame(root);
        clock.Advance(TimeSpan.FromMilliseconds(100));
        RenderFrame(root);
        parent.VisualChildren.Remove(exiting);
        RenderFrame(root);
        inner.Fill = new SolidColorBrush(Color.Red);

        clock.Advance(TimeSpan.FromMilliseconds(120));
        RenderFrame(root);

        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(exiting));
        Assert.DoesNotContain(inner, root.RenderQueue.Snapshot());
    }

    private static void RenderFrame(UIRoot root)
    {
        root.ProcessFrame();
        root.RetainedRenderer.Commit(root);
    }

    private static void ClearInitialMutationWork(UIRoot root)
    {
        root.ProcessFrame();
    }
}
