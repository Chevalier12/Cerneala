using System.Globalization;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Detective;

public static class FrameDiagnostics
{
    public static FrameDiagnosticsSnapshot Capture(FrameStats stats)
    {
        ArgumentNullException.ThrowIfNull(stats);
        return new FrameDiagnosticsSnapshot(
            stats.InheritedElements,
            stats.CommandStateElements,
            stats.AspectElements,
            stats.MeasuredElements,
            stats.ArrangedElements,
            stats.MeasureCalls,
            stats.ArrangeCalls,
            stats.RenderedElements,
            stats.HitTestElements,
            stats.ReusedCaches,
            stats.NoWorkFrames,
            stats.MotionFrames,
            stats.MotionNodesSampled,
            stats.MotionValuesChanged,
            stats.MotionPropertyWrites,
            stats.MotionCompleted,
            stats.MotionRenderInvalidations,
            stats.MotionLayoutInvalidations,
            stats.MotionSkippedByReducedMotion,
            stats.RelaySnapshotCallbacks,
            stats.RelayDequeuedCallbacks,
            stats.RelayExecutedCallbacks,
            stats.RelayCanceledCallbacks,
            stats.RelayFaultedCallbacks,
            stats.RelayDeferredCallbacks,
            stats.RelayBacklog,
            stats.HasWork);
    }

    public static string Format(FrameStats stats)
    {
        return Capture(stats).ToString();
    }
}

public sealed record FrameDiagnosticsSnapshot(
    int InheritedElements,
    int CommandStateElements,
    int AspectElements,
    int QueuedMeasureElements,
    int QueuedArrangeElements,
    int MeasureCalls,
    int ArrangeCalls,
    int RenderedElements,
    int HitTestElements,
    int ReusedCaches,
    int NoWorkFrames,
    int MotionFrames,
    int MotionNodesSampled,
    int MotionValuesChanged,
    int MotionPropertyWrites,
    int MotionCompleted,
    int MotionRenderInvalidations,
    int MotionLayoutInvalidations,
    int MotionSkippedByReducedMotion,
    bool HasWork)
{
    public FrameDiagnosticsSnapshot(
        int InheritedElements,
        int CommandStateElements,
        int AspectElements,
        int QueuedMeasureElements,
        int QueuedArrangeElements,
        int MeasureCalls,
        int ArrangeCalls,
        int RenderedElements,
        int HitTestElements,
        int ReusedCaches,
        int NoWorkFrames,
        int MotionFrames,
        int MotionNodesSampled,
        int MotionValuesChanged,
        int MotionPropertyWrites,
        int MotionCompleted,
        int MotionRenderInvalidations,
        int MotionLayoutInvalidations,
        int MotionSkippedByReducedMotion,
        int RelaySnapshotCallbacks,
        int RelayDequeuedCallbacks,
        int RelayExecutedCallbacks,
        int RelayCanceledCallbacks,
        int RelayFaultedCallbacks,
        int RelayDeferredCallbacks,
        int RelayBacklog,
        bool HasWork)
        : this(
            InheritedElements,
            CommandStateElements,
            AspectElements,
            QueuedMeasureElements,
            QueuedArrangeElements,
            MeasureCalls,
            ArrangeCalls,
            RenderedElements,
            HitTestElements,
            ReusedCaches,
            NoWorkFrames,
            MotionFrames,
            MotionNodesSampled,
            MotionValuesChanged,
            MotionPropertyWrites,
            MotionCompleted,
            MotionRenderInvalidations,
            MotionLayoutInvalidations,
            MotionSkippedByReducedMotion,
            HasWork)
    {
        this.RelaySnapshotCallbacks = RelaySnapshotCallbacks;
        this.RelayDequeuedCallbacks = RelayDequeuedCallbacks;
        this.RelayExecutedCallbacks = RelayExecutedCallbacks;
        this.RelayCanceledCallbacks = RelayCanceledCallbacks;
        this.RelayFaultedCallbacks = RelayFaultedCallbacks;
        this.RelayDeferredCallbacks = RelayDeferredCallbacks;
        this.RelayBacklog = RelayBacklog;
    }

    public int RelaySnapshotCallbacks { get; init; }

    public int RelayDequeuedCallbacks { get; init; }

    public int RelayExecutedCallbacks { get; init; }

    public int RelayCanceledCallbacks { get; init; }

    public int RelayFaultedCallbacks { get; init; }

    public int RelayDeferredCallbacks { get; init; }

    public int RelayBacklog { get; init; }

    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"frame queuedMeasure={QueuedMeasureElements}, queuedArrange={QueuedArrangeElements}, measureCalls={MeasureCalls}, arrangeCalls={ArrangeCalls}, renderCache={RenderedElements}, hitTest={HitTestElements}, reusedCaches={ReusedCaches}, noWork={NoWorkFrames}, motion={MotionFrames}, sampled={MotionNodesSampled}, motionValues={MotionValuesChanged}, motionWrites={MotionPropertyWrites}, completed={MotionCompleted}, motionRender={MotionRenderInvalidations}, motionLayout={MotionLayoutInvalidations}, reduced={MotionSkippedByReducedMotion}, relaySnapshot={RelaySnapshotCallbacks}, relayDequeued={RelayDequeuedCallbacks}, relayExecuted={RelayExecutedCallbacks}, relayCanceled={RelayCanceledCallbacks}, relayFaulted={RelayFaultedCallbacks}, relayDeferred={RelayDeferredCallbacks}, relayBacklog={RelayBacklog}, hasWork={HasWork}");
    }
}
