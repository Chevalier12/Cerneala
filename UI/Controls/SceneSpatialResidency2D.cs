using System.Collections.ObjectModel;
using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

/// <summary>Shares spatial acquisitions until the last interested region releases them.</summary>
public sealed class SceneSpatialResidency2D<T> : IDisposable, IAsyncDisposable where T : class
{
    private readonly object gate = new();
    private readonly ISceneSpatialSource2D<T> source;
    private readonly SemaphoreSlim loadSlots;
    private readonly Dictionary<(string Id, long Version), Resident> residents = [];
    private bool disposed;
    private int pendingLoads;
    private int operations;
    private TaskCompletionSource? idle;
    private List<Exception>? lateReleaseFailures;

    public SceneSpatialResidency2D(ISceneSpatialSource2D<T> source, int maximumConcurrentLoads = 4)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumConcurrentLoads);
        this.source = source;
        loadSlots = new(maximumConcurrentLoads);
    }

    public int ResidentCount
    {
        get { lock (gate) { return residents.Values.Count(static resident => resident.Lease is not null); } }
    }

    public int PendingLoadCount
    {
        get { lock (gate) { return pendingLoads; } }
    }

    public ValueTask<SceneSpatialRegion2D<T>> AcquireAsync(
        DrawRect? bounds,
        bool includeSimulated = false,
        CancellationToken cancellationToken = default) =>
        AcquireCoreAsync(bounds, includeSimulated, includeAll: false, null, cancellationToken);

    internal ValueTask<SceneSpatialRegion2D<T>> AcquireAllAsync(CancellationToken cancellationToken) =>
        AcquireCoreAsync(null, includeSimulated: true, includeAll: true, null, cancellationToken);

    internal ValueTask<SceneSpatialRegion2D<T>> AcquireSceneAsync(
        SceneBounds2D visualBounds, IReadOnlyList<SceneBounds2D> collisionBounds, CancellationToken cancellationToken) =>
        AcquireCoreAsync(visualBounds.Kind == SceneBoundsKind.Known ? visualBounds.Bounds : null,
            includeSimulated: true, includeAll: visualBounds.Kind == SceneBoundsKind.Unknown, collisionBounds, cancellationToken);

    // Optional preparation selects a bounded set from one catalog, not another
    // spatial rectangle that can pull in overlapping or simulated entries.
    internal ValueTask<SceneSpatialRegion2D<T>> AcquireEntriesAsync(
        IReadOnlyList<SceneSpatialEntry2D> catalog, IReadOnlyList<SceneSpatialEntry2D> entries,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(entries);
        return AcquireCoreAsync(null, includeSimulated: false, includeAll: false, null,
            cancellationToken, catalog, entries);
    }

    private async ValueTask<SceneSpatialRegion2D<T>> AcquireCoreAsync(
        DrawRect? bounds, bool includeSimulated, bool includeAll,
        IReadOnlyList<SceneBounds2D>? collisionBounds, CancellationToken cancellationToken,
        IReadOnlyList<SceneSpatialEntry2D>? expectedCatalog = null,
        IReadOnlyList<SceneSpatialEntry2D>? selectedEntries = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (bounds is DrawRect requestedBounds)
        {
            SceneSpatialEntry2D.ValidateBounds(requestedBounds, nameof(bounds));
        }

        IReadOnlyList<SceneSpatialEntry2D> catalog = source.Entries;
        if (expectedCatalog is not null && !ReferenceEquals(expectedCatalog, catalog))
        {
            throw new InvalidOperationException("The selected spatial catalog is no longer current.");
        }
        HashSet<SceneSpatialEntry2D>? selection = null;
        if (selectedEntries is not null)
        {
            selection = new(ReferenceEqualityComparer.Instance);
            foreach (SceneSpatialEntry2D entry in selectedEntries)
            {
                ArgumentNullException.ThrowIfNull(entry);
                if (!selection.Add(entry))
                {
                    throw new ArgumentException("Selected spatial entries must be unique.", nameof(selectedEntries));
                }
            }
        }
        List<SceneSpatialEntry2D> entries = [];
        List<Resident> acquired = [];
        List<Resident> starting = [];
        // Catalogs describe metadata, not live scene objects. No loader or user
        // release callback executes while the residency lock is held.
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            HashSet<string> identities = new(StringComparer.Ordinal);
            int selectedInCatalog = 0;
            foreach (SceneSpatialEntry2D entry in catalog)
            {
                ArgumentNullException.ThrowIfNull(entry);
                if (!identities.Add(entry.Id))
                {
                    throw new InvalidOperationException($"Spatial identity '{entry.Id}' occurs more than once.");
                }
                if (selection?.Contains(entry) == true) { selectedInCatalog++; }
            }
            if (selection is not null && selectedInCatalog != selection.Count)
            {
                throw new ArgumentException("Selected entries must belong to the captured spatial catalog.", nameof(selectedEntries));
            }
            foreach (SceneSpatialEntry2D entry in catalog)
            {
                if (selection is not null ? !selection.Contains(entry) :
                    !includeAll && !(includeSimulated && entry.IsSimulated) &&
                    !(bounds is DrawRect region && Intersects(region, entry.Bounds)) &&
                    !NeedsCollision(entry, collisionBounds))
                {
                    continue;
                }
                if (!residents.TryGetValue((entry.Id, entry.Version), out Resident? resident))
                {
                    resident = new(entry);
                    residents.Add((entry.Id, entry.Version), resident);
                    starting.Add(resident);
                    pendingLoads++;
                    BeginOperation();
                }
                resident.Interests++;
                acquired.Add(resident);
                entries.Add(entry);
            }
        }

        foreach (Resident resident in starting)
        {
            _ = LoadAsync(resident);
        }
        try
        {
            await Task.WhenAll(acquired.Select(static resident => resident.Ready.Task))
                .WaitAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            Dictionary<string, T> values = new(StringComparer.Ordinal);
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                foreach (Resident resident in acquired)
                {
                    values.Add(resident.Entry.Id, resident.Lease!.Value);
                }
            }
            return new(source, catalog, entries.AsReadOnly(),
                new ReadOnlyDictionary<string, T>(values), () => Release(acquired),
                () => !Volatile.Read(ref disposed), CreateRetainer(acquired));
        }
        catch (Exception failure)
        {
            try { Release(acquired); }
            catch (Exception releaseFailure) { throw new AggregateException(failure, releaseFailure); }
            throw;
        }
    }

    private Func<string, SceneSpatialLease2D<T>> CreateRetainer(List<Resident> acquired)
    {
        Dictionary<string, Resident> byId = acquired.ToDictionary(static resident => resident.Entry.Id, StringComparer.Ordinal);
        return id =>
        {
            Resident resident = byId[id];
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed || resident.Interests == 0 || resident.Lease is null, this);
                T value = resident.Lease.Value;
                resident.Interests++;
                return new(value, _ => Release([resident]));
            }
        };
    }

    public void Dispose()
    {
        List<Resident> releasing;
        lock (gate)
        {
            if (disposed) { return; }
            disposed = true;
            releasing = residents.Values.ToList();
            residents.Clear();
            foreach (Resident resident in releasing)
            {
                resident.Interests = 0;
                resident.Ready.TrySetException(new ObjectDisposedException(GetType().Name));
                BeginOperation();
            }
            if (operations == 0) { loadSlots.Dispose(); }
        }
        ReleaseResources(releasing);
    }

    public async ValueTask DisposeAsync()
    {
        Exception? synchronousFailure = null;
        try { Dispose(); }
        catch (Exception failure) { synchronousFailure = failure; }
        Task completion;
        lock (gate) { completion = idle?.Task ?? Task.CompletedTask; }
        await completion.ConfigureAwait(false);
        List<Exception> failures;
        lock (gate) { failures = lateReleaseFailures?.ToList() ?? []; }
        if (synchronousFailure is not null) { failures.Insert(0, synchronousFailure); }
        if (failures.Count > 0) { throw new AggregateException(failures); }
    }

    private async Task LoadAsync(Resident resident)
    {
        SceneSpatialLease2D<T>? lease = null;
        bool entered = false;
        try
        {
            await loadSlots.WaitAsync(resident.Cancellation.Token).ConfigureAwait(false);
            entered = true;
            lease = await source.LoadAsync(resident.Entry, resident.Cancellation.Token).ConfigureAwait(false);
            if (lease is null)
            {
                throw new InvalidOperationException("A spatial loader returned a null lease.");
            }
            // Read Value before publication: an already disposed lease is not a
            // successful acquisition and must never become an empty region.
            _ = lease.Value;
            lock (gate)
            {
                if (resident.Interests > 0 && !disposed)
                {
                    resident.Lease = lease;
                    lease = null;
                    resident.Ready.TrySetResult();
                }
                else
                {
                    resident.Ready.TrySetCanceled();
                }
            }
        }
        catch (OperationCanceledException cancellation)
        {
            resident.Ready.TrySetCanceled(cancellation.CancellationToken);
        }
        catch (Exception failure)
        {
            resident.Ready.TrySetException(failure);
        }
        finally
        {
            if (entered) { loadSlots.Release(); }
            // Late completions belong to the discarded request, never to a new
            // acquisition with the same identity or to a replacement source.
            try { lease?.Dispose(); }
            catch (Exception failure)
            {
                // Synchronous Dispose cannot wait for an uncooperative loader.
                // Async disposal observes errors from its eventual release.
                lock (gate) { (lateReleaseFailures ??= []).Add(failure); }
            }
            finally
            {
                lock (gate)
                {
                    pendingLoads--;
                    resident.Finished = true;
                    if (resident.Interests == 0 && !resident.Releasing) { resident.Cancellation.Dispose(); }
                    EndOperation();
                }
            }
        }
    }

    private void Release(List<Resident> acquired)
    {
        List<Resident> releasing = [];
        lock (gate)
        {
            foreach (Resident resident in acquired)
            {
                if (resident.Interests == 0 || --resident.Interests != 0) { continue; }
                residents.Remove((resident.Entry.Id, resident.Entry.Version));
                releasing.Add(resident);
                BeginOperation();
            }
        }
        ReleaseResources(releasing);
    }

    private void ReleaseResources(List<Resident> releasing)
    {
        List<Exception>? failures = null;
        foreach (Resident resident in releasing)
        {
            SceneSpatialLease2D<T>? lease;
            bool cancel;
            lock (gate)
            {
                lease = resident.Lease;
                resident.Lease = null;
                resident.Releasing = true;
                cancel = !resident.Finished;
            }
            try
            {
                // Cancellation callbacks are application code too. They may
                // reenter residency or throw, so do not invoke them under gate.
                if (cancel) { resident.Cancellation.Cancel(); }
            }
            catch (Exception failure) { (failures ??= []).Add(failure); }
            try { lease?.Dispose(); }
            catch (Exception failure) { (failures ??= []).Add(failure); }
            lock (gate)
            {
                resident.Releasing = false;
                if (resident.Finished) { resident.Cancellation.Dispose(); }
                EndOperation();
            }
        }
        if (failures is not null) { throw new AggregateException(failures); }
    }

    private void BeginOperation()
    {
        if (operations++ == 0) { idle = new(TaskCreationOptions.RunContinuationsAsynchronously); }
    }

    private void EndOperation()
    {
        if (--operations != 0) { return; }
        if (disposed) { loadSlots.Dispose(); }
        idle!.TrySetResult();
    }

    private static bool Intersects(DrawRect left, DrawRect right) =>
        left.X <= right.Right && left.Right >= right.X &&
        left.Y <= right.Bottom && left.Bottom >= right.Y;

    private static bool NeedsCollision(SceneSpatialEntry2D entry, IReadOnlyList<SceneBounds2D>? interests)
    {
        if (entry.CollisionBounds is not DrawRect bounds || interests is null) { return false; }
        foreach (SceneBounds2D interest in interests)
        {
            if (interest.Kind == SceneBoundsKind.Unknown ||
                interest.Kind == SceneBoundsKind.Known && Intersects(interest.Bounds, bounds)) { return true; }
        }
        return false;
    }

    private sealed class Resident(SceneSpatialEntry2D entry)
    {
        internal readonly SceneSpatialEntry2D Entry = entry;
        internal readonly CancellationTokenSource Cancellation = new();
        internal readonly TaskCompletionSource Ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal SceneSpatialLease2D<T>? Lease;
        internal int Interests;
        internal bool Finished;
        internal bool Releasing;
    }
}

/// <summary>A prepared snapshot that pins its payloads until disposed.</summary>
public sealed class SceneSpatialRegion2D<T> : IDisposable where T : class
{
    private readonly ISceneSpatialSource2D<T> source;
    private readonly IReadOnlyList<SceneSpatialEntry2D> catalog;
    private readonly SceneSpatialLease2D<IReadOnlyDictionary<string, T>> lease;
    private readonly Func<bool> ownerIsAlive;
    private readonly Func<string, SceneSpatialLease2D<T>> retainValue;
    private int disposed;

    internal SceneSpatialRegion2D(
        ISceneSpatialSource2D<T> source,
        IReadOnlyList<SceneSpatialEntry2D> catalog,
        IReadOnlyList<SceneSpatialEntry2D> entries,
        IReadOnlyDictionary<string, T> values,
        Action release,
        Func<bool> ownerIsAlive,
        Func<string, SceneSpatialLease2D<T>> retainValue)
    {
        this.source = source;
        this.catalog = catalog;
        Entries = entries;
        lease = new(values, _ => release());
        this.ownerIsAlive = ownerIsAlive;
        this.retainValue = retainValue;
    }

    public IReadOnlyList<SceneSpatialEntry2D> Entries { get; }

    public bool IsCurrent => Volatile.Read(ref disposed) == 0 && ownerIsAlive() &&
        ReferenceEquals(catalog, source.Entries);

    public T GetValue(string id)
    {
        ObjectDisposedException.ThrowIf(!ownerIsAlive(), this);
        return lease.Value[id];
    }

    // Realized nodes own their payload independently of a temporary preparation
    // snapshot, so one catalog deletion can retire one node and its acquisition.
    internal SceneSpatialLease2D<T> RetainValue(string id)
    {
        _ = GetValue(id);
        return retainValue(id);
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref disposed, 1);
        lease.Dispose();
    }
}
