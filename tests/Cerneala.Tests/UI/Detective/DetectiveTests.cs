using Cerneala.UI.Detective;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Aspect;
using Cerneala.Drawing.Prism;

namespace Cerneala.Tests.UI.Detective;

public sealed class DetectiveTests
{
    [Fact]
    public void RootOwnsOneDetectiveForItsRetainedSystems()
    {
        UIRoot root = new(320, 180, 1.5f);

        Assert.Same(root.Detective, root.Detective);
        Assert.NotNull(root.Detective.Motion);
        Assert.NotNull(root.Detective.AspectCounters);
        Assert.NotNull(root.Detective.RenderingCounters);
    }

    [Fact]
    public void CaptureProducesOneSnapshotAcrossRootOwnedDomains()
    {
        UIRoot root = new(320, 180, 1.5f);
        FrameStats stats = root.ProcessFrame();

        DetectiveSnapshot snapshot = root.Detective.Capture(stats);

        Assert.Equal(320, snapshot.Viewport.LogicalWidth);
        Assert.Equal(180, snapshot.Viewport.LogicalHeight);
        Assert.Equal(1.5f, snapshot.Viewport.Scale);
        Assert.Equal(root.InputCache.RebuildCount, snapshot.Input.RebuildCount);
        Assert.Equal(root.RetainedRenderCache.Version, snapshot.Rendering.Version);
        Assert.Equal(root.Motion.HasActiveMotion, snapshot.Motion.NeedsAnotherFrame);
    }

    [Fact]
    public void DetectiveOwnsTracingAndCounters()
    {
        UIRoot root = new();

        Assert.NotNull(root.Detective.Invalidation);
        Assert.NotNull(root.Detective.RenderingCounters);
        Assert.NotNull(root.Detective.Motion);
        Assert.NotNull(root.Detective.AspectCounters);
    }

    [Fact]
    public void LegacyDiagnosticEntryPointsAreNotPublic()
    {
        Type[] exportedTypes = typeof(UIRoot).Assembly.GetExportedTypes();

        Assert.DoesNotContain(exportedTypes, type => type.Namespace == "Cerneala.UI.Diagnostics");
        Assert.Null(typeof(UIRoot).GetProperty("Trace"));
        Assert.Null(typeof(UIRoot).GetProperty("RenderCounters"));
        Assert.Null(typeof(MotionSystem).GetProperty("Diagnostics"));
        Assert.Null(typeof(AspectEngine).GetProperty("Counters"));
        Assert.Null(typeof(AspectEngine).GetMethod("GetDiagnostics"));
        Assert.Equal("Cerneala.UI.Detective", typeof(MotionDiagnostics).Namespace);
        Assert.Equal("Cerneala.UI.Detective", typeof(AspectDiagnostics).Namespace);
        Assert.Equal("Cerneala.UI.Detective", typeof(PrismRendererDiagnostics).Namespace);
    }
}
