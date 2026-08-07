using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;



internal static class PrismCraquelureFilter
{
    private const float MinimumAlpha = 0.000001f;
    private const float Diagonal = 0.70710678118f;

    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        float cellSize = Math.Clamp(plan.Options4.X, 2, 256);
        float crackWidth = Math.Clamp(plan.Options4.Y, 0.01f, 0.25f);
        float depth = Math.Clamp(plan.Options4.Z, 0, 1);
        float brightness = Math.Clamp(plan.Options4.W, 0, 1);
        uint seed = DecodeSeed(plan.Options3);
        float antialias = Math.Clamp(0.75f / cellSize, 0.0025f, 0.25f);
        float smoothness = crackWidth * 0.35f;
        float rimNear = crackWidth + (antialias * 0.25f);
        float rimPeak = crackWidth + MathF.Max(0.012f, antialias * 1.25f);
        float rimFar = crackWidth + MathF.Max(0.06f, antialias * 3);
        float shadowStrength =
            (0.18f + (0.7f * depth)) *
            (0.55f + (0.45f * brightness));
        float highlightStrength =
            (0.02f + (0.18f * brightness)) *
            (0.4f + (0.6f * depth));
        Vector4[] output = new Vector4[source.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                Vector4 color = source[index];
                if (color.W <= MinimumAlpha)
                {
                    output[index] = Vector4.Zero;
                    continue;
                }

                Vector2 pattern = new(
                    (x + 0.5f) / cellSize,
                    (y + 0.5f) / cellSize);
                pattern += DomainWarp(pattern, seed);
                float edgeDistance = VoronoiEdgeDistance(pattern, seed);
                float crack = 1 - SmoothStep(
                    MathF.Max(crackWidth - smoothness, 0),
                    crackWidth + antialias,
                    edgeDistance);
                float rim = SmoothStep(rimNear, rimPeak, edgeDistance) *
                    (1 - SmoothStep(rimPeak, rimFar, edgeDistance));
                Vector3 straight = new(
                    color.X / color.W,
                    color.Y / color.W,
                    color.Z / color.W);
                straight = Vector3.Clamp(
                    (straight * (1 - (crack * shadowStrength))) +
                        new Vector3(rim * highlightStrength),
                    Vector3.Zero,
                    Vector3.One);
                output[index] = new Vector4(straight * color.W, color.W);
            }
        }

        return output;
    }

    private static Vector2 DomainWarp(Vector2 pattern, uint seed)
    {
        Vector2 position = pattern * 0.55f;
        float x = GradientNoise(
            position + new Vector2(19.1f, 7.7f),
            seed ^ 0x68bc21ebu);
        float y = GradientNoise(
            position + new Vector2(-5.4f, 23.6f),
            seed ^ 0x02e5be93u);
        return new Vector2(x, y) * 0.28f;
    }

    private static float GradientNoise(Vector2 position, uint seed)
    {
        int cellX = (int)MathF.Floor(position.X);
        int cellY = (int)MathF.Floor(position.Y);
        float localX = position.X - cellX;
        float localY = position.Y - cellY;
        float fadeX = Fade(localX);
        float fadeY = Fade(localY);
        float upper = float.Lerp(
            Vector2.Dot(
                Gradient(cellX, cellY, seed),
                new Vector2(localX, localY)),
            Vector2.Dot(
                Gradient(cellX + 1, cellY, seed),
                new Vector2(localX - 1, localY)),
            fadeX);
        float lower = float.Lerp(
            Vector2.Dot(
                Gradient(cellX, cellY + 1, seed),
                new Vector2(localX, localY - 1)),
            Vector2.Dot(
                Gradient(cellX + 1, cellY + 1, seed),
                new Vector2(localX - 1, localY - 1)),
            fadeX);
        return float.Lerp(upper, lower, fadeY) * 1.41421356237f;
    }

    private static Vector2 Gradient(int x, int y, uint seed) =>
        (Hash(x, y, seed) & 7u) switch
        {
            0 => new Vector2(1, 0),
            1 => new Vector2(-1, 0),
            2 => new Vector2(0, 1),
            3 => new Vector2(0, -1),
            4 => new Vector2(Diagonal, Diagonal),
            5 => new Vector2(-Diagonal, Diagonal),
            6 => new Vector2(Diagonal, -Diagonal),
            _ => new Vector2(-Diagonal, -Diagonal)
        };

    private static float VoronoiEdgeDistance(
        Vector2 pattern,
        uint seed)
    {
        int baseX = (int)MathF.Floor(pattern.X);
        int baseY = (int)MathF.Floor(pattern.Y);
        float nearestDistanceSquared = float.PositiveInfinity;
        Vector2 nearest = default;
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                Vector2 relative = Feature(
                    baseX + offsetX,
                    baseY + offsetY,
                    seed) - pattern;
                float distanceSquared = relative.LengthSquared();
                if (distanceSquared >= nearestDistanceSquared)
                {
                    continue;
                }

                nearestDistanceSquared = distanceSquared;
                nearest = relative;
            }
        }

        float edgeDistance = float.PositiveInfinity;
        for (int offsetY = -2; offsetY <= 2; offsetY++)
        {
            for (int offsetX = -2; offsetX <= 2; offsetX++)
            {
                Vector2 relative = Feature(
                    baseX + offsetX,
                    baseY + offsetY,
                    seed) - pattern;
                Vector2 between = relative - nearest;
                float lengthSquared = between.LengthSquared();
                if (lengthSquared <= 0.00001f)
                {
                    continue;
                }

                Vector2 normal = between / MathF.Sqrt(lengthSquared);
                edgeDistance = MathF.Min(
                    edgeDistance,
                    Vector2.Dot((nearest + relative) * 0.5f, normal));
            }
        }

        return MathF.Max(edgeDistance, 0);
    }

    private static Vector2 Feature(int cellX, int cellY, uint seed) =>
        new(
            cellX + 0.5f +
                (0.85f * (Random(cellX, cellY, seed ^ 0x13579bdfu) - 0.5f)),
            cellY + 0.5f +
                (0.85f * (Random(cellX, cellY, seed ^ 0x2468ace0u) - 0.5f)));

    private static float Random(int x, int y, uint seed) =>
        (Hash(x, y, seed) & 0x00ffffffu) / 16777215f;

    private static uint Hash(int x, int y, uint seed)
    {
        uint value = unchecked(
            ((uint)x * 0x9e3779b9u) ^
            ((uint)y * 0x85ebca6bu) ^
            seed);
        value ^= value >> 16;
        value = unchecked(value * 0x7feb352du);
        value ^= value >> 15;
        value = unchecked(value * 0x846ca68bu);
        value ^= value >> 16;
        return value;
    }

    private static float Fade(float value) =>
        value * value * value *
        ((value * ((value * 6) - 15)) + 10);

    private static float SmoothStep(
        float edge0,
        float edge1,
        float value)
    {
        float t = Math.Clamp((value - edge0) / (edge1 - edge0), 0, 1);
        return t * t * (3 - (2 * t));
    }

    private static uint DecodeSeed(Vector4 value) =>
        ((uint)value.Y << 16) |
        ((uint)value.X & 0xffffu);
}
