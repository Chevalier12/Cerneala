using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using SkiaSharp;

namespace Cerneala.Drawing.Text;

internal static class SkiaTextBlobCache
{
    private const int MaximumEntriesPerTypeface = 4_096;
    private static readonly ConditionalWeakTable<SKTypeface, TypefaceCache> Caches = new();

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
        return cache.Rent(
            key,
            () => Create(font, size, glyphIds, glyphPositions));
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

    internal readonly struct Lease(SKTextBlob value, bool ownsValue) : IDisposable
    {
        public SKTextBlob Value { get; } = value;

        public void Dispose()
        {
            if (ownsValue)
            {
                Value.Dispose();
            }
        }
    }

    private sealed class TypefaceCache
    {
        private readonly ConcurrentDictionary<CacheKey, Lazy<SKTextBlob>> entries = new();

        public Lease Rent(CacheKey key, Func<SKTextBlob> create)
        {
            if (entries.TryGetValue(key, out Lazy<SKTextBlob>? existing))
            {
                return new Lease(existing.Value, ownsValue: false);
            }

            if (entries.Count >= MaximumEntriesPerTypeface)
            {
                return new Lease(create(), ownsValue: true);
            }

            Lazy<SKTextBlob> candidate = new(
                create,
                LazyThreadSafetyMode.ExecutionAndPublication);
            Lazy<SKTextBlob> cached = entries.GetOrAdd(key, candidate);
            return new Lease(cached.Value, ownsValue: false);
        }
    }

    private readonly record struct CacheKey(string Text, float Size);
}
