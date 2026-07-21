using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Prism;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism.Surfaces;

internal sealed class PrismRetainedSurfaceCache : IDisposable
{
    private readonly PrismSurfacePool surfacePool;
    private readonly PrismSurfaceMemoryAccountant accountant;
    private readonly Dictionary<PrismRetainedCacheKey, CacheEntry> entries = [];
    private readonly Dictionary<PrismCacheOwnerToken, CacheEntry> ownerEntries = [];
    private readonly long[] evictionCounts =
        new long[(int)PrismCacheEvictionReason.ExplicitRemoval + 1];
    private CacheEntry? leastRecentlyUsed;
    private CacheEntry? mostRecentlyUsed;
    private long nextEntryId;
    private bool disposed;

    public PrismRetainedSurfaceCache(
        PrismSurfacePool surfacePool)
    {
        this.surfacePool = surfacePool ??
            throw new ArgumentNullException(nameof(surfacePool));
        accountant = surfacePool.MemoryAccountant;
        surfacePool.AttachRetainedCache(this);
    }

    public int EntryCount => entries.Count;

    public int PinnedEntryCount { get; private set; }

    public int ActiveLeaseCount { get; private set; }

    public long RetainedByteCount =>
        accountant.RetainedByteCount;

    public long PromotionCount { get; private set; }

    public long RejectedPromotionCount { get; private set; }

    public long EvictionCount { get; private set; }

    public PrismCacheEvictionReason LastEvictionReason { get; private set; }

    public long LookupCount { get; private set; }

    public int OwnerIndexCount => ownerEntries.Count;

    public int LastOwnerInvalidationVisitCount { get; private set; }

    public bool Contains(PrismRetainedCacheKey key)
    {
        VerifyUsable();
        return entries.TryGetValue(
                key,
                out CacheEntry? entry) &&
            !entry.RemoveWhenUnpinned;
    }

    public bool TryAcquire(
        PrismRetainedCacheKey key,
        out PrismRetainedSurfaceLease? lease)
    {
        VerifyUsable();
        if (!TryGetAcquirableEntry(
                key,
                out CacheEntry entry,
                out RenderTarget2D target))
        {
            lease = null;
            return false;
        }

        PrismRetainedSurfaceLease acquired = new();
        AttachLease(
            acquired,
            key,
            entry,
            target);
        lease = acquired;
        return true;
    }

    internal bool TryAcquire(
        PrismRetainedCacheKey key,
        PrismRetainedSurfaceLease lease)
    {
        VerifyUsable();
        ArgumentNullException.ThrowIfNull(lease);
        if (lease.IsActive)
        {
            throw new InvalidOperationException(
                "A Prism retained surface lease must be released before it can be reused.");
        }
        if (!TryGetAcquirableEntry(
                key,
                out CacheEntry entry,
                out RenderTarget2D target))
        {
            return false;
        }

        AttachLease(
            lease,
            key,
            entry,
            target);
        return true;
    }

    private void AttachLease(
        PrismRetainedSurfaceLease lease,
        PrismRetainedCacheKey key,
        CacheEntry entry,
        RenderTarget2D target)
    {
        lease.Attach(
            this,
            key,
            entry.Id,
            target);
        if (entry.PinCount == 0)
        {
            PinnedEntryCount++;
        }
        entry.PinCount++;
        ActiveLeaseCount++;
        Touch(entry);
    }

    private bool TryGetAcquirableEntry(
        PrismRetainedCacheKey key,
        out CacheEntry entry,
        out RenderTarget2D target)
    {
        LookupCount++;
        if (!entries.TryGetValue(
                key,
                out CacheEntry? candidate) ||
            candidate is null ||
            candidate.RemoveWhenUnpinned)
        {
            entry = null!;
            target = null!;
            return false;
        }

        entry = candidate;
        target = entry.Surface.Surface;
        if (!target.IsDisposed)
        {
            return true;
        }

        Evict(
            entry,
            returnToPool: false,
            PrismCacheEvictionReason.InvalidSurface);
        entry = null!;
        target = null!;
        return false;
    }

    public bool TryPromote(
        PrismRetainedCacheKey key,
        PrismSurfaceFrame frame,
        int executionIndex)
    {
        VerifyUsable();
        if (!frame.IsOwnedBy(surfacePool))
        {
            throw new ArgumentException(
                "A retained cache can promote surfaces only from its paired transient pool.",
                nameof(frame));
        }

        PrismSurfaceKey surfaceKey =
            frame.ValidatePromotion(executionIndex);
        long byteCount = surfaceKey.CalculateByteSize();
        PrismSurfaceBudget budget = accountant.Budget;
        if (budget.RetainedEntryLimit == 0 ||
            byteCount > budget.RetainedSoftByteLimit)
        {
            RejectedPromotionCount++;
            return false;
        }

        entries.TryGetValue(
            key,
            out CacheEntry? duplicate);
        if (duplicate is not null)
        {
            if (duplicate.PinCount != 0)
            {
                RejectedPromotionCount++;
                return false;
            }
        }

        long entryId = checked(nextEntryId + 1);
        CacheEntry pendingEntry;
        try
        {
            entries.EnsureCapacity(
                checked(entries.Count + 1));
            ownerEntries.EnsureCapacity(
                checked(ownerEntries.Count + 1));
            pendingEntry = new CacheEntry(entryId, key);
        }
        catch (OutOfMemoryException)
        {
            RejectedPromotionCount++;
            return false;
        }

        if (duplicate is not null)
        {
            Evict(
                duplicate,
                returnToPool: false,
                PrismCacheEvictionReason.Replacement);
        }

        if (!MakeRoom(byteCount))
        {
            RejectedPromotionCount++;
            return false;
        }

        PrismRetainedSurface? retained = null;
        try
        {
            retained =
                frame.PromoteToRetainedOwner(executionIndex);
            pendingEntry.Attach(retained);
            entries.Add(key, pendingEntry);
            IndexOwner(pendingEntry);
            AppendMostRecentlyUsed(pendingEntry);
            retained = null;
            nextEntryId = entryId;
            PromotionCount++;
            return true;
        }
        catch (OutOfMemoryException)
        {
            retained?.Dispose();
            RejectedPromotionCount++;
            return false;
        }
        catch
        {
            retained?.Dispose();
            throw;
        }
    }

    public bool Remove(PrismRetainedCacheKey key)
    {
        VerifyUsable();
        if (!entries.TryGetValue(
                key,
                out CacheEntry? entry))
        {
            return false;
        }

        if (entry.PinCount != 0)
        {
            MarkForRemoval(
                entry,
                PrismCacheEvictionReason.ExplicitRemoval);
            return true;
        }

        Evict(
            entry,
            returnToPool: true,
            PrismCacheEvictionReason.ExplicitRemoval);
        return true;
    }

    public int RemoveOwner(
        PrismCacheOwnerToken ownerToken)
    {
        VerifyUsable();
        if (ownerToken.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ownerToken),
                "A Prism retained cache owner invalidation requires a non-default token.");
        }

        LastOwnerInvalidationVisitCount = 0;
        if (!ownerEntries.Remove(
                ownerToken,
                out CacheEntry? entry))
        {
            return 0;
        }

        int removedCount = 0;
        while (entry is not null)
        {
            CacheEntry? next = entry.OwnerNext;
            entry.OwnerPrevious = null;
            entry.OwnerNext = null;
            removedCount++;
            LastOwnerInvalidationVisitCount++;
            if (entry.PinCount == 0)
            {
                Evict(
                    entry,
                    returnToPool: true,
                    PrismCacheEvictionReason.Invalidation,
                    ownerAlreadyUnlinked: true);
            }
            else
            {
                MarkForRemoval(
                    entry,
                    PrismCacheEvictionReason.Invalidation);
            }
            entry = next;
        }

        return removedCount;
    }

    public void Clear()
    {
        VerifyUsable();
        ClearCore(PrismCacheEvictionReason.ExplicitRemoval);
    }

    internal void Clear(PrismCacheEvictionReason reason)
    {
        VerifyUsable();
        ValidateEvictionReason(reason);
        ClearCore(reason);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        accountant.VerifyAccess();
        ClearCore(PrismCacheEvictionReason.Disposal);
        disposed = true;
        surfacePool.DetachRetainedCache(this);
    }

    internal bool EvictForTransient(long requiredByteCount)
    {
        accountant.VerifyAccess();
        while (!accountant.CanReserveTransient(
            requiredByteCount))
        {
            CacheEntry? candidate =
                FindLeastRecentlyUsedEvictable();
            if (candidate is null)
            {
                return false;
            }

            Evict(
                candidate,
                returnToPool: false,
                PrismCacheEvictionReason.TransientPressure);
        }

        return true;
    }

    internal void ResetFromPool(bool poolDisposing)
    {
        accountant.VerifyAccess();
        ClearCore(
            poolDisposing
                ? PrismCacheEvictionReason.Disposal
                : PrismCacheEvictionReason.DeviceReset);
        if (poolDisposing)
        {
            disposed = true;
        }
    }

    internal void VerifyLeaseAccess()
    {
        accountant.VerifyAccess();
    }

    internal void ReleaseLease(
        PrismRetainedCacheKey key,
        long entryId)
    {
        accountant.VerifyAccess();
        if (!entries.TryGetValue(
                key,
                out CacheEntry? entry) ||
            entry.Id != entryId ||
            entry.PinCount <= 0)
        {
            return;
        }

        entry.PinCount--;
        ActiveLeaseCount--;
        if (entry.PinCount == 0)
        {
            PinnedEntryCount--;
            if (entry.RemoveWhenUnpinned || disposed)
            {
                PrismCacheEvictionReason reason =
                    entry.PendingEvictionReason;
                if (reason == PrismCacheEvictionReason.None)
                {
                    reason = PrismCacheEvictionReason.Disposal;
                }
                Evict(
                    entry,
                    returnToPool: false,
                    reason);
            }
        }
    }

    private bool MakeRoom(long incomingByteCount)
    {
        PrismSurfaceBudget budget = accountant.Budget;
        while (entries.Count >=
                budget.RetainedEntryLimit ||
            accountant.RetainedByteCount >
                budget.RetainedSoftByteLimit -
                    incomingByteCount)
        {
            CacheEntry? candidate =
                FindLeastRecentlyUsedEvictable();
            if (candidate is null)
            {
                return false;
            }

            Evict(
                candidate,
                returnToPool: true,
                PrismCacheEvictionReason.Capacity);
        }

        return true;
    }

    private CacheEntry? FindLeastRecentlyUsedEvictable()
    {
        CacheEntry? candidate = leastRecentlyUsed;
        while (candidate is not null &&
            candidate.PinCount != 0)
        {
            candidate = candidate.Next;
        }

        return candidate;
    }

    public long GetEvictionCount(
        PrismCacheEvictionReason reason)
    {
        VerifyUsable();
        if (reason == PrismCacheEvictionReason.None)
        {
            return 0;
        }
        ValidateEvictionReason(reason);
        return evictionCounts[(int)reason];
    }

    private void ClearCore(PrismCacheEvictionReason reason)
    {
        CacheEntry? entry = leastRecentlyUsed;
        while (entry is not null)
        {
            CacheEntry? next = entry.Next;
            if (entry.PinCount == 0)
            {
                Evict(
                    entry,
                    returnToPool: false,
                    reason);
            }
            else
            {
                MarkForRemoval(entry, reason);
                UnlinkOwner(entry);
            }
            entry = next;
        }
        ownerEntries.Clear();
    }

    private void Evict(
        CacheEntry entry,
        bool returnToPool,
        PrismCacheEvictionReason reason,
        bool ownerAlreadyUnlinked = false)
    {
        ValidateEvictionReason(reason);
        if (entry.PinCount != 0)
        {
            throw new InvalidOperationException(
                "A pinned Prism retained surface cannot be evicted.");
        }

        entries.Remove(entry.Key);
        if (!ownerAlreadyUnlinked)
        {
            UnlinkOwner(entry);
        }
        Unlink(entry);
        entry.RemoveWhenUnpinned = true;
        bool reclaimed = false;
        if (returnToPool)
        {
            reclaimed =
                surfacePool.TryReclaim(entry.Surface);
        }
        if (!reclaimed)
        {
            entry.Surface.Dispose();
        }
        EvictionCount++;
        evictionCounts[(int)reason]++;
        LastEvictionReason = reason;
    }

    private static void MarkForRemoval(
        CacheEntry entry,
        PrismCacheEvictionReason reason)
    {
        ValidateEvictionReason(reason);
        entry.RemoveWhenUnpinned = true;
        if (entry.PendingEvictionReason ==
            PrismCacheEvictionReason.None)
        {
            entry.PendingEvictionReason = reason;
        }
    }

    private static void ValidateEvictionReason(
        PrismCacheEvictionReason reason)
    {
        if (reason == PrismCacheEvictionReason.None ||
            !Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                "A retained cache eviction requires a concrete reason.");
        }
    }

    private void IndexOwner(
        CacheEntry entry)
    {
        PrismCacheOwnerToken ownerToken =
            entry.Key.DependencyStamp.CacheOwnerToken;
        ownerEntries.TryGetValue(
            ownerToken,
            out CacheEntry? current);
        entry.OwnerPrevious = null;
        entry.OwnerNext = current;
        if (current is not null)
        {
            current.OwnerPrevious = entry;
        }
        ownerEntries[ownerToken] = entry;
    }

    private void UnlinkOwner(
        CacheEntry entry)
    {
        PrismCacheOwnerToken ownerToken =
            entry.Key.DependencyStamp.CacheOwnerToken;
        if (entry.OwnerPrevious is null)
        {
            if (entry.OwnerNext is null)
            {
                ownerEntries.Remove(ownerToken);
            }
            else
            {
                ownerEntries[ownerToken] =
                    entry.OwnerNext;
            }
        }
        else
        {
            entry.OwnerPrevious.OwnerNext =
                entry.OwnerNext;
        }

        if (entry.OwnerNext is not null)
        {
            entry.OwnerNext.OwnerPrevious =
                entry.OwnerPrevious;
        }
        entry.OwnerPrevious = null;
        entry.OwnerNext = null;
    }

    private void Touch(CacheEntry entry)
    {
        if (ReferenceEquals(entry, mostRecentlyUsed))
        {
            return;
        }

        Unlink(entry);
        AppendMostRecentlyUsed(entry);
    }

    private void AppendMostRecentlyUsed(CacheEntry entry)
    {
        entry.Previous = mostRecentlyUsed;
        entry.Next = null;
        if (mostRecentlyUsed is null)
        {
            leastRecentlyUsed = entry;
        }
        else
        {
            mostRecentlyUsed.Next = entry;
        }
        mostRecentlyUsed = entry;
    }

    private void Unlink(CacheEntry entry)
    {
        if (entry.Previous is null)
        {
            leastRecentlyUsed = entry.Next;
        }
        else
        {
            entry.Previous.Next = entry.Next;
        }

        if (entry.Next is null)
        {
            mostRecentlyUsed = entry.Previous;
        }
        else
        {
            entry.Next.Previous = entry.Previous;
        }

        entry.Previous = null;
        entry.Next = null;
    }

    private void VerifyUsable()
    {
        accountant.VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private sealed class CacheEntry
    {
        private PrismRetainedSurface? surface;

        public CacheEntry(
            long id,
            PrismRetainedCacheKey key)
        {
            Id = id;
            Key = key;
        }

        public long Id { get; }

        public PrismRetainedCacheKey Key { get; }

        public PrismRetainedSurface Surface =>
            surface ??
            throw new InvalidOperationException(
                "A Prism retained cache entry has no surface owner.");

        public int PinCount { get; set; }

        public bool RemoveWhenUnpinned { get; set; }

        public PrismCacheEvictionReason PendingEvictionReason { get; set; }

        public CacheEntry? Previous { get; set; }

        public CacheEntry? Next { get; set; }

        public CacheEntry? OwnerPrevious { get; set; }

        public CacheEntry? OwnerNext { get; set; }

        public void Attach(PrismRetainedSurface retained)
        {
            ArgumentNullException.ThrowIfNull(retained);
            if (surface is not null)
            {
                throw new InvalidOperationException(
                    "A Prism retained cache entry already owns a surface.");
            }

            surface = retained;
        }
    }
}

internal sealed class PrismRetainedSurfaceLease : IDisposable
{
    private PrismRetainedSurfaceCache? owner;
    private PrismRetainedCacheKey key;
    private long entryId;
    private RenderTarget2D? surface;

    internal PrismRetainedSurfaceLease()
    {
    }

    internal PrismRetainedSurfaceLease(
        PrismRetainedSurfaceCache owner,
        PrismRetainedCacheKey key,
        long entryId,
        RenderTarget2D surface)
    {
        Attach(owner, key, entryId, surface);
    }

    internal bool IsActive => owner is not null;

    internal void Attach(
        PrismRetainedSurfaceCache owner,
        PrismRetainedCacheKey key,
        long entryId,
        RenderTarget2D surface)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(surface);
        if (this.owner is not null)
        {
            throw new InvalidOperationException(
                "A Prism retained surface lease is already active.");
        }

        this.owner = owner;
        this.key = key;
        this.entryId = entryId;
        this.surface = surface;
    }

    public RenderTarget2D Surface
    {
        get
        {
            PrismRetainedSurfaceCache cache =
                owner ??
                throw new ObjectDisposedException(
                    nameof(PrismRetainedSurfaceLease));
            cache.VerifyLeaseAccess();
            return surface ??
                throw new ObjectDisposedException(
                    nameof(PrismRetainedSurfaceLease));
        }
    }

    public void Dispose()
    {
        PrismRetainedSurfaceCache? cache = owner;
        if (cache is null)
        {
            return;
        }

        cache.VerifyLeaseAccess();
        PrismRetainedCacheKey releasedKey = key;
        long releasedEntryId = entryId;
        owner = null;
        key = default;
        entryId = 0;
        surface = null;
        cache.ReleaseLease(releasedKey, releasedEntryId);
    }
}
