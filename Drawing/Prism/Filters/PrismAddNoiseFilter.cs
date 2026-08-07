using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismAddNoiseFilter
{
    public static Vector4 Apply(
        PrismNeighborhoodPlan plan,
        Vector4 center,
        int x,
        int y) =>
        PrismNeighborhoodMath.AddNoise(plan, center, x, y);
}
