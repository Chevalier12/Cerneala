using System.Collections.ObjectModel;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Resources;
using static Cerneala.UI.Controls.Scene2DModelValidator;

namespace Cerneala.UI.Controls;

[Flags]
public enum TileFlip2D
{
    None = 0,
    Horizontal = 1,
    Vertical = 2,
    Diagonal = 4
}

public readonly record struct TileCoordinate2D(int X, int Y);

public readonly record struct TileCellKey2D
{
    public TileCellKey2D(string layerId, TileCoordinate2D coordinate)
    {
        LayerId = ValidateLayerId(layerId);
        Coordinate = coordinate;
    }

    public TileCellKey2D(string layerId, int x, int y)
        : this(ValidateLayerId(layerId), new TileCoordinate2D(x, y))
    {
    }

    public string LayerId { get; }

    public TileCoordinate2D Coordinate { get; }

    private static string ValidateLayerId(string layerId)
    {
        if (string.IsNullOrWhiteSpace(layerId))
        {
            throw Diagnostic(new ArgumentException("Layer id cannot be empty.", nameof(layerId)), "SCN2D015");
        }

        return layerId;
    }
}

public readonly record struct TileMapBounds2D
{
    public TileMapBounds2D(int x, int y, int width, int height)
    {
        if (width <= 0)
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(width)), "SCN2D005");
        }
        if (height <= 0)
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(height)), "SCN2D005");
        }

        if ((long)x + width > int.MaxValue || (long)y + height > int.MaxValue)
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(width), "Bounds endpoints must fit Int32."), "SCN2D005");
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int X { get; }

    public int Y { get; }

    public int Width { get; }

    public int Height { get; }

    public int Right => checked(X + Width);

    public int Bottom => checked(Y + Height);

    public bool Contains(TileCoordinate2D coordinate) =>
        coordinate.X >= X && coordinate.X < Right &&
        coordinate.Y >= Y && coordinate.Y < Bottom;
}

public readonly record struct TileCell2D
{
    public TileCell2D(int tileId, TileFlip2D flip = TileFlip2D.None)
    {
        if (tileId < 0)
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(tileId)), "SCN2D006");
        }
        if ((flip & ~(TileFlip2D.Horizontal | TileFlip2D.Vertical | TileFlip2D.Diagonal)) != 0)
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(flip)), "SCN2D004");
        }

        TileId = tileId;
        Flip = flip;
    }

    public int TileId { get; }

    public TileFlip2D Flip { get; }
}

public sealed class TileDefinition2D
{
    private readonly ReadOnlyCollection<TileColliderDescriptor2D> colliders;

    public TileDefinition2D(
        int id,
        DrawRect sourceRect,
        IReadOnlyDictionary<string, object?>? properties = null,
        IEnumerable<TileColliderDescriptor2D>? colliders = null)
    {
        if (id <= 0)
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(id)), "SCN2D006");
        }
        if (sourceRect.Width <= 0 || sourceRect.Height <= 0)
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(sourceRect)), "SCN2D007");
        }

        Id = id;
        SourceRect = sourceRect;
        Properties = TileMapModelCopy.CopyProperties(properties);
        TileColliderDescriptor2D[] copiedColliders = colliders is null ? [] : CopyBounded(colliders, MaximumShapePoints, nameof(colliders));
        if (copiedColliders.Any(static collider => collider is null))
        {
            throw Diagnostic(new ArgumentException("Tile colliders cannot contain null descriptors.", nameof(colliders)), "SCN2D008");
        }
        this.colliders = Array.AsReadOnly(copiedColliders);
    }

    public int Id { get; }

    public DrawRect SourceRect { get; }

    public IReadOnlyDictionary<string, object?> Properties { get; }

    public IReadOnlyList<TileColliderDescriptor2D> Colliders => colliders;
}

public sealed class TileSet2D
{
    private readonly ReadOnlyCollection<TileDefinition2D> tiles;

    public TileSet2D(
        string id,
        ResourceId<ImageResource> atlasResourceId,
        IEnumerable<TileDefinition2D> tiles,
        long version = 1,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw Diagnostic(new ArgumentException("Tileset id cannot be empty.", nameof(id)), "SCN2D015");
        }
        if (string.IsNullOrWhiteSpace(atlasResourceId.Key))
        {
            throw Diagnostic(new ArgumentException("Atlas resource id cannot be empty.", nameof(atlasResourceId)), "SCN2D010");
        }
        if (version <= 0)
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(version)), "SCN2D003");
        }
        ArgumentNullException.ThrowIfNull(tiles);
        TileDefinition2D[] copied = CopyBounded(tiles, MaximumCells, nameof(tiles));
        if (copied.Length == 0)
        {
            throw Diagnostic(new ArgumentException("A tileset must define at least one tile.", nameof(tiles)), "SCN2D006");
        }
        if (copied.Any(static tile => tile is null))
        {
            throw Diagnostic(new ArgumentException("A tileset cannot contain null tile definitions.", nameof(tiles)), "SCN2D006");
        }
        int duplicateId = copied
            .GroupBy(static tile => tile.Id)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .FirstOrDefault();
        if (duplicateId != 0)
        {
            throw Diagnostic(new ArgumentException($"Tile id {duplicateId} is duplicated in tileset '{id}'.", nameof(tiles)), "SCN2D015");
        }

        Id = id;
        AtlasResourceId = atlasResourceId;
        this.tiles = Array.AsReadOnly(copied);
        Version = version;
        Properties = TileMapModelCopy.CopyProperties(properties);
    }

    public string Id { get; }

    public ResourceId<ImageResource> AtlasResourceId { get; }

    public IReadOnlyList<TileDefinition2D> Tiles => tiles;

    public long Version { get; }

    public IReadOnlyDictionary<string, object?> Properties { get; }
}

public sealed class TileChunk2D
{
    private readonly ReadOnlyCollection<TileCell2D> tiles;

    public TileChunk2D(
        TileCoordinate2D origin,
        int width,
        int height,
        IEnumerable<TileCell2D> tiles,
        long version = 1,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        if (width <= 0)
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(width)), "SCN2D005");
        }
        if (height <= 0)
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(height)), "SCN2D005");
        }
        if (version <= 0)
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(version)), "SCN2D003");
        }
        ArgumentNullException.ThrowIfNull(tiles);
        long count = (long)width * height;
        if (count > MaximumCells)
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(width), $"A chunk is limited to {MaximumCells} cells."), "SCN2D013");
        }
        _ = new TileMapBounds2D(origin.X, origin.Y, width, height);
        int expected = (int)count;
        TileCell2D[] copied = CopyBounded(tiles, expected, nameof(tiles), "SCN2D005");
        if (copied.Length != expected)
        {
            throw Diagnostic(new ArgumentException(
                $"Chunk tile count must equal width * height ({expected}).",
                nameof(tiles)), "SCN2D005");
        }

        Origin = origin;
        Width = width;
        Height = height;
        this.tiles = Array.AsReadOnly(copied);
        Version = version;
        Properties = TileMapModelCopy.CopyProperties(properties);
    }

    public TileCoordinate2D Origin { get; }

    public int Width { get; }

    public int Height { get; }

    public IReadOnlyList<TileCell2D> Tiles => tiles;

    public long Version { get; }

    public IReadOnlyDictionary<string, object?> Properties { get; }

    public bool Contains(TileCoordinate2D coordinate) =>
        coordinate.X >= Origin.X && coordinate.X < checked(Origin.X + Width) &&
        coordinate.Y >= Origin.Y && coordinate.Y < checked(Origin.Y + Height);

    public TileCell2D GetCell(TileCoordinate2D coordinate)
    {
        if (!Contains(coordinate))
        {
            throw new ArgumentOutOfRangeException(nameof(coordinate));
        }

        int localX = coordinate.X - Origin.X;
        int localY = coordinate.Y - Origin.Y;
        return tiles[(localY * Width) + localX];
    }
}

public sealed class TileLayer2DModel
{
    private readonly ReadOnlyCollection<TileChunk2D> chunks;

    public TileLayer2DModel(
        string id,
        IEnumerable<TileChunk2D> chunks,
        int order = 0,
        bool isVisible = true,
        DrawPoint offset = default,
        float opacity = 1,
        Color? tint = null,
        long version = 1,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw Diagnostic(new ArgumentException("Layer id cannot be empty.", nameof(id)), "SCN2D015");
        }
        if (!float.IsFinite(offset.X) || !float.IsFinite(offset.Y))
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(offset)), "SCN2D014");
        }
        if (!float.IsFinite(opacity) || opacity < 0 || opacity > 1)
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(opacity)), "SCN2D014");
        }
        if (version <= 0)
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(version)), "SCN2D003");
        }
        ArgumentNullException.ThrowIfNull(chunks);
        TileChunk2D[] copied = CopyBounded(chunks, MaximumChunks, nameof(chunks));
        if (copied.Any(static chunk => chunk is null))
        {
            throw Diagnostic(new ArgumentException("A tile layer cannot contain null chunks.", nameof(chunks)), "SCN2D005");
        }
        ValidateNoOverlaps(copied);

        Id = id;
        this.chunks = Array.AsReadOnly(copied);
        Order = order;
        IsVisible = isVisible;
        Offset = offset;
        Opacity = opacity;
        Tint = tint ?? Color.White;
        Version = version;
        Properties = TileMapModelCopy.CopyProperties(properties);
    }

    public string Id { get; }

    public bool IsVisible { get; }

    public DrawPoint Offset { get; }

    public float Opacity { get; }

    public Color Tint { get; }

    public int Order { get; }

    public IReadOnlyList<TileChunk2D> Chunks => chunks;

    public long Version { get; }

    public IReadOnlyDictionary<string, object?> Properties { get; }

    public bool TryGetCell(TileCoordinate2D coordinate, out TileCell2D cell)
    {
        foreach (TileChunk2D chunk in chunks)
        {
            if (chunk.Contains(coordinate))
            {
                cell = chunk.GetCell(coordinate);
                return true;
            }
        }

        cell = default;
        return false;
    }

    private static void ValidateNoOverlaps(IReadOnlyList<TileChunk2D> chunks)
    {
        if (chunks.Count < 2) { return; }
        // Sweep exclusive X bounds. Until the first overlap, active Y intervals
        // are disjoint, so only the immediate predecessor/successor can overlap.
        // Integer comparisons preserve negative and remote chunk coordinates.
        int[] ordered = Enumerable.Range(0, chunks.Count)
            .OrderBy(index => chunks[index].Origin.X).ThenBy(index => index).ToArray();
        SortedSet<ChunkInterval> active = new(Comparer<ChunkInterval>.Create(static (left, right) =>
        {
            int top = left.Top.CompareTo(right.Top);
            return top != 0 ? top : left.Index.CompareTo(right.Index);
        }));
        PriorityQueue<ChunkInterval, int> ending = new();
        ChunkInterval minimum = new(int.MinValue, int.MinValue, int.MinValue);
        ChunkInterval maximum = new(int.MaxValue, int.MaxValue, int.MaxValue);
        foreach (int index in ordered)
        {
            TileChunk2D chunk = chunks[index];
            while (ending.TryPeek(out ChunkInterval? expired, out int right) && right <= chunk.Origin.X)
            {
                ending.Dequeue();
                active.Remove(expired);
            }
            ChunkInterval probe = new(chunk.Origin.Y, int.MaxValue, 0);
            ChunkInterval? before = active.GetViewBetween(minimum, probe).Max;
            ChunkInterval? after = active.GetViewBetween(probe, maximum).Min;
            ChunkInterval? overlap = before is not null && before.Bottom > chunk.Origin.Y ? before
                : after is not null && after.Top < chunk.Origin.Y + chunk.Height ? after : null;
            if (overlap is not null)
            {
                TileChunk2D other = chunks[overlap.Index];
                throw Diagnostic(new ArgumentException(
                    $"Chunks at ({other.Origin.X},{other.Origin.Y}) and ({chunk.Origin.X},{chunk.Origin.Y}) overlap.",
                    nameof(chunks)), "SCN2D011");
            }
            ChunkInterval interval = new(chunk.Origin.Y, index, chunk.Origin.Y + chunk.Height);
            active.Add(interval);
            ending.Enqueue(interval, chunk.Origin.X + chunk.Width);
        }
    }

    private sealed record ChunkInterval(int Top, int Index, int Bottom);
}

public sealed class TileMap2DModel
{
    private readonly ReadOnlyCollection<TileSet2D> tileSets;
    private readonly ReadOnlyCollection<TileLayer2DModel> layers;
    private readonly Dictionary<int, ResolvedTile> tileLookup;

    public TileMap2DModel(
        DrawSize tileSize,
        IEnumerable<TileSet2D> tileSets,
        IEnumerable<TileLayer2DModel> layers,
        TileMapBounds2D? bounds = null,
        long version = 1,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        if (!float.IsFinite(tileSize.Width) || tileSize.Width <= 0 ||
            !float.IsFinite(tileSize.Height) || tileSize.Height <= 0)
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(tileSize)), "SCN2D005");
        }
        if (version <= 0)
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(version)), "SCN2D003");
        }
        ArgumentNullException.ThrowIfNull(tileSets);
        ArgumentNullException.ThrowIfNull(layers);
        if (bounds is TileMapBounds2D finite && (finite.Width <= 0 || finite.Height <= 0))
        {
            throw Diagnostic(new ArgumentException("Finite bounds must have positive dimensions.", nameof(bounds)), "SCN2D005");
        }
        TileSet2D[] copiedTileSets = CopyBounded(tileSets, MaximumLayers, nameof(tileSets));
        TileLayer2DModel[] copiedLayers = CopyBounded(layers, MaximumLayers, nameof(layers));
        if (copiedTileSets.Any(static tileSet => tileSet is null))
        {
            throw Diagnostic(new ArgumentException("A tilemap cannot contain null tilesets.", nameof(tileSets)), "SCN2D010");
        }
        if (copiedLayers.Any(static layer => layer is null))
        {
            throw Diagnostic(new ArgumentException("A tilemap cannot contain null layers.", nameof(layers)), "SCN2D005");
        }
        ValidateAggregateBudgets(copiedTileSets, copiedLayers);
        ValidateUniqueIds(copiedTileSets, copiedLayers);
        ValidateCells(tileSize, copiedTileSets, copiedLayers, bounds);

        TileSize = tileSize;
        this.tileSets = Array.AsReadOnly(copiedTileSets);
        this.layers = Array.AsReadOnly(copiedLayers);
        tileLookup = copiedTileSets
            .SelectMany(static tileSet => tileSet.Tiles.Select(tile =>
                new KeyValuePair<int, ResolvedTile>(
                    tile.Id,
                    new ResolvedTile(tileSet, tile))))
            .ToDictionary(static pair => pair.Key, static pair => pair.Value);
        Bounds = bounds;
        Version = version;
        Properties = TileMapModelCopy.CopyProperties(properties);
    }

    public DrawSize TileSize { get; }

    public TileMapBounds2D? Bounds { get; }

    public IReadOnlyList<TileSet2D> TileSets => tileSets;

    public IReadOnlyList<TileLayer2DModel> Layers => layers;

    public long Version { get; }

    public IReadOnlyDictionary<string, object?> Properties { get; }

    public bool TryResolveTile(
        int tileId,
        out TileSet2D? tileSet,
        out TileDefinition2D? definition)
    {
        if (tileId == 0)
        {
            tileSet = null;
            definition = null;
            return false;
        }

        if (tileLookup.TryGetValue(tileId, out ResolvedTile resolved))
        {
            tileSet = resolved.TileSet;
            definition = resolved.Definition;
            return true;
        }

        tileSet = null;
        definition = null;
        return false;
    }

    private readonly record struct ResolvedTile(
        TileSet2D TileSet,
        TileDefinition2D Definition);

    public bool TryGetLayer(string layerId, out TileLayer2DModel? layer)
    {
        layer = layers.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, layerId, StringComparison.Ordinal));
        return layer is not null;
    }

    private static void ValidateUniqueIds(
        IReadOnlyList<TileSet2D> tileSets,
        IReadOnlyList<TileLayer2DModel> layers)
    {
        string? duplicateTileset = tileSets
            .GroupBy(static tileSet => tileSet.Id, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .FirstOrDefault();
        if (duplicateTileset is not null)
        {
            throw Diagnostic(new ArgumentException($"Tileset id '{duplicateTileset}' is duplicated.", nameof(tileSets)), "SCN2D015");
        }

        int duplicateTile = tileSets
            .SelectMany(static tileSet => tileSet.Tiles)
            .GroupBy(static tile => tile.Id)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .FirstOrDefault();
        if (duplicateTile != 0)
        {
            throw Diagnostic(new ArgumentException($"Tile id {duplicateTile} is defined by multiple tilesets.", nameof(tileSets)), "SCN2D015");
        }

        string? duplicateLayer = layers
            .GroupBy(static layer => layer.Id, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .FirstOrDefault();
        if (duplicateLayer is not null)
        {
            throw Diagnostic(new ArgumentException($"Layer id '{duplicateLayer}' is duplicated.", nameof(layers)), "SCN2D015");
        }
    }

    private static void ValidateAggregateBudgets(IReadOnlyList<TileSet2D> tileSets, IReadOnlyList<TileLayer2DModel> layers)
    {
        long definitions = 0, chunks = 0, cells = 0;
        foreach (TileSet2D set in tileSets)
        {
            definitions += set.Tiles.Count;
            if (definitions > MaximumCells)
            {
                throw Diagnostic(new ArgumentException("A tilemap exceeds its total tile definition budget.", nameof(tileSets)), "SCN2D013");
            }
        }
        foreach (TileLayer2DModel layer in layers)
        {
            chunks += layer.Chunks.Count;
            if (chunks > MaximumChunks)
            {
                throw Diagnostic(new ArgumentException("A tilemap exceeds its total chunk budget.", nameof(layers)), "SCN2D013");
            }
            foreach (TileChunk2D chunk in layer.Chunks)
            {
                cells += chunk.Tiles.Count;
                if (cells > MaximumCells)
                {
                    throw Diagnostic(new ArgumentException("A tilemap exceeds its total cell budget.", nameof(layers)), "SCN2D013");
                }
            }
        }
    }

    private static void ValidateCells(
        DrawSize tileSize,
        IReadOnlyList<TileSet2D> tileSets,
        IReadOnlyList<TileLayer2DModel> layers,
        TileMapBounds2D? bounds)
    {
        Dictionary<int, TileDefinition2D> definitions = tileSets
            .SelectMany(static tileSet => tileSet.Tiles)
            .ToDictionary(static tile => tile.Id);
        long colliderInstances = 0;
        foreach (TileLayer2DModel layer in layers)
        {
            foreach (TileChunk2D chunk in layer.Chunks)
            {
                ValidateChunkGeometry(tileSize, chunk, layer.Offset);
                if (bounds is TileMapBounds2D finite &&
                    (!finite.Contains(chunk.Origin) ||
                     checked(chunk.Origin.X + chunk.Width) > finite.Right ||
                     checked(chunk.Origin.Y + chunk.Height) > finite.Bottom))
                {
                    throw Diagnostic(new ArgumentException(
                        $"Chunk ({chunk.Origin.X},{chunk.Origin.Y}) in layer '{layer.Id}' exceeds finite map bounds.",
                        nameof(layers)), "SCN2D005");
                }

                for (int index = 0; index < chunk.Tiles.Count; index++)
                {
                    TileCell2D cell = chunk.Tiles[index];
                    if (cell.TileId == 0) { continue; }
                    if (!definitions.TryGetValue(cell.TileId, out TileDefinition2D? definition))
                    {
                        throw Diagnostic(new ArgumentException(
                            $"Tile id {cell.TileId} in layer '{layer.Id}' has no tileset definition.",
                            nameof(layers)), "SCN2D006");
                    }
                    colliderInstances += definition.Colliders.Count;
                    if (colliderInstances > MaximumExpandedTileColliders)
                    {
                        throw Diagnostic(new ArgumentException($"A tilemap is limited to {MaximumExpandedTileColliders} expanded tile collider descriptors before coalescing.", nameof(layers)), "SCN2D013");
                    }
                    if (definition.Colliders.Count > 0)
                    {
                        Matrix3x2 placement = TileFlipGeometry2D.Transform(cell.Flip, tileSize) * Matrix3x2.CreateTranslation(
                            (chunk.Origin.X + index % chunk.Width) * tileSize.Width + layer.Offset.X,
                            (chunk.Origin.Y + index / chunk.Width) * tileSize.Height + layer.Offset.Y);
                        foreach (TileColliderDescriptor2D collider in definition.Colliders) { collider.ValidateGeometry(placement); }
                    }
                }
            }
        }
    }
}

internal static class TileMapModelCopy
{
    internal static IReadOnlyDictionary<string, object?> CopyProperties(
        IReadOnlyDictionary<string, object?>? properties) =>
        properties is null || properties.Count == 0
            ? new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>(StringComparer.Ordinal))
            : new ReadOnlyDictionary<string, object?>(
                new Dictionary<string, object?>(properties, StringComparer.Ordinal));
}
