using System.Collections.ObjectModel;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;

namespace Cerneala.UI.Controls;

[ContentProperty(nameof(Colliders))]
public sealed class TileInstance2D : SceneNode2D
{
    private readonly SpriteAnimationPlayback animationPlayback = new();

    public static readonly UiProperty<int> XProperty =
        UiProperty<int>.Register(
            nameof(X),
            typeof(TileInstance2D),
            new UiPropertyMetadata<int>(0, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<int> YProperty =
        UiProperty<int>.Register(
            nameof(Y),
            typeof(TileInstance2D),
            new UiPropertyMetadata<int>(0, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<int?> TileIdProperty =
        UiProperty<int?>.Register(
            nameof(TileId),
            typeof(TileInstance2D),
            new UiPropertyMetadata<int?>(
                null,
                UiPropertyOptions.AffectsRender,
                validateValue: static value => value is null or > 0));

    public static readonly UiProperty<DrawRect?> SourceRectProperty =
        UiProperty<DrawRect?>.Register(
            nameof(SourceRect),
            typeof(TileInstance2D),
            new UiPropertyMetadata<DrawRect?>(
                null,
                UiPropertyOptions.AffectsRender,
                validateValue: static value => value is null ||
                    (value.Value.Width > 0 && value.Value.Height > 0)));

    public static readonly UiProperty<Color> TintProperty =
        UiProperty<Color>.Register(
            nameof(Tint),
            typeof(TileInstance2D),
            new UiPropertyMetadata<Color>(Color.White, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<TileFlip2D?> FlipProperty =
        UiProperty<TileFlip2D?>.Register(
            nameof(Flip),
            typeof(TileInstance2D),
            new UiPropertyMetadata<TileFlip2D?>(
                null,
                UiPropertyOptions.AffectsRender,
                validateValue: static value => value is null ||
                    (value.Value & ~(TileFlip2D.Horizontal | TileFlip2D.Vertical | TileFlip2D.Diagonal)) == 0));

    public static readonly UiProperty<DrawPoint> TransformOriginProperty =
        UiProperty<DrawPoint>.Register(
            nameof(TransformOrigin),
            typeof(TileInstance2D),
            new UiPropertyMetadata<DrawPoint>(default, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<bool> ReplacesImportedCollidersProperty =
        UiProperty<bool>.Register(
            nameof(ReplacesImportedColliders),
            typeof(TileInstance2D),
            new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsHitTest));

    public static readonly UiProperty<SpriteAnimationSet?> AnimationsProperty =
        UiProperty<SpriteAnimationSet?>.Register(
            nameof(Animations),
            typeof(TileInstance2D),
            new UiPropertyMetadata<SpriteAnimationSet?>(null, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<string?> AnimationStateProperty =
        UiProperty<string?>.Register(
            nameof(AnimationState),
            typeof(TileInstance2D),
            new UiPropertyMetadata<string?>(null, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<double> AnimationPlaybackRateProperty =
        UiProperty<double>.Register(
            nameof(AnimationPlaybackRate),
            typeof(TileInstance2D),
            new UiPropertyMetadata<double>(
                1,
                UiPropertyOptions.AffectsRender,
                validateValue: static value => double.IsFinite(value) && value >= 0));

    public static readonly UiProperty<bool> IsAnimationPausedProperty =
        UiProperty<bool>.Register(
            nameof(IsAnimationPaused),
            typeof(TileInstance2D),
            new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<SpriteAnimationStateChangeMode> AnimationStateChangeModeProperty =
        UiProperty<SpriteAnimationStateChangeMode>.Register(
            nameof(AnimationStateChangeMode),
            typeof(TileInstance2D),
            new UiPropertyMetadata<SpriteAnimationStateChangeMode>(
                SpriteAnimationStateChangeMode.Restart,
                UiPropertyOptions.AffectsRender,
                validateValue: static value => value is SpriteAnimationStateChangeMode.Restart or SpriteAnimationStateChangeMode.Resume));

    public TileInstance2D()
    {
        Colliders = new ColliderCollection(this);
    }

    internal TileLayer2D? OwnerLayer { get; set; }

    public int X
    {
        get => GetValue(XProperty);
        set => SetValue(XProperty, value);
    }

    public int Y
    {
        get => GetValue(YProperty);
        set => SetValue(YProperty, value);
    }

    public int? TileId
    {
        get => GetValue(TileIdProperty);
        set => SetValue(TileIdProperty, value);
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

    public TileFlip2D? Flip
    {
        get => GetValue(FlipProperty);
        set => SetValue(FlipProperty, value);
    }

    public DrawPoint TransformOrigin
    {
        get => GetValue(TransformOriginProperty);
        set => SetValue(TransformOriginProperty, value);
    }

    public bool ReplacesImportedColliders
    {
        get => GetValue(ReplacesImportedCollidersProperty);
        set => SetValue(ReplacesImportedCollidersProperty, value);
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

    public Collection<Collider2D> Colliders { get; }

    internal TileCellKey2D GetKey() =>
        new(OwnerLayer?.LayerId ?? string.Empty, X, Y);

    internal override void Record(Scene2DRecordContext context)
    {
        OwnerLayer?.OwnerMap?.RecordPromotedTile(OwnerLayer, this, context);
    }

    internal override bool AdvanceAnimation(TimeSpan frameTime) =>
        animationPlayback.Advance(frameTime, AnimationPlaybackRate, IsAnimationPaused);

    internal override bool HasActiveAnimation =>
        animationPlayback.IsActive(AnimationPlaybackRate, IsAnimationPaused);

    internal void ResolveAnimatedVisual(
        DrawRect sourceRect,
        TileFlip2D flip,
        out DrawRect effectiveSourceRect,
        out TileFlip2D effectiveFlip)
    {
        SpriteAnimationFrame? frame = animationPlayback.CurrentFrame;
        effectiveSourceRect = frame?.SourceRect ?? sourceRect;
        effectiveFlip = (TileFlip2D)((int)flip ^ (int)(frame?.Flip ?? RenderSurface2DSpriteFlip.None));
    }

    internal override void AttachSurface(RenderSurface2D? surface)
    {
        base.AttachSurface(surface);
        foreach (Collider2D collider in Colliders)
        {
            collider.AttachSurface(surface);
        }
    }

    internal override Matrix3x2 GetLocalTransform()
    {
        Matrix3x2 transform = SceneGeometry2D.CreateLocalTransform(this, TransformOrigin);
        if (OwnerLayer?.OwnerMap?.Model is TileMap2DModel model)
        {
            transform *= Matrix3x2.CreateTranslation(
                X * model.TileSize.Width,
                Y * model.TileSize.Height);
        }

        return transform;
    }

    internal override SceneBounds2D GetVisibleLocalBounds()
    {
        TileMap2D? map = OwnerLayer?.OwnerMap;
        if (map?.Model is not TileMap2DModel model || Opacity <= 0)
        {
            return SceneBounds2D.Empty;
        }

        return SceneBounds2D.Known(new DrawRect(0, 0, model.TileSize.Width, model.TileSize.Height));
    }

    internal override SceneBounds2D GetHitTestLocalBounds()
    {
        TileMap2D? map = OwnerLayer?.OwnerMap;
        return map?.Model is TileMap2DModel model
            ? SceneBounds2D.Known(new DrawRect(0, 0, model.TileSize.Width, model.TileSize.Height))
            : SceneBounds2D.Empty;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, XProperty) ||
            ReferenceEquals(args.Property, YProperty) ||
            ReferenceEquals(args.Property, TileIdProperty) ||
            ReferenceEquals(args.Property, ReplacesImportedCollidersProperty))
        {
            OwnerLayer?.OwnerMap?.SynchronizeCollisionAdaptersAndNotify();
        }
        else if (SceneGeometry2D.IsSceneTransformProperty(args.Property) ||
                 ReferenceEquals(args.Property, TransformOriginProperty))
        {
            SceneGeometry2D.FindRootScene(this)?.NotifyCollisionMutation(
                this,
                SceneCollisionMutationKind.Geometry);
        }
        else if (ReferenceEquals(args.Property, UIElement.IsVisibleProperty) ||
                 ReferenceEquals(args.Property, UIElement.VisibilityProperty))
        {
            SceneGeometry2D.FindRootScene(this)?.NotifyCollisionMutation(
                this,
                SceneCollisionMutationKind.Participation);
        }

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

    private sealed class ColliderCollection(TileInstance2D owner) : Collection<Collider2D>
    {
        protected override void InsertItem(int index, Collider2D item)
        {
            ArgumentNullException.ThrowIfNull(item);
            owner.LogicalChildren.Insert(index, item);
            base.InsertItem(index, item);
            item.AttachSurface(owner.Surface);
            owner.OwnerLayer?.OwnerMap?.SynchronizeCollisionAdaptersAndNotify();
            owner.Surface?.InvalidateFrame();
        }

        protected override void SetItem(int index, Collider2D item)
        {
            ArgumentNullException.ThrowIfNull(item);
            Collider2D previous = this[index];
            previous.AttachSurface(null);
            owner.LogicalChildren.Remove(previous);
            owner.LogicalChildren.Insert(index, item);
            base.SetItem(index, item);
            item.AttachSurface(owner.Surface);
            owner.OwnerLayer?.OwnerMap?.SynchronizeCollisionAdaptersAndNotify();
            owner.Surface?.InvalidateFrame();
        }

        protected override void RemoveItem(int index)
        {
            Collider2D previous = this[index];
            previous.AttachSurface(null);
            owner.LogicalChildren.Remove(previous);
            base.RemoveItem(index);
            owner.OwnerLayer?.OwnerMap?.SynchronizeCollisionAdaptersAndNotify();
            owner.Surface?.InvalidateFrame();
        }

        protected override void ClearItems()
        {
            foreach (Collider2D collider in this)
            {
                collider.AttachSurface(null);
                owner.LogicalChildren.Remove(collider);
            }

            base.ClearItems();
            owner.OwnerLayer?.OwnerMap?.SynchronizeCollisionAdaptersAndNotify();
            owner.Surface?.InvalidateFrame();
        }
    }
}
