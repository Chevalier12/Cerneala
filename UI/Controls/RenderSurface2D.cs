using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls;

public delegate void RenderSurface2DDrawEventHandler(
    RenderSurface2D sender,
    RenderSurface2DFrame frame);

public partial class RenderSurface2D : ContentControl,
    ITimeSensitiveRenderElement,
    IRenderSurface2DFrameSource,
    IInputSubtreeHost,
    IGeometricHitTestHost
{
    public static readonly UiProperty<Color> ClearColorProperty =
        UiProperty<Color>.Register(
            nameof(ClearColor),
            typeof(RenderSurface2D),
            new UiPropertyMetadata<Color>(
                Color.Transparent,
                UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<RenderSurface2DRedrawMode> RedrawModeProperty =
        UiProperty<RenderSurface2DRedrawMode>.Register(
            nameof(RedrawMode),
            typeof(RenderSurface2D),
            new UiPropertyMetadata<RenderSurface2DRedrawMode>(
                RenderSurface2DRedrawMode.Continuous,
                UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<Scene2D?> SceneProperty =
        UiProperty<Scene2D?>.Register(
            nameof(Scene),
            typeof(RenderSurface2D),
            new UiPropertyMetadata<Scene2D?>(null, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<DrawRect?> ViewBoxProperty =
        UiProperty<DrawRect?>.Register(
            nameof(ViewBox),
            typeof(RenderSurface2D),
            new UiPropertyMetadata<DrawRect?>(
                null,
                UiPropertyOptions.AffectsRender,
                validateValue: value => value is null ||
                    (value.Value.Width > 0 && value.Value.Height > 0)));

    public static readonly UiProperty<DrawBrushStretch> StretchProperty =
        UiProperty<DrawBrushStretch>.Register(
            nameof(Stretch),
            typeof(RenderSurface2D),
            new UiPropertyMetadata<DrawBrushStretch>(
                DrawBrushStretch.Fill,
                UiPropertyOptions.AffectsRender));

    private readonly Cerneala.UI.Resources.ImageResourceLeaseSet frameImages = new();
    private readonly Dictionary<object, IRenderSurface2DBackendState> backendStates =
        new(ReferenceEqualityComparer.Instance);
    private HashSet<IDrawImageInvalidationSource> imageDependencies =
        new(ReferenceEqualityComparer.Instance);
    private HashSet<IDrawImageInvalidationSource> pendingImageDependencies =
        new(ReferenceEqualityComparer.Instance);
    private RenderSurface2DDrawEventHandler? draw;
    private long frameVersion = 1;
    private long contentVersion = 1;
    private TimeSpan currentFrameTime;
    private readonly List<SceneNode2D> activeAnimations = [];
    private SceneSimulationContext2D? simulationContext;
    internal const int WarmPreparationTileBudget = 256;
    private int nextWarmPreparationIndex;
    private readonly HashSet<Collider2D> hitTestColliders =
        new(ReferenceEqualityComparer.Instance);

    public Color ClearColor
    {
        get => GetValue(ClearColorProperty);
        set => SetValue(ClearColorProperty, value);
    }

    public RenderSurface2DRedrawMode RedrawMode
    {
        get => GetValue(RedrawModeProperty);
        set => SetValue(RedrawModeProperty, value);
    }

    public Scene2D? Scene
    {
        get => GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    public DrawRect? ViewBox
    {
        get => GetValue(ViewBoxProperty);
        set => SetValue(ViewBoxProperty, value);
    }

    public DrawBrushStretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    public event RenderSurface2DDrawEventHandler? Draw
    {
        add
        {
            bool wasDrawingActive = IsDrawingActive;
            draw += value;
            HandleDrawingMutation(wasDrawingActive);
        }
        remove
        {
            bool wasDrawingActive = IsDrawingActive;
            draw -= value;
            HandleDrawingMutation(wasDrawingActive);
        }
    }

    public void InvalidateFrame() => InvalidateFrame(contentChanged: true);

    private void InvalidateFrame(bool contentChanged)
    {
        AdvanceFrameVersion(contentChanged);
        IncrementRenderVersion();
        Invalidate(InvalidationFlags.Render, "RenderSurface2D frame changed");
    }

    public bool TryRootToScene(Vector2 rootPosition, out Vector2 scenePosition)
    {
        if (!float.IsFinite(rootPosition.X) || !float.IsFinite(rootPosition.Y) ||
            !Matrix3x2.Invert(GetSceneToRootTransform(), out Matrix3x2 rootToScene))
        {
            scenePosition = default;
            return false;
        }

        scenePosition = Vector2.Transform(rootPosition, rootToScene);
        return float.IsFinite(scenePosition.X) && float.IsFinite(scenePosition.Y);
    }

    public Vector2 SceneToRoot(Vector2 scenePosition)
    {
        if (!float.IsFinite(scenePosition.X) || !float.IsFinite(scenePosition.Y))
        {
            throw new ArgumentOutOfRangeException(
                nameof(scenePosition),
                scenePosition,
                "Scene coordinates must be finite.");
        }

        return Vector2.Transform(scenePosition, GetSceneToRootTransform());
    }

    public override void Invalidate(InvalidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        base.Invalidate(request);
        if (request.Flags.HasFlag(InvalidationFlags.Resource) && IsDrawingActive)
        {
            AdvanceFrameVersion();
        }
    }

    bool ITimeSensitiveRenderElement.UpdateRenderTime(TimeSpan frameTime)
    {
        if (frameTime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(frameTime));
        }
        currentFrameTime = frameTime;
        RefreshSpatialItems(ArrangedBounds.Width, ArrangedBounds.Height);
        CheckPresentation(GetPresentationBounds());
        bool animationChanged = false;
        if (IsAttached)
        {
            for (int index = activeAnimations.Count - 1; index >= 0; index--)
            {
                SceneNode2D node = activeAnimations[index];
                if (node.AdvanceAnimation(frameTime))
                {
                    node.IncrementRenderVersion();
                    animationChanged = true;
                }
                if (!node.HasActiveAnimation)
                {
                    RemoveAnimationRegistration(node);
                }
            }
        }
        if (!animationChanged &&
            (!IsDrawingActive || RedrawMode != RenderSurface2DRedrawMode.Continuous))
        {
            return false;
        }

        // Continuous scene recording is not itself a content mutation. Imperative
        // callbacks may depend on time or external state, so remain conservative.
        InvalidateFrame(animationChanged || draw is not null);
        return true;
    }

    internal int ActiveAnimationCount => activeAnimations.Count;

    internal void RegisterSpatialItems(ISceneSpatialParticipant2D items)
    {
        simulationContext?.Register(items);
        RefreshSpatialItems();
    }

    internal void UnregisterSpatialItems(ISceneSpatialParticipant2D items)
    {
        simulationContext?.Unregister(items);
        RefreshSpatialItems();
    }

    internal void CompleteWarmPreparation(IReadOnlyList<TileMap2D.WarmPreparationRequest> requests)
    {
        if (requests.Count == 0) { return; }
        int start = nextWarmPreparationIndex % requests.Count;
        int remaining = WarmPreparationTileBudget;
        bool granted = false;
        List<Exception>? failures = null;
        for (int offset = 0; offset < requests.Count; offset++)
        {
            int index = (start + offset) % requests.Count;
            try
            {
                int used = requests[index].Complete(remaining);
                remaining -= used;
                if (used > 0 && !granted)
                {
                    // Rotate after the first recipient, not the last one:
                    // smaller chunks filling a remainder must not starve a
                    // map whose next chunk needs the entire next-frame budget.
                    nextWarmPreparationIndex = (index + 1) % requests.Count;
                    granted = true;
                }
            }
            catch (Exception error)
            {
                (failures ??= []).Add(error);
                // Work before a failure may already have consumed budget.
                // Finish every map's retirement, but do no more preparation.
                remaining = 0;
            }
        }
        if (failures is not null) { throw new AggregateException(failures); }
    }

    internal void RefreshSpatialItems() => RefreshSpatialItems(ArrangedBounds.Width, ArrangedBounds.Height);

    private void RefreshSpatialItems(float width, float height)
    {
        simulationContext?.RefreshSpatialItems(width, height);
    }

    internal SceneBounds2D GetSpatialViewport(SceneNode2D items) =>
        GetSpatialViewport(items, ArrangedBounds.Width, ArrangedBounds.Height);

    internal SceneBounds2D GetSpatialViewport(SceneNode2D items, float width, float height)
    {
        if (width <= 0 || height <= 0) { return SceneBounds2D.Empty; }
        (int pixelWidth, int pixelHeight) = RenderSurface2DGeometry.GetPixelSize(width, height, Root?.Scale ?? 1);
        DrawRect bounds = new(0, 0, pixelWidth, pixelHeight);
        Matrix3x2 transform = SceneGeometry2D.GetLocalToSceneTransform(items);
        if (ViewBox is DrawRect viewBox) { transform *= CreateViewBoxTransform(viewBox, bounds, Stretch); }
        return SceneGeometry2D.TryTransformBoundsToLocal(bounds, transform, out DrawRect localBounds)
            ? SceneBounds2D.Known(localBounds) : SceneBounds2D.Unknown;
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        LayoutRect arranged = base.ArrangeCore(context);
        RefreshSpatialItems(arranged.Width, arranged.Height);
        return arranged;
    }

    internal void RefreshAnimationRegistration(SceneNode2D node)
    {
        if (!node.IsAttached || !node.HasActiveAnimation)
        {
            RemoveAnimationRegistration(node);
        }
        else if (node.ActiveAnimationIndex < 0)
        {
            node.ActiveAnimationIndex = activeAnimations.Count;
            activeAnimations.Add(node);
        }
    }

    internal void RemoveAnimationRegistration(SceneNode2D node)
    {
        int index = node.ActiveAnimationIndex;
        if (index < 0)
        {
            return;
        }
        int last = activeAnimations.Count - 1;
        SceneNode2D moved = activeAnimations[last];
        activeAnimations[index] = moved;
        moved.ActiveAnimationIndex = index;
        activeAnimations.RemoveAt(last);
        node.ActiveAnimationIndex = -1;
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (Scene is { } scene) { simulationContext = new(scene, this); }
        if (IsDrawingActive)
        {
            AdvanceFrameVersion();
        }
    }

    protected override void OnDetached()
    {
        StopSimulationContext();
        foreach (SceneNode2D node in activeAnimations)
        {
            node.ActiveAnimationIndex = -1;
        }
        activeAnimations.Clear();
        DisposeManagedSession();
        base.OnDetached();
    }

    protected override void OnRender(RenderContext context)
    {
        Border.RenderBackground(this, context);
        DrawRect bounds = Border.ToDrawRect(context.Bounds);
        if (bounds.Width > 0 &&
            bounds.Height > 0 &&
            IsDrawingActive)
        {
            context.DrawingContext.DrawRenderSurface2D(
                this,
                bounds,
                Color.White);
        }

        Border.RenderBorder(this, context);
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        if (args is UiPropertyChangedEventArgs<Scene2D?> sceneChange &&
            ReferenceEquals(args.Property, SceneProperty))
        {
            StopSimulationContext();
            sceneChange.OldValue?.AttachSurface(null);
            if (sceneChange.OldValue is not null)
            {
                LogicalChildren.Remove(sceneChange.OldValue);
            }

            if (sceneChange.NewValue is not null)
            {
                LogicalChildren.Add(sceneChange.NewValue);
                sceneChange.NewValue.AttachSurface(this);
                if (IsAttached)
                {
                    simulationContext = new(sceneChange.NewValue, this);
                    RefreshSpatialItems();
                }
            }
        }

        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, ClearColorProperty) ||
            ReferenceEquals(args.Property, RedrawModeProperty) ||
            ReferenceEquals(args.Property, ViewBoxProperty) ||
            ReferenceEquals(args.Property, StretchProperty))
        {
            AdvanceFrameVersion();
        }

        if (ReferenceEquals(args.Property, SceneProperty))
        {
            if (!IsDrawingActive)
            {
                DisposeManagedSession();
            }

            InvalidateFrame();
        }
    }

    internal bool IsDrawingActiveForTests => IsDrawingActive;

    IEnumerable<UIElement> IInputSubtreeHost.GetInputSubtreeChildren()
    {
        if (Scene is not null && CanRouteSceneInput)
        {
            yield return Scene;
        }
    }

    internal override void ValidatePropertyMutation(UiProperty property, object? value)
    {
        base.ValidatePropertyMutation(property, value);
        if (ReferenceEquals(property, SceneProperty) && value is Scene2D scene && !ReferenceEquals(scene, Scene))
        {
            // Reject a second owner before the property store or either tree changes.
            LogicalChildren.ValidateInsertion(LogicalChildren.Count, scene);
        }
    }

    private void StopSimulationContext()
    {
        SceneSimulationContext2D? previous = simulationContext;
        simulationContext = null;
        previous?.Retire();
    }

    UIElement? IGeometricHitTestHost.HitTestGeometry(
        ElementInputRouteMap routeMap,
        float rootX,
        float rootY,
        HitTestFilter filter)
    {
        Scene2D? scene = Scene;
        if (scene is null || !CanRouteSceneInput ||
            !TryRootToScene(new Vector2(rootX, rootY), out Vector2 scenePosition))
        {
            return null;
        }

        UIElement? hit = SceneHitTest2D.HitTest(
            scene,
            scenePosition,
            filter,
            hitTestColliders);
        return hit is not null && routeMap.TryGetId(hit, out _)
            ? hit
            : null;
    }

    private bool IsDrawingActive => draw is not null || Scene is not null;

    Color IRenderSurface2DFrameSource.ClearColor => ClearColor;

    long IRenderSurface2DFrameSource.FrameVersion => frameVersion;

    void IRenderSurface2DFrameSource.RecordFrame(
        DrawCommandList commands,
        DrawRect bounds)
    {
        using Cerneala.UI.Resources.ImageResourceLeaseSet.Scope imageUsage = frameImages.Begin();
        pendingImageDependencies.Clear();
        RenderSurface2DFrame frame = new(
            commands,
            bounds,
            currentFrameTime,
            contentVersion,
            TrackImageDependency);
        try
        {
            InvokeDraw(frame);
            if (CheckPresentation(bounds))
            {
                int sceneStart = commands.Count;
                Scene2D? recordedScene = Scene;
                DrawRect? recordedViewBox = ViewBox;
                DrawBrushStretch recordedStretch = Stretch;
                RecordScene(frame, bounds);
                // A source or camera can be superseded while recording. Do not
                // submit even the already-recorded prefix of that scene.
                bool current = ReferenceEquals(recordedScene, Scene) && recordedViewBox == ViewBox && recordedStretch == Stretch;
                if (!CheckPresentation(bounds) || !current)
                {
                    commands.Truncate(sceneStart);
                    frame.DiscardOptionalPreparation();
                    if (!current) { SetPresentation(RenderSurface2DPresentationState.Loading); }
                }
            }
            frame.Complete();
            CommitImageDependencies();
        }
        catch (Exception failure)
        {
            pendingImageDependencies.Clear();
            try { frame.Abort(); }
            catch (Exception cleanupFailure) { throw new AggregateException(failure, cleanupFailure); }
            throw;
        }
    }

    IRenderSurface2DBackendState? IRenderSurface2DFrameSource.GetBackendState(
        object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return backendStates.GetValueOrDefault(owner);
    }

    void IRenderSurface2DFrameSource.SetBackendState(
        object owner,
        IRenderSurface2DBackendState? state)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (backendStates.Remove(owner, out IRenderSurface2DBackendState? previous) &&
            !ReferenceEquals(previous, state))
        {
            previous.Dispose();
            Scene?.ReleaseRenderCaches();
        }

        if (state is not null)
        {
            backendStates[owner] = state;
        }
    }

    private void HandleDrawingMutation(bool wasDrawingActive)
    {
        bool isDrawingActive = IsDrawingActive;
        if (!isDrawingActive)
        {
            DisposeManagedSession();
        }

        InvalidateFrame();
    }

    private void InvokeDraw(RenderSurface2DFrame frame)
    {
        if (draw is null)
        {
            return;
        }

        foreach (RenderSurface2DDrawEventHandler handler in draw.GetInvocationList())
        {
            handler(this, frame);
        }
    }

    private void RecordScene(RenderSurface2DFrame frame, DrawRect bounds)
    {
        Scene2D? scene = Scene;
        if (scene is null)
        {
            return;
        }

        DrawRect? viewBox = ViewBox;
        if (viewBox is null)
        {
            scene.Record(new Scene2DRecordContext(
                this,
                frame,
                Matrix3x2.Identity,
                bounds));
            return;
        }

        Matrix3x2 transform = CreateViewBoxTransform(viewBox.Value, bounds, Stretch);
        frame.PushClip(bounds);
        frame.PushTransform(transform);
        try
        {
            scene.Record(new Scene2DRecordContext(
                this,
                frame,
                transform,
                bounds));
        }
        finally
        {
            frame.PopTransform();
            frame.PopClip();
        }
    }

    private static Matrix3x2 CreateViewBoxTransform(
        DrawRect viewBox,
        DrawRect bounds,
        DrawBrushStretch stretch)
    {
        float scaleX = stretch == DrawBrushStretch.None ? 1 : bounds.Width / viewBox.Width;
        float scaleY = stretch == DrawBrushStretch.None ? 1 : bounds.Height / viewBox.Height;
        if (stretch == DrawBrushStretch.Uniform)
        {
            scaleX = scaleY = MathF.Min(scaleX, scaleY);
        }
        else if (stretch == DrawBrushStretch.UniformToFill)
        {
            scaleX = scaleY = MathF.Max(scaleX, scaleY);
        }

        float contentWidth = viewBox.Width * scaleX;
        float contentHeight = viewBox.Height * scaleY;
        float offsetX = bounds.X + ((bounds.Width - contentWidth) * 0.5f);
        float offsetY = bounds.Y + ((bounds.Height - contentHeight) * 0.5f);
        return Matrix3x2.CreateTranslation(-viewBox.X, -viewBox.Y) *
            Matrix3x2.CreateScale(scaleX, scaleY) *
            Matrix3x2.CreateTranslation(offsetX, offsetY);
    }

    internal Matrix3x2 GetSceneToRootTransform()
    {
        (int pixelWidth, int pixelHeight) = RenderSurface2DGeometry.GetPixelSize(
            ArrangedBounds.Width, ArrangedBounds.Height, Root?.Scale ?? 1);
        Matrix3x2 pixelsToLayout = Matrix3x2.CreateScale(
            ArrangedBounds.Width / pixelWidth,
            ArrangedBounds.Height / pixelHeight) *
            Matrix3x2.CreateTranslation(ArrangedBounds.X, ArrangedBounds.Y);
        DrawRect? viewBox = ViewBox;
        Matrix3x2 sceneToPixels = viewBox is null
            ? Matrix3x2.Identity
            : CreateViewBoxTransform(
                viewBox.Value,
                new DrawRect(0, 0, pixelWidth, pixelHeight),
                Stretch);
        return sceneToPixels * pixelsToLayout *
            InputCoordinateConverter.GetElementToRootTransform(this);
    }

    private void TrackImageDependency(IDrawImage image)
    {
        frameImages.Retain(image, Root?.ImageResourceCache);
        if (image is IDrawImageInvalidationSource dependency)
        {
            pendingImageDependencies.Add(dependency);
        }
    }

    private void CommitImageDependencies()
    {
        foreach (IDrawImageInvalidationSource dependency in imageDependencies)
        {
            if (!pendingImageDependencies.Contains(dependency))
            {
                dependency.ContentChanged -= OnImageContentChanged;
            }
        }

        foreach (IDrawImageInvalidationSource dependency in pendingImageDependencies)
        {
            if (!imageDependencies.Contains(dependency))
            {
                dependency.ContentChanged += OnImageContentChanged;
            }
        }

        (imageDependencies, pendingImageDependencies) =
            (pendingImageDependencies, imageDependencies);
        pendingImageDependencies.Clear();
    }

    private void OnImageContentChanged(object? sender, EventArgs args)
    {
        if (RedrawMode == RenderSurface2DRedrawMode.OnDemand)
        {
            InvalidateFrame();
        }
    }

    private void AdvanceFrameVersion(bool contentChanged = true)
    {
        frameVersion = frameVersion == long.MaxValue
            ? 1
            : frameVersion + 1;
        if (contentChanged)
        {
            contentVersion = contentVersion == long.MaxValue ? 1 : contentVersion + 1;
        }
    }

    internal void ReleaseDrawingResources() => DisposeManagedSession();

    private void DisposeManagedSession()
    {
        Scene?.ReleaseRenderCaches();
        foreach (IDrawImageInvalidationSource dependency in imageDependencies)
        {
            dependency.ContentChanged -= OnImageContentChanged;
        }

        imageDependencies.Clear();
        pendingImageDependencies.Clear();
        foreach (IRenderSurface2DBackendState state in backendStates.Values)
        {
            state.Dispose();
        }

        backendStates.Clear();
        frameImages.Clear();
    }
}
