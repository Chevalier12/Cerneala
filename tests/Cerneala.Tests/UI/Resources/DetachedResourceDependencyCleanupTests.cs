using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Resources;

public sealed class DetachedResourceDependencyCleanupTests
{
    [Fact]
    public void DetachedImageResourceDependentIsRemovedFromRootTracker()
    {
        UIRoot root = RootWithDependent(out UIElement owner, out ResourceId<ImageResource> id);

        root.VisualChildren.Remove(owner);

        Assert.DoesNotContain(owner, root.ResourceDependencyTracker.GetDependents(id));
    }

    [Fact]
    public void DetachedTextResourceDependentIsRemovedFromRootTracker()
    {
        UIRoot root = new();
        UIElement owner = new();
        ResourceId<FontResource> id = new("Body");
        root.VisualChildren.Add(owner);
        root.ResourceDependencyTracker.RecordDependency(owner, id, InvalidationFlags.Measure | InvalidationFlags.Render);

        root.VisualChildren.Remove(owner);

        Assert.DoesNotContain(owner, root.ResourceDependencyTracker.GetDependents(id));
    }

    [Fact]
    public void ResourceChangeAfterDependentDetachDoesNotInvalidateDetachedElement()
    {
        UIRoot root = RootWithDependent(out UIElement owner, out ResourceId<ImageResource> id);
        root.VisualChildren.Remove(owner);
        long dirtyVersion = owner.DirtyState.Version;

        IReadOnlyList<ResourceDependencyChange> changes = root.ResourceDependencyTracker.NotifyResourceChanged(
            new ResourceChangedEventArgs(typeof(ImageResource), id.Key, null, null, 1));

        Assert.Empty(changes);
        Assert.Equal(dirtyVersion, owner.DirtyState.Version);
    }

    [Fact]
    public void RootResourceDependencyTrackerDoesNotRetainDetachedElementAfterDetach()
    {
        UIRoot root = RootWithDependent(out UIElement owner, out ResourceId<ImageResource> id);

        root.VisualChildren.Remove(owner);

        Assert.Empty(root.ResourceDependencyTracker.GetDependents(id));
    }

    private static UIRoot RootWithDependent(out UIElement owner, out ResourceId<ImageResource> id)
    {
        UIRoot root = new();
        owner = new UIElement();
        id = new ResourceId<ImageResource>("Logo");
        root.VisualChildren.Add(owner);
        root.ResourceDependencyTracker.RecordDependency(owner, id, InvalidationFlags.Render);
        return root;
    }
}
