using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismDisplaceFilter
{
    public static Vector2 Map(
        PrismResamplingPlan plan,
        Vector2 uv,
        Vector4 map,
        int width,
        int height) =>
        PrismResamplingMath.MapDisplace(
            plan,
            uv,
            map,
            width,
            height);

    public static Vector2 MapResourceCoordinate(
        PrismResamplingPlan plan,
        Vector2 uv,
        int width,
        int height,
        Vector2 resourceSize) =>
        PrismResamplingMath.MapDisplaceResourceCoordinate(
            plan,
            uv,
            width,
            height,
            resourceSize);
}
