using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Rendering;

public sealed class RetainedRendererTests
{
    [Fact]
    public void RenderBuildsRootCommandListFromLocalCaches()
    {
        UIRoot root = new();
        root.VisualChildren.Add(new RenderingTestElement(DrawColor.White));

        PrepareSubtree(root);
        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Single(commands);
        Assert.Equal(DrawCommandKind.FillRectangle, commands[0].Kind);
    }

    [Fact]
    public void ChildRenderChangeDoesNotRebuildUnrelatedSiblingLocalCommands()
    {
        UIRoot root = new();
        RenderingTestElement first = new(DrawColor.White);
        RenderingTestElement second = new(DrawColor.Black);
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        PrepareSubtree(root);
        root.RetainedRenderer.Commit(root);

        first.Invalidate(InvalidationFlags.Render, "test");
        root.ProcessFrame();
        root.RetainedRenderer.Commit(root);

        Assert.Equal(2, first.RenderCount);
        Assert.Equal(1, second.RenderCount);
    }

    [Fact]
    public void HiddenElementDoesNotEmitVisibleCommands()
    {
        UIRoot root = new();
        root.VisualChildren.Add(new RenderingTestElement(DrawColor.White)
        {
            Visibility = Visibility.Hidden
        });

        PrepareSubtree(root);
        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Empty(commands);
    }

    [Fact]
    public void RenderUsesUpdatedArrangedBoundsAfterAttachedElementMoves()
    {
        UIRoot root = new();
        RenderingTestElement child = new(DrawColor.White);
        root.VisualChildren.Add(child);
        PrepareSubtree(root);
        root.RetainedRenderer.Commit(root);

        child.Arrange(new ArrangeContext(new LayoutRect(4, 5, 10, 10)));
        child.Invalidate(InvalidationFlags.Render, "moved");
        root.ProcessFrame();
        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Single(commands);
        Assert.Equal(new DrawRect(4, 5, 1, 1), commands[0].Rect);
    }

    [Fact]
    public void SubmitPassesCommittedRootCommandsByReference()
    {
        UIRoot root = new();
        root.VisualChildren.Add(new RenderingTestElement(DrawColor.White));
        PrepareSubtree(root);
        DrawCommandList committed = root.RetainedRenderer.Commit(root);
        CapturingDrawingBackend backend = new();

        root.RetainedRenderer.Submit(root, backend);

        Assert.Same(committed, backend.LastCommands);
        Assert.Single(committed);
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

    private sealed class CapturingDrawingBackend : IDrawingBackend
    {
        public DrawCommandList? LastCommands { get; private set; }

        public void Render(DrawCommandList commands)
        {
            LastCommands = commands;
        }
    }
}
