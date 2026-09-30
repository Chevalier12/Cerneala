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
    public void ValueEqualOwnersRemainDistinctDependencies()
    {
        ResourceDependencyTracker tracker = new();
        ValueEqualOwner first = new();
        ValueEqualOwner second = new();
        ResourceId<string> id = new("Greeting");
        Assert.Equal(first, second);
        UIRoot root = new();
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);

        tracker.RecordDependency(first, id);
        tracker.RecordDependency(second, id);

        IReadOnlyCollection<UIElement> dependents = tracker.GetDependents(id);
        Assert.Equal(2, dependents.Count);
        Assert.Contains(dependents, owner => ReferenceEquals(owner, first));
        Assert.Contains(dependents, owner => ReferenceEquals(owner, second));

        Assert.Equal(2, tracker.NotifyResourceChanged(
            new ResourceChangedEventArgs(typeof(string), id.Key, null, "Hello", 1)).Count);
        Assert.True(tracker.GetDependencyVersion(first) > 0);
        Assert.True(tracker.GetDependencyVersion(second) > 0);
        Assert.NotEqual(tracker.GetDependencyVersion(first), tracker.GetDependencyVersion(second));

        tracker.RemoveOwner(first);

        Assert.Same(second, Assert.Single(tracker.GetDependents(id)));
        Assert.Equal(0, tracker.GetDependencyVersion(first));
        Assert.True(tracker.GetDependencyVersion(second) > 0);
    }

    [Fact]
    public void ValueEqualProvidersAreTrackedIndependentlyWithoutDuplicateSubscriptions()
    {
        ResourceDependencyTracker tracker = new();
        ValueEqualProvider first = new();
        ValueEqualProvider second = new();
        Assert.Equal(first, second);
        tracker.Track(first);
        tracker.Track(second);
        tracker.Track(first);
        tracker.Track(second);
        UIRoot root = new();
        UIElement owner = new();
        ResourceId<string> id = new("Greeting");
        root.VisualChildren.Add(owner);
        tracker.RecordDependency(owner, id);

        first.NotifyChanged(id, 1);
        Assert.Equal(1, tracker.GetDependencyVersion(owner));

        second.NotifyChanged(id, 2);
        Assert.Equal(2, tracker.GetDependencyVersion(owner));
        Assert.Equal(2, tracker.GetResourceVersion(id));
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

    private sealed class ValueEqualOwner : UIElement
    {
        public override bool Equals(object? obj) => obj is ValueEqualOwner;

        public override int GetHashCode() => 0;
    }

    private sealed class ValueEqualProvider : IObservableResourceProvider
    {
        public event EventHandler<ResourceChangedEventArgs>? ResourceChanged;

        public bool TryGetResource<T>(ResourceId<T> id, out T resource)
        {
            resource = default!;
            return false;
        }

        public void NotifyChanged(ResourceId<string> id, long version) => ResourceChanged?.Invoke(
            this, new ResourceChangedEventArgs(typeof(string), id.Key, null, "Hello", version));

        public override bool Equals(object? obj) => obj is ValueEqualProvider;

        public override int GetHashCode() => 0;
    }
}
