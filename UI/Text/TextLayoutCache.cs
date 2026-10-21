using System.Diagnostics.CodeAnalysis;

namespace Cerneala.UI.Text;

public sealed class TextLayoutCache
{
    public const int DefaultCapacity = 512;

    private readonly RecencyMap<TextLayoutKey, TextMeasureResult> results;
    private readonly RecencyMap<string, TextParagraphLayout> paragraphs;

    public TextLayoutCache(int capacity = DefaultCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        Capacity = capacity;
        results = new RecencyMap<TextLayoutKey, TextMeasureResult>(capacity);
        paragraphs = new RecencyMap<string, TextParagraphLayout>(capacity);
    }

    public int Capacity { get; }

    public int Hits { get; private set; }

    public int Misses { get; private set; }

    public int Count => results.Count + paragraphs.Count;

    public TextMeasureResult GetOrAdd(TextLayoutKey key, Func<TextLayoutKey, TextMeasureResult> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (results.TryGet(key, out TextMeasureResult? existing))
        {
            Hits++;
            return existing;
        }

        Misses++;
        TextMeasureResult result = factory(key);
        results.Add(key, result);
        return result;
    }

    // The paragraph layouts of TextMeasurer.MeasureParagraph. The key is the full description of the layout inputs.
    internal TextParagraphLayout GetOrAddParagraph(string key, Func<TextParagraphLayout> factory)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);
        if (paragraphs.TryGet(key, out TextParagraphLayout? existing))
        {
            Hits++;
            return existing;
        }

        Misses++;
        TextParagraphLayout result = factory();
        paragraphs.Add(key, result);
        return result;
    }

    public bool Contains(TextLayoutKey key)
    {
        return results.ContainsKey(key);
    }

    public void Clear()
    {
        results.Clear();
        paragraphs.Clear();
        Hits = 0;
        Misses = 0;
    }

    // A bounded map that drops the least recently used entry when it is over capacity.
    private sealed class RecencyMap<TKey, TValue>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, LinkedListNode<Entry>> entries = new();
        private readonly LinkedList<Entry> recency = new();
        private readonly int capacity;

        public RecencyMap(int capacity)
        {
            this.capacity = capacity;
        }

        public int Count => entries.Count;

        public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (entries.TryGetValue(key, out LinkedListNode<Entry>? node))
            {
                recency.Remove(node);
                recency.AddLast(node);
                value = node.Value.Value;
                return true;
            }

            value = default!;
            return false;
        }

        public void Add(TKey key, TValue value)
        {
            LinkedListNode<Entry> node = recency.AddLast(new Entry(key, value));
            entries.Add(key, node);
            if (entries.Count > capacity)
            {
                LinkedListNode<Entry> oldest = recency.First!;
                recency.RemoveFirst();
                entries.Remove(oldest.Value.Key);
            }
        }

        public bool ContainsKey(TKey key)
        {
            return entries.ContainsKey(key);
        }

        public void Clear()
        {
            entries.Clear();
            recency.Clear();
        }

        private sealed record Entry(TKey Key, TValue Value);
    }
}
