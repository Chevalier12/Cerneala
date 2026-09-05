using Cerneala.UI.Core;

namespace Cerneala.UI.Controls;

public sealed class CircleCollider2D : Collider2D
{
    public static readonly UiProperty<float> RadiusProperty =
        UiProperty<float>.Register(
            nameof(Radius),
            typeof(CircleCollider2D),
            new UiPropertyMetadata<float>(1, UiPropertyOptions.AffectsHitTest));

    public float Radius
    {
        get => GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }

    internal override ColliderLocalShape2D GetLocalShape() =>
        ColliderLocalShape2D.Circle(Radius);

    internal override void ValidatePropertyMutation(UiProperty property, object? value)
    {
        base.ValidatePropertyMutation(property, value);
        if (ReferenceEquals(property, RadiusProperty) &&
            value is float radius &&
            (!float.IsFinite(radius) || radius <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(value), radius, "Circle collider radius must be finite and greater than zero.");
        }
    }

    protected override bool IsColliderShapeProperty(UiProperty property) =>
        ReferenceEquals(property, RadiusProperty);
}
