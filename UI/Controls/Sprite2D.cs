using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Controls;

public sealed class Sprite2D : SceneNode2D
{
    private readonly SpriteAnimationPlayback animationPlayback = new();

    public static readonly UiProperty<ImageReference?> ImageProperty =
        UiProperty<ImageReference?>.Register(
            nameof(Image),
            typeof(Sprite2D),
            new UiPropertyMetadata<ImageReference?>(null, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<float> XProperty = RegisterCoordinate(nameof(X));
    public static readonly UiProperty<float> YProperty = RegisterCoordinate(nameof(Y));
    public static readonly UiProperty<float> SourceXProperty = RegisterSourceCoordinate(nameof(SourceX));
    public static readonly UiProperty<float> SourceYProperty = RegisterSourceCoordinate(nameof(SourceY));
    public static readonly UiProperty<float> SourceWidthProperty = RegisterSourceSize(nameof(SourceWidth));
    public static readonly UiProperty<float> SourceHeightProperty = RegisterSourceSize(nameof(SourceHeight));

    private static UiProperty<float> RegisterCoordinate(string name) =>
        UiProperty<float>.Register(name, typeof(Sprite2D),
            new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsRender,
                validateValue: float.IsFinite));

    private static UiProperty<float> RegisterSourceCoordinate(string name) =>
        UiProperty<float>.Register(name, typeof(Sprite2D),
            new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsRender,
                validateValue: static value => float.IsFinite(value) && value >= 0));

    private static UiProperty<float> RegisterSourceSize(string name) =>
        UiProperty<float>.Register(name, typeof(Sprite2D),
            new UiPropertyMetadata<float>(float.NaN, UiPropertyOptions.AffectsRender,
                validateValue: static value => float.IsNaN(value) || float.IsFinite(value) && value > 0));

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

    public ImageReference? Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public float X
    {
        get => GetValue(XProperty);
        set => SetValue(XProperty, value);
    }

    public float Y
    {
        get => GetValue(YProperty);
        set => SetValue(YProperty, value);
    }

    public float SourceX
    {
        get => GetValue(SourceXProperty);
        set => SetValue(SourceXProperty, value);
    }

    public float SourceY
    {
        get => GetValue(SourceYProperty);
        set => SetValue(SourceYProperty, value);
    }

    public float SourceWidth
    {
        get => GetValue(SourceWidthProperty);
        set => SetValue(SourceWidthProperty, value);
    }

    public float SourceHeight
    {
        get => GetValue(SourceHeightProperty);
        set => SetValue(SourceHeightProperty, value);
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

        ResolveGeometry(source, out DrawRect destination, out DrawRect? effectiveSourceRect, out DrawRect resolvedSourceRect);
        SpriteAnimationFrame? animationFrame = animationPlayback.CurrentFrame;
        RenderSurface2DSpriteFlip effectiveFlip = ComposeFlip(Flip, animationFrame?.Flip ?? RenderSurface2DSpriteFlip.None);
        SceneBounds2D bounds = SceneBounds2D.Known(GetDrawBounds(destination, resolvedSourceRect));
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
        IDrawImage? source = ResolveSource();
        if (source is not null)
        {
            ResolveGeometry(source, out DrawRect destination, out _, out DrawRect resolvedSourceRect);
            return SceneBounds2D.Known(GetDrawBounds(destination, resolvedSourceRect));
        }

        return Rotation == 0 && Origin == default && !float.IsNaN(Width) && !float.IsNaN(Height)
            ? SceneBounds2D.Known(new DrawRect(X, Y, Width, Height))
            : SceneBounds2D.Unknown;
    }

    private IDrawImage? ResolveSource()
    {
        ImageReference? reference = Image;
        if (reference?.ResourceId is not ResourceId<ImageResource> id)
        {
            SetRenderDependencies(RenderDependency.None);
            return reference?.DirectImage;
        }

        ImageResourceResolution resolution = ImageResourceResolver.Resolve(
            this,
            id,
            explicitProvider: null,
            explicitTracker: null,
            InvalidationFlags.Render,
            affectsIntrinsicSize: false);
        SetRenderDependencies(RenderDependencies
            .WithResourceIdentity(reference.ResourceIdentity)
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

    private void ResolveGeometry(
        IDrawImage source,
        out DrawRect destination,
        out DrawRect? sourceRect,
        out DrawRect resolvedSourceRect)
    {
        sourceRect = animationPlayback.CurrentFrame?.SourceRect;
        if (sourceRect is null &&
            (SourceX != 0 || SourceY != 0 || !float.IsNaN(SourceWidth) || !float.IsNaN(SourceHeight)))
        {
            sourceRect = new DrawRect(SourceX, SourceY,
                float.IsNaN(SourceWidth) ? source.Width - SourceX : SourceWidth,
                float.IsNaN(SourceHeight) ? source.Height - SourceY : SourceHeight);
        }
        resolvedSourceRect = DrawImageGeometry.ResolveSourceRect(source, sourceRect);
        destination = new DrawRect(X, Y,
            float.IsNaN(Width) ? resolvedSourceRect.Width : Width,
            float.IsNaN(Height) ? resolvedSourceRect.Height : Height);
    }

    private DrawRect GetDrawBounds(DrawRect destination, DrawRect resolvedSourceRect)
    {
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
            : destination;
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
