namespace Cerneala.Timbre.Engine;

// Runtime-wide counter of reserved reader/decoder/streaming-buffer bytes.
// It only counts: nothing of the limit is allocated, so the default
// long.MaxValue imposes no numeric cap.
internal sealed class TimbreMemoryPool(long limit)
{
    private long reserved;

    internal long Limit => limit;

    internal long Reserved => Interlocked.Read(ref reserved);

    internal bool TryReserve(long bytes)
    {
        long current = Interlocked.Read(ref reserved);
        while (true)
        {
            if (bytes > limit - current)
            {
                return false;
            }

            long observed = Interlocked.CompareExchange(ref reserved, current + bytes, current);
            if (observed == current)
            {
                return true;
            }

            current = observed;
        }
    }

    internal void Release(long bytes) => Interlocked.Add(ref reserved, -bytes);
}
