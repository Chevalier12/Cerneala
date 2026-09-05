using System.Numerics;
using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

internal static class TileFlipGeometry2D
{
    internal static Matrix3x2 Transform(TileFlip2D flip, DrawSize size)
    {
        Matrix3x2 transform = (flip & TileFlip2D.Diagonal) != 0
            ? new Matrix3x2(0, size.Height / size.Width, size.Width / size.Height, 0, 0, 0)
            : Matrix3x2.Identity;
        if ((flip & TileFlip2D.Horizontal) != 0)
        {
            transform *= new Matrix3x2(-1, 0, 0, 1, size.Width, 0);
        }
        if ((flip & TileFlip2D.Vertical) != 0)
        {
            transform *= new Matrix3x2(1, 0, 0, -1, 0, size.Height);
        }
        return transform;
    }

    internal static DrawSprite2D Sprite(DrawRect destination, DrawRect source, Color tint, TileFlip2D flip)
    {
        float rotation = 0;
        DrawImageFlip imageFlip = (DrawImageFlip)((int)flip & 3);
        if ((flip & TileFlip2D.Diagonal) != 0)
        {
            // Swap normalized axes inside the same rectangular cell, using the
            // existing batched sprite rotation/UV contract (no per-cell node).
            destination = new DrawRect(destination.Right, destination.Y, destination.Height, destination.Width);
            rotation = MathF.PI / 2;
            imageFlip = ((flip & TileFlip2D.Vertical) != 0 ? DrawImageFlip.Horizontal : DrawImageFlip.None) |
                ((flip & TileFlip2D.Horizontal) == 0 ? DrawImageFlip.Vertical : DrawImageFlip.None);
        }
        return new DrawSprite2D(destination, new DrawImageOptions(source, tint, rotation: rotation, flip: imageFlip, sampling: DrawSamplingMode.Point));
    }
}
