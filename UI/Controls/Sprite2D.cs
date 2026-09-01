using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls;

public sealed class Sprite2D : SceneNode2D
{
    public static readonly UiProperty<IDrawImage?> SourceProperty =
        UiProperty<IDrawImage?>.Register(
            nameof(Source),
            typeof(Sprite2D),
            new UiPropertyMetadata<IDrawImage?>(null, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<DrawRect> DestinationProperty =
        UiProperty<DrawRect>.Register(
            nameof(Destination),
            typeof(Sprite2D),
            new UiPropertyMetadata<DrawRect>(default, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<DrawRect?> SourceRectProperty =
        UiProperty<DrawRect?>.Register(
            nameof(SourceRect),
            typeof(Sprite2D),
            new UiPropertyMetadata<DrawRect?>(null, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<Color> TintProperty =
        UiProperty<Color>.Register(
            nameof(Tint),
            typeof(Sprite2D),
            new UiPropertyMetadata<Color>(Color.White, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<DrawPoint> OriginProperty =
        UiProperty<DrawPoint>.Register(
            nameof(Origin),
            typeof(Sprite2D),
            new UiPropertyMetadata<DrawPoint>(default, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<RenderSurface2DSpriteFlip> FlipProperty =
        UiProperty<RenderSurface2DSpriteFlip>.Register(
            nameof(Flip),
            typeof(Sprite2D),
            new UiPropertyMetadata<RenderSurface2DSpriteFlip>(
                RenderSurface2DSpriteFlip.None,
                UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<float> LayerDepthProperty =
        UiProperty<float>.Register(
            nameof(LayerDepth),
            typeof(Sprite2D),
            new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsRender));

    public IDrawImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public DrawRect Destination
    {
        get => GetValue(DestinationProperty);
        set => SetValue(DestinationProperty, value);
    }

    public DrawRect? SourceRect
    {
        get => GetValue(SourceRectProperty);
        set => SetValue(SourceRectProperty, value);
    }

    public Color Tint
    {
        get => GetValue(TintProperty);
        set => SetValue(TintProperty, value);
    }

    public DrawPoint Origin
    {
        get => GetValue(OriginProperty);
        set => SetValue(OriginProperty, value);
    }

    public RenderSurface2DSpriteFlip Flip
    {
        get => GetValue(FlipProperty);
        set => SetValue(FlipProperty, value);
    }

    public float LayerDepth
    {
        get => GetValue(LayerDepthProperty);
        set => SetValue(LayerDepthProperty, value);
    }

    internal override void Record(RenderSurface2DFrame frame)
    {
        IDrawImage? source = Source;
        if (!UIElementVisibility.ParticipatesInRendering(this) ||
            Opacity <= 0 ||
            source is null)
        {
            return;
        }

        Color tint = Tint;
        if (Opacity < 1)
        {
            tint = tint with
            {
                A = (byte)Math.Clamp((int)MathF.Round(tint.A * Opacity), 0, byte.MaxValue)
            };
        }

        DrawRect destination = Destination;
        bool hasPrism = frame.BeginPrism(this, destination);
        try
        {
            frame.DrawSprite(
                source,
                destination,
                SourceRect,
                tint,
                Rotation,
                Origin,
                Flip,
                LayerDepth);
        }
        finally
        {
            if (hasPrism)
            {
                frame.EndPrism();
            }
        }
    }
}
