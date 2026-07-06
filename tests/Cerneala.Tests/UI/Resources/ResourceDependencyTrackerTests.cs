using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Resources;

public sealed class ResourceDependencyTrackerTests
{
    [Fact]
    public void TrackerRecordsDependency()
    {
        ResourceDependencyTracker tracker = new();
        UIElement owner = new();
        ResourceId<string> id = new("Greeting");

        tracker.RecordDependency(owner, id, InvalidationFlags.Render);

        Assert.Contains(owner, tracker.GetDependents(id));
    }

    [Fact]
    public void ResourceReplacementUpdatesOwnerVersion()
    {
        ResourceStore store = new();
        ResourceDependencyTracker tracker = new();
        tracker.Track(store);
        UIRoot root = new();
        UIElement owner = new();
        ResourceId<string> id = new("Greeting");
        root.VisualChildren.Add(owner);
        tracker.RecordDependency(owner, id, InvalidationFlags.Render);

        store.SetResource(id, "Hello");

        Assert.Equal(1, tracker.GetDependencyVersion(owner));
        Assert.Equal(1, tracker.GetResourceVersion(id));
    }

    [Fact]
    public void TrackingSameProviderTwiceDoesNotDuplicateResourceChangeNotifications()
    {
        ResourceStore store = new();
        ResourceDependencyTracker tracker = new();
        tracker.Track(store);
        tracker.Track(store);
        UIRoot root = new();
        UIElement owner = new();
        ResourceId<string> id = new("Greeting");
        root.VisualChildren.Add(owner);
        tracker.RecordDependency(owner, id, InvalidationFlags.Render);

        store.SetResource(id, "Hello");

        Assert.Equal(1, tracker.GetDependencyVersion(owner));
        Assert.Equal(1, tracker.GetResourceVersion(id));
    }

    [Fact]
    public void DifferentResourceReplacementsAdvanceOwnerDependencyVersion()
    {
        ResourceStore store = new();
        ResourceDependencyTracker tracker = new();
        tracker.Track(store);
        UIRoot root = new();
        UIElement owner = new();
        ResourceId<string> first = new("First");
        ResourceId<string> second = new("Second");
        root.VisualChildren.Add(owner);
        tracker.RecordDependency(owner, first, InvalidationFlags.Render);
        tracker.RecordDependency(owner, second, InvalidationFlags.Render);

        store.SetResource(first, "A");
        long versionAfterFirstChange = tracker.GetDependencyVersion(owner);
        store.SetResource(second, "B");

        Assert.True(tracker.GetDependencyVersion(owner) > versionAfterFirstChange);
        Assert.Equal(1, tracker.GetResourceVersion(first));
        Assert.Equal(1, tracker.GetResourceVersion(second));
    }

    [Fact]
    public void ResourceChangeReturnsInvalidationMetadataForAttachedOwner()
    {
        ResourceDependencyTracker tracker = new();
        UIRoot root = new();
        UIElement owner = new();
        ResourceId<string> id = new("Greeting");
        root.VisualChildren.Add(owner);
        tracker.RecordDependency(owner, id, InvalidationFlags.Measure | InvalidationFlags.Render, affectsIntrinsicSize: false);

        IReadOnlyList<ResourceDependencyChange> changes = tracker.NotifyResourceChanged(
            new ResourceChangedEventArgs(typeof(string), id.Key, "Hello", "Hi", 7));

        ResourceDependencyChange change = Assert.Single(changes);
        Assert.Same(owner, change.Owner);
        Assert.Equal(InvalidationFlags.Measure | InvalidationFlags.Render, change.Effects);
        Assert.False(change.AffectsIntrinsicSize);
        Assert.Equal(1, tracker.GetDependencyVersion(owner));
        Assert.Equal(7, tracker.GetResourceVersion(id));
    }

    [Fact]
    public void ResourceChangeCleansUpDetachedOwners()
    {
        ResourceDependencyTracker tracker = new();
        UIRoot root = new();
        UIElement owner = new();
        ResourceId<string> id = new("Greeting");
        root.VisualChildren.Add(owner);
        tracker.RecordDependency(owner, id, InvalidationFlags.Render);
        root.VisualChildren.Remove(owner);

        IReadOnlyList<ResourceDependencyChange> changes = tracker.NotifyResourceChanged(
            new ResourceChangedEventArgs(typeof(string), id.Key, "Hello", "Hi", 1));

        Assert.Empty(changes);
        Assert.DoesNotContain(owner, tracker.GetDependents(id));
    }
}
