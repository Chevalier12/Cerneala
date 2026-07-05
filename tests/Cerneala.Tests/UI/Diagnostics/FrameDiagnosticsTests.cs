using Cerneala.UI.Diagnostics;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Diagnostics;

public sealed class FrameDiagnosticsTests
{
    [Fact]
    public void CaptureReportsFrameStatsCounters()
    {
        FrameStats stats = new();
        stats.Count(FramePhase.Measure);
        stats.CountMeasureCall();
        stats.CountMeasureCall();
        stats.Count(FramePhase.Arrange);
        stats.CountArrangeCall();
        stats.Count(FramePhase.RenderCache);
        stats.Count(FramePhase.HitTest);
        stats.CountReusedCache();

        FrameDiagnosticsSnapshot snapshot = FrameDiagnostics.Capture(stats);

        Assert.Equal(1, snapshot.QueuedMeasureElements);
        Assert.Equal(1, snapshot.QueuedArrangeElements);
        Assert.Equal(2, snapshot.MeasureCalls);
        Assert.Equal(1, snapshot.ArrangeCalls);
        Assert.Equal(1, snapshot.RenderedElements);
        Assert.Equal(1, snapshot.HitTestElements);
        Assert.Equal(1, snapshot.ReusedCaches);
        Assert.Equal(0, snapshot.NoWorkFrames);
        Assert.True(snapshot.HasWork);
    }

    [Fact]
    public void FormatUsesStableCounterNames()
    {
        FrameStats stats = new();
        stats.CountNoWorkFrame();

        string formatted = FrameDiagnostics.Format(stats);

        Assert.Equal("frame queuedMeasure=0, queuedArrange=0, measureCalls=0, arrangeCalls=0, renderCache=0, hitTest=0, reusedCaches=1, noWork=1, hasWork=False", formatted);
    }
}
