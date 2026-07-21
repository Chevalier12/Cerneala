namespace Cerneala.Drawing.MonoGame.Prism.Surfaces;

internal sealed class PrismSurfaceAllocationException :
    InvalidOperationException
{
    public PrismSurfaceAllocationException(
        PrismSurfaceKey key,
        long requestedByteCount,
        long currentByteCount,
        long hardByteLimit,
        Exception innerException)
        : base(
            $"A Prism surface could not be allocated for key '{key}': " +
            $"requestedBytes={requestedByteCount}, " +
            $"currentBytes={currentByteCount}, " +
            $"hardByteLimit={hardByteLimit}.",
            innerException)
    {
        Key = key;
        RequestedByteCount = requestedByteCount;
        CurrentByteCount = currentByteCount;
        HardByteLimit = hardByteLimit;
    }

    public PrismSurfaceKey Key { get; }

    public long RequestedByteCount { get; }

    public long CurrentByteCount { get; }

    public long HardByteLimit { get; }
}
