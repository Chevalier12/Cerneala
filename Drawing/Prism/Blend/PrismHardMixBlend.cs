namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismHardMixBlend
{
    internal static PrismBlendColor Evaluate(
        PrismBlendColor backdrop,
        PrismBlendColor source) =>
        PrismBlendMath.Zip(
            backdrop,
            source,
            (left, right) =>
                PrismVividLightBlend.EvaluateChannel(left, right) < 0.5
                    ? 0
                    : 1);
}
