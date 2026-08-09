using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismPosterEdgesFilter
{
    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height) =>
        PrismCatalogInkMath.PosterEdges(plan, source, width, height);
}
