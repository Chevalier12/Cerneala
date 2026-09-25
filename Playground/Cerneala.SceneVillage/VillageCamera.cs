using System.Numerics;
using Cerneala.Drawing;

namespace Cerneala.SceneVillage;

internal static class VillageCamera
{
    internal static DrawRect Follow(
        Vector2 playerCenter,
        float arrangedWidth,
        float arrangedHeight,
        float rootScale,
        float? contentScale,
        float zoom)
    {
        float scale = contentScale ?? rootScale;
        float width = GetWorldExtent(arrangedWidth, rootScale, scale, zoom);
        float height = GetWorldExtent(arrangedHeight, rootScale, scale, zoom);
        float left = Math.Clamp(playerCenter.X - width / 2f, 0f, VillageLayout.WorldSize - width);
        float top = Math.Clamp(playerCenter.Y - height / 2f, 0f, VillageLayout.WorldSize - height);
        return new DrawRect(left, top, width, height);
    }

    private static float GetWorldExtent(float arrangedExtent, float rootScale, float contentScale, float zoom)
    {
        // Match RenderSurface2D's full-resolution raster extent. The camera
        // changes world coverage, never the target's pixel dimensions.
        int targetPixels = Math.Max(1, checked((int)MathF.Ceiling(arrangedExtent * rootScale)));
        double requested = targetPixels / ((double)contentScale * zoom);
        return (float)Math.Clamp(requested, 1d, VillageLayout.WorldSize);
    }
}
