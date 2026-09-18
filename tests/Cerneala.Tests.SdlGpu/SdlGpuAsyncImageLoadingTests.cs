using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.UI.Resources;
using SkiaSharp;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuAsyncImageLoadingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SvgDecodesReleaseIntermediateRastersWhileDecodedLeasesPreservePixelsAcross32Cycles(bool compiled)
    {
        string path = CreateSvg();
        string prepared = path + SvgRasterizer.CompiledSidecarSuffix;
        try
        {
            if (compiled)
            {
                File.WriteAllBytes(prepared, SvgRasterizer.Compile(path));
                File.WriteAllText(prepared + ".sha256", SvgRasterizer.ComputeSourceSignature(path));
            }
            await using ImageResourceCache cache = new(new SdlGpuImageLoader(), maximumConcurrentLoads: 2);
            ImageResource resource = new(path);
            byte[] expectedPixels = [255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255];
            for (int cycle = 0; cycle < 32; cycle++)
            {
                using SvgRasterizer.RasterLease probe = SvgRasterizer.Acquire(path);
                byte[] oldRaster = probe.PngBytes;
                ImageResourceLease[] leases = await Task.WhenAll(Enumerable.Range(0, 16)
                    .Select(_ => cache.AcquireAsync(resource).AsTask())).WaitAsync(TimeSpan.FromSeconds(10));
                try
                {
                    SdlGpuImage decoded = Assert.IsType<SdlGpuImage>(leases[0].Image);
                    Assert.All(leases, lease => Assert.Same(decoded, lease.Image));
                    probe.Dispose();
                    using SvgRasterizer.RasterLease replacement = SvgRasterizer.Acquire(path);
                    Assert.NotSame(oldRaster, replacement.PngBytes);
                    Assert.Equal(oldRaster, replacement.PngBytes);
                    Assert.Equal(expectedPixels, decoded.RgbaPixels.ToArray());
                    Assert.Equal(1, cache.ResidentCount);
                }
                finally { foreach (ImageResourceLease lease in leases) { lease.Dispose(); } }
                Assert.Equal(0, cache.ResidentCount);
            }
            Assert.Equal(32, cache.LoadCount);
        }
        finally
        {
            File.Delete(path);
            File.Delete(prepared);
            File.Delete(prepared + ".sha256");
        }
    }

    [Fact]
    public void FailedSvgPngDecodeDoesNotPinItsIntermediateRaster()
    {
        string path = CreateSvg();
        string prepared = path + SvgRasterizer.CompiledSidecarSuffix;
        try
        {
            File.WriteAllBytes(prepared, [1, 2, 3]);
            File.WriteAllText(prepared + ".sha256", SvgRasterizer.ComputeSourceSignature(path));
            using SvgRasterizer.RasterLease probe = SvgRasterizer.Acquire(path);
            byte[] oldRaster = probe.PngBytes;
            Assert.Throws<InvalidDataException>(() => new SdlGpuImageLoader().Load(path));
            probe.Dispose();
            using SvgRasterizer.RasterLease replacement = SvgRasterizer.Acquire(path);
            Assert.NotSame(oldRaster, replacement.PngBytes);
            Assert.Equal(oldRaster, replacement.PngBytes);
        }
        finally
        {
            File.Delete(path);
            File.Delete(prepared);
            File.Delete(prepared + ".sha256");
        }
    }

    [Fact]
    public async Task FileBasedAsyncDecodeMatchesSynchronousPixelsAndReleasesOnLastLease()
    {
        string path = CreateImage();
        try
        {
            SdlGpuImageLoader loader = new();
            using SdlGpuImage reference = Assert.IsType<SdlGpuImage>(loader.Load(path));
            await using ImageResourceCache cache = new(loader, maximumConcurrentLoads: 2);
            ImageResource resource = new(path);
            ImageResourceLease[] leases = await Task.WhenAll(Enumerable.Range(0, 16)
                .Select(_ => cache.AcquireAsync(resource).AsTask())).WaitAsync(TimeSpan.FromSeconds(10));
            SdlGpuImage decoded = Assert.IsType<SdlGpuImage>(leases[0].Image);
            Assert.Equal(2, decoded.Width);
            Assert.Equal(2, decoded.Height);
            Assert.Equal(reference.RgbaPixels.ToArray(), decoded.RgbaPixels.ToArray());
            Assert.All(leases, lease => Assert.Same(decoded, lease.Image));
            Assert.Equal(1, cache.LoadCount);
            foreach (ImageResourceLease lease in leases) { lease.Dispose(); }
            Assert.Equal(0, cache.ResidentCount);
            Assert.Throws<ObjectDisposedException>(() => decoded.RgbaPixels);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task IndependentConcurrentDecodesPreservePixels()
    {
        string path = CreateImage();
        try
        {
            SdlGpuImageLoader loader = new();
            using SdlGpuImage reference = Assert.IsType<SdlGpuImage>(loader.Load(path));
            byte[] pixels = reference.RgbaPixels.ToArray();
            await Task.WhenAll(Enumerable.Range(0, 16).Select(async _ =>
            {
                using SdlGpuImage image = Assert.IsType<SdlGpuImage>(await loader.LoadAsync(path));
                Assert.Equal(pixels, image.RgbaPixels.ToArray());
            })).WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task PreCancelledLoadDoesNotOpenAMissingFile()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        SdlGpuImageLoader loader = new();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => loader.LoadAsync(
            Path.Combine(Path.GetTempPath(), $"Cerneala-missing-{Guid.NewGuid():N}.png"),
            cancellation.Token).AsTask());
    }

    private static string CreateImage()
    {
        string path = Path.Combine(Path.GetTempPath(), $"Cerneala-async-image-{Guid.NewGuid():N}.png");
        using SKBitmap bitmap = new(2, 2);
        bitmap.SetPixel(0, 0, SKColors.Red);
        bitmap.SetPixel(1, 0, SKColors.Green);
        bitmap.SetPixel(0, 1, SKColors.Blue);
        bitmap.SetPixel(1, 1, new SKColor(80, 120, 160, 128));
        using SKData png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, png.ToArray());
        return path;
    }

    private static string CreateSvg()
    {
        string path = Path.Combine(Path.GetTempPath(), $"Cerneala-async-svg-{Guid.NewGuid():N}.svg");
        File.WriteAllText(path, """
            <svg xmlns="http://www.w3.org/2000/svg" width="2" height="2" viewBox="0 0 2 2">
              <rect width="2" height="2" fill="#ff0000" />
            </svg>
            """);
        return path;
    }
}
