namespace Cerneala.Backends.SdlGpu;

internal readonly record struct SdlGpuDrawingFrameCounters(
    int FlushCount,
    int SubmissionCount,
    int MergedSubmissionCount,
    int DrawCallCount,
    int VertexCount,
    int IndexCount,
    long VertexBytes,
    long IndexBytes,
    int PipelineBindCount,
    int SamplerBindCount,
    int ScissorSetCount,
    int StencilReferenceSetCount)
{
    public SdlGpuDrawingFrameCounters AddFlush(CerberusFlushMetrics metrics)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(metrics.SubmissionCount);
        ArgumentOutOfRangeException.ThrowIfNegative(metrics.MergedSubmissionCount);
        ArgumentOutOfRangeException.ThrowIfNegative(metrics.VertexCount);
        ArgumentOutOfRangeException.ThrowIfNegative(metrics.IndexCount);
        ArgumentOutOfRangeException.ThrowIfNegative(metrics.VertexBytes);
        ArgumentOutOfRangeException.ThrowIfNegative(metrics.IndexBytes);
        ArgumentOutOfRangeException.ThrowIfNegative(metrics.DrawCallCount);
        ArgumentOutOfRangeException.ThrowIfNegative(metrics.PipelineBindCount);
        ArgumentOutOfRangeException.ThrowIfNegative(metrics.SamplerBindCount);
        ArgumentOutOfRangeException.ThrowIfNegative(metrics.ScissorSetCount);
        ArgumentOutOfRangeException.ThrowIfNegative(metrics.StencilReferenceSetCount);
        return new SdlGpuDrawingFrameCounters(
            checked(FlushCount + 1),
            checked(SubmissionCount + metrics.SubmissionCount),
            checked(MergedSubmissionCount + metrics.MergedSubmissionCount),
            checked(DrawCallCount + metrics.DrawCallCount),
            checked(VertexCount + metrics.VertexCount),
            checked(IndexCount + metrics.IndexCount),
            checked(VertexBytes + metrics.VertexBytes),
            checked(IndexBytes + metrics.IndexBytes),
            checked(PipelineBindCount + metrics.PipelineBindCount),
            checked(SamplerBindCount + metrics.SamplerBindCount),
            checked(ScissorSetCount + metrics.ScissorSetCount),
            checked(StencilReferenceSetCount + metrics.StencilReferenceSetCount));
    }
}
