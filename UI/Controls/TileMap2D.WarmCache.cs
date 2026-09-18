using Cerneala.Drawing;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Controls;

public sealed partial class TileMap2D
{
    internal const long WarmCacheBudgetBytes = 1_048_576;
    private readonly HashSet<TileChunkCacheKey> warmChunkKeys = [];
    private readonly HashSet<TileChunkCacheKey> warmDataKeys = [];
    private readonly Dictionary<TileChunkCacheKey, WarmAcquisition> warmAcquisitions = [];
    private readonly List<WarmCandidate> warmCandidates = [];
    private DrawRect? warmCandidateView;
    private readonly HashSet<IDrawImage> requiredImages = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<IDrawImage> warmImages = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<IDrawImage> candidateImages = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<ImageReference> requiredReferences = [];
    private readonly HashSet<ImageReference> warmReferences = [];
    private readonly HashSet<ImageReference> candidateReferences = [];
    private bool maintainingWarm;
    private readonly ImageResourceLeaseSet warmProbeImages = new();

    internal readonly record struct WarmPreparationRequest(
        TileMap2D Owner, long CacheFrameVersion, int RenderVersion, Color Tint)
    {
        internal int Complete(int tileBudget, bool prepare = true) => Owner.CompleteWarmCacheFrame(this, tileBudget, prepare);
    }

    private int CompleteWarmCacheFrame(WarmPreparationRequest request, int tileBudget, bool prepare)
    {
        if (cacheFrameVersion != request.CacheFrameVersion) { return 0; }
        cacheFrameVersion++;
        List<Exception>? failures = null;
        int prepared = 0;
        try
        {
            // Required ownership changes in the spatial update, not recording.
            // A later camera change may draw already-resident data, but cannot
            // retire its warm lease using a different viewport from that owner.
            bool canPrepare = prepare && RenderVersion == request.RenderVersion;
            prepared = MaintainWarmChunks(requestedViewport, request.Tint,
                canPrepare ? tileBudget : 0, allowPreparation: canPrepare);
        }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        try { CompleteCacheFrame(); }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        try { RetireUnusedData(); }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        if (failures is not null) { throw new AggregateException(failures); }
        return prepared;
    }

    private int MaintainWarmChunks(SceneBounds2D visibleBounds, Color tint, int tileBudget, bool allowPreparation)
    {
        if (maintainingWarm) { return 0; }
        if (Catalog is not { } catalog || SimulationContext is not { IsDisposed: false, IsHeadless: false } ||
            visibleBounds.Kind != SceneBoundsKind.Known || !IsPresentationVisible(catalog))
        {
            ClearWarmAcquisitions();
            warmChunkKeys.Clear();
            SetWarmDiagnostics(0, 0, 0, 0, 0, 0);
            return 0;
        }
        DrawRect view = visibleBounds.Bounds;
        DrawRect expanded = new(view.X - view.Width / 2, view.Y - view.Height / 2, view.Width * 2, view.Height * 2);
        if (!float.IsFinite(expanded.X) || !float.IsFinite(expanded.Y) ||
            !float.IsFinite(expanded.Right) || !float.IsFinite(expanded.Bottom))
        {
            ClearWarmAcquisitions();
            warmChunkKeys.Clear();
            SetWarmDiagnostics(0, 0, 0, 0, 0, 0);
            return 0;
        }
        maintainingWarm = true;
        long generation = sourceGeneration;
        int preparedTiles = 0, preparedBatches = 0;
        long charged = 0, dataBytes = 0, imageBytes = 0, estimatedBytes = 0;
        try
        {
            warmChunkKeys.Clear();
            warmDataKeys.Clear();
            requiredImages.Clear();
            requiredReferences.Clear();
            warmImages.Clear();
            warmReferences.Clear();
            foreach (TileChunkCacheKey key in visualChunkKeys)
            {
                if (spatialIndex!.TryGetChunk(key, out TileRenderChunk? chunk))
                {
                    foreach (ImageReference imageKey in chunk!.ImageKeys)
                    {
                        requiredReferences.Add(imageKey);
                        if (resolvedAtlases.TryGetValue(imageKey, out ResolvedAtlas atlas) && atlas.Image is { } image) { requiredImages.Add(image); }
                    }
                }
            }
            UpdateWarmCandidates(view, visibleBounds, SceneBounds2D.Known(expanded));
            foreach (WarmCandidate candidate in warmCandidates.ToArray())
            {
                if (generation != sourceGeneration || !ReferenceEquals(Catalog, catalog)) { return preparedTiles; }
                TileRenderChunk chunk = candidate.Chunk;
                TileChunkCacheKey key = chunk.GetKey();
                bool requiredData = requiredDataKeys.Contains(key);
                if (!requiredData && chunk.Info.DataResidencyBytes is null) { continue; }
                long dataCharge = requiredData ? 0 : chunk.Info.DataResidencyBytes!.Value;
                if (dataCharge > WarmCacheBudgetBytes - charged) { continue; }
                warmAcquisitions.TryGetValue(key, out WarmAcquisition? warm);
                if (warm?.Error is not null) { continue; }
                chunkCache.TryGetValue(key, out TileChunkCacheEntry? entry);
                bool hasData = TryGetResidentData(chunk, out _);
                bool canWork = allowPreparation && chunk.Count <= tileBudget - preparedTiles;
                if (!hasData && warm?.Request is null && !canWork) { continue; }

                // Probe without starting a decode. Existing pending acquisitions
                // remain alive; unknown-size cold images do not qualify for preload.
                bool imagesReady = ResolveWarmAtlases(chunk, warm, prepare: false, out Exception? imageError);
                if (imageError is not null)
                {
                    if (warm is not null) { warm.Error = imageError; warm.Images.Clear(); }
                    continue;
                }
                long additionalImages = GetAdditionalWarmImageCharge(chunk, catalog, out bool knownImages);
                if (!knownImages) { continue; }
                bool reused = entry is not null && entry.IsCurrent(this, chunk, tint);
                long constructionCharge = reused ? entry!.AllocationCharge : chunk.PreparationCharge;
                if (constructionCharge > WarmCacheBudgetBytes - charged - dataCharge ||
                    additionalImages > WarmCacheBudgetBytes - charged - dataCharge - constructionCharge) { continue; }
                if (!hasData && warm?.Request is null || !imagesReady && warm is null)
                {
                    if (!canWork) { continue; }
                }
                if (warm is null)
                {
                    warm = new(key, catalog);
                    warmAcquisitions.Add(key, warm);
                }
                warmDataKeys.Add(key);
                bool didWork = false;
                if (!hasData && warm.Request is null && canWork)
                {
                    didWork = true;
                    StartWarmAcquisition(chunk, catalog, warm);
                    if (generation != sourceGeneration || !ReferenceEquals(Catalog, catalog)) { return preparedTiles + chunk.Count; }
                    hasData = TryGetResidentData(chunk, out _);
                }
                if (warm.Error is not null) { warmDataKeys.Remove(key); continue; }
                if (!imagesReady && canWork && hasData && !warm.Images.HasPendingAcquisitions)
                {
                    didWork = true;
                    imagesReady = ResolveWarmAtlases(chunk, warm, prepare: true, out imageError);
                    if (imageError is not null)
                    {
                        warm.Error = imageError;
                        warm.Images.Clear();
                        warmDataKeys.Remove(key);
                    }
                    additionalImages = GetAdditionalWarmImageCharge(chunk, catalog, out knownImages);
                }
                if (didWork) { preparedTiles += chunk.Count; }
                if (!warmDataKeys.Contains(key)) { continue; }
                if (additionalImages > WarmCacheBudgetBytes - charged - dataCharge - constructionCharge)
                {
                    warmDataKeys.Remove(key);
                    continue;
                }
                if (hasData && imagesReady && !reused && (didWork || canWork))
                {
                    // Data may have arrived inline after the earlier image probe.
                    // Validate its newly loaded definitions before any optional batch.
                    try { ValidateResidentAtlases(chunk); }
                    catch (ArgumentException failure)
                    {
                        warm.Error = failure;
                        warm.Images.Clear();
                        warmDataKeys.Remove(key);
                        continue;
                    }
                    TileChunkCacheEntry? previous = entry;
                    entry = BuildChunkEntry(chunk, tint);
                    chunkCache[key] = entry;
                    previous?.Dispose();
                    if (!didWork) { preparedTiles += chunk.Count; }
                    preparedBatches += entry.Batches.Length;
                    reused = true;
                    constructionCharge = entry.AllocationCharge;
                }
                if (constructionCharge > WarmCacheBudgetBytes - charged - dataCharge ||
                    additionalImages > WarmCacheBudgetBytes - charged - dataCharge - constructionCharge)
                {
                    warmDataKeys.Remove(key);
                    continue;
                }
                if (reused && imagesReady && hasData)
                {
                    warmChunkKeys.Add(key);
                    estimatedBytes += entry!.RetainedBytes;
                    // The batch owns its independent image acquisitions.
                    warm.Images.Clear();
                }
                charged += dataCharge + constructionCharge + additionalImages;
                dataBytes += dataCharge;
                imageBytes += additionalImages;
                warmImages.UnionWith(candidateImages);
                warmReferences.UnionWith(candidateReferences);
            }
            if (generation == sourceGeneration && ReferenceEquals(Catalog, catalog))
            {
                RetireUnselectedWarmAcquisitions();
                SetWarmDiagnostics(charged, dataBytes, imageBytes, estimatedBytes, preparedTiles, preparedBatches);
            }
            return preparedTiles;
        }
        finally
        {
            candidateImages.Clear();
            candidateReferences.Clear();
            requiredImages.Clear();
            requiredReferences.Clear();
            warmImages.Clear();
            warmReferences.Clear();
            maintainingWarm = false;
        }
    }

    private void SetWarmDiagnostics(long charge, long dataBytes, long imageBytes, long retained, int tiles, int batches)
    {
        diagnostics = diagnostics with
        {
            WarmChunks = warmChunkKeys.Count, WarmDataBytes = dataBytes,
            WarmPendingChunks = warmDataKeys.Count - warmChunkKeys.Count,
            WarmBatchesPrepared = batches, WarmTilesPrepared = tiles, WarmRetainedBytes = retained,
            WarmChargedBytes = charge, WarmImageBytes = imageBytes,
            BatchesBuilt = diagnostics.BatchesBuilt + batches
        };
    }

    private void StartWarmAcquisition(TileRenderChunk chunk, TileMapCatalog2D catalog, WarmAcquisition warm)
    {
        MapRequest request = new(SimulationContext!, requestVersion, catalog);
        warm.Request = request;
        ValueTask<SceneSpatialRegion2D<TileMapChunkData2D>> acquisition;
        try { acquisition = residency!.AcquireEntriesAsync(catalog.Entries, [chunk.Info.Spatial], request.Token); }
        catch (Exception failure) { acquisition = ValueTask.FromException<SceneSpatialRegion2D<TileMapChunkData2D>>(failure); }
        warm.Preparation = CompleteRequestAsync(request, acquisition, warm);
        _ = ObservePreparationAsync(warm.Preparation);
    }

    private bool ResolveWarmAtlases(TileRenderChunk chunk, WarmAcquisition? warm, bool prepare, out Exception? error)
    {
        error = null;
        bool ready = true;
        ImageResourceLeaseSet leases = warm?.Images ?? warmProbeImages;
        using ImageResourceLeaseSet.Scope usage = leases.Begin();
        try
        {
            foreach (ImageReference key in chunk.ImageKeys)
            {
                if (resolvedAtlases.TryGetValue(key, out ResolvedAtlas existing) && existing.Image is not null &&
                    requiredImageKeys.Contains(key)) { continue; }
                ImageReference reference = key;
                ImageResourceResolution resolution = reference.ResourceId is { } resource
                    ? ImageResourceResolver.Resolve(this, resource, null, null, InvalidationFlags.Render,
                        affectsIntrinsicSize: false, acquisitions: leases,
                        access: prepare ? ImageResourceAccess.Prepare : ImageResourceAccess.ResidentOnly)
                    : default;
                if (resolution.Error is not null) { error = resolution.Error; return false; }
                IDrawImage? image = reference.DirectImage ?? resolution.Image;
                if (image is null && reference.ResourceId is not null)
                {
                    // ResidentOnly does not start a decode and does not expose
                    // a pending preparation's error. Prepare reports it later.
                    ready = false;
                }
                ResolvedAtlas atlas = new(image, resolution.Version);
                resolvedAtlases[key] = atlas;
                if (image is not null && key.ResourceId is { } id)
                {
                    resolvedAtlasSizes[id.Key] = atlas.Size;
                }
            }
            try { ValidateResidentAtlases(chunk); }
            catch (ArgumentException failure) { error = failure; return false; }
            return ready;
        }
        finally { if (warm is null) { leases.Clear(); } }
    }

    private long GetAdditionalWarmImageCharge(TileRenderChunk chunk, TileMapCatalog2D catalog, out bool known)
    {
        candidateImages.Clear();
        candidateReferences.Clear();
        known = true;
        long bytes = 0;
        foreach (ImageReference key in chunk.ImageKeys)
        {
            ImageReference reference = key;
            DrawSize size;
            if (resolvedAtlases.TryGetValue(key, out ResolvedAtlas atlas) && atlas.Image is { } image)
            {
                if (requiredImages.Contains(image) || warmImages.Contains(image) || !candidateImages.Add(image)) { continue; }
                size = atlas.Size;
            }
            else
            {
                if (requiredReferences.Contains(reference) || warmReferences.Contains(reference) || !candidateReferences.Add(reference)) { continue; }
                if (!catalog.TryGetImageSize(reference, out size)) { known = false; return 0; }
            }
            // Reserved declared sizes and actual resident RGBA sizes are both
            // conservative logical charges, not a process/GPU memory census.
            double charge = Math.Ceiling(size.Width) * Math.Ceiling(size.Height) * 4;
            if (charge > WarmCacheBudgetBytes - bytes) { return WarmCacheBudgetBytes + 1; }
            bytes += (long)charge;
        }
        return bytes;
    }

    private void UpdateWarmCandidates(DrawRect view, SceneBounds2D visible, SceneBounds2D warmBounds)
    {
        if (warmCandidateView == view) { return; }
        ClearWarmCandidates();
        foreach (TileRenderChunk chunk in spatialIndex!.Query(warmBounds))
        {
            if (IntersectsChunk(visible, chunk) || !IntersectsChunk(warmBounds, chunk)) { continue; }
            DrawRect bounds = chunk.Bounds.Bounds;
            double dx = Math.Max(0, Math.Max((double)view.X - bounds.Right, (double)bounds.X - view.Right));
            double dy = Math.Max(0, Math.Max((double)view.Y - bounds.Bottom, (double)bounds.Y - view.Bottom));
            warmCandidates.Add(new(chunk, dx * dx + dy * dy, warmCandidates.Count));
        }
        warmCandidates.Sort(static (left, right) =>
        {
            int distance = left.Distance.CompareTo(right.Distance);
            return distance != 0 ? distance : left.Order.CompareTo(right.Order);
        });
        warmCandidateView = view;
    }

    private void ClearWarmCandidates() { warmCandidateView = null; warmCandidates.Clear(); }

    private WarmAcquisition[] DetachWarmAcquisitions()
    {
        WarmAcquisition[] result = warmAcquisitions.Values.ToArray();
        warmAcquisitions.Clear();
        warmDataKeys.Clear();
        return result;
    }

    private void ClearWarmAcquisitions()
    {
        List<Exception>? failures = null;
        foreach (WarmAcquisition warm in DetachWarmAcquisitions())
        {
            try { warm.Dispose(); }
            catch (Exception failure) { (failures ??= []).Add(failure); }
        }
        if (failures is not null) { throw new AggregateException(failures); }
    }

    private void RetireUnselectedWarmAcquisitions()
    {
        HashSet<TileChunkCacheKey> candidates = warmCandidates.Select(static candidate => candidate.Chunk.GetKey()).ToHashSet();
        WarmAcquisition[] obsolete = warmAcquisitions.Values
            .Where(warm => !warmDataKeys.Contains(warm.Key) && (warm.Error is null || !candidates.Contains(warm.Key))).ToArray();
        foreach (WarmAcquisition warm in obsolete) { warmAcquisitions.Remove(warm.Key); }
        List<Exception>? failures = null;
        foreach (WarmAcquisition warm in obsolete)
        {
            try { warm.Dispose(); }
            catch (Exception failure) { (failures ??= []).Add(failure); }
        }
        if (failures is not null) { throw new AggregateException(failures); }
    }

    private void ClearObsoleteWarmAcquisitions(TileMapCatalog2D catalog)
    {
        List<WarmAcquisition> obsolete = [];
        foreach (WarmAcquisition warm in warmAcquisitions.Values)
        {
            if (warm.Catalog.TryGetChunk(warm.Key.Id, out TileMapChunkInfo2D? before) &&
                catalog.TryGetChunk(warm.Key.Id, out TileMapChunkInfo2D? after) &&
                before!.Spatial.Version == after!.Spatial.Version &&
                CanReusePayloadValidation(warm.Catalog, catalog, before, after))
            {
                warm.Catalog = catalog;
                warm.Error = null;
            }
            else { obsolete.Add(warm); }
        }
        foreach (WarmAcquisition warm in obsolete) { warmAcquisitions.Remove(warm.Key); warmDataKeys.Remove(warm.Key); }
        List<Exception>? failures = null;
        foreach (WarmAcquisition warm in obsolete)
        {
            try { warm.Dispose(); }
            catch (Exception failure) { (failures ??= []).Add(failure); }
        }
        if (failures is not null) { throw new AggregateException(failures); }
    }

    private void RetireObsoleteRenderChunks()
    {
        cacheFrameVersion++;
        staleChunkCacheKeys.Clear();
        foreach ((TileChunkCacheKey key, TileChunkCacheEntry entry) in chunkCache)
        {
            if (Catalog is null || spatialIndex is null || !spatialIndex.TryGetChunk(key, out TileRenderChunk? chunk) ||
                chunk is null || !entry.IsCatalogCurrent(this, chunk, Multiply(Catalog.Tint, Tint))) { staleChunkCacheKeys.Add(key); }
        }
        RemoveStaleCachedChunks();
    }

    private void RetireUnusedRenderChunks()
    {
        staleChunkCacheKeys.Clear();
        foreach (TileChunkCacheKey key in chunkCache.Keys)
        {
            if (!visualChunkKeys.Contains(key) && !warmChunkKeys.Contains(key)) { staleChunkCacheKeys.Add(key); }
        }
        RemoveStaleCachedChunks();
    }

    private void RemoveStaleCachedChunks()
    {
        List<TileChunkCacheEntry> removed = [];
        foreach (TileChunkCacheKey key in staleChunkCacheKeys)
        {
            warmChunkKeys.Remove(key);
            if (chunkCache.Remove(key, out TileChunkCacheEntry? entry)) { removed.Add(entry); }
        }
        staleChunkCacheKeys.Clear();
        List<Exception>? failures = null;
        foreach (TileChunkCacheEntry entry in removed)
        {
            try { entry.Dispose(); }
            catch (Exception failure) { (failures ??= []).Add(failure); }
        }
        if (failures is not null) { throw new AggregateException(failures); }
    }

    private readonly record struct WarmCandidate(TileRenderChunk Chunk, double Distance, int Order);

    private sealed class WarmAcquisition(TileChunkCacheKey key, TileMapCatalog2D catalog) : IDisposable
    {
        internal TileChunkCacheKey Key { get; } = key;
        internal TileMapCatalog2D Catalog = catalog;
        internal MapRequest? Request;
        internal Task Preparation = Task.CompletedTask;
        internal Exception? Error;
        internal ImageResourceLeaseSet Images { get; } = new();

        public void Dispose()
        {
            try { Request?.Cancel(); }
            finally { Images.Clear(); }
        }
    }
}
