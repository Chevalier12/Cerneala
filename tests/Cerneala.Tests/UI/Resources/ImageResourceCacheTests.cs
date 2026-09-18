using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;
using Cerneala.Backends.SdlGpu;
using SkiaSharp;

namespace Cerneala.Tests.UI.Resources;

public sealed class ImageResourceCacheTests
{
    [Fact]
    public void PathBackedImageResourceLoadsOncePerIdentity()
    {
        RecordingImageLoader loader = new();
        TestImage loaded = new(16, 8);
        loader.SetImage("logo.png", loaded);
        ImageResourceCache cache = new(loader);
        ImageResource resource = new("logo.png");

        using ImageResourceLease first = cache.Acquire(resource);
        using ImageResourceLease second = cache.Acquire(resource);

        Assert.Same(loaded, first.Image);
        Assert.Same(first.Image, second.Image);
        Assert.Equal(1, loader.GetLoadCount("logo.png"));
    }

    [Fact]
    public void PathBackedImageResourceReturnsCachedImageOnMeasureAndRender()
    {
        RecordingImageLoader loader = new();
        TestImage loaded = new(16, 8);
        loader.SetImage("logo.png", loaded);
        ResourceId<ImageResource> id = new("Logo");
        ResourceStore store = new();
        store.SetResource(id, new ImageResource("logo.png"));
        UIRoot root = new(100, 100);
        root.SetResourceProvider(store);
        root.SetImageLoader(loader);
        Image image = new()
        {
            SourceResourceId = id
        };
        root.VisualChildren.Add(image);

        LayoutSize desired = image.Measure(new MeasureContext(new LayoutSize(100, 100)));
        image.Arrange(new ArrangeContext(new LayoutRect(0, 0, desired.Width, desired.Height)));
        root.ProcessFrame();
        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Equal(new LayoutSize(16, 8), desired);
        Assert.Single(commands);
        Assert.Same(loaded, commands[0].Image);
        Assert.Equal(1, loader.GetLoadCount("logo.png"));
    }

    [Fact]
    public void DifferentPathsLoadDifferentImages()
    {
        RecordingImageLoader loader = new();
        TestImage logo = new(16, 8);
        TestImage avatar = new(24, 12);
        loader.SetImage("logo.png", logo);
        loader.SetImage("avatar.png", avatar);
        ImageResourceCache cache = new(loader);

        using ImageResourceLease first = cache.Acquire(new ImageResource("logo.png"));
        using ImageResourceLease second = cache.Acquire(new ImageResource("avatar.png"));

        Assert.Same(logo, first.Image);
        Assert.Same(avatar, second.Image);
        Assert.NotSame(first.Image, second.Image);
        Assert.Equal(1, loader.GetLoadCount("logo.png"));
        Assert.Equal(1, loader.GetLoadCount("avatar.png"));
    }

    [Fact]
    public void CacheClearPreservesAcquisitionsUntilTheirLastRelease()
    {
        RecordingImageLoader loader = new();
        DisposableTestImage loaded = new(16, 8);
        loader.SetImage("logo.png", loaded);
        ImageResourceCache cache = new(loader);
        ImageResourceLease lease = cache.Acquire(new ImageResource("logo.png"));

        cache.Clear();

        Assert.False(loaded.IsDisposed);
        Assert.Same(loaded, lease.Image);
        lease.Dispose();
        Assert.True(loaded.IsDisposed);
        Assert.Equal(1, loaded.DisposeCount);
    }

    [Fact]
    public void ExternallySuppliedImageIsNotDisposedByCache()
    {
        DisposableTestImage supplied = new(16, 8);
        ImageResourceCache cache = new(new RecordingImageLoader());

        ImageResourceLease resolved = cache.Acquire(new ImageResource(supplied));
        cache.Clear();

        Assert.Same(supplied, resolved.Image);
        resolved.Dispose();
        Assert.False(supplied.IsDisposed);
        Assert.Equal(0, supplied.DisposeCount);
    }

    [Fact]
    public void MissingLoaderThrowsClearRuntimeError()
    {
        ImageResourceCache cache = new(null);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            (Action)(() => cache.Acquire(new ImageResource("logo.png"))));

        Assert.Contains("image loader", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path-backed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SdlLoaderFallsBackToApplicationBaseForPackagedRelativePaths()
    {
        string relativePath = Path.Combine(
            "packaged-assets",
            Guid.NewGuid().ToString("N") + ".png");
        string workingDirectoryPath = Path.GetFullPath(relativePath);
        Assert.False(File.Exists(workingDirectoryPath));

        string packagedPath = Path.GetFullPath(relativePath, AppContext.BaseDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(packagedPath)!);
        try
        {
            using SKBitmap bitmap = new(1, 1);
            bitmap.Erase(SKColors.Red);
            using SKData encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(packagedPath, encoded.ToArray());
            using SdlGpuImage loaded = Assert.IsType<SdlGpuImage>(new SdlGpuImageLoader().Load(relativePath));
            Assert.Equal(1, loaded.Width);
            Assert.Equal(1, loaded.Height);
            Assert.Equal(new byte[] { 255, 0, 0, 255 }, loaded.RgbaPixels.ToArray());
        }
        finally
        {
            File.Delete(packagedPath);
        }
    }

    private sealed class RecordingImageLoader : IAsyncImageLoader
    {
        private readonly Dictionary<string, IDrawImage> images = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> loadCounts = new(StringComparer.Ordinal);

        public void SetImage(string path, IDrawImage image)
        {
            images[path] = image;
        }

        public int GetLoadCount(string path)
        {
            return loadCounts.GetValueOrDefault(path);
        }

        public IDrawImage Load(string path)
        {
            loadCounts[path] = GetLoadCount(path) + 1;
            return images.TryGetValue(path, out IDrawImage? image)
                ? image
                : throw new InvalidOperationException($"No fake image registered for '{path}'.");
        }

        public ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new(Load(path));
        }
    }

    private class TestImage(int width, int height) : IDrawImage
    {
        public int Width { get; } = width;

        public int Height { get; } = height;
    }

    private sealed class DisposableTestImage(int width, int height) : TestImage(width, height), IDisposable
    {
        public bool IsDisposed { get; private set; }

        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
            DisposeCount++;
        }
    }
}
