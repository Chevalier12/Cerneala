namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismLightenBlend
{
    internal static PrismBlendColor Evaluate(
        PrismBlendColor backdrop,
        PrismBlendColor source) =>
        PrismBlendMath.Zip(backdrop, source, Math.Max);
}
