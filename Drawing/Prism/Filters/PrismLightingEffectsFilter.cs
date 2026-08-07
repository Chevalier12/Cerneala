using System.Numerics;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismLightingEffectsFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        Vector4 center,
        Vector2 uv,
        int width,
        int height,
        Func<Vector2, Vector4>? heightResource,
        PrismLightingResource lighting) =>
        PrismCatalogFilterMath.LightingEffects(
            plan,
            center,
            uv,
            width,
            height,
            heightResource,
            lighting);
}
