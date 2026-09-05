using System.Numerics;

namespace Cerneala.UI.Controls;

public sealed class MoveCollisionResult2D
{
    internal MoveCollisionResult2D(
        Vector2 requestedDisplacement,
        Vector2 travel,
        CollisionHit2D? collision,
        CollisionHit2D[] triggerHits)
    {
        RequestedDisplacement = requestedDisplacement;
        Travel = travel;
        Remainder = requestedDisplacement - travel;
        Collision = collision;
        TriggerHits = Array.AsReadOnly(triggerHits);
    }

    public Vector2 RequestedDisplacement { get; }

    public Vector2 Travel { get; }

    public Vector2 Remainder { get; }

    public CollisionHit2D? Collision { get; }

    public IReadOnlyList<CollisionHit2D> TriggerHits { get; }
}
