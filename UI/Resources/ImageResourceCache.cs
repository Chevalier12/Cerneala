using Cerneala.Drawing;

namespace Cerneala.UI.Resources;

public sealed class ImageResourceCache : IDisposable, IAsyncDisposable
{
    private static readonly AsyncLocal<LoadScope?> currentLoad = new();
    private readonly object gate = new();
    private readonly IImageLoader? loader;
    private readonly SemaphoreSlim loadSlots;
    private readonly Dictionary<string, Entry> entries = new(StringComparer.Ordinal);
    private readonly Dictionary<IDrawImage, Entry> images = new(ReferenceEqualityComparer.Instance);
    private bool disposed;
    private int loadCount;
    private int pendingLoads;
    private int operations;
    private bool slotsDisposed;
    private TaskCompletionSource? idle;
    private List<Exception>? lateReleaseFailures;

    public ImageResourceCache(IImageLoader? loader, int maximumConcurrentLoads = 4)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumConcurrentLoads);
        this.loader = loader;
        loadSlots = new(maximumConcurrentLoads);
    }

    public int LoadCount { get { lock (gate) { return loadCount; } } }

    public int ResidentCount { get { lock (gate) { return images.Count; } } }

    public int PendingLoadCount { get { lock (gate) { return pendingLoads; } } }

    public ImageResourceLease Acquire(ImageResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        Entry? entry = Reserve(resource, asynchronous: false, out bool load);
        if (entry is null) { return ImageResourceLease.Borrow(resource.Resolve()); }
        if (load) { Load(entry, resource); }
        try
        {
            IDrawImage image = entry.Ready.Task.GetAwaiter().GetResult();
            return CreateLease(entry, image);
        }
        catch (Exception failure)
        {
            try { Release(entry); }
            catch (Exception releaseFailure) { throw new AggregateException(failure, releaseFailure); }
            throw;
        }
    }

    public async ValueTask<ImageResourceLease> AcquireAsync(
        ImageResource resource,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);
        cancellationToken.ThrowIfCancellationRequested();
        Entry? entry = Reserve(resource, asynchronous: true, out bool load);
        if (entry is null) { return ImageResourceLease.Borrow(resource.Resolve()); }
        if (load) { _ = LoadAsync(entry, resource); }
        try
        {
            IDrawImage image = await entry.Ready.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return CreateLease(entry, image);
        }
        catch (Exception failure)
        {
            try { Release(entry); }
            catch (Exception releaseFailure) { throw new AggregateException(failure, releaseFailure); }
            throw;
        }
    }

    private Entry? Reserve(ImageResource resource, bool asynchronous, out bool load)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            load = false;
            if (resource.HasEmbeddedImage)
            {
                return null;
            }
            if (loader is null)
            {
                throw new InvalidOperationException("An image loader is required for path-backed image resources.");
            }

            load = !entries.TryGetValue(resource.Identity, out Entry? entry);
            if (load)
            {
                if (asynchronous && loader is not IAsyncImageLoader)
                {
                    throw new NotSupportedException("Asynchronous acquisition requires an IAsyncImageLoader for a new image.");
                }
                entry = new Entry(resource.Identity);
                entries.Add(entry.Identity, entry);
                pendingLoads++;
                BeginOperation();
            }
            else if (!entry!.Ready.Task.IsCompleted && IsLoadingHere(entry))
            {
                throw new InvalidOperationException($"Image loader recursively acquired '{resource.Identity}'.");
            }
            entry!.References++;
            return entry;
        }
    }

    /// <summary>Forgets the current identity without invalidating outstanding acquisitions.</summary>
    public void Remove(ImageResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        lock (gate)
        {
            entries.Remove(resource.Identity);
        }
    }

    /// <summary>Forgets current identities; acquired images remain alive until their last release.</summary>
    public void Clear()
    {
        lock (gate)
        {
            entries.Clear();
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            disposed = true;
            entries.Clear();
            DisposeSlotsIfIdle();
        }
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        Task completion;
        lock (gate) { completion = idle?.Task ?? Task.CompletedTask; }
        await completion.ConfigureAwait(false);
        List<Exception>? failures;
        lock (gate) { failures = lateReleaseFailures?.ToList(); }
        if (failures is not null) { throw new AggregateException(failures); }
    }

    // An internal consumer can own interest before the image is ready. The
    // cache still owns loading, cancellation and abandoned-result cleanup;
    // there is no asynchronous lease handoff through a UI callback.
    internal ImageResourcePreparation Prepare(ImageResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        Entry? entry = Reserve(resource, asynchronous: true, out bool load);
        if (entry is null) { return new(Task.FromResult(resource.Resolve()), null, null); }
        ImageResourcePreparation preparation = new(entry.Ready.Task, entry, () => Release(entry));
        if (load) { _ = LoadAsync(entry, resource); }
        return preparation;
    }

    internal bool IsCurrent(ImageResource resource, ImageResourceLease lease) => IsCurrent(resource, lease.Token);

    internal bool IsCurrent(ImageResource resource, object? token)
    {
        lock (gate)
        {
            return !disposed && entries.TryGetValue(resource.Identity, out Entry? entry) &&
                ReferenceEquals(entry, token);
        }
    }

    // Optional preparation may acquire a completed current image, but must not
    // start I/O, wait for a pending decode, or resurrect a forgotten identity.
    internal ImageResourceLease? TryAcquireResident(ImageResource resource)
    {
        lock (gate)
        {
            return !disposed && entries.TryGetValue(resource.Identity, out Entry? entry) &&
                entry.Ready.Task.IsCompletedSuccessfully && entry.Image is not null
                    ? Retain(entry) : null;
        }
    }

    // Retained commands may still use an orphaned entry after Remove/Clear. This
    // lookup never loads and does not turn a caller-owned image into an owned one.
    internal ImageResourceLease? TryRetain(IDrawImage image)
    {
        lock (gate)
        {
            return images.TryGetValue(image, out Entry? entry) ? Retain(entry) : null;
        }
    }

    private void Load(Entry entry, ImageResource resource)
    {
        LoadScope? previous = currentLoad.Value;
        currentLoad.Value = new(entry, previous);
        try
        {
            // Custom loaders are not called under a cache lock.
            Publish(entry, resource.Resolve(loader));
        }
        catch (Exception error)
        {
            Fail(entry, error);
        }
        finally
        {
            currentLoad.Value = previous;
            FinishLoad(entry);
        }
    }

    private async Task LoadAsync(Entry entry, ImageResource resource)
    {
        bool entered = false;
        LoadScope? previous = currentLoad.Value;
        currentLoad.Value = new(entry, previous);
        try
        {
            await loadSlots.WaitAsync(entry.Cancellation.Token).ConfigureAwait(false);
            entered = true;
            IDrawImage image = await ((IAsyncImageLoader)loader!).LoadAsync(
                resource.Identity, entry.Cancellation.Token).ConfigureAwait(false);
            Publish(entry, image);
        }
        catch (Exception error) { Fail(entry, error); }
        finally
        {
            currentLoad.Value = previous;
            if (entered) { loadSlots.Release(); }
            FinishLoad(entry);
        }
    }

    private void Publish(Entry entry, IDrawImage image)
    {
        if (image is null) { throw new InvalidOperationException("An image loader returned null."); }
        lock (gate)
        {
            if (images.ContainsKey(image))
            {
                throw new InvalidOperationException("An image loader must not return an image already owned by another cache entry.");
            }
            if (entry.References > 0)
            {
                entry.Image = image;
                images.Add(image, entry);
                loadCount++;
                entry.Ready.TrySetResult(image);
                return;
            }
        }
        // All requesters cancelled. Never publish a late image into a newer entry.
        entry.Ready.TrySetCanceled();
        try { (image as IDisposable)?.Dispose(); }
        catch (Exception error)
        {
            lock (gate) { (lateReleaseFailures ??= []).Add(error); }
        }
    }

    private static void Fail(Entry entry, Exception error)
    {
        if (error is OperationCanceledException cancellation)
        {
            entry.Ready.TrySetCanceled(cancellation.CancellationToken);
        }
        else
        {
            entry.Ready.TrySetException(error);
            // A cancelled last requester may no longer await the shared load.
            _ = entry.Ready.Task.Exception;
        }
    }

    private void FinishLoad(Entry entry)
    {
        lock (gate)
        {
            pendingLoads--;
            entry.Finished = true;
            if (entry.References == 0 && !entry.Releasing) { entry.Cancellation.Dispose(); }
            EndOperation();
        }
    }

    private ImageResourceLease CreateLease(Entry entry, IDrawImage image) =>
        new(image, () => Retain(entry), () => Release(entry), entry);

    private ImageResourceLease Retain(Entry entry)
    {
        lock (gate)
        {
            if (entry.References == 0 || entry.Image is null)
            {
                throw new ObjectDisposedException(nameof(ImageResourceLease));
            }
            entry.References++;
            return CreateLease(entry, entry.Image);
        }
    }

    private void Release(Entry entry)
    {
        IDrawImage? released;
        bool cancel;
        lock (gate)
        {
            if (--entry.References != 0)
            {
                return;
            }
            if (entries.TryGetValue(entry.Identity, out Entry? current) && ReferenceEquals(current, entry))
            {
                entries.Remove(entry.Identity);
            }
            released = entry.Image;
            entry.Image = null;
            if (released is not null)
            {
                images.Remove(released);
            }
            entry.Releasing = true;
            cancel = !entry.Finished;
            BeginOperation();
        }
        try
        {
            try { if (cancel) { entry.Cancellation.Cancel(); } }
            finally { (released as IDisposable)?.Dispose(); }
        }
        finally
        {
            lock (gate)
            {
                entry.Releasing = false;
                if (entry.Finished) { entry.Cancellation.Dispose(); }
                EndOperation();
            }
        }
    }

    private void BeginOperation()
    {
        if (operations++ == 0) { idle = new(TaskCreationOptions.RunContinuationsAsynchronously); }
    }

    private void EndOperation()
    {
        if (--operations != 0) { return; }
        DisposeSlotsIfIdle();
        idle!.TrySetResult();
    }

    private void DisposeSlotsIfIdle()
    {
        if (disposed && operations == 0 && !slotsDisposed)
        {
            slotsDisposed = true;
            loadSlots.Dispose();
        }
    }

    private static bool IsLoadingHere(Entry entry)
    {
        for (LoadScope? scope = currentLoad.Value; scope is not null; scope = scope.Parent)
        {
            if (ReferenceEquals(entry, scope.Entry)) { return true; }
        }
        return false;
    }

    private sealed record LoadScope(Entry Entry, LoadScope? Parent);

    private sealed class Entry(string identity)
    {
        internal string Identity { get; } = identity;
        internal CancellationTokenSource Cancellation { get; } = new();
        internal TaskCompletionSource<IDrawImage> Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal IDrawImage? Image { get; set; }
        internal int References { get; set; }
        internal bool Finished { get; set; }
        internal bool Releasing { get; set; }
    }
}

internal sealed class ImageResourcePreparation(Task<IDrawImage> completion, object? token, Action? release) : IDisposable
{
    private Action? release = release;
    internal Task<IDrawImage> Completion { get; } = completion;
    internal object? Token { get; } = token;
    public void Dispose() => Interlocked.Exchange(ref release, null)?.Invoke();
}
