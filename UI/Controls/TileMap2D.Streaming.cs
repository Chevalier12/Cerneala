using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Relay;

namespace Cerneala.UI.Controls;

public sealed partial class TileMap2D
{
    private TileMapSource2D? observedSource;
    private SceneSpatialResidency2D<TileMapChunkData2D>? residency;
    private TileMapCatalog2D? appliedCatalog;
    private TileMapCatalog2D? requestedCatalog;
    private readonly Dictionary<TileChunkCacheKey, ResidentChunk> residentData = [];
    private readonly HashSet<TileChunkCacheKey> requiredDataKeys = [];
    private readonly HashSet<TileChunkCacheKey> visualChunkKeys = [];
    private SceneBounds2D requestedViewport;
    private IReadOnlyList<SceneBounds2D>? requestedCollisionInterests;
    private Matrix3x2 requestedTransform;
    private bool requestedVisibility;
    private readonly Dictionary<TileChunkCacheKey, Task> requiredPreparations = [];
    private readonly Dictionary<TileChunkCacheKey, Exception> requiredErrors = [];
    private readonly Dictionary<TileChunkCacheKey, MapRequest> requiredRequests = [];
    private long requestVersion;
    private long sourceGeneration;
    private readonly int creatorThreadId = Environment.CurrentManagedThreadId;
    private readonly List<Task> retiredGenerationDrains = [];
    private UiRelay? lastOwnerRelay;
    private Task? terminalDisposal;

    public Task Preparation { get; private set; } = Task.CompletedTask;
    public Exception? PreparationError { get; private set; }

    SceneNode2D ISceneSpatialParticipant2D.Node => this;
    Task ISceneSpatialParticipant2D.Preparation => Preparation;
    Task ISceneSpatialParticipant2D.GetCollisionPreparation(DrawRect bounds) => GetCollisionPreparation(bounds);
    void ISceneSpatialParticipant2D.UpdateSpatialInterest(SceneBounds2D visibleBounds, IReadOnlyList<SceneBounds2D> collisionInterest,
        DrawRect? surfaceBounds) => UpdateSpatialInterest(visibleBounds, collisionInterest, surfaceBounds);
    string? ISceneSpatialParticipant2D.GetUnpreparedCollisionEntry(DrawRect sceneBounds) => GetUnpreparedCollisionEntry(sceneBounds);

    public void Refresh()
    {
        VerifyMapOwnerAccess();
        ObjectDisposedException.ThrowIf(terminalDisposal is not null, this);
        requestedCatalog = null;
        foreach (WarmAcquisition warm in warmAcquisitions.Values) { warm.Error = null; }
        foreach ((TileChunkCacheKey key, MapRequest request) in requiredRequests.ToArray())
        {
            if (request.Error is not null) { requiredRequests.Remove(key); request.Cancel(); }
        }
        // Publication is authoritative even inside another participant's
        // materializer, when the shared interest sweep is already in progress.
        // Reconcile owned metadata/adapters now; new I/O still belongs to that sweep.
        if (observedSource is { } source && !ReferenceEquals(appliedCatalog, source.Catalog))
        {
            ApplyCatalog(source.Catalog);
        }
        SimulationContext?.RefreshSpatialItems();
    }

    protected override void OnAttached()
    {
        ObjectDisposedException.ThrowIf(terminalDisposal is not null, this);
        base.OnAttached();
        lastOwnerRelay = Root?.Relay ?? lastOwnerRelay;
        SimulationContext?.RefreshSpatialItems();
    }

    internal override void ValidateParentChange(UIElement? parent, ElementChildRole role, bool ownerManaged)
    {
        if (parent is not null) { ObjectDisposedException.ThrowIf(terminalDisposal is not null, this); }
        base.ValidateParentChange(parent, role, ownerManaged);
    }

    private void VerifyMapOwnerAccess()
    {
        if (lastOwnerRelay is { } relay) { relay.VerifyAccess(); }
        else if (Environment.CurrentManagedThreadId != creatorThreadId)
        {
            throw new InvalidOperationException("The map must be accessed on its creator thread before its first attachment.");
        }
        VerifyOwnerAccess();
    }

    public ValueTask DisposeAsync()
    {
        VerifyMapOwnerAccess();
        if (terminalDisposal is { } existing) { return new(existing); }
        if (LogicalParent is not null || VisualParent is not null || Root is not null ||
            SimulationContext is not null || Surface is not null)
        {
            throw new InvalidOperationException("Detach the map before terminal disposal.");
        }

        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        terminalDisposal = completion.Task;
        try
        {
            StopSource();
            source = null;
            _ = CompleteTerminalDisposalAsync(retiredGenerationDrains.ToArray(), completion);
        }
        catch (Exception failure)
        {
            completion.TrySetException(failure);
        }
        return new(completion.Task);
    }

    private static async Task CompleteTerminalDisposalAsync(Task[] retired, TaskCompletionSource completion)
    {
        List<Exception> failures = [];
        foreach (Task drain in retired)
        {
            try { await drain.ConfigureAwait(false); }
            catch (AggregateException aggregate) { failures.AddRange(aggregate.Flatten().InnerExceptions); }
            catch (Exception failure) { failures.Add(failure); }
        }
        if (failures.Count == 0) { completion.TrySetResult(); }
        else { completion.TrySetException(new AggregateException(failures)); }
    }

    internal override void OnSimulationContextChanged(SceneSimulationContext2D? previous)
    {
        lastOwnerRelay = SimulationContext?.Relay ?? previous?.Relay ?? lastOwnerRelay;
        ObjectDisposedException.ThrowIf(terminalDisposal is not null && SimulationContext is not null, this);
        StopSource();
        StartSource();
    }

    private void StartSource()
    {
        if (SimulationContext is not { IsDisposed: false } || observedSource is not null || Source is null) { return; }
        observedSource = Source;
        residency = new(observedSource);
        observedSource.Changed += OnSourceChanged;
    }

    private void OnSourceChanged(object? sender, EventArgs args)
    {
        SceneSimulationContext2D? context = SimulationContext;
        if (context is null || context.IsDisposed) { return; }
        if (context.Relay.CheckAccess())
        {
            if (ReferenceEquals(sender, observedSource)) { Refresh(); }
            return;
        }
        if (sender is not TileMapSource2D publisher) { return; }
        WeakReference<TileMap2D> target = new(this);
        WeakReference<SceneSimulationContext2D> lifetime = new(context);
        WeakReference<TileMapSource2D> source = new(publisher);
        context.Relay.Post(() =>
        {
            if (target.TryGetTarget(out TileMap2D? map) && lifetime.TryGetTarget(out var owner) && !owner.IsDisposed &&
                ReferenceEquals(map.SimulationContext, owner) && source.TryGetTarget(out var current) &&
                ReferenceEquals(map.observedSource, current)) { map.Refresh(); }
        });
    }

    private void UpdateSpatialInterest(SceneBounds2D visibleBounds, IReadOnlyList<SceneBounds2D> collisionInterest,
        DrawRect? surfaceBounds = null)
    {
        if (SimulationContext is not { IsDisposed: false } context || observedSource is null || residency is null) { return; }
        context.Relay.VerifyAccess();
        TileMapSource2D source = observedSource;
        TileMapCatalog2D catalog = source.Catalog;
        bool visible = catalog.IsVisible && UIElementVisibility.IsEffectivelyVisible(this);
        if (context.IsHeadless || !IsPresentationVisible(catalog)) { visibleBounds = SceneBounds2D.Empty; }
        else { visibleBounds = SceneSpatialInterest2D.ResolveInputBounds(this, visibleBounds, surfaceBounds: surfaceBounds); }
        Matrix3x2 transform = GetLocalToSceneTransform(catalog);
        if (ReferenceEquals(requestedCatalog, catalog) && requestedViewport == visibleBounds &&
            ReferenceEquals(requestedCollisionInterests, collisionInterest) && requestedTransform == transform &&
            requestedVisibility == visible) { return; }

        requestedCatalog = catalog;
        requestedViewport = visibleBounds;
        requestedCollisionInterests = collisionInterest;
        requestedTransform = transform;
        requestedVisibility = visible;
        PreparationError = null;
        if (!ReferenceEquals(appliedCatalog, catalog))
        {
            ApplyCatalog(catalog);
            if (!ReferenceEquals(observedSource, source) || !ReferenceEquals(source.Catalog, catalog) ||
                !ReferenceEquals(SimulationContext, context) || context.IsDisposed) { return; }
        }
        EnsureSpatialIndexes();
        requiredDataKeys.Clear();
        visualChunkKeys.Clear();
        List<SceneSpatialEntry2D> entries = [];
        List<SceneBounds2D> localInterests = new(collisionInterest.Count);
        foreach (SceneBounds2D interest in collisionInterest)
        {
            localInterests.Add(interest.Kind == SceneBoundsKind.Known &&
                SceneGeometry2D.TryTransformBoundsToLocal(interest.Bounds, transform, out DrawRect local)
                    ? SceneBounds2D.Known(local) : interest.Kind == SceneBoundsKind.Empty ? interest : SceneBounds2D.Unknown);
        }
        if (visible)
        {
            foreach (TileRenderChunk chunk in spatialIndex!.Chunks)
            {
                bool presentation = IntersectsChunk(visibleBounds, chunk);
                if (presentation) { visualChunkKeys.Add(chunk.GetKey()); }
                bool collision = chunk.Info.ExpandedColliderCount != 0 &&
                    localInterests.Any(interest => IntersectsCollisionBounds(interest, chunk.CollisionBounds));
                if (presentation || collision)
                {
                    requiredDataKeys.Add(chunk.GetKey());
                    entries.Add(chunk.Info.Spatial);
                }
            }
        }

        // Prune presentation acquisitions without loading. ResidentOnly also
        // keeps an already-pending acquisition for the still-required resource.
        ResolveAtlases(catalog, visibleBounds, Cerneala.UI.Resources.ImageResourceAccess.ResidentOnly, validateGeometry: false);
        // Keep only already-owned optional work here. Starting new work belongs
        // exclusively to the surface's shared, fair frame preparation budget.
        MaintainWarmChunks(visibleBounds, Multiply(catalog.Tint, Tint), 0, allowPreparation: false);
        RetireUnusedData();
        RetireUnusedRenderChunks();
        SynchronizeCollisionAdapters();
        if (!ReferenceEquals(observedSource, source) || !ReferenceEquals(source.Catalog, catalog) ||
            !ReferenceEquals(SimulationContext, context) || context.IsDisposed || residency is null) { return; }

        List<MapRequest> obsoleteRequests = [];
        foreach ((TileChunkCacheKey key, MapRequest request) in requiredRequests.ToArray())
        {
            if (!requiredDataKeys.Contains(key) || !ReferenceEquals(request.Catalog, catalog))
            {
                requiredRequests.Remove(key);
                obsoleteRequests.Add(request);
            }
        }
        requiredPreparations.Clear();
        requiredErrors.Clear();
        List<Task> work = new(entries.Count);
        foreach (SceneSpatialEntry2D entry in entries)
        {
            TileChunkCacheKey key = new(entry.Id);
            // The map already owns an independent payload lease. Re-evaluating
            // interest must not reacquire every unchanged, validated resident.
            if (residentData.TryGetValue(key, out ResidentChunk? resident) &&
                ReferenceEquals(resident.ValidatedCatalog, catalog)) { continue; }
            if (requiredRequests.TryGetValue(key, out MapRequest? retained))
            {
                requiredPreparations[key] = retained.Preparation;
                work.Add(retained.Preparation);
                if (retained.Error is { } failure)
                {
                    requiredErrors[key] = failure;
                    PreparationError ??= failure;
                }
                continue;
            }
            MapRequest request = new(context, ++requestVersion, catalog, key);
            requiredRequests.Add(key, request);
            ValueTask<SceneSpatialRegion2D<TileMapChunkData2D>> acquisition;
            try { acquisition = residency.AcquireEntriesAsync(catalog.Entries, [entry], request.Token); }
            catch (Exception failure) { acquisition = ValueTask.FromException<SceneSpatialRegion2D<TileMapChunkData2D>>(failure); }
            Task task = CompleteRequestAsync(request, acquisition, requiredKey: key);
            request.Preparation = task;
            work.Add(task);
            if (IsCurrent(request, null)) { requiredPreparations[key] = task; }
        }
        // A request belongs to one entry, not one camera/region sweep. Adding
        // an unrelated region must not cancel another region's pending task.
        foreach (MapRequest request in obsoleteRequests) { request.Cancel(); }
        Preparation = Task.WhenAll(work);
        _ = ObservePreparationAsync(Preparation);
    }

    private Matrix3x2 GetLocalToSceneTransform(TileMapCatalog2D catalog) =>
        GetLocalTransform(catalog) * (LogicalParent is SceneNode2D parent
            ? SceneGeometry2D.GetLocalToSceneTransform(parent) : Matrix3x2.Identity);

    private void ApplyCatalog(TileMapCatalog2D catalog)
    {
        TileMapCatalog2D? previousCatalog = appliedCatalog;
        appliedCatalog = catalog;
        tileInvalidations++;
        EnsureSpatialIndexes();
        List<ResidentChunk> obsolete = [];
        foreach ((TileChunkCacheKey key, ResidentChunk resident) in residentData.ToArray())
        {
            if (!catalog.TryGetChunk(key.Id, out TileMapChunkInfo2D? info) || info!.Spatial.Version != resident.Info.Spatial.Version)
            {
                residentData.Remove(key);
                obsolete.Add(resident);
            }
            else
            {
                if (ReferenceEquals(resident.ValidatedCatalog, previousCatalog) &&
                    CanReusePayloadValidation(previousCatalog!, catalog, resident.Info, info))
                {
                    resident.ValidatedCatalog = catalog;
                }
                resident.Info = info;
            }
        }
        // Header geometry changes require revalidation. Complete palette changes
        // belong to the payload revision and retire that acquisition above.
        ClearObsoleteWarmAcquisitions(catalog);
        RetireObsoleteRenderChunks();
        List<Exception>? failures = null;
        foreach (ResidentChunk resident in obsolete)
        {
            try { RetireResident(resident); }
            catch (Exception failure) { (failures ??= []).Add(failure); }
        }
        // Do not expose adapters from an earlier metadata revision while a
        // replacement acquisition is pending or invalid under the new header.
        try { SynchronizeCollisionAdapters(); }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        // Adapter retirement/replacement publishes its own mutation. A catalog
        // publication without a collision change must not reconcile the world again.
        Surface?.InvalidateFrame();
        if (failures is not null) { throw new AggregateException(failures); }
    }

    private async Task CompleteRequestAsync(MapRequest request,
        ValueTask<SceneSpatialRegion2D<TileMapChunkData2D>> acquisition, WarmAcquisition? warm = null,
        TileChunkCacheKey? requiredKey = null)
    {
        SceneSpatialRegion2D<TileMapChunkData2D>? acquired = null;
        CancellationToken token = request.Token;
        try
        {
            acquired = await acquisition.ConfigureAwait(false);
            if (request.Context.Relay.CheckAccess()) { Publish(); }
            else { await request.Context.Relay.InvokeAsync(Publish, token).ConfigureAwait(false); }
            void Publish()
            {
                token.ThrowIfCancellationRequested();
                if (!IsCurrent(request, warm) || (warm is null && !acquired.IsCurrent)) { return; }
                TileMapCatalog2D publishing = warm?.Catalog ?? request.Catalog;
                foreach (SceneSpatialEntry2D entry in acquired.Entries)
                {
                    publishing.TryGetChunk(entry.Id, out TileMapChunkInfo2D? info);
                    if (!residentData.TryGetValue(new(entry.Id), out ResidentChunk? existing) ||
                        !ReferenceEquals(existing.ValidatedCatalog, publishing))
                    {
                        publishing.ValidatePayload(info!, acquired.GetValue(entry.Id));
                    }
                }
                foreach (SceneSpatialEntry2D entry in acquired.Entries)
                {
                    TileChunkCacheKey key = new(entry.Id);
                    publishing.TryGetChunk(entry.Id, out TileMapChunkInfo2D? info);
                    if (!residentData.TryGetValue(key, out ResidentChunk? resident))
                    {
                        resident = new(info!, acquired.RetainValue(entry.Id));
                        residentData.Add(key, resident);
                    }
                    resident.ValidatedCatalog = publishing;
                    if (warm is null) { SynchronizeCollisionChunk(key, resident); }
                }
                if (warm is not null) { warm.Request = null; warm.Preparation = Task.CompletedTask; }
                Surface?.InvalidateFrame();
            }
        }
        catch (Exception failure)
        {
            if (!token.IsCancellationRequested)
            {
                if (request.Context.Relay.CheckAccess()) { Report(); }
                else { await request.Context.Relay.InvokeAsync(Report, token).ConfigureAwait(false); }
                void Report()
                {
                    if (!IsCurrent(request, warm)) { return; }
                    if (warm is null)
                    {
                        request.Error = failure;
                        PreparationError ??= failure;
                        if (requiredKey is { } key) { requiredErrors[key] = failure; }
                    }
                    else { warm.Error = failure; warm.Request = null; warm.Preparation = Task.CompletedTask; }
                    Surface?.InvalidateFrame();
                }
            }
            throw;
        }
        finally
        {
            try { acquired?.Dispose(); }
            finally { request.Complete(); }
        }
    }

    private Task GetCollisionPreparation(DrawRect sceneBounds)
    {
        TileMapCatalog2D? catalog = appliedCatalog;
        if (catalog is null || !ReferenceEquals(observedSource?.Catalog, catalog)) { return Preparation; }
        SceneBounds2D query = SceneGeometry2D.TryTransformBoundsToLocal(sceneBounds,
            GetLocalToSceneTransform(catalog), out DrawRect local) ? SceneBounds2D.Known(local) : SceneBounds2D.Unknown;
        List<Task> tasks = [];
        foreach (TileMapChunkInfo2D info in catalog.Chunks)
        {
            if (info.ExpandedColliderCount != 0 && info.Spatial.CollisionBounds is DrawRect collision &&
                IntersectsCollisionBounds(query, SceneBounds2D.Known(collision)) &&
                requiredPreparations.TryGetValue(new(info.Spatial.Id), out Task? task)) { tasks.Add(task); }
        }
        return Task.WhenAll(tasks);
    }

    private static bool CanReusePayloadValidation(TileMapCatalog2D previous, TileMapCatalog2D current,
        TileMapChunkInfo2D before, TileMapChunkInfo2D after)
    {
        if (previous.TileSize != current.TileSize || previous.Offset != current.Offset || before.Cells != after.Cells ||
            before.TileCount != after.TileCount || before.ExpandedColliderCount != after.ExpandedColliderCount ||
            before.Spatial.Bounds != after.Spatial.Bounds || before.Spatial.CollisionBounds != after.Spatial.CollisionBounds ||
            !before.TileIds.SequenceEqual(after.TileIds) || !before.Images.SequenceEqual(after.Images) ||
            previous.ImageSizes.Count != current.ImageSizes.Count) { return false; }
        // Definitions and collision metadata are immutable for this payload
        // revision; neither catalog owns or discovers the full palette.
        foreach ((string id, DrawSize size) in previous.ImageSizes)
        {
            if (!current.ImageSizes.TryGetValue(id, out DrawSize next) || next != size) { return false; }
        }
        return true;
    }

    private bool IsCurrent(MapRequest request, WarmAcquisition? warm) =>
        !request.Context.IsDisposed && ReferenceEquals(SimulationContext, request.Context) &&
        ReferenceEquals(observedSource?.Catalog, warm?.Catalog ?? request.Catalog) &&
        (warm is null ? request.RequiredKey is { } key &&
            requiredRequests.TryGetValue(key, out MapRequest? currentRequest) && ReferenceEquals(currentRequest, request) :
            warmAcquisitions.TryGetValue(warm.Key, out WarmAcquisition? current) && ReferenceEquals(warm, current) &&
            ReferenceEquals(warm.Request, request) && warmDataKeys.Contains(warm.Key));

    private static async Task ObservePreparationAsync(Task task)
    {
        try { await task.ConfigureAwait(false); }
        catch { } // Required failures remain observable through Preparation/Error.
    }

    private string? GetUnpreparedCollisionEntry(DrawRect sceneBounds)
    {
        TileMapSource2D? source = Source;
        if (source is null || !UIElementVisibility.IsEffectivelyVisible(this)) { return null; }
        TileMapCatalog2D catalog = source.Catalog;
        if (!ReferenceEquals(observedSource, source) || !ReferenceEquals(appliedCatalog, catalog))
        {
            if (appliedCatalog is { IsVisible: true } previous)
            {
                string? old = Find(previous, requireResident: false);
                if (old is not null) { return old; }
            }
            return catalog.IsVisible ? Find(catalog, requireResident: false) : null;
        }
        return catalog.IsVisible ? Find(catalog, requireResident: true) : null;

        string? Find(TileMapCatalog2D header, bool requireResident)
        {
            SceneBounds2D query = SceneGeometry2D.TryTransformBoundsToLocal(sceneBounds,
                GetLocalToSceneTransform(header), out DrawRect local) ? SceneBounds2D.Known(local) : SceneBounds2D.Unknown;
            foreach (TileMapChunkInfo2D info in header.Chunks)
            {
                if (info.ExpandedColliderCount == 0 || info.Spatial.CollisionBounds is not DrawRect collision ||
                    !IntersectsCollisionBounds(query, SceneBounds2D.Known(collision))) { continue; }
                TileChunkCacheKey key = new(info.Spatial.Id);
                if (!requireResident || SimulationContext is null ||
                    !residentData.TryGetValue(key, out ResidentChunk? resident) ||
                    !ReferenceEquals(resident.ValidatedCatalog, header) || !collisionChunks.ContainsKey(key)) { return info.Spatial.Id; }
            }
            return null;
        }
    }

    private static bool IntersectsCollisionBounds(SceneBounds2D interest, SceneBounds2D collision)
    {
        if (interest.Kind == SceneBoundsKind.Empty || collision.Kind == SceneBoundsKind.Empty) { return false; }
        if (interest.Kind == SceneBoundsKind.Unknown || collision.Kind == SceneBoundsKind.Unknown) { return true; }
        DrawRect a = interest.Bounds, b = collision.Bounds;
        return a.X <= b.Right && a.Right >= b.X && a.Y <= b.Bottom && a.Bottom >= b.Y;
    }

    private void RetireUnusedData()
    {
        ResidentChunk[] obsolete = residentData.Where(pair => !requiredDataKeys.Contains(pair.Key) && !warmDataKeys.Contains(pair.Key))
            .Select(static pair => pair.Value).ToArray();
        foreach (ResidentChunk resident in obsolete) { residentData.Remove(new(resident.Info.Spatial.Id)); }
        List<Exception>? failures = null;
        foreach (ResidentChunk resident in obsolete)
        {
            try { RetireResident(resident); }
            catch (Exception failure) { (failures ??= []).Add(failure); }
        }
        if (failures is not null) { throw new AggregateException(failures); }
    }

    private void RetireResident(ResidentChunk resident)
    {
        TileChunkCacheKey key = new(resident.Info.Spatial.Id);
        List<Exception>? failures = null;
        try
        {
            if (collisionChunks.TryGetValue(key, out TileCollisionChunkState? state) && ReferenceEquals(state.Resident, resident))
            {
                collisionChunks.Remove(key);
                RemoveCollisionChunk(state);
            }
        }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        try { resident.Payload.Dispose(); }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        if (failures is not null) { throw new AggregateException(failures); }
    }

    private void StopSource()
    {
        // Register this generation before any release callback can reenter or
        // fail. Terminal disposal waits even for work retired by earlier detaches.
        TaskCompletionSource retirement = new(TaskCreationOptions.RunContinuationsAsynchronously);
        retiredGenerationDrains.Add(retirement.Task);
        if (observedSource is not null) { observedSource.Changed -= OnSourceChanged; }
        observedSource = null;
        appliedCatalog = null;
        requestedCatalog = null;
        requiredPreparations.Clear();
        requiredErrors.Clear();
        indexedCatalog = null;
        spatialIndex = null;
        sourceGeneration++;
        requestVersion++;
        MapRequest[] previousRequests = requiredRequests.Values.ToArray();
        requiredRequests.Clear();
        SceneSpatialResidency2D<TileMapChunkData2D>? previous = residency;
        residency = null;
        ResidentChunk[] data = residentData.Values.ToArray();
        residentData.Clear();
        TileCollisionChunkState[] collisions = collisionChunks.Values.ToArray();
        collisionChunks.Clear();
        TileChunkCacheEntry[] batches = chunkCache.Values.ToArray();
        chunkCache.Clear();
        WarmAcquisition[] warm = DetachWarmAcquisitions();
        requiredDataKeys.Clear();
        visualChunkKeys.Clear();
        renderedChunkKeys.Clear();
        warmChunkKeys.Clear();
        ClearWarmCandidates();
        cacheFrameVersion++;
        resolvedAtlases.Clear();
        PreparationError = null;
        List<Exception> releaseFailures = [];
        List<Exception>? structuralFailures = null;
        TryRelease(() => SourceImageLeases.Clear());
        foreach (MapRequest request in previousRequests) { TryRelease(request.Cancel); }
        foreach (WarmAcquisition acquisition in warm) { TryRelease(acquisition.Dispose); }
        foreach (TileChunkCacheEntry batch in batches) { TryRelease(batch.Dispose); }
        foreach (TileCollisionChunkState state in collisions)
        {
            try { RemoveCollisionChunk(state); }
            catch (Exception failure) { (structuralFailures ??= []).Add(failure); }
        }
        foreach (ResidentChunk resident in data) { TryRelease(resident.Payload.Dispose); }
        Task residencyDrain = Task.CompletedTask;
        try { if (previous is not null) { residencyDrain = previous.DisposeAsync().AsTask(); } }
        catch (Exception failure) { releaseFailures.Add(failure); }
        _ = CompleteRetirementAsync(residencyDrain, releaseFailures, retirement);
        // External lifecycle/mutation hooks are not map resource-release errors.
        // Preserve their existing propagation instead of hiding them in DisposeAsync.
        SceneGeometry2D.FindRootScene(this)?.NotifyCollisionMutation(this, SceneCollisionMutationKind.Structure);
        Surface?.InvalidateFrame();
        if (structuralFailures is not null) { throw new AggregateException(structuralFailures); }

        void TryRelease(Action action)
        {
            try { action(); }
            catch (Exception failure) { releaseFailures.Add(failure); }
        }
    }

    private static async Task CompleteRetirementAsync(Task residencyDrain, List<Exception> releaseFailures,
        TaskCompletionSource completion)
    {
        try { await residencyDrain.ConfigureAwait(false); }
        catch (AggregateException aggregate) { releaseFailures.AddRange(aggregate.Flatten().InnerExceptions); }
        catch (Exception failure) { releaseFailures.Add(failure); }
        if (releaseFailures.Count == 0) { completion.TrySetResult(); }
        else { completion.TrySetException(new AggregateException(releaseFailures)); }
    }

    private sealed class ResidentChunk(TileMapChunkInfo2D info, SceneSpatialLease2D<TileMapChunkData2D> payload)
    {
        internal TileMapChunkInfo2D Info = info;
        internal readonly SceneSpatialLease2D<TileMapChunkData2D> Payload = payload;
        internal TileMapCatalog2D? ValidatedCatalog;
        internal DrawSize?[]? ValidatedAtlasSizes;
    }

    private sealed class MapRequest(SceneSimulationContext2D context, long version, TileMapCatalog2D catalog,
        TileChunkCacheKey? requiredKey = null)
        : ScenePreparationRequest2D(context, version)
    {
        internal TileMapCatalog2D Catalog { get; } = catalog;
        internal TileChunkCacheKey? RequiredKey { get; } = requiredKey;
        internal Task Preparation = Task.CompletedTask;
        internal Exception? Error;
    }
}
