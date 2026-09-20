using System.Numerics;
using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

public sealed partial class TileMap2D
{
    private readonly Dictionary<TileChunkCacheKey, TileCollisionChunkState> collisionChunks = [];

    private void SynchronizeCollisionAdapters()
    {
        List<TileCollisionChunkState> obsolete = [];
        foreach ((TileChunkCacheKey key, TileCollisionChunkState state) in collisionChunks.ToArray())
        {
            if (!requiredDataKeys.Contains(key) || !residentData.TryGetValue(key, out ResidentChunk? resident) ||
                !ReferenceEquals(resident.ValidatedCatalog, Catalog) || !ReferenceEquals(state.Resident, resident))
            {
                collisionChunks.Remove(key);
                obsolete.Add(state);
            }
        }
        List<Exception>? failures = null;
        foreach (TileCollisionChunkState state in obsolete)
        {
            try { RemoveCollisionChunk(state); }
            catch (Exception failure) { (failures ??= []).Add(failure); }
        }
        foreach (TileChunkCacheKey key in requiredDataKeys.ToArray())
        {
            if (residentData.TryGetValue(key, out ResidentChunk? resident))
            {
                try { SynchronizeCollisionChunk(key, resident); }
                catch (Exception failure) { (failures ??= []).Add(failure); }
            }
        }
        if (failures is not null) { throw new AggregateException(failures); }
    }

    private void SynchronizeCollisionChunk(TileChunkCacheKey key, ResidentChunk resident)
    {
        TileMapCatalog2D? catalog = Catalog;
        if (catalog is null || !requiredDataKeys.Contains(key) || resident.Info.ExpandedColliderCount == 0 ||
            !ReferenceEquals(resident.ValidatedCatalog, catalog) ||
            !residentData.TryGetValue(key, out ResidentChunk? current) || !ReferenceEquals(current, resident)) { return; }
        if (collisionChunks.TryGetValue(key, out TileCollisionChunkState? existing) && existing.IsCurrent(catalog, resident)) { return; }
        if (existing is not null) { collisionChunks.Remove(key); RemoveCollisionChunk(existing); }
        TileCollisionChunkState built = BuildCollisionChunk(catalog, resident);
        if (!ReferenceEquals(Catalog, catalog) || !residentData.TryGetValue(key, out current) || !ReferenceEquals(current, resident))
        {
            RemoveCollisionChunk(built);
            return;
        }
        collisionChunks[key] = built;
        SceneGeometry2D.FindRootScene(this)?.NotifyCollisionMutation(this, SceneCollisionMutationKind.Structure);
    }

    private TileCollisionChunkState BuildCollisionChunk(TileMapCatalog2D catalog, ResidentChunk resident)
    {
        TileMapChunkData2D data = resident.Payload.Value;
        List<TileStaticCollider2D> colliders = [];
        Dictionary<int, TileColliderDescriptor2D?> dependencies = [];
        try
        {
            if (data.Grid is not TileChunk2D chunk)
            {
                foreach (Tile tile in data.Placements)
                {
                    if (tile.Collider is { } descriptor)
                    {
                        AddCollider(new(descriptor, this, Matrix3x2.CreateTranslation(tile.X, tile.Y)));
                    }
                }
            }
            else
            {
                for (int localY = 0; localY < chunk.Height; localY++)
                for (int localX = 0; localX < chunk.Width;)
                {
                    int index = localY * chunk.Width + localX;
                    TileCoordinate2D coordinate = TileFlipGeometry2D.GetCellCoordinate(chunk, index);
                    TileCell2D cell = chunk.Tiles[index];
                    if (cell.TileId == 0) { localX++; continue; }
                    data.TryResolveTile(cell.TileId, out _, out TileDefinition2D? definition);
                    TileColliderDescriptor2D? descriptor = definition?.Collider;
                    dependencies.TryAdd(cell.TileId, descriptor);
                    if (descriptor is null) { localX++; continue; }
                    if (IsFullCellBox(descriptor, catalog.TileSize))
                    {
                        int run = 1;
                        while (localX + run < chunk.Width &&
                            TryGetMatchingFullCellBox(data, catalog.TileSize, chunk, localX + run, localY, descriptor, out TileDefinition2D? next))
                        {
                            dependencies.TryAdd(next!.Id, next.Collider);
                            run++;
                        }
                        AddCollider(new(descriptor, this,
                            Matrix3x2.CreateTranslation(coordinate.X * catalog.TileSize.Width, coordinate.Y * catalog.TileSize.Height),
                            boxWidth: catalog.TileSize.Width * run, boxHeight: catalog.TileSize.Height));
                        localX += run;
                    }
                    else
                    {
                        AddCollider(new(descriptor, this,
                            TileFlipGeometry2D.GetCellTransform(coordinate, cell.Flip, catalog.TileSize)));
                        localX++;
                    }
                }
            }
            return new(resident, catalog.TileSize, catalog.IsVisible, dependencies.ToArray(), colliders.ToArray());
        }
        catch (Exception failure)
        {
            try { RemoveCollisionNodes(colliders); }
            catch (Exception cleanup) { throw new AggregateException(failure, cleanup); }
            throw;
        }

        void AddCollider(TileStaticCollider2D collider)
        {
            colliders.Add(collider);
            collider.Enabled = catalog.IsVisible;
            LogicalChildren.InsertOwned(LogicalChildren.Count, collider);
            collider.AttachSurface(Surface);
        }
    }

    private static bool TryGetMatchingFullCellBox(TileMapChunkData2D data, DrawSize tileSize, TileChunk2D chunk, int x, int y,
        TileColliderDescriptor2D expected, out TileDefinition2D? definition)
    {
        TileCell2D cell = chunk.Tiles[y * chunk.Width + x];
        if (cell.TileId != 0 && data.TryResolveTile(cell.TileId, out _, out definition) &&
            definition?.Collider is TileColliderDescriptor2D collider &&
            IsFullCellBox(collider, tileSize) && TileColliderDescriptor2D.AreSemanticallyEqual(expected, collider)) { return true; }
        definition = null;
        return false;
    }

    private static bool IsFullCellBox(TileColliderDescriptor2D descriptor, DrawSize tileSize) =>
        descriptor.Shape == TileColliderShape2D.Box && descriptor.LocalTransform == Matrix3x2.Identity &&
        descriptor.OffsetX == 0 && descriptor.OffsetY == 0 && descriptor.Width == tileSize.Width && descriptor.Height == tileSize.Height;

    private void RemoveCollisionChunk(TileCollisionChunkState state) => RemoveCollisionNodes(state.Colliders);

    private void RemoveCollisionNodes(IEnumerable<TileStaticCollider2D> colliders)
    {
        List<Exception>? failures = null;
        foreach (TileStaticCollider2D collider in colliders)
        {
            try { collider.AttachSurface(null); }
            catch (Exception failure) { (failures ??= []).Add(failure); }
            try { if (ReferenceEquals(collider.LogicalParent, this)) { LogicalChildren.RemoveOwned(collider); } }
            catch (Exception failure) { (failures ??= []).Add(failure); }
        }
        SceneGeometry2D.FindRootScene(this)?.NotifyCollisionMutation(this, SceneCollisionMutationKind.Structure);
        if (failures is not null) { throw new AggregateException(failures); }
    }

    private void ClearCollisionResidency()
    {
        TileCollisionChunkState[] obsolete = collisionChunks.Values.ToArray();
        collisionChunks.Clear();
        List<Exception>? failures = null;
        foreach (TileCollisionChunkState state in obsolete)
        {
            try { RemoveCollisionChunk(state); }
            catch (Exception failure) { (failures ??= []).Add(failure); }
        }
        if (failures is not null) { throw new AggregateException(failures); }
    }

    private sealed class TileCollisionChunkState(ResidentChunk resident, DrawSize tileSize, bool visible,
        KeyValuePair<int, TileColliderDescriptor2D?>[] dependencies, TileStaticCollider2D[] colliders)
    {
        internal ResidentChunk Resident { get; } = resident;
        internal TileStaticCollider2D[] Colliders { get; } = colliders;
        internal bool IsCurrent(TileMapCatalog2D catalog, ResidentChunk current)
        {
            if (!ReferenceEquals(Resident, current) || tileSize != catalog.TileSize || visible != catalog.IsVisible) { return false; }
            foreach ((int id, TileColliderDescriptor2D? descriptor) in dependencies)
            {
                if (!current.Payload.Value.TryResolveTile(id, out _, out TileDefinition2D? definition) ||
                    !TileColliderDescriptor2D.AreSemanticallyEqual(descriptor, definition?.Collider)) { return false; }
            }
            return true;
        }
    }
}
