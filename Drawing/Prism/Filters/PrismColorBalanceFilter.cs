using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismColorBalanceFilter
{
    public static Vector3 Apply(
        PrismAdjustmentPlan plan,
        Vector3 color)
    {
        float luminance = PrismAdjustmentMath.Luminance(color);
        float shadows = 1 - PrismAdjustmentMath.SmoothStep(
            0,
            0.333f,
            luminance);
        float highlights = PrismAdjustmentMath.SmoothStep(
            0.550f,
            1,
            luminance);
        float midtones = 1 - shadows - highlights;
        Vector3 adjusted = color +
            (PrismAdjustmentMath.ToVector3(plan.Parameters0) * shadows) +
            (PrismAdjustmentMath.ToVector3(plan.Parameters1) * midtones) +
            (PrismAdjustmentMath.ToVector3(plan.Parameters2) * highlights);
        return plan.Parameters3.X > 0.5f
            ? PreserveLightness(color, adjusted)
            : adjusted;
    }

    private static Vector3 PreserveLightness(
        Vector3 source,
        Vector3 adjusted)
    {
        Vector3 sourceHsl = PrismOkhsl.FromLinearSrgb(source);
        Vector3 adjustedHsl = PrismOkhsl.FromLinearSrgb(adjusted);
        adjustedHsl.Z = sourceHsl.Z;
        return PrismOkhsl.ToLinearSrgb(adjustedHsl);
    }
}
