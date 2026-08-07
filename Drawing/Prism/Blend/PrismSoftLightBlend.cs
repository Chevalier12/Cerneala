namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismSoftLightBlend
{
    internal static PrismBlendColor Evaluate(
        PrismBlendColor backdrop,
        PrismBlendColor source) =>
        PrismBlendMath.Zip(backdrop, source, EvaluateChannel);

    private static double EvaluateChannel(double backdrop, double source)
    {
        if (source <= 0.5)
        {
            return backdrop -
                ((1 - (2 * source)) * backdrop * (1 - backdrop));
        }

        double curve = backdrop <= 0.25
            ? (((16 * backdrop) - 12) * backdrop + 4) * backdrop
            : Math.Sqrt(backdrop);
        return backdrop + (((2 * source) - 1) * (curve - backdrop));
    }
}
