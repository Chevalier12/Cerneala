using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls;

/// <summary>
/// Defines a cone-shaped collision area: a circular sector whose apex is the collider origin,
/// moved by <see cref="Collider2D.OffsetX"/> and <see cref="Collider2D.OffsetY"/>.
/// </summary>
public sealed class ConeCollider2D : Collider2D
{
    private const double DegreesToRadians = Math.PI / 180;
    private const double TurnDegrees = 360;

    /// <summary>Identifies the <see cref="Radius"/> UI property.</summary>
    public static readonly UiProperty<float> RadiusProperty =
        UiProperty<float>.Register(
            nameof(Radius),
            typeof(ConeCollider2D),
            new UiPropertyMetadata<float>(100, UiPropertyOptions.AffectsHitTest));

    /// <summary>Identifies the <see cref="Angle"/> UI property.</summary>
    public static readonly UiProperty<float> AngleProperty =
        UiProperty<float>.Register(
            nameof(Angle),
            typeof(ConeCollider2D),
            new UiPropertyMetadata<float>(90, UiPropertyOptions.AffectsHitTest));

    /// <summary>Identifies the <see cref="Direction"/> UI property.</summary>
    public static readonly UiProperty<float> DirectionProperty =
        UiProperty<float>.Register(
            nameof(Direction),
            typeof(ConeCollider2D),
            new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsHitTest));

    /// <summary>Gets or sets how far the cone reaches from its apex, in scene units. Must be finite and greater than zero.</summary>
    public float Radius
    {
        get => GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }

    /// <summary>
    /// Gets or sets how wide the cone is, in degrees, in the range greater than 0 up to 360.
    /// An angle of 360 is a full circle.
    /// </summary>
    public float Angle
    {
        get => GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    /// <summary>
    /// Gets or sets where the cone looks, in degrees. 0 looks along +X (right) and the angle grows clockwise on screen,
    /// so 90 looks down. Any finite value is kept as set and used normalized.
    /// </summary>
    public float Direction
    {
        get => GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    // A cone is only ever a Vision. Its owners' Vision slots are the only owner-managed insertions that reach
    // here: the body slot refuses a cone, and Scene2D.Children refuses every collider.
    internal override bool AllowsCollisionParent(UIElement parent) => parent is Sprite2D or Scene2D;

    internal override ColliderLocalShape2D GetLocalShape() =>
        GetLocalShape(LogicalParent is Sprite2D sprite ? sprite.Flip : RenderSurface2DSpriteFlip.None);

    // The flip mirrors Direction only; OffsetX and OffsetY stay where they are.
    internal ColliderLocalShape2D GetLocalShape(RenderSurface2DSpriteFlip flip)
    {
        float radius = Radius;
        float angle = Angle;
        if (angle >= TurnDegrees)
        {
            return ColliderLocalShape2D.Circle(radius);
        }

        double direction = Direction;
        if ((flip & RenderSurface2DSpriteFlip.Horizontal) != 0)
        {
            direction = 180 - direction;
        }

        if ((flip & RenderSurface2DSpriteFlip.Vertical) != 0)
        {
            direction = -direction;
        }

        double start = (direction % TurnDegrees) - (angle * 0.5);
        double startRadians = (start * DegreesToRadians) % (2 * Math.PI);
        if (startRadians < 0)
        {
            startRadians += 2 * Math.PI;
        }

        if (startRadians >= 2 * Math.PI)
        {
            startRadians = 0;
        }

        return ColliderLocalShape2D.Cone(radius, (float)startRadians, (float)(angle * DegreesToRadians));
    }

    internal override void ValidatePropertyMutation(UiProperty property, object? value)
    {
        base.ValidatePropertyMutation(property, value);
        if (value is not float number)
        {
            return;
        }

        if (ReferenceEquals(property, RadiusProperty) && (!float.IsFinite(number) || number <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(value), number, "Cone collider radius must be finite and greater than zero.");
        }

        if (ReferenceEquals(property, AngleProperty) && (!float.IsFinite(number) || number <= 0 || number > TurnDegrees))
        {
            throw new ArgumentOutOfRangeException(nameof(value), number, "Cone collider angle must be greater than zero and at most 360 degrees.");
        }

        if (ReferenceEquals(property, DirectionProperty) && !float.IsFinite(number))
        {
            throw new ArgumentOutOfRangeException(nameof(value), number, "Cone collider direction must be finite.");
        }
    }

    protected override bool IsColliderShapeProperty(UiProperty property) =>
        ReferenceEquals(property, RadiusProperty) ||
        ReferenceEquals(property, AngleProperty) ||
        ReferenceEquals(property, DirectionProperty);
}
