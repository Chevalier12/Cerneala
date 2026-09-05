using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

public sealed partial class TileMap2D
{
    private readonly Dictionary<TileChunkCacheKey, TileChunkCacheEntry> chunkCache = [];
    private readonly Dictionary<string, TileLayerSpatialIndex> layerSpatialIndexes = new(StringComparer.Ordinal);
    private readonly List<string> staleLayerSpatialIndexIds = [];
    private readonly HashSet<TileChunkCacheKey> currentModelChunkKeys = [];
    private readonly List<TileChunkCacheKey> staleChunkCacheKeys = [];
    private TileMap2DModel? indexedModel;

    internal IReadOnlyList<TileChunk2D> GetDebugChunks(TileLayer2DModel layer, SceneBounds2D visibleBounds)
    {
        EnsureSpatialIndexes();
        return layerSpatialIndexes[layer.Id].Query(visibleBounds, Model!.TileSize);
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
        IReadOnlyList<TileChunk2D> candidates = spatialIndex.Query(
            visibleBounds,
            Model!.TileSize);
        int encounteredPromotedCount = 0;
        foreach (TileChunk2D chunk in candidates)
        {
            diagnostics = diagnostics with
            {
                CandidateChunks = diagnostics.CandidateChunks + 1
            };
            if (!IntersectsChunk(visibleBounds, chunk, Model.TileSize))
            {
                encounteredPromotedCount += RecordPromotedFromCulledChunk(
                    presentation,
                    chunk,
                    promoted,
                    context);
                continue;
            }
            diagnostics = diagnostics with
            {
                VisibleChunks = diagnostics.VisibleChunks + 1,
                CandidateTiles = diagnostics.CandidateTiles + chunk.Tiles.Count
            };
            TileChunkCacheKey key = new(
                layer.Id,
                chunk.Origin,
                chunk.Width,
                chunk.Height);
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
                existing.IsCompatible(layer))
            {
                existing.UpdateChunks(layer);
            }
            else
            {
                layerSpatialIndexes[layer.Id] = new TileLayerSpatialIndex(layer);
            }
            staleLayerSpatialIndexIds.Remove(layer.Id);
            foreach (TileChunk2D chunk in layer.Chunks)
            {
                currentModelChunkKeys.Add(new TileChunkCacheKey(
                    layer.Id,
                    chunk.Origin,
                    chunk.Width,
                    chunk.Height));
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
        TileChunk2D chunk,
        DrawSize tileSize)
    {
        if (visibleBounds.Kind == SceneBoundsKind.Empty)
        {
            return false;
        }
        if (visibleBounds.Kind == SceneBoundsKind.Unknown)
        {
            return true;
        }

        double left = (double)chunk.Origin.X * tileSize.Width;
        double top = (double)chunk.Origin.Y * tileSize.Height;
        double right = (double)((long)chunk.Origin.X + chunk.Width) * tileSize.Width;
        double bottom = (double)((long)chunk.Origin.Y + chunk.Height) * tileSize.Height;
        if (!double.IsFinite(left) || !double.IsFinite(top) ||
            !double.IsFinite(right) || !double.IsFinite(bottom))
        {
            return true;
        }

        DrawRect viewport = visibleBounds.Bounds;
        return left < viewport.Right &&
            right > viewport.X &&
            top < viewport.Bottom &&
            bottom > viewport.Y;
    }

    private TileChunkCacheEntry BuildChunkEntry(
        TileChunk2D chunk,
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

        for (int index = 0; index < chunk.Tiles.Count; index++)
        {
            int localX = index % chunk.Width;
            int localY = index / chunk.Width;
            TileCoordinate2D coordinate = new(
                chunk.Origin.X + localX,
                chunk.Origin.Y + localY);
            TileCell2D cell = chunk.Tiles[index];
            if (promoted.ContainsKey(coordinate))
            {
                bool hadStaticContent = segmentBatches.Count > 0;
                FlushOrderSegment();
                pendingPromotionSplit |= hadStaticContent;
                suppressed.Add(coordinate);
                items.Add(TileChunkDrawItem.Promoted(coordinate));
                continue;
            }
            if (cell.TileId == 0 ||
                !model.TryResolveTile(cell.TileId, out TileSet2D? tileSet, out TileDefinition2D? definition) ||
                tileSet is null ||
                definition is null)
            {
                continue;
            }

            ResolvedAtlas atlas = resolvedAtlases[tileSet.Id];
            AddDependency(tileSet, atlas);
            if (atlas.Image is null)
            {
                continue;
            }
            if (pendingPromotionSplit)
            {
                batchSplits++;
                pendingPromotionSplit = false;
            }

            TileAtlasBatchBuilder? batch = segmentBatches.FirstOrDefault(candidate =>
                ReferenceEquals(candidate.Image, atlas.Image));
            if (batch is null)
            {
                batch = new TileAtlasBatchBuilder(atlas.Image);
                segmentBatches.Add(batch);
            }
            batch.Sprites.Add(TileFlipGeometry2D.Sprite(
                new DrawRect(
                    coordinate.X * model.TileSize.Width,
                    coordinate.Y * model.TileSize.Height,
                    model.TileSize.Width,
                    model.TileSize.Height),
                definition.SourceRect, tint, cell.Flip));
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

        void AddDependency(TileSet2D tileSet, ResolvedAtlas atlas)
        {
            if (dependencies.Any(candidate =>
                string.Equals(candidate.TileSetId, tileSet.Id, StringComparison.Ordinal)))
            {
                return;
            }
            dependencies.Add(new TileAtlasDependencyStamp(
                tileSet.Id,
                tileSet.Version,
                atlas.Version,
                atlas.Image));
        }
    }

    private sealed class TileChunkCacheEntry
    {
        internal TileChunkCacheEntry(
            long chunkVersion,
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
            TileChunk2D chunk,
            Color tint,
            IReadOnlyDictionary<TileCoordinate2D, TileInstance2D> promoted)
        {
            if (ChunkVersion != chunk.Version ||
                TileSize != owner.Model!.TileSize ||
                Tint != tint ||
                !SuppressionMatches(chunk, promoted))
            {
                return false;
            }

            foreach (TileAtlasDependencyStamp dependency in AtlasDependencies)
            {
                TileSet2D? tileSet = owner.Model.TileSets.FirstOrDefault(candidate =>
                    string.Equals(candidate.Id, dependency.TileSetId, StringComparison.Ordinal));
                if (tileSet is null ||
                    tileSet.Version != dependency.TileSetVersion ||
                    !owner.resolvedAtlases.TryGetValue(dependency.TileSetId, out ResolvedAtlas atlas) ||
                    atlas.Version != dependency.AtlasResourceVersion ||
                    !ReferenceEquals(atlas.Image, dependency.Image))
                {
                    return false;
                }
            }

            return true;
        }

        private bool SuppressionMatches(
            TileChunk2D chunk,
            IReadOnlyDictionary<TileCoordinate2D, TileInstance2D> promoted)
        {
            int count = 0;
            foreach (TileCoordinate2D coordinate in promoted.Keys)
            {
                if (!chunk.Contains(coordinate))
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

    private readonly record struct TileChunkCacheKey(
        string LayerId,
        TileCoordinate2D Origin,
        int Width,
        int Height);

    private readonly record struct TileAtlasDependencyStamp(
        string TileSetId,
        long TileSetVersion,
        long AtlasResourceVersion,
        IDrawImage? Image);

    private sealed class TileAtlasBatchBuilder(IDrawImage image)
    {
        internal IDrawImage Image { get; } = image;

        internal List<DrawSprite2D> Sprites { get; } = [];
    }

    private readonly record struct TileSpatialBucket(long X, long Y);

    private sealed class TileLayerSpatialIndex
    {
        private readonly TileChunk2D[] chunks;
        private readonly Dictionary<TileSpatialBucket, int[]> buckets;
        private readonly HashSet<int> candidateIndices = [];
        private readonly List<int> orderedCandidateIndices = [];
        private readonly List<TileChunk2D> queryResult = [];
        private readonly int bucketWidth;
        private readonly int bucketHeight;

        internal TileLayerSpatialIndex(TileLayer2DModel layer)
        {
            chunks = layer.Chunks.ToArray();
            bucketWidth = chunks.Length == 0 ? 1 : chunks.Max(static chunk => chunk.Width);
            bucketHeight = chunks.Length == 0 ? 1 : chunks.Max(static chunk => chunk.Height);
            Dictionary<TileSpatialBucket, List<int>> mutableBuckets = [];
            for (int index = 0; index < chunks.Length; index++)
            {
                TileChunk2D chunk = chunks[index];
                long minBucketX = FloorDivide(chunk.Origin.X, bucketWidth);
                long maxBucketX = FloorDivide((long)chunk.Origin.X + chunk.Width - 1, bucketWidth);
                long minBucketY = FloorDivide(chunk.Origin.Y, bucketHeight);
                long maxBucketY = FloorDivide((long)chunk.Origin.Y + chunk.Height - 1, bucketHeight);
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

        internal bool IsCompatible(TileLayer2DModel layer)
        {
            if (layer.Chunks.Count != chunks.Length)
            {
                return false;
            }

            for (int index = 0; index < chunks.Length; index++)
            {
                TileChunk2D current = chunks[index];
                TileChunk2D candidate = layer.Chunks[index];
                if (current.Origin != candidate.Origin ||
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
                chunks[index] = layer.Chunks[index];
            }
        }

        internal IReadOnlyList<TileChunk2D> Query(
            SceneBounds2D visibleBounds,
            DrawSize tileSize)
        {
            queryResult.Clear();
            if (visibleBounds.Kind == SceneBoundsKind.Empty || chunks.Length == 0)
            {
                return queryResult;
            }
            if (visibleBounds.Kind == SceneBoundsKind.Unknown)
            {
                queryResult.AddRange(chunks);
                return queryResult;
            }

            DrawRect bounds = visibleBounds.Bounds;
            long minTileX = FloorToLong(((double)bounds.X / tileSize.Width) - 1);
            long maxTileX = FloorToLong((double)bounds.Right / tileSize.Width);
            long minTileY = FloorToLong(((double)bounds.Y / tileSize.Height) - 1);
            long maxTileY = FloorToLong((double)bounds.Bottom / tileSize.Height);
            long minBucketX = FloorDivide(minTileX, bucketWidth);
            long maxBucketX = FloorDivide(maxTileX, bucketWidth);
            long minBucketY = FloorDivide(minTileY, bucketHeight);
            long maxBucketY = FloorDivide(maxTileY, bucketHeight);
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

        private static long FloorDivide(long value, int divisor)
        {
            long quotient = value / divisor;
            long remainder = value % divisor;
            return remainder < 0 ? quotient - 1 : quotient;
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
