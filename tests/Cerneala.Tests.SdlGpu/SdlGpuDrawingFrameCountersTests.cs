using Cerneala.Backends.SdlGpu;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuDrawingFrameCountersTests
{
    [Fact]
    public void AddFlushAggregatesSubmittedGeometry()
    {
        SdlGpuDrawingFrameCounters counters = default;

        counters = counters.AddFlush(vertices: 8, indices: 12, draws: 2);
        counters = counters.AddFlush(vertices: 4, indices: 6, draws: 1);

        Assert.Equal(2, counters.FlushCount);
        Assert.Equal(3, counters.DrawCallCount);
        Assert.Equal(12, counters.VertexCount);
        Assert.Equal(18, counters.IndexCount);
    }
}
