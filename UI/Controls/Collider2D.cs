using System.Numerics;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Controls;

public abstract class Collider2D : SceneNode2D
{
    private long simulatedVersion = -1;
    private bool isSimulated;
    private EventHandler<ColliderContactEventArgs>? entered;
    private EventHandler<ColliderContactEventArgs>? exited;
    // Contact state lives on the colliders, so it survives a collision world rebuild
    // and moves with a collider that changes its root scene.
    private HashSet<Collider2D>? reportedContacts;
    private HashSet<Collider2D>? reportedBy;

    internal override void ValidateParentChange(UIElement? parent, ElementChildRole role, bool ownerManaged)
    {
        UIElement? candidate = parent ?? LogicalParent;
        if (role != ElementChildRole.Logical || candidate is null || !AllowsCollisionParent(candidate))
        {
            throw new InvalidOperationException("Live colliders can only be owned by Sprite2D, or be the Vision of a Sprite2D or Scene2D; static Tile colliders are immutable data, never standalone scene nodes.");
        }
        if (!ownerManaged)
        {
            throw new InvalidOperationException("Change collider ownership through the owner's Collider or Vision property, not LogicalChildren or VisualChildren.");
        }
    }

    internal virtual bool AllowsCollisionParent(UIElement parent) => parent is Sprite2D;

    // The owner whose Vision slot holds this collider, set by that slot while it holds the collider.
    // A collider with an owner here is a sensor in the collision world: it never blocks, is never picked,
    // and only its owner learns what it touches.
    internal SceneNode2D? VisionOwner { get; set; }

    internal bool IsVisionSensor => VisionOwner is not null;

    // A sensor takes no part in input: it is not an input route node or a Servo target, and it adds nothing to the
    // input bounds of its owner, which Servo aims at. The slot sets VisionOwner before the cone joins the tree and
    // clears it after the cone leaves, so no route built in between sees another value.
    internal override bool ParticipatesInInputRoute => base.ParticipatesInInputRoute && !IsVisionSensor;

    public static readonly UiProperty<bool> EnabledProperty =
        UiProperty<bool>.Register(
            nameof(Enabled),
            typeof(Collider2D),
            new UiPropertyMetadata<bool>(true, UiPropertyOptions.AffectsHitTest));

    public static readonly UiProperty<bool> IsTriggerProperty =
        UiProperty<bool>.Register(
            nameof(IsTrigger),
            typeof(Collider2D),
            new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsHitTest));

    public static readonly UiProperty<bool> IsSimulatedProperty =
        UiProperty<bool>.Register(
            nameof(IsSimulated),
            typeof(Collider2D),
            new UiPropertyMetadata<bool>(false, UiPropertyOptions.None));

    public static readonly UiProperty<float> OffsetXProperty =
        UiProperty<float>.Register(
            nameof(OffsetX),
            typeof(Collider2D),
            new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsHitTest));

    public static readonly UiProperty<float> OffsetYProperty =
        UiProperty<float>.Register(
            nameof(OffsetY),
            typeof(Collider2D),
            new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsHitTest));

    public static readonly UiProperty<uint> CollisionLayerProperty =
        UiProperty<uint>.Register(
            nameof(CollisionLayer),
            typeof(Collider2D),
            new UiPropertyMetadata<uint>(1, UiPropertyOptions.AffectsHitTest));

    public static readonly UiProperty<uint> CollisionMaskProperty =
        UiProperty<uint>.Register(
            nameof(CollisionMask),
            typeof(Collider2D),
            new UiPropertyMetadata<uint>(uint.MaxValue, UiPropertyOptions.AffectsHitTest));

    public bool Enabled
    {
        get => GetValue(EnabledProperty);
        set => SetValue(EnabledProperty, value);
    }

    public bool IsTrigger
    {
        get => GetValue(IsTriggerProperty);
        set => SetValue(IsTriggerProperty, value);
    }

    /// <summary>Retains nearby collision data around this collider's active geometry.</summary>
    public bool IsSimulated
    {
        get => GetValue(IsSimulatedProperty);
        set => SetValue(IsSimulatedProperty, value);
    }

    // Interest collection scans every collider whenever any collider moves.
    internal bool IsSimulatedForInterest
    {
        get
        {
            long version = PropertyValueVersion;
            if (simulatedVersion != version)
            {
                isSimulated = IsSimulated;
                simulatedVersion = version;
            }
            return isSimulated;
        }
    }

    public float OffsetX
    {
        get => GetValue(OffsetXProperty);
        set => SetValue(OffsetXProperty, value);
    }

    public float OffsetY
    {
        get => GetValue(OffsetYProperty);
        set => SetValue(OffsetYProperty, value);
    }

    public uint CollisionLayer
    {
        get => GetValue(CollisionLayerProperty);
        set => SetValue(CollisionLayerProperty, value);
    }

    public uint CollisionMask
    {
        get => GetValue(CollisionMaskProperty);
        set => SetValue(CollisionMaskProperty, value);
    }

    /// <summary>
    /// Raised when another collider starts touching this one. The sender is this collider and
    /// <see cref="ColliderContactEventArgs.Other"/> is the collider that arrived.
    /// </summary>
    /// <remarks>
    /// Two colliders touch under the same rule as <see cref="CollisionWorld2D.Intersects"/>. Events are raised once
    /// per update of the scene's simulation context, never from inside the setter that moved a collider.
    /// A handler added while colliders already overlap receives <see cref="Entered"/> at the next update.
    /// </remarks>
    public event EventHandler<ColliderContactEventArgs>? Entered
    {
        add
        {
            VerifyOwnerAccess();
            bool hadHandlers = HasContactHandlers;
            entered += value;
            OnContactHandlersChanged(hadHandlers);
        }
        remove
        {
            VerifyOwnerAccess();
            bool hadHandlers = HasContactHandlers;
            entered -= value;
            OnContactHandlersChanged(hadHandlers);
        }
    }

    /// <summary>
    /// Raised when another collider stops touching this one, including when it leaves the scene,
    /// is disabled or hidden, or stops matching this collider's layer and mask.
    /// The sender is this collider and <see cref="ColliderContactEventArgs.Other"/> is the collider that left.
    /// </summary>
    /// <remarks>
    /// Removing the last handler of both events discards this collider's contacts without raising <see cref="Exited"/>.
    /// </remarks>
    public event EventHandler<ColliderContactEventArgs>? Exited
    {
        add
        {
            VerifyOwnerAccess();
            bool hadHandlers = HasContactHandlers;
            exited += value;
            OnContactHandlersChanged(hadHandlers);
        }
        remove
        {
            VerifyOwnerAccess();
            bool hadHandlers = HasContactHandlers;
            exited -= value;
            OnContactHandlersChanged(hadHandlers);
        }
    }

    internal bool HasContactHandlers => entered is not null || exited is not null;

    // Whether any contact bookkeeping must survive: handlers, or links to other colliders.
    internal bool HasContactState =>
        HasContactHandlers || reportedContacts is { Count: > 0 } || reportedBy is { Count: > 0 };

    // Ordinal of this collider's newest entry in any collision world. It keeps event order
    // stable for a collider that has just left its world.
    internal long ContactOrdinal { get; set; }

    internal bool ReportsContactWith(Collider2D other) => reportedContacts?.Contains(other) == true;

    internal void LinkReportedContact(Collider2D other)
    {
        (reportedContacts ??= new HashSet<Collider2D>(ReferenceEqualityComparer.Instance)).Add(other);
        (other.reportedBy ??= new HashSet<Collider2D>(ReferenceEqualityComparer.Instance)).Add(this);
    }

    internal void UnlinkReportedContact(Collider2D other)
    {
        reportedContacts?.Remove(other);
        other.reportedBy?.Remove(this);
    }

    // Appends the colliders this one has reported and the colliders that have reported it.
    internal void CopyContactPartners(List<Collider2D> destination)
    {
        if (reportedContacts is not null) { destination.AddRange(reportedContacts); }
        if (reportedBy is not null) { destination.AddRange(reportedBy); }
    }

    internal void RaiseEntered(Collider2D other) =>
        entered?.Invoke(this, new ColliderContactEventArgs(other));

    internal void RaiseExited(Collider2D other) =>
        exited?.Invoke(this, new ColliderContactEventArgs(other));

    private void OnContactHandlersChanged(bool hadHandlers)
    {
        bool hasHandlers = HasContactHandlers;
        if (hasHandlers == hadHandlers) { return; }

        if (!hasHandlers && reportedContacts is { Count: > 0 })
        {
            // Nobody listens any more: forget the contacts without raising Exited.
            foreach (Collider2D other in reportedContacts) { other.reportedBy?.Remove(this); }
            reportedContacts.Clear();
        }

        SceneGeometry2D.FindRootScene(this)?.CollisionWorld.OnContactTrackingChanged(this, hasHandlers);
    }

    internal override void OnSimulationContextChanged(SceneSimulationContext2D? previous)
    {
        base.OnSimulationContextChanged(previous);
        if (SimulationContext is not null && HasContactState)
        {
            // The new root scene's world did not see this collider's earlier contacts.
            SceneGeometry2D.FindRootScene(this)?.CollisionWorld.MarkContactDirty(this);
        }
    }

    internal override void Record(Scene2DRecordContext context)
    {
    }

    internal override Matrix3x2 GetLocalTransform() =>
        SceneGeometry2D.CreateLocalTransform(this, default);

    internal override SceneBounds2D GetVisibleLocalBounds() => SceneBounds2D.Empty;

    internal bool TryGetSceneGeometry(out ColliderGeometry2D geometry) =>
        SceneGeometry2D.TryCreateColliderGeometry(this, out geometry);

    internal bool TryGetActiveSceneGeometry(out ColliderGeometry2D geometry)
    {
        if (!Enabled ||
            CollisionLayer == 0 ||
            !UIElementVisibility.IsEffectivelyVisible(this))
        {
            geometry = default;
            return false;
        }

        return TryGetSceneGeometry(out geometry);
    }

    internal abstract ColliderLocalShape2D GetLocalShape();

    internal override void ValidatePropertyMutation(UiProperty property, object? value)
    {
        base.ValidatePropertyMutation(property, value);
        if ((ReferenceEquals(property, OffsetXProperty) ||
             ReferenceEquals(property, OffsetYProperty)) &&
            value is float coordinate &&
            !float.IsFinite(coordinate))
        {
            throw new ArgumentOutOfRangeException(nameof(value), coordinate, "Collider offsets must be finite.");
        }
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        OnColliderPropertyChanged(args);
        base.OnPropertyChanged(args);
        if (!IsCollisionProperty(args.Property))
        {
            return;
        }

        SceneCollisionMutationKind kind = GetMutationKind(args.Property);
        SceneGeometry2D.FindRootScene(this)?.NotifyCollisionMutation(this, kind);
    }

    protected virtual void OnColliderPropertyChanged(UiPropertyChangedEventArgs args)
    {
    }

    protected virtual bool IsColliderShapeProperty(UiProperty property) => false;

    private bool IsCollisionProperty(UiProperty property) =>
        ReferenceEquals(property, EnabledProperty) ||
        ReferenceEquals(property, IsTriggerProperty) ||
        ReferenceEquals(property, IsSimulatedProperty) ||
        ReferenceEquals(property, OffsetXProperty) ||
        ReferenceEquals(property, OffsetYProperty) ||
        ReferenceEquals(property, CollisionLayerProperty) ||
        ReferenceEquals(property, CollisionMaskProperty) ||
        ReferenceEquals(property, UIElement.IsVisibleProperty) ||
        ReferenceEquals(property, UIElement.VisibilityProperty) ||
        SceneGeometry2D.IsSceneTransformProperty(property) ||
        IsColliderShapeProperty(property);

    private SceneCollisionMutationKind GetMutationKind(UiProperty property)
    {
        if (ReferenceEquals(property, EnabledProperty) ||
            ReferenceEquals(property, IsSimulatedProperty) ||
            ReferenceEquals(property, UIElement.IsVisibleProperty) ||
            ReferenceEquals(property, UIElement.VisibilityProperty))
        {
            return SceneCollisionMutationKind.Participation;
        }

        if (ReferenceEquals(property, IsTriggerProperty) ||
            ReferenceEquals(property, CollisionLayerProperty) ||
            ReferenceEquals(property, CollisionMaskProperty))
        {
            return SceneCollisionMutationKind.Filter;
        }

        return SceneCollisionMutationKind.Geometry;
    }
}
