using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Controls;

public sealed class Sprite2D : SceneNode2D
{
    private ResourceId<ImageResource>? sourceResourceId;
    private readonly SpriteAnimationPlayback animationPlayback = new();

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

    public static readonly UiProperty<SpriteAnimationSet?> AnimationsProperty =
        UiProperty<SpriteAnimationSet?>.Register(
            nameof(Animations),
            typeof(Sprite2D),
            new UiPropertyMetadata<SpriteAnimationSet?>(null, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<string?> AnimationStateProperty =
        UiProperty<string?>.Register(
            nameof(AnimationState),
            typeof(Sprite2D),
            new UiPropertyMetadata<string?>(null, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<double> AnimationPlaybackRateProperty =
        UiProperty<double>.Register(
            nameof(AnimationPlaybackRate),
            typeof(Sprite2D),
            new UiPropertyMetadata<double>(
                1,
                UiPropertyOptions.AffectsRender,
                validateValue: static value => double.IsFinite(value) && value >= 0));

    public static readonly UiProperty<bool> IsAnimationPausedProperty =
        UiProperty<bool>.Register(
            nameof(IsAnimationPaused),
            typeof(Sprite2D),
            new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<SpriteAnimationStateChangeMode> AnimationStateChangeModeProperty =
        UiProperty<SpriteAnimationStateChangeMode>.Register(
            nameof(AnimationStateChangeMode),
            typeof(Sprite2D),
            new UiPropertyMetadata<SpriteAnimationStateChangeMode>(
                SpriteAnimationStateChangeMode.Restart,
                UiPropertyOptions.AffectsRender,
                validateValue: static value => value is SpriteAnimationStateChangeMode.Restart or SpriteAnimationStateChangeMode.Resume));

    public IDrawImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public ResourceId<ImageResource>? SourceResourceId
    {
        get => sourceResourceId;
        set
        {
            if (sourceResourceId == value)
            {
                return;
            }

            sourceResourceId = value;
            IncrementRenderVersion();
            Invalidate(InvalidationFlags.Render, "Sprite image resource id changed");
        }
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

    public SpriteAnimationSet? Animations
    {
        get => GetValue(AnimationsProperty);
        set => SetValue(AnimationsProperty, value);
    }

    public string? AnimationState
    {
        get => GetValue(AnimationStateProperty);
        set => SetValue(AnimationStateProperty, value);
    }

    public double AnimationPlaybackRate
    {
        get => GetValue(AnimationPlaybackRateProperty);
        set => SetValue(AnimationPlaybackRateProperty, value);
    }

    public bool IsAnimationPaused
    {
        get => GetValue(IsAnimationPausedProperty);
        set => SetValue(IsAnimationPausedProperty, value);
    }

    public SpriteAnimationStateChangeMode AnimationStateChangeMode
    {
        get => GetValue(AnimationStateChangeModeProperty);
        set => SetValue(AnimationStateChangeModeProperty, value);
    }

    public void RestartAnimation()
    {
        if (animationPlayback.Restart())
        {
            IncrementRenderVersion();
            Surface?.InvalidateFrame();
        }
        RefreshAnimationRegistration();
    }

    internal override bool AdvanceAnimation(TimeSpan frameTime) =>
        animationPlayback.Advance(frameTime, AnimationPlaybackRate, IsAnimationPaused);

    internal override bool HasActiveAnimation =>
        animationPlayback.IsActive(AnimationPlaybackRate, IsAnimationPaused);

    internal override void Record(Scene2DRecordContext context)
    {
        IDrawImage? source = ResolveSource();
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
        SpriteAnimationFrame? animationFrame = animationPlayback.CurrentFrame;
        DrawRect? effectiveSourceRect = animationFrame?.SourceRect ?? SourceRect;
        RenderSurface2DSpriteFlip effectiveFlip = ComposeFlip(Flip, animationFrame?.Flip ?? RenderSurface2DSpriteFlip.None);
        SceneBounds2D bounds = SceneBounds2D.Known(GetDrawBounds(source, effectiveSourceRect));
        if (!context.IntersectsVisibleLocalBounds(bounds) && !HasPrismInSceneAncestry(context))
        {
            return;
        }
        using ScenePrismScope prism = context.HasPrism(this)
            ? context.BeginPrism(this, bounds)
            : default;
        context.Frame.DrawSprite(
            source,
            destination,
            effectiveSourceRect,
            tint,
            Rotation,
            Origin,
            effectiveFlip,
            LayerDepth);
    }

    internal override SceneBounds2D GetVisibleLocalBounds()
    {
        if (Opacity <= 0)
        {
            return SceneBounds2D.Empty;
        }

        return GetHitTestLocalBounds();
    }

    internal override SceneBounds2D GetHitTestLocalBounds()
    {

        IDrawImage? source = SourceResourceId is null ? Source : null;
        if (source is not null)
        {
            return SceneBounds2D.Known(GetDrawBounds(source, animationPlayback.CurrentFrame?.SourceRect ?? SourceRect));
        }

        return Rotation == 0 && Origin == default
            ? SceneBounds2D.Known(Destination)
            : SceneBounds2D.Unknown;
    }

    private IDrawImage? ResolveSource()
    {
        if (SourceResourceId is not ResourceId<ImageResource> id)
        {
            SetRenderDependencies(RenderDependency.None);
            return Source;
        }

        ImageResourceResolution resolution = ImageResourceResolver.Resolve(
            this,
            id,
            explicitProvider: null,
            explicitTracker: null,
            InvalidationFlags.Render,
            affectsIntrinsicSize: false);
        SetRenderDependencies(RenderDependencies
            .WithResourceIdentity(id.ToString())
            .WithResourceVersion(resolution.Version));
        return resolution.Image;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, AnimationsProperty) ||
            ReferenceEquals(args.Property, AnimationStateProperty) ||
            ReferenceEquals(args.Property, AnimationStateChangeModeProperty))
        {
            animationPlayback.Synchronize(Animations, AnimationState, AnimationStateChangeMode);
        }
        if (ReferenceEquals(args.Property, AnimationsProperty) ||
            ReferenceEquals(args.Property, AnimationStateProperty) ||
            ReferenceEquals(args.Property, AnimationStateChangeModeProperty) ||
            ReferenceEquals(args.Property, AnimationPlaybackRateProperty) ||
            ReferenceEquals(args.Property, IsAnimationPausedProperty))
        {
            RefreshAnimationRegistration();
        }
    }

    private DrawRect GetDrawBounds(IDrawImage source, DrawRect? sourceRect)
    {
        DrawImageOptions options = new(
            sourceRect,
            Tint,
            opacity: 1,
            Rotation,
            Origin,
            (DrawImageFlip)Flip,
            LayerDepth);
        DrawRect resolvedSourceRect = DrawImageGeometry.ResolveSource(source, options);
        DrawRect destination = Destination;
        float originX = Origin.X * destination.Width / resolvedSourceRect.Width;
        float originY = Origin.Y * destination.Height / resolvedSourceRect.Height;
        System.Numerics.Matrix3x2 transform =
            System.Numerics.Matrix3x2.CreateTranslation(-originX, -originY) *
            System.Numerics.Matrix3x2.CreateRotation(Rotation) *
            System.Numerics.Matrix3x2.CreateTranslation(destination.X, destination.Y);
        return SceneGeometry2D.TryTransformBounds(
            new DrawRect(0, 0, destination.Width, destination.Height),
            transform,
            out DrawRect bounds)
            ? bounds
            : Destination;
    }

    private static RenderSurface2DSpriteFlip ComposeFlip(
        RenderSurface2DSpriteFlip baseFlip,
        RenderSurface2DSpriteFlip frameFlip) =>
        (RenderSurface2DSpriteFlip)((int)baseFlip ^ (int)frameFlip);

    private bool HasPrismInSceneAncestry(Scene2DRecordContext context)
    {
        // Effects can extend beyond source bounds. Do not discard their input
        // merely because the unprocessed sprite is outside the viewport.
        for (UIElement? owner = this; owner is SceneNode2D node; owner = owner.LogicalParent)
        {
            if (context.HasPrism(node))
            {
                return true;
            }
        }
        return false;
    }
}
