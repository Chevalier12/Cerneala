using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismChannelMixerFilter
{
    public static Vector3 Apply(
        PrismAdjustmentPlan plan,
        Vector3 color) =>
        PrismAdjustmentMath.ApplyMatrix(
            color,
            PrismAdjustmentMath.ToVector3(plan.Parameters0),
            PrismAdjustmentMath.ToVector3(plan.Parameters1),
            PrismAdjustmentMath.ToVector3(plan.Parameters2),
            PrismAdjustmentMath.ToVector3(plan.Parameters3));
}
