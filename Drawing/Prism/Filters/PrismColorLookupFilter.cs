using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismColorLookupFilter
{
    public static Vector3 Apply(
        Vector3 color,
        Func<Vector3, Vector3>? lookup,
        PrismHaldLut? haldLookup) =>
        haldLookup is null
            ? (lookup ?? throw new InvalidOperationException(
                "ColorLookup requires a Hald LUT or an analytic LUT callback."))(
                    PrismAdjustmentMath.Clamp01(color))
            : haldLookup.Sample(
                PrismAdjustmentMath.Clamp01(color),
                PrismHaldInterpolation.Trilinear);

    internal static Vector3 ApplyLookup(
        Vector3 color,
        Func<Vector3, Vector3> lookup,
        float intensity) =>
        Vector3.Lerp(
            color,
            lookup(PrismAdjustmentMath.Clamp01(color)),
            intensity);
}
