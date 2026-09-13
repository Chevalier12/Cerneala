using System.Numerics;
using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

public sealed partial class TileMap2D
{
    private readonly Dictionary<TileCollisionChunkKey, TileCollisionChunkState>
        collisionChunks = [];
    private TileMap2DModel? collisionPlacementModel;
    private readonly List<TileStaticCollider2D> placementColliders = [];

    internal void SynchronizeCollisionAdaptersAndNotify()
    {
        SynchronizeCollisionAdapters();
        SceneGeometry2D.FindRootScene(this)?.NotifyCollisionMutation(
            this,
            SceneCollisionMutationKind.Structure);
    }

    private void SynchronizeCollisionAdapters()
    {
        TileMap2DModel? model = Model;
        HashSet<TileCollisionChunkKey> current = new(model?.Chunks.Count ?? 0);
        SynchronizePlacementColliders(model);
        if (model is not null)
        {
            foreach (TileChunk2D chunk in model.Chunks)
            {
                TileCollisionChunkKey key = new(chunk.Origin, chunk.Width, chunk.Height);
                current.Add(key);
                if (collisionChunks.TryGetValue(key, out TileCollisionChunkState? existing) &&
                    existing.IsCurrent(model, chunk))
                {
                    continue;
                }

                if (existing is not null)
                {
                    RemoveCollisionChunk(existing);
                }

                collisionChunks[key] = BuildCollisionChunk(model, chunk);
            }
        }

        TileCollisionChunkKey[] stale = collisionChunks.Keys
            .Where(key => !current.Contains(key))
            .ToArray();
        foreach (TileCollisionChunkKey key in stale)
        {
            RemoveCollisionChunk(collisionChunks[key]);
            collisionChunks.Remove(key);
        }
    }

    private TileCollisionChunkState BuildCollisionChunk(
        TileMap2DModel model,
        TileChunk2D chunk)
    {
        List<TileStaticCollider2D> colliders = [];
        Dictionary<int, TileColliderDescriptor2D?> dependencies = new(
            Math.Min(chunk.Tiles.Count, model.TileDefinitionCount));
        for (int localY = 0; localY < chunk.Height; localY++)
        {
            for (int localX = 0; localX < chunk.Width;)
            {
                TileCoordinate2D coordinate = new(
                    chunk.Origin.X + localX,
                    chunk.Origin.Y + localY);
                TileCell2D cell = chunk.Tiles[(localY * chunk.Width) + localX];
                if (cell.TileId == 0)
                {
                    localX++;
                    continue;
                }

                model.TryResolveTile(cell.TileId, out _, out TileDefinition2D? definition);
                TileColliderDescriptor2D? descriptor = definition?.Collider;
                // Absence is a dependency too: adding a descriptor must invalidate
                // chunks that previously contributed no collider for this tile id.
                dependencies.TryAdd(cell.TileId, descriptor);
                if (descriptor is null)
                {
                    localX++;
                    continue;
                }

                TileColliderDescriptor2D? coalescible = IsFullCellBox(descriptor, model.TileSize)
                        ? descriptor
                        : null;
                if (coalescible is not null)
                {
                    int run = 1;
                    while (localX + run < chunk.Width &&
                        TryGetMatchingFullCellBox(
                            model,
                            chunk,
                            localX + run,
                            localY,
                            coalescible,
                            out TileDefinition2D? runDefinition))
                    {
                        dependencies.TryAdd(runDefinition!.Id, runDefinition.Collider);
                        run++;
                    }

                    AddCollider(new TileStaticCollider2D(
                        coalescible,
                        this,
                        Matrix3x2.CreateTranslation(coordinate.X * model.TileSize.Width, coordinate.Y * model.TileSize.Height),
                        boxWidth: model.TileSize.Width * run,
                        boxHeight: model.TileSize.Height));
                    localX += run;
                    continue;
                }

                AddCollider(new TileStaticCollider2D(
                    descriptor,
                    this,
                    TileFlipGeometry2D.Transform(cell.Flip, model.TileSize) *
                        Matrix3x2.CreateTranslation(coordinate.X * model.TileSize.Width, coordinate.Y * model.TileSize.Height)));
                localX++;
            }
        }

        return new TileCollisionChunkState(
            model.TileSize,
            model.IsVisible,
            chunk,
            dependencies.ToArray(),
            colliders.ToArray());

        void AddCollider(TileStaticCollider2D collider)
        {
            collider.Enabled = model.IsVisible;
            LogicalChildren.InsertOwned(LogicalChildren.Count, collider);
            collider.AttachSurface(Surface);
            colliders.Add(collider);
        }
    }

    private static bool TryGetMatchingFullCellBox(
        TileMap2DModel model,
        TileChunk2D chunk,
        int localX,
        int localY,
        TileColliderDescriptor2D expected,
        out TileDefinition2D? definition)
    {
        TileCell2D cell = chunk.Tiles[(localY * chunk.Width) + localX];
        if (cell.TileId != 0 &&
            model.TryResolveTile(cell.TileId, out _, out definition) &&
            definition is not null &&
            definition.Collider is TileColliderDescriptor2D collider &&
            IsFullCellBox(collider, model.TileSize) &&
            AreSemanticallyEqual(expected, collider))
        {
            return true;
        }

        definition = null;
        return false;
    }

    private static bool IsFullCellBox(
        TileColliderDescriptor2D descriptor,
        DrawSize tileSize) =>
        descriptor.Shape == TileColliderShape2D.Box &&
        descriptor.LocalTransform == System.Numerics.Matrix3x2.Identity &&
        descriptor.OffsetX == 0 &&
        descriptor.OffsetY == 0 &&
        descriptor.Width == tileSize.Width &&
        descriptor.Height == tileSize.Height;

    private static bool AreSemanticallyEqual(
        TileColliderDescriptor2D first,
        TileColliderDescriptor2D second) =>
        first.Shape == second.Shape &&
        first.LocalTransform == second.LocalTransform &&
        first.Width == second.Width &&
        first.Height == second.Height &&
        first.Radius == second.Radius &&
        string.Equals(first.Points, second.Points, StringComparison.Ordinal) &&
        first.OffsetX == second.OffsetX &&
        first.OffsetY == second.OffsetY &&
        first.CollisionLayer == second.CollisionLayer &&
        first.CollisionMask == second.CollisionMask &&
        first.IsTrigger == second.IsTrigger &&
        string.Equals(first.DebugIdentity, second.DebugIdentity, StringComparison.Ordinal) &&
        HaveEqualProperties(first.Properties, second.Properties);

    private static bool HaveEqualProperties(
        IReadOnlyDictionary<string, object?> first,
        IReadOnlyDictionary<string, object?> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        foreach ((string key, object? value) in first)
        {
            if (!second.TryGetValue(key, out object? other) || !Equals(value, other))
            {
                return false;
            }
        }

        return true;
    }

    private void SynchronizePlacementColliders(TileMap2DModel? model)
    {
        if (ReferenceEquals(collisionPlacementModel, model)) { return; }
        foreach (TileStaticCollider2D collider in placementColliders)
        {
            collider.AttachSurface(null);
            LogicalChildren.RemoveOwned(collider);
        }
        placementColliders.Clear();
        collisionPlacementModel = model;
        if (model?.IsFreePlacement != true) { return; }

        foreach (Tile tile in model.Tiles)
        {
            Matrix3x2 placement = Matrix3x2.CreateTranslation(tile.X, tile.Y);
            if (tile.Collider is TileColliderDescriptor2D descriptor)
            {
                TileStaticCollider2D collider = new(descriptor, this, placement);
                LogicalChildren.InsertOwned(LogicalChildren.Count, collider);
                collider.AttachSurface(Surface);
                placementColliders.Add(collider);
            }
        }
    }

    private void RemoveCollisionChunk(TileCollisionChunkState state)
    {
        foreach (TileStaticCollider2D collider in state.Colliders)
        {
            collider.AttachSurface(null);
            LogicalChildren.RemoveOwned(collider);
        }
    }

    private readonly record struct TileCollisionChunkKey(
        TileCoordinate2D Origin,
        int Width,
        int Height);

    private sealed class TileCollisionChunkState(
        DrawSize tileSize,
        bool mapIsVisible,
        TileChunk2D chunk,
        KeyValuePair<int, TileColliderDescriptor2D?>[] dependencies,
        TileStaticCollider2D[] colliders)
    {
        internal TileStaticCollider2D[] Colliders { get; } = colliders;

        internal bool IsCurrent(
            TileMap2DModel currentModel,
            TileChunk2D currentChunk)
        {
            if (tileSize != currentModel.TileSize ||
                mapIsVisible != currentModel.IsVisible ||
                (!ReferenceEquals(chunk, currentChunk) &&
                 !chunk.HasSameCells(currentChunk)))
            {
                return false;
            }

            foreach ((int tileId, TileColliderDescriptor2D? descriptor) in dependencies)
            {
                if (!currentModel.TryResolveTile(tileId, out _, out TileDefinition2D? current) ||
                    !ReferenceEquals(descriptor, current?.Collider))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
