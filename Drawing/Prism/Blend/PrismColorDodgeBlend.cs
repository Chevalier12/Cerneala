namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismColorDodgeBlend
{
    internal static PrismBlendColor Evaluate(
        PrismBlendColor backdrop,
        PrismBlendColor source) =>
        PrismBlendMath.Zip(backdrop, source, EvaluateChannel);

    internal static double EvaluateChannel(double backdrop, double source) =>
        source >= 1
            ? 1
            : Math.Min(1, backdrop / (1 - source));
}
