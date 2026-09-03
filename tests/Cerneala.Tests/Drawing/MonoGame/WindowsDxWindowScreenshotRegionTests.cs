using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Hosting.Windows;
using SkiaSharp;

namespace Cerneala.Tests.Drawing.MonoGame;

[Collection(Cerneala.Tests.UI.Hosting.WindowRuntimeTestCollection.Name)]
public sealed class WindowsDxWindowScreenshotRegionTests
{
    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void RegionalPngCropsTheFullyRenderedFramebuffer(float scale)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int dipWidth = 8;
        const int dipHeight = 6;
        int pixelWidth = (int)Math.Ceiling(dipWidth * scale);
        int pixelHeight = (int)Math.Ceiling(dipHeight * scale);
        using Fixture fixture = new(pixelWidth, pixelHeight, scale);
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.FillRectangle(new DrawRect(1, 1, 5, 4), Color.Red);
        drawing.FillRectangle(new DrawRect(3, 2, 2, 2), Color.Blue);
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        DrawingFrameContext frame = new(analysis);
        Assert.True(WindowScreenshotRegion.TryCreate(
            new Cerneala.UI.Layout.LayoutRect(1, 1, 5, 4),
            new UiViewport(dipWidth, dipHeight, scale),
            out WindowScreenshotRegion region));

        using MemoryStream fullOutput = new();
        ((IWindowScreenshotSource)fixture.Session).RenderPng(
            fullOutput,
            Color.Black,
            backend => backend.Render(commands, in frame));
        using MemoryStream cropOutput = new();
        ((IWindowScreenshotSource)fixture.Session).RenderPng(
            cropOutput,
            Color.Black,
            region,
            backend => backend.Render(commands, in frame));

        using SKBitmap full = SKBitmap.Decode(fullOutput.ToArray());
        using SKBitmap crop = SKBitmap.Decode(cropOutput.ToArray());
        Assert.Equal((pixelWidth, pixelHeight), (full.Width, full.Height));
        Assert.Equal((region.Width, region.Height), (crop.Width, crop.Height));
        Assert.Equal(SKColors.Red, crop.GetPixel(
            (int)Math.Ceiling(scale) - region.X,
            (int)Math.Ceiling(scale) - region.Y));
        Assert.Equal(SKColors.Blue, crop.GetPixel(
            (int)Math.Ceiling(3 * scale) - region.X,
            (int)Math.Ceiling(2 * scale) - region.Y));
        Assert.Equal(SKColors.Black, full.GetPixel(0, 0));
    }

    private sealed class Fixture : IDisposable
    {
        private readonly Win32WindowPlatform platform =
            new(new WindowsDxWindowGraphicsSessionFactory(useMultisampling: false));
        private readonly IPlatformWindow window;

        public Fixture(int pixelWidth, int pixelHeight, float scale)
        {
            window = platform.CreateWindow(
                new Window { Width = 8, Height = 6 },
                new CallbackSink());
            Session = Assert.IsType<WindowsDxWindowGraphicsSession>(window.GraphicsSession);
            Session.Resize(pixelWidth, pixelHeight, scale);
        }

        public WindowsDxWindowGraphicsSession Session { get; }

        public void Dispose()
        {
            window.Dispose();
            platform.Dispose();
        }
    }

    private sealed class CallbackSink : IWindowPlatformCallbacks
    {
        public void RequestClose() { }
        public void ActivationChanged(bool active) { }
        public void BoundsChanged(UiViewport viewport, float left, float top, WindowState state) { }
        public void RenderRequested() { }
    }
}
