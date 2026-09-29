using System.Collections.ObjectModel;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;

namespace Cerneala.UI.Controls;

[ContentProperty(nameof(Children))]
public class Scene2D : SceneNode2D
{
    public static readonly UiProperty<SceneOrderMode> OrderModeProperty =
        UiProperty<SceneOrderMode>.Register(
            nameof(OrderMode),
            typeof(Scene2D),
            new UiPropertyMetadata<SceneOrderMode>(
                SceneOrderMode.Source,
                UiPropertyOptions.AffectsRender,
            validateValue: value => value is SceneOrderMode.Source or
                SceneOrderMode.Layer or SceneOrderMode.LayerThenY));

    public static readonly UiProperty<DrawPoint> TransformOriginProperty =
        UiProperty<DrawPoint>.Register(
            nameof(TransformOrigin),
            typeof(Scene2D),
            new UiPropertyMetadata<DrawPoint>(default, UiPropertyOptions.AffectsRender));

    private readonly List<SceneOrderEntry> effectiveOrder = [];
    private readonly CollisionWorld2D ownedCollisionWorld;
    private long collisionMutationVersion;
    private long localTransformVersion = -1;
    private Matrix3x2 localTransform;
    private readonly SceneChildBoundsIndex2D childBounds = new();

    public Scene2D()
    {
        ownedCollisionWorld = new CollisionWorld2D(this);
        Children = new ChildCollection(this);
        LogicalChildren.Changed += (_, _) => childBounds.Invalidate();
    }

    public Collection<SceneNode2D> Children { get; }

    public CollisionWorld2D CollisionWorld =>
        (SceneGeometry2D.FindRootScene(this) ?? this).ownedCollisionWorld;

    public SceneOrderMode OrderMode
    {
        get => GetValue(OrderModeProperty);
        set => SetValue(OrderModeProperty, value);
    }

    public DrawPoint TransformOrigin
    {
        get => GetValue(TransformOriginProperty);
        set => SetValue(TransformOriginProperty, value);
    }

    internal long CollisionMutationVersion =>
        (SceneGeometry2D.FindRootScene(this) ?? this).collisionMutationVersion;

    internal event Action<SceneCollisionMutation2D>? CollisionMutation;

    internal void NotifyCollisionMutation(
        SceneNode2D node,
        SceneCollisionMutationKind kind)
    {
        ArgumentNullException.ThrowIfNull(node);
        Scene2D root = SceneGeometry2D.FindRootScene(this) ?? this;
        root.VerifyOwnerAccess();
        if (root.collisionMutationVersion == long.MaxValue)
        {
            throw new InvalidOperationException("Scene collision mutation version space was exhausted.");
        }

        long version = ++root.collisionMutationVersion;
        root.ownedCollisionWorld.ApplyMutation(node, kind, version);
        root.CollisionMutation?.Invoke(new SceneCollisionMutation2D(version, node, kind));
    }

    internal void ResetOwnedCollisionWorlds()
    {
        ownedCollisionWorld.Reset();
        foreach (SceneNode2D child in Children)
        {
            if (child is Scene2D scene)
            {
                scene.ResetOwnedCollisionWorlds();
            }
        }
    }

    internal override void AttachSurface(RenderSurface2D? surface)
    {
        if (!ReferenceEquals(Surface, surface)) { ownedCollisionWorld.InvalidateSpatialRegions(); }
        base.AttachSurface(surface);
        foreach (SceneNode2D child in Children)
        {
            child.AttachSurface(surface);
        }
    }

    protected override void OnDetached()
    {
        ownedCollisionWorld.InvalidateSpatialRegions();
        base.OnDetached();
    }

    internal override void Record(Scene2DRecordContext context)
    {
        if (!UIElementVisibility.ParticipatesInRendering(this) || Opacity <= 0)
        {
            ReleaseRenderCaches();
            return;
        }

        Matrix3x2 localTransform = GetLocalTransform();
        bool hasTransform = localTransform != Matrix3x2.Identity;
        bool hasOpacity = Opacity < 1;
        if (hasTransform)
        {
            context.Frame.PushTransform(localTransform);
        }

        if (hasOpacity)
        {
            context.Frame.PushOpacity(Opacity);
        }

        Scene2DRecordContext childContext = context.WithLocalTransform(localTransform);
        try
        {
            using ScenePrismScope prism = childContext.HasPrism(this)
                ? childContext.BeginPrism(this, GetVisibleLocalBounds())
                : default;
            // Without an enclosing effect a clearly offscreen sprite records
            // nothing, so it is not visited at all.
            Span<SceneChildBoundsIndex2D.Entry> indexed = HasPrismInScope(childContext)
                ? default
                : childBounds.Refresh(this);
            SceneBounds2D visible = childContext.GetConservativeVisibleLocalBounds();
            if (OrderMode == SceneOrderMode.Source && indexed.Length > 0 && indexed.Length == Children.Count &&
                !childBounds.HasOverlay)
            {
                // Source order is slot order; the query skips only entries
                // that are prunable and clearly outside. A debug overlay
                // observes the full recorded order, so it keeps that path.
                foreach (int slot in childBounds.Query(visible))
                {
                    ref SceneChildBoundsIndex2D.Entry entry = ref indexed[slot];
                    if (entry.IsOverlay ||
                        (entry.CanPrune && SceneChildBoundsIndex2D.IsClearlyOutside(entry.VisibleBounds, visible)))
                    {
                        continue;
                    }
                    entry.MayHoldResources = true;
                    entry.Node.Record(childContext);
                }
                RecordOverlays(childContext);
                return;
            }

            IReadOnlyList<SceneOrderEntry> ordered = GetEffectiveOrder(childContext);
            for (int index = 0; index < ordered.Count; index++)
            {
                SceneNode2D node = ordered[index].Node;
                int slot = ordered[index].SourceIndex;
                if ((uint)slot < (uint)indexed.Length && ReferenceEquals(indexed[slot].Node, node))
                {
                    ref SceneChildBoundsIndex2D.Entry indexedEntry = ref indexed[slot];
                    if (indexedEntry.CanPrune && SceneChildBoundsIndex2D.IsClearlyOutside(indexedEntry.VisibleBounds, visible))
                    {
                        continue;
                    }
                    indexedEntry.MayHoldResources = true;
                }
                node.Record(childContext);
            }
            RecordOverlays(childContext);
        }
        finally
        {
            if (hasOpacity)
            {
                context.Frame.PopOpacity();
            }

            if (hasTransform)
            {
                context.Frame.PopTransform();
            }
        }
    }

    // Debug presentation is a post-pass, never a gameplay order entry.
    private void RecordOverlays(Scene2DRecordContext childContext)
    {
        for (int index = 0; index < Children.Count; index++)
        {
            if (Children[index] is Scene2DDebugOverlay overlay) { overlay.Record(childContext); }
        }
    }

    internal override void ReleaseRenderCaches()
    {
        ReleaseOwnImageResources();
        for (int index = 0; index < Children.Count; index++)
        {
            Children[index].ReleaseRenderCaches();
        }
    }

    // Every child traversal composes this transform; derive it once per
    // property-store version instead of re-reading ten properties per child.
    internal override Matrix3x2 GetLocalTransform()
    {
        long version = PropertyValueVersion;
        if (localTransformVersion != version)
        {
            localTransform = SceneGeometry2D.CreateLocalTransform(this);
            localTransformVersion = version;
        }
        return localTransform;
    }

    internal override void CheckPresentation(ScenePresentationContext2D context)
    {
        if (!UIElementVisibility.ParticipatesInRendering(this) || Opacity <= 0) { return; }
        CheckPrismPresentation(context);
        SceneBounds2D visible = context.GetChildVisibleBounds(this);
        Span<SceneChildBoundsIndex2D.Entry> entries = childBounds.Refresh(this);
        // Entries the query omits are prunable, clearly outside and hold nothing.
        foreach (int slot in childBounds.Query(visible))
        {
            ref SceneChildBoundsIndex2D.Entry entry = ref entries[slot];
            if (entry.CanPrune && SceneChildBoundsIndex2D.IsClearlyOutside(entry.PresentationBounds, visible))
            {
                // The child's own check would cull and release it.
                SceneChildBoundsIndex2D.Release(ref entry, Surface);
                continue;
            }

            entry.MayHoldResources = true;
            if (!SceneChildBoundsIndex2D.IsPresentationSettled(ref entry, visible, Surface))
            {
                entry.Node.CheckPresentation(context);
            }
        }
    }

    internal override void OnChildGeometryChanged(SceneNode2D child) => childBounds.MarkDirty(child);

    internal IReadOnlyList<SceneNode2D> GetInputCandidates(DrawPoint point, IReadOnlySet<SceneNode2D>? colliderPaths) =>
        childBounds.CollectInputCandidates(this, point, colliderPaths);

    private bool HasPrismInScope(Scene2DRecordContext context)
    {
        for (UIElement? owner = this; owner is SceneNode2D node; owner = owner.LogicalParent)
        {
            if (context.HasPrism(node)) { return true; }
        }
        return false;
    }

    internal IReadOnlyList<SceneOrderEntry> GetEffectiveOrder(
        Scene2DRecordContext context)
    {
        effectiveOrder.Clear();
        SceneOrderMode orderMode = OrderMode;
        Span<SceneChildBoundsIndex2D.Entry> indexed = orderMode == SceneOrderMode.Source
            ? childBounds.Refresh(this)
            : default;
        if (orderMode == SceneOrderMode.Source && indexed.Length == Children.Count)
        {
            // Source order needs no bounds; layers come from the index rather
            // than from every child object.
            for (int index = 0; index < indexed.Length; index++)
            {
                ref SceneChildBoundsIndex2D.Entry entry = ref indexed[index];
                if (entry.IsOverlay) { continue; }
                effectiveOrder.Add(new SceneOrderEntry(
                    entry.Node,
                    index,
                    entry.IsLive ? entry.Node.OrderLayer : entry.Layer,
                    0));
            }
            return effectiveOrder;
        }

        for (int index = 0; index < Children.Count; index++)
        {
            SceneNode2D child = Children[index];
            if (child is Scene2DDebugOverlay) { continue; }
            effectiveOrder.Add(new SceneOrderEntry(
                child,
                index,
                child.OrderLayer,
                orderMode == SceneOrderMode.LayerThenY
                    ? GetSceneYAnchor(child, context)
                    : 0));
        }

        if (orderMode != SceneOrderMode.Source)
        {
            effectiveOrder.Sort(
                orderMode == SceneOrderMode.LayerThenY
                    ? SceneOrderEntryComparer.LayerThenY
                    : SceneOrderEntryComparer.Layer);
        }

        return effectiveOrder;
    }

    internal IReadOnlyList<SceneOrderEntry> RecordedOrder => effectiveOrder;

    internal override SceneBounds2D GetVisibleLocalBounds()
    {
        if (Opacity <= 0)
        {
            return SceneBounds2D.Empty;
        }

        SceneBounds2D result = SceneBounds2D.Empty;
        for (int index = 0; index < Children.Count; index++)
        {
            SceneNode2D child = Children[index];
            if (child is Scene2DDebugOverlay) { continue; }
            SceneBounds2D childBounds = SceneGeometry2D.TransformBounds(
                child.GetLocalBounds(),
                child.GetLocalTransform());
            result = SceneGeometry2D.Union(result, childBounds);
            if (result.Kind == SceneBoundsKind.Unknown)
            {
                break;
            }
        }

        return result;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (SceneGeometry2D.IsSceneTransformProperty(args.Property) ||
            ReferenceEquals(args.Property, TransformOriginProperty))
        {
            NotifyCollisionMutation(this, SceneCollisionMutationKind.Geometry);
        }
        else if (ReferenceEquals(args.Property, UIElement.IsVisibleProperty) ||
                 ReferenceEquals(args.Property, UIElement.VisibilityProperty))
        {
            NotifyCollisionMutation(this, SceneCollisionMutationKind.Participation);
        }
    }

    private static float GetSceneYAnchor(
        SceneNode2D child,
        Scene2DRecordContext context)
    {
        SceneBounds2D sceneBounds = SceneGeometry2D.TransformBounds(
            child.GetLocalBounds(),
            child.GetLocalTransform() * context.LocalToSceneTransform);
        return sceneBounds.Kind == SceneBoundsKind.Known
            ? sceneBounds.Bounds.Bottom
            : 0;
    }

    private sealed class SceneOrderEntryComparer : IComparer<SceneOrderEntry>
    {
        internal static SceneOrderEntryComparer Layer { get; } = new(useY: false);

        internal static SceneOrderEntryComparer LayerThenY { get; } = new(useY: true);

        private readonly bool useY;

        private SceneOrderEntryComparer(bool useY)
        {
            this.useY = useY;
        }

        public int Compare(SceneOrderEntry left, SceneOrderEntry right)
        {
            int layer = left.Layer.CompareTo(right.Layer);
            if (layer != 0)
            {
                return layer;
            }

            if (useY)
            {
                int y = left.YAnchor.CompareTo(right.YAnchor);
                if (y != 0)
                {
                    return y;
                }
            }

            return left.SourceIndex.CompareTo(right.SourceIndex);
        }
    }

    private sealed class ChildCollection(Scene2D owner) : Collection<SceneNode2D>
    {
        protected override void InsertItem(int index, SceneNode2D item)
        {
            ArgumentNullException.ThrowIfNull(item);
            owner.LogicalChildren.Insert(index, item);
            base.InsertItem(index, item);
            if (item is Scene2D scene)
            {
                scene.ResetOwnedCollisionWorlds();
            }
            item.AttachSurface(owner.Surface);
            owner.NotifyCollisionMutation(item, SceneCollisionMutationKind.Structure);
            owner.Surface?.InvalidateFrame();
        }

        protected override void SetItem(int index, SceneNode2D item)
        {
            owner.VerifyOwnerAccess();
            ArgumentNullException.ThrowIfNull(item);
            SceneNode2D previous = this[index];
            if (ReferenceEquals(previous, item)) { return; }
            owner.LogicalChildren.ValidateInsertion(index, item);
            previous.AttachSurface(null);
            owner.LogicalChildren.Remove(previous);
            owner.LogicalChildren.Insert(index, item);
            base.SetItem(index, item);
            if (previous is Scene2D previousScene)
            {
                previousScene.ResetOwnedCollisionWorlds();
            }
            if (item is Scene2D scene)
            {
                scene.ResetOwnedCollisionWorlds();
            }
            item.AttachSurface(owner.Surface);
            owner.NotifyCollisionMutation(item, SceneCollisionMutationKind.Structure);
            owner.Surface?.InvalidateFrame();
        }

        protected override void RemoveItem(int index)
        {
            owner.VerifyOwnerAccess();
            SceneNode2D previous = this[index];
            previous.AttachSurface(null);
            owner.LogicalChildren.Remove(previous);
            base.RemoveItem(index);
            if (previous is Scene2D scene)
            {
                scene.ResetOwnedCollisionWorlds();
            }
            owner.NotifyCollisionMutation(previous, SceneCollisionMutationKind.Structure);
            owner.Surface?.InvalidateFrame();
        }

        protected override void ClearItems()
        {
            owner.VerifyOwnerAccess();
            foreach (SceneNode2D child in this)
            {
                child.AttachSurface(null);
                owner.LogicalChildren.Remove(child);
                if (child is Scene2D scene)
                {
                    scene.ResetOwnedCollisionWorlds();
                }
            }

            base.ClearItems();
            owner.NotifyCollisionMutation(owner, SceneCollisionMutationKind.Structure);
            owner.Surface?.InvalidateFrame();
        }
    }
}

internal readonly record struct SceneOrderEntry(
    SceneNode2D Node,
    int SourceIndex,
    int Layer,
    float YAnchor);
