using Cerneala.Tests.Drawing.SdlGpu;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.Platforms.Sdl3;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Media;
using Cerneala.UI.Prism.Definitions;
using SkiaSharp;

namespace Cerneala.Tests.SdlGpu;

// Shared pixel and lifetime assertions migrated from the retired native smoke.
[Collection(SdlNativeTestCollection.Name)]
public sealed class NativeDrawingSmokeTests
{
    [SdlNativeFact]
    public void ThreeWindowsRenderIndependentlyAcrossResizeAndDisposal()
    {
        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory graphics = new(api);
        using SdlWindowPlatform platform = new(api, graphics);
        CallbackSink callbacks = new();
        using IPlatformWindow first = platform.CreateWindow(new Window { Width = 240, Height = 160 }, callbacks);
        using IPlatformWindow second = platform.CreateWindow(new Window { Width = 260, Height = 180 }, callbacks);
        using IPlatformWindow third = platform.CreateWindow(new Window { Width = 220, Height = 150 }, callbacks);
        first.Show();
        second.Show();
        third.Show();
        platform.PumpEvents();
        SdlGpuWindowGraphicsSession a = Assert.IsType<SdlGpuWindowGraphicsSession>(first.GraphicsSession);
        SdlGpuWindowGraphicsSession b = Assert.IsType<SdlGpuWindowGraphicsSession>(second.GraphicsSession);
        SdlGpuWindowGraphicsSession c = Assert.IsType<SdlGpuWindowGraphicsSession>(third.GraphicsSession);
        Assert.NotEqual(Assert.IsType<SdlWindowSurface>(first.Surface).WindowId,
            Assert.IsType<SdlWindowSurface>(second.Surface).WindowId);
        Assert.NotEqual(Assert.IsType<SdlWindowSurface>(second.Surface).WindowId,
            Assert.IsType<SdlWindowSurface>(third.Surface).WindowId);
        Check(a, new Color(210, 35, 45));
        Check(b, new Color(30, 150, 80));
        Check(c, new Color(55, 95, 175));
        using SdlGpuImage image = SdlDrawingFixture.SolidImage(new Color(200, 100, 50));
        DrawCommandList imageCommands = PrismTestData.Commands(
            DrawCommand.DrawImage(image, new DrawRect(0, 0, 8, 8), Color.White));
        Assert.Equal(new Color(200, 100, 50), SdlDrawingFixture.Render(a, imageCommands)[0]);
        Assert.Equal(new Color(200, 100, 50), SdlDrawingFixture.Render(b, imageCommands)[0]);
        a.Resize(192, 128, 1.25f);
        b.Resize(224, 144, 1.5f);
        Check(a, new Color(35, 90, 210));
        Check(b, new Color(220, 150, 30));
        first.Dispose();
        Check(b, new Color(120, 45, 190));
        second.Dispose();
        Check(c, new Color(20, 170, 180));

        static void Check(SdlGpuWindowGraphicsSession session, Color expected)
        {
            DrawCommandList commands = PrismTestData.Commands(DrawCommand.FillRectangle(
                new DrawRect(0, 0, session.PixelWidth, session.PixelHeight), expected));
            Color[] pixels = SdlDrawingFixture.Render(session, commands);
            Color actual = pixels[(session.PixelHeight / 2) * session.PixelWidth + session.PixelWidth / 2];
            Assert.InRange(Math.Abs(actual.R - expected.R), 0, 2);
            Assert.InRange(Math.Abs(actual.G - expected.G), 0, 2);
            Assert.InRange(Math.Abs(actual.B - expected.B), 0, 2);
        }
    }

    [Fact]
    public void ImageLoaderPremultipliesAlpha()
    {
        using SKBitmap bitmap = new(new SKImageInfo(1, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        bitmap.SetPixel(0, 0, new SKColor(200, 100, 50, 128));
        using SKImage encodedImage = SKImage.FromBitmap(bitmap);
        using SKData encoded = encodedImage.Encode(SKEncodedImageFormat.Png, 100);
        using MemoryStream stream = new(encoded.ToArray(), writable: false);
        using SdlGpuImage image = Assert.IsType<SdlGpuImage>(new SdlGpuImageLoader().Load(stream));
        Assert.Equal(new byte[] { 100, 50, 25, 128 }, image.RgbaPixels.ToArray());
    }

    [SdlNativeTheory]
    [InlineData(1f, 0)]
    [InlineData(1f, 1)]
    [InlineData(1f, 2)]
    [InlineData(1f, 3)]
    [InlineData(1.5f, 0)]
    [InlineData(1.5f, 1)]
    [InlineData(1.5f, 2)]
    [InlineData(1.5f, 3)]
    public void TextAndAllSmokeBrushesProduceVisibleGlyphPixels(float scale, int brushKind)
    {
        using SdlDrawingFixture fixture = new(260, 180);
        fixture.Session.Resize(224, 144, scale);
        IDrawFont font = new SystemFontSource().LoadFont("Arial", 24);
        DrawCommandList plain = PrismTestData.Commands(DrawCommand.DrawText(
            new DrawTextRun(font, "Cerneala", 24), new DrawPoint(12, 12), Color.Black));
        Assert.Contains(fixture.Render(plain, Color.White), pixel => pixel.R < 240 || pixel.G < 240 || pixel.B < 240);

        using SdlGpuImage image = new(2, 1, [230, 30, 40, 255, 30, 70, 230, 255]);
        IDrawBrush[] brushes =
        [
            new SolidColorBrush(Color.Black),
            new LinearGradientBrush(new DrawPoint(0, 0), new DrawPoint(180, 0),
                [new GradientStop(0, new Color(230, 30, 40)), new GradientStop(1, new Color(30, 70, 230))]),
            new RadialGradientBrush(new DrawPoint(90, 14), 90, 24,
                [new GradientStop(0, new Color(230, 30, 40)), new GradientStop(1, new Color(30, 70, 230))]),
            new ImageBrush(image, DrawBrushStretch.Fill)
        ];
        IDrawBrush brush = brushes[brushKind];
        {
            DrawCommandList commands = PrismTestData.Commands(DrawCommand.DrawText(
                new DrawTextRun(font, "MMMMMMMM", 28), new DrawPoint(20, 64), brush));
            Color[] pixels = fixture.Render(commands, Color.White);
            if (brush is SolidColorBrush)
            {
                Assert.Contains(pixels, pixel => pixel.R < 80 && pixel.G < 80 && pixel.B < 80);
            }
            else
            {
                Assert.Contains(pixels, pixel => pixel.R > pixel.B + 20 && pixel.G < 220);
                Assert.Contains(pixels, pixel => pixel.B > pixel.R + 20 && pixel.G < 220);
            }
        }
    }

    [SdlNativeTheory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public void OffscreenDrawingBrushPreservesEarlierFramePixels(float scale)
    {
        using SdlDrawingFixture fixture = new(260, 180);
        fixture.Session.Resize(224, 144, scale);
        DrawingBrush brush = new(
            [DrawCommand.FillRectangle(new DrawRect(0, 0, 16, 16), Color.White)],
            new DrawRect(0, 0, 16, 16));
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.FillRectangle(new DrawRect(0, 0, 224, 144), new Color(24, 40, 72)),
            DrawCommand.FillRectangle(new DrawRect(112, 72, 16, 16), brush));
        Color corner = fixture.Render(commands)[0];
        Assert.InRange(Math.Abs(corner.R - 24), 0, 2);
        Assert.InRange(Math.Abs(corner.G - 40), 0, 2);
        Assert.InRange(Math.Abs(corner.B - 72), 0, 2);
    }

    [SdlNativeFact]
    public void PrismCopyAndPresentationPreservePremultipliedAlpha()
    {
        using SdlPrismKernelFixture fixture = new();
        SdlGpuWindowGraphicsSession session = fixture.Session;
        using SdlGpuPrismSurfaceLease captured = session.DrawingResources.PrismResources.RentSurface(
            session.WindowIdentity, 32, 32, SdlGpuTextureFormat.R8G8B8A8Unorm, false);
        session.BeginFrame(new Color(20, 40, 60));
        try
        {
            nint source = session.DrawingResources.GetOrCreateTexture(
                session, new object(), 1, 1, [96, 32, 16, 128]).Handle;
            fixture.Draw(captured.Target, source, source, source,
                SdlPrismKernelFixture.CreateUniforms(0, 32, 32));
            Assert.IsType<SdlGpuDrawingBackend>(session.DrawingBackend).DrawPrismTexture(
                captured.Target.SampleTexture, session.WindowRenderTarget,
                destination: new DrawRect(0, 0, 32, 32));
        }
        finally
        {
            session.CompleteFrame(present: false);
        }
        byte[] pixels = session.CapturePresentedFrame().Pixels;
        int offset = (16 * session.PixelWidth + 16) * 4;
        Color actual = new(pixels[offset], pixels[offset + 1], pixels[offset + 2], pixels[offset + 3]);
        Assert.InRange(Math.Abs(actual.R - 106), 0, 3);
        Assert.InRange(Math.Abs(actual.G - 52), 0, 3);
        Assert.InRange(Math.Abs(actual.B - 46), 0, 3);
    }

    private sealed class CallbackSink : IWindowPlatformCallbacks
    {
        public void RequestClose() { }
        public void ActivationChanged(bool active) { }
        public void BoundsChanged(UiViewport viewport, float left, float top, WindowState state) { }
        public void RenderRequested() { }
    }
}
