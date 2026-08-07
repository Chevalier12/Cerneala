using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal readonly record struct PrismLevelsRange(
    float InputBlack,
    float InputWhite);

internal static class PrismLevelsAnalysis
{
    internal const int BinCount = 256;
    internal const float DefaultClippedFraction = 0.001f;

    public static PrismLevelsRange Calculate(
        ReadOnlySpan<Vector3> pixels,
        int channel,
        float clippedFraction = DefaultClippedFraction)
    {
        if ((uint)channel > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(channel));
        }
        if (!float.IsFinite(clippedFraction) ||
            clippedFraction is < 0 or >= 0.5f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(clippedFraction));
        }
        if (pixels.IsEmpty)
        {
            return new PrismLevelsRange(0, 1);
        }

        Span<int> histogram = stackalloc int[BinCount];
        for (int index = 0; index < pixels.Length; index++)
        {
            float value = ChannelValue(pixels[index], channel);
            int bin = (int)MathF.Round(
                Math.Clamp(value, 0, 1) * (BinCount - 1));
            histogram[bin]++;
        }

        double clippedCount = pixels.Length * clippedFraction;
        int cumulative = 0;
        int blackBin = 0;
        for (; blackBin < BinCount; blackBin++)
        {
            cumulative += histogram[blackBin];
            if (cumulative > clippedCount)
            {
                break;
            }
        }

        double whiteTarget = pixels.Length - clippedCount;
        cumulative = 0;
        int whiteBin = BinCount - 1;
        for (int bin = 0; bin < BinCount; bin++)
        {
            cumulative += histogram[bin];
            if (cumulative >= whiteTarget)
            {
                whiteBin = bin;
                break;
            }
        }

        if (blackBin >= whiteBin)
        {
            return new PrismLevelsRange(0, 1);
        }

        float scale = 1f / (BinCount - 1);
        return new PrismLevelsRange(
            blackBin * scale,
            whiteBin * scale);
    }

    private static float ChannelValue(
        Vector3 color,
        int channel) =>
        channel switch
        {
            1 => color.X,
            2 => color.Y,
            3 => color.Z,
            _ => Vector3.Dot(
                color,
                new Vector3(0.2126f, 0.7152f, 0.0722f))
        };
}
