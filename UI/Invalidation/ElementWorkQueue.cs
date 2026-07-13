using Cerneala.UI.Elements;

namespace Cerneala.UI.Invalidation;

internal readonly struct ElementQueueUnit
{
    public static ElementQueueUnit Value => default;
}

internal sealed class ElementWorkQueue<TMetadata>
{
    private readonly UIRoot root;
    private readonly Func<TMetadata, TMetadata, TMetadata> mergeMetadata;
    private readonly Dictionary<UIElement, QueueEntry> entries = new(ReferenceEqualityComparer.Instance);
    private long nextSequence;

    public ElementWorkQueue(
        UIRoot root,
        Func<TMetadata, TMetadata, TMetadata>? mergeMetadata = null)
    {
        this.root = root ?? throw new ArgumentNullException(nameof(root));
        this.mergeMetadata = mergeMetadata ?? (static (_, incoming) => incoming);
    }

    public int Count => entries.Count;

    public bool HasWork => entries.Count != 0;

    internal int LastSnapshotSortCount { get; private set; }

    public bool Contains(UIElement element)
    {
        return element is not null && entries.ContainsKey(element);
    }

    public void Enqueue(UIElement element, TMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (entries.TryGetValue(element, out QueueEntry current))
        {
            entries[element] = current with { Metadata = mergeMetadata(current.Metadata, metadata) };
            return;
        }

        entries.Add(element, new QueueEntry(metadata, nextSequence++));
    }

    public bool Remove(UIElement element)
    {
        return element is not null && entries.Remove(element);
    }

    public TMetadata GetMetadataOrDefault(UIElement element, TMetadata fallback)
    {
        return element is not null && entries.TryGetValue(element, out QueueEntry entry)
            ? entry.Metadata
            : fallback;
    }

    public IReadOnlyList<UIElement> Snapshot(bool reverse = false)
    {
        if (entries.Count == 0)
        {
            LastSnapshotSortCount = 0;
            return Array.Empty<UIElement>();
        }

        ElementQueueOrderIndex orderIndex = root.QueueOrderIndex;
        orderIndex.EnsureCurrent();

        List<OrderedElement> ordered = new(entries.Count);
        List<UIElement>? stale = null;
        foreach ((UIElement element, QueueEntry entry) in entries)
        {
            if (ReferenceEquals(element.Root, root) && orderIndex.TryGetOrdinal(element, out int ordinal))
            {
                ordered.Add(new OrderedElement(element, ordinal, entry.Sequence));
                continue;
            }

            if (ReferenceEquals(element.Root, root) && element.IsPresenceExiting)
            {
                ordered.Add(new OrderedElement(element, int.MaxValue, entry.Sequence));
                continue;
            }

            stale ??= [];
            stale.Add(element);
        }

        if (stale is not null)
        {
            foreach (UIElement element in stale)
            {
                entries.Remove(element);
            }
        }

        ordered.Sort(static (left, right) =>
        {
            int ordinalComparison = left.Ordinal.CompareTo(right.Ordinal);
            return ordinalComparison != 0
                ? ordinalComparison
                : left.Sequence.CompareTo(right.Sequence);
        });
        LastSnapshotSortCount = ordered.Count;

        UIElement[] snapshot = new UIElement[ordered.Count];
        for (int i = 0; i < ordered.Count; i++)
        {
            int destination = reverse ? ordered.Count - i - 1 : i;
            snapshot[destination] = ordered[i].Element;
        }

        return snapshot;
    }

    private readonly record struct QueueEntry(TMetadata Metadata, long Sequence);

    private readonly record struct OrderedElement(UIElement Element, int Ordinal, long Sequence);
}
