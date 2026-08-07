namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismDissolveBlend
{
    internal const int ThresholdSize = 256;

    private const string ThresholdResourceName =
        "Cerneala.Drawing.Prism.Blending.Assets.dissolve-fastnoise-ranks.bin";

    private static readonly Lazy<byte[]> CachedThresholds =
        new(LoadThresholds, LazyThreadSafetyMode.ExecutionAndPublication);

    internal static ReadOnlySpan<byte> Thresholds =>
        CachedThresholds.Value;

    internal static int NormalizeSeed(int dissolveSeed, int layerIdentity)
    {
        unchecked
        {
            uint hash = 2166136261;
            hash = (hash ^ (uint)dissolveSeed) * 16777619;
            hash = (hash ^ (uint)layerIdentity) * 16777619;
            return (int)(hash & 0xffff);
        }
    }

    internal static bool IsSelected(
        int pixelX,
        int pixelY,
        int normalizedSeed,
        double alpha)
    {
        return (ThresholdAt(
            pixelX,
            pixelY,
            normalizedSeed) / 256d) < alpha;
    }

    internal static byte ThresholdAt(
        int pixelX,
        int pixelY,
        int normalizedSeed)
    {
        int shiftedX = unchecked(
            pixelX + (normalizedSeed & 0xff)) & 0xff;
        int shiftedY = unchecked(
            pixelY + ((normalizedSeed >> 8) & 0xff)) & 0xff;
        return Thresholds[
            (shiftedY * ThresholdSize) + shiftedX];
    }

    private static byte[] LoadThresholds()
    {
        using Stream stream = typeof(PrismDissolveBlend)
            .Assembly
            .GetManifestResourceStream(ThresholdResourceName) ??
            throw new InvalidOperationException(
                $"Embedded Dissolve threshold map '{ThresholdResourceName}' is missing.");
        byte[] thresholds = new byte[
            ThresholdSize * ThresholdSize];
        stream.ReadExactly(thresholds);
        if (stream.ReadByte() >= 0)
        {
            throw new InvalidDataException(
                "The embedded Dissolve threshold map has trailing data.");
        }

        int[] histogram = new int[ThresholdSize];
        foreach (byte threshold in thresholds)
        {
            histogram[threshold]++;
        }
        if (histogram.Any(count => count != ThresholdSize))
        {
            throw new InvalidDataException(
                "The embedded Dissolve threshold map is not rank-normalized.");
        }

        return thresholds;
    }
}
