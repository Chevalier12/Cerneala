using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismPhotoFilterFilter
{
    public static Vector3 Apply(
        PrismAdjustmentPlan plan,
        Vector3 color) =>
        Vector3.Lerp(
            color,
            PrismAdjustmentMath.ToVector3(plan.Parameters0),
            plan.Parameters1.X);
}
