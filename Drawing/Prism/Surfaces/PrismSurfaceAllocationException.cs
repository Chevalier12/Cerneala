namespace Cerneala.Drawing.Prism.Surfaces;

internal sealed class PrismSurfaceAllocationException :
    InvalidOperationException
{
    public PrismSurfaceAllocationException(
        string surfaceKey,
        long requestedByteCount,
        long currentByteCount,
        long hardByteLimit,
        Exception innerException)
        : base(
            $"A Prism surface could not be allocated for key '{surfaceKey}': " +
            $"requestedBytes={requestedByteCount}, " +
            $"currentBytes={currentByteCount}, " +
            $"hardByteLimit={hardByteLimit}.",
            innerException)
    {
        SurfaceKey = surfaceKey;
        RequestedByteCount = requestedByteCount;
        CurrentByteCount = currentByteCount;
        HardByteLimit = hardByteLimit;
    }

    public string SurfaceKey { get; }

    public long RequestedByteCount { get; }

    public long CurrentByteCount { get; }

    public long HardByteLimit { get; }
}
