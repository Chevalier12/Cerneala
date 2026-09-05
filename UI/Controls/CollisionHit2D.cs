using System.Numerics;

namespace Cerneala.UI.Controls;

public sealed class CollisionHit2D
{
    internal CollisionHit2D(
        Collider2D collider,
        SceneNode2D entity,
        Vector2 point,
        Vector2 normal,
        float distance,
        float fraction,
        bool isTrigger)
    {
        Collider = collider;
        Entity = entity;
        Point = point;
        Normal = normal;
        Distance = distance;
        Fraction = fraction;
        IsTrigger = isTrigger;
    }

    public Collider2D Collider { get; }

    public SceneNode2D Entity { get; }

    public Vector2 Point { get; }

    public Vector2 Normal { get; }

    public float Distance { get; }

    public float Fraction { get; }

    public bool IsTrigger { get; }
}
