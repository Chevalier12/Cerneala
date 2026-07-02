using Cerneala.Drawing;
using HarfBuzzSharp;
using HarfBuzzBuffer = HarfBuzzSharp.Buffer;
using HarfBuzzFont = HarfBuzzSharp.Font;

namespace Cerneala.Text;

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

        byte[] fontData = ReadFontData(font);
        using MemoryStream fontStream = new(fontData);
        using Blob blob = Blob.FromStream(fontStream);
        using Face face = new(blob, 0);
        using HarfBuzzFont harfBuzzFont = new(face);
        int scale = Math.Max(1, (int)MathF.Round(font.Size * 64));
        harfBuzzFont.SetScale(scale, scale);
        harfBuzzFont.Shape(buffer);

        return new TextShapeResult(textRun.Text, buffer.Length);
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
}
