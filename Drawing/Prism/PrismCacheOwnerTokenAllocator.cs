using System.Threading;

namespace Cerneala.Drawing.Prism;

internal static class PrismCacheOwnerTokenAllocator
{
    private static long nextValue;

    public static PrismCacheOwnerToken Next()
    {
        long value = Interlocked.Increment(ref nextValue);
        if (value <= 0)
        {
            throw new InvalidOperationException(
                "Prism cache owner token space was exhausted.");
        }

        return new PrismCacheOwnerToken(value);
    }
}
