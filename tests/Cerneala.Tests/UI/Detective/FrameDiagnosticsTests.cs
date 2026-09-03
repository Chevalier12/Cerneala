using Cerneala.UI.Detective;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Relay;

namespace Cerneala.Tests.UI.Detective;

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
        stats.CountRelay(new UiRelayDrainResult(
            SnapshotCount: 7,
            Dequeued: 6,
            Executed: 5,
            Canceled: 4,
            Faulted: 3,
            Deferred: 2,
            Backlog: 1));

        FrameDiagnosticsSnapshot snapshot = FrameDiagnostics.Capture(stats);

        Assert.Equal(1, snapshot.QueuedMeasureElements);
        Assert.Equal(1, snapshot.QueuedArrangeElements);
        Assert.Equal(2, snapshot.MeasureCalls);
        Assert.Equal(1, snapshot.ArrangeCalls);
        Assert.Equal(1, snapshot.RenderedElements);
        Assert.Equal(1, snapshot.HitTestElements);
        Assert.Equal(1, snapshot.ReusedCaches);
        Assert.Equal(0, snapshot.NoWorkFrames);
        Assert.Equal(7, snapshot.RelaySnapshotCallbacks);
        Assert.Equal(6, snapshot.RelayDequeuedCallbacks);
        Assert.Equal(5, snapshot.RelayExecutedCallbacks);
        Assert.Equal(4, snapshot.RelayCanceledCallbacks);
        Assert.Equal(3, snapshot.RelayFaultedCallbacks);
        Assert.Equal(2, snapshot.RelayDeferredCallbacks);
        Assert.Equal(1, snapshot.RelayBacklog);
        Assert.True(snapshot.HasWork);
    }

    [Fact]
    public void FormatUsesStableCounterNames()
    {
        FrameStats stats = new();
        stats.CountNoWorkFrame();

        string formatted = FrameDiagnostics.Format(stats);

        Assert.Equal("frame queuedMeasure=0, queuedArrange=0, measureCalls=0, arrangeCalls=0, renderCache=0, hitTest=0, reusedCaches=1, noWork=1, motion=0, sampled=0, motionValues=0, motionWrites=0, completed=0, motionRender=0, motionLayout=0, reduced=0, relaySnapshot=0, relayDequeued=0, relayExecuted=0, relayCanceled=0, relayFaulted=0, relayDeferred=0, relayBacklog=0, hasWork=False", formatted);
    }

    [Fact]
    public void ExistingConstructorRemainsCompatibleAndDefaultsRelayCounters()
    {
        FrameDiagnosticsSnapshot snapshot = new(
            InheritedElements: 1,
            CommandStateElements: 2,
            AspectElements: 3,
            QueuedMeasureElements: 4,
            QueuedArrangeElements: 5,
            MeasureCalls: 6,
            ArrangeCalls: 7,
            RenderedElements: 8,
            HitTestElements: 9,
            ReusedCaches: 10,
            NoWorkFrames: 11,
            MotionFrames: 12,
            MotionNodesSampled: 13,
            MotionValuesChanged: 14,
            MotionPropertyWrites: 15,
            MotionCompleted: 16,
            MotionRenderInvalidations: 17,
            MotionLayoutInvalidations: 18,
            MotionSkippedByReducedMotion: 19,
            HasWork: true);

        Assert.Equal(0, snapshot.RelaySnapshotCallbacks);
        Assert.Equal(0, snapshot.RelayDequeuedCallbacks);
        Assert.Equal(0, snapshot.RelayExecutedCallbacks);
        Assert.Equal(0, snapshot.RelayCanceledCallbacks);
        Assert.Equal(0, snapshot.RelayFaultedCallbacks);
        Assert.Equal(0, snapshot.RelayDeferredCallbacks);
        Assert.Equal(0, snapshot.RelayBacklog);
    }
}
