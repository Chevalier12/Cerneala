using System.Collections.ObjectModel;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;

namespace Cerneala.UI.Controls;

[ContentProperty(nameof(Children))]
public sealed class Scene2D : SceneNode2D
{
    public static readonly UiProperty<SceneOrderMode> OrderModeProperty =
        UiProperty<SceneOrderMode>.Register(
            nameof(OrderMode),
            typeof(Scene2D),
            new UiPropertyMetadata<SceneOrderMode>(
                SceneOrderMode.Source,
                UiPropertyOptions.AffectsRender,
                validateValue: Enum.IsDefined));

    public static readonly UiProperty<DrawPoint> TransformOriginProperty =
        UiProperty<DrawPoint>.Register(
            nameof(TransformOrigin),
            typeof(Scene2D),
            new UiPropertyMetadata<DrawPoint>(default, UiPropertyOptions.AffectsRender));

    private readonly List<SceneOrderEntry> effectiveOrder = [];
    private readonly CollisionWorld2D ownedCollisionWorld;
    private long collisionMutationVersion;

    public Scene2D()
    {
        ownedCollisionWorld = new CollisionWorld2D(this);
        Children = new ChildCollection(this);
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
        base.AttachSurface(surface);
        foreach (SceneNode2D child in Children)
        {
            child.AttachSurface(surface);
        }
    }

    internal override void Record(Scene2DRecordContext context)
    {
        if (!UIElementVisibility.ParticipatesInRendering(this) || Opacity <= 0)
        {
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
            IReadOnlyList<SceneOrderEntry> ordered = GetEffectiveOrder(childContext);
            for (int index = 0; index < ordered.Count; index++)
            {
                SceneOrderEntry entry = ordered[index];
                entry.Node.Record(childContext.WithSourceIndex(entry.SourceIndex));
            }
            // Debug presentation is a post-pass, never a gameplay order entry.
            for (int index = 0; index < Children.Count; index++)
            {
                if (Children[index] is Scene2DDebugOverlay overlay) { overlay.Record(childContext); }
            }
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

    internal override void ReleaseRenderCaches()
    {
        foreach (SceneNode2D child in Children)
        {
            child.ReleaseRenderCaches();
        }
    }

    internal override Matrix3x2 GetLocalTransform() =>
        SceneGeometry2D.CreateLocalTransform(this);

    internal IReadOnlyList<SceneOrderEntry> GetEffectiveOrder(
        Scene2DRecordContext context)
    {
        effectiveOrder.Clear();
        for (int index = 0; index < Children.Count; index++)
        {
            SceneNode2D child = Children[index];
            if (child is Scene2DDebugOverlay) { continue; }
            effectiveOrder.Add(new SceneOrderEntry(
                child,
                index,
                child.Layer,
                OrderMode == SceneOrderMode.LayerThenY
                    ? GetSceneYAnchor(child, context)
                    : 0));
        }

        if (OrderMode != SceneOrderMode.Source)
        {
            effectiveOrder.Sort(
                OrderMode == SceneOrderMode.LayerThenY
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
        foreach (SceneNode2D child in Children)
        {
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
            ArgumentNullException.ThrowIfNull(item);
            SceneNode2D previous = this[index];
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
