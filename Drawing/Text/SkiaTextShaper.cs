using Cerneala.Drawing;
using HarfBuzzSharp;
using SkiaSharp;
using System.Runtime.InteropServices;
using HarfBuzzBuffer = HarfBuzzSharp.Buffer;
using HarfBuzzFont = HarfBuzzSharp.Font;

namespace Cerneala.Drawing.Text;

public sealed class SkiaTextShaper
{
    public TextShapeResult Shape(DrawTextRun textRun)
    {
        ArgumentNullException.ThrowIfNull(textRun);

        if (textRun.Font is not SkiaFont font)
        {
            throw new InvalidOperationException("SkiaTextShaper requires a SkiaFont.");
        }

        using HarfBuzzBuffer buffer = new();
        buffer.AddUtf16(textRun.Text);
        buffer.GuessSegmentProperties();

        using Blob blob = CreatePinnedBlob(ReadFontData(font));
        using Face face = new(blob, 0);
        using HarfBuzzFont harfBuzzFont = new(face);
        int unitsPerEm = Math.Max(1, face.UnitsPerEm);
        harfBuzzFont.SetScale(unitsPerEm, unitsPerEm);
        harfBuzzFont.SetFunctionsOpenType();
        harfBuzzFont.Shape(buffer);

        ushort[] glyphIds = GetGlyphIds(buffer);
        double textScale = textRun.Size / unitsPerEm;
        DrawPoint[] glyphPositions = GetGlyphPositions(buffer, textScale, out float advanceWidth);
        return new TextShapeResult(
            textRun.Text,
            buffer.Length,
            glyphIds,
            glyphPositions,
            advanceWidth,
            GetOriginOffset(font, textRun.Size, glyphIds, glyphPositions));
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

    private static DrawPoint GetOriginOffset(SkiaFont font, float size, ushort[] glyphIds, DrawPoint[] glyphPositions)
    {
        if (glyphIds.Length == 0)
        {
            return default;
        }

        using SKFont skFont = new(font.Typeface, size);
        using SKTextBlobBuilder builder = new();
        builder.AddPositionedRun(glyphIds, skFont, ToPoints(glyphPositions));
        using SKTextBlob textBlob = builder.Build() ?? throw new InvalidOperationException("Could not build text blob.");
        SKRect bounds = textBlob.Bounds;
        return new DrawPoint(bounds.Left, bounds.Top);
    }

    private static SKPoint[] ToPoints(DrawPoint[] positions)
    {
        SKPoint[] points = new SKPoint[positions.Length];

        for (int i = 0; i < positions.Length; i++)
        {
            points[i] = new SKPoint(positions[i].X, positions[i].Y);
        }

        return points;
    }

    private static byte[] ReadFontData(SkiaFont font)
    {
        using SkiaSharp.SKStreamAsset stream = font.Typeface.OpenStream();
        byte[] data = new byte[stream.Length];
        int read = stream.Read(data, data.Length);

        if (read != data.Length)
        {
            Array.Resize(ref data, read);
        }

        return data;
    }

    private static Blob CreatePinnedBlob(byte[] data)
    {
        if (data.Length == 0)
        {
            throw new InvalidOperationException("Cannot shape text because the font data is empty.");
        }

        GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        bool released = false;
        return new Blob(handle.AddrOfPinnedObject(), data.Length, MemoryMode.ReadOnly, () =>
        {
            if (released)
            {
                return;
            }

            released = true;
            handle.Free();
        });
    }

    private static float ToPixels(int harfBuzzValue, double textScale)
    {
        return (float)(harfBuzzValue * textScale);
    }
}
