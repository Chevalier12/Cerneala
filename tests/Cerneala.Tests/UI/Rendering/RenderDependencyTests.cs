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
}
