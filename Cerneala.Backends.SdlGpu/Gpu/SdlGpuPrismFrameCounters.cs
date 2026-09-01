using Cerneala.Drawing.Prism;

namespace Cerneala.Backends.SdlGpu;

internal readonly record struct SdlGpuPrismFrameCounters(
    int PassCount,
    int CaptureCount,
    long CreatedSurfaceCount,
    long ReusedSurfaceCount,
    int ActiveSurfaceCount,
    int FallbackCount,
    TimeSpan CpuSubmitTime)
{
    public SdlGpuPrismFrameCounters Add(PrismExecutionDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        PrismExecutionCounters counters = diagnostics.Counters;
        return new SdlGpuPrismFrameCounters(
            checked(PassCount + counters.PassCount),
            checked(CaptureCount + counters.CaptureCount),
            checked(CreatedSurfaceCount + counters.CreatedSurfaceCount),
            checked(ReusedSurfaceCount + counters.ReusedSurfaceCount),
            checked(ActiveSurfaceCount + counters.ActiveSurfaceCount),
            checked(FallbackCount + diagnostics.Count),
            CpuSubmitTime + counters.CpuSubmitTime);
    }
}
