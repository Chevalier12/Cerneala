using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Rendering;

public sealed class RetainedRenderCacheTests
{
    [Fact]
    public void RootCacheVersionChangesWhenRootCommandsAreBuilt()
    {
        UIRoot root = new();
        root.VisualChildren.Add(new RenderingTestElement(Color.White));
        int initialVersion = root.RetainedRenderCache.Version;

        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();
        root.RetainedRenderer.Commit(root);

        Assert.True(root.RetainedRenderCache.IsRootValid);
        Assert.True(root.RetainedRenderCache.Version > initialVersion);
    }

    [Fact]
    public void CachedRootCommandListIsReusedAcrossUnchangedDrawFrames()
    {
        UIRoot root = new();
        root.VisualChildren.Add(new RenderingTestElement(Color.White));

        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();
        DrawCommandList first = root.RetainedRenderer.Commit(root);
        DrawCommandList second = root.RetainedRenderer.Render(root);

        Assert.Same(first, second);
        Assert.Single(first);
    }

    [Fact]
    public void RootCacheIsInvalidatedWhenVisualTreeChanges()
    {
        UIRoot root = new();
        root.Invalidate(InvalidationFlags.Render, "test");
        root.ProcessFrame();
        root.RetainedRenderer.Commit(root);

        root.VisualChildren.Add(new RenderingTestElement(Color.White));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();
        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Single(commands);
    }
}
