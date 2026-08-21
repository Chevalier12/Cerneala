using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CernealaColor = Cerneala.Drawing.Color;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.UI.Controls;

[Flags]
public enum RenderSurface2DSpriteFlip
{
    None = 0,
    Horizontal = 1,
    Vertical = 2
}

public sealed class RenderSurface2DFrame
{
    private readonly SpriteBatch spriteBatch;
    private readonly Texture2D whitePixel;
    private bool active = true;

    internal RenderSurface2DFrame(
        SpriteBatch spriteBatch,
        Texture2D whitePixel,
        Rectangle bounds,
        TimeSpan frameTime)
    {
        this.spriteBatch = spriteBatch ??
            throw new ArgumentNullException(nameof(spriteBatch));
        this.whitePixel = whitePixel ??
            throw new ArgumentNullException(nameof(whitePixel));
        Bounds = new DrawRect(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        FrameTime = frameTime;
    }

    public DrawRect Bounds { get; }

    public TimeSpan FrameTime { get; }

    public void FillRectangle(DrawRect rectangle, CernealaColor color)
    {
        EnsureActive();
        Rectangle destination = new MonoGameDrawMapper(1).MapRectangle(rectangle);
        if (destination.Width <= 0 || destination.Height <= 0)
        {
            return;
        }

        spriteBatch.Draw(whitePixel, destination, ToPremultipliedColor(color));
    }

    public void DrawSprite(
        IDrawImage image,
        DrawRect destination,
        CernealaColor tint)
    {
        DrawSprite(
            image,
            destination,
            source: null,
            tint,
            rotation: 0,
            origin: default,
            RenderSurface2DSpriteFlip.None,
            layerDepth: 0);
    }

    public void DrawSprite(
        IDrawImage image,
        DrawRect destination,
        DrawRect? source,
        CernealaColor tint,
        float rotation = 0,
        DrawPoint origin = default,
        RenderSurface2DSpriteFlip flip = RenderSurface2DSpriteFlip.None,
        float layerDepth = 0)
    {
        EnsureActive();
        ArgumentNullException.ThrowIfNull(image);
        if (!float.IsFinite(rotation))
        {
            throw new ArgumentOutOfRangeException(nameof(rotation));
        }
        if (!float.IsFinite(layerDepth) || layerDepth < 0 || layerDepth > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(layerDepth));
        }
        if ((flip & ~(RenderSurface2DSpriteFlip.Horizontal | RenderSurface2DSpriteFlip.Vertical)) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(flip));
        }
        if (image is not MonoGameImage monoGameImage)
        {
            throw new InvalidOperationException(
                "RenderSurface2D requires a MonoGame-compatible IDrawImage on the MonoGame backend.");
        }
        if (!ReferenceEquals(
            monoGameImage.Texture.GraphicsDevice,
            spriteBatch.GraphicsDevice))
        {
            throw new InvalidOperationException(
                "A sprite image can only be drawn by the GraphicsDevice that created it.");
        }

        MonoGameDrawMapper mapper = new(1);
        Rectangle destinationRectangle = mapper.MapRectangle(destination);
        if (destinationRectangle.Width <= 0 || destinationRectangle.Height <= 0)
        {
            return;
        }

        Rectangle? sourceRectangle = source is DrawRect sourceBounds
            ? mapper.MapRectangle(sourceBounds)
            : null;
        SpriteEffects effects = SpriteEffects.None;
        if ((flip & RenderSurface2DSpriteFlip.Horizontal) != 0)
        {
            effects |= SpriteEffects.FlipHorizontally;
        }
        if ((flip & RenderSurface2DSpriteFlip.Vertical) != 0)
        {
            effects |= SpriteEffects.FlipVertically;
        }

        spriteBatch.Draw(
            monoGameImage.Texture,
            destinationRectangle,
            sourceRectangle,
            ToPremultipliedColor(tint),
            rotation,
            mapper.MapVector(origin),
            effects,
            layerDepth);
    }

    internal void Complete()
    {
        active = false;
    }

    private void EnsureActive()
    {
        ObjectDisposedException.ThrowIf(!active, this);
    }

    private static XnaColor ToPremultipliedColor(CernealaColor color)
    {
        return XnaColor.FromNonPremultiplied(
            color.R,
            color.G,
            color.B,
            color.A);
    }
}
