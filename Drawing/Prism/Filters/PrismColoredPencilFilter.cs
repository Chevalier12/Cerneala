using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismColoredPencilFilter
{
    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height) =>
        PrismCatalogFilterMath.ColoredPencil(
            plan,
            source,
            width,
            height);
}
