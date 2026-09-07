using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.Tests.Drawing.SdlGpu;
using SkiaSharp;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class SdlGpuScreenshotRegionTests
{
    [SdlNativeTheory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void RegionalPngCropsTheFullyRenderedFramebuffer(float scale)
    {
        const int dipWidth = 8;
        const int dipHeight = 6;
        int pixelWidth = (int)Math.Ceiling(dipWidth * scale);
        int pixelHeight = (int)Math.Ceiling(dipHeight * scale);
        using SdlDrawingFixture fixture = new(pixelWidth, pixelHeight, coordinateScale: scale);
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

}
