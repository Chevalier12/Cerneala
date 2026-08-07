using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismCurvesFilter
{
    public static Vector3 Apply(
        Vector3 color,
        Func<Vector3, Vector3>? lookup) =>
        (lookup ?? throw new InvalidOperationException(
            "Curves requires an analytic LUT callback."))(color);
}
