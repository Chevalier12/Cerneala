using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Resources;

public sealed class ElementResourceDictionaryTests
{
    [Fact]
    public void LookupUsesNearestLogicalAncestorThenRootProvider()
    {
        ResourceId<string> hostId = new("HostValue");
        ResourceStore hostResources = new();
        hostResources.SetResource(hostId, "host");

        UIRoot root = new();
        root.SetResourceProvider(hostResources);
        StackPanel parent = new();
        Border child = new();
        root.LogicalChildren.Add(parent);
        parent.LogicalChildren.Add(child);
        parent.Resources["Accent"] = "parent";

        Assert.Equal("parent", child.FindResource<string>("Accent"));
        Assert.Equal("host", child.FindResource(hostId));

        child.Resources["Accent"] = "child";
        Assert.Equal("child", child.FindResource<string>("Accent"));
    }

    [Fact]
    public void ExistingWrongTypedResourceShadowsAncestors()
    {
        StackPanel parent = new();
        Border child = new();
        parent.LogicalChildren.Add(child);
        parent.Resources["Value"] = "parent";
        child.Resources["Value"] = 42;

        Assert.False(child.TryFindResource<string>("Value", out _));
        Assert.Throws<KeyNotFoundException>(() => child.FindResource<string>("Value"));
    }

    [Fact]
    public void MutatingElementResourcesInvalidatesItsSubtree()
    {
        Border element = new();

        element.Resources["Accent"] = "red";

        Assert.True(element.DirtyState.Flags.HasFlag(InvalidationFlags.Render));
        Assert.True(element.DirtyState.Flags.HasFlag(InvalidationFlags.Subtree));
    }
}
