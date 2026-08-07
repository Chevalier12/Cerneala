namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismDarkenBlend
{
    internal static PrismBlendColor Evaluate(
        PrismBlendColor backdrop,
        PrismBlendColor source) =>
        PrismBlendMath.Zip(backdrop, source, Math.Min);
}
