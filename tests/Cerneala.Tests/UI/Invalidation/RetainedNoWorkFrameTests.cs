using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Invalidation;

public sealed class RetainedNoWorkFrameTests
{
    [Fact]
    public void UnchangedTreeDoesNotMeasureOnSecondFrame()
    {
        UIRoot root = RootWithChild(out UIElement child);
        int measured = 0;
        bool firstFrameMeasured = false;
        child.Invalidate(InvalidationFlags.Measure, "measure");

        root.ProcessFrame(new FramePhaseProcessors { Measure = _ => firstFrameMeasured = true });
        if (firstFrameMeasured)
        {
            measured++;
        }

        root.ProcessFrame(new FramePhaseProcessors { Measure = _ => measured++ });

        Assert.Equal(1, measured);
    }

    [Fact]
    public void UnchangedTreeDoesNotArrangeOnSecondFrame()
    {
        UIRoot root = RootWithChild(out UIElement child);
        int arranged = 0;
        child.Invalidate(InvalidationFlags.Arrange, "arrange");

        root.ProcessFrame(new FramePhaseProcessors { Arrange = _ => arranged++ });
        root.ProcessFrame(new FramePhaseProcessors { Arrange = _ => arranged++ });

        Assert.Equal(1, arranged);
    }

    [Fact]
    public void UnchangedTreeDoesNotRegenerateRenderCommandsOnSecondDraw()
    {
        UIRoot root = RootWithChild(out UIElement child);
        int rendered = 0;
        child.Invalidate(InvalidationFlags.Render, "render");

        root.ProcessFrame(new FramePhaseProcessors { RenderCache = _ => rendered++ });
        root.ProcessFrame(new FramePhaseProcessors { RenderCache = _ => rendered++ });

        Assert.Equal(1, rendered);
    }

    [Fact]
    public void DrawEveryFrameCanReuseCachedRootCommandList()
    {
        UIRoot root = new();

        FrameStats stats = root.ProcessFrame();

        Assert.Equal(1, stats.ReusedCaches);
        Assert.Equal(1, stats.NoWorkFrames);
    }

    [Fact]
    public void RenderOnlyInvalidationDoesNotRunMeasure()
    {
        UIRoot root = RootWithChild(out UIElement child);
        int measured = 0;
        int rendered = 0;
        child.Invalidate(InvalidationFlags.Render, "render");

        root.ProcessFrame(new FramePhaseProcessors
        {
            Measure = _ => measured++,
            RenderCache = _ => rendered++
        });

        Assert.Equal(0, measured);
        Assert.Equal(1, rendered);
    }

    [Fact]
    public void MeasureInvalidationRegeneratesRenderCommandsOnlyAfterLayoutSettles()
    {
        UIRoot root = RootWithChild(out UIElement child);
        List<FramePhase> phases = [];
        child.Invalidate(InvalidationFlags.Measure, "measure");

        root.ProcessFrame(new FramePhaseProcessors
        {
            Measure = _ => phases.Add(FramePhase.Measure),
            Arrange = _ => phases.Add(FramePhase.Arrange),
            RenderCache = _ => phases.Add(FramePhase.RenderCache)
        });

        int lastArrange = phases.LastIndexOf(FramePhase.Arrange);
        int firstRender = phases.IndexOf(FramePhase.RenderCache);
        Assert.True(lastArrange >= 0);
        Assert.True(firstRender > lastArrange);
    }

    [Fact]
    public void HoverChangeInvalidatesRenderOnlyWhenVisualStateActuallyChanges()
    {
        UIRoot root = RootWithChild(out UIElement child);
        child.Invalidate(InvalidationFlags.InputVisual, "hover");

        FrameStats first = root.ProcessFrame();
        FrameStats second = root.ProcessFrame();

        Assert.Equal(1, first.RenderedElements);
        Assert.Equal(0, second.RenderedElements);
        Assert.Equal(1, second.NoWorkFrames);
    }

    [Fact]
    public void TextColorChangeRebuildsRenderCommandsWithoutReshapingWhenMetricsAreUnchanged()
    {
        UIRoot root = RootWithChild(out UIElement child);
        int measured = 0;
        int rendered = 0;
        child.Invalidate(InvalidationFlags.Render, "text color");

        root.ProcessFrame(new FramePhaseProcessors
        {
            Measure = _ => measured++,
            RenderCache = _ => rendered++
        });

        Assert.Equal(0, measured);
        Assert.Equal(1, rendered);
    }

    private static UIRoot RootWithChild(out UIElement child)
    {
        UIRoot root = new();
        child = new UIElement();
        root.VisualChildren.Add(child);
        root.ProcessFrame();
        return root;
    }
}
