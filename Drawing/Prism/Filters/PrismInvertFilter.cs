using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismInvertFilter
{
    public static Vector3 Apply(Vector3 color) =>
        Vector3.One - color;
}
