using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Invalidation;

public sealed class DetachedQueuedElementTests
{
    [Fact]
    public void DetachedMeasureQueuedElementIsNotMeasured()
    {
        UIRoot root = RootWithChild(out UIElement child);
        child.Invalidate(InvalidationFlags.Measure, "queued");
        root.VisualChildren.Remove(child);

        Assert.DoesNotContain(child, root.LayoutQueue.SnapshotMeasure());
    }

    [Fact]
    public void DetachedRenderQueuedElementDoesNotRebuildRenderCache()
    {
        UIRoot root = RootWithChild(out UIElement child);
        child.Invalidate(InvalidationFlags.Render, "queued");
        root.VisualChildren.Remove(child);

        Assert.DoesNotContain(child, root.RenderQueue.Snapshot());
    }

    [Fact]
    public void DetachedAspectQueuedElementIsNotAspected()
    {
        UIRoot root = RootWithChild(out UIElement child);
        child.Invalidate(InvalidationFlags.Aspect, "queued");
        root.VisualChildren.Remove(child);

        Assert.DoesNotContain(child, root.AspectQueue.Snapshot());
    }

    [Fact]
    public void DetachedCommandStateQueuedElementIsNotRefreshed()
    {
        UIRoot root = new();
        ButtonBase button = new();
        root.VisualChildren.Add(button);
        root.ProcessFrame();
        button.QueueCommandStateRefresh();
        root.VisualChildren.Remove(button);

        Assert.DoesNotContain(button, root.CommandStateQueue.Snapshot());
    }

    [Fact]
    public void DetachedHitTestQueuedElementDoesNotRebuildRouteEntry()
    {
        UIRoot root = RootWithChild(out UIElement child);
        child.Invalidate(InvalidationFlags.HitTest, "queued");
        root.VisualChildren.Remove(child);

        Assert.DoesNotContain(child, root.HitTestQueue.Snapshot());
        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        Assert.False(routeMap.TryGetId(child, out _));
    }

    private static UIRoot RootWithChild(out UIElement child)
    {
        UIRoot root = new();
        child = new UIElement();
        root.VisualChildren.Add(child);
        root.ProcessFrame();
        return root;
    }
}
