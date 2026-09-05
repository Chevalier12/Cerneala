namespace Cerneala.Drawing;

// Shared raster extent contract for surface recording and input projection.
internal static class RenderSurface2DGeometry
{
    internal static (int Width, int Height) GetPixelSize(float width, float height, float scale) =>
        (Math.Max(1, checked((int)MathF.Ceiling(width * scale))),
         Math.Max(1, checked((int)MathF.Ceiling(height * scale))));
}
