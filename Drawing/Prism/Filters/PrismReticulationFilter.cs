using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;




internal static class PrismReticulationFilter
{
    private const float MinimumAlpha = 0.000001f;

    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        float cellSize = Math.Clamp(plan.Options4.X, 2, 256);
        float foregroundLevel = Math.Clamp(plan.Options4.Y, 0, 1);
        float backgroundLevel = Math.Clamp(plan.Options4.Z, 0, 1);
        uint seed = DecodeSeed(plan.Options3);
        Vector4[] result = new Vector4[source.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                Vector4 center = source[index];
                float alpha = Math.Clamp(center.W, 0, 1);
                if (alpha <= MinimumAlpha)
                {
                    result[index] = Vector4.Zero;
                    continue;
                }

                Vector3 straight = new(
                    center.X / alpha,
                    center.Y / alpha,
                    center.Z / alpha);
                straight = Vector3.Clamp(
                    straight,
                    Vector3.Zero,
                    Vector3.One);
                float luminance = Math.Clamp(
                    Vector3.Dot(
                        straight,
                        new Vector3(0.2126f, 0.7152f, 0.0722f)),
                    0,
                    1);
                float gap = CellularGap(
                    x + 0.5f,
                    y + 0.5f,
                    cellSize,
                    seed);
                float shadowWeight = MathF.Sqrt(1 - luminance);
                float highlightWidth = float.Lerp(
                    0.018f,
                    0.075f,
                    backgroundLevel);
                float shadowWidth = float.Lerp(
                    0.055f,
                    0.24f,
                    foregroundLevel);
                float ridgeWidth = float.Lerp(
                    highlightWidth,
                    shadowWidth,
                    shadowWeight);
                float ridge = 1 - SmoothStep(0, ridgeWidth, gap);
                float inkStrength = float.Lerp(
                    backgroundLevel * 0.38f,
                    foregroundLevel * 0.95f,
                    shadowWeight);
                float paper = float.Lerp(
                    luminance,
                    1,
                    backgroundLevel * 0.1f * MathF.Sqrt(luminance));
                float outputLuminance = Math.Clamp(
                    paper * (1 - (ridge * inkStrength)),
                    0,
                    1);
                Vector3 output = luminance <= MinimumAlpha
                    ? new Vector3(outputLuminance)
                    : Vector3.Clamp(
                        straight * (outputLuminance / luminance),
                        Vector3.Zero,
                        Vector3.One);
                result[index] = new Vector4(output * alpha, alpha);
            }
        }

        return result;
    }

    private static float CellularGap(
        float x,
        float y,
        float cellSize,
        uint seed)
    {
        float patternX = x / cellSize;
        float patternY = y / cellSize;
        int baseCellX = (int)MathF.Floor(patternX);
        int baseCellY = (int)MathF.Floor(patternY);
        float nearest = float.PositiveInfinity;
        float secondNearest = float.PositiveInfinity;

        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                int cellX = baseCellX + offsetX;
                int cellY = baseCellY + offsetY;
                Vector2 feature = PrismIncrementalVoronoiSet.Center(
                    cellX,
                    cellY,
                    seed,
                    1);
                float deltaX = patternX - feature.X;
                float deltaY = patternY - feature.Y;
                float distanceSquared =
                    (deltaX * deltaX) +
                    (deltaY * deltaY);
                if (distanceSquared < nearest)
                {
                    secondNearest = nearest;
                    nearest = distanceSquared;
                }
                else if (distanceSquared < secondNearest)
                {
                    secondNearest = distanceSquared;
                }
            }
        }

        return MathF.Sqrt(secondNearest) - MathF.Sqrt(nearest);
    }

    private static float SmoothStep(
        float edge0,
        float edge1,
        float value)
    {
        float t = Math.Clamp(
            (value - edge0) / (edge1 - edge0),
            0,
            1);
        return t * t * (3 - (2 * t));
    }

    private static uint DecodeSeed(Vector4 value) =>
        ((uint)value.X & 0xffffu) |
        (((uint)value.Y & 0xffffu) << 16);
}
