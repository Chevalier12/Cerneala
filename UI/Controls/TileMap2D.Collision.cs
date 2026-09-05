using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

public sealed partial class TileMap2D
{
    private readonly Dictionary<TileCollisionChunkKey, TileCollisionChunkState>
        collisionChunks = [];

    internal void SynchronizeCollisionAdaptersAndNotify()
    {
        SynchronizeCollisionAdapters();
        SceneGeometry2D.FindRootScene(this)?.NotifyCollisionMutation(
            this,
            SceneCollisionMutationKind.Structure);
    }

    private void SynchronizeCollisionAdapters()
    {
        HashSet<TileCollisionChunkKey> current = [];
        TileMap2DModel? model = Model;
        if (model is not null)
        {
            foreach (TileLayer2DModel layer in model.Layers)
            {
                TileLayer2D presentation = Layers.Single(candidate =>
                    string.Equals(candidate.LayerId, layer.Id, StringComparison.Ordinal));
                foreach (TileChunk2D chunk in layer.Chunks)
                {
                    TileCollisionChunkKey key = new(
                        layer.Id,
                        chunk.Origin,
                        chunk.Width,
                        chunk.Height);
                    current.Add(key);
                    HashSet<TileCoordinate2D> suppressed = presentation.PromotedTiles
                        .Where(static tile => tile.ReplacesImportedColliders)
                        .Select(static tile => new TileCoordinate2D(tile.X, tile.Y))
                        .Where(chunk.Contains)
                        .ToHashSet();
                    if (collisionChunks.TryGetValue(key, out TileCollisionChunkState? existing) &&
                        existing.IsCurrent(model, layer, presentation, chunk, suppressed))
                    {
                        continue;
                    }

                    if (existing is not null)
                    {
                        RemoveCollisionChunk(existing);
                    }

                    TileCollisionChunkState rebuilt = BuildCollisionChunk(
                        model,
                        layer,
                        presentation,
                        chunk,
                        suppressed);
                    collisionChunks[key] = rebuilt;
                }
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
        TileLayer2DModel layer,
        TileLayer2D presentation,
        TileChunk2D chunk,
        HashSet<TileCoordinate2D> suppressed)
    {
        List<TileStaticCollider2D> colliders = [];
        HashSet<TileDefinition2D> dependencies = new(ReferenceEqualityComparer.Instance);
        for (int localY = 0; localY < chunk.Height; localY++)
        {
            for (int localX = 0; localX < chunk.Width;)
            {
                TileCoordinate2D coordinate = new(
                    chunk.Origin.X + localX,
                    chunk.Origin.Y + localY);
                TileCell2D cell = chunk.Tiles[(localY * chunk.Width) + localX];
                if (cell.TileId == 0 || suppressed.Contains(coordinate) ||
                    !model.TryResolveTile(cell.TileId, out _, out TileDefinition2D? definition) ||
                    definition is null || definition.Colliders.Count == 0)
                {
                    localX++;
                    continue;
                }

                dependencies.Add(definition);
                TileColliderDescriptor2D? coalescible = definition.Colliders.Count == 1 &&
                    IsFullCellBox(definition.Colliders[0], model.TileSize)
                        ? definition.Colliders[0]
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
                            suppressed,
                            coalescible,
                            out TileDefinition2D? runDefinition))
                    {
                        dependencies.Add(runDefinition!);
                        run++;
                    }

                    AddCollider(new TileStaticCollider2D(
                        coalescible,
                        coordinate,
                        model.TileSize,
                        TileFlip2D.None,
                        boxWidth: model.TileSize.Width * run,
                        boxHeight: model.TileSize.Height));
                    localX += run;
                    continue;
                }

                foreach (TileColliderDescriptor2D descriptor in definition.Colliders)
                {
                    AddCollider(new TileStaticCollider2D(
                        descriptor,
                        coordinate,
                        model.TileSize,
                        cell.Flip));
                }
                localX++;
            }
        }

        return new TileCollisionChunkState(
            model.TileSize,
            layer.IsVisible,
            presentation,
            chunk,
            suppressed,
            dependencies.ToArray(),
            colliders.ToArray());

        void AddCollider(TileStaticCollider2D collider)
        {
            collider.Enabled = layer.IsVisible;
            presentation.LogicalChildren.Add(collider);
            collider.AttachSurface(Surface);
            colliders.Add(collider);
        }
    }

    private static bool TryGetMatchingFullCellBox(
        TileMap2DModel model,
        TileChunk2D chunk,
        int localX,
        int localY,
        IReadOnlySet<TileCoordinate2D> suppressed,
        TileColliderDescriptor2D expected,
        out TileDefinition2D? definition)
    {
        TileCoordinate2D coordinate = new(
            chunk.Origin.X + localX,
            chunk.Origin.Y + localY);
        TileCell2D cell = chunk.Tiles[(localY * chunk.Width) + localX];
        if (cell.TileId != 0 &&
            !suppressed.Contains(coordinate) &&
            model.TryResolveTile(cell.TileId, out _, out definition) &&
            definition is not null &&
            definition.Colliders.Count == 1 &&
            IsFullCellBox(definition.Colliders[0], model.TileSize) &&
            AreSemanticallyEqual(expected, definition.Colliders[0]))
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

    private static void RemoveCollisionChunk(TileCollisionChunkState state)
    {
        foreach (TileStaticCollider2D collider in state.Colliders)
        {
            collider.AttachSurface(null);
            state.Presentation.LogicalChildren.Remove(collider);
        }
    }

    private readonly record struct TileCollisionChunkKey(
        string LayerId,
        TileCoordinate2D Origin,
        int Width,
        int Height);

    private sealed class TileCollisionChunkState(
        DrawSize tileSize,
        bool layerIsVisible,
        TileLayer2D presentation,
        TileChunk2D chunk,
        HashSet<TileCoordinate2D> suppressed,
        TileDefinition2D[] dependencies,
        TileStaticCollider2D[] colliders)
    {
        internal TileLayer2D Presentation { get; } = presentation;

        internal TileStaticCollider2D[] Colliders { get; } = colliders;

        internal bool IsCurrent(
            TileMap2DModel currentModel,
            TileLayer2DModel currentLayer,
            TileLayer2D currentPresentation,
            TileChunk2D currentChunk,
            HashSet<TileCoordinate2D> currentSuppressed)
        {
            if (tileSize != currentModel.TileSize ||
                layerIsVisible != currentLayer.IsVisible ||
                !ReferenceEquals(Presentation, currentPresentation) ||
                !ReferenceEquals(chunk, currentChunk) ||
                !suppressed.SetEquals(currentSuppressed))
            {
                return false;
            }

            foreach (TileDefinition2D dependency in dependencies)
            {
                if (!currentModel.TryResolveTile(dependency.Id, out _, out TileDefinition2D? current) ||
                    !ReferenceEquals(dependency, current))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
