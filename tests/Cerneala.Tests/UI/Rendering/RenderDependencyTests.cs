using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Rendering;

public sealed class RenderDependencyTests
{
    [Fact]
    public void DependencyVersionsAreComparable()
    {
        RenderDependency first = RenderDependency.None.WithTextVersion(1).WithImageVersion(2);
        RenderDependency second = RenderDependency.None.WithTextVersion(1).WithImageVersion(2);
        RenderDependency changed = second.WithResourceVersion(3);

        Assert.Equal(first, second);
        Assert.NotEqual(first, changed);
    }

    [Fact]
    public void NoneUsesTheDeclaredEmptyIdentities()
    {
        Assert.Equal(string.Empty, RenderDependency.None.TextLayoutIdentity);
        Assert.Equal(string.Empty, RenderDependency.None.ResourceIdentity);
        Assert.Equal(new RenderDependency(0, "", 0, "", 0, 0), RenderDependency.None);
    }

    [Fact]
    public void NewElementStartsWithNoRenderDependencies()
    {
        Assert.Equal(RenderDependency.None, new UIElement().RenderDependencies);
    }
}
