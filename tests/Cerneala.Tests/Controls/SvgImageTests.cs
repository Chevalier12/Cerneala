using Cerneala.Drawing;
using System.Security.Cryptography;
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
            byte[] actual = SvgRasterizer.Rasterize(path);

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
            byte[] actual = SvgRasterizer.Rasterize(path);
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
}
