using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismShapeBlurFilter
{
    public static Vector4 Apply(
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        int edgeMode,
        Func<Vector2, Vector4> resource)
    {
        int count = Math.Max(1, pass.SampleCount);
        Vector4 total = Vector4.Zero;
        float totalWeight = 0;
        for (int kernelY = 0; kernelY < count; kernelY++)
        {
            float v = (kernelY + 0.5f) / count;
            float offsetY = count == 1
                ? 0
                : (((float)kernelY / (count - 1)) * 2 - 1) * pass.RadiusY;
            for (int kernelX = 0; kernelX < count; kernelX++)
            {
                float u = (kernelX + 0.5f) / count;
                float offsetX = count == 1
                    ? 0
                    : (((float)kernelX / (count - 1)) * 2 - 1) * pass.RadiusX;
                float weight = MathF.Max(0, resource(new Vector2(u, v)).W);
                total += PrismNeighborhoodMath.SampleBilinear(
                    source,
                    width,
                    height,
                    x - offsetX,
                    y - offsetY,
                    edgeMode) * weight;
                totalWeight += weight;
            }
        }

        return totalWeight > 0.000001f
            ? total / totalWeight
            : source[(y * width) + x];
    }
}
