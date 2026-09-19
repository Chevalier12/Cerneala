namespace Cerneala.Drawing.Prism.Graph;

internal static class PrismFnv1aHash
{
    public const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public static ulong MixUInt32(ulong hash, uint value) =>
        unchecked((hash ^ value) * Prime);

    public static ulong MixSingleBits(ulong hash, float value) =>
        MixUInt32(hash, BitConverter.SingleToUInt32Bits(value));

    public static ulong MixInt64(ulong hash, long value) =>
        unchecked((hash ^ (ulong)value) * Prime);
}
