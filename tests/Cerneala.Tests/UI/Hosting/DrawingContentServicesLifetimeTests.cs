using Cerneala.Drawing;
using Cerneala.UI.Hosting;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Hosting;

public sealed class DrawingContentServicesLifetimeTests
{
    [Fact]
    public void ContentServicesDisposeTheirOwnedImageCacheOnce()
    {
        RecordingImageLoader loader = new();
        DisposableTestImage image = new(16, 8);
        loader.SetImage("logo.png", image);
        DrawingContentServices services = new(imageLoader: loader);
        Assert.Same(loader, services.ImageLoader);
        services.ImageResourceCache.Resolve(new ImageResource("logo.png"));

        services.Dispose();
        services.Dispose();

        Assert.Equal(1, image.DisposeCount);
    }

    [Fact]
    public void ContentServicesCanBeCreatedAndDisposedSequentially()
    {
        DisposableTestImage first = new(16, 8);
        DisposableTestImage second = new(32, 16);
        DisposeServices(first);
        DisposeServices(second);
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(1, second.DisposeCount);

        static void DisposeServices(DisposableTestImage image)
        {
            RecordingImageLoader loader = new();
            loader.SetImage("logo.png", image);
            using DrawingContentServices services = new(imageLoader: loader);
            services.ImageResourceCache.Resolve(new ImageResource("logo.png"));
        }
    }

    private sealed class RecordingImageLoader : IImageLoader
    {
        private readonly Dictionary<string, IDrawImage> images = new(StringComparer.Ordinal);

        public void SetImage(string path, IDrawImage image)
        {
            images[path] = image;
        }

        public IDrawImage Load(string path)
        {
            return images.TryGetValue(path, out IDrawImage? image)
                ? image
                : throw new InvalidOperationException($"No fake image registered for '{path}'.");
        }
    }

    private sealed class DisposableTestImage(int width, int height) : IDrawImage, IDisposable
    {
        public int Width { get; } = width;

        public int Height { get; } = height;

        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
        }
    }
}
