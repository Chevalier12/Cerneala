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

        if (textRun.Font is SkiaFont font)
        {
            return RasterizeCore(textRun, color, font);
        }

        SKTypeface? matchedTypeface = SKFontManager.Default.MatchFamily(textRun.Font.FamilyName);
        if (matchedTypeface is null)
        {
            SkiaFont fallbackFont = new(SKTypeface.Default, textRun.Font.FamilyName, textRun.Size);
            return RasterizeCore(new DrawTextRun(fallbackFont, textRun.Text, textRun.Size), color, fallbackFont);
        }

        using (matchedTypeface)
        {
            SkiaFont resolvedFont = new(matchedTypeface, textRun.Font.FamilyName, textRun.Size);
            return RasterizeCore(new DrawTextRun(resolvedFont, textRun.Text, textRun.Size), color, resolvedFont);
        }
    }

    private RasterizedText RasterizeCore(DrawTextRun textRun, DrawColor color, SkiaFont font)
    {
        TextShapeResult shapeResult = _textShaper.Shape(textRun);

        if (shapeResult.GlyphCount == 0)
        {
            return new RasterizedText(1, 1, new byte[4], shapeResult);
        }

        using SKFont skFont = new(font.Typeface, textRun.Size);
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
        pixels = TrimTransparentLeftColumns(pixels, width, height, out int trimmedLeftColumns);
        return new RasterizedText(
            width - trimmedLeftColumns,
            height,
            pixels,
            shapeResult,
            new DrawPoint(bounds.Left + trimmedLeftColumns, bounds.Top));
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

    private static byte[] TrimTransparentLeftColumns(byte[] pixels, int width, int height, out int trimmedColumns)
    {
        trimmedColumns = 0;
        while (trimmedColumns < width - 1 && IsColumnTransparent(pixels, width, height, trimmedColumns))
        {
            trimmedColumns++;
        }

        if (trimmedColumns == 0)
        {
            return pixels;
        }

        int nextWidth = width - trimmedColumns;
        byte[] trimmed = new byte[nextWidth * height * 4];
        for (int y = 0; y < height; y++)
        {
            int sourceOffset = ((y * width) + trimmedColumns) * 4;
            int destinationOffset = (y * nextWidth) * 4;
            Buffer.BlockCopy(pixels, sourceOffset, trimmed, destinationOffset, nextWidth * 4);
        }

        return trimmed;
    }

    private static bool IsColumnTransparent(byte[] pixels, int width, int height, int x)
    {
        for (int y = 0; y < height; y++)
        {
            int alphaIndex = (((y * width) + x) * 4) + 3;
            if (pixels[alphaIndex] != 0)
            {
                return false;
            }
        }

        return true;
    }
}
