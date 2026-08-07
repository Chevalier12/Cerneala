namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismLuminosityBlend
{
    internal static PrismBlendColor Evaluate(
        PrismBlendColor backdrop,
        PrismBlendColor source) =>
        PrismBlendMath.SetLuminosity(
            backdrop,
            PrismBlendMath.Luminosity(source));
}
