namespace Cerneala.Timbre.Engine;

// Immutable decoded payloads shared by playbacks. Bytes are reserved before
// allocation; active playbacks pin entries, and eviction is LRU over unpinned
// entries only.
internal sealed class SoundPayloadCache(long capacityBytes)
{
    private readonly object gate = new();
    private readonly Dictionary<object, Entry> entries = [];
    private readonly LinkedList<Entry> recency = new();
    private long reservedBytes;

    internal long Bytes
    {
        get
        {
            lock (gate)
            {
                return reservedBytes;
            }
        }
    }

    internal int Count
    {
        get
        {
            lock (gate)
            {
                return entries.Count;
            }
        }
    }

    internal bool TryAcquire(object key, out Entry entry)
    {
        lock (gate)
        {
            if (entries.TryGetValue(key, out entry!))
            {
                entry.Pins++;
                Touch(entry);
                return true;
            }

            return false;
        }
    }

    internal bool Contains(object key)
    {
        lock (gate)
        {
            return entries.ContainsKey(key);
        }
    }

    internal bool TryReserve(long bytes)
    {
        lock (gate)
        {
            LinkedListNode<Entry>? candidate = recency.First;
            while (reservedBytes + bytes > capacityBytes && candidate is not null)
            {
                LinkedListNode<Entry>? next = candidate.Next;
                if (candidate.Value.Pins == 0)
                {
                    Evict(candidate.Value);
                }

                candidate = next;
            }

            if (reservedBytes + bytes > capacityBytes)
            {
                return false;
            }

            reservedBytes += bytes;
            return true;
        }
    }

    internal void Unreserve(long bytes)
    {
        lock (gate)
        {
            reservedBytes -= bytes;
        }
    }

    // `bytes` must already be reserved. A concurrent load of the same key keeps
    // the first entry and returns this reservation.
    internal Entry Add(object key, float[] samples, long frames, long bytes, bool pin)
    {
        lock (gate)
        {
            if (entries.TryGetValue(key, out Entry? existing))
            {
                reservedBytes -= bytes;
                if (pin)
                {
                    existing.Pins++;
                }

                Touch(existing);
                return existing;
            }

            Entry entry = new(key, samples, frames, bytes) { Pins = pin ? 1 : 0 };
            entry.Node = recency.AddLast(entry);
            entries.Add(key, entry);
            return entry;
        }
    }

    internal void Release(Entry entry)
    {
        lock (gate)
        {
            entry.Pins--;
        }
    }

    internal void Clear()
    {
        lock (gate)
        {
            entries.Clear();
            recency.Clear();
            reservedBytes = 0;
        }
    }

    private void Touch(Entry entry)
    {
        if (entry.Node is not null)
        {
            recency.Remove(entry.Node);
            recency.AddLast(entry.Node);
        }
    }

    private void Evict(Entry entry)
    {
        entries.Remove(entry.Key);
        recency.Remove(entry.Node!);
        entry.Node = null;
        reservedBytes -= entry.Bytes;
    }

    internal sealed class Entry(object key, float[] samples, long frames, long bytes)
    {
        internal object Key { get; } = key;

        internal float[] Samples { get; } = samples;

        internal long Frames { get; } = frames;

        internal long Bytes { get; } = bytes;

        internal int Pins { get; set; }

        internal LinkedListNode<Entry>? Node { get; set; }
    }
}
