namespace Cerneala.UI.Text;

public sealed class TextLayoutCache
{
    public const int DefaultCapacity = 512;

    private readonly Dictionary<TextLayoutKey, LinkedListNode<CacheEntry>> results = new();
    private readonly LinkedList<CacheEntry> recency = new();

    public TextLayoutCache(int capacity = DefaultCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        Capacity = capacity;
    }

    public int Capacity { get; }

    public int Hits { get; private set; }

    public int Misses { get; private set; }

    public int Count => results.Count;

    public TextMeasureResult GetOrAdd(TextLayoutKey key, Func<TextLayoutKey, TextMeasureResult> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (results.TryGetValue(key, out LinkedListNode<CacheEntry>? node))
        {
            Hits++;
            recency.Remove(node);
            recency.AddLast(node);
            return node.Value.Result;
        }

        Misses++;
        TextMeasureResult result = factory(key);
        node = recency.AddLast(new CacheEntry(key, result));
        results.Add(key, node);
        if (results.Count > Capacity)
        {
            LinkedListNode<CacheEntry> oldest = recency.First!;
            recency.RemoveFirst();
            results.Remove(oldest.Value.Key);
        }

        return result;
    }

    public bool Contains(TextLayoutKey key)
    {
        return results.ContainsKey(key);
    }

    public void Clear()
    {
        results.Clear();
        recency.Clear();
        Hits = 0;
        Misses = 0;
    }

    private sealed record CacheEntry(TextLayoutKey Key, TextMeasureResult Result);
}
