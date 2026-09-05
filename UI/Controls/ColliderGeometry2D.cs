using System.Numerics;
using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

internal enum ColliderShapeKind2D
{
    Box,
    Circle,
    Polygon,
    Segment
}

internal readonly record struct ColliderLocalShape2D
{
    private ColliderLocalShape2D(
        ColliderShapeKind2D kind,
        float width,
        float height,
        float radius,
        IReadOnlyList<Vector2>? vertices)
    {
        Kind = kind;
        Width = width;
        Height = height;
        Radius = radius;
        Vertices = vertices ?? Array.Empty<Vector2>();
    }

    internal ColliderShapeKind2D Kind { get; }

    internal float Width { get; }

    internal float Height { get; }

    internal float Radius { get; }

    internal IReadOnlyList<Vector2> Vertices { get; }

    internal static ColliderLocalShape2D Box(float width, float height) =>
        new(ColliderShapeKind2D.Box, width, height, 0, null);

    internal static ColliderLocalShape2D Circle(float radius) =>
        new(ColliderShapeKind2D.Circle, 0, 0, radius, null);

    internal static ColliderLocalShape2D Polygon(IReadOnlyList<Vector2> vertices) =>
        new(ColliderShapeKind2D.Polygon, 0, 0, 0, vertices);

    internal static ColliderLocalShape2D Segment(IReadOnlyList<Vector2> vertices) =>
        new(ColliderShapeKind2D.Segment, 0, 0, 0, vertices);
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
