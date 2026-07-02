using Cerneala.Drawing;
using SkiaSharp;

namespace Cerneala.Text;

public sealed class SkiaTextRasterizer
{
    public RasterizedText Rasterize(DrawTextRun textRun, DrawColor color)
    {
        ArgumentNullException.ThrowIfNull(textRun);

        if (textRun.Font is not SkiaFont font)
        {
            throw new InvalidOperationException("SkiaTextRasterizer requires a SkiaFont.");
        }

        using SKFont skFont = new(font.Typeface, font.Size);
        using SKPaint paint = new()
        {
            Color = ToColor(color),
            IsAntialias = true
        };

        SKRect bounds = new();
        skFont.MeasureText(textRun.Text, out bounds, paint);
        int width = Math.Max(1, (int)MathF.Ceiling(bounds.Width));
        int height = Math.Max(1, (int)MathF.Ceiling(bounds.Height));

        using SKBitmap bitmap = new(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawText(textRun.Text, -bounds.Left, -bounds.Top, SKTextAlign.Left, skFont, paint);

        byte[] pixels = bitmap.Bytes;
        return new RasterizedText(width, height, pixels);
    }

    private static SKColor ToColor(DrawColor color)
    {
        return new SKColor(color.R, color.G, color.B, color.A);
    }
}
