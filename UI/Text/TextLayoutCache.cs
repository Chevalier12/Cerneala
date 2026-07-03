namespace Cerneala.UI.Text;

public sealed class TextLayoutCache
{
    private readonly Dictionary<TextLayoutKey, TextMeasureResult> results = new();

    public int Hits { get; private set; }

    public int Misses { get; private set; }

    public TextMeasureResult GetOrAdd(TextLayoutKey key, Func<TextLayoutKey, TextMeasureResult> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (results.TryGetValue(key, out TextMeasureResult? result))
        {
            Hits++;
            return result;
        }

        Misses++;
        result = factory(key);
        results.Add(key, result);
        return result;
    }

    public bool Contains(TextLayoutKey key)
    {
        return results.ContainsKey(key);
    }

    public void Clear()
    {
        results.Clear();
        Hits = 0;
        Misses = 0;
    }
}
