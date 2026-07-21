using Cerneala.Drawing.MonoGame.Prism.Execution;

namespace Cerneala.UI.Hosting.MonoGame;

internal readonly record struct PrismOperationalDiagnostics(
    bool DevelopmentDetailsEnabled,
    int ActiveCompositionCount,
    int PlannedPassCount,
    int ExecutedPassCount,
    int CaptureCount,
    int ActiveSurfaceCount,
    int PeakLiveSurfaceCount,
    long SurfaceAllocationCount,
    long SurfaceReuseCount,
    long SurfaceByteCount,
    long PeakSurfaceByteCount,
    int FallbackCount,
    PrismExecutionDiagnostic? LastFallback,
    BackdropFrameDiagnosticSnapshot Backdrop,
    int ActiveBackdropLeaseCount,
    bool MotionActive)
{
    public static PrismOperationalDiagnostics Capture(
        PrismExecutionDiagnostics execution,
        BackdropFrameDiagnosticSnapshot backdrop,
        int activeBackdropLeaseCount,
        bool motionActive)
    {
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentOutOfRangeException.ThrowIfNegative(activeBackdropLeaseCount);
        PrismExecutionCounters counters = execution.Counters;
        return new PrismOperationalDiagnostics(
            execution.DetailedDiagnosticsEnabled,
            counters.ActiveCompositionCount,
            counters.PlannedPassCount,
            counters.PassCount,
            counters.CaptureCount,
            counters.ActiveSurfaceCount,
            counters.PeakLiveSurfaceCount,
            counters.CreatedSurfaceCount,
            counters.ReusedSurfaceCount,
            counters.SurfaceByteCount,
            counters.PeakSurfaceByteCount,
            counters.FallbackCount,
            execution.LastFallback,
            backdrop,
            activeBackdropLeaseCount,
            motionActive);
    }
}
