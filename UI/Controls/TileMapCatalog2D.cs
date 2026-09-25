using System.Collections.ObjectModel;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Resources;
using static Cerneala.UI.Controls.Scene2DModelValidator;

namespace Cerneala.UI.Controls;

/// <summary>Describes one static chunk without retaining its cells or placements.</summary>
internal sealed class TileMapChunkInfo2D
{
    private readonly HashSet<int> tileIds = [];
    private readonly HashSet<ImageReference> images = [];

    public TileMapChunkInfo2D(SceneSpatialEntry2D spatial, TileMapBounds2D cells,
        IEnumerable<int> tileIds, IEnumerable<ImageReference> images,
        int expandedColliderCount = 0, long? dataResidencyBytes = null)
        : this(spatial, GetCellCount(cells), expandedColliderCount, dataResidencyBytes)
    {
        Cells = cells;
        int[] copied = CopyBounded(tileIds, TileCount, nameof(tileIds));
        foreach (int id in copied)
        {
            if (id <= 0 || !this.tileIds.Add(id))
            {
                throw new ArgumentException("Declared tile IDs must be positive and unique.", nameof(tileIds));
            }
        }
        TileIds = Array.AsReadOnly(copied);
        Images = CopyImages(images);
    }

    public TileMapChunkInfo2D(SceneSpatialEntry2D spatial, int tileCount,
        IEnumerable<ImageReference> images, int expandedColliderCount = 0, long? dataResidencyBytes = null)
        : this(spatial, tileCount, expandedColliderCount, dataResidencyBytes)
    {
        Images = CopyImages(images);
    }

    private IReadOnlyList<ImageReference> CopyImages(IEnumerable<ImageReference> images)
    {
        ImageReference[] copied = CopyBounded(images, TileCount, nameof(images));
        foreach (ImageReference image in copied)
        {
            ArgumentNullException.ThrowIfNull(image);
            if (!this.images.Add(image)) { throw new ArgumentException("Declared images must be unique.", nameof(images)); }
        }
        return Array.AsReadOnly(copied);
    }

    private TileMapChunkInfo2D(SceneSpatialEntry2D spatial, int tileCount, int expandedColliderCount,
        long? dataResidencyBytes)
    {
        ArgumentNullException.ThrowIfNull(spatial);
        if (spatial.IsSimulated) { throw new ArgumentException("Static tile chunks cannot be simulated entities.", nameof(spatial)); }
        if (tileCount <= 0 || tileCount > MaximumCells) { throw new ArgumentOutOfRangeException(nameof(tileCount)); }
        if (expandedColliderCount < 0 || expandedColliderCount > tileCount || expandedColliderCount > MaximumExpandedTileColliders)
        {
            throw new ArgumentOutOfRangeException(nameof(expandedColliderCount));
        }
        if (expandedColliderCount > 0 && spatial.CollisionBounds is null)
        {
            throw new ArgumentException("Colliding chunks require collision bounds.", nameof(spatial));
        }
        if (dataResidencyBytes < 0) { throw new ArgumentOutOfRangeException(nameof(dataResidencyBytes)); }
        Spatial = spatial;
        TileCount = tileCount;
        ExpandedColliderCount = expandedColliderCount;
        DataResidencyBytes = dataResidencyBytes;
    }

    public SceneSpatialEntry2D Spatial { get; }
    public TileMapBounds2D? Cells { get; }
    public int TileCount { get; }
    public int ExpandedColliderCount { get; }
    /// <summary>
    /// Source-declared conservative charge for payload data retained by an acquisition,
    /// excluding backing data retained independently by the source and graphical resources.
    /// Null means unknown, not zero. This is not a measured process-memory limit.
    /// </summary>
    public long? DataResidencyBytes { get; }
    public IReadOnlyList<int> TileIds { get; } = Array.Empty<int>();
    public IReadOnlyList<ImageReference> Images { get; } = Array.Empty<ImageReference>();
    internal bool ContainsTile(int id) => tileIds.Contains(id);
    internal bool ContainsImage(ImageReference image) => images.Contains(image);

    private static int GetCellCount(TileMapBounds2D cells)
    {
        long count = (long)cells.Width * cells.Height;
        if (cells.Width <= 0 || cells.Height <= 0 || count > MaximumCells)
        {
            throw new ArgumentOutOfRangeException(nameof(cells));
        }
        return (int)count;
    }
}

/// <summary>An immutable map header and complete spatial catalog, not loaded cell data.</summary>
internal sealed class TileMapCatalog2D
{
    private readonly Dictionary<string, TileMapChunkInfo2D> byId = new(StringComparer.Ordinal);
    private readonly Dictionary<ImageReference, DrawSize> directSizes = [];

    public TileMapCatalog2D(string id, IEnumerable<TileMapChunkInfo2D> chunks,
        DrawSize? tileSize = null,
        TileMapBounds2D? bounds = null, int order = 0, bool isVisible = true,
        DrawPoint offset = default, float opacity = 1, Color? tint = null, long version = 1,
        IReadOnlyDictionary<string, DrawSize>? imageSizes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);
        if (!float.IsFinite(opacity) || opacity < 0 || opacity > 1) { throw new ArgumentOutOfRangeException(nameof(opacity)); }
        DrawArgument.ThrowIfNotValidPixelCoordinate(offset.X, nameof(offset));
        DrawArgument.ThrowIfNotValidPixelCoordinate(offset.Y, nameof(offset));
        if (tileSize is DrawSize gridSize) { ValidateImageSize(gridSize, nameof(tileSize)); }
        if (bounds is TileMapBounds2D finite && (tileSize is null || finite.Width <= 0 || finite.Height <= 0))
        {
            throw new ArgumentException("Finite cell bounds require a positive grid.", nameof(bounds));
        }

        TileMapChunkInfo2D[] copied = CopyBounded(chunks, MaximumChunks, nameof(chunks));
        Dictionary<string, DrawSize> sizes = new(StringComparer.Ordinal);
        if (imageSizes is not null)
        {
            foreach ((string key, DrawSize size) in imageSizes)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(key);
                ValidateImageSize(size, nameof(imageSizes));
                if (sizes.Count == MaximumLayers) { throw new ArgumentException("The catalog exceeds its image metadata budget.", nameof(imageSizes)); }
                sizes.Add(key, size);
            }
        }

        Id = id;
        IsFreePlacement = tileSize is null;
        TileSize = tileSize ?? default;
        Bounds = bounds;
        Order = order;
        IsVisible = isVisible;
        Offset = offset;
        Opacity = opacity;
        Tint = tint ?? Color.White;
        Version = version;
        ImageSizes = new ReadOnlyDictionary<string, DrawSize>(sizes);
        Chunks = Array.AsReadOnly(copied);
        long cells = 0, colliders = 0;
        foreach (TileMapChunkInfo2D info in copied)
        {
            ArgumentNullException.ThrowIfNull(info);
            if (!byId.TryAdd(info.Spatial.Id, info)) { throw new ArgumentException("Chunk identities must be unique.", nameof(chunks)); }
            if (IsFreePlacement != (info.Cells is null)) { throw new ArgumentException("A catalog cannot mix grid and free-placement chunks.", nameof(chunks)); }
            cells += info.TileCount;
            colliders += info.ExpandedColliderCount;
            if (cells > MaximumCells || colliders > MaximumExpandedTileColliders)
            {
                throw new ArgumentException("The catalog exceeds its aggregate cell or expanded collider budget.", nameof(chunks));
            }
            if (info.Cells is TileMapBounds2D grid)
            {
                DrawRect visual = GetGridBounds(TileSize, grid);
                RequireContains(info.Spatial.Bounds, visual, "Grid geometry exceeds its declared visual bounds.");
                if (bounds is TileMapBounds2D map && (!map.Contains(new(grid.X, grid.Y)) || grid.Right > map.Right || grid.Bottom > map.Bottom))
                {
                    throw new ArgumentException("Chunk exceeds finite map bounds.", nameof(chunks));
                }
            }
            foreach (ImageReference image in info.Images)
            {
                if (image.DirectImage is IDrawImage direct && !directSizes.ContainsKey(image))
                {
                    DrawSize size = new(direct.Width, direct.Height);
                    ValidateImageSize(size, nameof(chunks));
                    directSizes.Add(image, size);
                }
            }
            ValidateTranslatedBounds(info.Spatial.Bounds, offset);
            if (info.Spatial.CollisionBounds is DrawRect collision) { ValidateTranslatedBounds(collision, offset); }
        }
        if (!IsFreePlacement) { TileMap2DModel.ValidateNoOverlaps(copied.Select(static info => info.Cells!.Value).ToArray()); }
        Entries = Array.AsReadOnly(copied.Select(static info => info.Spatial).ToArray());
    }

    public string Id { get; }
    public bool IsFreePlacement { get; }
    public DrawSize TileSize { get; }
    public TileMapBounds2D? Bounds { get; }
    public IReadOnlyList<TileMapChunkInfo2D> Chunks { get; }
    public IReadOnlyList<SceneSpatialEntry2D> Entries { get; }
    public IReadOnlyDictionary<string, DrawSize> ImageSizes { get; }
    public int Order { get; }
    public bool IsVisible { get; }
    public DrawPoint Offset { get; }
    public float Opacity { get; }
    public Color Tint { get; }
    public long Version { get; }

    public bool TryGetImageSize(ImageReference image, out DrawSize size)
    {
        ArgumentNullException.ThrowIfNull(image);
        return image.ResourceId is ResourceId<ImageResource> resource
            ? ImageSizes.TryGetValue(resource.Key, out size) : directSizes.TryGetValue(image, out size);
    }

    internal bool TryGetChunk(string id, out TileMapChunkInfo2D? info) => byId.TryGetValue(id, out info);

    internal DrawRect GetPlacementDestination(Tile tile)
    {
        bool known = TryGetImageSize(tile.Image, out DrawSize size);
        if (!known && (float.IsNaN(tile.Width) || float.IsNaN(tile.Height)))
        {
            throw new ArgumentException($"Natural-size placement requires image metadata for '{tile.Image.ResourceId?.Key ?? "direct image"}'.", nameof(tile));
        }
        return tile.GetDestination(size);
    }

    internal void ValidatePayload(TileMapChunkInfo2D info, TileMapChunkData2D data)
    {
        ArgumentNullException.ThrowIfNull(data);
        int colliders = 0;
        DrawRect? collisionBounds = null;
        if (info.Cells is TileMapBounds2D cells)
        {
            TileChunk2D? chunk = data.Grid;
            if (chunk is null || chunk.Origin != new TileCoordinate2D(cells.X, cells.Y) ||
                chunk.Width != cells.Width || chunk.Height != cells.Height || chunk.Version != info.Spatial.Version)
            {
                throw new ArgumentException("Loaded grid coordinates, dimensions or revision differ from the catalog.", nameof(data));
            }
            foreach (TileSet2D set in data.TileSets)
            {
                if (!info.ContainsImage(data.GetTileSetImage(set.Id)))
                {
                    throw new ArgumentException("Loaded palette uses an undeclared image.", nameof(data));
                }
            }
            Scene2DDiagnosticCollector validation = new();
            Scene2DModelValidator.ValidateTileSets(data.TileSets, ImageSizes, validation, "$", requireAllAtlases: false);
            ThrowIfInvalid(validation.Complete(), nameof(data));
            ValidateChunkGeometry(TileSize, chunk, Offset);
            for (int index = 0; index < chunk.Tiles.Count; index++)
            {
                TileCell2D cell = chunk.Tiles[index];
                if (cell.TileId == 0) { continue; }
                if (!info.ContainsTile(cell.TileId) || !data.TryResolveTile(cell.TileId, out _, out TileDefinition2D? definition))
                {
                    throw new ArgumentException("Loaded cell uses an undeclared tile ID.", nameof(data));
                }
                if (definition!.Collider is not TileColliderDescriptor2D descriptor) { continue; }
                colliders++;
                TileCoordinate2D coordinate = TileFlipGeometry2D.GetCellCoordinate(chunk, index);
                Matrix3x2 placement = TileFlipGeometry2D.GetCellTransform(coordinate, cell.Flip, TileSize);
                descriptor.ValidateGeometry(placement * Matrix3x2.CreateTranslation(Offset.X, Offset.Y));
                collisionBounds = Union(collisionBounds, SceneGeometry2D.GetColliderBounds(descriptor, placement));
            }
        }
        else
        {
            if (data.Grid is not null || data.Placements.Count != info.TileCount)
            {
                throw new ArgumentException("Loaded placement kind or count differs from the catalog.", nameof(data));
            }
            foreach (Tile tile in data.Placements)
            {
                if (!info.ContainsImage(tile.Image)) { throw new ArgumentException("Loaded placement uses an undeclared image.", nameof(data)); }
                DrawRect destination = GetPlacementDestination(tile);
                RequireContains(info.Spatial.Bounds, destination, "Loaded placement exceeds its visual bounds.");
                ValidateTranslatedBounds(destination, Offset);
                if (tile.Collider is not TileColliderDescriptor2D descriptor) { continue; }
                colliders++;
                Matrix3x2 placement = Matrix3x2.CreateTranslation(tile.X, tile.Y);
                descriptor.ValidateGeometry(placement * Matrix3x2.CreateTranslation(Offset.X, Offset.Y));
                collisionBounds = Union(collisionBounds, SceneGeometry2D.GetColliderBounds(descriptor, placement));
            }
        }
        if (colliders != info.ExpandedColliderCount) { throw new ArgumentException("Loaded collider count differs from the catalog.", nameof(data)); }
        if (collisionBounds is DrawRect actual)
        {
            if (info.Spatial.CollisionBounds is not DrawRect declared) { throw new ArgumentException("Loaded collision geometry was not declared.", nameof(data)); }
            RequireContains(declared, actual, "Loaded collision geometry exceeds its declared bounds.");
        }
    }

    internal static DrawRect GetGridBounds(DrawSize tileSize, TileMapBounds2D cells)
    {
        DrawRect result = new(cells.X * tileSize.Width, cells.Y * tileSize.Height, cells.Width * tileSize.Width, cells.Height * tileSize.Height);
        ValidateTranslatedBounds(result, default);
        return result;
    }

    internal static DrawRect Union(DrawRect? left, DrawRect right)
    {
        if (left is not DrawRect first) { return right; }
        float x = Math.Min(first.X, right.X), y = Math.Min(first.Y, right.Y);
        DrawRect result = new(x, y, Math.Max(first.Right, right.Right) - x, Math.Max(first.Bottom, right.Bottom) - y);
        SceneSpatialEntry2D.ValidateBounds(result, nameof(right));
        return result;
    }

    private static void RequireContains(DrawRect outer, DrawRect inner, string message)
    {
        if (inner.X < outer.X || inner.Y < outer.Y || inner.Right > outer.Right || inner.Bottom > outer.Bottom)
        {
            throw new ArgumentException(message);
        }
    }

    private static void ValidateTranslatedBounds(DrawRect bounds, DrawPoint offset)
    {
        SceneSpatialEntry2D.ValidateBounds(bounds, nameof(bounds));
        DrawArgument.ThrowIfNotValidPixelCoordinate((float)((double)bounds.X + offset.X), nameof(bounds));
        DrawArgument.ThrowIfNotValidPixelCoordinate((float)((double)bounds.Y + offset.Y), nameof(bounds));
        DrawArgument.ThrowIfNotValidPixelCoordinate((float)((double)bounds.X + bounds.Width + offset.X), nameof(bounds));
        DrawArgument.ThrowIfNotValidPixelCoordinate((float)((double)bounds.Y + bounds.Height + offset.Y), nameof(bounds));
    }

    private static void ValidateImageSize(DrawSize size, string parameter)
    {
        if (!float.IsFinite(size.Width) || !float.IsFinite(size.Height) || size.Width <= 0 || size.Height <= 0)
        {
            throw new ArgumentException("Declared dimensions must be finite and positive.", parameter);
        }
    }
}
