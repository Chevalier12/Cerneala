using Cerneala.Drawing;
using Cerneala.UI.Resources;
using SkiaSharp;

namespace Cerneala.Backends.SdlGpu;

internal sealed class SdlGpuImageLoader : IImageLoader
{
    public IDrawImage Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Image path cannot be empty.", nameof(path));
        }

        string resolved = ResolvePath(path);
        if (string.Equals(Path.GetExtension(resolved), ".svg", StringComparison.OrdinalIgnoreCase))
        {
            using MemoryStream raster = new(SvgRasterizer.Rasterize(resolved), writable: false);
            return Load(raster);
        }

        using FileStream stream = File.OpenRead(resolved);
        return Load(stream);
    }

    public IDrawImage Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
        {
            throw new ArgumentException("Image stream must be readable.", nameof(stream));
        }

        using SKBitmap decoded = SKBitmap.Decode(stream) ??
            throw new InvalidDataException("Skia could not decode the image stream.");
        SKImageInfo info = new(
            decoded.Width,
            decoded.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul);
        using SKBitmap rgba = new(info);
        using (SKCanvas canvas = new(rgba))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(
                decoded,
                0,
                0,
                new SKSamplingOptions(SKFilterMode.Nearest));
            canvas.Flush();
        }

        return new SdlGpuImage(decoded.Width, decoded.Height, rgba.Bytes);
    }

    private static string ResolvePath(string path)
    {
        string workingDirectoryPath = Path.GetFullPath(path);
        return Path.IsPathFullyQualified(path) || File.Exists(workingDirectoryPath)
            ? workingDirectoryPath
            : Path.GetFullPath(path, AppContext.BaseDirectory);
    }
}

internal sealed class SdlGpuImage : IDrawImage, IDrawImageInvalidationSource, IDisposable
{
    private byte[]? rgbaPixels;

    public SdlGpuImage(int width, int height, byte[] rgbaPixels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentNullException.ThrowIfNull(rgbaPixels);
        if (rgbaPixels.Length != checked(width * height * 4))
        {
            throw new ArgumentException(
                "RGBA pixel data must contain exactly width * height * 4 bytes.",
                nameof(rgbaPixels));
        }

        Width = width;
        Height = height;
        this.rgbaPixels = (byte[])rgbaPixels.Clone();
    }

    public int Width { get; }

    public int Height { get; }

    internal ReadOnlyMemory<byte> RgbaPixels => rgbaPixels ??
        throw new ObjectDisposedException(nameof(SdlGpuImage));

    public event EventHandler? ContentChanged;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref rgbaPixels, null) is null)
        {
            return;
        }

        ContentChanged?.Invoke(this, EventArgs.Empty);
        ContentChanged = null;
    }
}
