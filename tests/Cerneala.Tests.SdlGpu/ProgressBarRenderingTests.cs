using Cerneala.Drawing;
using Cerneala.Tests.SdlGpu;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Drawing.SdlGpu;

// The border of a ProgressBar is painted as a Border's is, around the arranged bounds (10, 10, 40, 30), columns 10..49
// and rows 10..39, each side with its own signed thickness: a positive side paints outside the bounds, a negative side
// paints inside them. Value is 0, so no progress fill is drawn and the bounds show the background.
[Collection(SdlNativeTestCollection.Name)]
public sealed class ProgressBarRenderingTests
{
    private const int Size = 64;
    private const int Left = 10;
    private const int Top = 10;
    private const int Width = 40;
    private const int Height = 30;
    private static readonly Color BorderColor = new(255, 0, 0);
    private static readonly Color BackgroundColor = new(0, 0, 255);

    [SdlNativeFact]
    public void DefaultBorderThicknessPaintsAOnePixelRingJustOutsideTheBounds()
    {
        Color[] pixels = Render(new Thickness(1), out SdlDrawingFixture fixture);
        using (fixture)
        {
            // The ring is column 9, column 50, row 9 and row 40, measured outward from the first pixel outside.
            Assert.Equal(1, BandWidth(fixture, pixels, Left - 1, 25, -1, 0));
            Assert.Equal(1, BandWidth(fixture, pixels, Left + Width, 25, 1, 0));
            Assert.Equal(1, BandWidth(fixture, pixels, 30, Top - 1, 0, -1));
            Assert.Equal(1, BandWidth(fixture, pixels, 30, Top + Height, 0, 1));
            // The outermost pixels inside the bounds are the background.
            Assert.True(IsBackground(At(fixture, pixels, Left, 25)), $"left inside {At(fixture, pixels, Left, 25)}");
            Assert.True(IsBackground(At(fixture, pixels, Left + Width - 1, 25)), $"right inside {At(fixture, pixels, Left + Width - 1, 25)}");
            Assert.True(IsBackground(At(fixture, pixels, 30, Top)), $"top inside {At(fixture, pixels, 30, Top)}");
            Assert.True(IsBackground(At(fixture, pixels, 30, Top + Height - 1)), $"bottom inside {At(fixture, pixels, 30, Top + Height - 1)}");
            int beyond = MaximumChannelOutside(pixels, Left - 1, Top - 1, Width + 2, Height + 2);
            Assert.True(beyond <= 2, $"Maximum channel beyond the ring {beyond}/255.");
        }
    }

    [SdlNativeFact]
    public void PositiveBottomSidePaintsOnlyThatBandBelowTheBounds()
    {
        Color[] pixels = Render(new Thickness(0, 0, 0, 2), out SdlDrawingFixture fixture);
        using (fixture)
        {
            // Rows 40 and 41, over the columns of the bounds, are the border; the bounds themselves are all background.
            Assert.True(IsBorder(At(fixture, pixels, 10, 40)), $"bottom-left row 40 {At(fixture, pixels, 10, 40)}");
            Assert.True(IsBorder(At(fixture, pixels, 30, 40)), $"row 40 {At(fixture, pixels, 30, 40)}");
            Assert.True(IsBorder(At(fixture, pixels, 30, 41)), $"row 41 {At(fixture, pixels, 30, 41)}");
            Assert.True(IsBorder(At(fixture, pixels, 49, 41)), $"bottom-right row 41 {At(fixture, pixels, 49, 41)}");
            Assert.True(IsBackground(At(fixture, pixels, 30, 39)), $"row 39 {At(fixture, pixels, 30, 39)}");
            Assert.True(IsBackground(At(fixture, pixels, 30, 38)), $"row 38 {At(fixture, pixels, 30, 38)}");
            Assert.True(IsBackground(At(fixture, pixels, 30, 10)), $"top row {At(fixture, pixels, 30, 10)}");
            Assert.True(IsBackground(At(fixture, pixels, 10, 25)), $"left column {At(fixture, pixels, 10, 25)}");
            Assert.True(IsBackground(At(fixture, pixels, 49, 25)), $"right column {At(fixture, pixels, 49, 25)}");
            int beyond = MaximumChannelOutside(pixels, Left, Top, Width, Height + 2);
            Assert.True(beyond <= 2, $"Maximum channel beyond the band {beyond}/255.");
        }
    }

    [SdlNativeFact]
    public void PositiveSidesHaveTheirOwnBandWidthOutsideTheBounds()
    {
        Color[] pixels = Render(new Thickness(1, 2, 3, 4), out SdlDrawingFixture fixture);
        using (fixture)
        {
            Assert.Equal(1, BandWidth(fixture, pixels, Left - 1, 25, -1, 0));
            Assert.Equal(2, BandWidth(fixture, pixels, 30, Top - 1, 0, -1));
            Assert.Equal(3, BandWidth(fixture, pixels, Left + Width, 25, 1, 0));
            Assert.Equal(4, BandWidth(fixture, pixels, 30, Top + Height, 0, 1));
            Assert.True(IsBackground(At(fixture, pixels, Left, 25)), $"left inside {At(fixture, pixels, Left, 25)}");
            Assert.True(IsBackground(At(fixture, pixels, 30, Top + Height - 1)), $"bottom inside {At(fixture, pixels, 30, Top + Height - 1)}");
            // The outer rectangle is columns 9..52 and rows 8..43.
            int beyond = MaximumChannelOutside(pixels, 9, 8, 44, 36);
            Assert.True(beyond <= 2, $"Maximum channel beyond the outer rectangle {beyond}/255.");
        }
    }

    [SdlNativeFact]
    public void NegativeBottomSidePaintsOnlyThatBandInsideTheBounds()
    {
        Color[] pixels = Render(new Thickness(0, 0, 0, -2), out SdlDrawingFixture fixture);
        using (fixture)
        {
            Assert.True(IsBorder(At(fixture, pixels, 30, 38)), $"bottom row 38 {At(fixture, pixels, 30, 38)}");
            Assert.True(IsBorder(At(fixture, pixels, 30, 39)), $"bottom row 39 {At(fixture, pixels, 30, 39)}");
            Assert.True(IsBackground(At(fixture, pixels, 30, 37)), $"row 37 {At(fixture, pixels, 30, 37)}");
            Assert.True(IsBackground(At(fixture, pixels, 30, 10)), $"top row {At(fixture, pixels, 30, 10)}");
            Assert.True(IsBackground(At(fixture, pixels, 10, 25)), $"left column {At(fixture, pixels, 10, 25)}");
            Assert.True(IsBackground(At(fixture, pixels, 49, 25)), $"right column {At(fixture, pixels, 49, 25)}");
            int outside = MaximumChannelOutside(pixels, Left, Top, Width, Height);
            Assert.True(outside <= 2, $"Maximum channel outside the arranged bounds {outside}/255.");
        }
    }

    [SdlNativeFact]
    public void NegativeSidesHaveTheirOwnBandWidthInsideTheBounds()
    {
        Color[] pixels = Render(new Thickness(-1, -2, -3, -4), out SdlDrawingFixture fixture);
        using (fixture)
        {
            Assert.Equal(1, BandWidth(fixture, pixels, Left, 25, 1, 0));
            Assert.Equal(2, BandWidth(fixture, pixels, 30, Top, 0, 1));
            Assert.Equal(3, BandWidth(fixture, pixels, Left + Width - 1, 25, -1, 0));
            Assert.Equal(4, BandWidth(fixture, pixels, 30, Top + Height - 1, 0, -1));
            int outside = MaximumChannelOutside(pixels, Left, Top, Width, Height);
            Assert.True(outside <= 2, $"Maximum channel outside the arranged bounds {outside}/255.");
        }
    }

    private static Color[] Render(Thickness thickness, out SdlDrawingFixture fixture)
    {
        ProgressBar progress = new()
        {
            Value = 0,
            Background = new SolidColorBrush(BackgroundColor),
            BorderBrush = new SolidColorBrush(BorderColor),
            BorderThickness = thickness
        };
        fixture = new SdlDrawingFixture(Size, Size, useMultisampling: true, coordinateScale: 1f);
        UIRoot root = new();
        root.VisualChildren.Add(progress);
        root.ProcessFrame();
        progress.Arrange(new ArrangeContext(new LayoutRect(Left, Top, Width, Height)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "progress-bar-native-contract");
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

    private static int MaximumDelta(Color first, Color second) => Math.Max(
        Math.Abs(first.R - second.R),
        Math.Max(Math.Abs(first.G - second.G), Math.Abs(first.B - second.B)));
}
