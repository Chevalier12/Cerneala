using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Tests.SdlGpu;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismImageDisplayScaleTests
{
    private const int ImageSize = 260;
    private const float Origin = 40;
    private const int ProfileLength = 14;

    [SdlNativeTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void OuterGlowOnDrawingContextPrismImageFollowsTheDisplayScale(bool nineSlice)
    {
        GlowMeasurement atOne = Measure(1f, nineSlice);
        GlowMeasurement atScale = Measure(1.25f, nineSlice);

        // OuterGlowStyle.Size is in dip: 8 dip is 8 physical px at scale 1 and 10 at 1.25.
        Assert.Equal(8, atOne.Width);
        Assert.Equal(10, atScale.Width);
        Assert.Equal(atOne.TopNearLeft, atOne.TopNearRight);
        Assert.Equal(atOne.LeftNearTop, atOne.RightNearTop);
        Assert.Equal(atScale.TopNearLeft, atScale.TopNearRight);
        Assert.Equal(atScale.LeftNearTop, atScale.RightNearTop);
        Assert.Equal(atScale.TopNearLeft, atScale.LeftNearTop);
        Assert.True(
            atScale.RowAboveBox.Distinct().Count() == 1,
            $"row 5 px above the box at 1.25 has a brightness step: {string.Join(" ", atScale.RowAboveBox.Distinct())}");
    }

    private static GlowMeasurement Measure(float scale, bool nineSlice)
    {
        int size = (int)MathF.Ceiling(400 * scale);
        using SdlDrawingFixture fixture = new(size, size, coordinateScale: scale);
        byte[] rgba = new byte[ImageSize * ImageSize * 4];
        for (int index = 0; index < ImageSize * ImageSize; index++)
        {
            rgba[(index * 4) + 0] = 255;
            rgba[(index * 4) + 1] = 140;
            rgba[(index * 4) + 2] = 30;
            rgba[(index * 4) + 3] = 255;
        }

        using SdlGpuImage source = new(ImageSize, ImageSize, rgba);
        using PrismImage image = global::Cerneala.Drawing.Prism.Prism.Apply(source, new OuterGlowStyle
        {
            Color = new Color(60, 220, 120),
            Size = 8,
            Opacity = 1f,
        });
        DrawCommandList commands = new();
        DrawingContext context = new(commands);
        DrawRect destination = new(Origin, Origin, ImageSize, ImageSize);
        if (nineSlice)
        {
            context.DrawNineSlice(image, destination, new DrawInsets(12));
        }
        else
        {
            context.DrawImage(image, destination, new DrawImageOptions());
        }

        Color[] pixels = fixture.Render(commands, Color.Black);
        int width = fixture.Session.PixelWidth;
        Color At(int x, int y) => pixels[(y * width) + x];

        // Find the physical box edges on a row and a column inside the box.
        int probeY = (int)((Origin + (ImageSize / 2f)) * scale);
        int probeX = (int)((Origin + (ImageSize / 2f)) * scale);
        int left = Enumerable.Range(0, width).First(x => IsBox(At(x, probeY)));
        int right = Enumerable.Range(0, width).Last(x => IsBox(At(x, probeY)));
        int top = Enumerable.Range(0, width).First(y => IsBox(At(probeX, y)));

        int[] Profile(int x, int y, int stepX, int stepY) =>
            Enumerable.Range(1, ProfileLength).Select(step => (int)At(x + (stepX * step), y + (stepY * step)).G).ToArray();

        int[] topNearLeft = Profile(left + 10, top, 0, -1);
        int[] topNearRight = Profile(right - 10, top, 0, -1);
        int[] leftNearTop = Profile(left, top + 10, -1, 0);
        int[] rightNearTop = Profile(right, top + 10, 1, 0);
        int[] rowAboveBox = Enumerable.Range(left + 1, right - left - 1).Select(x => (int)At(x, top - 5).G).ToArray();
        return new GlowMeasurement(
            topNearLeft.Count(value => value > 0),
            topNearLeft,
            topNearRight,
            leftNearTop,
            rightNearTop,
            rowAboveBox);
    }

    private static bool IsBox(Color color) =>
        color.R > 200 && color.G is > 100 and < 180 && color.B < 80;

    private sealed record GlowMeasurement(
        int Width,
        int[] TopNearLeft,
        int[] TopNearRight,
        int[] LeftNearTop,
        int[] RightNearTop,
        int[] RowAboveBox);
}
