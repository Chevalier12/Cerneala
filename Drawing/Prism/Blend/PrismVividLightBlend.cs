namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismVividLightBlend
{
    internal static PrismBlendColor Evaluate(
        PrismBlendColor backdrop,
        PrismBlendColor source) =>
        PrismBlendMath.Zip(backdrop, source, EvaluateChannel);

    internal static double EvaluateChannel(double backdrop, double source) =>
        source < 0.5
            ? PrismColorBurnBlend.EvaluateChannel(backdrop, 2 * source)
            : PrismColorDodgeBlend.EvaluateChannel(
                backdrop,
                2 * (source - 0.5));
}
