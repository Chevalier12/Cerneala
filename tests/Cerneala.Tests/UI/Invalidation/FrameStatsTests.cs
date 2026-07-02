using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Invalidation;

public sealed class FrameStatsTests
{
    [Fact]
    public void CountsProcessedWork()
    {
        FrameStats stats = new();

        stats.Count(FramePhase.Measure);
        stats.Count(FramePhase.Arrange);
        stats.Count(FramePhase.RenderCache);
        stats.Count(FramePhase.HitTest);

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
}
