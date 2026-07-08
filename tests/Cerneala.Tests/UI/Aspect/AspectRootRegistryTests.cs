using Cerneala.UI.Aspect;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Aspect;

public sealed class AspectRootRegistryTests
{
    [Fact]
    public void RootCreatesDefaultAspectRegistry()
    {
        UIRoot root = new();

        Assert.NotNull(root.AspectRegistry);
    }

    [Fact]
    public void RegisteringPackageInvalidatesAspectForSubtree()
    {
        UIRoot root = new();

        root.AspectRegistry.Register(AspectPackage.Create("App"));

        Assert.True(root.DirtyState.Has(InvalidationFlags.Aspect));
    }
}
