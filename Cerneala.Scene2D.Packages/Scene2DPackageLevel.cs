using Cerneala.Drawing;
using Cerneala.UI.Controls;

namespace Cerneala.Scene2D.Packages;

/// <summary>Resident level headers with explicit acquisition of maps and nonspatial authoring data.</summary>
public sealed partial class Scene2DPackageLevel
{
    private readonly Scene2DPackage package;
    private readonly PackageLevel index;
    private readonly Dictionary<string, PackageMap> maps;
    private readonly Dictionary<string, PackageBlock> mapMetadata;
    private readonly Dictionary<string, PackageEntity> entities;
    private readonly Dictionary<TileCellKey2D, PackageBlock> promotions;

    internal Scene2DPackageLevel(Scene2DPackage package, PackageLevel index)
    {
        this.package = package;
        this.index = index;
        maps = index.Maps.ToDictionary(static map => map.Catalog.Id, StringComparer.Ordinal);
        mapMetadata = index.Maps.ToDictionary(static map => map.Catalog.Id, static map => map.Metadata, StringComparer.Ordinal);
        entities = index.Entities.ToDictionary(static entity => entity.Id, StringComparer.Ordinal);
        promotions = index.Promotions.ToDictionary(static promotion => promotion.Cell, static promotion => promotion.Data);
        TileMapIds = Array.AsReadOnly(index.Maps.Select(static map => map.Catalog.Id).ToArray());
        EntityIds = Array.AsReadOnly(index.Entities.Select(static entity => entity.Id).ToArray());
        Entities = Array.AsReadOnly(index.Entities.Select(static entity => entity.Info).ToArray());
        PromotionCells = Array.AsReadOnly(index.Promotions.Select(static promotion => promotion.Cell).ToArray());
    }

    public string Id => index.Id;
    public DrawPoint WorldOffset => index.WorldOffset;
    public DrawSize? TileSize => index.TileSize;
    public TileMapBounds2D? Bounds => index.Bounds;
    public IReadOnlyList<string> TileMapIds { get; }
    public IReadOnlyList<string> EntityIds { get; }
    public IReadOnlyList<Scene2DPackageEntityInfo> Entities { get; }
    public IReadOnlyList<TileCellKey2D> PromotionCells { get; }

    public ValueTask<Scene2DPackageMetadata> LoadMetadataAsync(CancellationToken cancellationToken = default) =>
        package.LoadAsync<Scene2DPackageMetadata>(index.Metadata, cancellationToken);

    public ValueTask<Scene2DPackageMetadata> LoadMapMetadataAsync(string mapId, CancellationToken cancellationToken = default) =>
        package.LoadAsync<Scene2DPackageMetadata>(mapMetadata[mapId], cancellationToken);

    public TileMap2D CreateTileMap(string mapId)
    {
        package.ThrowIfDisposed();
        return TileMap2D.FromSource(CreateSource(maps[mapId]));
    }

    public async ValueTask<Scene2DEntity> LoadEntityAsync(string id, CancellationToken cancellationToken = default)
    {
        PackageEntity entity = entities[id];
        Scene2DEntity value = await package.LoadAsync<Scene2DEntity>(entity.Data, cancellationToken).ConfigureAwait(false);
        if (!entity.Info.Matches(value)) { throw new InvalidDataException("Entity payload geometry or identity differs from its catalog."); }
        return value;
    }

    public async ValueTask<TilePromotion2D> LoadPromotionAsync(TileCellKey2D cell, CancellationToken cancellationToken = default)
    {
        TilePromotion2D value = await package.LoadAsync<TilePromotion2D>(promotions[cell], cancellationToken).ConfigureAwait(false);
        if (value.Cell != cell) { throw new InvalidDataException("Promotion payload identity differs from its catalog."); }
        return value;
    }

    private TileMapSource2D CreateSource(PackageMap map)
    {
        Dictionary<string, (long Version, PackageBlock Block)> chunks = new(StringComparer.Ordinal);
        for (int index = 0; index < map.Chunks.Length; index++)
        {
            SceneSpatialEntry2D entry = map.Catalog.Chunks[index].Spatial;
            chunks.Add(entry.Id, (entry.Version, map.Chunks[index]));
        }
        return new(map.Catalog, async (catalog, info, token) =>
        {
            if (!ReferenceEquals(catalog, map.Catalog) || !chunks.TryGetValue(info.Spatial.Id, out var entry) || entry.Version != info.Spatial.Version)
            { throw new InvalidOperationException("A prepared package source cannot load a replacement catalog. Publish a new application-owned source."); }
            TileMapChunkData2D value = await package.LoadAsync<TileMapChunkData2D>(entry.Block, token).ConfigureAwait(false);
            return new SceneSpatialLease2D<TileMapChunkData2D>(value);
        });
    }
}
