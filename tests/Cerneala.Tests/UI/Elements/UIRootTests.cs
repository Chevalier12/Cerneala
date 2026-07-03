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
        Assert.NotNull(root.ElementId);
        Assert.Equal(800, root.ViewportWidth);
        Assert.Equal(600, root.ViewportHeight);
        Assert.Equal(2, root.Scale);
    }

    [Fact]
    public void AddingChildAttachesSubtreeAndAssignsStableIds()
    {
        UIRoot root = new();
        UIElement parent = new();
        UIElement child = new();
        parent.VisualChildren.Add(child);

        root.VisualChildren.Add(parent);

        Assert.Same(root, parent.Root);
        Assert.Same(root, child.Root);
        Assert.NotNull(parent.ElementId);
        Assert.NotNull(child.ElementId);

        var parentId = parent.ElementId;
        root.SetViewport(100, 100, 1);

        Assert.Equal(parentId, parent.ElementId);
    }

    [Fact]
    public void RemovingChildDetachesSubtreeAndReleasesIds()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        var childId = child.ElementId!.Value;

        bool removed = root.VisualChildren.Remove(child);

        Assert.True(removed);
        Assert.False(child.IsAttached);
        Assert.Null(child.Root);
        Assert.Null(child.ElementId);
        Assert.False(root.ElementIds.TryGetElement(childId, out _));
    }

    [Fact]
    public void DistinctAttachedElementsWithEqualValuesReceiveDistinctIds()
    {
        ElementIdProvider provider = new();
        EqualValueElement first = new(1);
        EqualValueElement second = new(1);

        var firstId = provider.GetOrCreate(first);
        var secondId = provider.GetOrCreate(second);

        Assert.NotEqual(firstId, secondId);
        Assert.True(provider.TryGetElement(firstId, out UIElement? resolvedFirst));
        Assert.True(provider.TryGetElement(secondId, out UIElement? resolvedSecond));
        Assert.Same(first, resolvedFirst);
        Assert.Same(second, resolvedSecond);
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

    private sealed class EqualValueElement(int value) : UIElement
    {
        private readonly int value = value;

        public override bool Equals(object? obj)
        {
            return obj is EqualValueElement other && other.value == value;
        }

        public override int GetHashCode()
        {
            return value;
        }
    }
}
