using System.Runtime.CompilerServices;
using SkiaSharp;

namespace Cerneala.Drawing.Text;

internal static class SkiaTextBlobCache
{
    private const int MaximumEntriesPerTypeface = 4_096;
    private static readonly ConditionalWeakTable<SKTypeface, TypefaceCache> Caches = new();

    internal static int GetCachedEntryCount(SkiaFont font)
    {
        ArgumentNullException.ThrowIfNull(font);
        return Caches.TryGetValue(font.Typeface, out TypefaceCache? cache)
            ? cache.Count
            : 0;
    }

    public static Lease Rent(SkiaFont font, float size, TextShapeResult shapeResult)
    {
        return Rent(
            font,
            size,
            shapeResult.Text,
            shapeResult.GlyphIdBuffer,
            shapeResult.GlyphPositionBuffer);
    }

    public static Lease Rent(
        SkiaFont font,
        float size,
        string text,
        ushort[] glyphIds,
        DrawPoint[] glyphPositions)
    {
        TypefaceCache cache = Caches.GetValue(font.Typeface, _ => new TypefaceCache());
        CacheKey key = new(text, size);
        return cache.Rent(key, font, glyphIds, glyphPositions);
    }

    private static SKTextBlob Create(
        SkiaFont font,
        float size,
        ushort[] glyphIds,
        DrawPoint[] positions)
    {
        using SKFont skFont = SkiaTextRendering.CreateFont(font, size);
        using SKTextBlobBuilder builder = new();
        builder.AddPositionedRun(glyphIds, skFont, ToPoints(positions));
        return builder.Build() ?? throw new InvalidOperationException("Could not build text blob.");
    }

    private static SKPoint[] ToPoints(DrawPoint[] positions)
    {
        SKPoint[] points = new SKPoint[positions.Length];
        for (int index = 0; index < positions.Length; index++)
        {
            points[index] = new SKPoint(positions[index].X, positions[index].Y);
        }

        return points;
    }

    internal sealed class Lease : IDisposable
    {
        private CacheEntry? entry;

        internal Lease(CacheEntry entry)
        {
            this.entry = entry;
            entry.AddReference();
        }

        public SKTextBlob Value => entry?.Value ?? throw new ObjectDisposedException(nameof(Lease));

        public void Dispose()
        {
            // Copies of a lease reference still return the rental only once.
            Interlocked.Exchange(ref entry, null)?.Release();
        }
    }

    internal sealed class CacheEntry(SKTextBlob value)
    {
        // One reference belongs to the cache; each active lease adds another.
        private int references = 1;

        public SKTextBlob Value { get; } = value;

        public void AddReference() => Interlocked.Increment(ref references);

        public void Release()
        {
            if (Interlocked.Decrement(ref references) == 0)
            {
                Value.Dispose();
            }
        }
    }

    private sealed class TypefaceCache
    {
        private readonly object gate = new();
        private readonly Dictionary<CacheKey, CacheEntry> entries = new();
        private readonly Queue<CacheKey> insertionOrder = new();

        public int Count
        {
            get
            {
                lock (gate)
                {
                    return entries.Count;
                }
            }
        }

        public Lease Rent(CacheKey key, SkiaFont font, ushort[] glyphIds, DrawPoint[] glyphPositions)
        {
            // Admission, eviction and acquiring a lease are one atomic operation.
            lock (gate)
            {
                if (entries.TryGetValue(key, out CacheEntry? existing))
                {
                    return new Lease(existing);
                }

                CacheEntry candidate = new(Create(font, key.Size, glyphIds, glyphPositions));
                if (entries.Count == MaximumEntriesPerTypeface)
                {
                    // Match the shaping cache's FIFO policy, but release native blobs
                    // only once the evicted entry has no outstanding leases.
                    CacheKey oldestKey = insertionOrder.Dequeue();
                    CacheEntry oldest = entries[oldestKey];
                    entries.Remove(oldestKey);
                    oldest.Release();
                }

                entries.Add(key, candidate);
                insertionOrder.Enqueue(key);
                return new Lease(candidate);
            }
        }
    }

    private readonly record struct CacheKey(string Text, float Size);
}
