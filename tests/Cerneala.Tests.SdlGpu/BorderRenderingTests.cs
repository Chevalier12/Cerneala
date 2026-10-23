using Cerneala.Drawing;
using Cerneala.Tests.SdlGpu;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Drawing.SdlGpu;

// Negative sides paint inside the arranged bounds (10, 10, 40, 30), columns 10..49 and rows 10..39; positive sides paint
// outside them.
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
            BorderThickness = new Thickness(0, 0, 0, -2)
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
            BorderThickness = new Thickness(-1, -2, -3, -4)
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
            BorderThickness = new Thickness(-4)
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
            BorderThickness = new Thickness(-2),
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
            BorderThickness = new Thickness(-2),
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

    [SdlNativeFact]
    public void PositiveUniformBorderIsPaintedAroundTheBoundsWithSquareCorners()
    {
        Border border = new()
        {
            Background = new SolidColorBrush(BackgroundColor),
            BorderBrush = new SolidColorBrush(BorderColor),
            BorderThickness = new Thickness(4)
        };

        Color[] pixels = Render(border, out SdlDrawingFixture fixture);
        using (fixture)
        {
            // The outer rectangle is columns 6..53 and rows 6..43. Each band is measured outward from the first pixel
            // outside the bounds: columns 9..6, 50..53 and rows 9..6, 40..43.
            Assert.Equal(4, BandWidth(fixture, pixels, Left - 1, 25, -1, 0));
            Assert.Equal(4, BandWidth(fixture, pixels, Left + Width, 25, 1, 0));
            Assert.Equal(4, BandWidth(fixture, pixels, 30, Top - 1, 0, -1));
            Assert.Equal(4, BandWidth(fixture, pixels, 30, Top + Height, 0, 1));
            // The pixels just inside each side, and the corner pixel of the bounds, are the background.
            Assert.True(IsBackground(At(fixture, pixels, Left, 25)), $"left inside {At(fixture, pixels, Left, 25)}");
            Assert.True(IsBackground(At(fixture, pixels, Left + Width - 1, 25)), $"right inside {At(fixture, pixels, Left + Width - 1, 25)}");
            Assert.True(IsBackground(At(fixture, pixels, 30, Top)), $"top inside {At(fixture, pixels, 30, Top)}");
            Assert.True(IsBackground(At(fixture, pixels, 30, Top + Height - 1)), $"bottom inside {At(fixture, pixels, 30, Top + Height - 1)}");
            Assert.True(IsBackground(At(fixture, pixels, Left, Top)), $"top-left inside {At(fixture, pixels, Left, Top)}");
            // The outer corner blocks (6..9, 6..9) and the three others are border: the corners are square.
            for (int dy = 0; dy < 4; dy++)
            {
                for (int dx = 0; dx < 4; dx++)
                {
                    Assert.True(IsBorder(At(fixture, pixels, 6 + dx, 6 + dy)), $"top-left block {6 + dx},{6 + dy} {At(fixture, pixels, 6 + dx, 6 + dy)}");
                    Assert.True(IsBorder(At(fixture, pixels, 50 + dx, 6 + dy)), $"top-right block {50 + dx},{6 + dy} {At(fixture, pixels, 50 + dx, 6 + dy)}");
                    Assert.True(IsBorder(At(fixture, pixels, 6 + dx, 40 + dy)), $"bottom-left block {6 + dx},{40 + dy} {At(fixture, pixels, 6 + dx, 40 + dy)}");
                    Assert.True(IsBorder(At(fixture, pixels, 50 + dx, 40 + dy)), $"bottom-right block {50 + dx},{40 + dy} {At(fixture, pixels, 50 + dx, 40 + dy)}");
                }
            }

            int beyond = MaximumChannelOutside(pixels, 6, 6, 48, 38);
            Assert.True(beyond <= 2, $"Maximum channel beyond the outer rectangle {beyond}/255.");
        }
    }

    [SdlNativeFact]
    public void PositiveSidesHaveTheirOwnBandWidthOutsideTheBounds()
    {
        Border border = new()
        {
            Background = new SolidColorBrush(BackgroundColor),
            BorderBrush = new SolidColorBrush(BorderColor),
            BorderThickness = new Thickness(1, 2, 3, 4)
        };

        Color[] pixels = Render(border, out SdlDrawingFixture fixture);
        using (fixture)
        {
            // The outer rectangle is columns 9..52 and rows 8..43.
            Assert.Equal(1, BandWidth(fixture, pixels, Left - 1, 25, -1, 0));
            Assert.Equal(2, BandWidth(fixture, pixels, 30, Top - 1, 0, -1));
            Assert.Equal(3, BandWidth(fixture, pixels, Left + Width, 25, 1, 0));
            Assert.Equal(4, BandWidth(fixture, pixels, 30, Top + Height, 0, 1));
            Assert.True(IsBackground(At(fixture, pixels, Left, 25)), $"left inside {At(fixture, pixels, Left, 25)}");
            Assert.True(IsBackground(At(fixture, pixels, Left + Width - 1, 25)), $"right inside {At(fixture, pixels, Left + Width - 1, 25)}");
            Assert.True(IsBackground(At(fixture, pixels, 30, Top)), $"top inside {At(fixture, pixels, 30, Top)}");
            Assert.True(IsBackground(At(fixture, pixels, 30, Top + Height - 1)), $"bottom inside {At(fixture, pixels, 30, Top + Height - 1)}");
            Assert.True(IsBorder(At(fixture, pixels, 9, 8)), $"top-left outer corner {At(fixture, pixels, 9, 8)}");
            Assert.True(IsBorder(At(fixture, pixels, 52, 43)), $"bottom-right outer corner {At(fixture, pixels, 52, 43)}");
            int beyond = MaximumChannelOutside(pixels, 9, 8, 44, 36);
            Assert.True(beyond <= 2, $"Maximum channel beyond the outer rectangle {beyond}/255.");
        }
    }

    // Mixed signs: -4 on the left is inside (columns 10..13), 3 on the top is outside (rows 7..9), -2 on the right is
    // inside (columns 48..49), 3 on the bottom is outside (rows 40..42). The outer rectangle is columns 10..49 and rows
    // 7..42, so the top-left outer corner (10, 7) is border.
    [SdlNativeFact]
    public void MixedSignSidesPaintInsideOnNegativeSidesAndOutsideOnPositiveSides()
    {
        Border border = new()
        {
            Background = new SolidColorBrush(BackgroundColor),
            BorderBrush = new SolidColorBrush(BorderColor),
            BorderThickness = new Thickness(-4, 3, -2, 3)
        };

        Color[] pixels = Render(border, out SdlDrawingFixture fixture);
        using (fixture)
        {
            Assert.Equal(4, BandWidth(fixture, pixels, Left, 25, 1, 0));
            Assert.Equal(2, BandWidth(fixture, pixels, Left + Width - 1, 25, -1, 0));
            Assert.Equal(3, BandWidth(fixture, pixels, 30, Top - 1, 0, -1));
            Assert.Equal(3, BandWidth(fixture, pixels, 30, Top + Height, 0, 1));
            // The top and bottom rows of the bounds are background, since those sides are outside.
            Assert.True(IsBackground(At(fixture, pixels, 30, Top)), $"top row {At(fixture, pixels, 30, Top)}");
            Assert.True(IsBackground(At(fixture, pixels, 30, Top + Height - 1)), $"bottom row {At(fixture, pixels, 30, Top + Height - 1)}");
            Assert.True(IsBackground(At(fixture, pixels, 30, 25)), $"interior {At(fixture, pixels, 30, 25)}");
            Assert.True(IsBorder(At(fixture, pixels, 10, 7)), $"top-left outer corner {At(fixture, pixels, 10, 7)}");
            Assert.True(IsBorder(At(fixture, pixels, 49, 42)), $"bottom-right outer corner {At(fixture, pixels, 49, 42)}");
            int beyond = MaximumChannelOutside(pixels, 10, 7, 40, 36);
            Assert.True(beyond <= 2, $"Maximum channel beyond the outer rectangle {beyond}/255.");
        }
    }

    // Bounds (10, 10, 40, 30), CornerRadius 8, thickness +4. The bounds, and the background, have the radius-8 corner
    // whose top-left arc has its centre at (18, 18). The outer figure is (6, 6, 48, 38) with radius 8 + 4 = 12 around
    // the same centre. A pixel centre (x + 0.5, y + 0.5) is in the ring when its distance from the corner centre is
    // between 8 and 12: (10, 10) is at 10.6, so it lies inside the bounds, outside the rounded background, and is border;
    // (11, 11) is at 9.2; (13, 13) is at 6.4, inside radius 8, so it is background; (6, 6) is at 16.3 and (7, 7) at 14.9,
    // beyond 12, so they are clear. The bottom-right corner centre is (42, 32): (49, 39) is at 10.6, border, and
    // (53, 43) at 16.3, clear. Mid-left (8, 25) is on the straight part of the left band (columns 6..9, rows 18..32).
    [SdlNativeFact]
    public void PositiveBorderWithACornerRadiusKeepsItsWidthAroundTheRoundedCorner()
    {
        Border border = new()
        {
            Background = new SolidColorBrush(BackgroundColor),
            BorderBrush = new SolidColorBrush(BorderColor),
            BorderThickness = new Thickness(4),
            CornerRadius = new CornerRadius(8)
        };

        Color[] pixels = Render(border, out SdlDrawingFixture fixture);
        using (fixture)
        {
            Assert.True(IsBorder(At(fixture, pixels, 10, 10)), $"(10,10) {At(fixture, pixels, 10, 10)}");
            Assert.True(IsBorder(At(fixture, pixels, 11, 11)), $"(11,11) {At(fixture, pixels, 11, 11)}");
            Assert.True(IsBackground(At(fixture, pixels, 13, 13)), $"(13,13) {At(fixture, pixels, 13, 13)}");
            Assert.True(IsClear(At(fixture, pixels, 6, 6)), $"(6,6) {At(fixture, pixels, 6, 6)}");
            Assert.True(IsClear(At(fixture, pixels, 7, 7)), $"(7,7) {At(fixture, pixels, 7, 7)}");
            Assert.True(IsBorder(At(fixture, pixels, 8, 25)), $"mid-left {At(fixture, pixels, 8, 25)}");
            Assert.True(IsBorder(At(fixture, pixels, 49, 39)), $"(49,39) {At(fixture, pixels, 49, 39)}");
            Assert.True(IsClear(At(fixture, pixels, 53, 43)), $"(53,43) {At(fixture, pixels, 53, 43)}");
            Assert.True(IsBackground(At(fixture, pixels, 30, 25)), $"interior {At(fixture, pixels, 30, 25)}");
            int beyond = MaximumChannelOutside(pixels, 6, 6, 48, 38);
            Assert.True(beyond <= 2, $"Maximum channel beyond the outer rectangle {beyond}/255.");
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

    private static int MaximumChannelOutsideBounds(Color[] pixels) => MaximumChannelOutside(pixels, Left, Top, Width, Height);

    // The largest channel of any pixel outside the pixel rectangle (left, top, width, height).
    private static int MaximumChannelOutside(Color[] pixels, int left, int top, int width, int height)
    {
        int maximum = 0;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (x >= left && x < left + width && y >= top && y < top + height)
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
