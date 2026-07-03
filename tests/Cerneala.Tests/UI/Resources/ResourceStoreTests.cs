using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Resources;

public sealed class ResourceStoreTests
{
    [Fact]
    public void StoreReturnsTypedResource()
    {
        ResourceStore store = new();
        ResourceId<string> id = new("Greeting");

        store.SetResource(id, "Hello");

        Assert.True(store.TryGetResource(id, out string? value));
        Assert.Equal("Hello", value);
        Assert.Equal("Hello", store.GetResource(id));
    }

    [Fact]
    public void StoreReturnsStoredNullResource()
    {
        ResourceStore store = new();
        ResourceId<string?> id = new("OptionalGreeting");

        store.SetResource(id, null);

        Assert.True(store.TryGetResource(id, out string? value));
        Assert.Null(value);
        Assert.Null(store.GetResource(id));
        Assert.Equal(1, store.GetVersion(id));
    }

    [Fact]
    public void ReplacingResourceRaisesChangeEvent()
    {
        ResourceStore store = new();
        ResourceId<string> id = new("Greeting");
        ResourceChangedEventArgs? observed = null;
        store.SetResource(id, "Hello");
        store.ResourceChanged += (_, args) => observed = args;

        store.SetResource(id, "World");

        Assert.NotNull(observed);
        Assert.True(observed.Matches(id));
        Assert.Equal("Hello", observed.OldValue);
        Assert.Equal("World", observed.NewValue);
        Assert.Equal(2, observed.Version);
    }

    [Fact]
    public void NoOpReplacementDoesNotNotify()
    {
        ResourceStore store = new();
        ResourceId<string> id = new("Greeting");
        int count = 0;
        store.SetResource(id, "Hello");
        store.ResourceChanged += (_, _) => count++;

        store.SetResource(id, "Hello");

        Assert.Equal(0, count);
    }
}
