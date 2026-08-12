using SkiaSharp;
using Svg.Skia;

namespace Cerneala.Drawing;

internal static class SvgRasterizer
{
    private static readonly object CacheLock = new();
    private static readonly Dictionary<string, CacheEntry> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static byte[] Rasterize(string path)
    {
        FileInfo source = new(path);
        long lastWriteTicks = source.LastWriteTimeUtc.Ticks;
        long length = source.Length;

        lock (CacheLock)
        {
            if (Cache.TryGetValue(path, out CacheEntry cached) &&
                cached.LastWriteTicks == lastWriteTicks &&
                cached.SourceLength == length)
            {
                return cached.PngBytes;
            }

            byte[] pngBytes = RasterizeCore(path);
            Cache[path] = new CacheEntry(lastWriteTicks, length, pngBytes);
            return pngBytes;
        }
    }

    private static byte[] RasterizeCore(string path)
    {
        using SKSvg svg = new();
        SKPicture picture = svg.Load(path)
            ?? throw new InvalidDataException($"SVG '{path}' did not produce a drawable picture.");
        SKRect bounds = picture.CullRect;
        int width = Math.Max(1, (int)MathF.Ceiling(bounds.Width));
        int height = Math.Max(1, (int)MathF.Ceiling(bounds.Height));
        SKImageInfo imageInfo = new(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(imageInfo)
            ?? throw new InvalidOperationException($"Could not allocate a {width}x{height} SVG raster surface.");
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.Translate(-bounds.Left, -bounds.Top);
        surface.Canvas.DrawPicture(picture);
        surface.Canvas.Flush();

        using SKImage image = surface.Snapshot();
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("Could not encode the rasterized SVG as PNG.");
        return data.ToArray();
    }

    private readonly record struct CacheEntry(long LastWriteTicks, long SourceLength, byte[] PngBytes);
}
