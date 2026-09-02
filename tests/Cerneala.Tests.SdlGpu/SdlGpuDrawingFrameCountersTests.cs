using Cerneala.Backends.SdlGpu;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuDrawingFrameCountersTests
{
    [Fact]
    public void AddFlushAggregatesSubmittedGeometry()
    {
        SdlGpuDrawingFrameCounters counters = default;

        counters = counters.AddFlush(Metrics(3, 1, 8, 12, 2, 1, 2, 1, 1));
        counters = counters.AddFlush(Metrics(2, 1, 4, 6, 1, 1, 1, 1, 1));

        Assert.Equal(2, counters.FlushCount);
        Assert.Equal(5, counters.SubmissionCount);
        Assert.Equal(2, counters.MergedSubmissionCount);
        Assert.Equal(3, counters.DrawCallCount);
        Assert.Equal(12, counters.VertexCount);
        Assert.Equal(18, counters.IndexCount);
        Assert.Equal(12L * 32, counters.VertexBytes);
        Assert.Equal(18L * sizeof(int), counters.IndexBytes);
        Assert.Equal(2, counters.PipelineBindCount);
        Assert.Equal(3, counters.SamplerBindCount);
        Assert.Equal(2, counters.ScissorSetCount);
        Assert.Equal(2, counters.StencilReferenceSetCount);
    }

    [Fact]
    public void AddFlushUsesCheckedAggregation()
    {
        SdlGpuDrawingFrameCounters counters = default;
        counters = counters.AddFlush(Metrics(int.MaxValue, 0, 0, 0, 0, 0, 0, 0, 0));

        Assert.Throws<OverflowException>(() =>
            counters.AddFlush(Metrics(1, 0, 0, 0, 0, 0, 0, 0, 0)));
    }

    private static CerberusFlushMetrics Metrics(
        int submissions, int merged, int vertices, int indices, int draws,
        int pipelines, int samplers, int scissors, int stencils) => new(
            submissions,
            merged,
            vertices,
            indices,
            checked(vertices * 32),
            checked(indices * sizeof(int)),
            draws,
            pipelines,
            samplers,
            scissors,
            stencils);
}
