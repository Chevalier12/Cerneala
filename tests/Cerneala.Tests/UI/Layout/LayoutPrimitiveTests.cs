using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Layout;

public sealed class LayoutPrimitiveTests
{
    [Fact]
    public void LayoutSizeCanRepresentUnconstrainedDimensions()
    {
        LayoutSize size = LayoutSize.Unconstrained;

        Assert.True(size.IsWidthUnconstrained);
        Assert.True(size.IsHeightUnconstrained);
    }

    [Fact]
    public void LayoutPointAndRectAreLayoutTypes()
    {
        LayoutPoint point = new(2, 3);
        LayoutRect rect = new(point.X, point.Y, 10, 20);

        Assert.Equal(point, rect.Location);
        Assert.Equal(new LayoutSize(10, 20), rect.Size);
    }

    [Fact]
    public void ThicknessReportsTotalEdges()
    {
        Thickness thickness = new(1, 2, 3, 4);

        Assert.Equal(4, thickness.Horizontal);
        Assert.Equal(6, thickness.Vertical);
    }

    [Fact]
    public void LayoutRoundingIsExplicit()
    {
        LayoutRounding rounding = LayoutRounding.Enabled;

        Assert.Equal(new LayoutSize(2, 4), rounding.Round(new LayoutSize(1.6f, 3.5f)));
        Assert.Equal(new LayoutSize(1.6f, 3.5f), LayoutRounding.Disabled.Round(new LayoutSize(1.6f, 3.5f)));
    }

    [Fact]
    public void LayoutRoundingUsesPhysicalPixelStepsAtDpiScale()
    {
        LayoutRounding rounding = LayoutRounding.ForScale(1.25f);

        Assert.Equal(84, rounding.Round(new LayoutSize(83.5859375f, 18.3984375f)).Width);
        Assert.Equal(118.4f, rounding.Round(118));
        Assert.Equal(70.4f, rounding.Round(70.8f));
    }

    [Fact]
    public void LayoutRoundingRoundsRectSizesToTheNearestDevicePixel()
    {
        Assert.Equal(
            new LayoutRect(0, 2, 33, 10),
            LayoutRounding.Enabled.Round(new LayoutRect(0.4f, 1.6f, 33.4f, 10.5f)));

        // 57 device pixels at scale 1.75 are 32.571428 px; float noise in the product must not add a device pixel.
        LayoutRounding rounding = LayoutRounding.ForScale(1.75f);
        Assert.Equal(57, MathF.Round(rounding.Round(new LayoutRect(0, 0, 57 / 1.75f, 1)).Width * 1.75f));
    }

    [Fact]
    public void LayoutRoundingKeepsASizeThatAlreadyLiesOnDevicePixels()
    {
        // (57 / 1.75f) * 1.75f is slightly more than 57; rounding the size up must keep 57 device pixels, not make 58.
        LayoutRounding rounding = LayoutRounding.ForScale(1.75f);

        Assert.Equal(new LayoutSize(57 / 1.75f, 121 / 1.75f), rounding.Round(new LayoutSize(57 / 1.75f, 121 / 1.75f)));
        Assert.Equal(new LayoutSize(57 / 1.75f, 115 / 1.75f), rounding.Round(new LayoutSize(32.5f, 65.2f)));
    }

    [Fact]
    public void LayoutRoundingNeverRoundsASizeBelowItsValue()
    {
        // Content 100.003 pixels wide at scale 1 needs 101 device pixels; 100 would cut off its last 0.003 pixels.
        Assert.Equal(new LayoutSize(101, 3), LayoutRounding.Enabled.Round(new LayoutSize(100.003f, 2.001f)));
    }

    [Fact]
    public void LayoutRoundingKeepsALargeSizeThatAlreadyLiesOnDevicePixels()
    {
        // (114692 / 1.75f) * 1.75f is 114692.0078125: one float step above 114692 device pixels, more than 1/256 of one.
        LayoutRounding rounding = LayoutRounding.ForScale(1.75f);
        LayoutSize size = new(114692 / 1.75f, 114692 / 1.75f);

        Assert.Equal(size, rounding.Round(size));
    }

    [Fact]
    public void VisibilityHasThreeStates()
    {
        Assert.Contains(Visibility.Visible, Enum.GetValues<Visibility>());
        Assert.Contains(Visibility.Hidden, Enum.GetValues<Visibility>());
        Assert.Contains(Visibility.Collapsed, Enum.GetValues<Visibility>());
    }
}
