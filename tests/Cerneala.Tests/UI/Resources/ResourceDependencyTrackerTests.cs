using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Resources;

public sealed class ResourceDependencyTrackerTests
{
    [Fact]
    public void TrackerRecordsDependency()
    {
        ResourceDependencyTracker tracker = new();
        object owner = new();
        ResourceId<string> id = new("Greeting");

        tracker.RecordDependency(owner, id);

        Assert.Contains(owner, tracker.GetDependents(id));
    }

    [Fact]
    public void ResourceReplacementUpdatesOwnerVersion()
    {
        ResourceStore store = new();
        ResourceDependencyTracker tracker = new();
        tracker.Track(store);
        object owner = new();
        ResourceId<string> id = new("Greeting");
        tracker.RecordDependency(owner, id);

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
        object owner = new();
        ResourceId<string> first = new("First");
        ResourceId<string> second = new("Second");
        tracker.RecordDependency(owner, first);
        tracker.RecordDependency(owner, second);

        store.SetResource(first, "A");
        long versionAfterFirstChange = tracker.GetDependencyVersion(owner);
        store.SetResource(second, "B");

        Assert.True(tracker.GetDependencyVersion(owner) > versionAfterFirstChange);
        Assert.Equal(1, tracker.GetResourceVersion(first));
        Assert.Equal(1, tracker.GetResourceVersion(second));
    }
}
