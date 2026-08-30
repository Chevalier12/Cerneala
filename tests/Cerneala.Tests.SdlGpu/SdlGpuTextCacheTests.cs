using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Text;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Hosting;
using SkiaSharp;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuTextCacheTests
{
    [Fact]
    public void EquivalentSkiaFontWrappersReuseTheRasterizedTextEntry()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("text-cache", 160, 80, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(
                new SdlWindowSurface(window, api.GetWindowId(window)),
                pixelWidth: 160,
                pixelHeight: 80,
                coordinateScale: 1));
        SKTypeface typeface = SKTypeface.Default;
        DrawCommandList first = CreateTextCommands(new SkiaFont(typeface, "Default", 16));
        DrawCommandList second = CreateTextCommands(new SkiaFont(typeface, "Default", 16));

        Render(session, first);
        Render(session, second);

        IDrawingBackendFrameTimingSource timingSource =
            Assert.IsAssignableFrom<IDrawingBackendFrameTimingSource>(session.DrawingBackend);
        Assert.Equal(0, timingSource.LastFrameTiming.TextRequestCount);
        Assert.Equal(0, timingSource.LastFrameTiming.RasterizedPixelCount);
        Assert.Equal(TimeSpan.Zero, timingSource.LastFrameTiming.TextAtlasUpload);
    }

    private static DrawCommandList CreateTextCommands(IDrawFont font)
    {
        DrawCommandList commands = new();
        new DrawingContext(commands).DrawText(
            new DrawTextRun(font, "stable text", 16),
            new DrawPoint(4, 28),
            Color.White);
        return commands;
    }

    private static void Render(
        SdlGpuWindowGraphicsSession session,
        DrawCommandList commands)
    {
        DrawingFrameContext frame = new(new PrismFrameAnalyzer().Analyze(commands));
        session.BeginFrame(Color.Transparent);
        session.DrawingBackend.Render(commands, in frame);
        session.CompleteFrame(present: false);
    }
}
