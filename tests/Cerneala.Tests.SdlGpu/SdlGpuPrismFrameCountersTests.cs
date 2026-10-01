using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuPrismFrameCountersTests
{
    [Fact]
    public void AddAggregatesEveryMetricWithoutChangingItsInputs()
    {
        SdlGpuPrismFrameCounters initial = new(7, 11, 13, 17, 19, 23, TimeSpan.FromTicks(29));
        PrismExecutionDiagnostics diagnostics = CreateDiagnostics();
        PrismExecutionCounters execution = diagnostics.Counters;

        SdlGpuPrismFrameCounters first = initial.Add(diagnostics);
        SdlGpuPrismFrameCounters second = first.Add(diagnostics);

        Assert.Equal(new SdlGpuPrismFrameCounters(10, 13, 44, 54, 60, 25, TimeSpan.FromTicks(72)), first);
        Assert.Equal(new SdlGpuPrismFrameCounters(13, 15, 75, 91, 101, 27, TimeSpan.FromTicks(115)), second);
        Assert.Equal(new SdlGpuPrismFrameCounters(7, 11, 13, 17, 19, 23, TimeSpan.FromTicks(29)), initial);
        Assert.Equal(execution, diagnostics.Counters);
        Assert.Equal(2, diagnostics.Count);
        Assert.Equal(0, diagnostics.DetailedCount);
    }

    [Fact]
    public void AddingEmptyDiagnosticsPreservesTheAggregate()
    {
        SdlGpuPrismFrameCounters initial = new(7, 11, 13, 17, 19, 23, TimeSpan.FromTicks(29));

        Assert.Equal(initial, initial.Add(new PrismExecutionDiagnostics()));
    }

    [Fact]
    public void AddRejectsNullDiagnostics()
    {
        SdlGpuPrismFrameCounters initial = default;

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => initial.Add(null!));

        Assert.Equal("diagnostics", exception.ParamName);
    }

    [Theory]
    [InlineData(nameof(SdlGpuPrismFrameCounters.PassCount))]
    [InlineData(nameof(SdlGpuPrismFrameCounters.CaptureCount))]
    [InlineData(nameof(SdlGpuPrismFrameCounters.CreatedSurfaceCount))]
    [InlineData(nameof(SdlGpuPrismFrameCounters.ReusedSurfaceCount))]
    [InlineData(nameof(SdlGpuPrismFrameCounters.ActiveSurfaceCount))]
    [InlineData(nameof(SdlGpuPrismFrameCounters.FallbackCount))]
    [InlineData(nameof(SdlGpuPrismFrameCounters.CpuSubmitTime))]
    public void AddRejectsOverflowInEveryMetric(string metric)
    {
        SdlGpuPrismFrameCounters initial = default;
        initial = metric switch
        {
            nameof(SdlGpuPrismFrameCounters.PassCount) => initial with { PassCount = int.MaxValue },
            nameof(SdlGpuPrismFrameCounters.CaptureCount) => initial with { CaptureCount = int.MaxValue },
            nameof(SdlGpuPrismFrameCounters.CreatedSurfaceCount) => initial with { CreatedSurfaceCount = long.MaxValue },
            nameof(SdlGpuPrismFrameCounters.ReusedSurfaceCount) => initial with { ReusedSurfaceCount = long.MaxValue },
            nameof(SdlGpuPrismFrameCounters.ActiveSurfaceCount) => initial with { ActiveSurfaceCount = int.MaxValue },
            nameof(SdlGpuPrismFrameCounters.FallbackCount) => initial with { FallbackCount = int.MaxValue },
            nameof(SdlGpuPrismFrameCounters.CpuSubmitTime) => initial with { CpuSubmitTime = TimeSpan.MaxValue },
            _ => throw new ArgumentOutOfRangeException(nameof(metric))
        };
        PrismExecutionDiagnostics diagnostics = CreateDiagnostics();

        Assert.Throws<OverflowException>(() => initial.Add(diagnostics));
    }

    private static PrismExecutionDiagnostics CreateDiagnostics()
    {
        PrismExecutionDiagnostics diagnostics = new(detailedDiagnosticsEnabled: false);
        PrismGraphNode capture = new(
            new PrismGraphNodeId(new PrismCacheOwnerToken(1), 0, PrismGraphNodeKind.ControlCapture, 0),
            PrismGraphNodeKind.ControlCapture, 0, null, 0, "Capture", []);
        diagnostics.RecordGraphPass(capture);
        diagnostics.RecordGraphPass(capture);
        diagnostics.RecordPresentation(PrismExecutionPassKind.RootPresent, capture, 0);
        diagnostics.Record(null, 0, PrismFallbackReason.MissingBackdrop, "No backdrop.");
        diagnostics.Record(null, 0, PrismFallbackReason.MissingResource, "No resource.");
        diagnostics.CompleteExecution(
            createdSurfaces: 31,
            reusedSurfaces: 37,
            activeSurfaces: 41,
            surfaceBytes: 0,
            peakSurfaceBytes: 0,
            submitTime: TimeSpan.FromTicks(43));
        return diagnostics;
    }
}
