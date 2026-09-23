namespace Cerneala.Backends.SdlGpu;

internal readonly record struct SdlGpuRenderSurface3DFrameCounters(
    int RecordingCount,
    int PassCount,
    int UploadCount,
    long UploadBytes,
    int DrawCount,
    int TargetCreateCount,
    int TargetRetireCount,
    int LiveTargetCount);

// Stage-zero structural observer. The future executor reports work here; this
// type owns no GPU objects and exposes no public profiling contract.
internal sealed class SdlGpuRenderSurface3DDiagnostics
{
    private SdlGpuRenderSurface3DFrameCounters frameCounters;
    private int liveTargetCount;

    internal SdlGpuRenderSurface3DFrameCounters FrameCounters => frameCounters;

    internal void BeginFrame() => frameCounters = new(
        RecordingCount: 0,
        PassCount: 0,
        UploadCount: 0,
        UploadBytes: 0,
        DrawCount: 0,
        TargetCreateCount: 0,
        TargetRetireCount: 0,
        LiveTargetCount: liveTargetCount);

    internal void RecordRecording() => frameCounters = frameCounters with
    {
        RecordingCount = checked(frameCounters.RecordingCount + 1)
    };

    internal void RecordPass() => frameCounters = frameCounters with
    {
        PassCount = checked(frameCounters.PassCount + 1)
    };

    internal void RecordUpload(long byteCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteCount);
        frameCounters = frameCounters with
        {
            UploadCount = checked(frameCounters.UploadCount + 1),
            UploadBytes = checked(frameCounters.UploadBytes + byteCount)
        };
    }

    internal void RecordDraw() => frameCounters = frameCounters with
    {
        DrawCount = checked(frameCounters.DrawCount + 1)
    };

    internal void RecordTargetCreated()
    {
        int nextLiveTargetCount = checked(liveTargetCount + 1);
        int nextCreateCount = checked(frameCounters.TargetCreateCount + 1);
        liveTargetCount = nextLiveTargetCount;
        frameCounters = frameCounters with
        {
            TargetCreateCount = nextCreateCount,
            LiveTargetCount = liveTargetCount
        };
    }

    internal void RecordTargetRetired()
    {
        if (liveTargetCount == 0)
        {
            throw new InvalidOperationException(
                "A RenderSurface3D target cannot be retired when none are live.");
        }

        int nextRetireCount = checked(frameCounters.TargetRetireCount + 1);
        liveTargetCount--;
        frameCounters = frameCounters with
        {
            TargetRetireCount = nextRetireCount,
            LiveTargetCount = liveTargetCount
        };
    }
}
