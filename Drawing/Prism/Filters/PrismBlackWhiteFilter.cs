using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismBlackWhiteFilter
{
    public static Vector3 Apply(
        PrismAdjustmentPlan plan,
        Vector3 color)
    {
        Vector3 multipliers =
            PrismAdjustmentMath.ToVector3(plan.Parameters0);
        float normalization = 1;
        if (plan.Parameters0.W > 0.5f)
        {
            float sum = multipliers.X + multipliers.Y + multipliers.Z;
            if (sum != 0)
            {
                normalization = MathF.Abs(1 / sum);
            }
        }

        float gray = Vector3.Dot(color, multipliers) * normalization;
        return new Vector3(gray);
    }
}
