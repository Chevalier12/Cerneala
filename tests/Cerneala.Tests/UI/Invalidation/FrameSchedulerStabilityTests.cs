using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Invalidation;

public sealed class FrameSchedulerStabilityTests
{
    [Fact]
    public void SamePhaseWorkQueuedDuringMeasureRunsOnLaterFrame()
    {
        UIRoot root = new();
        UIElement first = new();
        UIElement second = new();
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        root.ProcessFrame();
        first.Invalidate(InvalidationFlags.Measure, "first measure");
        List<UIElement> measured = [];

        root.ProcessFrame(new FramePhaseProcessors
        {
            Measure = element =>
            {
                measured.Add(element);
                if (ReferenceEquals(element, first))
                {
                    second.Invalidate(InvalidationFlags.Measure, "queued during measure");
                }
            }
        });

        Assert.Contains(first, measured);
        Assert.DoesNotContain(second, measured);
        Assert.True(second.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(root.LayoutQueue.MeasureCount > 0);

        FrameStats next = root.ProcessFrame(new FramePhaseProcessors
        {
            Measure = element => measured.Add(element)
        });

        Assert.Contains(second, measured);
        Assert.True(next.MeasuredElements > 0);
    }

    [Fact]
    public void DownstreamWorkQueuedBeforeSnapshotRunsInSameFrame()
    {
        UIRoot root = new();
        UIElement first = new();
        UIElement second = new();
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        root.ProcessFrame();
        first.Invalidate(InvalidationFlags.Arrange, "first arrange");
        List<UIElement> rendered = [];

        root.ProcessFrame(new FramePhaseProcessors
        {
            Arrange = element =>
            {
                if (ReferenceEquals(element, first))
                {
                    second.Invalidate(InvalidationFlags.Render, "queued during arrange");
                }
            },
            RenderCache = element => rendered.Add(element)
        });

        Assert.Contains(second, rendered);
        Assert.False(second.DirtyState.Has(InvalidationFlags.Render));
        Assert.Equal(0, root.RenderQueue.Count);
    }

    [Fact]
    public void UnchangedSecondFrameIsNoWorkFrame()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        root.ProcessFrame();

        FrameStats second = root.ProcessFrame();

        Assert.Equal(1, second.NoWorkFrames);
        Assert.False(root.Scheduler.HasWork);
    }
}
