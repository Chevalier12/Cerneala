using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;
using Cerneala.UI.Resources.MonoGame;

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

        IDrawImage first = cache.Resolve(resource);
        IDrawImage second = cache.Resolve(resource);

        Assert.Same(loaded, first);
        Assert.Same(first, second);
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

        IDrawImage first = cache.Resolve(new ImageResource("logo.png"));
        IDrawImage second = cache.Resolve(new ImageResource("avatar.png"));

        Assert.Same(logo, first);
        Assert.Same(avatar, second);
        Assert.NotSame(first, second);
        Assert.Equal(1, loader.GetLoadCount("logo.png"));
        Assert.Equal(1, loader.GetLoadCount("avatar.png"));
    }

    [Fact]
    public void CacheClearDisposesOwnedDisposableImages()
    {
        RecordingImageLoader loader = new();
        DisposableTestImage loaded = new(16, 8);
        loader.SetImage("logo.png", loaded);
        ImageResourceCache cache = new(loader);
        cache.Resolve(new ImageResource("logo.png"));

        cache.Clear();

        Assert.True(loaded.IsDisposed);
        Assert.Equal(1, loaded.DisposeCount);
    }

    [Fact]
    public void ExternallySuppliedImageIsNotDisposedByCache()
    {
        DisposableTestImage supplied = new(16, 8);
        ImageResourceCache cache = new(new RecordingImageLoader());

        IDrawImage resolved = cache.Resolve(new ImageResource(supplied));
        cache.Clear();

        Assert.Same(supplied, resolved);
        Assert.False(supplied.IsDisposed);
        Assert.Equal(0, supplied.DisposeCount);
    }

    [Fact]
    public void MissingLoaderThrowsClearRuntimeError()
    {
        ImageResourceCache cache = new(null);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            (Action)(() => cache.Resolve(new ImageResource("logo.png"))));

        Assert.Contains("image loader", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path-backed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MonoGameLoaderFallsBackToApplicationBaseForPackagedRelativePaths()
    {
        string relativePath = Path.Combine(
            "packaged-assets",
            Guid.NewGuid().ToString("N") + ".png");
        string workingDirectoryPath = Path.GetFullPath(relativePath);
        Assert.False(File.Exists(workingDirectoryPath));

        string resolved = MonoGameImageLoader.ResolvePath(relativePath);

        Assert.Equal(
            Path.GetFullPath(relativePath, AppContext.BaseDirectory),
            resolved);
    }

    private sealed class RecordingImageLoader : IImageLoader
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
