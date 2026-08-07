using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismTilesFilter
{
    private const float UintScale = 1f / 4294967296f;

    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        float tileCount = Math.Clamp(
            MathF.Round(plan.GetOption("Tiles").X),
            1,
            16384);
        float cellSize = MathF.Max(
            MathF.Max(width, height) / tileCount,
            1);
        Vector2 pixelCenter = new(x + 0.5f, y + 0.5f);
        Vector2 cellCoordinate = pixelCenter / cellSize;
        Vector2 cell = new(
            MathF.Floor(cellCoordinate.X),
            MathF.Floor(cellCoordinate.Y));
        Vector2 cellOrigin = cell * cellSize;
        Vector2 cellEnd = Vector2.Min(
            cellOrigin + new Vector2(cellSize),
            new Vector2(width, height));
        Vector2 cellExtent = cellEnd - cellOrigin;

        Vector4 seedOption = plan.GetOption("Seed");
        uint seedLow = (uint)MathF.Round(seedOption.X);
        uint seedHigh = (uint)MathF.Round(seedOption.Y);
        uint cellX = (uint)cell.X;
        uint cellY = (uint)cell.Y;
        Vector2 jitter = new(
            (Uniform(cellX, cellY, seedLow, seedHigh, 0) * 2) - 1,
            (Uniform(cellX, cellY, seedLow, seedHigh, 1) * 2) - 1);
        float maximumOffset = Math.Clamp(
            plan.GetOption("MaximumOffset").X,
            0,
            1);
        Vector2 sourceCenter =
            pixelCenter - (jitter * maximumOffset * cellExtent);

        if (sourceCenter.X < cellOrigin.X ||
            sourceCenter.Y < cellOrigin.Y ||
            sourceCenter.X >= cellEnd.X ||
            sourceCenter.Y >= cellEnd.Y)
        {
            Vector4 background = plan.GetOption("Background");
            return new Vector4(
                background.X * background.W,
                background.Y * background.W,
                background.Z * background.W,
                background.W);
        }

        Vector2 firstPixelCenter = new(
            MathF.Ceiling(cellOrigin.X - 0.5f) + 0.5f,
            MathF.Ceiling(cellOrigin.Y - 0.5f) + 0.5f);
        Vector2 lastPixelCenter = new(
            MathF.Ceiling(cellEnd.X - 0.5f) - 0.5f,
            MathF.Ceiling(cellEnd.Y - 0.5f) - 0.5f);
        sourceCenter = Vector2.Clamp(
            sourceCenter,
            firstPixelCenter,
            lastPixelCenter);

        return BilinearSample(
            source,
            width,
            height,
            sourceCenter - new Vector2(0.5f));
    }

    private static Vector4 BilinearSample(
        Vector4[] source,
        int width,
        int height,
        Vector2 sample)
    {
        float sampleX = Math.Clamp(sample.X, 0, width - 1);
        float sampleY = Math.Clamp(sample.Y, 0, height - 1);
        int x0 = (int)MathF.Floor(sampleX);
        int y0 = (int)MathF.Floor(sampleY);
        int x1 = Math.Min(x0 + 1, width - 1);
        int y1 = Math.Min(y0 + 1, height - 1);
        float fractionX = sampleX - x0;
        float fractionY = sampleY - y0;

        if (fractionX == 0 && fractionY == 0)
        {
            return source[(y0 * width) + x0];
        }

        Vector4 top = Vector4.Lerp(
            source[(y0 * width) + x0],
            source[(y0 * width) + x1],
            fractionX);
        Vector4 bottom = Vector4.Lerp(
            source[(y1 * width) + x0],
            source[(y1 * width) + x1],
            fractionX);
        return Vector4.Lerp(top, bottom, fractionY);
    }

    private static float Uniform(
        uint x,
        uint y,
        uint seedLow,
        uint seedHigh,
        uint channel)
    {
        uint value;
        unchecked
        {
            uint input =
                (x * 0x9e3779b9u) ^
                (y * 0x85ebca6bu) ^
                seedLow ^
                (seedHigh * 0x27d4eb2du) ^
                (channel * 0xc2b2ae35u);
            uint state = (input * 747796405u) + 2891336453u;
            uint word =
                ((state >> ((int)(state >> 28) + 4)) ^ state) *
                277803737u;
            value = (word >> 22) ^ word;
        }

        return (value + 0.5f) * UintScale;
    }
}
