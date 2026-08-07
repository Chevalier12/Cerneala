namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismColorBlend
{
    internal static PrismBlendColor Evaluate(
        PrismBlendColor backdrop,
        PrismBlendColor source) =>
        PrismBlendMath.SetLuminosity(
            source,
            PrismBlendMath.Luminosity(backdrop));
}
