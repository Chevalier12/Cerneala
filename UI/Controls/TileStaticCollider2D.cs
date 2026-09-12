using System.Numerics;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls;

internal sealed class TileStaticCollider2D : Collider2D
{
    private readonly ColliderLocalShape2D shape;
    private readonly TileLayer2D collisionHost;
    private readonly Matrix3x2 localTransform;

    internal TileStaticCollider2D(
        TileColliderDescriptor2D descriptor,
        TileLayer2D collisionHost,
        Matrix3x2 placement,
        float? boxWidth = null,
        float? boxHeight = null)
    {
        this.collisionHost = collisionHost;
        localTransform = descriptor.LocalTransform * placement;
        shape = descriptor.CreateLocalShape(boxWidth, boxHeight);
        OffsetX = descriptor.OffsetX;
        OffsetY = descriptor.OffsetY;
        CollisionLayer = descriptor.CollisionLayer;
        CollisionMask = descriptor.CollisionMask;
        IsTrigger = descriptor.IsTrigger;
        IsHitTestVisible = false;
        DebugIdentity = descriptor.DebugIdentity;
    }

    internal string? DebugIdentity { get; }

    internal override bool ParticipatesInInputRoute => false;

    // Immutable tile data owns this adapter; its layer is only a transform host.
    internal override bool AllowsCollisionParent(UIElement parent) => ReferenceEquals(parent, collisionHost);

    internal override ColliderLocalShape2D GetLocalShape() => shape;

    internal override Matrix3x2 GetLocalTransform() => localTransform;
}
