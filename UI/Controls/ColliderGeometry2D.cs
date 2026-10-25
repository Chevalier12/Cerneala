using System.Numerics;
using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

internal enum ColliderShapeKind2D
{
    Box,
    Circle,
    Polygon,
    Segment,
    Cone
}

internal readonly record struct ColliderLocalShape2D
{
    private ColliderLocalShape2D(
        ColliderShapeKind2D kind,
        float width,
        float height,
        float radius,
        IReadOnlyList<Vector2>? vertices,
        float startAngle = 0,
        float sweep = 0)
    {
        Kind = kind;
        Width = width;
        Height = height;
        Radius = radius;
        Vertices = vertices ?? Array.Empty<Vector2>();
        StartAngle = startAngle;
        Sweep = sweep;
    }

    internal ColliderShapeKind2D Kind { get; }

    internal float Width { get; }

    internal float Height { get; }

    internal float Radius { get; }

    internal IReadOnlyList<Vector2> Vertices { get; }

    // Cone only, in radians: the first edge angle and the angular width of the arc.
    // Angles grow from +X toward +Y, which is clockwise on screen.
    internal float StartAngle { get; }

    internal float Sweep { get; }

    internal static ColliderLocalShape2D Box(float width, float height) =>
        new(ColliderShapeKind2D.Box, width, height, 0, null);

    internal static ColliderLocalShape2D Circle(float radius) =>
        new(ColliderShapeKind2D.Circle, 0, 0, radius, null);

    internal static ColliderLocalShape2D Polygon(IReadOnlyList<Vector2> vertices) =>
        new(ColliderShapeKind2D.Polygon, 0, 0, 0, vertices);

    internal static ColliderLocalShape2D Segment(IReadOnlyList<Vector2> vertices) =>
        new(ColliderShapeKind2D.Segment, 0, 0, 0, vertices);

    internal static ColliderLocalShape2D Cone(float radius, float startAngle, float sweep) =>
        new(ColliderShapeKind2D.Cone, 0, 0, radius, null, startAngle, sweep);
}

internal readonly record struct ColliderGeometry2D(
    Collider2D Collider,
    ColliderLocalShape2D LocalShape,
    Matrix3x2 ShapeToSceneTransform,
    DrawRect SceneBounds);

internal enum SceneCollisionMutationKind
{
    Structure,
    Geometry,
    Filter,
    Participation
}

internal readonly record struct SceneCollisionMutation2D(
    long Version,
    SceneNode2D Node,
    SceneCollisionMutationKind Kind);
