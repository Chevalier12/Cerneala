using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;




internal static class PrismGrainFilter
{
    private const int CandidatesPerCell = 2;
    private const float MinimumAlpha = 0.000001f;
    private const float MeanRadiusScaleSquared = 1.0408333333f;

    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        float intensity = Math.Clamp(plan.Options4.X, 0, 1);
        if (intensity <= 0)
        {
            return (Vector4[])source.Clone();
        }

        float contrast = Math.Clamp(plan.Options4.Y, 0, 1);
        int type = (int)MathF.Round(plan.Options4.Z);
        float cellSize = Math.Clamp(plan.Options4.W, 1, 256);
        float radiusX = Math.Clamp(plan.Options5.X, 0.1f, 96);
        float radiusY = Math.Clamp(plan.Options5.Y, 0.1f, 96);
        float softness = Math.Clamp(plan.Options5.Z, 0.01f, 0.49f);
        float typeGain = Math.Clamp(plan.Options5.W, 0.25f, 2);
        uint seed = DecodeSeed(plan.Options2);
        float areaRatio = Math.Clamp(
            MathF.PI * radiusX * radiusY * MeanRadiusScaleSquared /
                (cellSize * cellSize),
            0.0001f,
            0.95f);
        float exponent = float.Lerp(1.55f, 0.65f, contrast);
        float amplitude =
            intensity * float.Lerp(0.18f, 0.42f, contrast) * typeGain;
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

                Vector3 straight = Vector3.Clamp(
                    new Vector3(color.X, color.Y, color.Z) / color.W,
                    Vector3.Zero,
                    Vector3.One);
                float luminance = Vector3.Dot(
                    straight,
                    new Vector3(0.2126f, 0.7152f, 0.0722f));
                float targetOccupancy =
                    0.03f + (0.94f * (1 - luminance));
                float probability = Math.Clamp(
                    -MathF.Log(MathF.Max(1 - targetOccupancy, 0.0001f)) /
                        (CandidatesPerCell * areaRatio),
                    0,
                    1);
                float expectedOccupancy = 1 - MathF.Exp(
                    -CandidatesPerCell * probability * areaRatio);
                float occupancy = BooleanCoverage(
                    x + 0.5f,
                    y + 0.5f,
                    cellSize,
                    radiusX,
                    radiusY,
                    softness,
                    probability,
                    type,
                    seed);
                float deviation = expectedOccupancy - occupancy;
                float shaped = MathF.CopySign(
                    MathF.Pow(MathF.Abs(deviation), exponent),
                    deviation);
                Vector3 result = Vector3.Clamp(
                    straight + new Vector3(shaped * amplitude),
                    Vector3.Zero,
                    Vector3.One);
                output[index] = new Vector4(result * color.W, color.W);
            }
        }

        return output;
    }

    private static float BooleanCoverage(
        float pixelX,
        float pixelY,
        float cellSize,
        float radiusX,
        float radiusY,
        float softness,
        float probability,
        int type,
        uint seed)
    {
        int baseX = (int)MathF.Floor(pixelX / cellSize);
        int baseY = (int)MathF.Floor(pixelY / cellSize);
        float uncovered = 1;
        for (int sample = 0; sample < 9 * CandidatesPerCell; sample++)
        {
            int candidate = sample % CandidatesPerCell;
            int cellIndex = sample / CandidatesPerCell;
            int cellX = baseX + (cellIndex % 3) - 1;
            int cellY = baseY + (cellIndex / 3) - 1;
            uint candidateSeed = unchecked(
                seed ^ ((uint)candidate * 0x9e3779b9u));
            if (Random(
                    cellX,
                    cellY,
                    candidateSeed ^ 0xa511e9b3u) >= probability)
            {
                continue;
            }

            float centerX = (cellX + Random(
                cellX,
                cellY,
                candidateSeed ^ 0x63d83595u)) * cellSize;
            float centerY = (cellY + Random(
                cellX,
                cellY,
                candidateSeed ^ 0xb5297a4du)) * cellSize;
            float radiusRandom = Random(
                cellX,
                cellY,
                candidateSeed ^ 0x1b56c4e9u);
            float radiusScale = type == 3
                ? 0.45f + (1.35f * radiusRandom * radiusRandom)
                : 0.65f + (0.7f * radiusRandom);
            float dx =
                (pixelX - centerX) / (radiusX * radiusScale);
            float dy =
                (pixelY - centerY) / (radiusY * radiusScale);
            float distance = MathF.Sqrt((dx * dx) + (dy * dy));
            float coverage = 1 - SmoothStep(
                1 - softness,
                1 + softness,
                distance);
            uncovered *= 1 - coverage;
        }

        return 1 - uncovered;
    }

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

    private static float SmoothStep(float edge0, float edge1, float value)
    {
        float t = Math.Clamp((value - edge0) / (edge1 - edge0), 0, 1);
        return t * t * (3 - (2 * t));
    }

    private static uint DecodeSeed(Vector4 value) =>
        ((uint)value.Y << 16) |
        ((uint)value.X & 0xffffu);
}
