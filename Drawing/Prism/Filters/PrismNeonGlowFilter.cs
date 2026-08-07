using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismNeonGlowFilter
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
        Vector2 uv,
        Vector4 center,
        PrismResamplingMath.MipLevel[]? mipChain) =>
        pass.Kind switch
        {
            PrismResamplingPassKind.NeonEdgeExtract =>
                PrismResamplingMath.NeonGlowEdge(
                    source, width, height, x, y),
            PrismResamplingPassKind.NeonBlurHorizontal =>
                PrismResamplingMath.GaussianAxis(
                    plan,
                    source,
                    width,
                    height,
                    x,
                    y,
                    plan.Options0.Y,
                    horizontal: true,
                    brightPass: false),
            PrismResamplingPassKind.NeonBlurVertical =>
                PrismResamplingMath.GaussianAxis(
                    plan,
                    source,
                    width,
                    height,
                    x,
                    y,
                    plan.Options0.Y,
                    horizontal: false,
                    brightPass: false),
            PrismResamplingPassKind.NeonPyramidComposite
                when mipChain is not null =>
                PrismResamplingMath.NeonGlowPyramidComposite(
                    plan,
                    original[(y * width) + x],
                    mipChain,
                    uv),
            _ => center
        };
}
