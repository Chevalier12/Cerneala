using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.UI.Controls;

internal static class SceneHitTest2D
{
    internal static UIElement? HitTest(
        Scene2D scene,
        Vector2 scenePoint,
        HitTestFilter filter,
        HashSet<Collider2D> colliderHits)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(colliderHits);
        scene.CollisionWorld.CollectPointHits(scenePoint, colliderHits);
        return HitTestChildren(
            scene,
            scene.GetLocalTransform(),
            scenePoint,
            filter,
            colliderHits);
    }

    private static UIElement? HitTestChildren(
        SceneNode2D owner,
        Matrix3x2 ownerToScene,
        Vector2 scenePoint,
        HitTestFilter filter,
        IReadOnlySet<Collider2D> colliderHits)
    {
        IReadOnlyList<SceneNode2D> children = GetChildrenInDrawOrder(
            owner,
            ownerToScene);
        for (int index = children.Count - 1; index >= 0; index--)
        {
            SceneNode2D child = children[index];
            if (child is Collider2D)
            {
                continue;
            }

            UIElement? hit = HitTestNode(
                child,
                ownerToScene,
                scenePoint,
                filter,
                colliderHits);
            if (hit is not null)
            {
                return hit;
            }
        }

        for (int index = children.Count - 1; index >= 0; index--)
        {
            if (children[index] is Collider2D collider &&
                HitTestCollider(collider, filter, colliderHits))
            {
                return collider;
            }
        }

        return null;
    }

    private static UIElement? HitTestNode(
        SceneNode2D node,
        Matrix3x2 parentToScene,
        Vector2 scenePoint,
        HitTestFilter filter,
        IReadOnlySet<Collider2D> colliderHits)
    {
        HitTestFilterBehavior behavior = filter.Evaluate(node);
        if (behavior == HitTestFilterBehavior.ExcludeSubtree ||
            node.IsPresenceExiting ||
            !UIElementVisibility.ParticipatesInHitTest(node) ||
            !node.IsEnabled)
        {
            return null;
        }

        Matrix3x2 nodeToScene = node.GetLocalTransform() * parentToScene;
        IReadOnlyList<SceneNode2D> children = GetChildrenInDrawOrder(
            node,
            nodeToScene);

        for (int index = children.Count - 1; index >= 0; index--)
        {
            if (children[index] is not Collider2D collider ||
                !HitTestCollider(collider, filter, colliderHits))
            {
                continue;
            }

            return behavior == HitTestFilterBehavior.Exclude ? collider : node;
        }

        for (int index = children.Count - 1; index >= 0; index--)
        {
            SceneNode2D child = children[index];
            if (child is Collider2D)
            {
                continue;
            }

            UIElement? childHit = HitTestNode(
                child,
                nodeToScene,
                scenePoint,
                filter,
                colliderHits);
            if (childHit is not null)
            {
                return childHit;
            }
        }

        if (behavior == HitTestFilterBehavior.Exclude)
        {
            return null;
        }

        SceneBounds2D bounds = node.GetHitTestLocalBounds();
        if (bounds.Kind != SceneBoundsKind.Known ||
            !SceneGeometry2D.TryTransformToLocal(
                new DrawPoint(scenePoint.X, scenePoint.Y),
                nodeToScene,
                out DrawPoint localPoint))
        {
            return null;
        }

        return Contains(bounds.Bounds, localPoint.X, localPoint.Y)
            ? node
            : null;
    }

    private static bool HitTestCollider(
        Collider2D collider,
        HitTestFilter filter,
        IReadOnlySet<Collider2D> colliderHits)
    {
        return collider.ParticipatesInInputRoute &&
            !collider.IsPresenceExiting &&
            UIElementVisibility.ParticipatesInHitTest(collider) &&
            collider.IsEnabled &&
            collider.Enabled &&
            collider.CollisionLayer != 0 &&
            filter.Evaluate(collider) == HitTestFilterBehavior.Include &&
            colliderHits.Contains(collider);
    }

    private static IReadOnlyList<SceneNode2D> GetChildrenInDrawOrder(
        SceneNode2D owner,
        Matrix3x2 ownerToScene)
    {
        List<SceneNode2D> children = owner.LogicalChildren
            .OfType<SceneNode2D>()
            .Where(static child => child.ParticipatesInInputRoute)
            .ToList();
        if (owner is not Scene2D scene || scene.OrderMode == SceneOrderMode.Source)
        {
            return children;
        }

        Dictionary<SceneNode2D, int> sourceIndices = new(
            ReferenceEqualityComparer.Instance);
        Dictionary<SceneNode2D, float> yAnchors = new(
            ReferenceEqualityComparer.Instance);
        for (int index = 0; index < children.Count; index++)
        {
            SceneNode2D child = children[index];
            sourceIndices.Add(child, index);
            SceneBounds2D transformed = SceneGeometry2D.TransformBounds(
                child.GetLocalBounds(),
                child.GetLocalTransform() * ownerToScene);
            yAnchors.Add(
                child,
                transformed.Kind == SceneBoundsKind.Known
                    ? transformed.Bounds.Bottom
                    : 0);
        }

        children.Sort((left, right) =>
        {
            int layer = left.Layer.CompareTo(right.Layer);
            if (layer != 0)
            {
                return layer;
            }

            if (scene.OrderMode == SceneOrderMode.LayerThenY)
            {
                int y = yAnchors[left].CompareTo(yAnchors[right]);
                if (y != 0)
                {
                    return y;
                }
            }

            return sourceIndices[left].CompareTo(sourceIndices[right]);
        });
        return children;
    }

    private static bool Contains(DrawRect bounds, float x, float y) =>
        x >= bounds.X &&
        y >= bounds.Y &&
        x <= bounds.Right &&
        y <= bounds.Bottom;
}
