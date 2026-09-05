using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Resources;
using static Cerneala.UI.Controls.Scene2DModelValidator;

namespace Cerneala.UI.Controls;

public sealed class Scene2DAsset
{
    public Scene2DAsset(ResourceId<ImageResource> resourceId, string path, DrawSize size)
    {
        if (string.IsNullOrWhiteSpace(resourceId.Key) || string.IsNullOrWhiteSpace(path))
        {
            throw Diagnostic(new ArgumentException("An asset requires a resource ID and a root-relative path."), "SCN2D010");
        }
        string normalized = path.Replace('\\', '/');
        if (normalized.StartsWith('/') || normalized.Contains(':') || normalized.Contains('\0') ||
            normalized.Split('/').Any(part => part is "" or "." or ".."))
        {
            throw Diagnostic(new ArgumentException("Asset paths must be normalized, local and root-relative.", nameof(path)), "SCN2D010");
        }
        if (size.Width <= 0 || size.Height <= 0)
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(size), "Atlas dimensions must be positive."), "SCN2D007");
        }
        ResourceId = resourceId;
        Path = normalized;
        Size = size;
    }

    public ResourceId<ImageResource> ResourceId { get; }
    public string Path { get; }
    public DrawSize Size { get; }
}

public sealed class TilePromotion2D
{
    public TilePromotion2D(TileCellKey2D cell, int? tileId = null, IReadOnlyDictionary<string, object?>? properties = null)
    {
        if (string.IsNullOrWhiteSpace(cell.LayerId) || tileId is <= 0)
        {
            throw Diagnostic(new ArgumentException("A promotion requires a layer identity and an optional positive tile ID."), "SCN2D012");
        }
        Cell = cell;
        TileId = tileId;
        Properties = TileMapModelCopy.CopyProperties(properties);
    }

    public TileCellKey2D Cell { get; }
    public int? TileId { get; }
    public IReadOnlyDictionary<string, object?> Properties { get; }
}

public sealed class Scene2DEntity
{
    public Scene2DEntity(
        string id,
        string layerId,
        DrawPoint position,
        DrawSize size,
        string shape = "Point",
        string points = "",
        float rotation = 0,
        DrawPoint pivot = default,
        string role = "Metadata",
        IEnumerable<TileColliderDescriptor2D>? colliders = null,
        int order = 0,
        bool isVisible = true,
        float opacity = 1,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(layerId))
        {
            throw Diagnostic(new ArgumentException("An entity requires stable entity and layer identities."), "SCN2D015");
        }
        if (!float.IsFinite(rotation) || !float.IsFinite(opacity) || opacity < 0 || opacity > 1)
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(rotation), "Entity rotation must be finite and opacity must be in [0,1]."), "SCN2D014");
        }
        if (role is not ("Metadata" or "Spawn" or "Collider" or "Promote"))
        {
            throw Diagnostic(new ArgumentException("Unsupported entity role.", nameof(role)), "SCN2D004");
        }
        IReadOnlyList<Vector2> vertices;
        switch (shape)
        {
            case "Box":
            case "Ellipse":
                if (size.Width <= 0 || size.Height <= 0)
                {
                    throw Diagnostic(new ArgumentException("Box and ellipse dimensions must be positive.", nameof(size)), "SCN2D008");
                }
                vertices = Array.Empty<Vector2>();
                break;
            case "Polygon":
                vertices = PolygonCollider2D.ParsePoints(points);
                break;
            case "Polyline":
                vertices = ParseShapePoints(points, 2, MaximumShapePoints);
                for (int index = 1; index < vertices.Count; index++)
                {
                    SegmentCollider2D.ValidateEndpoints(vertices[index - 1], vertices[index]);
                }
                break;
            case "Point":
                vertices = Array.Empty<Vector2>();
                break;
            default:
                throw Diagnostic(new ArgumentException("Unsupported entity geometry.", nameof(shape)), "SCN2D004");
        }
        TileColliderDescriptor2D[] copied = colliders is null ? [] : CopyBounded(colliders, MaximumShapePoints, nameof(colliders));
        if (copied.Any(item => item is null) || (role == "Collider" && (copied.Length == 0 || shape == "Point")))
        {
            throw Diagnostic(new ArgumentException("Collider entities require non-point geometry and valid collider descriptors.", nameof(colliders)), "SCN2D008");
        }
        Id = id;
        LayerId = layerId;
        Position = position;
        Size = size;
        Shape = shape;
        Points = points;
        Vertices = vertices;
        Rotation = rotation;
        Pivot = pivot;
        Role = role;
        Colliders = Array.AsReadOnly(copied);
        Order = order;
        IsVisible = isVisible;
        Opacity = opacity;
        Properties = TileMapModelCopy.CopyProperties(properties);
    }

    public string Id { get; }
    public string LayerId { get; }
    public DrawPoint Position { get; }
    public DrawSize Size { get; }
    public string Shape { get; }
    public string Points { get; }
    public IReadOnlyList<Vector2> Vertices { get; }
    public float Rotation { get; }
    public DrawPoint Pivot { get; }
    public string Role { get; }
    public IReadOnlyList<TileColliderDescriptor2D> Colliders { get; }
    public int Order { get; }
    public bool IsVisible { get; }
    public float Opacity { get; }
    public IReadOnlyDictionary<string, object?> Properties { get; }
}

public sealed class Scene2DLevel
{
    public Scene2DLevel(
        string id,
        TileMap2DModel tileMap,
        DrawPoint worldOffset = default,
        IEnumerable<Scene2DEntity>? entities = null,
        IEnumerable<TilePromotion2D>? promotions = null,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw Diagnostic(new ArgumentException("A level requires a stable identity.", nameof(id)), "SCN2D015");
        }
        ArgumentNullException.ThrowIfNull(tileMap);
        Id = id;
        TileMap = tileMap;
        WorldOffset = worldOffset;
        Entities = Array.AsReadOnly(entities is null ? [] : CopyBounded(entities, MaximumEntities, nameof(entities)));
        Promotions = Array.AsReadOnly(promotions is null ? [] : CopyBounded(promotions, MaximumEntities, nameof(promotions)));
        Properties = TileMapModelCopy.CopyProperties(properties);
        Scene2DDiagnosticCollector diagnostics = new(null);
        ValidateLevel(this, diagnostics, "$");
        ThrowIfInvalid(diagnostics.Complete(), nameof(tileMap));
    }

    public string Id { get; }
    public TileMap2DModel TileMap { get; }
    public DrawPoint WorldOffset { get; }
    public IReadOnlyList<Scene2DEntity> Entities { get; }
    public IReadOnlyList<TilePromotion2D> Promotions { get; }
    public IReadOnlyDictionary<string, object?> Properties { get; }
}

public sealed class Scene2DDocument
{
    public const int CurrentSchemaVersion = 1;

    public Scene2DDocument(
        IEnumerable<Scene2DLevel> levels,
        IEnumerable<Scene2DAsset> assets,
        int schemaVersion = CurrentSchemaVersion,
        IReadOnlyDictionary<string, object?>? properties = null,
        Scene2DValidationOptions? validationOptions = null)
    {
        if (schemaVersion != CurrentSchemaVersion)
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(schemaVersion), "Unsupported core scene document schema."), "SCN2D003");
        }
        Levels = Array.AsReadOnly(CopyBounded(levels, MaximumLayers, nameof(levels)));
        Assets = Array.AsReadOnly(CopyBounded(assets, MaximumLayers, nameof(assets)));
        SchemaVersion = schemaVersion;
        Properties = TileMapModelCopy.CopyProperties(properties);
        ThrowIfInvalid(Validate(this, validationOptions), nameof(levels));
    }

    public int SchemaVersion { get; }
    public IReadOnlyList<Scene2DLevel> Levels { get; }
    public IReadOnlyList<Scene2DAsset> Assets { get; }
    public IReadOnlyDictionary<string, object?> Properties { get; }
}
