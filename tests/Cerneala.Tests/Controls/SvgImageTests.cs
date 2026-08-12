using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
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

    private static string WriteSvg(string markup)
    {
        string path = Path.Combine(Path.GetTempPath(), $"cerneala-svg-{Guid.NewGuid():N}.svg");
        File.WriteAllText(path, markup);
        return path;
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
}
