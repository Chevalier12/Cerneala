using Cerneala.Backends.SdlGpu;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuRenderSurface3DDiagnosticsTests
{
    [Fact]
    public void AggregatesFrameWorkAndTargetLifecycle()
    {
        SdlGpuRenderSurface3DDiagnostics diagnostics = new();

        diagnostics.BeginFrame();
        diagnostics.RecordRecording();
        diagnostics.RecordRecording();
        diagnostics.RecordPass();
        diagnostics.RecordUpload(48);
        diagnostics.RecordUpload(80);
        diagnostics.RecordDraw();
        diagnostics.RecordDraw();
        diagnostics.RecordDraw();
        diagnostics.RecordTargetCreated();
        diagnostics.RecordTargetCreated();
        diagnostics.RecordTargetRetired();

        Assert.Equal(
            new SdlGpuRenderSurface3DFrameCounters(
                RecordingCount: 2,
                PassCount: 1,
                UploadCount: 2,
                UploadBytes: 128,
                DrawCount: 3,
                TargetCreateCount: 2,
                TargetRetireCount: 1,
                LiveTargetCount: 1),
            diagnostics.FrameCounters);
    }

    [Fact]
    public void BeginFrameResetsFrameEventsButPreservesLiveTargets()
    {
        SdlGpuRenderSurface3DDiagnostics diagnostics = new();
        diagnostics.BeginFrame();
        diagnostics.RecordRecording();
        diagnostics.RecordPass();
        diagnostics.RecordUpload(64);
        diagnostics.RecordDraw();
        diagnostics.RecordTargetCreated();

        diagnostics.BeginFrame();

        Assert.Equal(
            new SdlGpuRenderSurface3DFrameCounters(
                RecordingCount: 0,
                PassCount: 0,
                UploadCount: 0,
                UploadBytes: 0,
                DrawCount: 0,
                TargetCreateCount: 0,
                TargetRetireCount: 0,
                LiveTargetCount: 1),
            diagnostics.FrameCounters);
    }

    [Fact]
    public void RejectsInvalidUploadAndTargetRetirementCounts()
    {
        SdlGpuRenderSurface3DDiagnostics diagnostics = new();
        diagnostics.BeginFrame();

        Assert.Throws<ArgumentOutOfRangeException>(() => diagnostics.RecordUpload(-1));
        Assert.Throws<InvalidOperationException>(() => diagnostics.RecordTargetRetired());
    }

    [Fact]
    public void UsesCheckedAggregation()
    {
        SdlGpuRenderSurface3DDiagnostics diagnostics = new();
        diagnostics.BeginFrame();
        diagnostics.RecordUpload(long.MaxValue);

        Assert.Throws<OverflowException>(() => diagnostics.RecordUpload(1));
    }

    [Fact]
    public void BackendFrameStartResetsEventsAndKeepsLiveTargetGauge()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow(
            "render-surface-3d-diagnostics",
            32,
            16,
            SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)), 32, 16, 1));
        SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(session.DrawingBackend);

        session.BeginFrame(Cerneala.Drawing.Color.Black);
        backend.RenderSurface3DDiagnostics.RecordRecording();
        backend.RenderSurface3DDiagnostics.RecordTargetCreated();
        session.CompleteFrame(present: false);
        Assert.Equal(1, backend.LastFrameRenderSurface3DCounters.RecordingCount);
        Assert.Equal(1, backend.LastFrameRenderSurface3DCounters.LiveTargetCount);

        session.BeginFrame(Cerneala.Drawing.Color.Black);
        try
        {
            Assert.Equal(0, backend.LastFrameRenderSurface3DCounters.RecordingCount);
            Assert.Equal(0, backend.LastFrameRenderSurface3DCounters.TargetCreateCount);
            Assert.Equal(1, backend.LastFrameRenderSurface3DCounters.LiveTargetCount);
        }
        finally
        {
            backend.RenderSurface3DDiagnostics.RecordTargetRetired();
            session.CompleteFrame(present: false);
        }

        Assert.Equal(1, backend.LastFrameRenderSurface3DCounters.TargetRetireCount);
        Assert.Equal(0, backend.LastFrameRenderSurface3DCounters.LiveTargetCount);
    }
}
