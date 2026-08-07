using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismMaximumFilter
{
    public static Vector4[] ApplyPass(
        Vector4[] source,
        int width,
        int height,
        PrismCatalogFilterPass pass) =>
        PrismArbitraryFlatMorphology.DilateRound(
            source,
            width,
            height,
            pass.RadiusX,
            pass.RadiusY);
}
