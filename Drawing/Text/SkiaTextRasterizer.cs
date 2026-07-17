using System.Buffers;
using System.Collections.Concurrent;
using Cerneala.Drawing;
using SkiaSharp;

namespace Cerneala.Drawing.Text;

public sealed class SkiaTextRasterizer
{
    private const int MaxPooledPaints = 32;
    private static readonly ConcurrentBag<SKPaint> PaintPool = [];
    private static readonly SKSurfaceProperties RgbHorizontalSurfaceProperties =
        new(SKPixelGeometry.RgbHorizontal);
    private static int pooledPaintCount;
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

        float mappedBaselineX = position.X * coordinateScale;
        float mappedBaselineY = position.Y * coordinateScale;
        DrawPoint pixelPhase = new(
            mappedBaselineX - MathF.Floor(mappedBaselineX),
            mappedBaselineY - MathF.Floor(mappedBaselineY));
        return RasterizeSubpixelAtPhase(textRun, color, coordinateScale, pixelPhase);
    }

    internal RasterizedText[] RasterizeSubpixelAtPhase(
        DrawTextRun textRun,
        Color color,
        float coordinateScale,
        DrawPoint pixelPhase)
    {
        ArgumentNullException.ThrowIfNull(textRun);
        if (!float.IsFinite(coordinateScale) || coordinateScale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(coordinateScale));
        }

        if (!float.IsFinite(pixelPhase.X) ||
            !float.IsFinite(pixelPhase.Y) ||
            pixelPhase.X < 0 ||
            pixelPhase.X >= 1 ||
            pixelPhase.Y < 0 ||
            pixelPhase.Y >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelPhase));
        }

        return WithResolvedFont(
            textRun,
            (resolvedRun, font) => RasterizeSubpixelCore(
                resolvedRun,
                color,
                font,
                coordinateScale,
                pixelPhase));
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
            return RasterizedText.FromOwnedPixels(1, 1, new byte[4], shapeResult);
        }

        using PaintLease paintLease = RentPaint(ToColor(color));
        SKPaint paint = paintLease.Value;

        using SkiaTextBlobCache.Lease textBlobLease = SkiaTextBlobCache.Rent(font, textRun.Size, shapeResult);
        SKTextBlob textBlob = textBlobLease.Value;
        SKRect bounds = textBlob.Bounds;
        int width = Math.Max(1, (int)MathF.Ceiling(bounds.Width));
        int height = Math.Max(1, (int)MathF.Ceiling(bounds.Height));

        using SKBitmap bitmap = new(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawText(textBlob, -bounds.Left, -bounds.Top, paint);

        byte[] pixels = bitmap.Bytes;
        pixels = TrimTransparentLeftColumns(pixels, width, height, out int trimmedLeftColumns);
        return RasterizedText.FromOwnedPixels(
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
        DrawPoint pixelPhase)
    {
        TextShapeResult shapeResult = _textShaper.Shape(textRun);
        if (shapeResult.GlyphCount == 0)
        {
            RasterizedText empty = RasterizedText.FromOwnedPixels(1, 1, new byte[4], shapeResult);
            return [empty, empty, empty];
        }

        using PaintLease paintLease = RentPaint(new SKColor(color.R, color.G, color.B, 255));
        SKPaint paint = paintLease.Value;
        using SkiaTextBlobCache.Lease textBlobLease = SkiaTextBlobCache.Rent(font, textRun.Size, shapeResult);
        SKTextBlob textBlob = textBlobLease.Value;

        SKRect bounds = textBlob.Bounds;
        float phaseX = pixelPhase.X;
        float phaseY = pixelPhase.Y;
        float localBaselineX = phaseX + 1 - MathF.Floor(phaseX + (bounds.Left * coordinateScale));
        float localBaselineY = phaseY + 1 - MathF.Floor(phaseY + (bounds.Top * coordinateScale));
        int width = Math.Max(1, (int)MathF.Ceiling(localBaselineX + (bounds.Right * coordinateScale)) + 1);
        int height = Math.Max(1, (int)MathF.Ceiling(localBaselineY + (bounds.Bottom * coordinateScale)) + 1);
        int globalPixelLeft = (int)MathF.Floor(phaseX + (bounds.Left * coordinateScale)) - 1;
        int globalPixelTop = (int)MathF.Floor(phaseY + (bounds.Top * coordinateScale)) - 1;

        SKImageInfo imageInfo = new(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        bool needsWhiteReference = color.R < 128 || color.G < 128 || color.B < 128;
        bool needsBlackReference = color.R >= 128 || color.G >= 128 || color.B >= 128;
        using SKSurface? whiteReference = needsWhiteReference
            ? RasterizeSubpixelReference(
                imageInfo,
                RgbHorizontalSurfaceProperties,
                textBlob,
                paint,
                coordinateScale,
                localBaselineX,
                localBaselineY,
                SKColors.White)
            : null;
        using SKSurface? blackReference = needsBlackReference
            ? RasterizeSubpixelReference(
                imageInfo,
                RgbHorizontalSurfaceProperties,
                textBlob,
                paint,
                coordinateScale,
                localBaselineX,
                localBaselineY,
                SKColors.Black)
            : null;
        using SKPixmap? whitePixels = whiteReference?.PeekPixels();
        using SKPixmap? blackPixels = blackReference?.PeekPixels();
        ReadOnlySpan<byte> whiteSpan = whitePixels is null ? default : whitePixels.GetPixelSpan();
        ReadOnlySpan<byte> blackSpan = blackPixels is null ? default : blackPixels.GetPixelSpan();
        byte[] layers = CreateSubpixelLayers(whiteSpan, blackSpan, color);
        int layerByteCount = !whiteSpan.IsEmpty ? whiteSpan.Length : blackSpan.Length;
        DrawPoint originOffset = new(
            globalPixelLeft - phaseX,
            globalPixelTop - phaseY);

        return
        [
            RasterizedText.FromPooledPixelSlice(
                width,
                height,
                layers,
                0,
                shapeResult,
                originOffset,
                returnPixelBufferToPool: true),
            RasterizedText.FromPooledPixelSlice(
                width,
                height,
                layers,
                layerByteCount,
                shapeResult,
                originOffset,
                returnPixelBufferToPool: false),
            RasterizedText.FromPooledPixelSlice(
                width,
                height,
                layers,
                layerByteCount * 2,
                shapeResult,
                originOffset,
                returnPixelBufferToPool: false)
        ];
    }

    private static SKSurface RasterizeSubpixelReference(
        SKImageInfo imageInfo,
        SKSurfaceProperties properties,
        SKTextBlob textBlob,
        SKPaint paint,
        float coordinateScale,
        float localBaselineX,
        float localBaselineY,
        SKColor background)
    {
        SKSurface surface = SKSurface.Create(imageInfo, properties)
            ?? throw new InvalidOperationException("Could not create the subpixel text surface.");
        surface.Canvas.Clear(background);
        surface.Canvas.Scale(coordinateScale);
        surface.Canvas.DrawText(
            textBlob,
            localBaselineX / coordinateScale,
            localBaselineY / coordinateScale,
            paint);

        return surface;
    }

    private static byte[] CreateSubpixelLayers(
        ReadOnlySpan<byte> whiteReference,
        ReadOnlySpan<byte> blackReference,
        Color color)
    {
        int layerByteCount = !whiteReference.IsEmpty
            ? whiteReference.Length
            : !blackReference.IsEmpty
                ? blackReference.Length
                : throw new ArgumentException("At least one subpixel reference is required.");
        int totalLayerBytes = checked(layerByteCount * 3);
        byte[] layers = ArrayPool<byte>.Shared.Rent(totalLayerBytes);
        layers.AsSpan(0, totalLayerBytes).Clear();
        bool opaque = color.A == byte.MaxValue;

        for (int index = 0; index < layerByteCount; index += 4)
        {
            byte redCoverage = ApplyOpacity(
                RecoverCoverage(whiteReference, blackReference, index + 2, color.R),
                color.A,
                opaque);
            layers[index] = redCoverage;
            layers[index + 3] = redCoverage;

            int greenIndex = layerByteCount + index;
            byte greenCoverage = ApplyOpacity(
                RecoverCoverage(whiteReference, blackReference, index + 1, color.G),
                color.A,
                opaque);
            layers[greenIndex + 1] = greenCoverage;
            layers[greenIndex + 3] = greenCoverage;

            int blueIndex = (layerByteCount * 2) + index;
            byte blueCoverage = ApplyOpacity(
                RecoverCoverage(whiteReference, blackReference, index, color.B),
                color.A,
                opaque);
            layers[blueIndex + 2] = blueCoverage;
            layers[blueIndex + 3] = blueCoverage;
        }

        return layers;
    }

    private static int RecoverCoverage(
        ReadOnlySpan<byte> whiteReference,
        ReadOnlySpan<byte> blackReference,
        int index,
        byte foreground)
    {
        return foreground >= 128
            ? Math.Clamp(((blackReference[index] * 255) + (foreground / 2)) / foreground, 0, 255)
            : Math.Clamp((((255 - whiteReference[index]) * 255) + ((255 - foreground) / 2)) / (255 - foreground), 0, 255);
    }

    private static byte ApplyOpacity(int coverage, byte opacity, bool opaque)
    {
        return opaque ? (byte)coverage : MultiplyByte(coverage, opacity);
    }

    private static byte MultiplyByte(int left, int right)
    {
        return (byte)(((left * right) + 127) / 255);
    }

    private static SKColor ToColor(Color color)
    {
        return new SKColor(color.R, color.G, color.B, color.A);
    }

    private static PaintLease RentPaint(SKColor color)
    {
        SKPaint paint;
        if (PaintPool.TryTake(out SKPaint? pooledPaint))
        {
            Interlocked.Decrement(ref pooledPaintCount);
            paint = pooledPaint;
        }
        else
        {
            paint = new SKPaint();
        }

        paint.Color = color;
        paint.IsAntialias = true;
        return new PaintLease(paint);
    }

    private static void ReturnPaint(SKPaint paint)
    {
        if (Interlocked.Increment(ref pooledPaintCount) <= MaxPooledPaints)
        {
            PaintPool.Add(paint);
            return;
        }

        Interlocked.Decrement(ref pooledPaintCount);
        paint.Dispose();
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

    private readonly struct PaintLease(SKPaint value) : IDisposable
    {
        internal SKPaint Value { get; } = value;

        public void Dispose()
        {
            ReturnPaint(Value);
        }
    }

}
