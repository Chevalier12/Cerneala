using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismNtscColorsFilter
{
    public static Vector4 ApplyPixel(Vector4 center) =>
        PrismCatalogFilterMath.NtscReduceLuminance(center);
}
