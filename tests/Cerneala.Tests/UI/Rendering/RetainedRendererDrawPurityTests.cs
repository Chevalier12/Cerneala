using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Rendering;

public sealed class RetainedRendererDrawPurityTests
{
    [Fact]
    public void RenderThrowsWhenRootCommandListIsNotCommitted()
    {
        UIRoot root = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            root.RetainedRenderer.Render(root));

        Assert.Contains("committed", exception.Message);
    }

    [Fact]
    public void CommitBuildsRootCommandsFromPreparedLocalCaches()
    {
        UIRoot root = new();
        RenderingTestElement child = new(DrawColor.White);
        root.VisualChildren.Add(child);
        PrepareSubtree(root);
        int renderCountAfterPrepare = child.RenderCount;

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Single(commands);
        Assert.Equal(renderCountAfterPrepare, child.RenderCount);
        Assert.Same(commands, root.RetainedRenderer.Render(root));
    }

    [Fact]
    public void SubmitUsesCommittedCommandListWithoutCopying()
    {
        UIRoot root = new();
        RenderingTestElement child = new(DrawColor.White);
        root.VisualChildren.Add(child);
        PrepareSubtree(root);
        DrawCommandList committed = root.RetainedRenderer.Commit(root);
        CapturingBackend backend = new();

        root.RetainedRenderer.Submit(root, backend);

        Assert.Same(committed, backend.LastCommands);
        Assert.Equal(1, child.RenderCount);
    }

    [Fact]
    public void SubmitThrowsWhenRootCommandListIsInvalid()
    {
        UIRoot root = new();
        CapturingBackend backend = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            root.RetainedRenderer.Submit(root, backend));

        Assert.Contains("committed", exception.Message);
        Assert.Equal(0, backend.RenderCalls);
    }

    private static void PrepareSubtree(UIElement element)
    {
        UIRoot root = element.Root ?? throw new InvalidOperationException("Element must be attached.");
        RenderCounters counters = root.RenderCounters;
        root.RetainedRenderCache.GetElementCache(element).Ensure(element, counters, forceRebuild: true);
        foreach (UIElement child in element.VisualChildren)
        {
            PrepareSubtree(child);
        }
    }

    private sealed class CapturingBackend : IDrawingBackend
    {
        public int RenderCalls { get; private set; }

        public DrawCommandList? LastCommands { get; private set; }

        public void Render(DrawCommandList commands)
        {
            RenderCalls++;
            LastCommands = commands;
        }
    }
}
