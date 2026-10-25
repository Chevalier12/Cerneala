using System.Numerics;

namespace Cerneala.UI.Controls;

// How the image of a Sprite2D is mirrored right now, as far as a Vision cone follows it: the flip the image is drawn
// with (Sprite2D.Flip composed with the Flip of the current animation frame) and the centre lines of the sprite's local
// image rectangle that the cone's apex mirrors across. The image flips by swapping texture coordinates inside an
// unchanged destination rectangle, so that rectangle's centre is the axis.
// A centre is zero on an axis that is not flipped, so two mirrors are equal exactly when they place the cone alike.
internal readonly record struct SpriteMirror2D(RenderSurface2DSpriteFlip Flip, float CenterX, float CenterY)
{
    internal bool IsHorizontal => (Flip & RenderSurface2DSpriteFlip.Horizontal) != 0;

    internal bool IsVertical => (Flip & RenderSurface2DSpriteFlip.Vertical) != 0;

    // The apex of a cone at (offsetX, offsetY) moves to its mirror image across each flipped centre line.
    internal Vector2 MirrorOffset(float offsetX, float offsetY) => new(
        IsHorizontal ? (2 * CenterX) - offsetX : offsetX,
        IsVertical ? (2 * CenterY) - offsetY : offsetY);
}
