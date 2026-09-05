using System.Collections.ObjectModel;
using System.Numerics;
using Cerneala.UI.Core;
using static Cerneala.UI.Controls.Scene2DModelValidator;

namespace Cerneala.UI.Controls;

public sealed class PolygonCollider2D : Collider2D
{
    private const float ValidationEpsilon = 1e-5f;
    private const string DefaultPoints = "0,0 1,0 0,1";
    private IReadOnlyList<Vector2> vertices = ParsePoints(DefaultPoints);

    public static readonly UiProperty<string> PointsProperty =
        UiProperty<string>.Register(
            nameof(Points),
            typeof(PolygonCollider2D),
            new UiPropertyMetadata<string>(DefaultPoints, UiPropertyOptions.AffectsHitTest));

    public string Points
    {
        get => GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public IReadOnlyList<Vector2> Vertices => vertices;

    internal override ColliderLocalShape2D GetLocalShape() =>
        ColliderLocalShape2D.Polygon(vertices);

    internal override void ValidatePropertyMutation(UiProperty property, object? value)
    {
        base.ValidatePropertyMutation(property, value);
        if (ReferenceEquals(property, PointsProperty))
        {
            _ = ParsePoints(value as string);
        }
    }

    protected override void OnColliderPropertyChanged(UiPropertyChangedEventArgs args)
    {
        if (ReferenceEquals(args.Property, PointsProperty))
        {
            vertices = ParsePoints((string?)args.NewValue);
        }
    }

    protected override bool IsColliderShapeProperty(UiProperty property) =>
        ReferenceEquals(property, PointsProperty);

    internal static IReadOnlyList<Vector2> ParsePoints(string? value)
    {
        Vector2[] parsed = ParseShapePoints(value, 3, MaximumShapePoints).ToArray();

        float orientation = 0;
        for (int index = 0; index < parsed.Length; index++)
        {
            Vector2 first = parsed[index];
            Vector2 second = parsed[(index + 1) % parsed.Length];
            Vector2 third = parsed[(index + 2) % parsed.Length];
            float cross = Cross(second - first, third - second);
            if (!float.IsFinite(cross) || MathF.Abs(cross) <= ValidationEpsilon)
            {
                throw Diagnostic(new ArgumentException(
                    "Polygon collider points must form a non-degenerate strictly convex polygon.",
                    nameof(value)), "SCN2D008");
            }

            float sign = MathF.Sign(cross);
            if (orientation == 0)
            {
                orientation = sign;
            }
            else if (sign != orientation)
            {
                throw Diagnostic(new ArgumentException("Polygon collider must be convex.", nameof(value)), "SCN2D008");
            }
        }

        // Consistent local turns also admit self-intersecting stars. Every vertex
        // must lie strictly on the interior side of every nonincident edge.
        for (int edge = 0; edge < parsed.Length; edge++)
        {
            int next = (edge + 1) % parsed.Length;
            for (int point = 0; point < parsed.Length; point++)
            {
                if (point == edge || point == next) { continue; }
                float side = Cross(parsed[next] - parsed[edge], parsed[point] - parsed[edge]);
                if (!float.IsFinite(side) || side * orientation <= ValidationEpsilon)
                {
                    throw Diagnostic(new ArgumentException("Polygon collider must be simple and strictly convex.", nameof(value)), "SCN2D008");
                }
            }
        }

        return new ReadOnlyCollection<Vector2>(parsed);
    }

    private static float Cross(Vector2 left, Vector2 right) =>
        (left.X * right.Y) - (left.Y * right.X);
}
