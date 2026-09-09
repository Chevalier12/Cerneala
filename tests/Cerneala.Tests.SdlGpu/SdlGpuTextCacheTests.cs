using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Text;
using Cerneala.Platforms.Sdl3;
using Cerneala.Tests.Drawing.SdlGpu;
using Cerneala.UI.Hosting;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using SkiaSharp;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class SdlGpuTextCacheTests
{
    [SdlNativeTheory]
    [InlineData(false, 1f)]
    [InlineData(false, 1.25f)]
    [InlineData(false, 1.5f)]
    [InlineData(false, 2f)]
    [InlineData(true, 1f)]
    [InlineData(true, 1.25f)]
    [InlineData(true, 1.5f)]
    [InlineData(true, 2f)]
    public void CachedBrushTextPreservesPixelsAndBaselineAcrossTranslationAndRetirement(bool imageBrush, float scale)
    {
        using SdlDrawingFixture fixture = new(320, 120, coordinateScale: scale);
        using SdlGpuImage image = SdlDrawingFixture.SolidImage(new Color(100, 50, 25, 128));
        IDrawBrush brush = imageBrush
            ? new ImageBrush(image, opacity: 0.5f)
            : new LinearGradientBrush(new DrawPoint(0, 0), new DrawPoint(80, 0),
                [new GradientStop(0, Color.Red), new GradientStop(1, Color.Blue)], 0.5f);
        IDrawFont font = new SystemFontSource().LoadFont("Arial", 24);
        DrawCommandList first = Commands(new DrawPoint(12.125f / scale, 48.375f / scale));
        Color[] cold = fixture.Render(first, Color.Transparent);
        Assert.Contains(cold, pixel => pixel.A > 32);
        Assert.Equal(1, fixture.Backend.LastFrameTiming.TextRequestCount);
        Assert.Equal(cold, fixture.Render(first, Color.Transparent));
        Assert.Equal(0, fixture.Backend.LastFrameTiming.TextRequestCount);

        // Whole physical-pixel translation preserves the canonical phase, but
        // must use the current baseline with the cached tight-raster origin.
        DrawCommandList moved = Commands(new DrawPoint(20.125f / scale, 56.375f / scale));
        Color[] translated = fixture.Render(moved, Color.Transparent);
        Assert.Equal(0, fixture.Backend.LastFrameTiming.TextRequestCount);
        Assert.False(cold.AsSpan().SequenceEqual(translated));
        fixture.Render(new DrawCommandList());
        fixture.Render(new DrawCommandList());
        Assert.Equal(translated, fixture.Render(moved, Color.Transparent));
        Assert.Equal(1, fixture.Backend.LastFrameTiming.TextRequestCount);

        DrawCommandList Commands(DrawPoint baseline)
        {
            DrawCommandList commands = new();
            commands.Add(DrawCommand.DrawText(new DrawTextRun(font, "jÁg text", 24), baseline, brush));
            return commands;
        }
    }

    [SdlNativeFact]
    public void CachedTextCoverageDoesNotFreezeVisualBrushContent()
    {
        using SdlDrawingFixture fixture = new(260, 100);
        Cerneala.UI.Controls.Shapes.Rectangle source = new() { Fill = new SolidColorBrush(Color.Red) };
        source.Arrange(new ArrangeContext(new LayoutRect(0, 0, 16, 16)));
        VisualBrush brush = new(source);
        IDrawFont font = new SystemFontSource().LoadFont("Arial", 24);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.DrawText(new DrawTextRun(font, "MMMM", 24), new DrawPoint(12, 40), brush));
        Color[] red = fixture.Render(commands, Color.Transparent);
        Assert.Contains(red, pixel => pixel.R > 128 && pixel.B == 0);
        Assert.Equal(red, fixture.Render(commands, Color.Transparent));
        Assert.Equal(0, fixture.Backend.LastFrameTiming.TextRequestCount);

        source.Fill = new SolidColorBrush(Color.Blue);
        Color[] blue = fixture.Render(commands, Color.Transparent);
        Assert.Equal(0, fixture.Backend.LastFrameTiming.TextRequestCount);
        Assert.Contains(blue, pixel => pixel.B > 128 && pixel.R == 0);
        Assert.DoesNotContain(blue, pixel => pixel.R > 0);
        Assert.Equal(blue, fixture.Render(commands, Color.Transparent));
        Assert.Equal(0, fixture.Backend.LastFrameTiming.TextRequestCount);
    }

    [SdlNativeTheory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void RetentionAndBudgetEvictionPreserveNativeTextPixels(float scale)
    {
        using SdlDrawingFixture fixture = new(1120, 760, coordinateScale: scale);
        IDrawFont font = new SystemFontSource().LoadFont("Arial", 96);
        DrawCommandList first = Commands("MM");
        DrawCommandList second = Commands("HH");
        Color[] expectedFirst = fixture.Render(first);
        Assert.Equal(3, fixture.Session.DrawingResources.TextAtlasEntryCount);
        Color[] expectedSecond = fixture.Render(second);
        Assert.Equal(6, fixture.Session.DrawingResources.TextAtlasEntryCount);
        Assert.Contains(expectedFirst, pixel => pixel.R > 128);
        Assert.Contains(expectedSecond, pixel => pixel.G > 128);

        Color[] retainedFirst = fixture.Render(first);
        Assert.Equal(0, fixture.Backend.LastFrameTiming.TextRequestCount);
        Assert.Equal(expectedFirst, retainedFirst);
        Assert.Equal(expectedSecond, fixture.Render(second));
        Assert.Equal(0, fixture.Backend.LastFrameTiming.TextRequestCount);

        for (int frame = 0; frame < 240; frame++)
        {
            Render(fixture.Session, Commands($"{frame:D2}"));
            Assert.InRange(fixture.Session.DrawingResources.TextAtlasPageCount, 1, 8);
        }
        Assert.Equal(8, fixture.Session.DrawingResources.TextAtlasPageCount);
        Assert.Equal(expectedFirst, fixture.Render(first));
        Assert.True(fixture.Backend.LastFrameTiming.TextRequestCount > 0);
        Assert.Equal(expectedSecond, fixture.Render(second));

        DrawCommandList Commands(string text)
        {
            DrawCommandList commands = new();
            commands.Add(DrawCommand.DrawText(
                new DrawTextRun(font, text, 96), new DrawPoint(4.125f, 330.375f), Color.White));
            return commands;
        }
    }

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
