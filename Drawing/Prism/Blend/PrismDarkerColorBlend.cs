namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismDarkerColorBlend
{
    internal static PrismBlendColor Evaluate(
        PrismBlendColor backdrop,
        PrismBlendColor source) =>
        PrismBlendMath.Luminosity(backdrop) <= PrismBlendMath.Luminosity(source)
            ? backdrop
            : source;
}
