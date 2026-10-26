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
    private readonly ColliderSlot2D visionSlot;
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
    // The mirror the Vision cone last used or was told about; see RefreshVisionMirror.
    private SpriteMirror2D visionMirror;

    public Sprite2D()
    {
        colliderSlot = new ColliderSlot2D(this, ColliderSlotRole2D.Body);
        visionSlot = new ColliderSlot2D(this, ColliderSlotRole2D.Vision);
    }

    public Collider2D? Collider
    {
        get => colliderSlot.Value;
        set => colliderSlot.Value = value;
    }

    /// <summary>
    /// Gets or sets the cone this sprite sees with, or <see langword="null"/> for none. The cone is a logical child of
    /// the sprite in a slot of its own, separate from <see cref="Collider"/>, and it moves, turns and mirrors with the sprite.
    /// </summary>
    public ConeCollider2D? Vision
    {
        get => (ConeCollider2D?)visionSlot.Value;
        set
        {
            visionSlot.Value = value;
            // A sprite with a Vision is never parked (CanParkAnimation), so one that was parked while it had none resumes.
            if (value is not null && IsAnimationParked)
            {
                Surface?.UnparkAnimation(this);
            }
        }
    }

    internal override void AttachSurface(RenderSurface2D? surface)
    {
        base.AttachSurface(surface);
        colliderSlot.AttachSurface(surface);
        visionSlot.AttachSurface(surface);
    }

    // Shapes use destination units relative to the sprite anchor. Image sizing,
    // crop, Origin, Flip and animation frames change presentation, not collision geometry; the one
    // exception is a Vision cone, which mirrors with the image (see SpriteMirror2D).
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

    // Changing it requests no frame: an awake node is delivered on every time advance of its surface, drawn or not.
    public static readonly UiProperty<bool> IsAwakeProperty =
        UiProperty<bool>.Register(
            nameof(IsAwake),
            typeof(Sprite2D),
            new UiPropertyMetadata<bool>(false));

    /// <summary>
    /// Gets or sets whether this sprite receives <see cref="Awake"/> once per time advance of the render surface that shows
    /// its scene. The default is <see langword="false"/>. The sprite receives the event whether or not it is drawn, so
    /// the game keeps going off screen; it receives none while it is not in a scene shown by a surface that is attached to
    /// a UI root.
    /// </summary>
    public bool IsAwake
    {
        get => GetValue(IsAwakeProperty);
        set => SetValue(IsAwakeProperty, value);
    }

    /// <summary>
    /// Occurs once per time advance of the render surface while <see cref="IsAwake"/> is <see langword="true"/>. The first
    /// event after the sprite became awake, or entered a shown scene while awake, reports a zero
    /// <see cref="FrameTimeEventArgs.Elapsed"/>; later events report the elapsed time of the frame. A handler can set
    /// <see cref="IsAwake"/> to <see langword="false"/> to stop the events from the next frame.
    /// </summary>
    public event EventHandler<FrameTimeEventArgs>? Awake
    {
        add => AddAwakeHandler(value);
        remove => RemoveAwakeHandler(value);
    }

    internal override bool WantsAwake => IsAwake;

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
            RefreshVisionMirror();
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
        if (changed) { RefreshVisionMirror(); }
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

    // A parked sprite is not visited for its frame changes while it is culled. The cone of a Vision lives in the
    // collision world whether or not the sprite is drawn, and its frames flip it, so such a sprite stays visited.
    internal override bool CanParkAnimation => !AnimationFrameAffectsPresentation && visionSlot.Value is null;

    internal override long NextAnimationChangeTicks => animationPlayback.NextChangeClockTicks;

    internal override bool AdvanceScheduledAnimation()
    {
        bool changed = animationPlayback.AdvanceToClock();
        if (changed && Current.FrameAffectsBounds)
        {
            NotifyContainerGeometryChanged();
        }
        if (changed) { RefreshVisionMirror(); }
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
            ResolveGeometry(current, geometry, out SpriteGeometry culledGeometry);
            if (!context.IntersectsVisibleLocalBounds(SceneBounds2D.Known(GetDrawBounds(current, culledGeometry))) &&
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

        ResolveGeometry(current, source, out SpriteGeometry placement);
        DrawRect destination = placement.Destination;
        DrawRect drawBounds = GetDrawBounds(current, placement);
        SceneBounds2D bounds = SceneBounds2D.Known(drawBounds);
        if (!context.IntersectsVisibleLocalBounds(bounds) && !HasPrismInSceneAncestry(context))
        {
            return;
        }
        DrawImageOptions options = new(
            source: placement.SourceRect,
            tint: tint,
            rotation: current.Rotation,
            origin: placement.DrawOrigin,
            flip: (DrawImageFlip)placement.Flip,
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
            ResolveGeometry(current, source, out SpriteGeometry geometry);
            localBounds = SceneBounds2D.Known(geometry.LocalBounds);
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
        // A trimmed frame is sized and anchored by its logical (untrimmed) frame.
        DrawSize? frame = animationPlayback.CurrentFrame?.FrameSize;
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
        if (ReferenceEquals(args.Property, IsAwakeProperty))
        {
            RefreshAwakeRegistration();
        }
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

        // After the playback follows the new selection: the cone mirrors with the image that is drawn now.
        // Body colliders ignore all of these properties.
        if (visionSlot.Value is not null && IsVisionMirrorProperty(args.Property))
        {
            RefreshVisionMirror();
        }
    }

    // The properties that decide the flip the image is drawn with or the rectangle it is drawn in.
    private static bool IsVisionMirrorProperty(UiProperty property) =>
        ReferenceEquals(property, FlipProperty) ||
        ReferenceEquals(property, ImageProperty) ||
        ReferenceEquals(property, UIElement.WidthProperty) ||
        ReferenceEquals(property, UIElement.HeightProperty) ||
        ReferenceEquals(property, OriginProperty) ||
        ReferenceEquals(property, SourceXProperty) ||
        ReferenceEquals(property, SourceYProperty) ||
        ReferenceEquals(property, SourceWidthProperty) ||
        ReferenceEquals(property, SourceHeightProperty) ||
        ReferenceEquals(property, AnimationsProperty) ||
        ReferenceEquals(property, AnimationStateProperty);

    // The mirror the cone uses: read by the cone each time its geometry is created, so the value kept here is the
    // one the collision world last saw.
    internal SpriteMirror2D ReadVisionMirror()
    {
        visionMirror = ComputeVisionMirror();
        return visionMirror;
    }

    // Tells the collision world that the cone moved, but only when the mirror differs from the one it last used.
    // Frame changes that keep the composed flip, and size changes of an unflipped axis, mutate nothing.
    // Runs from property changes and from animation advances, which are the time phase. It does not run while a frame
    // is recorded: the one advance that recording can start is unparking, and a sprite with a Vision is never parked.
    private void RefreshVisionMirror()
    {
        if (visionSlot.Value is not { } vision) { return; }

        SpriteMirror2D mirror = ComputeVisionMirror();
        if (mirror == visionMirror) { return; }

        visionMirror = mirror;
        SceneGeometry2D.FindRootScene(this)?.NotifyCollisionMutation(vision, SceneCollisionMutationKind.Geometry);
    }

    private SpriteMirror2D ComputeVisionMirror()
    {
        RenderSurface2DSpriteFlip flip = ComposeFlip(Current.Flip, animationPlayback.CurrentFrame?.Flip ?? RenderSurface2DSpriteFlip.None);
        if (flip == RenderSurface2DSpriteFlip.None) { return default; }

        // The image flips inside the rectangle it is drawn in, so that rectangle's centre lines are the axes. A sprite
        // with no known rectangle (no image and no declared size) mirrors across its anchor.
        SceneBounds2D local = GetHitTestLocalBounds();
        if (local.Kind != SceneBoundsKind.Known) { return new SpriteMirror2D(flip, 0, 0); }

        DrawRect bounds = local.Bounds;
        return new SpriteMirror2D(
            flip,
            (flip & RenderSurface2DSpriteFlip.Horizontal) != 0 ? bounds.X + (bounds.Width / 2) : 0,
            (flip & RenderSurface2DSpriteFlip.Vertical) != 0 ? bounds.Y + (bounds.Height / 2) : 0);
    }

    // A trimmed animation frame draws only its visible pixels, but the sprite's
    // size, Origin and bounds describe the logical (untrimmed) frame. The draw
    // moves the image origin by the pixels' offset inside that frame, so each
    // pixel lands where the untrimmed frame would have put it.
    private void ResolveGeometry(in Snapshot current, IDrawImage source, out SpriteGeometry geometry)
    {
        SpriteAnimationFrame? frame = animationPlayback.CurrentFrame;
        DrawRect? sourceRect = frame?.SourceRect;
        if (sourceRect is null &&
            (current.SourceX != 0 || current.SourceY != 0 || !float.IsNaN(current.SourceWidth) || !float.IsNaN(current.SourceHeight)))
        {
            sourceRect = new DrawRect(current.SourceX, current.SourceY,
                float.IsNaN(current.SourceWidth) ? source.Width - current.SourceX : current.SourceWidth,
                float.IsNaN(current.SourceHeight) ? source.Height - current.SourceY : current.SourceHeight);
        }
        DrawRect resolvedSourceRect = DrawImageGeometry.ResolveSourceRect(source, sourceRect);
        float sourceWidth = resolvedSourceRect.Width;
        float sourceHeight = resolvedSourceRect.Height;
        float frameWidth = frame?.FrameSize.Width ?? sourceWidth;
        float frameHeight = frame?.FrameSize.Height ?? sourceHeight;
        float width = float.IsNaN(current.Width) ? frameWidth : current.Width;
        float height = float.IsNaN(current.Height) ? frameHeight : current.Height;
        RenderSurface2DSpriteFlip flip = ComposeFlip(current.Flip, frame?.Flip ?? RenderSurface2DSpriteFlip.None);

        float drawnWidth = sourceWidth == frameWidth ? width : sourceWidth * width / frameWidth;
        float drawnHeight = sourceHeight == frameHeight ? height : sourceHeight * height / frameHeight;
        DrawPoint drawOrigin = current.Origin;
        if (frame is not null)
        {
            DrawPoint offset = frame.Offset;
            float offsetX = (flip & RenderSurface2DSpriteFlip.Horizontal) != 0
                ? frameWidth - offset.X - sourceWidth
                : offset.X;
            float offsetY = (flip & RenderSurface2DSpriteFlip.Vertical) != 0
                ? frameHeight - offset.Y - sourceHeight
                : offset.Y;
            drawOrigin = new DrawPoint(current.Origin.X - offsetX, current.Origin.Y - offsetY);
        }

        geometry = new SpriteGeometry(
            sourceRect,
            new DrawRect(current.X, current.Y, drawnWidth, drawnHeight),
            new DrawRect(current.X, current.Y, width, height),
            new DrawRect(
                -(current.Origin.X * width / frameWidth),
                -(current.Origin.Y * height / frameHeight),
                width,
                height),
            drawOrigin,
            flip);
    }

    private static DrawRect GetDrawBounds(in Snapshot current, in SpriteGeometry geometry)
    {
        return SceneGeometry2D.TryTransformBounds(
            geometry.LocalBounds,
            current.LocalTransform,
            out DrawRect bounds)
            ? bounds
            : geometry.LogicalDestination;
    }

    private readonly struct SpriteGeometry
    {
        internal SpriteGeometry(
            DrawRect? sourceRect,
            DrawRect destination,
            DrawRect logicalDestination,
            DrawRect localBounds,
            DrawPoint drawOrigin,
            RenderSurface2DSpriteFlip flip)
        {
            SourceRect = sourceRect;
            Destination = destination;
            LogicalDestination = logicalDestination;
            LocalBounds = localBounds;
            DrawOrigin = drawOrigin;
            Flip = flip;
        }

        // The source rectangle to draw, or null for the whole image.
        internal DrawRect? SourceRect { get; }

        // Where the visible pixels draw; smaller than the logical frame when trimmed.
        internal DrawRect Destination { get; }

        // The untrimmed frame at the sprite position.
        internal DrawRect LogicalDestination { get; }

        // Bounds of the untrimmed frame relative to the sprite anchor.
        internal DrawRect LocalBounds { get; }

        internal DrawPoint DrawOrigin { get; }

        internal RenderSurface2DSpriteFlip Flip { get; }
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
