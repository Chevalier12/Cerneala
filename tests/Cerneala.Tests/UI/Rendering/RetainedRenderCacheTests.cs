using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Rendering;

public sealed class RetainedRenderCacheTests
{
    [Fact]
    public void RootCacheVersionChangesWhenRootCommandsAreBuilt()
    {
        UIRoot root = new();
        root.VisualChildren.Add(new RenderingTestElement(DrawColor.White));
        int initialVersion = root.RetainedRenderCache.Version;

        root.RetainedRenderer.Render(root);

        Assert.True(root.RetainedRenderCache.IsRootValid);
        Assert.True(root.RetainedRenderCache.Version > initialVersion);
    }

    [Fact]
    public void CachedRootCommandListIsReusedAcrossUnchangedDrawFrames()
    {
        UIRoot root = new();
        root.VisualChildren.Add(new RenderingTestElement(DrawColor.White));

        DrawCommandList first = root.RetainedRenderer.Render(root);
        DrawCommandList second = root.RetainedRenderer.Render(root);

        Assert.Same(first, second);
        Assert.Single(first);
    }

    [Fact]
    public void RootCacheIsInvalidatedWhenVisualTreeChanges()
    {
        UIRoot root = new();
        root.RetainedRenderer.Render(root);

        root.VisualChildren.Add(new RenderingTestElement(DrawColor.White));
        DrawCommandList commands = root.RetainedRenderer.Render(root);

        Assert.Single(commands);
    }
}
