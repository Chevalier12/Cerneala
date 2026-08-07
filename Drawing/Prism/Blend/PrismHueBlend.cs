namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismHueBlend
{
    internal static PrismBlendColor Evaluate(
        PrismBlendColor backdrop,
        PrismBlendColor source) =>
        PrismBlendMath.SetLuminosity(
            PrismBlendMath.SetSaturation(
                source,
                PrismBlendMath.Saturation(backdrop)),
            PrismBlendMath.Luminosity(backdrop));
}
