using Cerneala.Drawing;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.Drawing.SdlGpu;
using Cerneala.UI.Media;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class DrawingBackendLifecycleContractTests
{
    [SdlNativeTheory]
    [InlineData(80f, 0f, true, false)]
    [InlineData(0f, 40f, false, true)]
    [InlineData(80f, 40f, true, true)]
    public void LinearGradientColorVariesOnlyAlongItsSpecifiedAxes(
        float endX, float endY, bool variesHorizontally, bool variesVertically)
    {
        using SdlDrawingFixture fixture = new();
        LinearGradientBrush brush = new(new DrawPoint(0, 0), new DrawPoint(endX, endY),
            [new GradientStop(0, Color.White), new GradientStop(1, Color.Black)]);
        Color[] pixels = fixture.Render(PrismTestData.Commands(DrawCommand.FillRectangle(new DrawRect(0, 0, 80, 40), brush)));
        Color reference = fixture.Sample(pixels, 10, 10);
        Assert.Equal(variesHorizontally, reference != fixture.Sample(pixels, 20, 10));
        Assert.Equal(variesVertically, reference != fixture.Sample(pixels, 10, 20));
    }

    [SdlNativeFact]
    public void CoordinateScaleAppliesToActualDrawBounds()
    {
        using SdlDrawingFixture fixture = new(coordinateScale: 2);
        Color[] pixels = fixture.Render(PrismTestData.Commands(
            DrawCommand.FillRectangle(new DrawRect(1, 2, 3, 4), Color.White)));
        for (int y = 0; y < fixture.Session.PixelHeight; y++)
        {
            for (int x = 0; x < fixture.Session.PixelWidth; x++)
            {
                Color expected = x >= 2 && x < 8 && y >= 4 && y < 12 ? Color.White : Color.Black;
                Assert.Equal(expected, pixels[y * fixture.Session.PixelWidth + x]);
            }
        }
    }

    [SdlNativeFact]
    public void FailedCompositingFrameDoesNotPoisonTheNextFrame()
    {
        using SdlDrawingFixture fixture = new();
        DrawCommandList failing = PrismTestData.Commands(
            DrawCommand.FillRectangle(new DrawRect(0, 0, 16, 16), new Color(20, 40, 80)),
            DrawCommand.PushLayer(new DrawLayerOptions(opacity: 0.5f)),
            DrawCommand.DrawImage(new UnsupportedImage(), new DrawRect(0, 0, 8, 8), Color.White),
            DrawCommand.PopLayer());
        Assert.Throws<InvalidOperationException>(() => fixture.Render(failing));

        Color expected = new(42, 96, 173);
        DrawCommandList recovery = PrismTestData.Commands(
            DrawCommand.FillRectangle(new DrawRect(0, 0, 96, 64), expected));
        for (int frame = 0; frame < 2; frame++)
        {
            Assert.All(fixture.Render(recovery), pixel => Assert.Equal(expected, pixel));
        }
    }

    private sealed class UnsupportedImage : IDrawImage
    {
        public int Width => 1;
        public int Height => 1;
    }
}
