using Cerneala.Drawing;
using Cerneala.Tests.UI.Rendering;
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Diagnostics;

public sealed class RenderCacheDumperTests
{
    [Fact]
    public void DumpIncludesRootAndElementCacheState()
    {
        UIRoot root = new();
        RenderingTestElement child = new(Color.White);
        root.VisualChildren.Add(child);
        child.Invalidate(InvalidationFlags.Render, "render");

        root.ProcessFrame();

        string dump = new RenderCacheDumper().Dump(root);

        Assert.Contains("Render cache", dump, StringComparison.Ordinal);
        Assert.Contains("root valid=", dump, StringComparison.Ordinal);
        Assert.Contains($"RenderingTestElement#{child.ElementId}", dump, StringComparison.Ordinal);
        Assert.Contains("cacheValid=True", dump, StringComparison.Ordinal);
        Assert.Contains("commands=1", dump, StringComparison.Ordinal);
    }
}
