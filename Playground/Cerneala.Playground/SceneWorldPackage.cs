using Cerneala.Drawing;
using Cerneala.Scene2D.Packages;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;

namespace Cerneala.Playground;

// Application composition, not a general mutable-map API. Only the two edits
// already offered by this demo are supported: promote the door and toggle Plant.
internal sealed class SceneWorldPackage : IDisposable
{
    private readonly Scene2DPackage package;
    private readonly CellPublication plant;

    private SceneWorldPackage(Scene2DPackage package, Scene2DPackageLevel level,
        DrawPoint spawn, string spawnState, bool doorClosed)
    {
        this.package = package;
        Spawn = spawn;
        SpawnState = spawnState;
        DoorClosed = doorClosed;
        plant = new(level, level.TileMaps.Single(map => map.Catalog.Id == "2"), new(11, 10), promote: false);
        CellPublication door = new(level, level.TileMaps.Single(map => map.Catalog.Id == "4"), new(14, 9), promote: true);
        TileMaps = Array.AsReadOnly(level.TileMaps.Select(map => map.Catalog.Id switch
        {
            "2" => plant.Source, "4" => door.Source, _ => map
        }).ToArray());
        Atlas = new(package.GetFilePath(package.Assets.Single(asset => asset.ResourceId.Key == "world-atlas.png").Path));
        Scene2DPackageEntityInfo[] walls = level.Entities.Where(entity => entity.Role == "Collider").ToArray();
        if (walls.Length != 6) { throw new InvalidDataException("The village requires its six authored wall regions."); }
        ColliderSource = new(walls.Select(info => new SceneSpatialEntry2D(info.Id, info.AuthoringBounds, info.CollisionBounds)),
            async (entry, token) =>
            {
                using var lease = await level.LoadEntityAsync(entry.Id, token).ConfigureAwait(false);
                Scene2DEntity entity = lease.Value;
                if (entity.Shape != "Box" || entity.Rotation != 0 || entity.Collider is not { Shape: TileColliderShape2D.Box } collider)
                { throw new InvalidDataException("This sample composes axis-aligned box walls only."); }
                // Copy the gameplay fields, not the authored Properties/vertices.
                return new SceneSpatialLease2D<object>(new SceneWorldBox(entity.Position.X, entity.Position.Y,
                    entity.Size.Width, entity.Size.Height, collider.CollisionLayer, collider.CollisionMask));
            });
    }

    internal IReadOnlyList<TileMapSource2D> TileMaps { get; }
    internal SceneSpatialSource2D<object> ColliderSource { get; }
    internal ImageResource Atlas { get; }
    internal DrawPoint Spawn { get; }
    internal string SpawnState { get; }
    internal bool DoorClosed { get; }

    internal static async Task<SceneWorldPackage> OpenAsync(string path, CancellationToken token)
    {
        Scene2DPackage package = await Scene2DPackage.OpenAsync(path, cancellationToken: token).ConfigureAwait(false);
        try
        {
            Scene2DPackageLevel level = package.Levels.Single();
            if (level.WorldOffset != default || level.TileMaps.Any(map => map.Catalog.Offset != default))
            { throw new InvalidDataException("The authored village composition uses a zero-offset level and layers."); }
            if (level.PromotionCells.Single() != new TileCellKey2D("4", 14, 9))
            { throw new InvalidDataException("The authored door declaration requires cell (4,14,9)."); }
            using var spawn = await level.LoadEntityAsync(level.Entities.Single(info => info.Role == "Spawn").Id, token).ConfigureAwait(false);
            using var door = await level.LoadPromotionAsync(level.PromotionCells[0], token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return new(package, level, spawn.Value.Position, (string)spawn.Value.Properties["InitialState"]!,
                Equals(door.Value.Properties["InitialState"], "Closed"));
        }
        catch { package.Dispose(); throw; }
    }

    internal Task<CellPublication.PreparedEdit> PreparePlantAsync(CancellationToken token) => plant.PrepareAsync(token);
    public void Dispose() => package.Dispose();

    internal sealed class CellPublication
    {
        private readonly Scene2DPackageLevel level;
        private readonly TileMapSource2D original;
        private readonly TileCoordinate2D cell;
        private readonly TileMapChunkInfo2D originalChunk;
        private readonly bool promote;
        private TileMapChunkData2D? publishingPayload;

        internal CellPublication(Scene2DPackageLevel level, TileMapSource2D original, TileCoordinate2D cell, bool promote)
        {
            this.level = level;
            this.original = original;
            this.cell = cell;
            this.promote = promote;
            originalChunk = original.Catalog.Chunks.Single(info => info.Cells!.Value.Contains(cell));
            if (originalChunk.ExpandedColliderCount != 0)
            { throw new InvalidDataException("Village cell edits must not replace authored collision geometry."); }
            Source = new(original.Catalog, LoadAsync);
            if (promote) { Source.SetCatalog(NextCatalog(Source.Catalog)); }
        }

        internal TileMapSource2D Source { get; }

        private TileMapCatalog2D NextCatalog(TileMapCatalog2D before)
        {
            TileMapChunkInfo2D current = before.Chunks.Single(info => info.Spatial.Id == originalChunk.Spatial.Id);
            SceneSpatialEntry2D spatial = new(current.Spatial.Id, current.Spatial.Bounds,
                current.Spatial.CollisionBounds, version: checked(current.Spatial.Version + 1));
            TileMapChunkInfo2D replacement = new(spatial, current.Cells!.Value,
                promote ? current.TileIds : current.TileIds.Concat([15]).Distinct(), current.Images,
                current.ExpandedColliderCount, dataResidencyBytes: null);
            // The edited payload is reconstructed, not retained in an in-memory
            // backing map. Its opaque metadata charge is unknown, so it cannot
            // consume optional warm residency under an invented zero charge.
            return new(before.Id, before.Chunks.Select(info => ReferenceEquals(info, current) ? replacement : info),
                before.TileSize, before.Bounds, before.Order, before.IsVisible, before.Offset, before.Opacity,
                before.Tint, checked(before.Version + 1), before.ImageSizes);
        }

        internal async Task<PreparedEdit> PrepareAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            TileMapCatalog2D before = Source.Catalog;
            TileMapCatalog2D next = NextCatalog(before);
            TileMapChunkInfo2D info = next.Chunks.Single(info => info.Spatial.Id == originalChunk.Spatial.Id);
            SceneSpatialLease2D<TileMapChunkData2D> acquired = await RebuildAsync(next, info, token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                return new(this, before, next, acquired);
            }
            catch { acquired.Dispose(); throw; }
        }

        internal sealed class PreparedEdit(CellPublication owner, TileMapCatalog2D before, TileMapCatalog2D next,
            SceneSpatialLease2D<TileMapChunkData2D> payload) : IDisposable
        {
            internal void Publish()
            {
                TileMapChunkData2D data = payload.Value;
                if (!ReferenceEquals(owner.Source.Catalog, before))
                    throw new OperationCanceledException("The prepared cell revision was superseded.");
                // UI-owned Changed subscribers reconcile their current interests
                // synchronously. Share this managed payload only during publication;
                // their returned leases, not this source, own subsequent residency.
                Volatile.Write(ref owner.publishingPayload, data);
                try { owner.Source.SetCatalog(next); }
                finally { Volatile.Write(ref owner.publishingPayload, null); }
            }

            public void Dispose() => payload.Dispose();
        }

        private ValueTask<SceneSpatialLease2D<TileMapChunkData2D>> LoadAsync(
            TileMapCatalog2D catalog, TileMapChunkInfo2D info, CancellationToken token)
        {
            TileMapChunkData2D? prepared = Volatile.Read(ref publishingPayload);
            if (info.Spatial.Id == originalChunk.Spatial.Id && prepared?.Grid?.Version == info.Spatial.Version)
                return ValueTask.FromResult(new SceneSpatialLease2D<TileMapChunkData2D>(prepared));
            return RebuildAsync(catalog, info, token);
        }

        private async ValueTask<SceneSpatialLease2D<TileMapChunkData2D>> RebuildAsync(
            TileMapCatalog2D catalog, TileMapChunkInfo2D info, CancellationToken token)
        {
            if (info.Spatial.Id != originalChunk.Spatial.Id || info.Spatial.Version == originalChunk.Spatial.Version)
            { return await original.LoadAsync(info.Spatial, token).ConfigureAwait(false); }
            using var acquired = await original.LoadAsync(originalChunk.Spatial, token).ConfigureAwait(false);
            TileChunk2D before = acquired.Value.Grid!;
            TileCell2D[] cells = before.Tiles.ToArray();
            int index = (cell.Y - before.Origin.Y) * before.Width + cell.X - before.Origin.X;
            if (!promote && cells[index].TileId != 0)
            { throw new InvalidDataException("The authored decorative Plant cell must start empty."); }
            // The revision completely describes this demo's alternating delta.
            // In-flight old revisions do not consult a mutable 'current tile'.
            cells[index] = new(promote ? 0 : (info.Spatial.Version - originalChunk.Spatial.Version) % 2 == 1 ? 15 : 0);
            TileChunk2D grid = new(before.Origin, before.Width, before.Height, cells, info.Spatial.Version, before.Properties);
            if (promote) { return new(new(grid, UsedPalette(acquired.Value.TileSets, cells))); }
            // Tile 15 need not occur in the original chunk. Acquire the authoring
            // palette explicitly, then retain only definitions used by this lease.
            using var metadata = await level.LoadMapMetadataAsync(catalog.Id, token).ConfigureAwait(false);
            TileSet2D[] palette = UsedPalette(metadata.Value.TileSets, cells);
            if (palette.Any(set => set.Tiles.Any(tile => tile.Collider is not null)))
            { throw new InvalidDataException("Plant must preserve the village's separate collision owners."); }
            token.ThrowIfCancellationRequested();
            return new(new(grid, palette));
        }

        private static TileSet2D[] UsedPalette(IReadOnlyList<TileSet2D> palette, TileCell2D[] cells)
        {
            HashSet<int> used = cells.Where(value => value.TileId != 0).Select(value => value.TileId).ToHashSet();
            return palette.Where(set => set.Tiles.Any(tile => used.Contains(tile.Id)))
                .Select(set => new TileSet2D(set.Id, set.AtlasResourceId, set.Tiles.Where(tile => used.Contains(tile.Id)),
                    set.Version, set.Properties)).ToArray();
        }
    }
}
