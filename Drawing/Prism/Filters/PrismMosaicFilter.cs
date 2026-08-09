using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismMosaicFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 cell = PrismCatalogFilterMath.OptionVector(
            plan,
            "CellSize",
            new Vector4(8, 8, 0, 0));
        float cellX = MathF.Max(1, cell.X);
        float cellY = MathF.Max(1, cell.Y);
        if (PrismCatalogFilterMath.Option(
                plan,
                "PreserveEdges",
                0) >= 0.5f)
        {
            return PrismCatalogQuantizationMath.BilateralMosaic(
                source,
                width,
                height,
                x,
                y,
                cellX,
                cellY);
        }
        return PrismCatalogFilterMath.SamplePixel(
            source,
            width,
            height,
            (MathF.Floor(x / cellX) + 0.5f) * cellX,
            (MathF.Floor(y / cellY) + 0.5f) * cellY);
    }
}
