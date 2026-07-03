using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Rendering;

public sealed class RetainedRendererTests
{
    [Fact]
    public void RenderBuildsRootCommandListFromLocalCaches()
    {
        UIRoot root = new();
        root.VisualChildren.Add(new RenderingTestElement(DrawColor.White));

        DrawCommandList commands = root.RetainedRenderer.Render(root);

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
        root.RetainedRenderer.Render(root);

        first.Invalidate(InvalidationFlags.Render, "test");
        root.ProcessFrame();
        root.RetainedRenderer.Render(root);

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

        DrawCommandList commands = root.RetainedRenderer.Render(root);

        Assert.Empty(commands);
    }

    [Fact]
    public void RenderUsesUpdatedArrangedBoundsAfterAttachedElementMoves()
    {
        UIRoot root = new();
        RenderingTestElement child = new(DrawColor.White);
        root.VisualChildren.Add(child);
        root.RetainedRenderer.Render(root);

        child.Arrange(new ArrangeContext(new LayoutRect(4, 5, 10, 10)));
        DrawCommandList commands = root.RetainedRenderer.Render(root);

        Assert.Single(commands);
        Assert.Equal(new DrawRect(4, 5, 1, 1), commands[0].Rect);
    }

    [Fact]
    public void BackendCannotMutateCachedRootCommandsDuringSubmit()
    {
        UIRoot root = new();
        root.VisualChildren.Add(new RenderingTestElement(DrawColor.White));
        MutatingDrawingBackend backend = new();

        root.RetainedRenderer.Submit(root, backend);
        DrawCommandList commands = root.RetainedRenderer.Render(root);

        Assert.Equal(1, backend.SubmittedCommandCount);
        Assert.Single(commands);
        Assert.Equal(DrawCommandKind.FillRectangle, commands[0].Kind);
    }

    private sealed class MutatingDrawingBackend : IDrawingBackend
    {
        public int SubmittedCommandCount { get; private set; }

        public void Render(DrawCommandList commands)
        {
            SubmittedCommandCount = commands.Count;
            commands.Clear();
        }
    }
}
