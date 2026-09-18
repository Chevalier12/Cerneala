using Cerneala.Drawing;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;
using SkiaSharp;

namespace Cerneala.Tests.Controls;

public sealed class SvgImageTests
{
    [Fact]
    public void SvgImageRasterizesGeneralSvgContentThroughImageLoaderStream()
    {
        string path = WriteSvg(
            """
            <svg xmlns="http://www.w3.org/2000/svg" width="12" height="8" viewBox="0 0 12 8">
              <circle cx="6" cy="4" r="3" fill="#ff0000" />
            </svg>
            """);

        try
        {
            RecordingImageLoader loader = new();
            UIRoot root = new();
            root.SetImageLoader(loader);
            SvgImage image = new() { SourcePath = path };

            root.VisualChildren.Add(image);

            Assert.Equal(1, loader.StreamLoadCount);
            Assert.Equal(0, loader.PathLoadCount);
            Assert.Equal(12, image.Source?.Width);
            Assert.Equal(8, image.Source?.Height);
            Assert.True(loader.ContainsVisibleRedPixel);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ChangingSourcePathReloadsAndDisposesThePreviousImage()
    {
        string firstPath = WriteSvg(
            """
            <svg xmlns="http://www.w3.org/2000/svg" width="4" height="3">
              <rect width="4" height="3" fill="#00ff00" />
            </svg>
            """);
        string secondPath = WriteSvg(
            """
            <svg xmlns="http://www.w3.org/2000/svg" width="7" height="5">
              <rect width="7" height="5" fill="#0000ff" />
            </svg>
            """);

        try
        {
            RecordingImageLoader loader = new();
            UIRoot root = new();
            root.SetImageLoader(loader);
            SvgImage image = new() { SourcePath = firstPath };
            root.VisualChildren.Add(image);
            RecordingImage firstImage = Assert.IsType<RecordingImage>(image.Source);

            image.SourcePath = secondPath;

            Assert.True(firstImage.IsDisposed);
            Assert.Equal(2, loader.StreamLoadCount);
            Assert.Equal(7, image.Source?.Width);
            Assert.Equal(5, image.Source?.Height);
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    [Fact]
    public void AttachingWithoutAnImageLoaderLeavesTheSourceEmpty()
    {
        UIRoot root = new();
        SvgImage image = new() { SourcePath = "unused.svg" };

        root.VisualChildren.Add(image);

        Assert.Null(image.Source);
    }

    [Fact]
    public void SvgInCollapsedSubtreeDoesNotRasterizeUntilTheSubtreeBecomesVisible()
    {
        string path = WriteSvg(
            """
            <svg xmlns="http://www.w3.org/2000/svg" width="5" height="3">
              <rect width="5" height="3" fill="#ffffff" />
            </svg>
            """);

        try
        {
            RecordingImageLoader loader = new();
            UIRoot root = new();
            root.SetImageLoader(loader);
            UIElement collapsedParent = new() { Visibility = Visibility.Collapsed };
            SvgImage image = new() { SourcePath = path };
            collapsedParent.VisualChildren.Add(image);

            root.VisualChildren.Add(collapsedParent);

            Assert.Equal(0, loader.StreamLoadCount);
            Assert.Null(image.Source);

            collapsedParent.Visibility = Visibility.Visible;

            Assert.Equal(1, loader.StreamLoadCount);
            Assert.NotNull(image.Source);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SvgRasterizerUsesACompiledSidecarWithoutParsingTheSourceAtRuntime()
    {
        string path = Path.Combine(Path.GetTempPath(), $"cerneala-svg-{Guid.NewGuid():N}.svg");
        string compiledPath = path + ".cerneala.png";
        string signaturePath = compiledPath + ".sha256";
        File.WriteAllText(path, "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"7\" height=\"5\" />");
        byte[] expected = CreatePng(width: 3, height: 2);
        File.WriteAllBytes(compiledPath, expected);
        File.WriteAllText(signaturePath, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));

        try
        {
            using SvgRasterizer.RasterLease raster = SvgRasterizer.Acquire(path);
            byte[] actual = raster.PngBytes;

            Assert.Equal(expected, actual);
        }
        finally
        {
            File.Delete(path);
            File.Delete(compiledPath);
            File.Delete(signaturePath);
        }
    }

    [Fact]
    public void SvgRasterizerRejectsACompiledSidecarForDifferentSourceContent()
    {
        string path = WriteSvg(
            """
            <svg xmlns="http://www.w3.org/2000/svg" width="7" height="5">
              <rect width="7" height="5" fill="#00ff00" />
            </svg>
            """);
        string compiledPath = path + ".cerneala.png";
        string signaturePath = compiledPath + ".sha256";
        File.WriteAllBytes(compiledPath, CreatePng(width: 3, height: 2));
        File.WriteAllText(signaturePath, new string('0', 64));

        try
        {
            using SvgRasterizer.RasterLease raster = SvgRasterizer.Acquire(path);
            byte[] actual = raster.PngBytes;
            using SKBitmap bitmap = SKBitmap.Decode(actual);

            Assert.Equal(7, bitmap.Width);
            Assert.Equal(5, bitmap.Height);
        }
        finally
        {
            File.Delete(path);
            File.Delete(compiledPath);
            File.Delete(signaturePath);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompletedSvgDecodesDoNotRetain32UnownedRasterBuffers(bool compiled)
    {
        List<string> paths = [];
        try
        {
            WeakReference<byte[]>[] rasters = CreateUnownedRasters(paths, compiled);
            for (int cycle = 0; cycle < 3; cycle++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            Assert.Equal(32, rasters.Length);
            Assert.Equal(0, rasters.Count(raster => raster.TryGetTarget(out _)));
        }
        finally
        {
            foreach (string path in paths)
            {
                File.Delete(path);
                File.Delete(path + ".cerneala.png");
                File.Delete(path + ".cerneala.png.sha256");
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<byte[]>[] CreateUnownedRasters(List<string> paths, bool compiled)
    {
        WeakReference<byte[]>[] rasters = new WeakReference<byte[]>[32];
        for (int index = 0; index < rasters.Length; index++)
        {
            string path = WriteSvg("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"16\"><rect width=\"16\" height=\"16\" fill=\"red\" /></svg>");
            paths.Add(path);
            if (compiled)
            {
                File.WriteAllBytes(path + ".cerneala.png", CreatePng(16, 16));
                File.WriteAllText(path + ".cerneala.png.sha256", SvgRasterizer.ComputeSourceSignature(path));
            }
            using SvgRasterizer.RasterLease raster = SvgRasterizer.Acquire(path);
            rasters[index] = new(raster.PngBytes);
        }
        return rasters;
    }

    [Fact]
    public void ActiveRasterAcquisitionsShareAcross64ConcurrentDecodesAndRetireTheLastOwner()
    {
        string path = WriteSvg("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"16\" />");
        try
        {
            using SvgRasterizer.RasterLease keeper = SvgRasterizer.Acquire(path);
            byte[] original = keeper.PngBytes;
            Parallel.For(0, 64, _ =>
            {
                using SvgRasterizer.RasterLease acquired = SvgRasterizer.Acquire(path);
                Assert.Same(original, acquired.PngBytes);
                acquired.Dispose();
                Assert.Throws<ObjectDisposedException>(() => acquired.PngBytes);
            });
            Assert.Same(original, keeper.PngBytes);
            keeper.Dispose();
            byte[] next = AcquireAndRelease(path);
            Assert.NotSame(original, next);
            Assert.Equal(original, next);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ReleasingAnOldArtifactDoesNotEvictItsCurrentReplacement()
    {
        string path = WriteSvg("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"16\" />");
        string compiled = path + ".cerneala.png";
        try
        {
            File.WriteAllBytes(compiled, CreatePng(3, 2));
            File.WriteAllText(compiled + ".sha256", SvgRasterizer.ComputeSourceSignature(path));
            File.SetLastWriteTimeUtc(compiled, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            using SvgRasterizer.RasterLease old = SvgRasterizer.Acquire(path);
            byte[] oldBytes = old.PngBytes;
            byte[] replacement = CreatePng(7, 5);
            File.WriteAllBytes(compiled, replacement);
            File.SetLastWriteTimeUtc(compiled, new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc));
            using SvgRasterizer.RasterLease current = SvgRasterizer.Acquire(path);
            Assert.Equal(replacement, current.PngBytes);
            Assert.NotSame(oldBytes, current.PngBytes);

            old.Dispose();

            Assert.Same(current.PngBytes, AcquireAndRelease(path));
            using SKBitmap original = SKBitmap.Decode(oldBytes);
            Assert.Equal(3, original.Width);
            Assert.Equal(2, original.Height);
        }
        finally
        {
            File.Delete(path);
            File.Delete(compiled);
            File.Delete(compiled + ".sha256");
        }
    }

    [Fact]
    public void TwoSvgControlsKeepSharedRasterUntilBothDetach()
    {
        string path = WriteSvg("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"16\" />");
        UIRoot root = new();
        RecordingImageLoader loader = new();
        root.SetImageLoader(loader);
        SvgImage first = new() { SourcePath = path }, second = new() { SourcePath = path };
        try
        {
            root.VisualChildren.Add(first);
            root.VisualChildren.Add(second);
            RecordingImage firstImage = Assert.IsType<RecordingImage>(first.Source);
            RecordingImage secondImage = Assert.IsType<RecordingImage>(second.Source);
            byte[] original = AcquireAndRelease(path);

            root.VisualChildren.Remove(first);

            Assert.True(firstImage.IsDisposed);
            Assert.False(secondImage.IsDisposed);
            Assert.Same(original, AcquireAndRelease(path));
            root.VisualChildren.Remove(second);
            Assert.True(secondImage.IsDisposed);
            Assert.NotSame(original, AcquireAndRelease(path));
            Assert.Equal(2, loader.StreamLoadCount);
        }
        finally
        {
            root.VisualChildren.Remove(first);
            root.VisualChildren.Remove(second);
            File.Delete(path);
        }
    }

    [Fact]
    public void FailedBackendDecodeReleasesItsRasterAcquisition()
    {
        string path = WriteSvg("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"16\" />");
        UIRoot root = new();
        root.SetImageLoader(new ThrowingImageLoader());
        SvgImage image = new() { SourcePath = path };
        try
        {
            using SvgRasterizer.RasterLease probe = SvgRasterizer.Acquire(path);
            byte[] original = probe.PngBytes;
            Assert.Throws<InvalidOperationException>(() => root.VisualChildren.Add(image));
            Assert.Null(image.Source);
            probe.Dispose();
            Assert.NotSame(original, AcquireAndRelease(path));
        }
        finally
        {
            root.VisualChildren.Remove(image);
            File.Delete(path);
        }
    }

    [Fact]
    public void ClosedRasterHandleDoesNotRetainItsReleasedBuffer()
    {
        string path = WriteSvg("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"16\" />");
        try
        {
            (SvgRasterizer.RasterLease closed, WeakReference<byte[]> data) = CreateClosedRaster(path);
            for (int cycle = 0; cycle < 3; cycle++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            Assert.False(data.TryGetTarget(out _));
            closed.Dispose();
            Assert.Throws<ObjectDisposedException>(() => closed.PngBytes);
            GC.KeepAlive(closed);
        }
        finally { File.Delete(path); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (SvgRasterizer.RasterLease, WeakReference<byte[]>) CreateClosedRaster(string path)
    {
        SvgRasterizer.RasterLease lease = SvgRasterizer.Acquire(path);
        WeakReference<byte[]> data = new(lease.PngBytes);
        lease.Dispose();
        return (lease, data);
    }

    private static byte[] AcquireAndRelease(string path)
    {
        using SvgRasterizer.RasterLease lease = SvgRasterizer.Acquire(path);
        return lease.PngBytes;
    }

    private static string WriteSvg(string markup)
    {
        string path = Path.Combine(Path.GetTempPath(), $"cerneala-svg-{Guid.NewGuid():N}.svg");
        File.WriteAllText(path, markup);
        return path;
    }

    private static byte[] CreatePng(int width, int height)
    {
        using SKBitmap bitmap = new(width, height);
        bitmap.Erase(SKColors.Magenta);
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private sealed class RecordingImageLoader : IImageLoader
    {
        public int PathLoadCount { get; private set; }

        public int StreamLoadCount { get; private set; }

        public bool ContainsVisibleRedPixel { get; private set; }

        public IDrawImage Load(string path)
        {
            PathLoadCount++;
            throw new InvalidOperationException("SvgImage must load rasterized data from memory.");
        }

        public IDrawImage Load(Stream stream)
        {
            StreamLoadCount++;
            using SKBitmap bitmap = SKBitmap.Decode(stream)
                ?? throw new InvalidOperationException("The rasterized SVG was not a valid bitmap.");
            ContainsVisibleRedPixel = bitmap.Pixels.Any(
                color => color.Alpha > 0 && color.Red > color.Green && color.Red > color.Blue);
            return new RecordingImage(bitmap.Width, bitmap.Height);
        }
    }

    private sealed class RecordingImage(int width, int height) : IDrawImage, IDisposable
    {
        public int Width { get; } = width;

        public int Height { get; } = height;

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class ThrowingImageLoader : IImageLoader
    {
        public IDrawImage Load(string path) => throw new InvalidOperationException("Unexpected path decode.");
        public IDrawImage Load(Stream stream) => throw new InvalidOperationException("Intentional backend decode failure.");
    }
}
