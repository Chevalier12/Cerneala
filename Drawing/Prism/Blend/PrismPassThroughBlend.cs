namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismPassThroughBlend
{
    internal static PrismBlendColor Evaluate(
        PrismBlendColor backdrop,
        PrismBlendColor source) => source;
}
