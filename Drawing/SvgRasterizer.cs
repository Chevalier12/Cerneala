using SkiaSharp;
using Svg.Skia;
using System.Security.Cryptography;

namespace Cerneala.Drawing;

internal static class SvgRasterizer
{
    internal const string CompiledSidecarSuffix = ".cerneala.png";

    private static readonly object CacheLock = new();
    private static readonly Dictionary<string, CacheEntry> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static byte[] Rasterize(string path)
    {
        string compiledPath = path + CompiledSidecarSuffix;
        bool hasCompiledSidecar = HasCurrentCompiledSidecar(path, compiledPath);
        string artifactPath = hasCompiledSidecar ? compiledPath : path;
        FileInfo source = new(artifactPath);
        long lastWriteTicks = source.LastWriteTimeUtc.Ticks;
        long length = source.Length;

        lock (CacheLock)
        {
            if (Cache.TryGetValue(path, out CacheEntry cached) &&
                string.Equals(cached.ArtifactPath, artifactPath, StringComparison.OrdinalIgnoreCase) &&
                cached.LastWriteTicks == lastWriteTicks &&
                cached.SourceLength == length)
            {
                return cached.PngBytes;
            }

            byte[] pngBytes = hasCompiledSidecar
                ? File.ReadAllBytes(compiledPath)
                : RasterizeCore(path);
            Cache[path] = new CacheEntry(artifactPath, lastWriteTicks, length, pngBytes);
            return pngBytes;
        }
    }

    internal static byte[] Compile(string path) => RasterizeCore(path);

    internal static string ComputeSourceSignature(string path)
    {
        using FileStream source = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(source));
    }

    private static bool HasCurrentCompiledSidecar(string sourcePath, string compiledPath)
    {
        string signaturePath = compiledPath + ".sha256";
        if (!File.Exists(compiledPath) || !File.Exists(signaturePath))
        {
            return false;
        }

        string expectedSignature = File.ReadAllText(signaturePath).Trim();
        return expectedSignature.Length == 64 &&
            string.Equals(
                expectedSignature,
                ComputeSourceSignature(sourcePath),
                StringComparison.OrdinalIgnoreCase);
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

    private readonly record struct CacheEntry(
        string ArtifactPath,
        long LastWriteTicks,
        long SourceLength,
        byte[] PngBytes);
}
