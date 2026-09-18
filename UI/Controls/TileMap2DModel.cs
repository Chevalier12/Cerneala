using System.Collections.ObjectModel;
using System.Numerics;
using System.Runtime.InteropServices;
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
    public TileCellKey2D(string mapId, TileCoordinate2D coordinate)
    {
        MapId = ValidateMapId(mapId);
        Coordinate = coordinate;
    }

    public TileCellKey2D(string mapId, int x, int y)
        : this(ValidateMapId(mapId), new TileCoordinate2D(x, y))
    {
    }

    public string MapId { get; }

    public TileCoordinate2D Coordinate { get; }

    private static string ValidateMapId(string mapId)
    {
        if (string.IsNullOrWhiteSpace(mapId))
        {
            throw Diagnostic(new ArgumentException("Map id cannot be empty.", nameof(mapId)), "SCN2D015");
        }

        return mapId;
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
    public TileDefinition2D(
        int id,
        DrawRect sourceRect,
        IReadOnlyDictionary<string, object?>? properties = null,
        TileColliderDescriptor2D? collider = null)
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
        Collider = collider;
    }

    public int Id { get; }

    public DrawRect SourceRect { get; }

    public IReadOnlyDictionary<string, object?> Properties { get; }

    public TileColliderDescriptor2D? Collider { get; }
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
        HashSet<int> ids = [];
        int duplicateId = 0;
        // Reverse traversal retains the duplicate with the earliest first
        // occurrence, matching the original diagnostic without storing groups.
        for (int index = copied.Length - 1; index >= 0; index--)
        {
            if (!ids.Add(copied[index].Id)) { duplicateId = copied[index].Id; }
        }
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
    private readonly TileCell2D[] tiles;

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
        this.tiles = copied;
        Tiles = Array.AsReadOnly(copied);
        Version = version;
        Properties = TileMapModelCopy.CopyProperties(properties);
    }

    public TileCoordinate2D Origin { get; }

    public int Width { get; }

    public int Height { get; }

    public IReadOnlyList<TileCell2D> Tiles { get; }

    // TileCell2D is exactly two Int32 fields, without padding or references.
    // The guarded layout permits exact byte comparison, not a hash/version shortcut.
    internal bool HasSameCells(TileChunk2D other) =>
        MemoryMarshal.AsBytes(tiles.AsSpan()).SequenceEqual(
            MemoryMarshal.AsBytes(other.tiles.AsSpan()));

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

public sealed class TileMap2DModel
{
    private readonly ReadOnlyCollection<TileSet2D> tileSets;
    private readonly ReadOnlyCollection<TileChunk2D> chunks;
    private readonly Dictionary<int, ResolvedTile> tileLookup;

    public TileMap2DModel(IEnumerable<Tile> tiles, long version = 1, string id = "Tiles")
    {
        ArgumentNullException.ThrowIfNull(tiles);
        Id = ValidateId(id);
        if (version <= 0) { throw Diagnostic(new ArgumentOutOfRangeException(nameof(version)), "SCN2D003"); }
        Tile[] copied = CopyBounded(tiles, MaximumCells, nameof(tiles));
        if (copied.Any(static tile => tile is null))
        {
            throw new ArgumentException("Tile placements cannot contain null.", nameof(tiles));
        }
        long colliderCount = 0;
        foreach (Tile tile in copied)
        {
            colliderCount += tile.Collider is null ? 0 : 1;
            if (colliderCount > MaximumExpandedTileColliders)
            {
                throw Diagnostic(new ArgumentException($"A tilemap is limited to {MaximumExpandedTileColliders} expanded tile collider descriptors.", nameof(tiles)), "SCN2D013");
            }
        }
        Tiles = Array.AsReadOnly(copied);
        PlacementImages = copied.Select(static tile => tile.Image).Distinct().ToArray();
        tileSets = Array.AsReadOnly(Array.Empty<TileSet2D>());
        chunks = Array.AsReadOnly(Array.Empty<TileChunk2D>());
        tileLookup = [];
        Version = version;
        Properties = TileMapModelCopy.CopyProperties(null);
        ExpandedColliderCount = colliderCount;
        IsFreePlacement = true;
    }

    public TileMap2DModel(
        string id,
        DrawSize tileSize,
        IEnumerable<TileSet2D> tileSets,
        IEnumerable<TileChunk2D> chunks,
        TileMapBounds2D? bounds = null,
        int order = 0,
        bool isVisible = true,
        DrawPoint offset = default,
        float opacity = 1,
        Color? tint = null,
        long version = 1,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        Id = ValidateId(id);
        if (!float.IsFinite(tileSize.Width) || tileSize.Width <= 0 ||
            !float.IsFinite(tileSize.Height) || tileSize.Height <= 0)
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(tileSize)), "SCN2D005");
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
        ArgumentNullException.ThrowIfNull(tileSets);
        ArgumentNullException.ThrowIfNull(chunks);
        if (bounds is TileMapBounds2D finite && (finite.Width <= 0 || finite.Height <= 0))
        {
            throw Diagnostic(new ArgumentException("Finite bounds must have positive dimensions.", nameof(bounds)), "SCN2D005");
        }
        TileSet2D[] copiedTileSets = CopyBounded(tileSets, MaximumLayers, nameof(tileSets));
        TileChunk2D[] copiedChunks = CopyBounded(chunks, MaximumChunks, nameof(chunks));
        if (copiedTileSets.Any(static tileSet => tileSet is null))
        {
            throw Diagnostic(new ArgumentException("A tilemap cannot contain null tilesets.", nameof(tileSets)), "SCN2D010");
        }
        if (copiedChunks.Any(static chunk => chunk is null))
        {
            throw Diagnostic(new ArgumentException("A tilemap cannot contain null chunks.", nameof(chunks)), "SCN2D005");
        }
        ValidateAggregateBudgets(copiedTileSets, copiedChunks);
        ValidateNoOverlaps(copiedChunks.Select(static chunk => new TileMapBounds2D(chunk.Origin.X, chunk.Origin.Y, chunk.Width, chunk.Height)).ToArray());
        ValidateUniqueIds(copiedTileSets);
        ExpandedColliderCount = ValidateCells(id, tileSize, copiedTileSets, copiedChunks, bounds, offset);

        TileSize = tileSize;
        this.tileSets = Array.AsReadOnly(copiedTileSets);
        this.chunks = Array.AsReadOnly(copiedChunks);
        tileLookup = copiedTileSets
            .SelectMany(static tileSet => tileSet.Tiles.Select(tile =>
                new KeyValuePair<int, ResolvedTile>(
                    tile.Id,
                    new ResolvedTile(tileSet, tile))))
            .ToDictionary(static pair => pair.Key, static pair => pair.Value);
        Bounds = bounds;
        Order = order;
        IsVisible = isVisible;
        Offset = offset;
        Opacity = opacity;
        Tint = tint ?? Color.White;
        Version = version;
        Properties = TileMapModelCopy.CopyProperties(properties);
    }

    public string Id { get; }

    public DrawSize TileSize { get; }

    public TileMapBounds2D? Bounds { get; }

    public IReadOnlyList<TileSet2D> TileSets => tileSets;

    public IReadOnlyList<TileChunk2D> Chunks => chunks;

    public IReadOnlyList<Tile> Tiles { get; } = Array.Empty<Tile>();

    public int Order { get; }

    public bool IsVisible { get; } = true;

    public DrawPoint Offset { get; }

    public float Opacity { get; } = 1;

    public Color Tint { get; } = Color.White;

    public long Version { get; }

    public IReadOnlyDictionary<string, object?> Properties { get; }

    internal IReadOnlyList<ImageReference> PlacementImages { get; } = Array.Empty<ImageReference>();

    internal bool IsFreePlacement { get; }

    internal long ExpandedColliderCount { get; }

    internal int TileDefinitionCount => tileLookup.Count;

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

    internal static void ValidateUniqueIds(
        IReadOnlyList<TileSet2D> tileSets)
    {
        HashSet<string> setIds = new(StringComparer.Ordinal);
        string? duplicateTileset = null;
        // Preserve first-occurrence diagnostic order and report duplicate set
        // identities before checking tile identities, as before.
        for (int index = tileSets.Count - 1; index >= 0; index--)
        {
            if (!setIds.Add(tileSets[index].Id)) { duplicateTileset = tileSets[index].Id; }
        }
        if (duplicateTileset is not null)
        {
            throw Diagnostic(new ArgumentException($"Tileset id '{duplicateTileset}' is duplicated.", nameof(tileSets)), "SCN2D015");
        }

        HashSet<int> tileIds = [];
        int duplicateTile = 0;
        for (int setIndex = tileSets.Count - 1; setIndex >= 0; setIndex--)
        {
            IReadOnlyList<TileDefinition2D> definitions = tileSets[setIndex].Tiles;
            for (int tileIndex = definitions.Count - 1; tileIndex >= 0; tileIndex--)
            {
                if (!tileIds.Add(definitions[tileIndex].Id)) { duplicateTile = definitions[tileIndex].Id; }
            }
        }
        if (duplicateTile != 0)
        {
            throw Diagnostic(new ArgumentException($"Tile id {duplicateTile} is defined by multiple tilesets.", nameof(tileSets)), "SCN2D015");
        }

    }

    private static string ValidateId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw Diagnostic(new ArgumentException("Map id cannot be empty.", nameof(id)), "SCN2D015");
        }
        return id;
    }

    private static void ValidateAggregateBudgets(IReadOnlyList<TileSet2D> tileSets, IReadOnlyList<TileChunk2D> chunks)
    {
        long definitions = 0, cells = 0;
        foreach (TileSet2D set in tileSets)
        {
            definitions += set.Tiles.Count;
            if (definitions > MaximumCells)
            {
                throw Diagnostic(new ArgumentException("A tilemap exceeds its total tile definition budget.", nameof(tileSets)), "SCN2D013");
            }
        }
        foreach (TileChunk2D chunk in chunks)
        {
            cells += chunk.Tiles.Count;
            if (cells > MaximumCells)
            {
                throw Diagnostic(new ArgumentException("A tilemap exceeds its total cell budget.", nameof(chunks)), "SCN2D013");
            }
        }
    }

    private static long ValidateCells(
        string id,
        DrawSize tileSize,
        IReadOnlyList<TileSet2D> tileSets,
        IReadOnlyList<TileChunk2D> chunks,
        TileMapBounds2D? bounds,
        DrawPoint offset)
    {
        Dictionary<int, TileDefinition2D> definitions = tileSets
            .SelectMany(static tileSet => tileSet.Tiles)
            .ToDictionary(static tile => tile.Id);
        long colliderInstances = 0;
        foreach (TileChunk2D chunk in chunks)
        {
            ValidateChunkGeometry(tileSize, chunk, offset);
            if (bounds is TileMapBounds2D finite &&
                (!finite.Contains(chunk.Origin) ||
                 checked(chunk.Origin.X + chunk.Width) > finite.Right ||
                 checked(chunk.Origin.Y + chunk.Height) > finite.Bottom))
            {
                throw Diagnostic(new ArgumentException(
                    $"Chunk ({chunk.Origin.X},{chunk.Origin.Y}) in map '{id}' exceeds finite map bounds.",
                    nameof(chunks)), "SCN2D005");
            }

            for (int index = 0; index < chunk.Tiles.Count; index++)
            {
                TileCell2D cell = chunk.Tiles[index];
                if (cell.TileId == 0) { continue; }
                if (!definitions.TryGetValue(cell.TileId, out TileDefinition2D? definition))
                {
                    throw Diagnostic(new ArgumentException(
                        $"Tile id {cell.TileId} in map '{id}' has no tileset definition.",
                        nameof(chunks)), "SCN2D006");
                }
                colliderInstances += definition.Collider is null ? 0 : 1;
                if (colliderInstances > MaximumExpandedTileColliders)
                {
                    throw Diagnostic(new ArgumentException($"A tilemap is limited to {MaximumExpandedTileColliders} expanded tile collider descriptors before coalescing.", nameof(chunks)), "SCN2D013");
                }
                if (definition.Collider is TileColliderDescriptor2D collider)
                {
                    Matrix3x2 placement = TileFlipGeometry2D.Transform(cell.Flip, tileSize) * Matrix3x2.CreateTranslation(
                        (chunk.Origin.X + index % chunk.Width) * tileSize.Width + offset.X,
                        (chunk.Origin.Y + index / chunk.Width) * tileSize.Height + offset.Y);
                    collider.ValidateGeometry(placement);
                }
            }
        }
        return colliderInstances;
    }

    internal static void ValidateNoOverlaps(IReadOnlyList<TileMapBounds2D> chunks)
    {
        if (chunks.Count < 2) { return; }
        // Sweep exclusive X bounds. Until the first overlap, active Y intervals
        // are disjoint, so only the immediate predecessor/successor can overlap.
        // Integer comparisons preserve negative and remote chunk coordinates.
        int[] ordered = Enumerable.Range(0, chunks.Count)
            .OrderBy(index => chunks[index].X).ThenBy(index => index).ToArray();
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
            TileMapBounds2D chunk = chunks[index];
            while (ending.TryPeek(out ChunkInterval? expired, out int right) && right <= chunk.X)
            {
                ending.Dequeue();
                active.Remove(expired);
            }
            ChunkInterval probe = new(chunk.Y, int.MaxValue, 0);
            ChunkInterval? before = active.GetViewBetween(minimum, probe).Max;
            ChunkInterval? after = active.GetViewBetween(probe, maximum).Min;
            ChunkInterval? overlap = before is not null && before.Bottom > chunk.Y ? before
                : after is not null && after.Top < chunk.Bottom ? after : null;
            if (overlap is not null)
            {
                TileMapBounds2D other = chunks[overlap.Index];
                throw Diagnostic(new ArgumentException(
                    $"Chunks at ({other.X},{other.Y}) and ({chunk.X},{chunk.Y}) overlap.",
                    nameof(chunks)), "SCN2D011");
            }
            ChunkInterval interval = new(chunk.Y, index, chunk.Bottom);
            active.Add(interval);
            ending.Enqueue(interval, chunk.Right);
        }
    }

    private sealed record ChunkInterval(int Top, int Index, int Bottom);
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
