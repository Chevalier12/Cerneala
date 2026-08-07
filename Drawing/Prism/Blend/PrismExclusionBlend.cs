namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismExclusionBlend
{
    internal static PrismBlendColor Evaluate(
        PrismBlendColor backdrop,
        PrismBlendColor source) =>
        PrismBlendMath.Zip(
            backdrop,
            source,
            (left, right) => left + right - (2 * left * right));
}
