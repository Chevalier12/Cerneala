using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismLiquifyFilter
{
    public static Vector2 Map(
        PrismResamplingPlan plan,
        Vector2 uv,
        Vector4 mesh,
        Vector4? maskSample) =>
        PrismResamplingMath.MapLiquify(
            plan,
            uv,
            mesh,
            maskSample);

    public static Vector4 Sample(
        PrismResamplingPlan plan,
        Vector4[] source,
        int width,
        int height,
        Vector2 uv,
        Vector2 mapped,
        int edgeMode,
        Vector4 fill,
        Func<Vector2, Vector4>? primaryResource,
        Func<Vector2, Vector4>? auxiliaryResource) =>
        PrismResamplingMath.SampleLiquify(
            plan,
            source,
            width,
            height,
            uv,
            mapped,
            edgeMode,
            fill,
            primaryResource,
            auxiliaryResource);
}
