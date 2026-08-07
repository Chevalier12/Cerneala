using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;



internal static class PrismMosaicTilesFilter
{
    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        float tileSize = MathF.Max(
            plan.GetOption("TileSize").X,
            1);
        float groutWidth = Math.Clamp(
            plan.GetOption("GroutWidth").X,
            0,
            tileSize);
        float lightenGrout = Math.Clamp(
            plan.GetOption("LightenGrout").X,
            0,
            1);
        float halfGrout = groutWidth * 0.5f;
        Vector4[] result = new Vector4[source.Length];

        for (int y = 0; y < height; y++)
        {
            float pixelY = y + 0.5f;
            float cellY = MathF.Floor(pixelY / tileSize);
            float localY = pixelY - (cellY * tileSize);
            float edgeY = MathF.Min(localY, tileSize - localY);
            float sampleY = ((cellY + 0.5f) * tileSize) - 0.5f;
            for (int x = 0; x < width; x++)
            {
                float pixelX = x + 0.5f;
                float cellX = MathF.Floor(pixelX / tileSize);
                float localX = pixelX - (cellX * tileSize);
                float edgeX = MathF.Min(localX, tileSize - localX);
                float sampleX = ((cellX + 0.5f) * tileSize) - 0.5f;
                Vector4 tile = SampleBilinear(
                    source,
                    width,
                    height,
                    sampleX,
                    sampleY);
                result[(y * width) + x] =
                    MathF.Min(edgeX, edgeY) < halfGrout
                        ? Lighten(tile, lightenGrout)
                        : tile;
            }
        }

        return result;
    }

    private static Vector4 SampleBilinear(
        Vector4[] source,
        int width,
        int height,
        float x,
        float y)
    {
        float sampleX = Math.Clamp(x, 0, width - 1);
        float sampleY = Math.Clamp(y, 0, height - 1);
        int left = (int)MathF.Floor(sampleX);
        int top = (int)MathF.Floor(sampleY);
        int right = Math.Min(left + 1, width - 1);
        int bottom = Math.Min(top + 1, height - 1);
        float horizontal = sampleX - left;
        float vertical = sampleY - top;
        Vector4 upper = Vector4.Lerp(
            source[(top * width) + left],
            source[(top * width) + right],
            horizontal);
        Vector4 lower = Vector4.Lerp(
            source[(bottom * width) + left],
            source[(bottom * width) + right],
            horizontal);
        return Vector4.Lerp(upper, lower, vertical);
    }

    private static Vector4 Lighten(Vector4 color, float amount)
    {
        if (color.W <= 0 || amount <= 0)
        {
            return color;
        }

        Vector3 straight = new(color.X, color.Y, color.Z);
        straight /= color.W;
        straight = Vector3.Lerp(straight, Vector3.One, amount);
        straight = Vector3.Clamp(straight, Vector3.Zero, Vector3.One);
        return new Vector4(straight * color.W, color.W);
    }
}
