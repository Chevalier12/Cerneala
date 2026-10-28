using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;
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
        root.VisualChildren.Add(new RenderingTestElement(Color.White));

        PrepareSubtree(root);
        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Single(commands);
        Assert.Equal(DrawCommandKind.FillRectangle, commands[0].Kind);
    }

    [Fact]
    public void ChildRenderChangeDoesNotRebuildUnrelatedSiblingLocalCommands()
    {
        UIRoot root = new();
        RenderingTestElement first = new(Color.White);
        RenderingTestElement second = new(Color.Black);
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        root.ProcessFrame();
        PrepareSubtree(root);
        root.RetainedRenderer.Commit(root);
        int firstRenderCountAfterPrepare = first.RenderCount;
        int secondRenderCountAfterPrepare = second.RenderCount;

        first.Invalidate(InvalidationFlags.Render, "test");
        root.ProcessFrame();
        root.RetainedRenderer.Commit(root);

        Assert.Equal(firstRenderCountAfterPrepare + 1, first.RenderCount);
        Assert.Equal(secondRenderCountAfterPrepare, second.RenderCount);
    }

    [Fact]
    public void HiddenElementDoesNotEmitVisibleCommands()
    {
        UIRoot root = new();
        root.VisualChildren.Add(new RenderingTestElement(Color.White)
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
        RenderingTestElement child = new(Color.White);
        root.VisualChildren.Add(child);
        root.ProcessFrame();
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
        root.VisualChildren.Add(new RenderingTestElement(Color.White));
        PrepareSubtree(root);
        DrawCommandList committed = root.RetainedRenderer.Commit(root);
        CapturingDrawingBackend backend = new();
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(committed);
        DrawingFrameContext frameContext = new(analysis);

        root.RetainedRenderer.Submit(root, backend, in frameContext);

        Assert.Same(committed, backend.LastCommands);
        Assert.Same(analysis, backend.LastFrameContext!.Value.PrismAnalysis);
        Assert.Single(committed);
    }

    // The documented example commits a new root without a frame; an empty root must also commit after a
    // frame (#955). The root with a child is the control row.
    [Theory]
    [InlineData("documented")]
    [InlineData("emptyAfterFrame")]
    [InlineData("emptyWithViewportAfterFrame")]
    [InlineData("childAfterFrame")]
    public void DocumentedCommitExampleRunsOnAFreshRoot(string row)
    {
        UIRoot root = row == "emptyWithViewportAfterFrame" ? new UIRoot(800, 600) : new UIRoot();
        if (row == "childAfterFrame")
        {
            root.VisualChildren.Add(new RenderingTestElement(Color.White));
        }

        if (row != "documented")
        {
            root.ProcessFrame();
        }

        DrawCommandList? committed = null;
        DrawCommandList? rendered = null;
        Exception? failure = Record.Exception(() =>
        {
            committed = root.RetainedRenderer.Commit(root);
            rendered = root.RetainedRenderer.Render(root);
        });

        Assert.True(failure is null, $"{row}: {failure?.GetType().Name}: {failure?.Message}");
        Assert.Same(committed, rendered);
        Assert.Equal(row == "childAfterFrame" ? 1 : 0, committed!.Count);
    }

    // A huge but finite scale makes the pivot translation overflow float (#1201). Like hit testing, the element
    // whose transform overflows draws nothing, its subtree is skipped, and a sibling still draws.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OverflowingElementTransformSkipsItsSubtreeWithoutThrowing(bool overflowOnAncestor)
    {
        UIRoot root = new(200, 160);
        RenderingTestElement parent = new(new Color(1, 0, 0)) { Width = 100, Height = 80 };
        RenderingTestElement child = new(new Color(2, 0, 0)) { Width = 100, Height = 80 };
        RenderingTestElement sibling = new(Color.Black) { Width = 100, Height = 80 };
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);
        root.VisualChildren.Add(sibling);
        root.ProcessFrame();
        root.RetainedRenderer.Commit(root);

        (overflowOnAncestor ? parent : child).ScaleX = float.MaxValue;
        for (int commit = 0; commit < 2; commit++)
        {
            root.ProcessFrame();
            DrawCommandList? commands = null;
            Exception? failure = Record.Exception(() => commands = root.RetainedRenderer.Commit(root));

            Assert.True(failure is null, $"commit {commit}: {failure?.GetType().Name}: {failure?.Message}");
            Color[] colors = commands!.Where(command => command.Kind == DrawCommandKind.FillRectangle)
                .Select(command => command.Color).ToArray();
            Assert.Equal(overflowOnAncestor ? [Color.Black] : [new Color(1, 0, 0), Color.Black], colors);
            Assert.DoesNotContain(commands!, command => command.Kind == DrawCommandKind.PushTransform);
        }

        (overflowOnAncestor ? parent : child).ScaleX = 1;
        root.ProcessFrame();
        Assert.Equal(
            [new Color(1, 0, 0), new Color(2, 0, 0), Color.Black],
            root.RetainedRenderer.Commit(root).Where(command => command.Kind == DrawCommandKind.FillRectangle)
                .Select(command => command.Color));
    }

    private static void PrepareSubtree(UIElement element)
    {
        UIRoot root = element.Root ?? throw new InvalidOperationException("Element must be attached.");
        RenderCounters counters = root.Detective.RenderingCounters;
        root.RetainedRenderCache.GetElementCache(element).Ensure(element, counters, forceRebuild: true);
        foreach (UIElement child in element.VisualChildren)
        {
            PrepareSubtree(child);
        }
    }

    private sealed class CapturingDrawingBackend : IDrawingBackend
    {
        public DrawCommandList? LastCommands { get; private set; }

        public DrawingFrameContext? LastFrameContext { get; private set; }

        public void Render(
            DrawCommandList commands,
            in DrawingFrameContext frameContext)
        {
            frameContext.EnsureCurrent(commands);
            LastCommands = commands;
            LastFrameContext = frameContext;
        }
    }
}
