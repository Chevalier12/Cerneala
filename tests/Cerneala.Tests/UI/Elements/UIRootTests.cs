using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Elements;

public sealed class UIRootTests
{
    [Fact]
    public void RootIsAttachedOnCreation()
    {
        UIRoot root = new(800, 600, 2);

        Assert.True(root.IsAttached);
        Assert.Same(root, root.Root);
        Assert.Equal(800, root.ViewportWidth);
        Assert.Equal(600, root.ViewportHeight);
        Assert.Equal(2, root.Scale);
    }

    [Fact]
    public void AddingChildAttachesSubtree()
    {
        UIRoot root = new();
        UIElement parent = new();
        UIElement child = new();
        parent.VisualChildren.Add(child);

        root.VisualChildren.Add(parent);

        Assert.True(parent.IsAttached);
        Assert.True(child.IsAttached);
        Assert.Same(root, parent.Root);
        Assert.Same(root, child.Root);
    }

    [Fact]
    public void RemovingChildDetachesSubtree()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);

        bool removed = root.VisualChildren.Remove(child);

        Assert.True(removed);
        Assert.False(child.IsAttached);
        Assert.Null(child.Root);
    }

    [Fact]
    public void TreeVersionChangesOnTreeMutation()
    {
        UIRoot root = new();
        int initialVersion = root.TreeVersion;
        UIElement child = new();

        root.VisualChildren.Add(child);
        int afterAdd = root.TreeVersion;
        root.VisualChildren.Remove(child);

        Assert.True(afterAdd > initialVersion);
        Assert.True(root.TreeVersion > afterAdd);
    }

    [Fact]
    public void RootOwnsSchedulerAndReturnsFrameStats()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        root.ProcessFrame();
        child.Invalidate(InvalidationFlags.Render, "render");

        FrameStats stats = root.ProcessFrame();

        Assert.Same(root.Scheduler, root.Scheduler);
        Assert.Equal(1, stats.RenderedElements);
        Assert.False(root.Scheduler.HasWork);
    }
}
