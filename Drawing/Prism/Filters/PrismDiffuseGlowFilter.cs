using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismDiffuseGlowFilter
{
    public static Vector4 ApplyPass(
        PrismResamplingPlan plan,
        PrismResamplingPass pass,
        Vector4[] source,
        Vector4[] original,
        int width,
        int height,
        int x,
        int y,
        Vector4 center) =>
        pass.Kind switch
        {
            PrismResamplingPassKind.BloomExtractHorizontal =>
                PrismResamplingMath.BloomHorizontal(
                    plan, source, width, height, x, y),
            PrismResamplingPassKind.BloomVerticalComposite =>
                PrismResamplingMath.BloomVerticalComposite(
                    plan, source, original, width, height, x, y),
            PrismResamplingPassKind.Grain =>
                PrismResamplingMath.Grain(plan, center, x, y),
            _ => center
        };
}
