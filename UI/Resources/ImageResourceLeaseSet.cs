using System.Runtime.CompilerServices;
using Cerneala.Drawing;

namespace Cerneala.UI.Resources;

// Dispatcher-owned usage set. Rebuilding a usage scope retires only acquisitions
// absent from that scope; another scope/command cache keeps its own acquisitions.
internal sealed class ImageResourceLeaseSet
{
    private readonly Dictionary<Key, Acquisition> leases = [];
    private readonly HashSet<Key> used = [];
    private readonly List<Key> unused = [];
    private bool recording;

    internal bool HasPendingAcquisitions
    {
        get
        {
            foreach (Acquisition acquisition in leases.Values)
            {
                if (acquisition.IsActive && acquisition.Read().IsPending) { return true; }
            }
            return false;
        }
    }

    internal Scope Begin()
    {
        if (recording) { throw new InvalidOperationException("Image usage scopes cannot be nested."); }
        recording = true;
        used.Clear();
        return new Scope(this);
    }

    internal IDrawImage Acquire(ImageResource resource, ImageResourceCache? cache)
    {
        EnsureRecording();
        if (resource.HasEmbeddedImage || cache is null) { return resource.Resolve(); }
        Key key = new(cache, resource.Identity, null);
        used.Add(key);
        if (leases.TryGetValue(key, out Acquisition? previous) && previous.IsCurrent(resource, cache) &&
            previous.Read().Image is IDrawImage resident)
        {
            return resident;
        }
        ImageResourceLease next = cache.Acquire(resource);
        leases[key] = new(next);
        previous?.Dispose();
        return next.Image;
    }

    internal void Retain(IDrawImage image, ImageResourceCache? cache)
    {
        EnsureRecording();
        if (cache is null) { return; }
        Key key = new(cache, null, image);
        used.Add(key);
        if (!leases.ContainsKey(key) && cache.TryRetain(image) is ImageResourceLease lease)
        {
            leases.Add(key, new(lease));
        }
    }

    internal IDrawImage? TryAcquireResident(ImageResource resource, ImageResourceCache? cache)
    {
        EnsureRecording();
        if (resource.HasEmbeddedImage) { return resource.Resolve(); }
        if (cache is null) { return null; }
        Key key = new(cache, resource.Identity, null);
        if (leases.TryGetValue(key, out Acquisition? previous) && previous.IsCurrent(resource, cache))
        {
            used.Add(key);
            return previous.Read().Image;
        }
        ImageResourceLease? next = cache.TryAcquireResident(resource);
        if (next is not null)
        {
            used.Add(key);
            leases[key] = new(next);
        }
        else { leases.Remove(key); }
        previous?.Dispose();
        return next?.Image;
    }

    internal ImageResourceLoadResult Prepare(ImageResource resource, ImageResourceCache? cache,
        out Acquisition? started)
    {
        EnsureRecording();
        started = null;
        if (resource.HasEmbeddedImage) { return new(resource.Resolve()); }
        Key key = new(cache, resource.Identity, null);
        used.Add(key);
        if (leases.TryGetValue(key, out Acquisition? previous) && previous.IsCurrent(resource, cache))
        {
            return previous.Read();
        }

        Acquisition next;
        try
        {
            if (cache is null) { throw new InvalidOperationException("An image cache is required to prepare a path-backed image."); }
            next = new(cache.Prepare(resource));
        }
        catch (Exception error) { next = new(error); }
        leases[key] = next;
        started = next;
        previous?.Dispose();
        return next.Read();
    }

    internal void RetainCommands(DrawCommandList commands, ImageResourceCache? cache)
    {
        using Scope scope = Begin();
        if (cache is null) { return; }
        foreach (DrawCommand command in commands)
        {
            DrawCommandMetadata.TrackImageDependencies(command, image => Retain(image, cache));
        }
    }

    internal void Clear()
    {
        used.Clear();
        ReleaseUnused();
    }

    private void End()
    {
        recording = false;
        ReleaseUnused();
    }

    private void ReleaseUnused()
    {
        unused.Clear();
        foreach (Key key in leases.Keys)
        {
            if (!used.Contains(key)) { unused.Add(key); }
        }
        List<Exception>? failures = null;
        foreach (Key key in unused)
        {
            Acquisition released = leases[key];
            leases.Remove(key);
            try { released.Dispose(); }
            catch (Exception error) { (failures ??= []).Add(error); }
        }
        unused.Clear();
        if (failures is not null) { throw new AggregateException(failures); }
    }

    private void EnsureRecording()
    {
        if (!recording) { throw new InvalidOperationException("An image usage scope is required."); }
    }

    internal readonly struct Scope(ImageResourceLeaseSet owner) : IDisposable
    {
        public void Dispose() => owner.End();
    }

    internal sealed class Acquisition : IDisposable
    {
        private readonly ImageResourceLease? lease;
        private readonly ImageResourcePreparation? preparation;
        private Exception? error;
        private int disposed;

        internal Acquisition(ImageResourceLease lease) { this.lease = lease; }
        internal Acquisition(ImageResourcePreparation preparation) { this.preparation = preparation; }
        internal Acquisition(Exception error) { this.error = error; }
        internal Task? Completion => preparation?.Completion;
        internal bool IsActive => Volatile.Read(ref disposed) == 0;

        internal bool IsCurrent(ImageResource resource, ImageResourceCache? cache)
        {
            object? token = lease?.Token ?? preparation?.Token;
            return IsActive && (token is null || cache?.IsCurrent(resource, token) == true);
        }

        internal ImageResourceLoadResult Read()
        {
            if (lease is not null) { return new(lease.Image); }
            if (error is not null) { return new(null, Error: error); }
            Task<IDrawImage> completion = preparation!.Completion;
            if (!completion.IsCompleted) { return new(null, IsPending: true); }
            if (completion.IsCompletedSuccessfully) { return new(completion.Result); }
            // Preserve one failure instance until this acquisition is replaced;
            // polling a failed image must not cause an error/retry storm.
            error = completion.Exception?.InnerException ?? new TaskCanceledException(completion);
            return new(null, Error: error);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) { return; }
            try { lease?.Dispose(); }
            finally { preparation?.Dispose(); }
        }
    }

    private readonly struct Key(ImageResourceCache? cache, string? identity, IDrawImage? image) : IEquatable<Key>
    {
        private readonly ImageResourceCache? cache = cache;
        private readonly string? identity = identity;
        private readonly IDrawImage? image = image;

        public bool Equals(Key other) => ReferenceEquals(cache, other.cache) &&
            StringComparer.Ordinal.Equals(identity, other.identity) && ReferenceEquals(image, other.image);
        public override bool Equals(object? obj) => obj is Key other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(cache is null ? 0 : RuntimeHelpers.GetHashCode(cache),
            identity is null ? 0 : StringComparer.Ordinal.GetHashCode(identity),
            image is null ? 0 : RuntimeHelpers.GetHashCode(image));
    }
}

internal readonly record struct ImageResourceLoadResult(IDrawImage? Image, bool IsPending = false, Exception? Error = null);
