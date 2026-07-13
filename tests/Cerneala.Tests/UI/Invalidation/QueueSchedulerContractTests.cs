using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Invalidation;

public sealed class QueueSchedulerContractTests
{
    [Fact]
    public void ExceptionKeepsCurrentAndUnprocessedSnapshotEntries()
    {
        UIRoot root = new();
        UIElement first = new();
        UIElement second = new();
        UIElement third = new();
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        root.VisualChildren.Add(third);
        root.ProcessFrame();
        root.RenderQueue.Enqueue(first);
        root.RenderQueue.Enqueue(second);
        root.RenderQueue.Enqueue(third);

        Assert.Throws<InvalidOperationException>(() => root.ProcessFrame(new FramePhaseProcessors
        {
            RenderCache = element =>
            {
                if (ReferenceEquals(element, second))
                {
                    throw new InvalidOperationException("boom");
                }
            }
        }));

        Assert.Equal([second, third], root.RenderQueue.Snapshot());
    }

    [Fact]
    public void ArrangeQueuedDuringMeasureRunsInSameFrame()
    {
        UIRoot root = new();
        UIElement source = new();
        UIElement downstream = new();
        root.VisualChildren.Add(source);
        root.VisualChildren.Add(downstream);
        root.ProcessFrame();
        root.LayoutQueue.EnqueueMeasure(source);
        List<UIElement> arranged = [];

        root.ProcessFrame(new FramePhaseProcessors
        {
            Measure = element =>
            {
                if (ReferenceEquals(element, source))
                {
                    root.LayoutQueue.EnqueueArrange(downstream);
                }
            },
            Arrange = arranged.Add
        });

        Assert.Contains(downstream, arranged);
    }

    [Fact]
    public void DetachDuringPhaseKeepsCapturedSnapshotStableAndCleansFutureWork()
    {
        UIRoot root = new();
        UIElement first = new();
        UIElement second = new();
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        root.ProcessFrame();
        root.RenderQueue.Enqueue(first);
        root.RenderQueue.Enqueue(second);
        List<UIElement> rendered = [];

        root.ProcessFrame(new FramePhaseProcessors
        {
            RenderCache = element =>
            {
                rendered.Add(element);
                if (ReferenceEquals(element, first))
                {
                    root.VisualChildren.Remove(second);
                }
            }
        });

        Assert.Equal([first, second], rendered);
        Assert.DoesNotContain(second, root.RenderQueue.Snapshot());
    }

    [Fact]
    public void IdleFrameDoesNotBuildOrderIndex()
    {
        UIRoot root = new();

        FrameStats stats = root.ProcessFrame();

        Assert.Equal(1, stats.NoWorkFrames);
        Assert.Equal(0, root.QueueOrderIndex.BuildCount);
    }
}
