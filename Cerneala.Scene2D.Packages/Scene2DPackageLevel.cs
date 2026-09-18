using Cerneala.Drawing;
using Cerneala.UI.Controls;

namespace Cerneala.Scene2D.Packages;

/// <summary>Resident level headers with explicit acquisition of maps and nonspatial authoring data.</summary>
public sealed class Scene2DPackageLevel
{
    private readonly Scene2DPackage package;
    private readonly PackageLevel index;
    private readonly Dictionary<string, PackageBlock> mapMetadata;
    private readonly Dictionary<string, PackageEntity> entities;
    private readonly Dictionary<TileCellKey2D, PackageBlock> promotions;

    internal Scene2DPackageLevel(Scene2DPackage package, PackageLevel index)
    {
        this.package = package;
        this.index = index;
        mapMetadata = index.Maps.ToDictionary(static map => map.Catalog.Id, static map => map.Metadata, StringComparer.Ordinal);
        entities = index.Entities.ToDictionary(static entity => entity.Id, StringComparer.Ordinal);
        promotions = index.Promotions.ToDictionary(static promotion => promotion.Cell, static promotion => promotion.Data);
        TileMaps = Array.AsReadOnly(index.Maps.Select(CreateSource).ToArray());
        EntityIds = Array.AsReadOnly(index.Entities.Select(static entity => entity.Id).ToArray());
        Entities = Array.AsReadOnly(index.Entities.Select(static entity => entity.Info).ToArray());
        PromotionCells = Array.AsReadOnly(index.Promotions.Select(static promotion => promotion.Cell).ToArray());
    }

    public string Id => index.Id;
    public DrawPoint WorldOffset => index.WorldOffset;
    public DrawSize? TileSize => index.TileSize;
    public TileMapBounds2D? Bounds => index.Bounds;
    public IReadOnlyList<TileMapSource2D> TileMaps { get; }
    public IReadOnlyList<string> EntityIds { get; }
    public IReadOnlyList<Scene2DPackageEntityInfo> Entities { get; }
    public IReadOnlyList<TileCellKey2D> PromotionCells { get; }

    public ValueTask<SceneSpatialLease2D<Scene2DPackageMetadata>> LoadMetadataAsync(CancellationToken cancellationToken = default) =>
        package.LoadAsync<Scene2DPackageMetadata>(index.Metadata, cancellationToken);

    public ValueTask<SceneSpatialLease2D<Scene2DPackageMetadata>> LoadMapMetadataAsync(string mapId, CancellationToken cancellationToken = default) =>
        package.LoadAsync<Scene2DPackageMetadata>(mapMetadata[mapId], cancellationToken);

    public async ValueTask<SceneSpatialLease2D<Scene2DEntity>> LoadEntityAsync(string id, CancellationToken cancellationToken = default)
    {
        PackageEntity entity = entities[id];
        SceneSpatialLease2D<Scene2DEntity> lease = await package.LoadAsync<Scene2DEntity>(entity.Data, cancellationToken).ConfigureAwait(false);
        try
        {
            if (!entity.Info.Matches(lease.Value)) { throw new InvalidDataException("Entity payload geometry or identity differs from its catalog."); }
            return lease;
        }
        catch { lease.Dispose(); throw; }
    }

    public SceneSpatialSource2D<object> CreateEntitySource(Func<Scene2DPackageEntityInfo, SceneSpatialEntry2D?> describe)
    {
        ArgumentNullException.ThrowIfNull(describe);
        List<SceneSpatialEntry2D> entries = [];
        foreach (Scene2DPackageEntityInfo entity in Entities)
        {
            SceneSpatialEntry2D? entry = describe(entity);
            if (entry is null) { continue; }
            if (entry.Id != entity.Id || entry.Version != 1)
            { throw new ArgumentException("An entity adapter must preserve the package identity and payload version 1.", nameof(describe)); }
            entries.Add(entry);
        }
        return new(entries, async (entry, cancellationToken) =>
        {
            if (entry.Version != 1 || !entities.ContainsKey(entry.Id))
            { throw new InvalidOperationException("This entity payload is not part of the prepared package."); }
            SceneSpatialLease2D<Scene2DEntity> acquired = await LoadEntityAsync(entry.Id, cancellationToken).ConfigureAwait(false);
            return new SceneSpatialLease2D<object>(acquired.Value, _ => acquired.Dispose());
        });
    }

    public async ValueTask<SceneSpatialLease2D<TilePromotion2D>> LoadPromotionAsync(TileCellKey2D cell, CancellationToken cancellationToken = default)
    {
        SceneSpatialLease2D<TilePromotion2D> lease = await package.LoadAsync<TilePromotion2D>(promotions[cell], cancellationToken).ConfigureAwait(false);
        if (lease.Value.Cell == cell) { return lease; }
        lease.Dispose();
        throw new InvalidDataException("Promotion payload identity differs from its catalog.");
    }

    private TileMapSource2D CreateSource(PackageMap map)
    {
        Dictionary<string, (long Version, PackageBlock Block)> chunks = new(StringComparer.Ordinal);
        for (int index = 0; index < map.Chunks.Length; index++)
        {
            SceneSpatialEntry2D entry = map.Catalog.Chunks[index].Spatial;
            chunks.Add(entry.Id, (entry.Version, map.Chunks[index]));
        }
        return new(map.Catalog, (catalog, info, token) =>
        {
            if (!ReferenceEquals(catalog, map.Catalog) || !chunks.TryGetValue(info.Spatial.Id, out var entry) || entry.Version != info.Spatial.Version)
            { throw new InvalidOperationException("A prepared package source cannot load a replacement catalog. Publish a new application-owned source."); }
            return package.LoadAsync<TileMapChunkData2D>(entry.Block, token);
        });
    }
}
