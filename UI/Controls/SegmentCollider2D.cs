using System.Numerics;
using Cerneala.UI.Core;

namespace Cerneala.UI.Controls;

public sealed class SegmentCollider2D : Collider2D
{
    private IReadOnlyList<Vector2> vertices = Array.AsReadOnly(new[] { Vector2.Zero, Vector2.UnitX });

    public static readonly UiProperty<float> EndXProperty = UiProperty<float>.Register(
        nameof(EndX), typeof(SegmentCollider2D), new UiPropertyMetadata<float>(1, UiPropertyOptions.AffectsHitTest));
    public static readonly UiProperty<float> EndYProperty = UiProperty<float>.Register(
        nameof(EndY), typeof(SegmentCollider2D), new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsHitTest));

    public float EndX { get => GetValue(EndXProperty); set => SetValue(EndXProperty, value); }
    public float EndY { get => GetValue(EndYProperty); set => SetValue(EndYProperty, value); }

    internal override ColliderLocalShape2D GetLocalShape() => ColliderLocalShape2D.Segment(vertices);

    internal static void ValidateEndpoints(Vector2 first, Vector2 second)
    {
        float squared = Vector2.DistanceSquared(first, second);
        if (!float.IsFinite(first.X) || !float.IsFinite(first.Y) || !float.IsFinite(second.X) || !float.IsFinite(second.Y) ||
            !float.IsFinite(squared) || squared <= CollisionNarrowPhase2D.Epsilon * CollisionNarrowPhase2D.Epsilon)
        {
            throw Scene2DModelValidator.Diagnostic(new ArgumentException("Segment endpoints must be finite, distinct and within numeric range."), "SCN2D008");
        }
    }

    internal override void ValidatePropertyMutation(UiProperty property, object? value)
    {
        base.ValidatePropertyMutation(property, value);
        if (ReferenceEquals(property, EndXProperty) || ReferenceEquals(property, EndYProperty))
        {
            ValidateEndpoints(Vector2.Zero, new Vector2(
                ReferenceEquals(property, EndXProperty) ? (float)value! : EndX,
                ReferenceEquals(property, EndYProperty) ? (float)value! : EndY));
        }
    }

    protected override bool IsColliderShapeProperty(UiProperty property) =>
        ReferenceEquals(property, EndXProperty) || ReferenceEquals(property, EndYProperty);

    protected override void OnColliderPropertyChanged(UiPropertyChangedEventArgs args)
    {
        if (IsColliderShapeProperty(args.Property))
        {
            vertices = Array.AsReadOnly(new[] { Vector2.Zero, new Vector2(EndX, EndY) });
        }
    }
}
