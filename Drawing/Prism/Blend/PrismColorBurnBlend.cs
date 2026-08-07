namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismColorBurnBlend
{
    internal static PrismBlendColor Evaluate(
        PrismBlendColor backdrop,
        PrismBlendColor source) =>
        PrismBlendMath.Zip(backdrop, source, EvaluateChannel);

    internal static double EvaluateChannel(double backdrop, double source) =>
        source <= 0
            ? 0
            : 1 - Math.Min(1, (1 - backdrop) / source);
}
