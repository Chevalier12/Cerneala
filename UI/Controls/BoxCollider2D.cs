using Cerneala.UI.Core;

namespace Cerneala.UI.Controls;

public sealed class BoxCollider2D : Collider2D
{
    public new static readonly UiProperty<float> WidthProperty =
        UiProperty<float>.Register(
            nameof(Width),
            typeof(BoxCollider2D),
            new UiPropertyMetadata<float>(1, UiPropertyOptions.AffectsHitTest));

    public new static readonly UiProperty<float> HeightProperty =
        UiProperty<float>.Register(
            nameof(Height),
            typeof(BoxCollider2D),
            new UiPropertyMetadata<float>(1, UiPropertyOptions.AffectsHitTest));

    public new float Width
    {
        get => GetValue(WidthProperty);
        set => SetValue(WidthProperty, value);
    }

    public new float Height
    {
        get => GetValue(HeightProperty);
        set => SetValue(HeightProperty, value);
    }

    internal override ColliderLocalShape2D GetLocalShape() =>
        ColliderLocalShape2D.Box(Width, Height);

    internal override void ValidatePropertyMutation(UiProperty property, object? value)
    {
        base.ValidatePropertyMutation(property, value);
        if ((ReferenceEquals(property, WidthProperty) ||
             ReferenceEquals(property, HeightProperty)) &&
            value is float dimension &&
            (!float.IsFinite(dimension) || dimension <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(value), dimension, "Box collider dimensions must be finite and greater than zero.");
        }
    }

    protected override bool IsColliderShapeProperty(UiProperty property) =>
        ReferenceEquals(property, WidthProperty) ||
        ReferenceEquals(property, HeightProperty);
}
