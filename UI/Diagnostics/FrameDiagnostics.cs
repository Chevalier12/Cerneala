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
    bool HasWork)
{
    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"frame queuedMeasure={QueuedMeasureElements}, queuedArrange={QueuedArrangeElements}, measureCalls={MeasureCalls}, arrangeCalls={ArrangeCalls}, renderCache={RenderedElements}, hitTest={HitTestElements}, reusedCaches={ReusedCaches}, noWork={NoWorkFrames}, hasWork={HasWork}");
    }
}
