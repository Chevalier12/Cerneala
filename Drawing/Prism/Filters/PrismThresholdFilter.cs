using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismThresholdFilter
{
    public static Vector3 Apply(Vector3 color, float level)
    {
        float value = PrismAdjustmentMath.Luminance(color) > level
            ? 1
            : 0;
        return new Vector3(value);
    }
}
