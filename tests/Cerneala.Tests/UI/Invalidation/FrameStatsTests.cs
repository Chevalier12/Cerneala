using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Invalidation;

public sealed class FrameStatsTests
{
    [Fact]
    public void CountsProcessedWork()
    {
        FrameStats stats = new();

        stats.Count(FramePhase.InheritedProperties);
        stats.Count(FramePhase.Style);
        stats.Count(FramePhase.Measure);
        stats.Count(FramePhase.Arrange);
        stats.Count(FramePhase.RenderCache);
        stats.Count(FramePhase.HitTest);

        Assert.Equal(1, stats.InheritedElements);
        Assert.Equal(1, stats.StyledElements);
        Assert.Equal(1, stats.MeasuredElements);
        Assert.Equal(1, stats.ArrangedElements);
        Assert.Equal(1, stats.RenderedElements);
        Assert.Equal(1, stats.HitTestElements);
        Assert.True(stats.HasWork);
    }

    [Fact]
    public void CountsNoWorkFramesAsCacheReuse()
    {
        FrameStats stats = new();

        stats.CountNoWorkFrame();

        Assert.Equal(1, stats.NoWorkFrames);
        Assert.Equal(1, stats.ReusedCaches);
        Assert.False(stats.HasWork);
    }

    [Fact]
    public void CountsActualLayoutCallsSeparatelyFromQueuedElements()
    {
        FrameStats stats = new();

        stats.Count(FramePhase.Measure);
        stats.CountMeasureCall();
        stats.CountMeasureCall();
        stats.Count(FramePhase.Arrange);
        stats.CountArrangeCall();
        stats.CountArrangeCall();
        stats.CountArrangeCall();

        Assert.Equal(1, stats.MeasuredElements);
        Assert.Equal(2, stats.MeasureCalls);
        Assert.Equal(1, stats.ArrangedElements);
        Assert.Equal(3, stats.ArrangeCalls);
    }
}
