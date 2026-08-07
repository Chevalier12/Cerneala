namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismOverlayBlend
{
    internal static PrismBlendColor Evaluate(
        PrismBlendColor backdrop,
        PrismBlendColor source) =>
        PrismBlendMath.Zip(backdrop, source, EvaluateChannel);

    internal static double EvaluateChannel(double backdrop, double source) =>
        backdrop <= 0.5
            ? 2 * backdrop * source
            : 1 - (2 * (1 - backdrop) * (1 - source));
}
