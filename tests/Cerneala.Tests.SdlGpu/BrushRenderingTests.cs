using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.Drawing.SdlGpu;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class BrushRenderingTests
{
    [SdlNativeFact]
    public void LinearGradientSamplingInterpolatesColorAndOpacity()
    {
        using SdlDrawingFixture fixture = new();
        // Align the old diagnostic's exact logical sample with a pixel center.
        LinearGradientBrush brush = new(new DrawPoint(0.5f, 0), new DrawPoint(10.5f, 0),
            [new GradientStop(0, Color.Black), new GradientStop(1, new Color(200, 100, 50))], 0.5f);
        Color actual = fixture.Render(PrismTestData.Commands(DrawCommand.FillRectangle(
            new DrawRect(0, 0, 16, 16), brush, 0.5f)), Color.Transparent)[5];
        // The old straight RGBA (100,50,25,64) becomes premultiplied GPU bytes.
        Assert.InRange(Math.Abs(actual.R - 25), 0, 1);
        Assert.InRange(Math.Abs(actual.G - 13), 0, 1);
        Assert.InRange(Math.Abs(actual.B - 6), 0, 1);
        Assert.InRange(Math.Abs(actual.A - 64), 0, 1);
    }

    [SdlNativeFact]
    public void RadialGradientSamplingClampsOutsideRadius()
    {
        using SdlDrawingFixture fixture = new();
        RadialGradientBrush brush = Radial();
        Color[] pixels = fixture.Render(PrismTestData.Commands(DrawCommand.FillRectangle(
            new DrawRect(0, 0, 32, 32), brush)));
        Assert.Equal(Color.White, fixture.Sample(pixels, 5, 5));
        Assert.Equal(Color.Black, fixture.Sample(pixels, 20, 20));
    }

    [SdlNativeFact]
    public void RadialGradientSamplingUsesPaintBoundsLocalCoordinates()
    {
        using SdlDrawingFixture fixture = new(128, 224);
        Color[] pixels = fixture.Render(PrismTestData.Commands(DrawCommand.FillRectangle(
            new DrawRect(100, 200, 10, 10), Radial())));
        Assert.Equal(Color.White, fixture.Sample(pixels, 105, 205));
    }

    [SdlNativeFact]
    public void DegenerateLinearGradientUsesLastStop()
    {
        using SdlDrawingFixture fixture = new();
        LinearGradientBrush brush = new(new DrawPoint(5, 5), new DrawPoint(5, 5),
            [new GradientStop(0, Color.White), new GradientStop(1, Color.Black)]);
        Color[] pixels = fixture.Render(PrismTestData.Commands(DrawCommand.FillRectangle(
            new DrawRect(0, 0, 16, 16), brush)), Color.White);
        Assert.Equal(Color.Black, fixture.Sample(pixels, 5, 5));
    }

    [SdlNativeFact]
    public void UniformTilePreservesAspectRatioAndAlignment()
    {
        using SdlDrawingFixture fixture = new(100, 100);
        using SdlGpuImage image = new(200, 100, Enumerable.Repeat((byte)255, 200 * 100 * 4).ToArray());
        ImageBrush brush = new(image, DrawBrushStretch.Uniform,
            DrawBrushAlignmentX.Right, DrawBrushAlignmentY.Bottom);
        Color[] pixels = fixture.Render(PrismTestData.Commands(DrawCommand.FillRectangle(
            new DrawRect(0, 0, 100, 100), brush)));
        for (int y = 0; y < 100; y++)
        {
            for (int x = 0; x < 100; x++)
            {
                Assert.Equal(y < 50 ? Color.Black : Color.White, pixels[y * 100 + x]);
            }
        }
    }

    [SdlNativeFact]
    public void VisualBrushGraphRejectsSelfReference()
    {
        using SdlDrawingFixture fixture = new();
        Cerneala.UI.Controls.Shapes.Rectangle source = new();
        source.Arrange(new ArrangeContext(new LayoutRect(0, 0, 10, 10)));
        VisualBrush brush = new(source);
        source.Fill = brush;
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            fixture.Render(PrismTestData.Commands(DrawCommand.FillRectangle(new DrawRect(0, 0, 10, 10), brush))));
        Assert.Contains("cycle", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static RadialGradientBrush Radial() => new(new DrawPoint(5.5f, 5.5f), 5, 5,
        [new GradientStop(0, Color.White), new GradientStop(1, Color.Black)]);

    [SdlNativeFact]
    public void NonRepeatingImageViewportDoesNotExtendEdgePixelsOutsideItsTile()
    {
        using SdlDrawingFixture fixture = new();
        using SdlGpuImage image = SdlDrawingFixture.SolidImage(Color.White);
        ImageBrush brush = new(image, viewport: new DrawRect(8, 8, 8, 8));
        Color[] pixels = fixture.Render(PrismTestData.Commands(
            DrawCommand.FillRectangle(new DrawRect(0, 0, 32, 32), brush)));
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                Assert.Equal(x >= 8 && x < 16 && y >= 8 && y < 16 ? Color.White : Color.Black,
                    fixture.Sample(pixels, x, y));
            }
        }
    }

    [SdlNativeTheory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void NestedCaptureCompositingDoesNotReuseAnActiveParentLayer(bool multisampling, bool useSurface)
    {
        using SdlDrawingFixture fixture = new(useMultisampling: multisampling);
        DrawRect full = new(0, 0, 96, 64);
        DrawingBrush brush = new([
            DrawCommand.PushOpacity(0.5f),
            DrawCommand.FillRectangle(new DrawRect(48, 0, 48, 64), Color.White),
            DrawCommand.PopOpacity()], full);
        using RecordedSurface surface = new((commands, _) =>
        {
            foreach (DrawCommand command in brush.Commands) { commands.Add(command); }
        }, Color.Transparent);
        Color[] pixels = fixture.Render(PrismTestData.Commands(
            DrawCommand.PushOpacity(0.5f),
            DrawCommand.FillRectangle(full, new Color(255, 0, 0)),
            useSurface
                ? DrawCommand.RenderSurface2D(surface, full, Color.White)
                : DrawCommand.FillRectangle(full, brush),
            DrawCommand.PopOpacity()));
        AssertColor(new Color(128, 0, 0), fixture.Sample(pixels, 16, 16));
        AssertColor(new Color(128, 64, 64), fixture.Sample(pixels, 64, 16));
    }

    [SdlNativeFact]
    public void VisualCaptureReusesStableRasterAndRepaintsChangedSource()
    {
        using SdlDrawingFixture fixture = new();
        Cerneala.UI.Controls.Shapes.Rectangle source = new() { Fill = new SolidColorBrush(new Color(255, 0, 0)) };
        source.Arrange(new ArrangeContext(new LayoutRect(0, 0, 16, 16)));
        VisualBrush brush = new(source);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.FillRectangle(new DrawRect(16, 16, 32, 32), brush));
        Color[] first = fixture.Render(commands);
        AssertColor(new Color(255, 0, 0), fixture.Sample(first, 24, 24));
        Assert.Equal(first, fixture.Render(commands));
        Assert.Equal(1, fixture.Backend.LastFrameCounters.DrawCallCount);
        source.Fill = new SolidColorBrush(new Color(0, 0, 255));
        AssertColor(new Color(0, 0, 255), fixture.Sample(fixture.Render(commands), 24, 24));
        Assert.True(fixture.Backend.LastFrameCounters.DrawCallCount > 1);
    }

    [SdlNativeFact]
    public void ImageTextCoverageIsStableAcrossRasterReuse()
    {
        using SdlDrawingFixture fixture = new(260, 100);
        using SdlGpuImage image = SdlDrawingFixture.SolidImage(new Color(100, 50, 25, 128));
        ImageBrush brush = new(image, opacity: 0.5f);
        IDrawFont font = new Cerneala.Drawing.Text.SystemFontSource().LoadFont("Arial", 24);
        DrawCommandList commands = PrismTestData.Commands(DrawCommand.DrawText(
            new DrawTextRun(font, "MMMMMMMM", 24), new DrawPoint(12, 40), brush));
        Color[] first = fixture.Render(commands, Color.Transparent);
        Assert.Contains(first, pixel => pixel.A > 32);
        for (int frame = 0; frame < 8; frame++)
        {
            Assert.Equal(first, fixture.Render(commands, Color.Transparent));
            Assert.Equal(1, fixture.Backend.LastFrameCounters.DrawCallCount);
        }
    }

    private static void AssertColor(Color expected, Color actual)
    {
        Assert.InRange(Math.Abs(expected.R - actual.R), 0, 2);
        Assert.InRange(Math.Abs(expected.G - actual.G), 0, 2);
        Assert.InRange(Math.Abs(expected.B - actual.B), 0, 2);
        Assert.InRange(Math.Abs(expected.A - actual.A), 0, 2);
    }
}
