using System.Globalization;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Diagnostics;

public static class FrameDiagnostics
{
    public static FrameDiagnosticsSnapshot Capture(FrameStats stats)
    {
        ArgumentNullException.ThrowIfNull(stats);
        return new FrameDiagnosticsSnapshot(
            stats.InheritedElements,
            stats.CommandStateElements,
            stats.StyledElements,
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
    int StyledElements,
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
    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"frame queuedMeasure={QueuedMeasureElements}, queuedArrange={QueuedArrangeElements}, measureCalls={MeasureCalls}, arrangeCalls={ArrangeCalls}, renderCache={RenderedElements}, hitTest={HitTestElements}, reusedCaches={ReusedCaches}, noWork={NoWorkFrames}, motion={MotionFrames}, sampled={MotionNodesSampled}, motionValues={MotionValuesChanged}, motionWrites={MotionPropertyWrites}, completed={MotionCompleted}, motionRender={MotionRenderInvalidations}, motionLayout={MotionLayoutInvalidations}, reduced={MotionSkippedByReducedMotion}, hasWork={HasWork}");
    }
}
