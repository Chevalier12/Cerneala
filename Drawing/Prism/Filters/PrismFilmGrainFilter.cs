using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismFilmGrainFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        Vector4 center,
        int x,
        int y) =>
        PrismCatalogFilterMath.FilmGrain(plan, center, x, y);
}
