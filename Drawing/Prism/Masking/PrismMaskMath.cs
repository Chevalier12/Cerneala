using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Drawing.Prism.Masking;

internal static class PrismMaskMath
{
    public static double ResolveScalar(
        PrismPremultipliedColor sample,
        PrismMaskChannel channel,
        double density,
        bool invert) =>
        PrismMaskStyle.ResolveScalar(sample, channel, density, invert);

    public static double FeatherNine(ReadOnlySpan<double> samples)
        => PrismMaskStyle.FeatherNine(samples);

    public static PrismPremultipliedColor ApplyMask(
        PrismPremultipliedColor content,
        double mask)
        => PrismMaskStyle.Apply(content, mask);

    public static PrismPremultipliedColor ApplyClip(
        PrismPremultipliedColor content,
        PrismPremultipliedColor clippingBase)
        => PrismClippingStyle.Apply(content, clippingBase);

    internal static PrismPremultipliedColor Scale(
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
