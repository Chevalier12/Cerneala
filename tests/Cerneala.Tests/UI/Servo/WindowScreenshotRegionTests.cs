using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Servo;

public sealed class WindowScreenshotRegionTests
{
    [Theory]
    [InlineData(1f, 1, 2, 4, 3)]
    [InlineData(1.5f, 1, 3, 6, 4)]
    [InlineData(2f, 2, 4, 7, 6)]
    public void FractionalDipBoundsUseFloorCeilAtEveryScale(
        float scale,
        int x,
        int y,
        int width,
        int height)
    {
        Assert.True(WindowScreenshotRegion.TryCreate(
            new LayoutRect(1.2f, 2.4f, 3.2f, 2.2f),
            new UiViewport(10, 8, scale),
            out WindowScreenshotRegion region));

        Assert.Equal(new WindowScreenshotRegion(x, y, width, height), region);
    }

    [Theory]
    [InlineData(-2.2f, 1f, 4f, 2f, 0, 1, 2, 2)]
    [InlineData(8.5f, 1f, 4f, 2f, 8, 1, 2, 2)]
    [InlineData(1f, -2.2f, 2f, 4f, 1, 0, 2, 2)]
    [InlineData(1f, 6.5f, 2f, 4f, 1, 6, 2, 2)]
    public void RegionClampsEachFramebufferEdge(
        float boundsX,
        float boundsY,
        float boundsWidth,
        float boundsHeight,
        int x,
        int y,
        int width,
        int height)
    {
        Assert.True(WindowScreenshotRegion.TryCreate(
            new LayoutRect(boundsX, boundsY, boundsWidth, boundsHeight),
            new UiViewport(10, 8),
            out WindowScreenshotRegion region));
        Assert.Equal(new WindowScreenshotRegion(x, y, width, height), region);
    }

    [Theory]
    [InlineData(-5f, 0f, 2f, 2f)]
    [InlineData(12f, 0f, 2f, 2f)]
    [InlineData(0f, -5f, 2f, 2f)]
    [InlineData(0f, 12f, 2f, 2f)]
    [InlineData(0f, 0f, 0f, 2f)]
    public void EmptyOrUnusableRegionIsRejected(
        float x,
        float y,
        float width,
        float height)
    {
        Assert.False(WindowScreenshotRegion.TryCreate(
            new LayoutRect(x, y, width, height),
            new UiViewport(10, 8),
            out _));
    }

    [Theory]
    [InlineData(898, 600, 1.75f)]
    [InlineData(896, 600, 1.75f)]
    [InlineData(1000, 600, 2f)]
    [InlineData(600, 898, 1.75f)]
    public void FullViewportRegionCoversExactlyThePhysicalPixelsTheViewportWasCreatedFrom(
        int pixelWidth,
        int pixelHeight,
        float scale)
    {
        UiViewport viewport = UiViewport.FromPhysicalPixels(pixelWidth, pixelHeight, scale);

        Assert.True(WindowScreenshotRegion.TryCreate(
            new LayoutRect(0, 0, viewport.Width, viewport.Height),
            viewport,
            out WindowScreenshotRegion region));

        Assert.Equal(new WindowScreenshotRegion(0, 0, pixelWidth, pixelHeight), region);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(1.75f)]
    [InlineData(2f)]
    public void EveryPhysicalSizeUpTo4000RoundTripsThroughTheFullViewportRegion(float scale)
    {
        List<int> mismatches = [];
        for (int pixels = 1; pixels <= 4000; pixels++)
        {
            UiViewport viewport = UiViewport.FromPhysicalPixels(pixels, pixels, scale);
            if (!WindowScreenshotRegion.TryCreate(
                    new LayoutRect(0, 0, viewport.Width, viewport.Height),
                    viewport,
                    out WindowScreenshotRegion region) ||
                region != new WindowScreenshotRegion(0, 0, pixels, pixels))
            {
                mismatches.Add(pixels);
            }
        }

        Assert.Empty(mismatches);
    }

    [Fact]
    public void RgbaCropPreservesExactFullFramePixelsIncludingOverlayPixels()
    {
        byte[] full = Enumerable.Range(0, 4 * 3 * 4).Select(value => (byte)value).ToArray();
        WindowPreviewFrame frame = new(full, 4, 3, 16);

        WindowPreviewFrame crop = WindowScreenshotPixels.CropRgba(
            frame,
            new WindowScreenshotRegion(1, 1, 2, 2));

        Assert.Equal((2, 2, 8), (crop.PixelWidth, crop.PixelHeight, crop.Stride));
        Assert.Equal(full[20..28].Concat(full[36..44]), crop.Pixels);
    }
}
