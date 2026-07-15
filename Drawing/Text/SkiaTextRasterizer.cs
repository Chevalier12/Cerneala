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

    public RasterizedText Rasterize(DrawTextRun textRun, Color color)
    {
        ArgumentNullException.ThrowIfNull(textRun);

        return WithResolvedFont(textRun, (resolvedRun, font) => RasterizeCore(resolvedRun, color, font));
    }

    public RasterizedText RasterizeMask(DrawTextRun textRun)
    {
        ArgumentNullException.ThrowIfNull(textRun);
        return WithResolvedFont(textRun, (resolvedRun, font) => RasterizeCore(resolvedRun, Color.White, font));
    }

    internal RasterizedText[] RasterizeSubpixelMask(
        DrawTextRun textRun,
        float coordinateScale,
        DrawPoint position)
    {
        return RasterizeSubpixel(textRun, Color.White, coordinateScale, position);
    }

    internal RasterizedText[] RasterizeSubpixel(
        DrawTextRun textRun,
        Color color,
        float coordinateScale,
        DrawPoint position)
    {
        ArgumentNullException.ThrowIfNull(textRun);
        if (!float.IsFinite(coordinateScale) || coordinateScale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(coordinateScale));
        }

        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        return WithResolvedFont(
            textRun,
            (resolvedRun, font) => RasterizeSubpixelCore(resolvedRun, color, font, coordinateScale, position));
    }

    private static TResult WithResolvedFont<TResult>(
        DrawTextRun textRun,
        Func<DrawTextRun, SkiaFont, TResult> rasterize)
    {
        ArgumentNullException.ThrowIfNull(rasterize);

        if (textRun.Font is SkiaFont font)
        {
            return rasterize(textRun, font);
        }

        SKTypeface? matchedTypeface = SKFontManager.Default.MatchFamily(textRun.Font.FamilyName);
        if (matchedTypeface is null)
        {
            SkiaFont fallbackFont = new(SKTypeface.Default, textRun.Font.FamilyName, textRun.Size);
            return rasterize(new DrawTextRun(fallbackFont, textRun.Text, textRun.Size), fallbackFont);
        }

        using (matchedTypeface)
        {
            SkiaFont resolvedFont = new(matchedTypeface, textRun.Font.FamilyName, textRun.Size);
            return rasterize(new DrawTextRun(resolvedFont, textRun.Text, textRun.Size), resolvedFont);
        }
    }

    private RasterizedText RasterizeCore(DrawTextRun textRun, Color color, SkiaFont font)
    {
        TextShapeResult shapeResult = _textShaper.Shape(textRun);

        if (shapeResult.GlyphCount == 0)
        {
            return new RasterizedText(1, 1, new byte[4], shapeResult);
        }

        using SKFont skFont = SkiaTextRendering.CreateFont(font, textRun.Size);
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

    private RasterizedText[] RasterizeSubpixelCore(
        DrawTextRun textRun,
        Color color,
        SkiaFont font,
        float coordinateScale,
        DrawPoint position)
    {
        TextShapeResult shapeResult = _textShaper.Shape(textRun);
        if (shapeResult.GlyphCount == 0)
        {
            RasterizedText empty = new(1, 1, new byte[4], shapeResult);
            return [empty, empty, empty];
        }

        using SKFont skFont = SkiaTextRendering.CreateFont(font, textRun.Size);
        using SKPaint paint = new()
        {
            Color = new SKColor(color.R, color.G, color.B, 255),
            IsAntialias = true
        };
        using SKTextBlob textBlob = CreateTextBlob(skFont, shapeResult);

        SKRect bounds = textBlob.Bounds;
        float mappedBaselineX = position.X * coordinateScale;
        float mappedBaselineY = position.Y * coordinateScale;
        float phaseX = mappedBaselineX - MathF.Floor(mappedBaselineX);
        float phaseY = mappedBaselineY - MathF.Floor(mappedBaselineY);
        float localBaselineX = phaseX + 1 - MathF.Floor(phaseX + (bounds.Left * coordinateScale));
        float localBaselineY = phaseY + 1 - MathF.Floor(phaseY + (bounds.Top * coordinateScale));
        int width = Math.Max(1, (int)MathF.Ceiling(localBaselineX + (bounds.Right * coordinateScale)) + 1);
        int height = Math.Max(1, (int)MathF.Ceiling(localBaselineY + (bounds.Bottom * coordinateScale)) + 1);
        int globalPixelLeft = (int)MathF.Floor(mappedBaselineX + (bounds.Left * coordinateScale)) - 1;
        int globalPixelTop = (int)MathF.Floor(mappedBaselineY + (bounds.Top * coordinateScale)) - 1;

        SKImageInfo imageInfo = new(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKSurfaceProperties properties = new(SKPixelGeometry.RgbHorizontal);
        byte[] whiteReference = RasterizeSubpixelReference(
            imageInfo,
            properties,
            textBlob,
            paint,
            coordinateScale,
            localBaselineX,
            localBaselineY,
            SKColors.White);
        byte[] blackReference = RasterizeSubpixelReference(
            imageInfo,
            properties,
            textBlob,
            paint,
            coordinateScale,
            localBaselineX,
            localBaselineY,
            SKColors.Black);
        byte[][] layers = CreateSubpixelLayers(whiteReference, blackReference, color);
        DrawPoint originOffset = new(
            globalPixelLeft - mappedBaselineX,
            globalPixelTop - mappedBaselineY);

        return
        [
            new RasterizedText(width, height, layers[0], shapeResult, originOffset),
            new RasterizedText(width, height, layers[1], shapeResult, originOffset),
            new RasterizedText(width, height, layers[2], shapeResult, originOffset)
        ];
    }

    private static byte[] RasterizeSubpixelReference(
        SKImageInfo imageInfo,
        SKSurfaceProperties properties,
        SKTextBlob textBlob,
        SKPaint paint,
        float coordinateScale,
        float localBaselineX,
        float localBaselineY,
        SKColor background)
    {
        using SKSurface surface = SKSurface.Create(imageInfo, properties)
            ?? throw new InvalidOperationException("Could not create the subpixel text surface.");
        surface.Canvas.Clear(background);
        surface.Canvas.Scale(coordinateScale);
        surface.Canvas.DrawText(
            textBlob,
            localBaselineX / coordinateScale,
            localBaselineY / coordinateScale,
            paint);

        using SKImage image = surface.Snapshot();
        using SKPixmap pixmap = image.PeekPixels();
        return pixmap.GetPixelSpan().ToArray();
    }

    private static byte[][] CreateSubpixelLayers(byte[] whiteReference, byte[] blackReference, Color color)
    {
        byte[][] layers =
        [
            new byte[whiteReference.Length],
            new byte[whiteReference.Length],
            new byte[whiteReference.Length]
        ];

        for (int index = 0; index < whiteReference.Length; index += 4)
        {
            WriteLayerPixel(layers[0], index, channel: 0, RecoverCoverage(whiteReference[index + 2], blackReference[index + 2], color.R), 255, color.A);
            WriteLayerPixel(layers[1], index, channel: 1, RecoverCoverage(whiteReference[index + 1], blackReference[index + 1], color.G), 255, color.A);
            WriteLayerPixel(layers[2], index, channel: 2, RecoverCoverage(whiteReference[index], blackReference[index], color.B), 255, color.A);
        }

        return layers;
    }

    private static int RecoverCoverage(byte overWhite, byte overBlack, byte foreground)
    {
        return foreground >= 128
            ? Math.Clamp(((overBlack * 255) + (foreground / 2)) / foreground, 0, 255)
            : Math.Clamp((((255 - overWhite) * 255) + ((255 - foreground) / 2)) / (255 - foreground), 0, 255);
    }

    private static void WriteLayerPixel(
        byte[] pixels,
        int index,
        int channel,
        int coverage,
        byte foreground,
        byte opacity)
    {
        byte effectiveCoverage = MultiplyByte(coverage, opacity);
        pixels[index + channel] = MultiplyByte(foreground, effectiveCoverage);
        pixels[index + 3] = effectiveCoverage;
    }

    private static byte MultiplyByte(int left, int right)
    {
        return (byte)(((left * right) + 127) / 255);
    }

    private static SKColor ToColor(Color color)
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
