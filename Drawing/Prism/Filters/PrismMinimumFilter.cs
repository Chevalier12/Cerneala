using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismMinimumFilter
{
    public static Vector4[] ApplyPass(
        Vector4[] source,
        int width,
        int height,
        PrismCatalogFilterPass pass) =>
        pass.Iteration == 1
            ? PrismArbitraryFlatMorphology.ErodeSquare(
                source,
                width,
                height,
                pass.RadiusX,
                pass.RadiusY)
            : PrismArbitraryFlatMorphology.ErodeRound(
                source,
                width,
                height,
                pass.RadiusX,
                pass.RadiusY);
}
