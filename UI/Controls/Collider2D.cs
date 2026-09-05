using System.Numerics;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Controls;

public abstract class Collider2D : SceneNode2D
{
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
