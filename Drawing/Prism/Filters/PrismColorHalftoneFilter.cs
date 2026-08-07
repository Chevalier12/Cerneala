using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismColorHalftoneFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4 center,
        int x,
        int y) =>
        PrismCatalogFilterMath.ColorHalftone(
            plan,
            center,
            new Vector2(x + 0.5f, y + 0.5f),
            pass.RadiusX);
}
