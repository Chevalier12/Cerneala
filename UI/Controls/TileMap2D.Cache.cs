using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

public sealed partial class TileMap2D
{
    private const int PlacementChunkSize = 256;
    private readonly Dictionary<TileChunkCacheKey, TileChunkCacheEntry> chunkCache = [];
    private readonly Dictionary<string, TileLayerSpatialIndex> layerSpatialIndexes = new(StringComparer.Ordinal);
    private readonly List<string> staleLayerSpatialIndexIds = [];
    private readonly HashSet<TileChunkCacheKey> currentModelChunkKeys = [];
    private readonly List<TileChunkCacheKey> staleChunkCacheKeys = [];
    private TileMap2DModel? indexedModel;

    internal IReadOnlyList<TileRenderChunk> GetDebugChunks(TileLayer2DModel layer, SceneBounds2D visibleBounds)
    {
        ResolveAtlases(Model!);
        EnsureSpatialIndexes();
        return layerSpatialIndexes[layer.Id].Query(visibleBounds);
    }

    internal SceneBounds2D GetLayerBounds(TileLayer2DModel layer)
    {
        ResolveAtlases(Model!);
        EnsureSpatialIndexes();
        return layerSpatialIndexes[layer.Id].Bounds;
    }

    private void BeginCacheFrame()
    {
        EnsureSpatialIndexes();
    }

    private void CompleteCacheFrame()
    {
        staleChunkCacheKeys.Clear();
        long retainedBytes = 0;
        int retainedObjects = 0;
        foreach ((TileChunkCacheKey key, TileChunkCacheEntry entry) in chunkCache)
        {
            if (!currentModelChunkKeys.Contains(key))
            {
                staleChunkCacheKeys.Add(key);
                continue;
            }

            retainedBytes += entry.RetainedBytes;
            retainedObjects += entry.RetainedObjects;
        }

        foreach (TileChunkCacheKey key in staleChunkCacheKeys)
        {
            chunkCache.Remove(key);
        }

        diagnostics = diagnostics with
        {
            RetainedBytes = retainedBytes,
            RetainedObjects = retainedObjects
        };
    }

    private void ClearTileCache()
    {
        chunkCache.Clear();
        layerSpatialIndexes.Clear();
        staleLayerSpatialIndexIds.Clear();
        currentModelChunkKeys.Clear();
        staleChunkCacheKeys.Clear();
        indexedModel = null;
    }

    private void RecordCachedLayer(
        TileLayer2D presentation,
        TileLayer2DModel layer,
        Color tint,
        Scene2DRecordContext context)
    {
        Dictionary<TileCoordinate2D, TileInstance2D> promoted = new();
        foreach (TileInstance2D instance in presentation.PromotedTiles)
        {
            TileCoordinate2D coordinate = new(instance.X, instance.Y);
            if (!promoted.TryAdd(coordinate, instance))
            {
                throw new InvalidOperationException(
                    $"Tile ({instance.X},{instance.Y}) is promoted more than once in layer '{layer.Id}'.");
            }
            ValidatePromotedInstance(layer, coordinate, instance);
        }

        SceneBounds2D visibleBounds = context.GetConservativeVisibleLocalBounds();
        TileLayerSpatialIndex spatialIndex = layerSpatialIndexes[layer.Id];
        IReadOnlyList<TileRenderChunk> candidates = spatialIndex.Query(visibleBounds);
        int encounteredPromotedCount = 0;
        foreach (TileRenderChunk chunk in candidates)
        {
            diagnostics = diagnostics with
            {
                CandidateChunks = diagnostics.CandidateChunks + 1
            };
            if (!IntersectsChunk(visibleBounds, chunk))
            {
                encounteredPromotedCount += chunk.Grid is TileChunk2D grid ? RecordPromotedFromCulledChunk(
                    presentation,
                    grid,
                    promoted,
                    context) : 0;
                continue;
            }
            diagnostics = diagnostics with
            {
                VisibleChunks = diagnostics.VisibleChunks + 1,
                CandidateTiles = diagnostics.CandidateTiles + chunk.Count
            };
            TileChunkCacheKey key = chunk.GetKey(layer.Id);
            bool reused = chunkCache.TryGetValue(key, out TileChunkCacheEntry? entry) &&
                entry.IsCurrent(this, chunk, tint, promoted);
            if (!reused)
            {
                entry = BuildChunkEntry(chunk, tint, promoted);
                chunkCache[key] = entry;
            }

            TileChunkCacheEntry currentEntry = entry!;
            diagnostics = diagnostics with
            {
                BatchesBuilt = diagnostics.BatchesBuilt + (reused ? 0 : currentEntry.BatchCount),
                BatchesRebuilt = diagnostics.BatchesRebuilt + (reused ? 0 : currentEntry.BatchCount),
                BatchesReused = diagnostics.BatchesReused + (reused ? currentEntry.BatchCount : 0),
                DrawnTiles = diagnostics.DrawnTiles + currentEntry.StaticTileCount,
                BatchSplits = diagnostics.BatchSplits + currentEntry.BatchSplits
            };
            foreach (TileChunkDrawItem item in currentEntry.Items)
            {
                if (item.Batch is DrawSpriteBatch batch)
                {
                    context.Frame.DrawSpriteBatch(batch);
                    diagnostics = diagnostics with
                    {
                        DrawCommands = diagnostics.DrawCommands + 1
                    };
                }
                else if (promoted.TryGetValue(item.PromotedCoordinate, out TileInstance2D? instance))
                {
                    encounteredPromotedCount++;
                    RecordPromotedTile(presentation, instance, context);
                }
            }
        }
        diagnostics = diagnostics with
        {
            PromotedInstancesCulled = diagnostics.PromotedInstancesCulled +
                promoted.Count - encounteredPromotedCount
        };
    }

    private int RecordPromotedFromCulledChunk(
        TileLayer2D presentation,
        TileChunk2D chunk,
        IReadOnlyDictionary<TileCoordinate2D, TileInstance2D> promoted,
        Scene2DRecordContext context)
    {
        List<KeyValuePair<TileCoordinate2D, TileInstance2D>>? ordered = null;
        foreach (KeyValuePair<TileCoordinate2D, TileInstance2D> entry in promoted)
        {
            if (!chunk.Contains(entry.Key))
            {
                continue;
            }

            ordered ??= [];
            ordered.Add(entry);
        }
        if (ordered is null)
        {
            return 0;
        }

        ordered.Sort((left, right) =>
        {
            int leftIndex = ((left.Key.Y - chunk.Origin.Y) * chunk.Width) +
                left.Key.X - chunk.Origin.X;
            int rightIndex = ((right.Key.Y - chunk.Origin.Y) * chunk.Width) +
                right.Key.X - chunk.Origin.X;
            return leftIndex.CompareTo(rightIndex);
        });
        foreach (KeyValuePair<TileCoordinate2D, TileInstance2D> entry in ordered)
        {
            RecordPromotedTile(presentation, entry.Value, context);
        }
        return ordered.Count;
    }

    private void EnsureSpatialIndexes()
    {
        TileMap2DModel model = Model!;
        if (ReferenceEquals(indexedModel, model))
        {
            return;
        }

        staleLayerSpatialIndexIds.Clear();
        staleLayerSpatialIndexIds.AddRange(layerSpatialIndexes.Keys);
        currentModelChunkKeys.Clear();
        foreach (TileLayer2DModel layer in model.Layers)
        {
            if (layerSpatialIndexes.TryGetValue(layer.Id, out TileLayerSpatialIndex? existing) &&
                !model.IsFreePlacement && existing.IsCompatible(layer, model.TileSize))
            {
                existing.UpdateChunks(layer);
            }
            else
            {
                layerSpatialIndexes[layer.Id] = new TileLayerSpatialIndex(CreateRenderChunks(model, layer), model.TileSize);
            }
            staleLayerSpatialIndexIds.Remove(layer.Id);
            foreach (TileRenderChunk chunk in layerSpatialIndexes[layer.Id].Chunks)
            {
                currentModelChunkKeys.Add(chunk.GetKey(layer.Id));
            }
        }
        foreach (string layerId in staleLayerSpatialIndexIds)
        {
            layerSpatialIndexes.Remove(layerId);
        }

        indexedModel = model;
    }

    private static bool IntersectsChunk(
        SceneBounds2D visibleBounds,
        TileRenderChunk chunk)
    {
        if (visibleBounds.Kind == SceneBoundsKind.Empty)
        {
            return false;
        }
        if (visibleBounds.Kind == SceneBoundsKind.Unknown)
        {
            return true;
        }

        if (chunk.Bounds.Kind == SceneBoundsKind.Empty) { return false; }
        if (chunk.Bounds.Kind == SceneBoundsKind.Unknown) { return true; }
        DrawRect rect = chunk.Bounds.Bounds;
        DrawRect viewport = visibleBounds.Bounds;
        return rect.X < viewport.Right && rect.Right > viewport.X &&
            rect.Y < viewport.Bottom && rect.Bottom > viewport.Y;
    }

    private TileChunkCacheEntry BuildChunkEntry(
        TileRenderChunk chunk,
        Color tint,
        IReadOnlyDictionary<TileCoordinate2D, TileInstance2D> promoted)
    {
        TileMap2DModel model = Model!;
        List<TileChunkDrawItem> items = [];
        List<TileAtlasBatchBuilder> segmentBatches = [];
        List<TileCoordinate2D> suppressed = [];
        List<TileAtlasDependencyStamp> dependencies = [];
        int staticTileCount = 0;
        int batchSplits = 0;
        bool pendingPromotionSplit = false;

        for (int index = 0; index < chunk.Count; index++)
        {
            TileChunk2D? grid = chunk.Grid;
            TileCoordinate2D coordinate = grid is null ? default : new(
                grid.Origin.X + index % grid.Width,
                grid.Origin.Y + index / grid.Width);
            if (grid is not null && promoted.ContainsKey(coordinate))
            {
                bool hadStaticContent = segmentBatches.Count > 0;
                FlushOrderSegment();
                pendingPromotionSplit |= hadStaticContent;
                suppressed.Add(coordinate);
                items.Add(TileChunkDrawItem.Promoted(coordinate));
                continue;
            }
            TileImageKey imageKey;
            DrawRect destination;
            DrawRect source;
            TileFlip2D flip;
            if (grid is not null)
            {
                TileCell2D cell = grid.Tiles[index];
                if (cell.TileId == 0 || !model.TryResolveTile(cell.TileId, out TileSet2D? tileSet, out TileDefinition2D? definition) ||
                    tileSet is null || definition is null) { continue; }
                imageKey = new TileImageKey(tileSet.Id, null);
                destination = new DrawRect(coordinate.X * model.TileSize.Width, coordinate.Y * model.TileSize.Height,
                    model.TileSize.Width, model.TileSize.Height);
                source = definition.SourceRect;
                flip = cell.Flip;
            }
            else
            {
                Tile tile = chunk.Placements![chunk.Start + index];
                imageKey = new TileImageKey(null, tile.Image);
                DrawSize size = resolvedAtlases[imageKey].Size;
                destination = tile.GetDestination(size);
                source = new DrawRect(0, 0, size.Width, size.Height);
                flip = TileFlip2D.None;
            }

            ResolvedAtlas atlas = resolvedAtlases[imageKey];
            AddDependency(imageKey, atlas);
            if (atlas.Image is null)
            {
                continue;
            }
            if (pendingPromotionSplit)
            {
                batchSplits++;
                pendingPromotionSplit = false;
            }

            // Grid cells cannot overlap. Free placements can, so only adjacent
            // equal-image runs may be coalesced without changing painter order.
            TileAtlasBatchBuilder? batch = grid is not null
                ? segmentBatches.FirstOrDefault(candidate => ReferenceEquals(candidate.Image, atlas.Image))
                : segmentBatches.LastOrDefault();
            if (batch is not null && !ReferenceEquals(batch.Image, atlas.Image)) { batch = null; }
            if (batch is null)
            {
                batch = new TileAtlasBatchBuilder(atlas.Image);
                segmentBatches.Add(batch);
            }
            batch.Sprites.Add(TileFlipGeometry2D.Sprite(
                destination, source, tint, flip));
            staticTileCount++;
        }
        FlushOrderSegment();

        TileChunkDrawItem[] copiedItems = items.ToArray();
        TileCoordinate2D[] copiedSuppressed = suppressed.ToArray();
        TileAtlasDependencyStamp[] copiedDependencies = dependencies.ToArray();
        int batchCount = copiedItems.Count(static item => item.Batch is not null);
        long retainedBytes = 4096L +
            (staticTileCount * 128L) +
            (copiedItems.Length * 32L) +
            (copiedDependencies.Length * 32L);
        int retainedObjects = 4 + copiedItems.Length + staticTileCount;
        return new TileChunkCacheEntry(
            chunk.Version,
            chunk.Placements,
            model.TileSize,
            tint,
            copiedSuppressed,
            copiedDependencies,
            copiedItems,
            staticTileCount,
            batchCount,
            batchSplits,
            retainedBytes,
            retainedObjects);

        void FlushOrderSegment()
        {
            foreach (TileAtlasBatchBuilder batch in segmentBatches)
            {
                items.Add(TileChunkDrawItem.Static(
                    new DrawSpriteBatch(batch.Image, batch.Sprites)));
            }
            segmentBatches.Clear();
        }

        void AddDependency(TileImageKey key, ResolvedAtlas atlas)
        {
            if (dependencies.Any(candidate => candidate.Key == key))
            {
                return;
            }
            dependencies.Add(new TileAtlasDependencyStamp(
                key,
                atlas.DefinitionVersion,
                atlas.Version,
                atlas.Image,
                atlas.Size));
        }
    }

    private sealed class TileChunkCacheEntry
    {
        internal TileChunkCacheEntry(
            long chunkVersion,
            IReadOnlyList<Tile>? placements,
            DrawSize tileSize,
            Color tint,
            TileCoordinate2D[] suppressedCoordinates,
            TileAtlasDependencyStamp[] atlasDependencies,
            TileChunkDrawItem[] items,
            int staticTileCount,
            int batchCount,
            int batchSplits,
            long retainedBytes,
            int retainedObjects)
        {
            ChunkVersion = chunkVersion;
            Placements = placements;
            TileSize = tileSize;
            Tint = tint;
            SuppressedCoordinates = suppressedCoordinates;
            AtlasDependencies = atlasDependencies;
            Items = items;
            StaticTileCount = staticTileCount;
            BatchCount = batchCount;
            BatchSplits = batchSplits;
            RetainedBytes = retainedBytes;
            RetainedObjects = retainedObjects;
        }

        internal long ChunkVersion { get; }

        private IReadOnlyList<Tile>? Placements { get; }

        internal DrawSize TileSize { get; }

        internal Color Tint { get; }

        internal TileCoordinate2D[] SuppressedCoordinates { get; }

        internal TileAtlasDependencyStamp[] AtlasDependencies { get; }

        internal TileChunkDrawItem[] Items { get; }

        internal int StaticTileCount { get; }

        internal int BatchCount { get; }

        internal int BatchSplits { get; }

        internal long RetainedBytes { get; }

        internal int RetainedObjects { get; }

        internal bool IsCurrent(
            TileMap2D owner,
            TileRenderChunk chunk,
            Color tint,
            IReadOnlyDictionary<TileCoordinate2D, TileInstance2D> promoted)
        {
            if (ChunkVersion != chunk.Version ||
                !ReferenceEquals(Placements, chunk.Placements) ||
                TileSize != owner.Model!.TileSize ||
                Tint != tint ||
                !SuppressionMatches(chunk, promoted))
            {
                return false;
            }

            foreach (TileAtlasDependencyStamp dependency in AtlasDependencies)
            {
                if (!owner.resolvedAtlases.TryGetValue(dependency.Key, out ResolvedAtlas atlas) ||
                    atlas.DefinitionVersion != dependency.TileSetVersion ||
                    atlas.Version != dependency.AtlasResourceVersion ||
                    atlas.Size != dependency.Size ||
                    !ReferenceEquals(atlas.Image, dependency.Image))
                {
                    return false;
                }
            }

            return true;
        }

        private bool SuppressionMatches(
            TileRenderChunk chunk,
            IReadOnlyDictionary<TileCoordinate2D, TileInstance2D> promoted)
        {
            int count = 0;
            foreach (TileCoordinate2D coordinate in promoted.Keys)
            {
                if (chunk.Grid is null || !chunk.Grid.Contains(coordinate))
                {
                    continue;
                }
                count++;
                if (!SuppressedCoordinates.Contains(coordinate))
                {
                    return false;
                }
            }
            return count == SuppressedCoordinates.Length;
        }
    }

    internal readonly record struct TileChunkCacheKey(
        string LayerId,
        TileCoordinate2D? Origin,
        int Width,
        int Height,
        int Start = 0);

    private readonly record struct TileAtlasDependencyStamp(
        TileImageKey Key,
        long TileSetVersion,
        long AtlasResourceVersion,
        IDrawImage? Image,
        DrawSize Size);

    private sealed class TileAtlasBatchBuilder(IDrawImage image)
    {
        internal IDrawImage Image { get; } = image;

        internal List<DrawSprite2D> Sprites { get; } = [];
    }

    private readonly record struct TileSpatialBucket(long X, long Y);

    internal sealed class TileRenderChunk
    {
        internal TileRenderChunk(TileChunk2D grid, DrawSize tileSize)
        {
            Grid = grid;
            Count = grid.Tiles.Count;
            Bounds = SceneBounds2D.Known(new DrawRect(grid.Origin.X * tileSize.Width, grid.Origin.Y * tileSize.Height,
                grid.Width * tileSize.Width, grid.Height * tileSize.Height));
        }

        internal TileRenderChunk(IReadOnlyList<Tile> placements, int start, int count, long version, SceneBounds2D bounds)
        {
            Placements = placements;
            Start = start;
            Count = count;
            PlacementVersion = version;
            Bounds = bounds;
        }

        internal TileChunk2D? Grid { get; set; }
        internal IReadOnlyList<Tile>? Placements { get; }
        internal int Start { get; }
        internal int Count { get; }
        internal SceneBounds2D Bounds { get; }
        private long PlacementVersion { get; }
        internal long Version => Grid?.Version ?? PlacementVersion;
        internal TileChunkCacheKey GetKey(string layerId) =>
            new(layerId, Grid?.Origin, Grid?.Width ?? 0, Grid?.Height ?? 0, Start);
    }

    private TileRenderChunk[] CreateRenderChunks(TileMap2DModel model, TileLayer2DModel layer)
    {
        if (!model.IsFreePlacement)
        {
            return layer.Chunks.Select(chunk => new TileRenderChunk(chunk, model.TileSize)).ToArray();
        }
        TileRenderChunk[] chunks = new TileRenderChunk[(model.Tiles.Count + PlacementChunkSize - 1) / PlacementChunkSize];
        for (int index = 0; index < chunks.Length; index++)
        {
            int start = index * PlacementChunkSize;
            int count = Math.Min(PlacementChunkSize, model.Tiles.Count - start);
            SceneBounds2D bounds = SceneBounds2D.Empty;
            for (int offset = 0; offset < count; offset++)
            {
                Tile tile = model.Tiles[start + offset];
                ResolvedAtlas atlas = resolvedAtlases[new TileImageKey(null, tile.Image)];
                if (atlas.Image is null) { continue; }
                DrawRect rect = tile.GetDestination(atlas.Size);
                if (rect.Width == 0 || rect.Height == 0) { continue; }
                bounds = SceneGeometry2D.Union(bounds, SceneBounds2D.Known(rect));
            }
            chunks[index] = new TileRenderChunk(model.Tiles, start, count, model.Version, bounds);
        }
        return chunks;
    }

    private sealed class TileLayerSpatialIndex
    {
        private readonly TileRenderChunk[] chunks;
        private readonly Dictionary<TileSpatialBucket, int[]> buckets;
        private readonly HashSet<int> candidateIndices = [];
        private readonly List<int> orderedCandidateIndices = [];
        private readonly List<TileRenderChunk> queryResult = [];
        private readonly double bucketWidth;
        private readonly double bucketHeight;
        private readonly DrawSize tileSize;
        private readonly bool hasUnknownBounds;

        internal IReadOnlyList<TileRenderChunk> Chunks => chunks;
        internal SceneBounds2D Bounds { get; }

        internal TileLayerSpatialIndex(TileRenderChunk[] chunks, DrawSize tileSize)
        {
            this.chunks = chunks;
            this.tileSize = tileSize;
            bucketWidth = chunks.Length == 0 ? 1 : Math.Max(1, chunks.Max(static chunk => chunk.Bounds.Bounds.Width));
            bucketHeight = chunks.Length == 0 ? 1 : Math.Max(1, chunks.Max(static chunk => chunk.Bounds.Bounds.Height));
            Bounds = SceneBounds2D.Empty;
            Dictionary<TileSpatialBucket, List<int>> mutableBuckets = [];
            for (int index = 0; index < chunks.Length; index++)
            {
                TileRenderChunk chunk = chunks[index];
                Bounds = SceneGeometry2D.Union(Bounds, chunk.Bounds);
                if (chunk.Bounds.Kind == SceneBoundsKind.Unknown) { hasUnknownBounds = true; continue; }
                if (chunk.Bounds.Kind == SceneBoundsKind.Empty) { continue; }
                DrawRect bounds = chunk.Bounds.Bounds;
                long minBucketX = FloorToLong(bounds.X / bucketWidth);
                long maxBucketX = FloorToLong(Math.Ceiling(bounds.Right / bucketWidth) - 1);
                long minBucketY = FloorToLong(bounds.Y / bucketHeight);
                long maxBucketY = FloorToLong(Math.Ceiling(bounds.Bottom / bucketHeight) - 1);
                for (long y = minBucketY; y <= maxBucketY; y++)
                {
                    for (long x = minBucketX; x <= maxBucketX; x++)
                    {
                        TileSpatialBucket key = new(x, y);
                        if (!mutableBuckets.TryGetValue(key, out List<int>? values))
                        {
                            values = [];
                            mutableBuckets.Add(key, values);
                        }
                        values.Add(index);
                    }
                }
            }
            buckets = mutableBuckets.ToDictionary(
                static pair => pair.Key,
                static pair => pair.Value.ToArray());
        }

        internal bool IsCompatible(TileLayer2DModel layer, DrawSize tileSize)
        {
            if (this.tileSize != tileSize || layer.Chunks.Count != chunks.Length)
            {
                return false;
            }

            for (int index = 0; index < chunks.Length; index++)
            {
                TileChunk2D? current = chunks[index].Grid;
                TileChunk2D candidate = layer.Chunks[index];
                if (current is null || current.Origin != candidate.Origin ||
                    current.Width != candidate.Width ||
                    current.Height != candidate.Height)
                {
                    return false;
                }
            }
            return true;
        }

        internal void UpdateChunks(TileLayer2DModel layer)
        {
            for (int index = 0; index < chunks.Length; index++)
            {
                chunks[index].Grid = layer.Chunks[index];
            }
        }

        internal IReadOnlyList<TileRenderChunk> Query(SceneBounds2D visibleBounds)
        {
            queryResult.Clear();
            if (visibleBounds.Kind == SceneBoundsKind.Empty || chunks.Length == 0)
            {
                return queryResult;
            }
            if (visibleBounds.Kind == SceneBoundsKind.Unknown || hasUnknownBounds)
            {
                queryResult.AddRange(chunks);
                return queryResult;
            }

            DrawRect bounds = visibleBounds.Bounds;
            long minBucketX = FloorToLong(((double)bounds.X - tileSize.Width) / bucketWidth);
            long maxBucketX = FloorToLong(bounds.Right / bucketWidth);
            long minBucketY = FloorToLong(((double)bounds.Y - tileSize.Height) / bucketHeight);
            long maxBucketY = FloorToLong(bounds.Bottom / bucketHeight);
            if (ShouldScanExistingChunks(
                minBucketX,
                maxBucketX,
                minBucketY,
                maxBucketY))
            {
                queryResult.AddRange(chunks);
                return queryResult;
            }

            candidateIndices.Clear();
            orderedCandidateIndices.Clear();
            for (long y = minBucketY; ; y++)
            {
                for (long x = minBucketX; ; x++)
                {
                    if (!buckets.TryGetValue(new TileSpatialBucket(x, y), out int[]? values))
                    {
                        if (x == maxBucketX)
                        {
                            break;
                        }
                        continue;
                    }
                    foreach (int index in values)
                    {
                        if (candidateIndices.Add(index))
                        {
                            orderedCandidateIndices.Add(index);
                        }
                    }
                    if (x == maxBucketX)
                    {
                        break;
                    }
                }
                if (y == maxBucketY)
                {
                    break;
                }
            }
            orderedCandidateIndices.Sort();
            foreach (int index in orderedCandidateIndices)
            {
                queryResult.Add(chunks[index]);
            }
            return queryResult;
        }

        private bool ShouldScanExistingChunks(
            long minBucketX,
            long maxBucketX,
            long minBucketY,
            long maxBucketY)
        {
            if (maxBucketX < minBucketX || maxBucketY < minBucketY)
            {
                return true;
            }

            ulong width = (ulong)(maxBucketX - minBucketX) + 1;
            ulong height = (ulong)(maxBucketY - minBucketY) + 1;
            ulong maximumQueries = (ulong)Math.Max(1, buckets.Count) * 4UL;
            return width > maximumQueries ||
                height > maximumQueries ||
                width * height > maximumQueries;
        }

        private static long FloorToLong(double value)
        {
            if (double.IsNaN(value))
            {
                return 0;
            }
            if (value <= long.MinValue)
            {
                return long.MinValue;
            }
            if (value >= long.MaxValue)
            {
                return long.MaxValue;
            }
            return (long)Math.Floor(value);
        }

    }

    private readonly record struct TileChunkDrawItem(
        DrawSpriteBatch? Batch,
        TileCoordinate2D PromotedCoordinate)
    {
        internal static TileChunkDrawItem Static(DrawSpriteBatch batch) =>
            new(batch, default);

        internal static TileChunkDrawItem Promoted(TileCoordinate2D coordinate) =>
            new(null, coordinate);
    }
}
