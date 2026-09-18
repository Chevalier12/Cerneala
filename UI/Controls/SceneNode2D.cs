using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Controls;

public abstract class SceneNode2D : UIElement, IInputSubtreeHost, IInputCoordinateSpace, IInputRouteGuard
{
    private InvalidOperationException? missingPrismDomainError;
    private PrismInputDependency.LocalInputCache? prismInputCache;

    internal bool TryGetLocalPrismInputOutset(PrismInstance instance, out Vector2 outset) =>
        (prismInputCache ??= new()).TryGet(instance, Root?.Scale ?? 1,
            this is Sprite2D ? GetLocalTransform() : Matrix3x2.Identity, out outset);

    protected SceneNode2D() => LogicalChildren.Changed += OnSceneChildrenChanged;

    public SceneSimulationContext2D? SimulationContext { get; private set; }

    internal override Cerneala.UI.Relay.UiRelay? OwnerRelay => SimulationContext?.Relay ?? base.OwnerRelay;

    internal override void ValidateParentChange(UIElement? parent, ElementChildRole role, bool ownerManaged)
    {
        base.ValidateParentChange(parent, role, ownerManaged);
        VerifyOwnerAccess();
        parent?.VerifyOwnerAccess();
        if (parent is not null && SimulationContext is { } context && ReferenceEquals(context.Scene, this))
        {
            throw new InvalidOperationException("Dispose the root scene's simulation context before reparenting it.");
        }
        if (parent is SceneNode2D { SimulationContext: { } destination }) { ValidateSimulationAttachment(destination); }
    }

    internal override void ValidateLifecycleRoot(UIRoot root)
    {
        if (SimulationContext is { } context && (context.IsHeadless || !ReferenceEquals(context.Relay, root.Relay)))
        {
            throw new InvalidOperationException("Dispose the independent simulation context before attaching its scene to UI.");
        }
        base.ValidateLifecycleRoot(root);
    }

    internal void ValidateSimulationAttachment(SceneSimulationContext2D context)
    {
        VerifyOwnerAccess();
        if (SimulationContext is not null && !ReferenceEquals(SimulationContext, context))
        {
            throw new InvalidOperationException("A scene node cannot belong to two simulation contexts.");
        }
        if (Root is not null && (context.IsHeadless || !ReferenceEquals(Root.Relay, context.Relay)))
        {
            throw new InvalidOperationException("The simulation context and UI root must have the same owner.");
        }
        ValidateDataLifecycle(context.Relay);
        foreach (SceneNode2D child in LogicalChildren.OfType<SceneNode2D>()) { child.ValidateSimulationAttachment(context); }
    }

    internal void AttachSimulationContext(SceneSimulationContext2D? context)
    {
        SceneSimulationContext2D? previous = SimulationContext;
        if (ReferenceEquals(previous, context)) { return; }
        previous?.Relay.VerifyAccess();
        context?.Relay.VerifyAccess();
        SceneSimulationContext2D scope = context ?? previous!;
        scope.BeginTreeChange();
        List<Exception>? failures = null;
        try
        {
            if (this is ISceneSpatialParticipant2D oldItems) { previous?.Unregister(oldItems); }
            if (previous?.IsHeadless == true)
            {
                try { DetachDataLifecycle(); }
                catch (Exception failure) { (failures ??= []).Add(failure); }
            }
            SimulationContext = context;
            try { OnSimulationContextChanged(previous); }
            catch (Exception failure) { (failures ??= []).Add(failure); }
            if (context?.IsHeadless == true)
            {
                try { AttachDataLifecycle(); }
                catch (Exception failure) { (failures ??= []).Add(failure); }
            }
            foreach (SceneNode2D child in LogicalChildren.OfType<SceneNode2D>().ToArray())
            {
                try { child.AttachSimulationContext(context); }
                catch (Exception failure) { (failures ??= []).Add(failure); }
            }
            if (this is ISceneSpatialParticipant2D newItems) { context?.Register(newItems); }
        }
        finally { scope.EndTreeChange(); }
        if (failures is not null) { throw new AggregateException(failures); }
    }

    internal virtual void OnSimulationContextChanged(SceneSimulationContext2D? previous) { }

    private void OnSceneChildrenChanged(object? sender, ElementTreeChange change)
    {
        if (change.Child is SceneNode2D child)
        {
            child.AttachSimulationContext(change.Kind == ElementTreeChangeKind.Added ? SimulationContext : null);
        }
    }

    public static readonly UiProperty<int> LayerProperty =
        UiProperty<int>.Register(
            nameof(Layer),
            typeof(SceneNode2D),
            new UiPropertyMetadata<int>(0, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<DrawRect?> PrismInputDomainProperty =
        UiProperty<DrawRect?>.Register(nameof(PrismInputDomain), typeof(SceneNode2D),
            new UiPropertyMetadata<DrawRect?>(null, UiPropertyOptions.AffectsRender,
                validateValue: value => value is null ||
                    float.IsFinite(value.Value.X) && float.IsFinite(value.Value.Y) &&
                    float.IsFinite(value.Value.Right) && float.IsFinite(value.Value.Bottom) &&
                    value.Value.Width > 0 && value.Value.Height > 0));

    public DrawRect? PrismInputDomain
    {
        get => GetValue(PrismInputDomainProperty);
        set => SetValue(PrismInputDomainProperty, value);
    }

    internal RenderSurface2D? Surface { get; private set; }

    // Scene nodes are rendered through their logical owner, not the layout tree.
    internal override UIElement? PrismVisualParent => base.PrismVisualParent ?? LogicalParent;

    internal int ActiveAnimationIndex { get; set; } = -1;

    internal virtual bool HasActiveAnimation => false;

    internal virtual bool AdvanceAnimation(TimeSpan frameTime) => false;

    internal void RefreshAnimationRegistration() => Surface?.RefreshAnimationRegistration(this);

    public int Layer
    {
        get => GetValue(LayerProperty);
        set => SetValue(LayerProperty, value);
    }

    internal virtual void AttachSurface(RenderSurface2D? surface)
    {
        Surface?.RemoveAnimationRegistration(this);
        Surface = surface;
        RefreshAnimationRegistration();
    }

    internal abstract void Record(Scene2DRecordContext context);

    internal virtual void CheckPresentation(ScenePresentationContext2D context)
    {
        if (!UIElementVisibility.ParticipatesInRendering(this) || Opacity <= 0) { return; }
        CheckPrismPresentation(context);
        foreach (SceneNode2D child in LogicalChildren.OfType<SceneNode2D>()) { child.CheckPresentation(context); }
    }

    internal void CheckPrismPresentation(ScenePresentationContext2D context)
    {
        if (PrismAttachment.TryGetRenderState(this, out PrismInstance? instance, out _))
        {
            if (!CheckPrismInputDomain(context, instance!)) { return; }
            // Traverse the same live resource dependencies used for recording,
            // without producing a second graph or owning a second image cache.
            DrawCommandListBuilder.ResolvePrismResources(this, instance!,
                ImageResourceAccess.Prepare, context.RequireImage);
        }
    }

    internal bool CheckPrismInputDomain(ScenePresentationContext2D context) =>
        !PrismAttachment.TryGetRenderState(this, out PrismInstance? instance, out _) ||
            CheckPrismInputDomain(context, instance!);

    private bool CheckPrismInputDomain(ScenePresentationContext2D context, PrismInstance instance)
    {
        if (SimulationContext is { SpatialItems.Count: > 0 } && PrismInputDomain is null &&
            PrismInputDependency.RequiresWholeInput(instance) && !TryGetLocalPrismInputOutset(instance, out _))
        {
            context.Require(missingPrismDomainError ??= new InvalidOperationException(
                "A scene Prism operation without supported automatic input requires a finite PrismInputDomain on its owning scene node."));
            return false;
        }
        missingPrismDomainError = null;
        return true;
    }

    bool IInputRouteGuard.IsInputRouteAvailable => Surface?.CanRouteSceneInput ?? true;

    internal virtual void ReleaseRenderCaches()
    {
        ReleaseImageResources();
        foreach (SceneNode2D child in LogicalChildren.OfType<SceneNode2D>())
        {
            child.ReleaseRenderCaches();
        }
    }

    internal SceneBounds2D GetLocalBounds()
    {
        return UIElementVisibility.ParticipatesInRendering(this)
            ? GetVisibleLocalBounds()
            : SceneBounds2D.Empty;
    }

    internal virtual System.Numerics.Matrix3x2 GetLocalTransform() =>
        System.Numerics.Matrix3x2.Identity;

    internal abstract SceneBounds2D GetVisibleLocalBounds();

    internal virtual SceneBounds2D GetHitTestLocalBounds() => SceneBounds2D.Empty;

    internal virtual bool ParticipatesInInputRoute => true;

    IEnumerable<UIElement> IInputSubtreeHost.GetInputSubtreeChildren() =>
        LogicalChildren
            .OfType<SceneNode2D>()
            .Where(static child => child.ParticipatesInInputRoute);

    LayoutRect IInputCoordinateSpace.GetRootBounds()
    {
        if (Surface is null)
        {
            return default;
        }

        SceneBounds2D bounds = SceneGeometry2D.GetInputBounds(
            this,
            SceneGeometry2D.GetLocalToSceneTransform(this) * Surface.GetSceneToRootTransform());
        return bounds.Kind == SceneBoundsKind.Known
            ? new LayoutRect(bounds.Bounds.X, bounds.Bounds.Y, bounds.Bounds.Width, bounds.Bounds.Height)
            : default;
    }

    bool IInputCoordinateSpace.TryRootToLocal(
        Vector2 rootPosition,
        out Vector2 localPosition)
    {
        if (Surface is null ||
            !Surface.TryRootToScene(rootPosition, out Vector2 scenePosition) ||
            !SceneGeometry2D.TryTransformToLocal(
                new DrawPoint(scenePosition.X, scenePosition.Y),
                SceneGeometry2D.GetLocalToSceneTransform(this),
                out DrawPoint localPoint))
        {
            localPosition = default;
            return false;
        }

        localPosition = new Vector2(localPoint.X, localPoint.Y);
        return true;
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        ProcessPendingAspect();
        RefreshAnimationRegistration();
    }

    protected override void OnDetached()
    {
        Surface?.RemoveAnimationRegistration(this);
        base.OnDetached();
    }

    public override void Invalidate(InvalidationRequest request)
    {
        base.Invalidate(request);
        ProcessPendingAspect();
        Surface?.InvalidateFrame();
    }

    private void ProcessPendingAspect()
    {
        if (Root is not UIRoot root ||
            !DirtyState.Has(InvalidationFlags.Aspect))
        {
            return;
        }

        root.AspectProcessor.Process(this);
        root.AspectQueue.Remove(this);
        DirtyState.Clear(InvalidationFlags.Aspect);
    }
}
