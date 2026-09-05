using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls;

internal enum SceneBoundsKind
{
    Empty,
    Known,
    Unknown
}

internal readonly record struct SceneBounds2D
{
    private SceneBounds2D(SceneBoundsKind kind, DrawRect bounds)
    {
        Kind = kind;
        Bounds = bounds;
    }

    internal static SceneBounds2D Empty { get; } =
        new(SceneBoundsKind.Empty, default);

    internal static SceneBounds2D Unknown { get; } =
        new(SceneBoundsKind.Unknown, default);

    internal SceneBoundsKind Kind { get; }

    internal DrawRect Bounds { get; }

    internal static SceneBounds2D Known(DrawRect bounds) =>
        new(SceneBoundsKind.Known, bounds);
}

internal static class SceneGeometry2D
{
    // Project the same geometry and child ownership used by scene input, without
    // arranging logical nodes or touching the collision index. Transform each
    // shape before union so nested transforms do not repeatedly inflate AABBs.
    internal static SceneBounds2D GetInputBounds(SceneNode2D node, Matrix3x2 nodeToRoot)
    {
        SceneBounds2D result;
        if (node is Collider2D collider)
        {
            Matrix3x2 shapeToRoot = Matrix3x2.CreateTranslation(collider.OffsetX, collider.OffsetY) * nodeToRoot;
            result = TryGetColliderBounds(collider.GetLocalShape(), shapeToRoot, out DrawRect bounds)
                ? SceneBounds2D.Known(bounds)
                : SceneBounds2D.Unknown;
        }
        else
        {
            result = TransformBounds(node.GetHitTestLocalBounds(), nodeToRoot);
        }

        foreach (UIElement child in ((Cerneala.UI.Input.IInputSubtreeHost)node).GetInputSubtreeChildren())
        {
            if (child is SceneNode2D sceneChild)
            {
                result = Union(result, GetInputBounds(sceneChild, sceneChild.GetLocalTransform() * nodeToRoot));
            }
        }

        return result;
    }

    internal static bool IsSceneTransformProperty(UiProperty property) =>
        ReferenceEquals(property, UIElement.RenderTransformProperty) ||
        ReferenceEquals(property, UIElement.TranslateXProperty) ||
        ReferenceEquals(property, UIElement.TranslateYProperty) ||
        ReferenceEquals(property, UIElement.ScaleProperty) ||
        ReferenceEquals(property, UIElement.ScaleXProperty) ||
        ReferenceEquals(property, UIElement.ScaleYProperty) ||
        ReferenceEquals(property, UIElement.RotationProperty) ||
        ReferenceEquals(property, UIElement.SkewXProperty) ||
        ReferenceEquals(property, UIElement.SkewYProperty);

    internal static Matrix3x2 CreateLocalTransform(Scene2D scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        return CreateLocalTransform(scene, scene.TransformOrigin);
    }

    internal static Matrix3x2 CreateLocalTransform(
        UIElement element,
        DrawPoint origin)
    {
        ArgumentNullException.ThrowIfNull(element);
        Matrix3x2 transform = Matrix3x2.CreateTranslation(-origin.X, -origin.Y);
        transform *= Matrix3x2.CreateScale(
            element.Scale * element.ScaleX,
            element.Scale * element.ScaleY);
        transform *= Matrix3x2.CreateSkew(element.SkewX, element.SkewY);
        transform *= Matrix3x2.CreateRotation(element.Rotation);
        transform *= Matrix3x2.CreateTranslation(element.TranslateX, element.TranslateY);
        transform *= element.RenderTransform.Matrix.ToNumerics();
        transform *= Matrix3x2.CreateTranslation(origin.X, origin.Y);
        return transform;
    }

    internal static SceneBounds2D TransformBounds(
        SceneBounds2D bounds,
        Matrix3x2 transform)
    {
        return bounds.Kind switch
        {
            SceneBoundsKind.Empty => SceneBounds2D.Empty,
            SceneBoundsKind.Unknown => SceneBounds2D.Unknown,
            _ => TryTransformBounds(bounds.Bounds, transform, out DrawRect transformed)
                ? SceneBounds2D.Known(transformed)
                : SceneBounds2D.Unknown
        };
    }

    internal static SceneBounds2D Union(
        SceneBounds2D first,
        SceneBounds2D second)
    {
        if (first.Kind == SceneBoundsKind.Unknown ||
            second.Kind == SceneBoundsKind.Unknown)
        {
            return SceneBounds2D.Unknown;
        }

        if (first.Kind == SceneBoundsKind.Empty)
        {
            return second;
        }

        if (second.Kind == SceneBoundsKind.Empty)
        {
            return first;
        }

        DrawRect a = first.Bounds;
        DrawRect b = second.Bounds;
        float left = MathF.Min(a.X, b.X);
        float top = MathF.Min(a.Y, b.Y);
        float right = MathF.Max(a.Right, b.Right);
        float bottom = MathF.Max(a.Bottom, b.Bottom);
        return TryCreateBounds(left, top, right, bottom, out DrawRect union)
            ? SceneBounds2D.Known(union)
            : SceneBounds2D.Unknown;
    }

    internal static bool TryTransformBounds(
        DrawRect bounds,
        Matrix3x2 transform,
        out DrawRect transformed)
    {
        Vector2 topLeft = Vector2.Transform(new Vector2(bounds.X, bounds.Y), transform);
        Vector2 topRight = Vector2.Transform(new Vector2(bounds.Right, bounds.Y), transform);
        Vector2 bottomLeft = Vector2.Transform(new Vector2(bounds.X, bounds.Bottom), transform);
        Vector2 bottomRight = Vector2.Transform(new Vector2(bounds.Right, bounds.Bottom), transform);
        float left = MathF.Min(
            MathF.Min(topLeft.X, topRight.X),
            MathF.Min(bottomLeft.X, bottomRight.X));
        float top = MathF.Min(
            MathF.Min(topLeft.Y, topRight.Y),
            MathF.Min(bottomLeft.Y, bottomRight.Y));
        float right = MathF.Max(
            MathF.Max(topLeft.X, topRight.X),
            MathF.Max(bottomLeft.X, bottomRight.X));
        float bottom = MathF.Max(
            MathF.Max(topLeft.Y, topRight.Y),
            MathF.Max(bottomLeft.Y, bottomRight.Y));
        return TryCreateBounds(left, top, right, bottom, out transformed);
    }

    internal static bool TryTransformToLocal(
        DrawPoint worldPoint,
        Matrix3x2 localToWorld,
        out DrawPoint localPoint)
    {
        if (!Matrix3x2.Invert(localToWorld, out Matrix3x2 worldToLocal))
        {
            localPoint = default;
            return false;
        }

        Vector2 transformed = Vector2.Transform(
            new Vector2(worldPoint.X, worldPoint.Y),
            worldToLocal);
        if (!float.IsFinite(transformed.X) || !float.IsFinite(transformed.Y))
        {
            localPoint = default;
            return false;
        }

        localPoint = new DrawPoint(transformed.X, transformed.Y);
        return true;
    }

    internal static bool TryTransformBoundsToLocal(
        DrawRect worldBounds,
        Matrix3x2 localToWorld,
        out DrawRect localBounds)
    {
        if (!Matrix3x2.Invert(localToWorld, out Matrix3x2 worldToLocal))
        {
            localBounds = default;
            return false;
        }

        return TryTransformBounds(worldBounds, worldToLocal, out localBounds);
    }

    internal static Scene2D? FindRootScene(SceneNode2D node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Scene2D? root = node as Scene2D;
        for (UIElement? current = node.LogicalParent;
            current is not null;
            current = current.LogicalParent)
        {
            if (current is Scene2D scene)
            {
                root = scene;
            }
        }

        return root;
    }

    internal static Matrix3x2 GetLocalToSceneTransform(SceneNode2D node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Matrix3x2 result = Matrix3x2.Identity;
        for (UIElement? current = node;
            current is SceneNode2D sceneNode;
            current = current.LogicalParent)
        {
            result *= sceneNode.GetLocalTransform();
        }

        return result;
    }

    internal static bool TryCreateColliderGeometry(
        Collider2D collider,
        out ColliderGeometry2D geometry)
    {
        ArgumentNullException.ThrowIfNull(collider);
        ColliderLocalShape2D shape = collider.GetLocalShape();
        Matrix3x2 shapeToScene =
            Matrix3x2.CreateTranslation(collider.OffsetX, collider.OffsetY) *
            GetLocalToSceneTransform(collider);
        if (!TryGetColliderBounds(shape, shapeToScene, out DrawRect bounds))
        {
            geometry = default;
            return false;
        }

        geometry = new ColliderGeometry2D(collider, shape, shapeToScene, bounds);
        return true;
    }

    internal static bool ContainsPoint(
        ColliderGeometry2D geometry,
        Vector2 scenePoint)
    {
        if (!TryTransformToLocal(
            new DrawPoint(scenePoint.X, scenePoint.Y),
            geometry.ShapeToSceneTransform,
            out DrawPoint localPoint))
        {
            return false;
        }

        ColliderLocalShape2D shape = geometry.LocalShape;
        float epsilon = CollisionNarrowPhase2D.Epsilon;
        return shape.Kind switch
        {
            ColliderShapeKind2D.Box =>
                localPoint.X >= -epsilon &&
                localPoint.Y >= -epsilon &&
                localPoint.X <= shape.Width + epsilon &&
                localPoint.Y <= shape.Height + epsilon,
            ColliderShapeKind2D.Circle =>
                ((localPoint.X * localPoint.X) + (localPoint.Y * localPoint.Y)) <=
                ((shape.Radius + epsilon) * (shape.Radius + epsilon)),
            ColliderShapeKind2D.Polygon => ContainsConvexPoint(
                shape.Vertices,
                new Vector2(localPoint.X, localPoint.Y),
                epsilon),
            ColliderShapeKind2D.Segment => ContainsSegmentPoint(shape.Vertices,
                new Vector2(localPoint.X, localPoint.Y), epsilon),
            _ => false
        };
    }

    private static bool ContainsConvexPoint(
        IReadOnlyList<Vector2> vertices,
        Vector2 point,
        float epsilon)
    {
        float sign = 0;
        for (int index = 0; index < vertices.Count; index++)
        {
            Vector2 start = vertices[index];
            Vector2 end = vertices[(index + 1) % vertices.Count];
            Vector2 edge = end - start;
            Vector2 relative = point - start;
            float cross = (edge.X * relative.Y) - (edge.Y * relative.X);
            if (MathF.Abs(cross) <= epsilon)
            {
                continue;
            }

            float currentSign = MathF.Sign(cross);
            if (sign != 0 && currentSign != sign)
            {
                return false;
            }

            sign = currentSign;
        }

        return true;
    }

    private static bool ContainsSegmentPoint(IReadOnlyList<Vector2> vertices, Vector2 point, float epsilon)
    {
        Vector2 edge = vertices[1] - vertices[0];
        float fraction = Math.Clamp(Vector2.Dot(point - vertices[0], edge) / edge.LengthSquared(), 0, 1);
        return Vector2.DistanceSquared(point, vertices[0] + edge * fraction) <= epsilon * epsilon;
    }

    internal static bool TryGetColliderBounds(
        ColliderLocalShape2D shape,
        Matrix3x2 shapeToScene,
        out DrawRect bounds)
    {
        switch (shape.Kind)
        {
            case ColliderShapeKind2D.Box:
                return TryTransformBounds(
                    new DrawRect(0, 0, shape.Width, shape.Height),
                    shapeToScene,
                    out bounds);
            case ColliderShapeKind2D.Circle:
                Vector2 center = Vector2.Transform(Vector2.Zero, shapeToScene);
                float extentX = shape.Radius * MathF.Sqrt(
                    (shapeToScene.M11 * shapeToScene.M11) +
                    (shapeToScene.M21 * shapeToScene.M21));
                float extentY = shape.Radius * MathF.Sqrt(
                    (shapeToScene.M12 * shapeToScene.M12) +
                    (shapeToScene.M22 * shapeToScene.M22));
                return TryCreateBounds(
                    center.X - extentX,
                    center.Y - extentY,
                    center.X + extentX,
                    center.Y + extentY,
                    out bounds);
            case ColliderShapeKind2D.Polygon:
            case ColliderShapeKind2D.Segment:
                return TryGetPolygonBounds(shape.Vertices, shapeToScene, out bounds);
            default:
                bounds = default;
                return false;
        }
    }

    private static bool TryGetPolygonBounds(
        IReadOnlyList<Vector2> vertices,
        Matrix3x2 transform,
        out DrawRect bounds)
    {
        if (vertices.Count == 0)
        {
            bounds = default;
            return false;
        }

        Vector2 first = Vector2.Transform(vertices[0], transform);
        float left = first.X;
        float top = first.Y;
        float right = first.X;
        float bottom = first.Y;
        for (int index = 1; index < vertices.Count; index++)
        {
            Vector2 point = Vector2.Transform(vertices[index], transform);
            left = MathF.Min(left, point.X);
            top = MathF.Min(top, point.Y);
            right = MathF.Max(right, point.X);
            bottom = MathF.Max(bottom, point.Y);
        }

        return TryCreateBounds(left, top, right, bottom, out bounds);
    }

    private static bool TryCreateBounds(
        float left,
        float top,
        float right,
        float bottom,
        out DrawRect bounds)
    {
        float width = right - left;
        float height = bottom - top;
        if (!float.IsFinite(left) ||
            !float.IsFinite(top) ||
            !float.IsFinite(width) ||
            !float.IsFinite(height) ||
            width < 0 ||
            height < 0)
        {
            bounds = default;
            return false;
        }

        bounds = new DrawRect(left, top, width, height);
        return true;
    }
}
