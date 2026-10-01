using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismMotionBlurFilter
{
    public static Vector4 Apply(
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        int edgeMode)
    {
        int count = Math.Max(1, pass.SampleCount);
        Vector4 total = Vector4.Zero;
        float totalWeight = 0;
        for (int index = 0; index < count; index++)
        {
            float position = count <= 1
                ? 0
                : (((float)index / (count - 1)) * 2) - 1;
            float weight = MathF.Exp(-3.125f * position * position);
            float sampleX = x + (pass.RadiusX * position);
            float sampleY = y + (pass.RadiusY * position);
            total += PrismNeighborhoodMath.SampleBilinear(
                source,
                width,
                height,
                sampleX,
                sampleY,
                edgeMode) * weight;
            totalWeight += weight;
        }
        return total / MathF.Max(totalWeight, 0.000001f);
    }
}
