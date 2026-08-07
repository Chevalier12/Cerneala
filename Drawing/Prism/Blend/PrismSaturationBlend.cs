namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismSaturationBlend
{
    internal static PrismBlendColor Evaluate(
        PrismBlendColor backdrop,
        PrismBlendColor source) =>
        PrismBlendMath.SetLuminosity(
            PrismBlendMath.SetSaturation(
                backdrop,
                PrismBlendMath.Saturation(source)),
            PrismBlendMath.Luminosity(backdrop));
}
