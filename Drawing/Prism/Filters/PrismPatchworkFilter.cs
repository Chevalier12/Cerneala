using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;



internal static class PrismPatchworkFilter
{
    private const float MinimumAlpha = 0.000001f;

    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        float squareSize = MathF.Max(
            plan.GetOption("SquareSize").X,
            1);
        float relief = Math.Clamp(
            plan.GetOption("Relief").X,
            0,
            1);
        uint seed = UnpackInteger(plan.GetOption("Seed"));
        Vector4[] result = new Vector4[source.Length];

        for (int y = 0; y < height; y++)
        {
            float pixelY = y + 0.5f;
            int cellY = (int)MathF.Floor(pixelY / squareSize);
            float localY = pixelY - (cellY * squareSize);
            float sampleY = ((cellY + 0.5f) * squareSize) - 0.5f;
            for (int x = 0; x < width; x++)
            {
                float pixelX = x + 0.5f;
                int cellX = (int)MathF.Floor(pixelX / squareSize);
                float localX = pixelX - (cellX * squareSize);
                float sampleX = ((cellX + 0.5f) * squareSize) - 0.5f;
                Vector4 tile = PrismCatalogFilterMath.SamplePixelBilinear(
                    source,
                    width,
                    height,
                    sampleX,
                    sampleY);
                result[(y * width) + x] = ShadeTile(
                    tile,
                    squareSize,
                    localX,
                    localY,
                    cellX,
                    cellY,
                    relief,
                    seed);
            }
        }

        return result;
    }

    private static Vector4 ShadeTile(
        Vector4 tile,
        float squareSize,
        float localX,
        float localY,
        int cellX,
        int cellY,
        float relief,
        uint seed)
    {
        if (tile.W <= MinimumAlpha || relief <= 0)
        {
            return tile;
        }

        float depth = (Hash(
            cellX,
            cellY,
            seed ^ 0xa511e9b3u) * 2) - 1;
        float normalizedX = ((localX / squareSize) * 2) - 1;
        float normalizedY = ((localY / squareSize) * 2) - 1;
        float edge = SmoothStep(
            0.5f,
            1,
            MathF.Max(MathF.Abs(normalizedX), MathF.Abs(normalizedY)));
        float directional = -(normalizedX + normalizedY) * 0.5f;
        float depthSign = depth >= 0 ? 1 : -1;
        float bevelScale = 0.5f + (MathF.Abs(depth) * 0.5f);
        float shade = relief * (
            (depth * 0.18f) +
            (directional * edge * depthSign * bevelScale * 0.32f));
        Vector3 straight = new(tile.X, tile.Y, tile.Z);
        straight /= tile.W;
        straight = Vector3.Clamp(
            straight + new Vector3(shade),
            Vector3.Zero,
            Vector3.One);
        return new Vector4(straight * tile.W, tile.W);
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

    private static float Hash(int x, int y, uint seed)
    {
        uint value =
            unchecked((uint)x * 0x9e3779b9u) ^
            unchecked((uint)y * 0x85ebca6bu) ^
            seed;
        value ^= value >> 16;
        value *= 0x7feb352du;
        value ^= value >> 15;
        value *= 0x846ca68bu;
        value ^= value >> 16;
        return (value & 0x00ffffffu) / 16777215f;
    }

    private static uint UnpackInteger(Vector4 value) =>
        ((uint)value.Y << 16) |
        ((uint)value.X & 0xffffu);
}
