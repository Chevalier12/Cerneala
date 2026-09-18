using System.Diagnostics.CodeAnalysis;
using Cerneala.Drawing;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Controls;

public sealed partial class TileMap2D
{
    private readonly Dictionary<TileChunkCacheKey, TileChunkCacheEntry> chunkCache = [];
    private TileMapSpatialIndex? spatialIndex;
    private readonly HashSet<TileChunkCacheKey> renderedChunkKeys = [];
    private readonly List<TileChunkCacheKey> staleChunkCacheKeys = [];
    private TileMapCatalog2D? indexedCatalog;
    private long cacheFrameVersion;

    internal IReadOnlyList<TileRenderChunk> GetDebugChunks(SceneBounds2D visibleBounds)
    {
        EnsureSpatialIndexes();
        return spatialIndex?.Query(visibleBounds) ?? Array.Empty<TileRenderChunk>();
    }

    internal bool TryGetResidentData(TileRenderChunk chunk, [NotNullWhen(true)] out TileMapChunkData2D? data)
    {
        data = residentData.TryGetValue(chunk.GetKey(), out ResidentChunk? resident) &&
            resident.Info.Spatial.Version == chunk.Version && ReferenceEquals(resident.ValidatedCatalog, Catalog) ? resident.Payload.Value : null;
        return data is not null;
    }

    internal SceneBounds2D GetMapBounds()
    {
        EnsureSpatialIndexes();
        return spatialIndex?.Bounds ?? SceneBounds2D.Empty;
    }

    private void BeginCacheFrame()
    {
        cacheFrameVersion++;
        renderedChunkKeys.Clear();
        EnsureSpatialIndexes();
    }

    private void CompleteCacheFrame()
    {
        staleChunkCacheKeys.Clear();
        long retainedBytes = 0;
        int retainedObjects = 0;
        foreach ((TileChunkCacheKey key, TileChunkCacheEntry entry) in chunkCache)
        {
            if (!renderedChunkKeys.Contains(key) && !warmChunkKeys.Contains(key))
            {
                staleChunkCacheKeys.Add(key);
                continue;
            }
            retainedBytes += entry.RetainedBytes;
            retainedObjects += entry.RetainedObjects;
        }
        RemoveStaleCachedChunks();
        diagnostics = diagnostics with { RetainedBytes = retainedBytes, RetainedObjects = retainedObjects };
    }

    private void ClearTileCache()
    {
        cacheFrameVersion++;
        staleChunkCacheKeys.Clear();
        staleChunkCacheKeys.AddRange(chunkCache.Keys);
        List<Exception>? failures = null;
        try { RemoveStaleCachedChunks(); }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        renderedChunkKeys.Clear();
        warmChunkKeys.Clear();
        ClearWarmCandidates();
        try { ClearWarmAcquisitions(); }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        try { RetireUnusedData(); }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        if (failures is not null) { throw new AggregateException(failures); }
    }

    private void RecordCachedMap(Color tint, Scene2DRecordContext context, SceneBounds2D visibleBounds)
    {
        foreach (TileRenderChunk chunk in spatialIndex!.Query(visibleBounds))
        {
            diagnostics = diagnostics with { CandidateChunks = diagnostics.CandidateChunks + 1 };
            if (!IntersectsChunk(visibleBounds, chunk)) { continue; }
            diagnostics = diagnostics with
            {
                VisibleChunks = diagnostics.VisibleChunks + 1,
                CandidateTiles = diagnostics.CandidateTiles + chunk.Count
            };
            TileChunkCacheKey key = chunk.GetKey();
            renderedChunkKeys.Add(key);
            bool reused = chunkCache.TryGetValue(key, out TileChunkCacheEntry? entry) && entry.IsCurrent(this, chunk, tint);
            if (!reused)
            {
                TileChunkCacheEntry? previous = entry;
                entry = BuildChunkEntry(chunk, tint);
                chunkCache[key] = entry;
                previous?.Dispose();
            }
            TileChunkCacheEntry current = entry!;
            diagnostics = diagnostics with
            {
                BatchesBuilt = diagnostics.BatchesBuilt + (reused ? 0 : current.Batches.Length),
                BatchesRebuilt = diagnostics.BatchesRebuilt + (reused ? 0 : current.Batches.Length),
                BatchesReused = diagnostics.BatchesReused + (reused ? current.Batches.Length : 0),
                DrawnTiles = diagnostics.DrawnTiles + current.StaticTileCount
            };
            foreach (DrawSpriteBatch batch in current.Batches)
            {
                context.Frame.DrawSpriteBatch(batch);
                diagnostics = diagnostics with { DrawCommands = diagnostics.DrawCommands + 1 };
            }
        }
    }

    private void EnsureSpatialIndexes()
    {
        TileMapCatalog2D? catalog = Catalog;
        if (ReferenceEquals(indexedCatalog, catalog)) { return; }
        if (catalog is null)
        {
            ClearWarmCandidates();
            spatialIndex = null;
        }
        else if (spatialIndex is not null && spatialIndex.IsCompatible(catalog))
        {
            spatialIndex.UpdateChunks(catalog);
        }
        else
        {
            ClearWarmCandidates();
            int start = 0;
            TileRenderChunk[] chunks = new TileRenderChunk[catalog.Chunks.Count];
            for (int index = 0; index < chunks.Length; index++)
            {
                TileMapChunkInfo2D info = catalog.Chunks[index];
                chunks[index] = new(info, start);
                start += info.TileCount;
            }
            spatialIndex = new(chunks, catalog.TileSize);
        }
        indexedCatalog = catalog;
    }

    private static bool IntersectsChunk(SceneBounds2D visibleBounds, TileRenderChunk chunk)
    {
        if (visibleBounds.Kind == SceneBoundsKind.Empty) { return false; }
        if (visibleBounds.Kind == SceneBoundsKind.Unknown) { return true; }
        DrawRect rect = chunk.Bounds.Bounds, viewport = visibleBounds.Bounds;
        return rect.X < viewport.Right && rect.Right > viewport.X && rect.Y < viewport.Bottom && rect.Bottom > viewport.Y;
    }

    private TileChunkCacheEntry BuildChunkEntry(TileRenderChunk chunk, Color tint)
    {
        if (!TryGetResidentData(chunk, out TileMapChunkData2D? data))
        {
            throw new InvalidOperationException($"Tile chunk '{chunk.Info.Spatial.Id}' must be prepared before recording.");
        }
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        TileMapCatalog2D catalog = Catalog!;
        List<TileAtlasBatchBuilder> batches = [];
        List<TileAtlasDependencyStamp> dependencies = [];
        int staticTileCount = 0;
        for (int index = 0; index < chunk.Count; index++)
        {
            TileChunk2D? grid = data.Grid;
            ImageReference imageKey;
            DrawRect destination, source;
            TileFlip2D flip;
            if (grid is not null)
            {
                TileCell2D cell = grid.Tiles[index];
                if (cell.TileId == 0 || !data.TryResolveTile(cell.TileId, out TileSet2D? tileSet, out TileDefinition2D? definition) ||
                    tileSet is null || definition is null) { continue; }
                TileCoordinate2D coordinate = new(grid.Origin.X + index % grid.Width, grid.Origin.Y + index / grid.Width);
                imageKey = data.GetTileSetImage(tileSet.Id);
                destination = new(coordinate.X * catalog.TileSize.Width, coordinate.Y * catalog.TileSize.Height,
                    catalog.TileSize.Width, catalog.TileSize.Height);
                source = definition.SourceRect;
                flip = cell.Flip;
            }
            else
            {
                Tile tile = data.Placements[index];
                imageKey = tile.Image;
                DrawSize size = resolvedAtlases[imageKey].Size;
                destination = catalog.GetPlacementDestination(tile);
                source = new(0, 0, size.Width, size.Height);
                flip = TileFlip2D.None;
            }
            ResolvedAtlas atlas = resolvedAtlases[imageKey];
            AddDependency(imageKey, atlas);
            if (atlas.Image is null) { continue; }
            // Free placements preserve painter order: merge only adjacent runs.
            TileAtlasBatchBuilder? batch = null;
            int firstCandidate = grid is not null ? 0 : Math.Max(0, batches.Count - 1);
            for (int candidateIndex = firstCandidate; candidateIndex < batches.Count; candidateIndex++)
            {
                if (ReferenceEquals(batches[candidateIndex].Image, atlas.Image)) { batch = batches[candidateIndex]; break; }
            }
            if (batch is null) { batch = new(atlas.Image); batches.Add(batch); }
            batch.Sprites.Add(TileFlipGeometry2D.Sprite(destination, source, tint, flip));
            staticTileCount++;
        }
        DrawSpriteBatch[] copiedBatches = batches.Select(static batch => new DrawSpriteBatch(batch.Image, batch.Sprites)).ToArray();
        TileAtlasDependencyStamp[] copiedDependencies = dependencies.ToArray();
        long retainedBytes = 4096L + staticTileCount * 128L + copiedBatches.Length * 32L + copiedDependencies.Length * 32L;
        int retainedObjects = 4 + copiedBatches.Length + staticTileCount;
        ImageResourceLeaseSet leases = new();
        try
        {
            using (leases.Begin())
            {
                foreach (DrawSpriteBatch batch in copiedBatches) { leases.Retain(batch.Image, Root?.ImageResourceCache); }
            }
            TileChunkCacheEntry result = new(chunk.Version, chunk.Info.Cells, catalog.TileSize, tint,
                copiedDependencies, copiedBatches, staticTileCount, retainedBytes, retainedObjects, leases);
            result.AllocationCharge = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            chunk.PreparationCharge = result.AllocationCharge;
            return result;
        }
        catch (Exception failure)
        {
            try { leases.Clear(); }
            catch (Exception releaseFailure) { throw new AggregateException(failure, releaseFailure); }
            throw;
        }

        void AddDependency(ImageReference key, ResolvedAtlas atlas)
        {
            foreach (TileAtlasDependencyStamp candidate in dependencies) { if (candidate.Key == key) { return; } }
            DrawSize natural = catalog.IsFreePlacement && catalog.TryGetImageSize(key, out DrawSize size) ? size : default;
            dependencies.Add(new(key, atlas.Version, atlas.Image, atlas.Size, natural));
        }
    }

    private sealed class TileChunkCacheEntry(long chunkVersion, TileMapBounds2D? cells, DrawSize tileSize,
        Color tint, TileAtlasDependencyStamp[] atlasDependencies, DrawSpriteBatch[] batches, int staticTileCount,
        long retainedBytes, int retainedObjects, ImageResourceLeaseSet leases) : IDisposable
    {
        internal DrawSpriteBatch[] Batches { get; } = batches;
        internal int StaticTileCount { get; } = staticTileCount;
        internal long RetainedBytes { get; } = retainedBytes;
        internal int RetainedObjects { get; } = retainedObjects;
        internal long AllocationCharge { get; set; }
        public void Dispose() => leases.Clear();

        internal bool IsCatalogCurrent(TileMap2D owner, TileRenderChunk chunk, Color currentTint)
        {
            TileMapCatalog2D catalog = owner.Catalog!;
            if (chunkVersion != chunk.Version || cells != chunk.Info.Cells || tileSize != catalog.TileSize || tint != currentTint) { return false; }
            foreach (TileAtlasDependencyStamp dependency in atlasDependencies)
            {
                if (!chunk.ImageKeys.Contains(dependency.Key)) { return false; }
                if (catalog.IsFreePlacement)
                {
                    DrawSize natural = catalog.TryGetImageSize(dependency.Key, out DrawSize size) ? size : default;
                    if (natural != dependency.NaturalSize) { return false; }
                }
            }
            return true;
        }

        internal bool IsCurrent(TileMap2D owner, TileRenderChunk chunk, Color currentTint)
        {
            if (!IsCatalogCurrent(owner, chunk, currentTint)) { return false; }
            foreach (TileAtlasDependencyStamp dependency in atlasDependencies)
            {
                if (!owner.resolvedAtlases.TryGetValue(dependency.Key, out ResolvedAtlas atlas) ||
                    atlas.Version != dependency.AtlasResourceVersion ||
                    atlas.Size != dependency.Size || !ReferenceEquals(atlas.Image, dependency.Image)) { return false; }
            }
            return true;
        }
    }

    internal readonly record struct TileChunkCacheKey(string Id);
    private readonly record struct TileAtlasDependencyStamp(ImageReference Key, long AtlasResourceVersion,
        IDrawImage? Image, DrawSize Size, DrawSize NaturalSize);
    private sealed class TileAtlasBatchBuilder(IDrawImage image)
    {
        internal IDrawImage Image { get; } = image;
        internal List<DrawSprite2D> Sprites { get; } = [];
    }
    private readonly record struct TileSpatialBucket(long X, long Y);

    // The complete index retains only headers, never cells, placements or leases.
    internal sealed class TileRenderChunk
    {
        internal TileRenderChunk(TileMapChunkInfo2D info, int start)
        {
            Info = info;
            Start = start;
        }
        internal TileMapChunkInfo2D Info { get; private set; }
        internal int Start { get; }
        internal int Count => Info.TileCount;
        internal SceneBounds2D Bounds => SceneBounds2D.Known(Info.Spatial.Bounds);
        internal SceneBounds2D CollisionBounds => Info.Spatial.CollisionBounds is DrawRect bounds ? SceneBounds2D.Known(bounds) : SceneBounds2D.Empty;
        internal long Version => Info.Spatial.Version;
        internal long PreparationCharge { get; set; }
        internal IReadOnlyList<ImageReference> ImageKeys => Info.Images;
        internal TileChunkCacheKey GetKey() => new(Info.Spatial.Id);

        internal void Update(TileMapChunkInfo2D info)
        {
            if (Info.Spatial.Version != info.Spatial.Version) { PreparationCharge = 0; }
            Info = info;
        }
    }

    private sealed class TileMapSpatialIndex
    {
        private readonly TileRenderChunk[] chunks;
        private readonly Dictionary<TileSpatialBucket, int[]> buckets;
        private readonly HashSet<int> candidateIndices = [];
        private readonly List<int> orderedCandidateIndices = [];
        private readonly List<TileRenderChunk> queryResult = [];
        private readonly Dictionary<TileChunkCacheKey, TileRenderChunk> chunksByKey;
        private readonly double bucketWidth, bucketHeight;
        private readonly DrawSize tileSize;
        internal IReadOnlyList<TileRenderChunk> Chunks => chunks;
        internal SceneBounds2D Bounds { get; }
        internal bool TryGetChunk(TileChunkCacheKey key, out TileRenderChunk? chunk) => chunksByKey.TryGetValue(key, out chunk);

        internal TileMapSpatialIndex(TileRenderChunk[] chunks, DrawSize tileSize)
        {
            this.chunks = chunks;
            this.tileSize = tileSize;
            chunksByKey = chunks.ToDictionary(static chunk => chunk.GetKey());
            bucketWidth = chunks.Length == 0 ? 1 : Math.Max(1, chunks.Max(static chunk => chunk.Bounds.Bounds.Width));
            bucketHeight = chunks.Length == 0 ? 1 : Math.Max(1, chunks.Max(static chunk => chunk.Bounds.Bounds.Height));
            Bounds = SceneBounds2D.Empty;
            Dictionary<TileSpatialBucket, List<int>> mutableBuckets = [];
            for (int index = 0; index < chunks.Length; index++)
            {
                DrawRect bounds = chunks[index].Bounds.Bounds;
                Bounds = SceneGeometry2D.Union(Bounds, chunks[index].Bounds);
                long minX = FloorToLong(bounds.X / bucketWidth), maxX = FloorToLong(Math.Ceiling(bounds.Right / bucketWidth) - 1);
                long minY = FloorToLong(bounds.Y / bucketHeight), maxY = FloorToLong(Math.Ceiling(bounds.Bottom / bucketHeight) - 1);
                for (long y = minY; y <= maxY; y++)
                for (long x = minX; x <= maxX; x++)
                {
                    TileSpatialBucket key = new(x, y);
                    if (!mutableBuckets.TryGetValue(key, out List<int>? values)) { values = []; mutableBuckets.Add(key, values); }
                    values.Add(index);
                }
            }
            buckets = mutableBuckets.ToDictionary(static pair => pair.Key, static pair => pair.Value.ToArray());
        }

        internal bool IsCompatible(TileMapCatalog2D catalog)
        {
            if (tileSize != catalog.TileSize || catalog.Chunks.Count != chunks.Length) { return false; }
            for (int index = 0; index < chunks.Length; index++)
            {
                TileMapChunkInfo2D current = chunks[index].Info, candidate = catalog.Chunks[index];
                if (current.Spatial.Id != candidate.Spatial.Id || current.Spatial.Bounds != candidate.Spatial.Bounds ||
                    current.Cells != candidate.Cells || current.TileCount != candidate.TileCount) { return false; }
            }
            return true;
        }

        internal void UpdateChunks(TileMapCatalog2D catalog)
        {
            for (int index = 0; index < chunks.Length; index++) { chunks[index].Update(catalog.Chunks[index]); }
        }

        internal IReadOnlyList<TileRenderChunk> Query(SceneBounds2D visible)
        {
            queryResult.Clear();
            if (visible.Kind == SceneBoundsKind.Empty || chunks.Length == 0) { return queryResult; }
            if (visible.Kind == SceneBoundsKind.Unknown) { queryResult.AddRange(chunks); return queryResult; }
            DrawRect bounds = visible.Bounds;
            long minX = FloorToLong(((double)bounds.X - tileSize.Width) / bucketWidth), maxX = FloorToLong(bounds.Right / bucketWidth);
            long minY = FloorToLong(((double)bounds.Y - tileSize.Height) / bucketHeight), maxY = FloorToLong(bounds.Bottom / bucketHeight);
            if (maxX < minX || maxY < minY) { queryResult.AddRange(chunks); return queryResult; }
            ulong width = (ulong)(maxX - minX) + 1, height = (ulong)(maxY - minY) + 1;
            ulong maximum = (ulong)Math.Max(1, buckets.Count) * 4UL;
            if (width > maximum || height > maximum || width * height > maximum) { queryResult.AddRange(chunks); return queryResult; }
            candidateIndices.Clear();
            orderedCandidateIndices.Clear();
            for (long y = minY; ; y++)
            {
                for (long x = minX; ; x++)
                {
                    if (buckets.TryGetValue(new(x, y), out int[]? values))
                    {
                        foreach (int index in values) { if (candidateIndices.Add(index)) { orderedCandidateIndices.Add(index); } }
                    }
                    if (x == maxX) { break; }
                }
                if (y == maxY) { break; }
            }
            orderedCandidateIndices.Sort();
            foreach (int index in orderedCandidateIndices) { queryResult.Add(chunks[index]); }
            return queryResult;
        }

        private static long FloorToLong(double value) => double.IsNaN(value) ? 0 :
            value <= long.MinValue ? long.MinValue : value >= long.MaxValue ? long.MaxValue : (long)Math.Floor(value);
    }
}
