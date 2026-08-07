using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismFragmentFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        float offset = pass.RadiusX;
        return (
            PrismCatalogFilterMath.SamplePixelBilinear(
                source, width, height, x - offset, y - offset) +
            PrismCatalogFilterMath.SamplePixelBilinear(
                source, width, height, x + offset, y - offset) +
            PrismCatalogFilterMath.SamplePixelBilinear(
                source, width, height, x - offset, y + offset) +
            PrismCatalogFilterMath.SamplePixelBilinear(
                source, width, height, x + offset, y + offset)) / 4;
    }
}
