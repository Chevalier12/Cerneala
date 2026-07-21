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
    private readonly List<PrismCacheInvalidation> items = [];
    private int readIndex;

    public int Count => items.Count - readIndex;

    public void EnqueueOwner(
        PrismCacheOwnerToken ownerToken)
    {
        if (Count > 0 &&
            items[^1].Kind == PrismCacheInvalidationKind.All)
        {
            return;
        }

        items.Add(
            PrismCacheInvalidation.ForOwner(ownerToken));
    }

    public void EnqueueAll()
    {
        items.Clear();
        readIndex = 0;
        items.Add(PrismCacheInvalidation.All);
    }

    public bool TryDequeue(
        out PrismCacheInvalidation invalidation)
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
