namespace Cerneala.Backends.SdlGpu;

internal readonly record struct SdlGpuDrawingFrameCounters(
    int FlushCount,
    int DrawCallCount,
    int VertexCount,
    int IndexCount)
{
    public SdlGpuDrawingFrameCounters AddFlush(int vertices, int indices, int draws)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(vertices);
        ArgumentOutOfRangeException.ThrowIfNegative(indices);
        ArgumentOutOfRangeException.ThrowIfNegative(draws);
        return new SdlGpuDrawingFrameCounters(
            checked(FlushCount + 1),
            checked(DrawCallCount + draws),
            checked(VertexCount + vertices),
            checked(IndexCount + indices));
    }
}
