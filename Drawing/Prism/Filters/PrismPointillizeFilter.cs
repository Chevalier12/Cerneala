using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismPointillizeFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y) =>
        PrismCatalogFilterMath.Pointillize(
            source,
            width,
            height,
            x,
            y,
            MathF.Max(
                1,
                PrismCatalogFilterMath.Option(
                    plan,
                    "CellSize",
                    10)),
            PrismCatalogFilterMath.Seed(plan, "Seed"),
            PrismCatalogFilterMath.AssociatedColor(
                PrismCatalogFilterMath.OptionVector(
                    plan,
                    "Background",
                    Vector4.Zero),
                1));
}
