using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Markup;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Controls;

[ContentProperty(nameof(Collider))]
public sealed class Sprite2D : SceneNode2D
{
    private readonly SpriteAnimationPlayback animationPlayback = new();
    private readonly ColliderSlot2D colliderSlot;
    // Scene traversals visit every sprite several times per frame. Read the
    // properties they use once per property-store version, not once per visit.
    private long snapshotVersion = -1;
    private Snapshot snapshot;
    // A resolved resource image stays valid while its lease is held and no
    // resolution input (resources, ancestry, cache identity) has changed.
    private IDrawImage? residentSource;
    private ImageReference? residentReference;
    private ImageResolutionStamp residentStamp;
    private int residentReleaseGeneration;
    private long residentVersion;
    // Dimensions of a completed image observed without a lease, for geometry.
    private IDrawImage? geometrySource;
    private ImageReference? geometryReference;
    private ImageResolutionStamp geometryStamp;
    // The command this sprite last recorded and the inputs that shaped it.
    private DrawCommand recordedCommand;
    private long recordedCommandVersion;
    private DrawRect recordedDrawBounds;
    private IDrawImage? recordedSource;
    private long recordedVersion;
    private SpriteAnimationFrame? recordedFrame;
    private int recordedImageWidth;
    private int recordedImageHeight;
    // Hit-test and culling bounds for the same inputs, including an image's size.
    private SceneBounds2D localBounds;
    private bool hasLocalBounds;
    private long localBoundsVersion;
    private SpriteAnimationFrame? localBoundsFrame;
    private IDrawImage? localBoundsSource;
    private int localBoundsImageWidth;
    private int localBoundsImageHeight;

    public Sprite2D()
    {
        colliderSlot = new ColliderSlot2D(this);
    }

    public Collider2D? Collider
    {
        get => colliderSlot.Value;
        set => colliderSlot.Value = value;
    }

    internal override void AttachSurface(RenderSurface2D? surface)
    {
        base.AttachSurface(surface);
        colliderSlot.AttachSurface(surface);
    }

    // Shapes use destination units relative to the sprite anchor. Image sizing,
    // crop, Origin and Flip change presentation, not collision geometry.
    internal override Matrix3x2 GetLocalTransform() => Current.LocalTransform;

    internal override bool IsSceneRendered => Current.IsRendered;

    // Frames change only the source rectangle; readiness depends on it only
    // through bounds or an attached effect.
    internal override bool AnimationFrameAffectsPresentation =>
        Current.FrameAffectsBounds || Cerneala.UI.Prism.Runtime.PrismAttachment.TryGetRenderState(this, out _, out _);

    private ref readonly Snapshot Current
    {
        get
        {
            long version = PropertyValueVersion;
            if (snapshotVersion != version)
            {
                snapshot = new Snapshot(this);
                snapshotVersion = version;
            }
            return ref snapshot;
        }
    }

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

    public static readonly UiProperty<DrawSamplingMode> SamplingProperty =
        UiProperty<DrawSamplingMode>.Register(
            nameof(Sampling),
            typeof(Sprite2D),
            new UiPropertyMetadata<DrawSamplingMode>(
                DrawSamplingMode.Linear,
                UiPropertyOptions.AffectsRender,
                validateValue: static value => value is DrawSamplingMode.Point or DrawSamplingMode.Linear));

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

    public DrawSamplingMode Sampling
    {
        get => GetValue(SamplingProperty);
        set => SetValue(SamplingProperty, value);
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
            if (Current.FrameAffectsBounds) { NotifyContainerGeometryChanged(); }
            IncrementRenderVersion();
            Surface?.InvalidateFrame();
        }
        RefreshAnimationRegistration();
    }

    internal override bool AdvanceAnimation(TimeSpan frameTime)
    {
        bool changed = animationPlayback.Advance(frameTime, Current.AnimationPlaybackRate, Current.IsAnimationPaused);
        if (changed && Current.FrameAffectsBounds)
        {
            // The new frame's source rectangle sizes or anchors this sprite.
            NotifyContainerGeometryChanged();
        }
        return changed;
    }

    internal override bool HasActiveAnimation =>
        animationPlayback.IsActive(Current.AnimationPlaybackRate, Current.IsAnimationPaused);

    // At rate one the elapsed time is exactly the accumulated frame time, so a
    // surface clock can drive it without per-frame visits.
    internal override bool CanScheduleAnimation =>
        Current.AnimationPlaybackRate == 1.0 && !Current.IsAnimationPaused;

    internal override void AttachAnimationClock(SpriteAnimationClock? clock) =>
        animationPlayback.AttachClock(clock);

    internal override bool CanParkAnimation => !AnimationFrameAffectsPresentation;

    internal override long NextAnimationChangeTicks => animationPlayback.NextChangeClockTicks;

    internal override bool AdvanceScheduledAnimation()
    {
        bool changed = animationPlayback.AdvanceToClock();
        if (changed && Current.FrameAffectsBounds)
        {
            NotifyContainerGeometryChanged();
        }
        return changed;
    }

    internal override void CheckPresentation(ScenePresentationContext2D context)
    {
        ref readonly Snapshot current = ref Current;
        SceneBounds2D declared = GetDeclaredLocalBounds(current);
        if (!current.IsRendered || current.Opacity <= 0)
        {
            ReleaseRenderCaches();
            return;
        }
        if (declared.Kind == SceneBoundsKind.Known &&
            !ScenePresentationContext2D.Intersects(context.GetVisibleBounds(this), declared.Bounds))
        {
            // Missing required domains intentionally select empty input. Report
            // that error before culling, without acquiring source or Prism images.
            CheckPrismInputDomain(context);
            ReleaseRenderCaches();
            return;
        }

        ImageResourceResolution source = ResolveSource(ImageResourceAccess.Prepare);
        context.RequireImage(source);
        if (source.Image is not null || source.IsPending) { CheckPrismPresentation(context); }
        else { PrismImageLeases.Clear(); }
    }

    internal override void Record(Scene2DRecordContext context)
    {
        ref readonly Snapshot current = ref Current;
        if (!current.IsRendered || current.Opacity <= 0)
        {
            return;
        }

        SpriteAnimationFrame? animationFrame = animationPlayback.CurrentFrame;
        if (recordedSource is not null &&
            recordedVersion == snapshotVersion &&
            ReferenceEquals(recordedFrame, animationFrame) &&
            IsResidentCurrent(current.Image) &&
            ReferenceEquals(recordedSource, residentSource) &&
            recordedSource.Width == recordedImageWidth &&
            recordedSource.Height == recordedImageHeight)
        {
            // Nothing that shapes this command changed since it was recorded.
            SceneBounds2D recordedBounds = SceneBounds2D.Known(recordedDrawBounds);
            if (!context.IntersectsVisibleLocalBounds(recordedBounds) && !HasPrismInSceneAncestry(context))
            {
                return;
            }
            using ScenePrismScope recordedPrism = context.HasPrism(this)
                ? context.BeginPrism(this, recordedBounds)
                : default;
            context.Frame.AddRecordedCommand(recordedCommand, new RetainedCommandKey(this, recordedCommandVersion));
            return;
        }

        RecordChanged(context, in current, animationFrame);
    }

    // Keep command construction and its large temporaries off the retained
    // replay path. The snapshot remains a reference to the same live field.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private void RecordChanged(Scene2DRecordContext context, in Snapshot current, SpriteAnimationFrame? animationFrame)
    {
        // Cull on geometry before acquiring the image, so an offscreen sprite
        // does not take and then drop an image lease on every frame.
        if (ResolveGeometrySource() is IDrawImage geometry)
        {
            ResolveGeometry(current, geometry, out DrawRect culledDestination, out _, out DrawRect culledSource);
            if (!context.IntersectsVisibleLocalBounds(SceneBounds2D.Known(GetDrawBounds(current, culledDestination, culledSource))) &&
                !HasPrismInSceneAncestry(context))
            {
                return;
            }
        }

        IDrawImage? source = ResolveSource(ImageResourceAccess.ResidentOnly).Image;
        if (source is null)
        {
            return;
        }

        Color tint = current.Tint;
        if (current.Opacity < 1)
        {
            tint = tint with
            {
                A = (byte)Math.Clamp((int)MathF.Round(tint.A * current.Opacity), 0, byte.MaxValue)
            };
        }

        ResolveGeometry(current, source, out DrawRect destination, out DrawRect? effectiveSourceRect, out DrawRect resolvedSourceRect);
        RenderSurface2DSpriteFlip effectiveFlip = ComposeFlip(current.Flip, animationFrame?.Flip ?? RenderSurface2DSpriteFlip.None);
        DrawRect drawBounds = GetDrawBounds(current, destination, resolvedSourceRect);
        SceneBounds2D bounds = SceneBounds2D.Known(drawBounds);
        if (!context.IntersectsVisibleLocalBounds(bounds) && !HasPrismInSceneAncestry(context))
        {
            return;
        }
        DrawImageOptions options = new(
            source: effectiveSourceRect,
            tint: tint,
            rotation: current.Rotation,
            origin: current.Origin,
            flip: (DrawImageFlip)effectiveFlip,
            layerDepth: current.LayerDepth,
            sampling: current.Sampling);
        using ScenePrismScope prism = context.HasPrism(this)
            ? context.BeginPrism(this, bounds)
            : default;
        if (source is PrismImage || !ReferenceEquals(source, residentSource))
        {
            // Effect-backed and unleased images are expanded per recording.
            recordedSource = null;
            context.Frame.DrawImage(source, destination, options);
            return;
        }

        recordedCommand = DrawCommand.DrawImage(source, destination, options);
        recordedCommandVersion++;
        recordedDrawBounds = drawBounds;
        recordedSource = source;
        recordedVersion = snapshotVersion;
        recordedFrame = animationFrame;
        recordedImageWidth = source.Width;
        recordedImageHeight = source.Height;
        context.Frame.AddRecordedCommand(recordedCommand, new RetainedCommandKey(this, recordedCommandVersion));
    }

    // The resident image a presentation check would resolve is already held.
    internal bool HasCurrentResidentImage => IsResidentCurrent(Current.Image);

    private bool IsResidentCurrent(ImageReference? reference) =>
        residentSource is not null &&
        ReferenceEquals(reference, residentReference) &&
        residentStamp.IsSet && residentStamp == ImageResourceResolutionEpoch.Capture(Root) &&
        residentReleaseGeneration == ImageReleaseGeneration;

    internal override SceneBounds2D GetVisibleLocalBounds()
    {
        if (Current.Opacity <= 0)
        {
            return SceneBounds2D.Empty;
        }

        return GetHitTestLocalBounds();
    }

    internal override SceneBounds2D GetHitTestLocalBounds()
    {
        // Geometry/input observation may read a completed image, but it must
        // never decode, wait for or retain an image. Required presentation owns
        // preparation; querying a cold sprite is not a second loading path.
        IDrawImage? source = ResolveGeometrySource();
        ref readonly Snapshot current = ref Current;
        SpriteAnimationFrame? frame = animationPlayback.CurrentFrame;
        if (hasLocalBounds &&
            localBoundsVersion == snapshotVersion &&
            ReferenceEquals(localBoundsFrame, frame) &&
            ReferenceEquals(localBoundsSource, source) &&
            (source is null || (source.Width == localBoundsImageWidth && source.Height == localBoundsImageHeight)))
        {
            return localBounds;
        }

        if (source is not null)
        {
            ResolveGeometry(current, source, out DrawRect destination, out _, out DrawRect resolvedSourceRect);
            localBounds = SceneBounds2D.Known(GetImageLocalBounds(current, destination, resolvedSourceRect));
        }
        else
        {
            localBounds = GetDeclaredLocalBounds(current);
        }

        hasLocalBounds = true;
        localBoundsVersion = snapshotVersion;
        localBoundsFrame = frame;
        localBoundsSource = source;
        localBoundsImageWidth = source?.Width ?? 0;
        localBoundsImageHeight = source?.Height ?? 0;
        return localBounds;
    }

    // The bounds presentation culls by; empty when the sprite is not drawn.
    internal SceneBounds2D GetPresentationLocalBounds()
    {
        ref readonly Snapshot current = ref Current;
        return !current.IsRendered || current.Opacity <= 0
            ? SceneBounds2D.Empty
            : GetDeclaredLocalBounds(current);
    }

    private SceneBounds2D GetDeclaredLocalBounds(in Snapshot current)
    {
        DrawRect? frame = animationPlayback.CurrentFrame?.SourceRect;
        float sourceWidth = frame?.Width ?? current.SourceWidth;
        float sourceHeight = frame?.Height ?? current.SourceHeight;
        float width = float.IsNaN(current.Width) ? sourceWidth : current.Width;
        float height = float.IsNaN(current.Height) ? sourceHeight : current.Height;
        DrawPoint origin = current.Origin;
        bool needsUnknownSourceWidth = origin.X != 0 && float.IsNaN(sourceWidth);
        bool needsUnknownSourceHeight = origin.Y != 0 && float.IsNaN(sourceHeight);
        if (float.IsNaN(width) || float.IsNaN(height) || needsUnknownSourceWidth || needsUnknownSourceHeight)
        {
            // Unknown natural dimensions cannot prove that an image is outside
            // the camera. Spatial SceneItems metadata can still exclude its
            // entire object without decoding any of its images.
            return SceneBounds2D.Unknown;
        }
        float originX = origin.X == 0 ? 0 : origin.X * width / sourceWidth;
        float originY = origin.Y == 0 ? 0 : origin.Y * height / sourceHeight;
        return SceneBounds2D.Known(new(-originX, -originY, width, height));
    }

    private ImageResourceResolution ResolveSource(ImageResourceAccess access)
    {
        ImageReference? reference = Current.Image;
        if (IsResidentCurrent(reference))
        {
            // Every access mode yields the held resident image unchanged.
            return new(residentSource, residentVersion);
        }

        ImageResolutionStamp stamp = ImageResourceResolutionEpoch.Capture(Root);
        ImageResourceResolution resolution = ResolveSourceCore(reference, access);
        residentSource = reference?.ResourceId is not null && !resolution.IsPending && resolution.Error is null
            ? resolution.Image
            : null;
        residentReference = reference;
        residentStamp = stamp;
        residentReleaseGeneration = ImageReleaseGeneration;
        residentVersion = resolution.Version;
        return resolution;
    }

    private IDrawImage? ResolveGeometrySource()
    {
        ImageReference? reference = Current.Image;
        if (reference?.ResourceId is not ResourceId<ImageResource> id)
        {
            return reference?.DirectImage;
        }

        if (IsResidentCurrent(reference))
        {
            return residentSource;
        }

        ImageResolutionStamp stamp = ImageResourceResolutionEpoch.Capture(Root);

        if (geometrySource is not null && ReferenceEquals(reference, geometryReference) && stamp.IsSet && geometryStamp == stamp)
        {
            return geometrySource;
        }

        // A missing image can complete later without an epoch change, so only
        // an observed completed image is remembered.
        ImageResourceResolution resolution = ImageResourceResolver.Resolve(
            this,
            id,
            explicitProvider: null,
            explicitTracker: null,
            InvalidationFlags.Render,
            affectsIntrinsicSize: false,
            access: ImageResourceAccess.PeekResident);
        SetRenderDependencies(RenderDependencies
            .WithResourceIdentity(reference.ResourceIdentity)
            .WithResourceVersion(resolution.Version));
        geometrySource = resolution.Image;
        geometryReference = reference;
        geometryStamp = stamp;
        return geometrySource;
    }

    private ImageResourceResolution ResolveSourceCore(ImageReference? reference, ImageResourceAccess access)
    {
        using ImageResourceLeaseSet.Scope usage = SourceImageLeases.Begin();
        if (reference?.ResourceId is not ResourceId<ImageResource> id)
        {
            SetRenderDependencies(RenderDependency.None);
            return new(reference?.DirectImage, 0);
        }

        ImageResourceResolution resolution = ImageResourceResolver.Resolve(
            this,
            id,
            explicitProvider: null,
            explicitTracker: null,
            InvalidationFlags.Render,
            affectsIntrinsicSize: false,
            access: access);
        SetRenderDependencies(RenderDependencies
            .WithResourceIdentity(reference.ResourceIdentity)
            .WithResourceVersion(resolution.Version));
        return resolution;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, XProperty) || ReferenceEquals(args.Property, YProperty) ||
            ReferenceEquals(args.Property, UIElement.RotationProperty))
        {
            SceneGeometry2D.FindRootScene(this)?.NotifyCollisionMutation(this, SceneCollisionMutationKind.Geometry);
        }
        else if (ReferenceEquals(args.Property, UIElement.IsVisibleProperty) ||
                 ReferenceEquals(args.Property, UIElement.VisibilityProperty))
        {
            SceneGeometry2D.FindRootScene(this)?.NotifyCollisionMutation(this, SceneCollisionMutationKind.Participation);
        }

        bool animationSelectionChanged =
            ReferenceEquals(args.Property, AnimationsProperty) ||
            ReferenceEquals(args.Property, AnimationStateProperty) ||
            ReferenceEquals(args.Property, AnimationStateChangeModeProperty);
        if (animationSelectionChanged)
        {
            animationPlayback.Synchronize(Animations, AnimationState, AnimationStateChangeMode);
        }
        if (animationSelectionChanged ||
            ReferenceEquals(args.Property, AnimationPlaybackRateProperty) ||
            ReferenceEquals(args.Property, IsAnimationPausedProperty))
        {
            RefreshAnimationRegistration();
        }
    }

    private void ResolveGeometry(
        in Snapshot current,
        IDrawImage source,
        out DrawRect destination,
        out DrawRect? sourceRect,
        out DrawRect resolvedSourceRect)
    {
        sourceRect = animationPlayback.CurrentFrame?.SourceRect;
        if (sourceRect is null &&
            (current.SourceX != 0 || current.SourceY != 0 || !float.IsNaN(current.SourceWidth) || !float.IsNaN(current.SourceHeight)))
        {
            sourceRect = new DrawRect(current.SourceX, current.SourceY,
                float.IsNaN(current.SourceWidth) ? source.Width - current.SourceX : current.SourceWidth,
                float.IsNaN(current.SourceHeight) ? source.Height - current.SourceY : current.SourceHeight);
        }
        resolvedSourceRect = DrawImageGeometry.ResolveSourceRect(source, sourceRect);
        destination = new DrawRect(current.X, current.Y,
            float.IsNaN(current.Width) ? resolvedSourceRect.Width : current.Width,
            float.IsNaN(current.Height) ? resolvedSourceRect.Height : current.Height);
    }

    private static DrawRect GetImageLocalBounds(in Snapshot current, DrawRect destination, DrawRect resolvedSourceRect)
    {
        float originX = current.Origin.X * destination.Width / resolvedSourceRect.Width;
        float originY = current.Origin.Y * destination.Height / resolvedSourceRect.Height;
        return new DrawRect(-originX, -originY, destination.Width, destination.Height);
    }

    private static DrawRect GetDrawBounds(in Snapshot current, DrawRect destination, DrawRect resolvedSourceRect)
    {
        return SceneGeometry2D.TryTransformBounds(
            GetImageLocalBounds(current, destination, resolvedSourceRect),
            current.LocalTransform,
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

    private readonly struct Snapshot
    {
        internal Snapshot(Sprite2D sprite)
        {
            Image = sprite.Image;
            X = sprite.X;
            Y = sprite.Y;
            Rotation = sprite.Rotation;
            Width = sprite.Width;
            Height = sprite.Height;
            SourceX = sprite.SourceX;
            SourceY = sprite.SourceY;
            SourceWidth = sprite.SourceWidth;
            SourceHeight = sprite.SourceHeight;
            Tint = sprite.Tint;
            Origin = sprite.Origin;
            Flip = sprite.Flip;
            Sampling = sprite.Sampling;
            LayerDepth = sprite.LayerDepth;
            Opacity = sprite.Opacity;
            IsRendered = UIElementVisibility.ParticipatesInRendering(sprite);
            AnimationPlaybackRate = sprite.AnimationPlaybackRate;
            IsAnimationPaused = sprite.IsAnimationPaused;
            // Frame source sizes only matter for an undeclared size or an anchor.
            FrameAffectsBounds = float.IsNaN(Width) || float.IsNaN(Height) || Origin.X != 0 || Origin.Y != 0;
            LocalTransform = Matrix3x2.CreateRotation(Rotation) * Matrix3x2.CreateTranslation(X, Y);
        }

        internal ImageReference? Image { get; }
        internal float X { get; }
        internal float Y { get; }
        internal float Rotation { get; }
        internal float Width { get; }
        internal float Height { get; }
        internal float SourceX { get; }
        internal float SourceY { get; }
        internal float SourceWidth { get; }
        internal float SourceHeight { get; }
        internal Color Tint { get; }
        internal DrawPoint Origin { get; }
        internal RenderSurface2DSpriteFlip Flip { get; }
        internal DrawSamplingMode Sampling { get; }
        internal float LayerDepth { get; }
        internal float Opacity { get; }
        internal bool IsRendered { get; }
        internal double AnimationPlaybackRate { get; }
        internal bool IsAnimationPaused { get; }
        internal bool FrameAffectsBounds { get; }
        internal Matrix3x2 LocalTransform { get; }
    }
}
