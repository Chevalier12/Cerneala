using SkiaSharp;
using Svg.Skia;
using System.Security.Cryptography;

namespace Cerneala.Drawing;

internal static class SvgRasterizer
{
    internal const string CompiledSidecarSuffix = ".cerneala.png";

    private static readonly object CacheLock = new();
    private static readonly Dictionary<string, CacheEntry> Cache = new(StringComparer.OrdinalIgnoreCase);

    internal static RasterLease Acquire(string path)
    {
        string compiledPath = path + CompiledSidecarSuffix;
        bool hasCompiledSidecar = HasCurrentCompiledSidecar(path, compiledPath);
        string artifactPath = hasCompiledSidecar ? compiledPath : path;
        FileInfo source = new(artifactPath);
        long lastWriteTicks = source.LastWriteTimeUtc.Ticks;
        long length = source.Length;

        lock (CacheLock)
        {
            CacheEntry entry;
            if (Cache.TryGetValue(path, out CacheEntry? cached) &&
                string.Equals(cached.ArtifactPath, artifactPath, StringComparison.OrdinalIgnoreCase) &&
                cached.LastWriteTicks == lastWriteTicks &&
                cached.SourceLength == length)
            {
                entry = cached;
            }
            else
            {
                byte[] pngBytes = hasCompiledSidecar
                    ? File.ReadAllBytes(compiledPath)
                    : RasterizeCore(path);
                entry = new CacheEntry(path, artifactPath, lastWriteTicks, length, pngBytes);
                Cache[path] = entry;
            }
            RasterLease lease = new(entry);
            entry.Acquisitions = checked(entry.Acquisitions + 1);
            return lease;
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

    internal sealed class CacheEntry(
        string path,
        string artifactPath,
        long lastWriteTicks,
        long sourceLength,
        byte[] pngBytes)
    {
        internal string Path { get; } = path;
        internal string ArtifactPath { get; } = artifactPath;
        internal long LastWriteTicks { get; } = lastWriteTicks;
        internal long SourceLength { get; } = sourceLength;
        internal byte[] PngBytes { get; } = pngBytes;
        internal int Acquisitions;
    }

    internal sealed class RasterLease(CacheEntry entry) : IDisposable
    {
        private CacheEntry? current = entry;

        internal byte[] PngBytes => Volatile.Read(ref current)?.PngBytes ??
            throw new ObjectDisposedException(nameof(RasterLease));

        public void Dispose()
        {
            CacheEntry? released = Interlocked.Exchange(ref current, null);
            if (released is null) { return; }
            lock (CacheLock)
            {
                released.Acquisitions--;
                if (released.Acquisitions == 0 &&
                    Cache.TryGetValue(released.Path, out CacheEntry? cached) &&
                    ReferenceEquals(cached, released))
                {
                    Cache.Remove(released.Path);
                }
            }
        }
    }
}
