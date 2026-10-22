using Cerneala.Drawing;
using Cerneala.Tests.SdlGpu;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Drawing.SdlGpu;

// The border is painted inside the arranged bounds (10, 10, 40, 30): columns 10..49, rows 10..39.
[Collection(SdlNativeTestCollection.Name)]
public sealed class BorderRenderingTests
{
    private const int Size = 64;
    private const int Left = 10;
    private const int Top = 10;
    private const int Width = 40;
    private const int Height = 30;
    private static readonly Color BorderColor = new(255, 0, 0);
    private static readonly Color BackgroundColor = new(0, 0, 255);

    [SdlNativeFact]
    public void SingleSideBorderPaintsOnlyThatSideInsideTheBounds()
    {
        Border border = new()
        {
            BorderBrush = new SolidColorBrush(BorderColor),
            BorderThickness = new Thickness(0, 0, 0, 2)
        };

        Color[] pixels = Render(border, out SdlDrawingFixture fixture);
        using (fixture)
        {
            // The bottom two rows, over the full width, are the border.
            Assert.True(IsBorder(At(fixture, pixels, 10, 38)), $"bottom-left {At(fixture, pixels, 10, 38)}");
            Assert.True(IsBorder(At(fixture, pixels, 30, 38)), $"bottom row 38 {At(fixture, pixels, 30, 38)}");
            Assert.True(IsBorder(At(fixture, pixels, 30, 39)), $"bottom row 39 {At(fixture, pixels, 30, 39)}");
            Assert.True(IsBorder(At(fixture, pixels, 49, 39)), $"bottom-right {At(fixture, pixels, 49, 39)}");
            // The row above the band, the top row, the left and right columns and the interior are not.
            Assert.True(IsClear(At(fixture, pixels, 30, 37)), $"row 37 {At(fixture, pixels, 30, 37)}");
            Assert.True(IsClear(At(fixture, pixels, 30, 10)), $"top row {At(fixture, pixels, 30, 10)}");
            Assert.True(IsClear(At(fixture, pixels, 10, 25)), $"left column {At(fixture, pixels, 10, 25)}");
            Assert.True(IsClear(At(fixture, pixels, 49, 25)), $"right column {At(fixture, pixels, 49, 25)}");
            Assert.True(IsClear(At(fixture, pixels, 30, 25)), $"interior {At(fixture, pixels, 30, 25)}");
            Assert.True(MaximumChannelOutsideBounds(pixels) <= 2, $"Maximum channel outside the arranged bounds {MaximumChannelOutsideBounds(pixels)}/255.");
        }
    }

    [SdlNativeFact]
    public void EachSideHasItsOwnBandWidth()
    {
        Border border = new()
        {
            BorderBrush = new SolidColorBrush(BorderColor),
            BorderThickness = new Thickness(1, 2, 3, 4)
        };

        Color[] pixels = Render(border, out SdlDrawingFixture fixture);
        using (fixture)
        {
            Assert.Equal(1, BandWidth(fixture, pixels, Left, 25, 1, 0));
            Assert.Equal(2, BandWidth(fixture, pixels, 30, Top, 0, 1));
            Assert.Equal(3, BandWidth(fixture, pixels, Left + Width - 1, 25, -1, 0));
            Assert.Equal(4, BandWidth(fixture, pixels, 30, Top + Height - 1, 0, -1));
            Assert.True(IsClear(At(fixture, pixels, 30, 25)), $"interior {At(fixture, pixels, 30, 25)}");
            Assert.True(MaximumChannelOutsideBounds(pixels) <= 2, $"Maximum channel outside the arranged bounds {MaximumChannelOutsideBounds(pixels)}/255.");
        }
    }

    [SdlNativeFact]
    public void UniformBorderStaysInsideTheArrangedBounds()
    {
        Border border = new()
        {
            BorderBrush = new SolidColorBrush(BorderColor),
            BorderThickness = new Thickness(4)
        };

        Color[] pixels = Render(border, out SdlDrawingFixture fixture);
        using (fixture)
        {
            Assert.Equal(4, BandWidth(fixture, pixels, Left, 25, 1, 0));
            Assert.Equal(4, BandWidth(fixture, pixels, 30, Top, 0, 1));
            Assert.Equal(4, BandWidth(fixture, pixels, Left + Width - 1, 25, -1, 0));
            Assert.Equal(4, BandWidth(fixture, pixels, 30, Top + Height - 1, 0, -1));
            Assert.True(IsClear(At(fixture, pixels, 30, 25)), $"interior {At(fixture, pixels, 30, 25)}");
            int outside = MaximumChannelOutsideBounds(pixels);
            Assert.True(outside <= 2, $"Maximum channel outside the arranged bounds {outside}/255.");
        }
    }

    [SdlNativeFact]
    public void UniformCornerRadiusRoundsTheBackgroundAndTheBorderAtAllFourCorners()
    {
        Border border = new()
        {
            Background = new SolidColorBrush(BackgroundColor),
            BorderBrush = new SolidColorBrush(BorderColor),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8)
        };

        Color[] pixels = Render(border, out SdlDrawingFixture fixture);
        using (fixture)
        {
            // 1px in from the corner of the bounds lies outside the radius-8 arc: nothing is painted there.
            Assert.True(IsClear(At(fixture, pixels, Left + 1, Top + 1)), $"top-left {At(fixture, pixels, Left + 1, Top + 1)}");
            Assert.True(IsClear(At(fixture, pixels, Left + Width - 2, Top + 1)), $"top-right {At(fixture, pixels, Left + Width - 2, Top + 1)}");
            Assert.True(IsClear(At(fixture, pixels, Left + Width - 2, Top + Height - 2)), $"bottom-right {At(fixture, pixels, Left + Width - 2, Top + Height - 2)}");
            Assert.True(IsClear(At(fixture, pixels, Left + 1, Top + Height - 2)), $"bottom-left {At(fixture, pixels, Left + 1, Top + Height - 2)}");
            // The middle of each side is the border, and the interior is the background.
            Assert.True(IsBorder(At(fixture, pixels, Left, 25)), $"left {At(fixture, pixels, Left, 25)}");
            Assert.True(IsBorder(At(fixture, pixels, Left + Width - 1, 25)), $"right {At(fixture, pixels, Left + Width - 1, 25)}");
            Assert.True(IsBorder(At(fixture, pixels, 30, Top)), $"top {At(fixture, pixels, 30, Top)}");
            Assert.True(IsBorder(At(fixture, pixels, 30, Top + Height - 1)), $"bottom {At(fixture, pixels, 30, Top + Height - 1)}");
            Assert.True(IsBackground(At(fixture, pixels, 30, 25)), $"interior {At(fixture, pixels, 30, 25)}");
            Assert.True(MaximumChannelOutsideBounds(pixels) <= 2, $"Maximum channel outside the arranged bounds {MaximumChannelOutsideBounds(pixels)}/255.");
        }
    }

    [SdlNativeFact]
    public void PerCornerRadiusRoundsOnlyTheRequestedCorners()
    {
        Border border = new()
        {
            Background = new SolidColorBrush(BackgroundColor),
            BorderBrush = new SolidColorBrush(BorderColor),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8, 0, 0, 8)
        };

        Color[] pixels = Render(border, out SdlDrawingFixture fixture);
        using (fixture)
        {
            // Top-left and bottom-left are rounded.
            Assert.True(IsClear(At(fixture, pixels, Left + 1, Top + 1)), $"top-left {At(fixture, pixels, Left + 1, Top + 1)}");
            Assert.True(IsClear(At(fixture, pixels, Left + 1, Top + Height - 2)), $"bottom-left {At(fixture, pixels, Left + 1, Top + Height - 2)}");
            // Top-right and bottom-right are square: the corner pixels are the border.
            Assert.True(IsBorder(At(fixture, pixels, Left + Width - 2, Top + 1)), $"top-right {At(fixture, pixels, Left + Width - 2, Top + 1)}");
            Assert.True(IsBorder(At(fixture, pixels, Left + Width - 2, Top + Height - 2)), $"bottom-right {At(fixture, pixels, Left + Width - 2, Top + Height - 2)}");
            Assert.True(IsBorder(At(fixture, pixels, Left + Width - 1, Top)), $"top-right corner {At(fixture, pixels, Left + Width - 1, Top)}");
            Assert.True(IsBorder(At(fixture, pixels, Left + Width - 1, Top + Height - 1)), $"bottom-right corner {At(fixture, pixels, Left + Width - 1, Top + Height - 1)}");
            Assert.True(IsBackground(At(fixture, pixels, 30, 25)), $"interior {At(fixture, pixels, 30, 25)}");
            Assert.True(MaximumChannelOutsideBounds(pixels) <= 2, $"Maximum channel outside the arranged bounds {MaximumChannelOutsideBounds(pixels)}/255.");
        }
    }

    private static Color[] Render(Border border, out SdlDrawingFixture fixture)
    {
        fixture = new SdlDrawingFixture(Size, Size, useMultisampling: true, coordinateScale: 1f);
        UIRoot root = new();
        root.VisualChildren.Add(border);
        root.ProcessFrame();
        border.Arrange(new ArrangeContext(new LayoutRect(Left, Top, Width, Height)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "border-native-contract");
        root.ProcessFrame();
        return fixture.Render(root.RetainedRenderer.Commit(root), Color.Black);
    }

    private static Color At(SdlDrawingFixture fixture, Color[] pixels, int x, int y) =>
        fixture.Sample(pixels, x, y);

    // Counts the border pixels from (x, y) along (dx, dy) until the first pixel that is not the border color.
    private static int BandWidth(SdlDrawingFixture fixture, Color[] pixels, int x, int y, int dx, int dy)
    {
        int count = 0;
        while (IsBorder(At(fixture, pixels, x, y)))
        {
            count++;
            x += dx;
            y += dy;
        }

        return count;
    }

    private static int MaximumChannelOutsideBounds(Color[] pixels)
    {
        int maximum = 0;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (x >= Left && x < Left + Width && y >= Top && y < Top + Height)
                {
                    continue;
                }

                Color pixel = pixels[(y * Size) + x];
                maximum = Math.Max(maximum, Math.Max(pixel.R, Math.Max(pixel.G, pixel.B)));
            }
        }

        return maximum;
    }

    private static bool IsBorder(Color pixel) => MaximumDelta(pixel, BorderColor) <= 2;

    private static bool IsBackground(Color pixel) => MaximumDelta(pixel, BackgroundColor) <= 2;

    private static bool IsClear(Color pixel) => MaximumDelta(pixel, Color.Black) <= 2;

    private static int MaximumDelta(Color first, Color second) => Math.Max(
        Math.Abs(first.R - second.R),
        Math.Max(Math.Abs(first.G - second.G), Math.Abs(first.B - second.B)));
}
