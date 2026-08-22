using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Drawing.MonoGame;

internal enum RenderSurface2DCommandKind
{
    FillRectangle,
    DrawSprite
}

internal readonly struct RenderSurface2DCommand
{
    private RenderSurface2DCommand(
        RenderSurface2DCommandKind kind,
        Rectangle destination,
        Texture2D? texture,
        Rectangle? source,
        XnaColor color,
        float rotation,
        Vector2 origin,
        SpriteEffects effects,
        float layerDepth)
    {
        Kind = kind;
        Destination = destination;
        Texture = texture;
        Source = source;
        Color = color;
        Rotation = rotation;
        Origin = origin;
        Effects = effects;
        LayerDepth = layerDepth;
    }

    public RenderSurface2DCommandKind Kind { get; }

    public Rectangle Destination { get; }

    public Texture2D? Texture { get; }

    public Rectangle? Source { get; }

    public XnaColor Color { get; }

    public float Rotation { get; }

    public Vector2 Origin { get; }

    public SpriteEffects Effects { get; }

    public float LayerDepth { get; }

    public static RenderSurface2DCommand FillRectangle(
        Rectangle destination,
        XnaColor color) =>
        new(
            RenderSurface2DCommandKind.FillRectangle,
            destination,
            texture: null,
            source: null,
            color,
            rotation: 0,
            origin: Vector2.Zero,
            SpriteEffects.None,
            layerDepth: 0);

    public static RenderSurface2DCommand DrawSprite(
        Texture2D texture,
        Rectangle destination,
        Rectangle? source,
        XnaColor color,
        float rotation,
        Vector2 origin,
        SpriteEffects effects,
        float layerDepth) =>
        new(
            RenderSurface2DCommandKind.DrawSprite,
            destination,
            texture,
            source,
            color,
            rotation,
            origin,
            effects,
            layerDepth);

    public Rectangle ResolveDamageBounds(Rectangle surfaceBounds)
    {
        if (Kind == RenderSurface2DCommandKind.DrawSprite &&
            (Rotation != 0 || Origin != Vector2.Zero))
        {
            return surfaceBounds;
        }

        return Destination;
    }

    public bool VisuallyEquals(in RenderSurface2DCommand other)
    {
        return Kind == other.Kind &&
            Destination == other.Destination &&
            ReferenceEquals(Texture, other.Texture) &&
            Source == other.Source &&
            Color == other.Color &&
            Rotation == other.Rotation &&
            Origin == other.Origin &&
            Effects == other.Effects &&
            LayerDepth == other.LayerDepth;
    }

    public void Replay(SpriteBatch spriteBatch, Texture2D whitePixel)
    {
        if (Kind == RenderSurface2DCommandKind.FillRectangle)
        {
            spriteBatch.Draw(whitePixel, Destination, Color);
            return;
        }

        spriteBatch.Draw(
            Texture!,
            Destination,
            Source,
            Color,
            Rotation,
            Origin,
            Effects,
            LayerDepth);
    }
}
