namespace Cerneala.Drawing.Prism;

internal enum PrismCacheInvalidationKind
{
    Owner,
    All
}

internal readonly record struct PrismCacheInvalidation
{
    private PrismCacheInvalidation(
        PrismCacheInvalidationKind kind,
        PrismCacheOwnerToken ownerToken)
    {
        Kind = kind;
        OwnerToken = ownerToken;
    }

    public PrismCacheInvalidationKind Kind { get; }

    public PrismCacheOwnerToken OwnerToken { get; }

    public static PrismCacheInvalidation ForOwner(
        PrismCacheOwnerToken ownerToken)
    {
        if (ownerToken.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ownerToken),
                "A Prism cache invalidation requires a non-default owner token.");
        }

        return new PrismCacheInvalidation(
            PrismCacheInvalidationKind.Owner,
            ownerToken);
    }

    public static PrismCacheInvalidation All { get; } =
        new(PrismCacheInvalidationKind.All, default);
}

internal sealed class PrismCacheInvalidationQueue
{
    private readonly object gate = new();
    private readonly List<PrismCacheInvalidation> items = [];
    private int readIndex;

    public PrismCacheInvalidationQueue()
    {
        PrismCacheInvalidationHub.Register(this);
    }

    public int Count
    {
        get
        {
            lock (gate)
            {
                return items.Count - readIndex;
            }
        }
    }

    public void EnqueueOwner(
        PrismCacheOwnerToken ownerToken)
    {
        PrismCacheInvalidation invalidation =
            PrismCacheInvalidation.ForOwner(ownerToken);
        lock (gate)
        {
            if (items.Count > readIndex &&
                (items[^1].Kind == PrismCacheInvalidationKind.All ||
                 items[^1].OwnerToken == ownerToken))
            {
                return;
            }

            items.Add(invalidation);
        }
    }

    public void EnqueueAll()
    {
        lock (gate)
        {
            items.Clear();
            readIndex = 0;
            items.Add(PrismCacheInvalidation.All);
        }
    }

    public bool TryDequeue(
        out PrismCacheInvalidation invalidation)
    {
        lock (gate)
        {
            if (readIndex >= items.Count)
            {
                invalidation = default;
                return false;
            }

            invalidation = items[readIndex++];
            if (readIndex == items.Count)
            {
                items.Clear();
                readIndex = 0;
            }
            return true;
        }
    }
}

internal static class PrismCacheInvalidationHub
{
    private static readonly object Gate = new();
    private static readonly List<WeakReference<PrismCacheInvalidationQueue>> Queues = [];

    public static void Register(PrismCacheInvalidationQueue queue)
    {
        ArgumentNullException.ThrowIfNull(queue);
        lock (Gate)
        {
            RemoveCollectedQueues();
            Queues.Add(new WeakReference<PrismCacheInvalidationQueue>(queue));
        }
    }

    public static void EnqueueOwner(PrismCacheOwnerToken ownerToken)
    {
        PrismCacheInvalidationQueue[] targets;
        lock (Gate)
        {
            List<PrismCacheInvalidationQueue> alive = new(Queues.Count);
            for (int index = Queues.Count - 1; index >= 0; index--)
            {
                if (Queues[index].TryGetTarget(out PrismCacheInvalidationQueue? queue))
                {
                    alive.Add(queue);
                }
                else
                {
                    Queues.RemoveAt(index);
                }
            }
            targets = alive.ToArray();
        }

        foreach (PrismCacheInvalidationQueue queue in targets)
        {
            queue.EnqueueOwner(ownerToken);
        }
    }

    private static void RemoveCollectedQueues()
    {
        for (int index = Queues.Count - 1; index >= 0; index--)
        {
            if (!Queues[index].TryGetTarget(out _))
            {
                Queues.RemoveAt(index);
            }
        }
    }
}
