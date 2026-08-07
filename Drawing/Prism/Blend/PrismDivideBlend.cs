namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismDivideBlend
{
    internal static PrismBlendColor Evaluate(
        PrismBlendColor backdrop,
        PrismBlendColor source) =>
        PrismBlendMath.Zip(
            backdrop,
            source,
            (left, right) => right <= 0 ? 1 : left / right);
}
