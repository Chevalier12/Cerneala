using Cerneala.Drawing;
using SkiaSharp;

namespace Cerneala.Drawing.Text;

public sealed class SkiaTextRasterizer
{
    private readonly SkiaTextShaper _textShaper;

    public SkiaTextRasterizer()
        : this(new SkiaTextShaper())
    {
    }

    public SkiaTextRasterizer(SkiaTextShaper textShaper)
    {
        _textShaper = textShaper ?? throw new ArgumentNullException(nameof(textShaper));
    }

    public RasterizedText Rasterize(DrawTextRun textRun, DrawColor color)
    {
        ArgumentNullException.ThrowIfNull(textRun);

        if (textRun.Font is not SkiaFont font)
        {
            throw new InvalidOperationException("SkiaTextRasterizer requires a SkiaFont.");
        }

        TextShapeResult shapeResult = _textShaper.Shape(textRun);
        using SKFont skFont = new(font.Typeface, font.Size);
        using SKPaint paint = new()
        {
            Color = ToColor(color),
            IsAntialias = true
        };

        using SKTextBlob textBlob = CreateTextBlob(skFont, shapeResult);
        SKRect bounds = textBlob.Bounds;
        int width = Math.Max(1, (int)MathF.Ceiling(bounds.Width));
        int height = Math.Max(1, (int)MathF.Ceiling(bounds.Height));

        using SKBitmap bitmap = new(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawText(textBlob, -bounds.Left, -bounds.Top, paint);

        byte[] pixels = bitmap.Bytes;
        return new RasterizedText(width, height, pixels, shapeResult);
    }

    private static SKColor ToColor(DrawColor color)
    {
        return new SKColor(color.R, color.G, color.B, color.A);
    }

    private static SKTextBlob CreateTextBlob(SKFont font, TextShapeResult shapeResult)
    {
        using SKTextBlobBuilder builder = new();
        builder.AddPositionedRun(shapeResult.GlyphIds, font, ToPoints(shapeResult.GlyphPositions));
        return builder.Build() ?? throw new InvalidOperationException("Could not build text blob.");
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
}
