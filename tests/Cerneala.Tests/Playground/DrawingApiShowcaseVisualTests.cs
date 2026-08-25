using System.Buffers.Binary;
using Cerneala.Playground;
using Cerneala.Drawing;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Media;
using Cerneala.Tests.UI.Hosting;
using SkiaSharp;

namespace Cerneala.Tests.Playground;

[Collection(WindowRuntimeTestCollection.Name)]
public sealed class DrawingApiShowcaseVisualTests : IDisposable
{
    public DrawingApiShowcaseVisualTests()
    {
        Application.ResetForTesting();
        WindowApplicationRuntime.ResetForTesting();
    }

    public void Dispose()
    {
        WindowApplicationRuntime.ResetForTesting();
        Application.ResetForTesting();
    }

    [Theory]
    [InlineData(1f, 500, 464)]
    [InlineData(1.5f, 750, 696)]
    public void WindowScreenshotRendersTheShowcaseAtMultipleDpiScales(
        float renderScale,
        int expectedPixelWidth,
        int expectedPixelHeight)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string screenshotPath = Path.Combine(
            Path.GetTempPath(),
            $"cerneala-drawing-api-{renderScale:0.0}-{Guid.NewGuid():N}.png");

        try
        {
            using DesignPreviewSession session = DesignPreviewSession.Create(
                new Application(),
                static () => new DrawingApiShowcaseView(),
                width: 500,
                height: 464,
                renderScale);
            session.Pump(TimeSpan.FromMilliseconds(16));
            session.Pump(TimeSpan.FromMilliseconds(16));
            session.SaveScreenshot(screenshotPath);

            Assert.True(new FileInfo(screenshotPath).Length > 1_000);
            Assert.Equal(
                (expectedPixelWidth, expectedPixelHeight),
                ReadPngDimensions(screenshotPath));
        }
        finally
        {
            File.Delete(screenshotPath);
        }
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public void WindowScreenshotAntialiasesRenderSurfacePathEdges(float renderScale)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string screenshotPath = Path.Combine(
            Path.GetTempPath(),
            $"cerneala-path-edge-coverage-{renderScale:0.0}-{Guid.NewGuid():N}.png");

        try
        {
            using DesignPreviewSession session = DesignPreviewSession.Create(
                new Application(),
                static () => new PathEdgeCoverageProbe(),
                width: 160,
                height: 160,
                renderScale);
            session.Pump(TimeSpan.FromMilliseconds(16));
            session.Pump(TimeSpan.FromMilliseconds(16));
            session.SaveScreenshot(screenshotPath);

            using SKBitmap bitmap = SKBitmap.Decode(screenshotPath);
            int partialCoveragePixels = CountPartialCoveragePixels(
                bitmap,
                new SKColor(10, 14, 20),
                new SKColor(77, 240, 255));

            Assert.True(
                partialCoveragePixels >= 100,
                $"Expected at least 100 partially covered path-edge pixels, but found {partialCoveragePixels}.");
        }
        finally
        {
            File.Delete(screenshotPath);
        }
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public void WindowScreenshotAntialiasesFilledPrimitiveEdges(float renderScale)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string screenshotPath = Path.Combine(
            Path.GetTempPath(),
            $"cerneala-primitive-edge-coverage-{renderScale:0.0}-{Guid.NewGuid():N}.png");

        try
        {
            using DesignPreviewSession session = DesignPreviewSession.Create(
                new Application(),
                static () => new PrimitiveEdgeCoverageProbe(),
                width: 200,
                height: 100,
                renderScale);
            session.Pump(TimeSpan.FromMilliseconds(16));
            session.Pump(TimeSpan.FromMilliseconds(16));
            session.SaveScreenshot(screenshotPath);

            using SKBitmap bitmap = SKBitmap.Decode(screenshotPath);
            int ellipseCoverage = CountPartialCoveragePixels(
                bitmap,
                new SKColor(10, 14, 20),
                new SKColor(77, 240, 255),
                new SKRectI(0, 0, bitmap.Width / 2, bitmap.Height));
            int roundedRectangleCoverage = CountPartialCoveragePixels(
                bitmap,
                new SKColor(10, 14, 20),
                new SKColor(77, 240, 255),
                new SKRectI(bitmap.Width / 2, 0, bitmap.Width, bitmap.Height));

            Assert.True(
                ellipseCoverage >= 40,
                $"Expected at least 40 partially covered ellipse-edge pixels, but found {ellipseCoverage}.");
            Assert.True(
                roundedRectangleCoverage >= 20,
                $"Expected at least 20 partially covered rounded-rectangle pixels, but found {roundedRectangleCoverage}.");
        }
        finally
        {
            File.Delete(screenshotPath);
        }
    }

    private static (int Width, int Height) ReadPngDimensions(string path)
    {
        Span<byte> header = stackalloc byte[24];
        using FileStream stream = File.OpenRead(path);
        stream.ReadExactly(header);

        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, header[..8].ToArray());
        return (
            BinaryPrimitives.ReadInt32BigEndian(header[16..20]),
            BinaryPrimitives.ReadInt32BigEndian(header[20..24]));
    }

    private static int CountPartialCoveragePixels(
        SKBitmap bitmap,
        SKColor background,
        SKColor foreground) =>
        CountPartialCoveragePixels(
            bitmap,
            background,
            foreground,
            new SKRectI(0, 0, bitmap.Width, bitmap.Height));

    private static int CountPartialCoveragePixels(
        SKBitmap bitmap,
        SKColor background,
        SKColor foreground,
        SKRectI region)
    {
        int count = 0;
        for (int y = region.Top; y < region.Bottom; y++)
        {
            for (int x = region.Left; x < region.Right; x++)
            {
                SKColor pixel = bitmap.GetPixel(x, y);
                if (ColorDistance(pixel, background) >= 4 &&
                    ColorDistance(pixel, foreground) >= 4)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int ColorDistance(SKColor first, SKColor second) =>
        Math.Max(
            Math.Abs(first.Red - second.Red),
            Math.Max(
                Math.Abs(first.Green - second.Green),
                Math.Abs(first.Blue - second.Blue)));

    private sealed class PathEdgeCoverageProbe : RenderSurface2D
    {
        private static readonly SolidColorBrush SurfaceBrush = new(new Color(10, 14, 20));
        private static readonly SolidColorBrush ShapeBrush = new(new Color(77, 240, 255));
        private static readonly DrawPath ShapePath = DrawPathParser.ParseSvg(
            "M20 90 C20 35 70 20 80 60 C90 20 140 35 140 90 C140 125 105 145 80 155 C55 145 20 125 20 90 Z");

        public PathEdgeCoverageProbe()
        {
            ClearColor = new Color(10, 14, 20);
            RedrawMode = RenderSurface2DRedrawMode.OnDemand;
        }

        protected override void OnDraw(RenderSurface2DFrame frame)
        {
            frame.FillRectangle(frame.Bounds, SurfaceBrush);
            frame.FillPath(
                ShapePath,
                ShapePath.Bounds,
                new DrawRect(10, 5, 140, 150),
                ShapeBrush);
        }
    }

    private sealed class PrimitiveEdgeCoverageProbe : RenderSurface2D
    {
        private static readonly SolidColorBrush SurfaceBrush = new(new Color(10, 14, 20));
        private static readonly SolidColorBrush ShapeBrush = new(new Color(77, 240, 255));

        public PrimitiveEdgeCoverageProbe()
        {
            ClearColor = new Color(10, 14, 20);
            RedrawMode = RenderSurface2DRedrawMode.OnDemand;
        }

        protected override void OnDraw(RenderSurface2DFrame frame)
        {
            frame.FillRectangle(frame.Bounds, SurfaceBrush);
            frame.FillEllipse(new DrawRect(20, 20, 60, 60), ShapeBrush);
            frame.FillRoundedRectangle(
                new DrawRect(120, 20, 60, 60),
                new DrawCornerRadius(16),
                ShapeBrush);
        }
    }
}
