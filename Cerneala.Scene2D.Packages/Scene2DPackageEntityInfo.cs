using Cerneala.Drawing;
using Cerneala.UI.Controls;

namespace Cerneala.Scene2D.Packages;

/// <summary>Resident authoring geometry for game-owned spatial/template decisions, without entity payload data.</summary>
public sealed class Scene2DPackageEntityInfo
{
    internal Scene2DPackageEntityInfo(string id, string mapId, string role, DrawRect authoringBounds, DrawRect? collisionBounds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapId);
        if (role is not ("Metadata" or "Spawn" or "Collider" or "Promote"))
        { throw new ArgumentException("Unsupported entity role.", nameof(role)); }
        _ = new SceneSpatialEntry2D(id, authoringBounds, collisionBounds);
        Id = id;
        MapId = mapId;
        Role = role;
        AuthoringBounds = authoringBounds;
        CollisionBounds = collisionBounds;
    }

    public string Id { get; }
    public string MapId { get; }
    public string Role { get; }
    public DrawRect AuthoringBounds { get; }
    public DrawRect? CollisionBounds { get; }

    internal static Scene2DPackageEntityInfo FromEntity(Scene2DEntity entity) =>
        new(entity.Id, entity.MapId, entity.Role, entity.GetAuthoringBounds(), entity.GetCollisionBounds());

    internal bool Matches(Scene2DEntity entity) => Id == entity.Id && MapId == entity.MapId && Role == entity.Role &&
        AuthoringBounds == entity.GetAuthoringBounds() && CollisionBounds == entity.GetCollisionBounds();
}
