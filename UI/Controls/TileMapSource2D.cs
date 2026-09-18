using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Resources;
using static Cerneala.UI.Controls.Scene2DModelValidator;

namespace Cerneala.UI.Controls;

/// <summary>An immutable acquisition payload containing either grid cells or ordered free placements.</summary>
public sealed class TileMapChunkData2D
{
    private readonly Dictionary<int, (TileSet2D Set, TileDefinition2D Tile)> tileLookup = [];
    private readonly Dictionary<string, ImageReference> images = new(StringComparer.Ordinal);

    public TileMapChunkData2D(TileChunk2D grid, IEnumerable<TileSet2D> tileSets)
    {
        ArgumentNullException.ThrowIfNull(grid);
        TileSet2D[] sets = CopyBounded(tileSets, MaximumLayers, nameof(tileSets));
        foreach (TileSet2D set in sets)
        {
            if (set is null || set.Tiles.Count == 0)
            {
                throw new ArgumentException("A chunk palette requires non-null, nonempty tilesets.", nameof(tileSets));
            }
        }
        TileMap2DModel.ValidateUniqueIds(sets);
        HashSet<int> used = [];
        foreach (TileCell2D cell in grid.Tiles) { if (cell.TileId != 0) { used.Add(cell.TileId); } }
        foreach (TileSet2D set in sets)
        {
            images.Add(set.Id, new ImageReference(set.AtlasResourceId));
            foreach (TileDefinition2D tile in set.Tiles)
            {
                if (!used.Contains(tile.Id)) { throw new ArgumentException("A chunk palette may only contain used definitions.", nameof(tileSets)); }
                tileLookup.Add(tile.Id, (set, tile));
            }
        }
        if (used.Count != tileLookup.Count) { throw new ArgumentException("Every nonempty cell requires a loaded definition.", nameof(tileSets)); }
        Grid = grid;
        TileSets = Array.AsReadOnly(sets);
    }

    public TileMapChunkData2D(IEnumerable<Tile> placements)
    {
        Tile[] copied = CopyBounded(placements, MaximumCells, nameof(placements));
        if (copied.Any(static tile => tile is null)) { throw new ArgumentException("Placements cannot contain null.", nameof(placements)); }
        Placements = Array.AsReadOnly(copied);
    }

    public TileChunk2D? Grid { get; }
    public IReadOnlyList<Tile> Placements { get; } = Array.Empty<Tile>();
    public IReadOnlyList<TileSet2D> TileSets { get; } = Array.Empty<TileSet2D>();

    public bool TryResolveTile(int tileId, out TileSet2D? tileSet, out TileDefinition2D? definition)
    {
        bool found = tileLookup.TryGetValue(tileId, out var resolved);
        tileSet = resolved.Set;
        definition = resolved.Tile;
        return found;
    }

    internal ImageReference GetTileSetImage(string id) => images[id];
    internal IEnumerable<ImageReference> Images => images.Values;
}

/// <summary>Publishes metadata atomically and validates asynchronously acquired static chunks.</summary>
public sealed class TileMapSource2D : ISceneSpatialSource2D<TileMapChunkData2D>
{
    private readonly Func<TileMapCatalog2D, TileMapChunkInfo2D, CancellationToken, ValueTask<SceneSpatialLease2D<TileMapChunkData2D>>> load;
    private TileMapCatalog2D catalog;

    public TileMapSource2D(TileMapCatalog2D catalog,
        Func<TileMapCatalog2D, TileMapChunkInfo2D, CancellationToken, ValueTask<SceneSpatialLease2D<TileMapChunkData2D>>> load)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(load);
        this.catalog = catalog;
        this.load = load;
    }

    public TileMapCatalog2D Catalog => Volatile.Read(ref catalog);
    public IReadOnlyList<SceneSpatialEntry2D> Entries => Catalog.Entries;
    public event EventHandler? Changed;

    public void SetCatalog(TileMapCatalog2D replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        Interlocked.Exchange(ref catalog, replacement);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async ValueTask<SceneSpatialLease2D<TileMapChunkData2D>> LoadAsync(
        SceneSpatialEntry2D entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();
        TileMapCatalog2D snapshot = Catalog;
        if (!snapshot.TryGetChunk(entry.Id, out TileMapChunkInfo2D? info) || info!.Spatial.Version != entry.Version)
        {
            throw new InvalidOperationException("The requested chunk is not part of the current source revision.");
        }
        SceneSpatialLease2D<TileMapChunkData2D>? lease = await load(snapshot, info, cancellationToken).ConfigureAwait(false);
        try
        {
            if (lease is null) { throw new InvalidOperationException("A tile loader returned a null lease."); }
            // A loader may finish after cancellation. Hand a valid acquisition
            // back to its requester: residency owns retirement of late results
            // and must be able to observe any failure from that retirement.
            snapshot.ValidatePayload(info, lease.Value);
            return lease;
        }
        catch (Exception failure)
        {
            try { lease?.Dispose(); }
            catch (Exception releaseFailure) { throw new AggregateException(failure, releaseFailure); }
            throw;
        }
    }

    public static TileMapSource2D FromModel(TileMap2DModel model, IReadOnlyDictionary<string, DrawSize>? imageSizes = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        List<TileMapChunkInfo2D> infos = [];
        Dictionary<string, TileMapChunkData2D> payloads = new(StringComparer.Ordinal);
        if (model.IsFreePlacement)
        {
            // Consecutive ranges preserve painter order even when their visual
            // bounds overlap. This adapter remains an in-memory backing store.
            const int placementChunkSize = 256;
            for (int start = 0; start < model.Tiles.Count; start += placementChunkSize)
            {
                TileMapChunkData2D data = new(model.Tiles.Skip(start).Take(placementChunkSize));
                DrawRect? visual = null, collision = null;
                int colliders = 0;
                foreach (Tile tile in data.Placements)
                {
                    DrawSize size = default;
                    bool known = tile.Image.DirectImage is IDrawImage;
                    if (tile.Image.DirectImage is IDrawImage direct) { size = new(direct.Width, direct.Height); }
                    else if (imageSizes is not null) { known = imageSizes.TryGetValue(tile.Image.ResourceId!.Value.Key, out size); }
                    if (!known && (float.IsNaN(tile.Width) || float.IsNaN(tile.Height)))
                    {
                        throw new ArgumentException($"Natural-size placement requires image metadata for '{tile.Image.ResourceId?.Key}'.", nameof(imageSizes));
                    }
                    visual = TileMapCatalog2D.Union(visual, tile.GetDestination(size));
                    if (tile.Collider is not TileColliderDescriptor2D descriptor) { continue; }
                    colliders++;
                    collision = TileMapCatalog2D.Union(collision,
                        SceneGeometry2D.GetColliderBounds(descriptor, Matrix3x2.CreateTranslation(tile.X, tile.Y)));
                }
                string id = FormattableString.Invariant($"placements:{start}");
                infos.Add(new(new SceneSpatialEntry2D(id, visual!.Value, collision, version: model.Version),
                    data.Placements.Count, data.Placements.Select(static tile => tile.Image).Distinct(), colliders,
                    dataResidencyBytes: 0));
                payloads.Add(id, data);
            }
        }
        else
        {
            foreach (TileChunk2D chunk in model.Chunks)
            {
                DrawRect? collision = null;
                int colliders = 0;
                HashSet<int> ids = [];
                Dictionary<TileSet2D, List<TileDefinition2D>> usedSets = [];
                for (int index = 0; index < chunk.Tiles.Count; index++)
                {
                    TileCell2D cell = chunk.Tiles[index];
                    if (cell.TileId == 0) { continue; }
                    model.TryResolveTile(cell.TileId, out TileSet2D? set, out TileDefinition2D? definition);
                    if (ids.Add(cell.TileId))
                    {
                        if (!usedSets.TryGetValue(set!, out List<TileDefinition2D>? definitions))
                        {
                            definitions = [];
                            usedSets.Add(set!, definitions);
                        }
                        definitions.Add(definition!);
                    }
                    if (definition!.Collider is not TileColliderDescriptor2D descriptor) { continue; }
                    colliders++;
                    Matrix3x2 placement = TileFlipGeometry2D.Transform(cell.Flip, model.TileSize) * Matrix3x2.CreateTranslation(
                        (chunk.Origin.X + index % chunk.Width) * model.TileSize.Width,
                        (chunk.Origin.Y + index / chunk.Width) * model.TileSize.Height);
                    collision = TileMapCatalog2D.Union(collision, SceneGeometry2D.GetColliderBounds(descriptor, placement));
                }
                TileMapBounds2D cells = new(chunk.Origin.X, chunk.Origin.Y, chunk.Width, chunk.Height);
                string id = FormattableString.Invariant($"grid:{cells.X}:{cells.Y}:{cells.Width}:{cells.Height}");
                List<TileSet2D> palette = [];
                foreach ((TileSet2D set, List<TileDefinition2D> definitions) in usedSets)
                {
                    palette.Add(new(set.Id, set.AtlasResourceId, definitions, set.Version, set.Properties));
                }
                TileMapChunkData2D data = new(chunk, palette);
                infos.Add(new(new SceneSpatialEntry2D(id, TileMapCatalog2D.GetGridBounds(model.TileSize, cells), collision, version: chunk.Version),
                    cells, ids, data.Images.Distinct(), colliders, dataResidencyBytes: 0));
                payloads.Add(id, data);
            }
        }
        TileMapCatalog2D snapshot = new(model.Id, infos, model.IsFreePlacement ? null : model.TileSize,
            model.Bounds, model.Order, model.IsVisible, model.Offset, model.Opacity, model.Tint, model.Version, imageSizes);
        // Every payload above belongs to this fixed backing dictionary, including
        // placement arrays and opaque properties. An acquisition does not create
        // or extend their residency; zero is not a claim that the source uses no RAM.
        return new(snapshot, (_, info, _) => ValueTask.FromResult(new SceneSpatialLease2D<TileMapChunkData2D>(payloads[info.Spatial.Id])));
    }
}
