using Cerneala.Drawing;
using HarfBuzzSharp;
using SkiaSharp;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using HarfBuzzBuffer = HarfBuzzSharp.Buffer;
using HarfBuzzFont = HarfBuzzSharp.Font;

namespace Cerneala.Drawing.Text;

public sealed class SkiaTextShaper
{
    private const int MaximumCachedShapesPerTypeface = 4_096;
    private static readonly ConditionalWeakTable<SKTypeface, CachedHarfBuzzFace> FaceCache = new();
    private static readonly ConditionalWeakTable<SKTypeface, ShapeCache> ShapesByTypeface = new();

    public TextShapeResult Shape(DrawTextRun textRun)
    {
        ArgumentNullException.ThrowIfNull(textRun);

        if (textRun.Font is not SkiaFont font)
        {
            throw new InvalidOperationException("SkiaTextShaper requires a SkiaFont.");
        }

        ShapeCache cache = ShapesByTypeface.GetValue(font.Typeface, _ => new ShapeCache());
        ShapeCacheKey key = new(textRun.Text, textRun.Size);
        return cache.GetOrAdd(key, () => ShapeCore(textRun, font));
    }

    private static TextShapeResult ShapeCore(DrawTextRun textRun, SkiaFont font)
    {
        using HarfBuzzBuffer buffer = new();
        buffer.AddUtf16(textRun.Text);
        buffer.GuessSegmentProperties();

        CachedHarfBuzzFace cachedFace = FaceCache.GetValue(
            font.Typeface,
            _ => new CachedHarfBuzzFace(OpenTypeFontData.Read(font)));
        using HarfBuzzFont harfBuzzFont = new(cachedFace.Face);
        int unitsPerEm = cachedFace.UnitsPerEm;
        harfBuzzFont.SetScale(unitsPerEm, unitsPerEm);
        harfBuzzFont.SetFunctionsOpenType();
        harfBuzzFont.Shape(buffer);

        ushort[] glyphIds = GetGlyphIds(buffer);
        double textScale = textRun.Size / unitsPerEm;
        DrawPoint[] glyphPositions = GetGlyphPositions(buffer, textScale, out float advanceWidth);
        return TextShapeResult.FromOwnedGlyphs(
            textRun.Text,
            glyphIds,
            glyphPositions,
            advanceWidth,
            GetOriginOffset(font, textRun, glyphIds, glyphPositions));
    }

    private static ushort[] GetGlyphIds(HarfBuzzBuffer buffer)
    {
        ReadOnlySpan<GlyphInfo> glyphInfos = buffer.GetGlyphInfoSpan();
        ushort[] glyphIds = new ushort[glyphInfos.Length];

        for (int i = 0; i < glyphInfos.Length; i++)
        {
            glyphIds[i] = checked((ushort)glyphInfos[i].Codepoint);
        }

        return glyphIds;
    }

    private static DrawPoint[] GetGlyphPositions(
        HarfBuzzBuffer buffer,
        double textScale,
        out float advanceWidth)
    {
        ReadOnlySpan<GlyphPosition> glyphPositions = buffer.GetGlyphPositionSpan();
        DrawPoint[] positions = new DrawPoint[glyphPositions.Length];
        float x = 0;
        float y = 0;

        for (int i = 0; i < glyphPositions.Length; i++)
        {
            GlyphPosition glyphPosition = glyphPositions[i];
            positions[i] = new DrawPoint(
                x + ToPixels(glyphPosition.XOffset, textScale),
                y - ToPixels(glyphPosition.YOffset, textScale));
            x += ToPixels(glyphPosition.XAdvance, textScale);
            y -= ToPixels(glyphPosition.YAdvance, textScale);
        }

        advanceWidth = x;
        return positions;
    }

    private static DrawPoint GetOriginOffset(
        SkiaFont font,
        DrawTextRun textRun,
        ushort[] glyphIds,
        DrawPoint[] glyphPositions)
    {
        if (glyphIds.Length == 0)
        {
            return default;
        }

        using SkiaTextBlobCache.Lease lease = SkiaTextBlobCache.Rent(
            font,
            textRun.Size,
            textRun.Text,
            glyphIds,
            glyphPositions);
        SKRect bounds = lease.Value.Bounds;
        return new DrawPoint(bounds.Left, bounds.Top);
    }

    private static float ToPixels(int harfBuzzValue, double textScale)
    {
        return (float)(harfBuzzValue * textScale);
    }

    private sealed class CachedHarfBuzzFace
    {
        private readonly Blob blob;

        public CachedHarfBuzzFace(OpenTypeFontData data)
        {
            blob = data.CreatePinnedBlob();
            Face = new Face(blob, data.FaceIndex);
            UnitsPerEm = Math.Max(1, Face.UnitsPerEm);
        }

        public Face Face { get; }

        public int UnitsPerEm { get; }
    }

    private sealed class ShapeCache
    {
        private readonly ConcurrentDictionary<ShapeCacheKey, Lazy<TextShapeResult>> entries = new();
        private readonly ConcurrentQueue<ShapeCacheKey> insertionOrder = new();

        public TextShapeResult GetOrAdd(ShapeCacheKey key, Func<TextShapeResult> create)
        {
            if (entries.TryGetValue(key, out Lazy<TextShapeResult>? existing))
            {
                return existing.Value;
            }

            Lazy<TextShapeResult> candidate = new(
                create,
                LazyThreadSafetyMode.ExecutionAndPublication);
            Lazy<TextShapeResult> cached = entries.GetOrAdd(key, candidate);
            if (ReferenceEquals(cached, candidate))
            {
                insertionOrder.Enqueue(key);
                Trim();
            }

            return cached.Value;
        }

        private void Trim()
        {
            while (entries.Count > MaximumCachedShapesPerTypeface &&
                insertionOrder.TryDequeue(out ShapeCacheKey oldest))
            {
                entries.TryRemove(oldest, out _);
            }
        }
    }

    private readonly record struct ShapeCacheKey(string Text, float Size);
}
