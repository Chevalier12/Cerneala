namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismPinLightBlend
{
    internal static PrismBlendColor Evaluate(
        PrismBlendColor backdrop,
        PrismBlendColor source) =>
        PrismBlendMath.Zip(backdrop, source, EvaluateChannel);

    private static double EvaluateChannel(double backdrop, double source) =>
        source < 0.5
            ? Math.Min(backdrop, 2 * source)
            : Math.Max(backdrop, (2 * source) - 1);
}
