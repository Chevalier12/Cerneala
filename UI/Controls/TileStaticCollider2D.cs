using System.Numerics;
using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

internal sealed class TileStaticCollider2D : Collider2D
{
    private readonly ColliderLocalShape2D shape;
    private readonly TileCoordinate2D coordinate;
    private readonly DrawSize tileSize;
    private readonly TileFlip2D flip;
    private readonly Matrix3x2 localTransform;

    internal TileStaticCollider2D(
        TileColliderDescriptor2D descriptor,
        TileCoordinate2D coordinate,
        DrawSize tileSize,
        TileFlip2D flip,
        float? boxWidth = null,
        float? boxHeight = null)
    {
        this.coordinate = coordinate;
        this.tileSize = tileSize;
        this.flip = flip;
        localTransform = descriptor.LocalTransform;
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

    internal override ColliderLocalShape2D GetLocalShape() => shape;

    internal override Matrix3x2 GetLocalTransform()
    {
        Matrix3x2 transform = localTransform * TileFlipGeometry2D.Transform(flip, tileSize);

        transform *= Matrix3x2.CreateTranslation(
            coordinate.X * tileSize.Width,
            coordinate.Y * tileSize.Height);
        return transform;
    }
}
