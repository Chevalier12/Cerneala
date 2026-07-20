using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Drawing.Prism.Masking;

internal static class PrismMaskMath
{
    private static readonly double[] FeatherWeights =
        [1, 4, 7, 10, 12, 10, 7, 4, 1];

    public static double ResolveScalar(
        PrismPremultipliedColor sample,
        PrismMaskChannel channel,
        double density,
        bool invert)
    {
        if (!Enum.IsDefined(channel))
        {
            throw new ArgumentOutOfRangeException(
                nameof(channel),
                channel,
                "Unknown Prism mask channel.");
        }
        if (!double.IsFinite(density) || density is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(density),
                density,
                "Mask density must be in [0, 1].");
        }

        double value = channel == PrismMaskChannel.Alpha
            ? Math.Clamp(sample.Alpha, 0, 1)
            : ResolveLuminance(sample);
        if (invert)
        {
            value = 1 - value;
        }

        return 1 + ((value - 1) * density);
    }

    public static double FeatherNine(ReadOnlySpan<double> samples)
    {
        if (samples.Length != FeatherWeights.Length)
        {
            throw new ArgumentException(
                "The Prism mask feather kernel requires nine samples.",
                nameof(samples));
        }

        double result = 0;
        for (int index = 0; index < FeatherWeights.Length; index++)
        {
            result +=
                Math.Clamp(samples[index], 0, 1) *
                FeatherWeights[index];
        }
        return result / 56;
    }

    public static PrismPremultipliedColor ApplyMask(
        PrismPremultipliedColor content,
        double mask)
    {
        return Scale(content, Math.Clamp(mask, 0, 1));
    }

    public static PrismPremultipliedColor ApplyClip(
        PrismPremultipliedColor content,
        PrismPremultipliedColor clippingBase)
    {
        return Scale(
            content,
            Math.Clamp(clippingBase.Alpha, 0, 1));
    }

    private static double ResolveLuminance(
        PrismPremultipliedColor sample)
    {
        if (sample.Alpha <= 0)
        {
            return 0;
        }

        double inverseAlpha = 1 / sample.Alpha;
        return Math.Clamp(
            (sample.Red * inverseAlpha * 0.2126) +
            (sample.Green * inverseAlpha * 0.7152) +
            (sample.Blue * inverseAlpha * 0.0722),
            0,
            1);
    }

    private static PrismPremultipliedColor Scale(
        PrismPremultipliedColor color,
        double amount)
    {
        return new PrismPremultipliedColor(
            color.Red * amount,
            color.Green * amount,
            color.Blue * amount,
            color.Alpha * amount);
    }
}
